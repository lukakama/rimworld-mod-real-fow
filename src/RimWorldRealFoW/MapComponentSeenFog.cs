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
	class MapComponentSeenFog : MapComponent {
		public uint[] shownCells = null;
		public bool[] revealedCells = null;

		bool initialized = false;

		public MapComponentSeenFog(Map map) : base(map) {
			shownCells = new uint[map.cellIndices.NumGridCells];
			revealedCells = new bool[map.cellIndices.NumGridCells];
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();

			if (!initialized) {
				initialized = true;
				init();
			}
		}

		private void init() {
			// First tick: hide all unseen objects.
			foreach (IntVec3 cell in map.AllCells) {
				foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
					if ((t.Faction == null || !t.Faction.IsPlayer) && shownCells[map.cellIndices.CellToIndex(cell)] == 0u) {
						CompHiddenable comp = t.TryGetComp<CompHiddenable>();
						if (comp != null) {
							comp.hide();
						}
					}
				}
			}

			// Update all thing pov.
			foreach (Thing thing in map.listerThings.AllThings) {
				CompFieldOfView comp = thing.TryGetComp<CompFieldOfView>();
				if (comp != null) {
					comp.updateFoV();
				}
			}

			// Reveal the starting position if home map and no pawns (landing).
			if (map.IsPlayerHome && map.mapPawns.ColonistsSpawnedCount == 0) {
				IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;

				ShadowCaster.ComputeFieldOfViewWithShadowCasting(playerStartSpot.x, playerStartSpot.z, Mathf.RoundToInt(CompFieldOfView.MAX_RANGE),
					// isOpaque
					(int x, int y) => {
						if (x < 0 || y < 0 || x >= map.Size.x || y >= map.Size.z) {
							return true;
						}
						Building b = map.edificeGrid[map.cellIndices.CellToIndex(x, y)];
						return (b != null && !b.CanBeSeenOver());
					},
					// setFoV
					(int x, int y) => {
						if (!revealedCells[map.cellIndices.CellToIndex(x, y)]) {
							IntVec3 cell = new IntVec3(x, 0, y);
							revealedCells[map.cellIndices.CellToIndex(x, y)] = true;
						}
					});
			}

			// Redraw everything.
			foreach (IntVec3 current in map.AllCells) {
				map.mapDrawer.MapMeshDirty(current, SectionLayer_FoVLayer.mapMeshFlag | MapMeshFlag.Things, true, false);
			}
		}

		public override void ExposeData() {
			base.ExposeData();

			ArrayExposeUtility.ExposeBoolArray(ref revealedCells, map.Size.x, map.Size.z, "revealedCells");
		}

		public void refogAll() {
			FogGrid fogGrid = map.fogGrid;
			for (int i = 0; i < fogGrid.fogGrid.Length; i++) {
				fogGrid.fogGrid[i] = true;
			}
			foreach (IntVec3 current in map.AllCells) {
				map.mapDrawer.MapMeshDirty(current, MapMeshFlag.FogOfWar | SectionLayer_FoVLayer.mapMeshFlag);
			}
			FloodFillerFog.FloodUnfog(map.mapPawns.FreeColonistsSpawned.RandomElement<Pawn>().Position, map);

			foreach (Thing thing in map.listerThings.AllThings) {
				CompFieldOfView comp = thing.TryGetComp<CompFieldOfView>();
				if (comp != null) {
					comp.updateFoV();
				}
			}
		}

		public void incrementSeen(IntVec3 cell) {
			int idx = map.cellIndices.CellToIndex(cell);
			if (++shownCells[idx] == 1u) {
				if (!revealedCells[idx]) {
					revealedCells[idx] = true;
				}

				FogGrid fogGrid = map.fogGrid;
				if (fogGrid.fogGrid[idx]) {
					fogGrid.Unfog(cell);
				}

				if (initialized) {
					map.mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
					if (t.Faction == null || !t.Faction.IsPlayer) {
						CompHiddenable comp = t.TryGetComp<CompHiddenable>();
						if (comp != null) {
							comp.show();
						}
					}
				}
			}
		}

		internal void decrementSeen(IntVec3 cell) {
			int idx = map.cellIndices.CellToIndex(cell);

			if (--shownCells[idx] == 0u) {
				if (initialized) {
					map.mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
					if (t.Faction == null || !t.Faction.IsPlayer) {
						CompHiddenable comp = t.TryGetComp<CompHiddenable>();
						if (comp != null) {
							comp.hide();
						}
					}
				}
			}
		}
	}
}
