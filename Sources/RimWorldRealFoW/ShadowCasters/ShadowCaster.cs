// Unknown lincense
// Source: https://blogs.msdn.microsoft.com/ericlippert/tag/shadowcasting/

using System;

namespace RimWorldRealFoW.ShadowCasters {
	// Octants
	//
	//
	//                 \2|1/
	//                 3\|/0
	//               ----+----
	//                 4/|\7
	//                 /5|6\
	//
	// 

	public class ShadowCaster {
		// Takes a circle in the form of a center point and radius, and a function that
		// can tell whether a given cell is opaque. Calls the setFoV action on
		// every cell that is both within the radius and visible from the center. 

		private static FastQueue<ColumnPortion> queue = new FastQueue<ColumnPortion>(64);

		public static void computeFieldOfViewWithShadowCasting(
				int startX, int startY, int radius,
				bool[] viewBlockerCells, int maxX, int maxY,
				bool[] fovGrid, int fovGridMinX, int fovGridMinY, int fovGridWidth,
				byte specificOctant = 255,
				int targetX = -1,
				int targetY = -1) {

			if (specificOctant == 255) {
				for (byte octant = 0; octant < 8; ++octant) {
						computeFieldOfViewInOctantZero(
						octant,
						fovGrid,
						fovGridMinX,
						fovGridMinY,
						fovGridWidth,
						radius,
						startX,
						startY,
						maxX,
						maxY,
						viewBlockerCells,
						targetX,
						targetY);
				}
			} else {
				computeFieldOfViewInOctantZero(
					specificOctant,
					fovGrid,
					fovGridMinX,
					fovGridMinY,
					fovGridWidth,
					radius,
					startX,
					startY,
					maxX,
					maxY,
					viewBlockerCells,
					targetX,
					targetY);
			}
		}

		private static void computeFieldOfViewInOctantZero(
				byte octant,
				bool[] fovGrid, int fovGridMinX, int fovGridMinY, int fovGridWidth,
				int radius,
				int startX,
				int startY,
				int maxX,
				int maxY,
				bool[] viewBlockerCells,
				int targetX,
				int targetY) {

			queue.Enqueue(new ColumnPortion(0, new DirectionVector(1, 0), new DirectionVector(1, 1)));

			while (!queue.Empty()) {
				ColumnPortion current = queue.Dequeue();

				int x = current.X;
				if (x <= radius) {
					DirectionVector topVector = current.TopVector;
					DirectionVector bottomVector = current.BottomVector;

					// This method has two main purposes: (1) it marks points inside the
					// portion that are within the radius as in the field of view, and 
					// (2) it computes which portions of the following column are in the 
					// field of view, and puts them on a work queue for later processing. 

					// Search for transitions from opaque to transparent or
					// transparent to opaque and use those to determine what
					// portions of the *next* column are visible from the origin.

					// Start at the top of the column portion and work down.

					int topY;
					if (x == 0) {
						topY = 0;
					} else {
						int quotient = (2 * x + 1) * topVector.Y / (2 * topVector.X);
						int remainder = (2 * x + 1) * topVector.Y % (2 * topVector.X);

						if (remainder > topVector.X) {
							topY = quotient + 1;
						} else {
							topY = quotient;
						}
					}

					// Note that this can find a top cell that is actually entirely blocked by
					// the cell below it; consider detecting and eliminating that.

					int bottomY;
					if (x == 0) {
						bottomY = 0;
					} else {
						int quotient = (2 * x - 1) * bottomVector.Y / (2 * bottomVector.X);
						int remainder = (2 * x - 1) * bottomVector.Y % (2 * bottomVector.X);

						if (remainder >= bottomVector.X) {
							bottomY = quotient + 1;
						} else {
							bottomY = quotient;
						}
					}

					// A more sophisticated algorithm would say that a cell is visible if there is 
					// *any* straight line segment that passes through *any* portion of the origin cell
					// and any portion of the target cell, passing through only transparent cells
					// along the way. This is the "Permissive Field Of View" algorithm, and it
					// is much harder to implement.

					bool wasLastCellOpaque = false;
					bool lastCellCalcuated = false;

					bool inRadius;
					bool currentIsOpaque;

					int worldY = 0;
					int worldX = 0;
					if (octant == 1 || octant == 2) {
						worldY = startY + x;
					} else if (octant == 3 || octant == 4) {
						worldX = startX - x;
					} else if (octant == 5 || octant == 6) {
						worldY = startY - x;
					} else {
						worldX = startX + x;
					}

					for (int y = topY; y >= bottomY; --y) {
						if (octant == 1 || octant == 6) {
							worldX = startX + y;
						} else if (octant == 2 || octant == 5) {
							worldX = startX - y;
						} else if (octant == 4 || octant == 7) {
							worldY = startY - y;
						} else {
							worldY = startY + y;
						}

						// Is the lower-left corner of cell (x,y) within the radius?
						inRadius = (2 * x - 1) * (2 * x - 1) + (2 * y - 1) * (2 * y - 1) <= 4 * radius * radius;

						if (inRadius && worldX >= 0 && worldY >= 0 && worldX < maxX && worldY < maxY) {
							if (targetX == -1) {
								fovGrid[((worldY - fovGridMinY) * fovGridWidth) + (worldX - fovGridMinX)] = true;

							} else if (targetX == worldX && targetY == worldY) {
								// The current cell is in the field of view.
								// TODO: setFieldOfView(worldX, worldY);
								fovGrid[0] = true;
								queue.Clear();
								return;
							}
						}

						// A cell that was too far away to be seen is effectively
						// an opaque cell; nothing "above" it is going to be visible
						// in the next column, so we might as well treat it as 
						// an opaque cell and not scan the cells that are also too
						// far away in the next column.
						currentIsOpaque = !inRadius || worldX < 0 || worldY < 0 || worldX >= maxX || worldY >= maxY || viewBlockerCells[(worldY * maxX) + worldX];

						if (lastCellCalcuated) {
							if (currentIsOpaque) {
								// We've found a boundary from transparent to opaque. Make a note
								// of it and revisit it later.
								if (!wasLastCellOpaque) {
									// The new bottom vector touches the upper left corner of 
									// opaque cell that is below the transparent cell. 
									queue.Enqueue(new ColumnPortion(
											x + 1,
											new DirectionVector(x * 2 - 1, y * 2 + 1),
											topVector));
								}
							} else if (wasLastCellOpaque) {
								// We've found a boundary from opaque to transparent. Adjust the
								// top vector so that when we find the next boundary or do
								// the bottom cell, we have the right top vector.
								//
								// The new top vector touches the lower right corner of the 
								// opaque cell that is above the transparent cell, which is
								// the upper right corner of the current transparent cell.
								topVector = new DirectionVector(x * 2 + 1, y * 2 + 1);
							}
						}
						lastCellCalcuated = true;
						wasLastCellOpaque = currentIsOpaque;
					}

					// Make a note of the lowest opaque-->transparent transition, if there is one. 
					if (lastCellCalcuated && !wasLastCellOpaque)
						queue.Enqueue(new ColumnPortion(x + 1, bottomVector, topVector));
				}
			}
		}

		private class FastQueue<T> {
			private T[] nodes;
			private int currentPos;
			private int nextInsertPos;

			public FastQueue(int size) {
				nodes = new T[size];
				currentPos = 0;
				nextInsertPos = 0;
			}

			public void Enqueue(T value) {
				nodes[nextInsertPos++] = value;
				if (nextInsertPos >= nodes.Length) {
					nextInsertPos = 0;
				}

				// Overlap! Grow needed.
				if (nextInsertPos == currentPos) {
					T[] newNodes = new T[nodes.Length * 2];
					if (nextInsertPos == 0) {
						nextInsertPos = nodes.Length;
						Array.Copy(nodes, newNodes, nodes.Length);
					} else {
						Array.Copy(nodes, 0, newNodes, 0, nextInsertPos	);
						Array.Copy(nodes, currentPos, newNodes, newNodes.Length - (nodes.Length - currentPos), nodes.Length - currentPos);
						currentPos = newNodes.Length - (nodes.Length - currentPos);
					}
					nodes = newNodes;
				}
			}
			public T Dequeue() {
				int ret = currentPos++;
				if (currentPos >= nodes.Length) {
					currentPos = 0;
				}
				return nodes[ret];
			}

			public void Clear() {
				currentPos = 0;
				nextInsertPos = 0;
			}

			public bool Empty() {
				return currentPos == nextInsertPos;
			}
		}


		private struct ColumnPortion {
			public int X;
			public DirectionVector BottomVector;
			public DirectionVector TopVector;

			public ColumnPortion(int x, DirectionVector bottom, DirectionVector top) {
				this.X = x;
				this.BottomVector = bottom;
				this.TopVector = top;
			}
		}
		private struct DirectionVector {
			public int X;
			public int Y;

			public DirectionVector(int x, int y) {
				this.X = x;
				this.Y = y;
			}
		}
	}
}
