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
using Verse;

namespace RimWorldRealFoW {
   class MapComponentSeenFog : MapComponent {
      public uint[] shownCells = null;

      bool tickReceived;

      public MapComponentSeenFog(Map map) : base(map) {
         shownCells = new uint[map.cellIndices.NumGridCells];
         tickReceived = false;
      }

      public override void MapComponentTick() {
         base.MapComponentTick();

         if (!tickReceived) {
            tickReceived = true;
         }
         // TODO: Handle rooms;
      }
      
      public void refogAll() {
         FogGrid fogGrid = map.fogGrid;
         for (int i = 0; i < fogGrid.fogGrid.Length; i++) {
            fogGrid.fogGrid[i] = true;
         }
         foreach (IntVec3 current in map.AllCells) {
            map.mapDrawer.MapMeshDirty(current, MapMeshFlag.FogOfWar);
         }
         _FloodFillerFog.FloodUnfog(map.mapPawns.FreeColonistsSpawned.RandomElement<Pawn>().Position, map);
      }

      public void incrementSeen(IntVec3 cell) {
         int idx = map.cellIndices.CellToIndex(cell);
         if (++shownCells[idx] == 1u) {
            if (tickReceived) {
               map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.FogOfWar);
               if (map.fogGrid.IsFogged(idx)) {
                  map.fogGrid.Unfog(cell);
               }
            } else {
               map.fogGrid.fogGrid[idx] = false;
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
            if (tickReceived) {
               map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.FogOfWar);
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
