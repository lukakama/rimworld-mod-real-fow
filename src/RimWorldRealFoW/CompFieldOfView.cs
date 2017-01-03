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
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
   class CompFieldOfView : ThingComp {
      public static readonly float MAX_RANGE = 32f;

      private bool calculated;
      private IntVec3 lastPosition;
      private float lastSightRange;
      private bool lastIsPeeking;

      private List<IntVec3> seenCells;
      private List<IntVec3> newSeenCells;

      private Map map;
      private MapComponentSeenFog mapCompSeenFog;

      private CompHiddenable compHiddenable;
      private CompGlower compGlower;
      private CompPowerTrader compPowerTrader;
      private CompRefuelable compRefuelable;
      private CompFlickable compFlickable;
      private CompMannable mannableComp;

      private Pawn pawn;
      private Building_TurretGun turret;

      public override void PostSpawnSetup() {
         base.PostSpawnSetup();

         calculated = false;
         lastPosition = IntVec3.Invalid;
         lastSightRange = 0f;
         lastIsPeeking = false;

         seenCells = new List<IntVec3>(512);
         newSeenCells = new List<IntVec3>(512);

         compHiddenable = parent.GetComp<CompHiddenable>();
         compGlower = parent.GetComp<CompGlower>();
         compPowerTrader = parent.GetComp<CompPowerTrader>();
         compRefuelable = parent.GetComp<CompRefuelable>();
         compFlickable = parent.GetComp<CompFlickable>();
         mannableComp = parent.GetComp<CompMannable>();

         pawn = parent as Pawn;
         turret = parent as Building_TurretGun;

         updateFoV();
      }

      public override void ReceiveCompSignal(string signal) {
         base.ReceiveCompSignal(signal);

         updateFoV();
      }

      public override void CompTick() {
         base.CompTick();

         // Check every 25 thick.
         if (Find.TickManager.TicksGame % 25 == 0) {
            updateFoV();
         }
      }

      public void updateFoV() {
         if (Current.ProgramState == ProgramState.MapInitializing) {
            return;
         }

         Thing thing = base.parent;

         if (thing != null && thing.Spawned && thing.Map != null && thing.Position != IntVec3.Invalid) {
            if (map != thing.Map) {
               if (map != null) {
                  unseeSeenCells();
               }
               map = thing.Map;
               mapCompSeenFog = thing.Map.GetComponent<MapComponentSeenFog>();
            }

            if (mapCompSeenFog == null) {
               mapCompSeenFog = new MapComponentSeenFog(thing.Map);
               thing.Map.components.Add(mapCompSeenFog);
               mapCompSeenFog.refogAll();
            }

            if (thing.Faction != null && thing.Faction.IsPlayer && (pawn == null || !pawn.Dead)) {
               // Player things or alive pawn!

               if (pawn != null) {
                  // Alive Pawns!

                  float sightRange;
                  if (pawn.CurJob != null && pawn.jobs.curDriver.asleep) {
                     // Sleeping: sight reduced to 20%.
                     sightRange = Mathf.Max(MAX_RANGE * pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Sight) * 0.2f, 1f);
                  } else {
                     sightRange = Mathf.Max(MAX_RANGE * pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Sight), 1f);
                  }
                  bool isPeeking = false;
                  if (pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.AttackStatic || pawn.CurJob.def == JobDefOf.AttackMelee ||
                          pawn.CurJob.def == JobDefOf.Wait || pawn.CurJob.def == JobDefOf.WaitCombat)) {
                     isPeeking = true;
                  }
                  if (!calculated || pawn.Position != lastPosition || sightRange != lastSightRange || isPeeking != lastIsPeeking) {
                     calculated = true;
                     lastPosition = pawn.Position;
                     lastSightRange = sightRange;
                     lastIsPeeking = isPeeking;

                     calculateFoV(thing, sightRange, isPeeking);
                  }

               } else if (compGlower != null) {
                  // Glowers!

                  float sightRange = compGlower.Props.glowRadius;

                  if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
                          (compRefuelable != null && !compRefuelable.HasFuel) ||
                          (compFlickable != null && !compFlickable.SwitchIsOn)) {
                     sightRange = 0f;
                  }

                  if (!calculated || thing.Position != lastPosition || sightRange != lastSightRange) {
                     calculated = true;
                     lastPosition = thing.Position;
                     lastSightRange = sightRange;

                     calculateFoV(thing, sightRange, false);
                  }

               } else if (turret != null && mannableComp == null) {
                  // Automatic turrets!

                  float sightRange = turret.GunCompEq.PrimaryVerb.verbProps.range;

                  if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
                          (compRefuelable != null && !compRefuelable.HasFuel) ||
                          (compFlickable != null && !compFlickable.SwitchIsOn)) {
                     sightRange = 0f;
                  }

                  if (!calculated || thing.Position != lastPosition || sightRange != lastSightRange) {
                     calculated = true;
                     lastPosition = thing.Position;
                     lastSightRange = sightRange;

                     calculateFoV(thing, sightRange, false);
                  }

               } else {
                  // Others!

                  if (!calculated || thing.Position != lastPosition) {
                     calculated = true;
                     lastPosition = thing.Position;

                     calculateFoV(thing, 0f, false);
                  }
               }

            } else {

               // Non player thing! (this should be moved to another Component...)
               if (!calculated || thing.Position != lastPosition) {
                  calculated = true;
                  lastPosition = thing.Position;

                  if (mapCompSeenFog != null && compHiddenable != null && !map.fogGrid.IsFogged(thing.Position)) {
                     if (mapCompSeenFog.shownCells[map.cellIndices.CellToIndex(thing.Position)] == 0) {
                        compHiddenable.hide();
                     } else {
                        compHiddenable.show();
                     }
                  }
               }
            }
         }
      }

      public override void PostDeSpawn(Map map) {
         base.PostDeSpawn(map);

         unseeSeenCells();
      }

      public bool hasMechanoidInSeenCell(Map map) {
         foreach (IntVec3 c in seenCells) {
            List<Thing> thingList = c.GetThingList(map);
            for (int l = 0; l < thingList.Count; l++) {
               Pawn cPawn = thingList[l] as Pawn;
               if (cPawn != null) {
                  cPawn.mindState.Active = true;
                  if (cPawn.def.race.IsMechanoid) {
                     return true;
                  }
               }
            }
         }

         return false;
      }

      public void calculateFoV(Thing thing, float radius, bool peek) {
         /*if (!(thing is Pawn)) {
				Log.Message("calculateFoV: " + thing.ThingID);
			}*/

         newSeenCells.Clear();

         foreach (IntVec3 occupiedCell in thing.OccupiedRect().Cells) {
            if (occupiedCell.InBounds(map)) {
               newSeenCells.Add(occupiedCell);
               mapCompSeenFog.incrementSeen(occupiedCell);
            }
         }

         if (radius > 0) {
            IEnumerable<IntVec3> positions = new IntVec3[] { thing.Position };

            if (peek) {
               positions = positions.Concat(GenAdj.CellsAdjacentCardinal(thing));
            }

            foreach (IntVec3 position in positions) {
               if (position.IsInside(thing) || position.CanBeSeenOverFast(map)) {
                  if (radius != 0) {
                     ShadowCaster.ComputeFieldOfViewWithShadowCasting(position.x, position.z, Mathf.RoundToInt(radius) - (position.IsInside(thing) ? 0 : 1),
                        // isOpaque
                        (int x, int y) => {
                           // Out of map position are opaques...
                           if (x < 0 || y < 0 || x >= map.Size.x || y >= map.Size.z) {
                              return true;
                           }
                           Building b = map.edificeGrid[map.cellIndices.CellToIndex(x, y)];
                           return (b != null && !b.CanBeSeenOver());
                        },
                        // setFoV
                        (int x, int y) => {
                           if (x >= 0 && y >= 0 && x < map.Size.x && y < map.Size.z) {
                              IntVec3 cell = new IntVec3(x, 0, y);
                              newSeenCells.Add(cell);
                              mapCompSeenFog.incrementSeen(cell);
                           }
                        });
                  }
               }
            }
         }

         unseeSeenCells();

         seenCells.AddRange(newSeenCells);

         newSeenCells.Clear();
      }

      private void unseeSeenCells() {
         foreach (IntVec3 cell in seenCells) {
            mapCompSeenFog.decrementSeen(cell);
         }
         seenCells.Clear();
      }
   }
}
