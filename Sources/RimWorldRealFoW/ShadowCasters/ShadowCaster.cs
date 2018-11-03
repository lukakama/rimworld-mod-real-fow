// Unknown lincense
// Source: https://blogs.msdn.microsoft.com/ericlippert/tag/shadowcasting/

using RimWorld;
using RimWorldRealFoW.Utils;
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

	// Compared to original algorithm, this version has been modded to work with a fast iterive 
	// approach with structs references, custom queue and witouth deferreds, improving overall 
	// performances and resources usage.
	public class ShadowCaster {
		private static ColumnPortionQueue queue = new ColumnPortionQueue(64);

		public static void computeFieldOfViewWithShadowCasting(
				int startX, int startY, int radius,
				bool[] viewBlockerCells, int maxX, int maxY,
				bool handleSeenAndCache, MapComponentSeenFog mapCompSeenFog, Faction faction, int[] factionShownCells,
				bool[] fovGrid, int fovGridMinX, int fovGridMinY, int fovGridWidth,
				bool[] oldFovGrid, int oldFovGridMinX, int oldFovGridMaxX, int oldFovGridMinY, int oldFovGridMaxY, int oldFovGridWidth,
				byte specificOctant = 255,
				int targetX = -1,
				int targetY = -1) {

#if InternalProfile
			ProfilingUtils.startProfiling("computeFieldOfViewWithShadowCasting");
#endif
			int r_r_4 = 4 * radius * radius;

			if (specificOctant == 255) {
				for (byte octant = 0; octant < 8; ++octant) {
					computeFieldOfViewInOctantZero(
					octant,
					fovGrid,
					fovGridMinX,
					fovGridMinY,
					fovGridWidth,
					oldFovGrid,
					oldFovGridMinX,
					oldFovGridMaxX,
					oldFovGridMinY,
					oldFovGridMaxY,
					oldFovGridWidth,
					radius,
					r_r_4,
					startX,
					startY,
					maxX,
					maxY,
					viewBlockerCells,
					handleSeenAndCache, mapCompSeenFog, faction, factionShownCells,
					targetX,
					targetY,
					0, 1, 1, 1, 0);
				}
			} else {
				computeFieldOfViewInOctantZero(
					specificOctant,
					fovGrid,
					fovGridMinX,
					fovGridMinY,
					fovGridWidth,
					oldFovGrid,
					oldFovGridMinX,
					oldFovGridMaxX,
					oldFovGridMinY,
					oldFovGridMaxY,
					oldFovGridWidth,
					radius,
					r_r_4,
					startX,
					startY,
					maxX,
					maxY,
					viewBlockerCells,
					handleSeenAndCache, mapCompSeenFog, faction, factionShownCells,
					targetX,
					targetY,
					0, 1, 1, 1, 0);
			}

#if InternalProfile
			ProfilingUtils.stopProfiling("computeFieldOfViewWithShadowCasting");
#endif
		}

		private static void computeFieldOfViewInOctantZero(
				byte octant,
				bool[] fovGrid,
				int fovGridMinX,
				int fovGridMinY,
				int fovGridWidth,
				bool[] oldFovGrid,
				int oldFovGridMinX,
				int oldFovGridMaxX,
				int oldFovGridMinY,
				int oldFovGridMaxY,
				int oldFovGridWidth,
				int radius,
				int r_r_4,
				int startX,
				int startY,
				int maxX,
				int maxY,
				bool[] viewBlockerCells,
				bool handleSeenAndCache,
				MapComponentSeenFog mapCompSeenFog,
				Faction faction,
				int[] factionShownCells,
				int targetX,
				int targetY,
				int x,
				int topVectorX,
				int topVectorY,
				int bottomVectorX,
				int bottomVectorY) {

			int topY;
			int bottomY;
			bool inRadius;
			bool currentIsOpaque;

			bool wasLastCellOpaque;
			bool lastCellCalcuated;

			int quotient;
			int remainder;

			int fogGridIdx;
			int oldFogGridIdx;

			int x2;
			int y2;

			int worldY = 0;
			int worldX = 0;
			int worldIdx = 0;

			bool firstIteration = true;

			while (firstIteration || !queue.Empty()) {
				if (!firstIteration) {
					ref ColumnPortion columnPortion = ref queue.Dequeue();
					x = columnPortion.x;
					topVectorX = columnPortion.topVectorX;
					topVectorY = columnPortion.topVectorY;
					bottomVectorX = columnPortion.bottomVectorX;
					bottomVectorY = columnPortion.bottomVectorY;
				} else {
					firstIteration = false;
				}

				while (x <= radius) {
					x2 = 2 * x;

					// This method has two main purposes: (1) it marks points inside the
					// portion that are within the radius as in the field of view, and 
					// (2) it computes which portions of the following column are in the 
					// field of view, and puts them on a work queue for later processing. 

					// Search for transitions from opaque to transparent or
					// transparent to opaque and use those to determine what
					// portions of the *next* column are visible from the origin.

					// Start at the top of the column portion and work down.

					if (x == 0) {
						topY = 0;
					} else {
						quotient = (x2 + 1) * topVectorY / (2 * topVectorX);
						remainder = (x2 + 1) * topVectorY % (2 * topVectorX);

						if (remainder > topVectorX) {
							topY = quotient + 1;
						} else {
							topY = quotient;
						}
					}

					// Note that this can find a top cell that is actually entirely blocked by
					// the cell below it; consider detecting and eliminating that.

					if (x == 0) {
						bottomY = 0;
					} else {
						quotient = (x2 - 1) * bottomVectorY / (2 * bottomVectorX);
						remainder = (x2 - 1) * bottomVectorY % (2 * bottomVectorX);

						if (remainder >= bottomVectorX) {
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

					wasLastCellOpaque = false;
					lastCellCalcuated = false;

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
						y2 = 2 * y;

						if (octant == 1 || octant == 6) {
							worldX = startX + y;
						} else if (octant == 2 || octant == 5) {
							worldX = startX - y;
						} else if (octant == 4 || octant == 7) {
							worldY = startY - y;
						} else {
							worldY = startY + y;
						}

						worldIdx = (worldY * maxX) + worldX;
						
						// Is the lower-left corner of cell (x,y) within the radius?
						inRadius = x * x + y * y <= radius * radius;

						if (inRadius && worldX >= 0 && worldY >= 0 && worldX < maxX && worldY < maxY) {
							if (targetX == -1) {
								fogGridIdx = ((worldY - fovGridMinY) * fovGridWidth) + (worldX - fovGridMinX);
								if (!fovGrid[fogGridIdx]) {
									fovGrid[fogGridIdx] = true;
									if (handleSeenAndCache) {
										if (oldFovGrid == null || worldX < oldFovGridMinX || worldY < oldFovGridMinY || worldX > oldFovGridMaxX || worldY > oldFovGridMaxY) {
											mapCompSeenFog.incrementSeen(faction, factionShownCells, worldIdx);
										} else {
											oldFogGridIdx = ((worldY - oldFovGridMinY) * oldFovGridWidth) + (worldX - oldFovGridMinX);
											if (!oldFovGrid[oldFogGridIdx]) {
												// Old cell was not visible. Increment seen counter in global grid.
												mapCompSeenFog.incrementSeen(faction, factionShownCells, worldIdx);
											} else {
												// Old cell was already visible. Mark it to not be unseen.
												oldFovGrid[oldFogGridIdx] = false;
											}
										}
									}
								}

							} else if (targetX == worldX && targetY == worldY) {
								// The target cell is in the field of view.
								fovGrid[0] = true;
								return;
							}
						}

						// A cell that was too far away to be seen is effectively
						// an opaque cell; nothing "above" it is going to be visible
						// in the next column, so we might as well treat it as 
						// an opaque cell and not scan the cells that are also too
						// far away in the next column.
						currentIsOpaque = !inRadius || worldX < 0 || worldY < 0 || worldX >= maxX || worldY >= maxY || viewBlockerCells[worldIdx];

						if (lastCellCalcuated) {
							if (currentIsOpaque) {
								// We've found a boundary from transparent to opaque. Make a note
								// of it and revisit it later.
								if (!wasLastCellOpaque) {
									// The new bottom vector touches the upper left corner of 
									// opaque cell that is below the transparent cell. 
									ref ColumnPortion columnPortion = ref queue.Enqueue();
									columnPortion.x = x + 1;
									columnPortion.topVectorX = topVectorX;
									columnPortion.topVectorY = topVectorY;
									columnPortion.bottomVectorX = x2 - 1;
									columnPortion.bottomVectorY = y2 + 1;

								}
							} else if (wasLastCellOpaque) {
								// We've found a boundary from opaque to transparent. Adjust the
								// top vector so that when we find the next boundary or do
								// the bottom cell, we have the right top vector.
								//
								// The new top vector touches the lower right corner of the 
								// opaque cell that is above the transparent cell, which is
								// the upper right corner of the current transparent cell.
								topVectorX = x2 + 1;
								topVectorY = y2 + 1;
							}
						}
						lastCellCalcuated = true;
						wasLastCellOpaque = currentIsOpaque;
					}

					// Make a note of the lowest opaque-->transparent transition, if there is one. 
					if (lastCellCalcuated && !wasLastCellOpaque) {
						x += 1;
					} else {
						break;
					}
				}
			}
		}

		private class ColumnPortionQueue {
			private ColumnPortion[] nodes;
			private int currentPos;
			private int nextInsertPos;

			public ColumnPortionQueue(int size) {
				nodes = new ColumnPortion[size];
				currentPos = 0;
				nextInsertPos = 0;
			}

			public ref ColumnPortion Enqueue() {
				int pos = nextInsertPos++;

				if (nextInsertPos >= nodes.Length) {
					nextInsertPos = 0;
				}

				// Overlap! Grow needed.
				if (nextInsertPos == currentPos) {
					ColumnPortion[] newNodes = new ColumnPortion[nodes.Length * 2];
					if (nextInsertPos == 0) {
						nextInsertPos = nodes.Length;
						Array.Copy(nodes, newNodes, nodes.Length);
					} else {
						Array.Copy(nodes, 0, newNodes, 0, nextInsertPos);
						Array.Copy(nodes, currentPos, newNodes, newNodes.Length - (nodes.Length - currentPos), nodes.Length - currentPos);
						currentPos = newNodes.Length - (nodes.Length - currentPos);
					}
					nodes = newNodes;
				}

				return ref nodes[pos];
			}
			public ref ColumnPortion Dequeue() {
				int pos = currentPos++;
				if (currentPos >= nodes.Length) {
					currentPos = 0;
				}
				return ref nodes[pos];
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
			public int x;
			public int topVectorX;
			public int topVectorY;
			public int bottomVectorX;
			public int bottomVectorY;
		}
	}
}
