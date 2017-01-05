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
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	class MapComponentSeenFog : MapComponent {
		private int[][] factionShownCells = null;
		public bool[] revealedCells = null;

		bool initialized = false;

		public MapComponentSeenFog(Map map) : base(map) {
			factionShownCells = new int[Mathf.CeilToInt(Find.World.factionManager.AllFactionsListForReading.Count * 1.2f)][];
			revealedCells = new bool[map.cellIndices.NumGridCells];
		}

		public override void MapComponentUpdate() {
			base.MapComponentUpdate();

			if (!initialized) {
				initialized = true;
				init();
			}
		}

		public int[] getShownCells(Faction faction) {
			if (factionShownCells.Length < faction.loadID) {
				int[][] newFactionShownCells = new int[Mathf.CeilToInt(faction.loadID * 1.2f)][];
				Array.Copy(factionShownCells, newFactionShownCells, factionShownCells.Length);
				factionShownCells = newFactionShownCells;
			}

			int[] shownCells = factionShownCells[faction.loadID];
			if (shownCells == null) {
				shownCells = new int[map.cellIndices.NumGridCells];
				factionShownCells[faction.loadID] = shownCells;
			}
			return shownCells;
		}

		private void init() {
			// First tick: hide all unseen objects.
			foreach (IntVec3 cell in map.AllCells) {
				foreach (Thing t in map.thingGrid.ThingsAt(cell)) {
					if ((t.Faction == null || !t.Faction.IsPlayer) && getShownCells(Faction.OfPlayer)[map.cellIndices.CellToIndex(cell)] == 0) {
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
				ShadowCaster shadowCaster = new ShadowCaster();
				shadowCaster.computeFieldOfViewWithShadowCasting(playerStartSpot.x, playerStartSpot.z, Mathf.RoundToInt(CompFieldOfView.MAX_RANGE),
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
			int idx = map.cellIndices.CellToIndex(cell);
			if ((++getShownCells(faction)[idx] == 1) && faction.IsPlayer) {
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

		internal void decrementSeen(Faction faction, IntVec3 cell) {
			int idx = map.cellIndices.CellToIndex(cell);

			if ((--getShownCells(faction)[idx] == 0) && faction.IsPlayer) {
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
