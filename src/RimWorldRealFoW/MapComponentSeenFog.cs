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
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	class MapComponentSeenFog : MapComponent {
		public int[] factionsShownCells = null;
		public bool[] revealedCells = null;

		private int maxFactionLoadId;

		private int mapCellLength;
		private int mapSizeX;

		private bool initialized = false;

		private CellIndices cellIndices;

		public MapComponentSeenFog(Map map) : base(map) {
			mapCellLength = map.cellIndices.NumGridCells;
			cellIndices = map.cellIndices;
			mapSizeX = map.Size.x;

			maxFactionLoadId = Find.World.factionManager.AllFactionsListForReading.Count;

			factionsShownCells = new int[(mapCellLength * (maxFactionLoadId + 1)) - 1];
			revealedCells = new bool[mapCellLength];
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();

			if (!initialized) {
				initialized = true;
				init();
			}
		}

		public int getBaseIdx(Faction faction) {
			return faction.loadID * mapCellLength;
		}

		public int resolveIdx(Faction faction, IntVec3 cell) {
			if (maxFactionLoadId < faction.loadID) {
				maxFactionLoadId = faction.loadID;
				int[] newFactionShownCells = new int[(mapCellLength * (maxFactionLoadId + 1)) - 1];
				Array.Copy(factionsShownCells, newFactionShownCells, factionsShownCells.Length);
				factionsShownCells = newFactionShownCells;
			}

			return (faction.loadID * mapCellLength) + (cell.z * mapSizeX) + cell.x;
		}

		public bool isShown(Faction faction, IntVec3 cell) {
			return factionsShownCells[resolveIdx(faction, cell)] != 0;
		}

		private void init() {
			// Update all thing FoV and visibility.
			foreach (Thing thing in map.listerThings.AllThings) {
				CompFieldOfView compFoV = thing.TryGetComp<CompFieldOfView>();
				CompHideFromPlayer compVisibility = thing.TryGetComp<CompHideFromPlayer>();
				if (compFoV != null) {
					compFoV.updateFoV();
				}
				if (compVisibility != null) {
					compVisibility.updateVisibility(true);
				}
			}

			// Reveal the starting position if home map and no pawns (landing).
			if (map.IsPlayerHome && map.mapPawns.ColonistsSpawnedCount == 0) {
				IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;
				ShadowCaster shadowCaster = new ShadowCaster();
				shadowCaster.computeFieldOfViewWithShadowCasting(playerStartSpot.x, playerStartSpot.z, Mathf.RoundToInt(CompFieldOfView.NON_MECH_DEFAULT_RANGE),
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

		public void incrementSeen(Faction faction, IntVec3 cell) {
			if ((++factionsShownCells[resolveIdx(faction, cell)] == 1) && faction.IsPlayer) {
				int idx = map.cellIndices.CellToIndex(cell);
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
					CompHideFromPlayer comp = t.TryGetComp<CompHideFromPlayer>();
					if (comp != null) {
						comp.updateVisibility(true);
					}
				}
			}
		}

		internal void decrementSeen(Faction faction, IntVec3 cell) {
			if ((--factionsShownCells[resolveIdx(faction, cell)] == 0) && faction.IsPlayer) {
				if (initialized) {
					map.mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
					CompHideFromPlayer comp = t.TryGetComp<CompHideFromPlayer>();
					if (comp != null) {
						comp.updateVisibility(true);
					}
				}
			}
		}
	}
}
