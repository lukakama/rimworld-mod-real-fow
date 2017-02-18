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
using RimWorldRealFoW.ShadowCasters;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.ThingComps {
	class CompFieldOfViewWatcher : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompFieldOfViewWatcher));

		public static readonly float NON_MECH_DEFAULT_RANGE = 32f;
		public static readonly float MECH_DEFAULT_RANGE = 40f;

		private bool calculated;
		private IntVec3 lastPosition;
		private int lastSightRange;
		private bool lastIsPeeking;
		private Faction lastFaction;

		private float baseViewRange;
		
		private bool[] viewMap1;
		private bool[] viewMap2;

		private CellRect viewRect;

		private bool viewMapSwitch = false;

		private IntVec3[] viewPositions;

		private Map map;
		private MapComponentSeenFog mapCompSeenFog;
		private ThingGrid thingGrid;
		private CellIndices cellIndices;

		private CompHiddenable compHiddenable;
		private CompGlower compGlower;
		private CompPowerTrader compPowerTrader;
		private CompRefuelable compRefuelable;
		private CompFlickable compFlickable;
		private CompMannable compMannable;
		private CompProvideVision compProvideVision;

		private bool setupDone = false;

		private Pawn pawn;
		private Building building;
		private Building_TurretGun turret;

		private RaceProperties raceProps;
		private Pawn_PathFollower pawnPather;

		private int lastMovementTick;

		private int lastPositionUpdateTick;

		private bool disabled;

		public int sightRange {
			get {
				return lastSightRange;
			}
		}
		
		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			setupDone = true;

			calculated = false;
			lastPosition = IntVec3.Invalid;
			lastSightRange = 0;
			lastIsPeeking = false;

			viewMap1 = new bool[0];
			viewMap2 = new bool[0];

			viewRect = new CellRect(-1, -1, 0, 0);

			viewPositions = new IntVec3[5];
			
			compHiddenable = parent.GetComp<CompHiddenable>();
			compGlower = parent.GetComp<CompGlower>();
			compPowerTrader = parent.GetComp<CompPowerTrader>();
			compRefuelable = parent.GetComp<CompRefuelable>();
			compFlickable = parent.GetComp<CompFlickable>();
			compMannable = parent.GetComp<CompMannable>();
			compProvideVision = parent.GetComp<CompProvideVision>();

			pawn = parent as Pawn;
			building = parent as Building;
			turret = parent as Building_TurretGun;

			if (pawn != null) {
				pawnPather = pawn.pather;
				raceProps = pawn.RaceProps;
			}

			map = parent.Map;
			mapCompSeenFog = map.getMapComponentSeenFog();
			thingGrid = map.thingGrid;
			cellIndices = map.cellIndices;

			if (parent.def.race == null || !parent.def.race.IsMechanoid) {
				baseViewRange = NON_MECH_DEFAULT_RANGE;
			} else {
				baseViewRange = MECH_DEFAULT_RANGE;
			}

			disabled = false;

			lastMovementTick = Find.TickManager.TicksGame;
			lastPositionUpdateTick = lastMovementTick;
			updateFoV();
		}

		public override void PostExposeData() {
			base.PostExposeData();

			Scribe_Values.LookValue<int>(ref this.lastMovementTick, "fovLastMovementTick", Find.TickManager.TicksGame, false);
			Scribe_Values.LookValue<bool>(ref this.disabled, "fovDisabled", false, false);
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			updateFoV();
		}

		private bool doBenchmark = false;
		private bool benchmarkPerformed = false;

		public override void CompTick() {
			base.CompTick();

			int currentTick = Find.TickManager.TicksGame;

			if (parent != null && parent.Spawned && pawn != null && pawnPather != null && pawnPather.MovingNow) {
				lastMovementTick = currentTick;
			}

			if (doBenchmark && !benchmarkPerformed) {
				benchmarkPerformed = true;
				benchmark();
			}

			// Update at every position change and then after every 1/2 second from last position change.
			if (lastPosition != IntVec3.Invalid && lastPosition != parent.Position) {
				lastPositionUpdateTick = currentTick;
				updateFoV();
			} else if ((currentTick - lastPositionUpdateTick) % 30 == 0) {
				updateFoV();
			}
		}

		private void benchmark() {
			if (!disabled && setupDone && Current.ProgramState != ProgramState.MapInitializing && pawn != null && pawn.Faction != null && pawn.Faction.IsPlayer) {
				IntVec3 currPos = pawn.Position;
				IntVec3 simulatedPos = new IntVec3((int) (currPos.x - (NON_MECH_DEFAULT_RANGE * 2)), 0, currPos.z);

				Stopwatch sw = new Stopwatch();

				sw.Start();

				// Stress test: simulate 4000 deaths and 4000 spawns.
				// IMPORTANT: Adjust simulatedPos to benchmark map free area and use only on worlds with one colonist.
				for (int i = 0; i < 4000; i++) {
					if (pawn.Position == simulatedPos) {
						pawn.SetPositionDirect(currPos);
					} else {
						pawn.SetPositionDirect(simulatedPos);
					}
					updateFoV(true);
				}

				sw.Stop();

				Log.Message("Benchmark: " + sw.ElapsedMilliseconds);

				// Worst case scenario using a local reference map with 2 open areas and long view range, computed 4000 times each with vision cleaning 
				// on each computation (usualy only perimeter updated).
				// On a Core i5 2500:
				//  - ~900ms for a colonist: ~0.23ms per raw field of view computation, so 1 FPS drop each ~72 concurrently updating pawns (for 60 FPS a frame needs to be rendered in ~16.67ms).
				//  - ~800ms for a non colonist: ~0.20ms per raw field of view computation, so 1 FPS drop each ~80 concurrently updating pawns (for 60 FPS a frame needs to be rendered in ~16.67ms).
			}
		}

		private void initMap() {
			if (map != parent.Map) {
				if (map != null) {
					unseeSeenCells();
				}
				map = parent.Map;
				mapCompSeenFog = map.getMapComponentSeenFog();
				thingGrid = map.thingGrid;
				cellIndices = map.cellIndices;
			}
		}

		public void updateFoV(bool forceUpdate = false) {
			if (disabled || !setupDone || Current.ProgramState == ProgramState.MapInitializing) {
				return;
			}

			Thing thing = base.parent;
			IntVec3 newPosition = thing.Position;
			if (thing != null && thing.Spawned && thing.Map != null && newPosition != IntVec3.Invalid) {
				initMap();

				if (thing.Faction != null && (pawn == null || !pawn.Dead)) {
					// Faction things or alive pawn!

					if (pawn != null) {
						// Alive Pawns!

						int sightRange;
						bool isPeeking = false;
						if (raceProps != null && raceProps.Animal && (pawn.playerSettings == null || pawn.playerSettings.master == null || pawn.training == null || !pawn.training.IsCompleted(TrainableDefOf.Release))) {
							// If animal, only those with a master set and release training can contribute to the faction FoW.
							sightRange = -1;
						} else {
							sightRange = calcPawnSightRange(newPosition, false, false);

							if (pawn.CurJob != null) {
								JobDef jobDef = pawn.CurJob.def;
								if ((pawnPather == null || !pawnPather.MovingNow) && (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.WaitCombat || jobDef == JobDefOf.Hunt)) {
									isPeeking = true;
								}
							}
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange || isPeeking != lastIsPeeking) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;
							lastIsPeeking = isPeeking;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
							}
							lastFaction = thing.Faction;

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, isPeeking);
							} else {
								unseeSeenCells();
							}
						}

					/* Replaced by surveillance cameras
					} else if (compGlower != null) {
						// Glowers!

						int sightRange = Mathf.RoundToInt(compGlower.Props.glowRadius);

						if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = -1;
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
							}
							lastFaction = thing.Faction;

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells();
							}
						}
					*/
					} else if (turret != null && compMannable == null) {
						// Automatic turrets!

						int sightRange = Mathf.RoundToInt(turret.GunCompEq.PrimaryVerb.verbProps.range);

						
						if (Find.Storyteller.difficulty.difficulty >= 4 || // Intense and Extreme difficulties disable FoV from turrets.
									(compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = -1;
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
							}
							lastFaction = thing.Faction;

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells();
							}
						}

					} else if (compProvideVision != null) {
						// Vision providers!

						// TODO: Calculate range applying dark and weather debufs. 
						int sightRange = Mathf.RoundToInt(compProvideVision.Props.viewRadius);

						if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = -1;
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
							}
							lastFaction = thing.Faction;

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells();
							}
						}

					} else {
						// Disable the component (this thing doesn't need the FoV calculation).
						disabled = true;
					}
				} else if (thing.Faction != lastFaction) {
					// Faction change (from a faction to nothing). Unseen and clear old seen cells
					if (lastFaction != null) {
						unseeSeenCells(lastFaction);
					}
					lastFaction = thing.Faction;
				}
			}
		}

		public int calcPawnSightRange(IntVec3 position, bool forTargeting, bool shouldMove) {
			if (pawn == null) {
				Log.Error("calcPawnSightRange performed on non pawn thing");
				return 0;
			}

			float sightRange = 0f;

			initMap();

			bool sleeping = (parent.def.race == null || !pawn.def.race.IsMechanoid) && pawn.CurJob != null && pawn.jobs.curDriver.asleep;

			if (!shouldMove && !sleeping && (pawnPather == null || !pawnPather.MovingNow)) {
				Verb verb = pawn.TryGetAttackVerb(true);
				if (verb != null && verb.verbProps.range > baseViewRange && verb.verbProps.requireLineOfSight && verb.ownerEquipment.def.IsRangedWeapon) {
					if (pawn.CurJob != null) {
						JobDef jobDef = pawn.CurJob.def;
						if (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.WaitCombat || jobDef == JobDefOf.Hunt) {
							float weaponRange = verb.verbProps.range;
							if (baseViewRange < weaponRange) {
								int ticksStanding = Find.TickManager.TicksGame - lastMovementTick;

								float statValue = pawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
								int ticksToSearch = (verb.verbProps.warmupTime * statValue).SecondsToTicks() * Mathf.RoundToInt((weaponRange - baseViewRange) / 2);

								if (ticksStanding >= ticksToSearch) {
									sightRange = verb.verbProps.range * pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Sight);
								} else {
									int incValue = Mathf.RoundToInt((verb.verbProps.range - baseViewRange) * ((float) ticksStanding / ticksToSearch));

									sightRange = (baseViewRange + incValue) * pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Sight);
								}
							}
						}
					}
				}
			}

			if (sightRange == 0f) {
				sightRange = baseViewRange * pawn.health.capacities.GetEfficiency(PawnCapacityDefOf.Sight);
			}

			if (!forTargeting && sleeping) {
				// Sleeping: sight reduced to 20% (if not for targeting).
				sightRange *= 0.2f;
			}
			// TODO: Apply moving penality?
			/*else if (!calcOnlyBase && pawnPather.MovingNow) {
				// When moving, sight reduced to 90%s.
				sightRange *= 0.9f;
			}
			*/

			// Check if standing on an affect view object.
			List<CompAffectVision> compsAffectVision = mapCompSeenFog.compAffectVisionGrid[cellIndices.CellToIndex(position)];
			int compsCount = compsAffectVision.Count;
			for (int i = 0; i < compsCount; i++) {
				sightRange *= compsAffectVision[i].Props.fovMultiplier;
			}

			// Additional dark and weather debuff.
			if (pawn.def.race == null || !pawn.def.race.IsMechanoid) {
				float currGlow = map.glowGrid.GameGlowAt(position);
				if (currGlow != 1f) {
					int darkModifier = 60;
					// Each bionic eye reduce the dark debuff by 20.
					foreach (Hediff hediff in pawn.health.hediffSet.GetHediffs<Hediff_AddedPart>()) {
						if (hediff.def == HediffDefOf.BionicEye) {
							darkModifier += 20;
						}
					}

					// Apply only if to debuff.
					if (darkModifier < 100) {
						// Adjusted to glow (100% full light - 60% dark).
						sightRange *= Mathf.Lerp((darkModifier / 100f), 1f, currGlow);
					}
				}

				if (!map.roofGrid.Roofed(position.x, position.z)) {
					float weatherFactor = map.weatherManager.CurWeatherAccuracyMultiplier;
					if (weatherFactor != 1f) {
						// Weather factor is applied by half.
						sightRange *= Mathf.Lerp(0.5f, 1f, weatherFactor);
					}
				}
			}

			// Mininum sight.
			if (sightRange < 1f) {
				return 1;
			}

			return Mathf.RoundToInt(sightRange);
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			unseeSeenCells();
		}

		public void calculateFoV(Thing thing, int intRadius, bool peek) {
			//if (!(thing is Pawn)) {
			//	Log.Message("calculateFoV: " + thing.ThingID);
			//}

			int mapSizeX = map.Size.x;
			int mapSizeZ = map.Size.z;

			bool[] viewMap = viewMapSwitch ? this.viewMap1 : this.viewMap2;
			bool[] newViewMap = viewMapSwitch ? this.viewMap2 : this.viewMap1;

			IntVec3 position = thing.Position;
			Faction faction = parent.Faction;

			int peekMod = (peek ? 1 : 0);

			// Calculate new view rect.
			CellRect occupedRect = thing.OccupiedRect();
			int newViewRectMinX = Math.Min(position.x - intRadius - peekMod, occupedRect.minX);
			int newViewRectMaxX = Math.Max(position.x + intRadius + peekMod, occupedRect.maxX);
			int newViewRectMinZ = Math.Min(position.z - intRadius - peekMod, occupedRect.minZ);
			int newViewRectMaxZ = Math.Max(position.z + intRadius + peekMod, occupedRect.maxZ);

			int newViewWidth = newViewRectMaxX - newViewRectMinX + 1;
			int newViewArea = newViewWidth * (newViewRectMaxZ - newViewRectMinZ + 1);

			// Clear or reset the new view map.
			if (newViewMap.Length < newViewArea) {
				newViewMap = new bool[newViewArea];
				if (viewMapSwitch) {
					this.viewMap2 = newViewMap;
				} else {
					this.viewMap1 = newViewMap;
				}
			} else {
				Array.Clear(newViewMap, 0, newViewArea);
			}

			int viewRectMinZ = viewRect.minZ;
			int viewRectMaxZ = viewRect.maxZ;
			int viewRectMinX = viewRect.minX;
			int viewRectMaxX = viewRect.maxX;

			int viewWidth = viewRect.Width;
			int viewArea = viewRect.Area;

			// Occupied cells are always visible.
			for (int x = occupedRect.minX; x <= occupedRect.maxX; x++) {
				for (int z = occupedRect.minZ; z <= occupedRect.maxZ; z++) {
					newViewMap[((z - newViewRectMinZ) * newViewWidth) + (x - newViewRectMinX)] = true;
				}
			}

			// Calculate Field of View (if necessary).
			if (intRadius > 0) {

				bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;

				int viewPositionsCount;
				viewPositions[0] = position;

				if (!peek) {
					viewPositionsCount = 1;
				} else {
					viewPositionsCount = 5;
					for (int i = 0; i < 4; i++) {
						viewPositions[1 + i] = position + GenAdj.CardinalDirections[i];
					}
				}
				int mapWitdh = map.Size.x - 1;
				int mapHeight = map.Size.z - 1;

				IntVec3 viewPosition;
				for (int i = 0; i < viewPositionsCount; i++) {
					viewPosition = viewPositions[i];
					if (viewPosition.x >= 0 && viewPosition.z >= 0 && viewPosition.x <= mapWitdh && viewPosition.z <= mapHeight &&
								(i == 0 || viewPosition.IsInside(thing) || !viewBlockerCells[(viewPosition.z * mapSizeX)  + viewPosition.x])) {
						ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
							viewBlockerCells, mapSizeX, mapSizeZ, newViewMap, newViewRectMinX, newViewRectMinZ, newViewWidth);
					}
				}
			}

			// Check seen cells.
			int newX;
			int newZ;
			for (int i = 0; i < newViewArea; i++) {
				if (newViewMap[i]) {
					newX = newViewRectMinX + (i % newViewWidth);
					newZ = newViewRectMinZ + (i / newViewWidth);
					// Mark as seen only new cells.
					if (newX < viewRectMinX || newZ < viewRectMinZ || newX > viewRectMaxX || newZ > viewRectMaxZ || viewMap.Length == 0 ||
							!viewMap[((newZ - viewRectMinZ) *  viewWidth) + (newX - viewRectMinX)]) {
						mapCompSeenFog.incrementSeen(faction, (newZ * mapSizeX) + newX);
					}
				}
			}

			// Mark as unseen old cells not present anymore in the updated FoV.
			if (viewMap.Length > 0) {
				int x;
				int z;
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						x = viewRectMinX + (i % viewWidth);
						z = viewRectMinZ + (i / viewWidth);
						if (x < newViewRectMinX || z < newViewRectMinZ || x > newViewRectMaxX || z > newViewRectMaxZ ||
								!newViewMap[((z - newViewRectMinZ) * newViewWidth) + (x - newViewRectMinX)]) {
							mapCompSeenFog.decrementSeen(faction, (z * mapSizeX) + x);
						}
					}
				}
			}

			// Use te new view area.
			viewMapSwitch = !viewMapSwitch;

			// Update the view rect.
			viewRect.maxX = newViewRectMaxX;
			viewRect.minX = newViewRectMinX;
			viewRect.maxZ = newViewRectMaxZ;
			viewRect.minZ = newViewRectMinZ;
		}

		private void unseeSeenCells(Faction faction = null) {
			if (faction == null) {
				faction = parent.Faction;
			}
			bool[] viewMap = viewMapSwitch ? this.viewMap1 : this.viewMap2;

			if (viewRect.maxX >= 0 && viewRect.minX >= 0 && viewRect.maxZ >= 0 && viewRect.minZ >= 0 && viewMap.Length > 0) {
				int mapX = map.Size.x;

				int viewWidth = viewRect.Width;
				int viewArea = viewRect.Area;
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						mapCompSeenFog.decrementSeen(faction, (((i / viewWidth) + viewRect.minZ) * mapX) + ((i % viewWidth) + viewRect.minX));
						viewMap[i] = false;
					}
				}

				// Clear the view rect.
				viewRect.maxX = -1;
				viewRect.minX = -1;
				viewRect.maxZ = -1;
				viewRect.minZ = -1;
			}
		}
	}
}
