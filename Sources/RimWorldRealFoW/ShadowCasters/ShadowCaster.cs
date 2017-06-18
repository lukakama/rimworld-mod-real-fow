// Unknown lincense
// Source: https://blogs.msdn.microsoft.com/ericlippert/tag/shadowcasting/

using RimWorld;

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

	// Compared to original algorithm, this version has been modded to works with an hybrid
	// recursive and iterive approach without the use of structs and deferreds, improving overall 
	// performances and system resources usage.
	public class ShadowCaster {
		public static void computeFieldOfViewWithShadowCasting(
				int startX, int startY, int radius,
				bool[] viewBlockerCells, int maxX, int maxY,
				bool handleSeenAndCache, MapComponentSeenFog mapCompSeenFog, Faction faction,
				bool[] fovGrid, int fovGridMinX, int fovGridMinY, int fovGridWidth,
				bool[] oldFovGrid, int oldFovGridMinX, int oldFovGridMaxX, int oldFovGridMinY, int oldFovGridMaxY, int oldFovGridWidth,
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
					oldFovGrid,
					oldFovGridMinX,
					oldFovGridMaxX,
					oldFovGridMinY,
					oldFovGridMaxY,
					oldFovGridWidth,
					radius,
					4 * radius * radius,
					startX,
					startY,
					maxX,
					maxY,
					viewBlockerCells,
					handleSeenAndCache, mapCompSeenFog, faction,
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
					4 * radius * radius,
					startX,
					startY,
					maxX,
					maxY,
					viewBlockerCells,
					handleSeenAndCache, mapCompSeenFog, faction,
					targetX,
					targetY,
					0, 1, 1, 1, 0);
			}
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

			int worldY = 0;
			int worldX = 0;

			while (x <= radius) {
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
					quotient = (2 * x + 1) * topVectorY / (2 * topVectorX);
					remainder = (2 * x + 1) * topVectorY % (2 * topVectorX);

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
					quotient = (2 * x - 1) * bottomVectorY / (2 * bottomVectorX);
					remainder = (2 * x - 1) * bottomVectorY % (2 * bottomVectorX);

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
					inRadius = (2 * x - 1) * (2 * x - 1) + (2 * y - 1) * (2 * y - 1) <= r_r_4;

					if (inRadius && worldX >= 0 && worldY >= 0 && worldX < maxX && worldY < maxY) {
						if (targetX == -1) {
							fogGridIdx = ((worldY - fovGridMinY) * fovGridWidth) + (worldX - fovGridMinX);
							if (!fovGrid[fogGridIdx]) {
								fovGrid[fogGridIdx] = true;
								if (handleSeenAndCache) {
									if (oldFovGrid == null || worldX < oldFovGridMinX || worldY < oldFovGridMinY || worldX > oldFovGridMaxX || worldY > oldFovGridMaxY) {
										mapCompSeenFog.incrementSeen(faction, (worldY * maxX) + worldX);
									} else {
										oldFogGridIdx = ((worldY - oldFovGridMinY) * oldFovGridWidth) + (worldX - oldFovGridMinX);
										if (!oldFovGrid[oldFogGridIdx]) {
											// Old cell was not visible. Increment seen counter in global grid.
											mapCompSeenFog.incrementSeen(faction, (worldY * maxX) + worldX);
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
					currentIsOpaque = !inRadius || worldX < 0 || worldY < 0 || worldX >= maxX || worldY >= maxY || viewBlockerCells[(worldY * maxX) + worldX];

					if (lastCellCalcuated) {
						if (currentIsOpaque) {
							// We've found a boundary from transparent to opaque. Make a note
							// of it and revisit it later.
							if (!wasLastCellOpaque) {
								// The new bottom vector touches the upper left corner of 
								// opaque cell that is below the transparent cell. 
								computeFieldOfViewInOctantZero(octant, 
									fovGrid, fovGridMinX, fovGridMinY, fovGridWidth, 
									oldFovGrid, oldFovGridMinX, oldFovGridMaxX, oldFovGridMinY, oldFovGridMaxY, oldFovGridWidth, 
									radius, r_r_4, startX, startY, maxX, maxY, viewBlockerCells, 
									handleSeenAndCache, mapCompSeenFog, faction,
									targetX, targetY, x + 1, topVectorX, topVectorY, x * 2 - 1, y * 2 + 1);
								if (targetX != -1 && fovGrid[0]) {
									// Quit if looking for target and found it.
									return;
								}
							}
						} else if (wasLastCellOpaque) {
							// We've found a boundary from opaque to transparent. Adjust the
							// top vector so that when we find the next boundary or do
							// the bottom cell, we have the right top vector.
							//
							// The new top vector touches the lower right corner of the 
							// opaque cell that is above the transparent cell, which is
							// the upper right corner of the current transparent cell.
							topVectorX = x * 2 + 1;
							topVectorY = y * 2 + 1;
						}
					}
					lastCellCalcuated = true;
					wasLastCellOpaque = currentIsOpaque;
				}

				// Make a note of the lowest opaque-->transparent transition, if there is one. 
				if (lastCellCalcuated && !wasLastCellOpaque) {
					x += 1;
				} else {
					return;
				}
			}
		}
	}
}
