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
using RimWorldRealFoW.SectionLayers;
using RimWorldRealFoW.ShadowCasters;
using RimWorldRealFoW.ThingComps;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	class MapComponentSeenFog : MapComponent {
		public int[] factionsShownCells = null;
		public bool[] knownCells = null;

		private int maxFactionLoadId;

		private int mapCellLength;
		private int mapSizeX;
		private int mapSizeZ;
		private FogGrid fogGrid;
		private DesignationManager designationManager;
		private MapDrawer mapDrawer;

		private ThingGrid thingGrid;

		private bool initialized = false;

		public MapComponentSeenFog(Map map) : base(map) {
			mapCellLength = map.cellIndices.NumGridCells;
			mapSizeX = map.Size.x;
			mapSizeZ = map.Size.z;

			fogGrid = map.fogGrid;
			thingGrid = map.thingGrid;
			mapDrawer = map.mapDrawer;

			designationManager = this.map.designationManager;

			maxFactionLoadId = Find.World.factionManager.AllFactionsListForReading.Count;

			factionsShownCells = new int[(mapCellLength * (maxFactionLoadId + 1)) - 1];
			knownCells = new bool[mapCellLength];
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

		public int resolveIdx(Faction faction, int idxCell) {
			if (maxFactionLoadId < faction.loadID) {
				maxFactionLoadId = faction.loadID;
				int[] newFactionShownCells = new int[(mapCellLength * (maxFactionLoadId + 1)) - 1];
				Array.Copy(factionsShownCells, newFactionShownCells, factionsShownCells.Length);
				factionsShownCells = newFactionShownCells;
			}

			return (faction.loadID * mapCellLength) + idxCell;
		}

		public bool isShown(Faction faction, IntVec3 cell) {
			return factionsShownCells[resolveIdx(faction, (cell.z * mapSizeX) + cell.x)] != 0;
		}
		
		public bool isShown(Faction faction, int x, int z) {
			return factionsShownCells[resolveIdx(faction, (z * mapSizeX) + x)] != 0;
		}

		private void init() {
			// Reveal the starting position if home map and no pawns (landing).
			if (map.IsPlayerHome && map.mapPawns.ColonistsSpawnedCount == 0) {
				IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;
				ShadowCaster shadowCaster = new ShadowCaster();
				shadowCaster.computeFieldOfViewWithShadowCasting(playerStartSpot.x, playerStartSpot.z, Mathf.RoundToInt(CompFieldOfViewWatcher.NON_MECH_DEFAULT_RANGE),
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
						if (!knownCells[map.cellIndices.CellToIndex(x, y)]) {
							IntVec3 cell = new IntVec3(x, 0, y);
							knownCells[map.cellIndices.CellToIndex(x, y)] = true;

							foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
								CompHideFromPlayer comp = t.TryGetComp<CompHideFromPlayer>();
								if (comp != null) {
									comp.forceSeen();
								}
							}
						}
					});
			}

			// Update all thing FoV and visibility.
			foreach (Thing thing in map.listerThings.AllThings) {
				CompFieldOfViewWatcher compFoV = thing.TryGetComp<CompFieldOfViewWatcher>();
				CompHideFromPlayer compVisibility = thing.TryGetComp<CompHideFromPlayer>();
				if (compFoV != null) {
					compFoV.updateFoV();
				}
				if (compVisibility != null) {
					compVisibility.updateVisibility(true);
				}
			}

			// Redraw everything.
			mapDrawer.RegenerateEverythingNow();
		}

		public override void ExposeData() {
			base.ExposeData();

			ArrayExposeUtility.ExposeBoolArray(ref knownCells, map.Size.x, map.Size.z, "revealedCells");
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
				CompFieldOfViewWatcher comp = thing.TryGetComp<CompFieldOfViewWatcher>();
				if (comp != null) {
					comp.updateFoV();
				}
			}
		}

		public void incrementSeen(Faction faction, int idx) {
			if ((++factionsShownCells[resolveIdx(faction, idx)] == 1) && faction.IsPlayer) {
				IntVec3 cell = CellIndicesUtility.IndexToCell(idx, mapSizeX, mapSizeZ);

				if (!knownCells[idx]) {
					knownCells[idx] = true;
				}

				if (fogGrid.IsFogged(idx)) {
					fogGrid.Unfog(cell);
				}

				Designation designation = designationManager.DesignationAt(cell, DesignationDefOf.Mine);
				if (designation != null && MineUtility.MineableInCell(cell, this.map) == null) {
					designation.Delete();
				}

				if (initialized) {
					mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				List<Thing> things = thingGrid.ThingsListAtFast(idx);
				CompHideFromPlayer comp;
				int thingsCount = things.Count;
				for (int i = 0; i < thingsCount; i++) {
					comp = things[i].TryGetComp<CompHideFromPlayer>();
					if (comp != null) {
						comp.updateVisibility(true);
					}
				}
			}
		}

		public void decrementSeen(Faction faction, int idx) {
			if ((--factionsShownCells[resolveIdx(faction, idx)] == 0) && faction.IsPlayer) {
				IntVec3 cell = CellIndicesUtility.IndexToCell(idx, mapSizeX, mapSizeZ);

				if (initialized) {
					mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}
				
				List<Thing> things = thingGrid.ThingsListAtFast(idx);
				CompHideFromPlayer comp;
				int thingsCount = things.Count;
				for (int i = 0; i < thingsCount; i++) {
					comp = things[i].TryGetComp<CompHideFromPlayer>();
					if (comp != null) {
						comp.updateVisibility(true);
					}
				}
			}
		}
	}
}
