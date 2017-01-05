//   Copyright 2017 Luca De Petrillo
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	static class _Verb {
		private static bool CanHitCellFromCellIgnoringRange(this Verb _this, IntVec3 sourceSq, IntVec3 targetLoc) {
			if (_this.caster.Faction != null && _this.caster is Pawn) {
				return (!_this.verbProps.mustCastOnOpenGround || (targetLoc.Standable(_this.caster.Map) && !_this.caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn))) && (!_this.verbProps.requireLineOfSight || 
					(GenSight.LineOfSight(sourceSq, targetLoc, _this.caster.Map, true) && (seenByFaction(_this.caster, targetLoc) || fovLineOfSight(sourceSq, targetLoc, _this.caster))));
			}

			return (!_this.verbProps.mustCastOnOpenGround || (targetLoc.Standable(_this.caster.Map) && !_this.caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn))) && (!_this.verbProps.requireLineOfSight || GenSight.LineOfSight(sourceSq, targetLoc, _this.caster.Map, true));
		}

		private static bool seenByFaction(Thing thing, IntVec3 targetLoc) {
			MapComponentSeenFog seenFog = thing.Map.GetComponent<MapComponentSeenFog>();
			if (seenFog != null) {
				return seenFog.getShownCells(thing.Faction)[thing.Map.cellIndices.CellToIndex(targetLoc)] != 0;
			}

			return true;
		}

		private static bool fovLineOfSight(IntVec3 sourceSq, IntVec3 targetLoc, Thing thing) {
			CompFieldOfView compFoV = thing.TryGetComp<CompFieldOfView>();
			int sightRange = Mathf.RoundToInt(compFoV.calcPawnSightRange(sourceSq));

			if (!sourceSq.InHorDistOf(targetLoc, sightRange)) {
				// If out of sightRange.
				return false;
			}


			// Limit to needed octant.
			IntVec3 dir = targetLoc - sourceSq;

			int octant;
			if (dir.x >= 0) {
				if (dir.z >= 0) {
					if (dir.x >= dir.z) {
						octant = 0;
					} else {
						octant = 1;
					}
				} else {
					if (dir.x >= -dir.z) {
						octant = 7;
					} else {
						octant = 6;
					}
				}
			} else {
				if (dir.z >= 0) {
					if (-dir.x >= dir.z) {
						octant = 3;
					} else {
						octant = 2;
					}
				} else {
					if (-dir.x >= -dir.z) {
						octant = 4;
					} else {
						octant = 5;
					}
				}
			}

			Map map = thing.Map;
			bool targetFound = false;
			compFoV.shadowCaster.computeFieldOfViewWithShadowCasting(sourceSq.x, sourceSq.z, sightRange,
					// isOpaque
					(int x, int y) => {
						// Once target found, all other cells are opaque.
						if (targetFound) {
							return true;
						}
						// Out of map positions are opaques...
						if (x < 0 || y < 0 || x >= map.Size.x || y >= map.Size.z) {
							return true;
						}
						Building b = map.edificeGrid[map.cellIndices.CellToIndex(x, y)];
						return (b != null && !b.CanBeSeenOver());
					},
					// setFoV
					(int x, int y) => {
						if (targetLoc.x == x && targetLoc.z == y) {
							targetFound = true;
						}
					},
					octant);

			return targetFound;
		}
	}
}
