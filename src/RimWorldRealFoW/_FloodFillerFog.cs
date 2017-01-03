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
	class _FloodFillerFog {
		public static FloodUnfogResult FloodUnfog(IntVec3 root, Map map) {
			FloodUnfogResult result = default(FloodUnfogResult);
			if (Find.TickManager.TicksGame == 0) {
				ShadowCaster.ComputeFieldOfViewWithShadowCasting(root.x, root.z, Mathf.RoundToInt(CompFieldOfView.MAX_RANGE),
					// isOpaque
					(int x, int y) => {
						Building b = map.edificeGrid[map.cellIndices.CellToIndex(x, y)];
						return (b != null && !b.CanBeSeenOver());
					},
					// setFoV
					(int x, int y) => {
						IntVec3 cell = new IntVec3(x, 0, y);
						if (map.fogGrid.IsFogged(cell)) {
							map.fogGrid.Unfog(cell);
						}
					});
			} else {
				foreach (Thing thing in map.listerThings.AllThings) {
					CompFieldOfView comp = thing.TryGetComp<CompFieldOfView>();
					if (comp != null) {
						comp.updateFoV();
						result.mechanoidFound |= comp.hasMechanoidInSeenCell(map);
					}
				}
			}
			return result;
		}
	}
}
