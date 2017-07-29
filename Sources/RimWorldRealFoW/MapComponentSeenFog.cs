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
using RimWorldRealFoW.ThingComps.ThingSubComps;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	public class MapComponentSeenFog : MapComponent {
		public int[][] factionsShownCells = null;
		public bool[] knownCells = null;

		public bool[] viewBlockerCells = null;

		private IntVec3[] idxToCellCache;

		private List<CompHideFromPlayer>[] compHideFromPlayerGrid;
		public List<CompAffectVision>[] compAffectVisionGrid;

		private int maxFactionLoadId;

		private int mapCellLength;
		private int mapSizeX;
		private int mapSizeZ;
		private FogGrid fogGrid;
		private DesignationManager designationManager;
		private MapDrawer mapDrawer;

		private ThingGrid thingGrid;

		public bool initialized = false;
		
		public MapComponentSeenFog(Map map) : base(map) {
			mapCellLength = map.cellIndices.NumGridCells;
			mapSizeX = map.Size.x;
			mapSizeZ = map.Size.z;

			fogGrid = map.fogGrid;
			thingGrid = map.thingGrid;
			mapDrawer = map.mapDrawer;

			designationManager = this.map.designationManager;

			maxFactionLoadId = 0;
			foreach (Faction faction in Find.World.factionManager.AllFactionsListForReading) {
				maxFactionLoadId = Math.Max(maxFactionLoadId, faction.loadID);
			}
			factionsShownCells = new int[maxFactionLoadId + 1][];

			knownCells = new bool[mapCellLength];
			viewBlockerCells = new bool[mapCellLength];

			idxToCellCache = new IntVec3[mapCellLength];
			compHideFromPlayerGrid = new List<CompHideFromPlayer>[mapCellLength];
			compAffectVisionGrid = new List<CompAffectVision>[mapCellLength];
			for (int i = 0; i < mapCellLength; i++) {
				idxToCellCache[i] = CellIndicesUtility.IndexToCell(i, mapSizeX);

				compHideFromPlayerGrid[i] = new List<CompHideFromPlayer>(16);
				compAffectVisionGrid[i] = new List<CompAffectVision>(16);
			}
		}

		public override void MapComponentUpdate() {
			if (!initialized) {
				initialized = true;

				init();

				// Some mods (Allows Tools) inject designators at play time and not at mod initialization time.
				// So we need to patch them here.
				RealFoWModStarter.patchDesignators();
			}
		}
		
		public int[] getFactionShownCells(Faction faction) {
			if (faction == null) {
				return null;
			}

			if (maxFactionLoadId < faction.loadID) {
				// Increase the jagged array.
				maxFactionLoadId = faction.loadID + 1;
				int[][] newFactionShownCells = new int[maxFactionLoadId + 1][];

				// Copy old references.
				Array.Copy(factionsShownCells, newFactionShownCells, factionsShownCells.Length);

				factionsShownCells = newFactionShownCells;
			}

			// Lazy init faction shown grids (some mods could create dummy factions not used, causing a huge amount of memory waste).
			if (factionsShownCells[faction.loadID] == null) {
				factionsShownCells[faction.loadID] = new int[mapCellLength];
			}

			return factionsShownCells[faction.loadID];
		}

		public bool isShown(Faction faction, IntVec3 cell) {
			return isShown(faction, cell.x, cell.z);
		}
		
		public bool isShown(Faction faction, int x, int z) {
			return getFactionShownCells(faction)[(z * mapSizeX) + x] != 0;
		}

		public void registerCompHideFromPlayerPosition(CompHideFromPlayer comp, int x, int z) {
			if (x >= 0 && z >= 0 && x < mapSizeX && z < mapSizeZ) {
				compHideFromPlayerGrid[(z * mapSizeX) + x].Add(comp);
			}
		}
		public void deregisterCompHideFromPlayerPosition(CompHideFromPlayer comp, int x, int z) {
			if (x >= 0 && z >= 0 && x < mapSizeX && z < mapSizeZ) {
				compHideFromPlayerGrid[(z * mapSizeX) + x].Remove(comp);
			}
		}

		public void registerCompAffectVisionPosition(CompAffectVision comp, int x, int z) {
			if (x >= 0 && z >= 0 && x < mapSizeX  && z < mapSizeZ) {
				compAffectVisionGrid[(z * mapSizeX) + x].Add(comp);
			}
		}
		public void deregisterCompAffectVisionPosition(CompAffectVision comp, int x, int z) {
			if (x >= 0 && z >= 0 && x < mapSizeX && z < mapSizeZ) {
				compAffectVisionGrid[(z * mapSizeX) + x].Remove(comp);
			}
		}

		private void init() {
			// Reveal the starting position if home map and no pawns (landing).
			if (map.IsPlayerHome && map.mapPawns.ColonistsSpawnedCount == 0) {
				IntVec3 playerStartSpot = MapGenerator.PlayerStartSpot;
				ShadowCaster.computeFieldOfViewWithShadowCasting(playerStartSpot.x, playerStartSpot.z, Mathf.RoundToInt(CompFieldOfViewWatcher.NON_MECH_DEFAULT_RANGE),
					viewBlockerCells, map.Size.x, map.Size.z, 
					false, null, null, null, // Directly updating known cells. No need to call incrementSeen.
					knownCells, 0, 0, mapSizeX, 
					null, 0, 0, 0, 0, 0);

				for (int i = 0; i < mapCellLength; i++) {
					if (knownCells[i]) {
						IntVec3 cell = CellIndicesUtility.IndexToCell(i, mapSizeX);
						foreach (Thing t in map.thingGrid.ThingsListAtFast(cell)) {
							CompMainComponent compMain = (CompMainComponent) t.TryGetComp(CompMainComponent.COMP_DEF);
							if (compMain != null && compMain.compHideFromPlayer != null) {
								compMain.compHideFromPlayer.forceSeen();
							}
						}
					}
				}
			}

			// Update all thing FoV and visibility.
			foreach (Thing thing in map.listerThings.AllThings) {
				if (thing.Spawned) {
					CompMainComponent compMain = (CompMainComponent) thing.TryGetComp(CompMainComponent.COMP_DEF);
					if (compMain != null) {
						if (compMain.compComponentsPositionTracker != null) {
							compMain.compComponentsPositionTracker.updatePosition();
						}
						if (compMain.compFieldOfViewWatcher != null) {
							compMain.compFieldOfViewWatcher.updateFoV();
						}
						if (compMain.compHideFromPlayer != null) {
							compMain.compHideFromPlayer.updateVisibility(true);
						}
					}
				}
			}

			// Redraw everything.
			mapDrawer.RegenerateEverythingNow();
		}

		public override void ExposeData() {
			base.ExposeData();

			ArrayExposeUtility.ExposeBoolArray(ref knownCells, map.Size.x, map.Size.z, "revealedCells");
		}

		public void revealCell(int idx) {
			if (!knownCells[idx]) {
				IntVec3 cell = idxToCellCache[idx];

				knownCells[idx] = true;

				Designation designation = designationManager.DesignationAt(cell, DesignationDefOf.Mine);
				if (designation != null && MineUtility.MineableInCell(cell, map) == null) {
					designation.Delete();
				}

				if (initialized) {
					mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				List<CompHideFromPlayer> comps = compHideFromPlayerGrid[idx];
				int compCount = comps.Count;
				for (int i = 0; i < compCount; i++) {
					comps[i].updateVisibility(true);
				}
			}
		}

		public void incrementSeen(Faction faction, int[] factionShownCells, int idx) {
			if ((++factionShownCells[idx] == 1) && faction.IsPlayer) {
				IntVec3 cell = idxToCellCache[idx];

				knownCells[idx] = true;
				
				Designation designation = designationManager.DesignationAt(cell, DesignationDefOf.Mine);
				if (designation != null && MineUtility.MineableInCell(cell, map) == null) {
					designation.Delete();
				}

				if (initialized) {
					mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				List<CompHideFromPlayer> comps = compHideFromPlayerGrid[idx];
				int compCount = comps.Count;
				for (int i = 0; i < compCount; i++) {
					comps[i].updateVisibility(true);
				}
			}
		}

		public void decrementSeen(Faction faction, int[] factionShownCells, int idx) {
			if ((--factionShownCells[idx] == 0) && faction.IsPlayer) {
				IntVec3 cell = idxToCellCache[idx];

				if (initialized) {
					mapDrawer.MapMeshDirty(cell, SectionLayer_FoVLayer.mapMeshFlag, true, false);
				}

				List<CompHideFromPlayer> comps = compHideFromPlayerGrid[idx];
				int compCount = comps.Count;
				for (int i = 0; i < compCount; i++) {
					comps[i].updateVisibility(true);
				}
			}
		}
	}
}
