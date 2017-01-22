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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.ThingComps {
	class CompFieldOfViewWatcher : ThingComp {
		public static readonly float NON_MECH_DEFAULT_RANGE = 32f;
		public static readonly float MECH_DEFAULT_RANGE = 40f;

		private bool calculated;
		private IntVec3 lastPosition;
		private int lastSightRange;
		private bool lastIsPeeking;
		private Faction lastFaction;

		private float baseViewRange;
		
		private bool[] viewMap;
		private CellRect viewRect;
		private bool[] newViewMap;
		private CellRect newViewRect;

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

		public ShadowCaster shadowCaster;

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

			shadowCaster = new ShadowCaster();

			calculated = false;
			lastPosition = IntVec3.Invalid;
			lastSightRange = 0;
			lastIsPeeking = false;

			viewMap = new bool[0];
			viewRect = new CellRect(-1, -1, 0, 0);

			newViewMap = new bool[0];
			newViewRect = new CellRect(-1, -1, 0, 0);

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
			mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();
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

			// Update at every position change, every 30 ticks from last position change.
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

				// Took ~1030ms on a Core i5 2500 in a worst case scenario (2 open areas): ~0.25ms per computation.
			}
		}

		private void initMap() {
			if (map != parent.Map) {
				if (map != null) {
					unseeSeenCells();
				}
				map = parent.Map;
				mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();
				thingGrid = map.thingGrid;
				cellIndices = map.cellIndices;
			}

			if (mapCompSeenFog == null) {
				// Try to retrieve.
				mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();

				// If still null, initialize it (old save).
				if (mapCompSeenFog == null) {
					mapCompSeenFog = new MapComponentSeenFog(map);
					map.components.Add(mapCompSeenFog);
					mapCompSeenFog.refogAll();
				}
			}
		}

		public void updateFoV(bool forceUpdate = false) {
			if (disabled || !setupDone || Current.ProgramState == ProgramState.MapInitializing) {
				return;
			}

			Thing thing = base.parent;

			if (thing != null && thing.Spawned && thing.Map != null && thing.Position != IntVec3.Invalid) {
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
							sightRange = calcPawnSightRange(pawn.Position, false, false);

							if (pawn.CurJob != null) {
								JobDef jobDef = pawn.CurJob.def;
								if ((pawnPather == null || !pawnPather.MovingNow) && (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.WaitCombat || jobDef == JobDefOf.Hunt)) {
									isPeeking = true;
								}
							}
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || pawn.Position != lastPosition || sightRange != lastSightRange || isPeeking != lastIsPeeking) {
							calculated = true;
							lastPosition = pawn.Position;
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

						if (!calculated || forceUpdate || thing.Faction != lastFaction || thing.Position != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = thing.Position;
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

						if (!calculated || forceUpdate || thing.Faction != lastFaction || thing.Position != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = thing.Position;
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

						if (!calculated || forceUpdate || thing.Faction != lastFaction || thing.Position != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = thing.Position;
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
					bool canLookForTarget = false;
					if (pawn.CurJob != null) {
						JobDef jobDef = pawn.CurJob.def;
						if (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.WaitCombat || jobDef == JobDefOf.Hunt) {
							canLookForTarget = true;
						}
					}

					if (canLookForTarget) {
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
			List<Thing> things = thingGrid.ThingsListAtFast(cellIndices.CellToIndex(position));
			CompAffectVision comp;
			int thingsCount = things.Count;
			for (int i = 0; i < thingsCount; i++) {
				comp = things[i].TryGetComp<CompAffectVision>();
				if (comp != null) {
					sightRange *= comp.Props.fovMultiplier;
				}
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

				if (!position.Roofed(map)) {
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

			// Local references (C# is slow to access fields...).
			Map map = this.map;
			int mapSizeX = map.Size.x;

			bool[] viewMap = this.viewMap;
			bool[] newViewMap = this.newViewMap;
			IntVec3[] viewPositions = this.viewPositions;

			EdificeGrid edificeGrid = map.edificeGrid;
			CellIndices cellIndices = map.cellIndices;

			IntVec3 position = thing.Position;

			Faction faction = parent.Faction;

			int peekMod = (peek ? 1 : 0);

			// Calculate new view rect.
			CellRect occupedRect = thing.OccupiedRect();
			newViewRect.maxX = Math.Max(position.x + intRadius + peekMod, occupedRect.maxX);
			newViewRect.minX = Math.Min(position.x - intRadius - peekMod, occupedRect.minX);
			newViewRect.maxZ = Math.Max(position.z + intRadius + peekMod, occupedRect.maxZ);
			newViewRect.minZ = Math.Min(position.z - intRadius - peekMod, occupedRect.minZ);

			var newViewRectMinX = newViewRect.minX;
			var newViewRectMaxX = newViewRect.maxX;
			var newViewRectMinZ = newViewRect.minZ;
			var newViewRectMaxZ = newViewRect.maxZ;

			int newViewWidth = newViewRect.Width;
			int newViewArea = newViewRect.Area;

			// Clear or reset the new view map.
			if (newViewMap.Length < newViewArea) {
				newViewMap = new bool[newViewArea];
				this.newViewMap = newViewMap;
			} else {
				for (int i = 0; i < newViewArea; i++) {
					newViewMap[i] = false;
				}
			}

			var viewRectMinZ = viewRect.minZ;
			var viewRectMaxZ = viewRect.maxZ;
			var viewRectMinX = viewRect.minX;
			var viewRectMaxX = viewRect.maxX;
			
			int viewWidth = viewRect.Width;
			int viewArea = viewRect.Area;

			// Occupied cells are always visible.
			for (int x = occupedRect.minX; x <= occupedRect.maxX; x++) {
				for (int z = occupedRect.minZ; z <= occupedRect.maxZ; z++) {
					newViewMap[CellIndicesUtility.CellToIndex(x - newViewRectMinX, z - newViewRectMinZ, newViewWidth)] = true;
					// Mark as seen only new cells.
					if (x < viewRectMinX || z < viewRectMinZ || x > viewRectMaxX || z > viewRectMaxZ || viewMap.Length == 0 ||
							!viewMap[CellIndicesUtility.CellToIndex(x - viewRectMinX, z - viewRectMinZ, viewWidth)]) {
						mapCompSeenFog.incrementSeen(faction, CellIndicesUtility.CellToIndex(x, z, mapSizeX));
					}
				}
			}

			// Calculate Field of View (if necessary).
			if (intRadius > 0) {
				int viewPositionsCount;
				viewPositions[0] = thing.Position;

				if (!peek) {
					viewPositionsCount = 1;
				} else {
					viewPositionsCount = 5;
					for (int i = 0; i < 4; i++) {
						viewPositions[1 + i] = thing.Position + GenAdj.CardinalDirections[i];
					}
				}

				int mapWitdh = map.Size.x - 1;
				int mapHeight = map.Size.z - 1;
				Building b;
				for (int i = 0; i < viewPositionsCount; i++) {
					IntVec3 viewPosition = viewPositions[i];
					if (viewPosition.IsInside(thing) || viewPosition.CanBeSeenOver(map)) {
						if (intRadius != 0) {
							shadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								// isOpaque
								delegate (int x, int z) {
									// Out of map positions are opaques...
									if (x <= 0 || z <= 0 || x >= mapWitdh || z >= mapHeight) {
										return true;
									}
									b = edificeGrid[cellIndices.CellToIndex(x, z)];
									return (b != null && !b.CanBeSeenOver());
								},
								// setFoV
								delegate (int x, int z) {
									int newIdx = CellIndicesUtility.CellToIndex(x - newViewRectMinX, z - newViewRectMinZ, newViewWidth);
									if (!newViewMap[newIdx]) {
										newViewMap[newIdx] = true;
										// Mark as seen only new cells.
										if (x < viewRectMinX || z < viewRectMinZ || x > viewRectMaxX || z > viewRectMaxZ || viewMap.Length == 0 ||
												!viewMap[CellIndicesUtility.CellToIndex(x - viewRectMinX, z - viewRectMinZ, viewWidth)]) {
											mapCompSeenFog.incrementSeen(faction, CellIndicesUtility.CellToIndex(x, z, mapSizeX));
										}
									}
								});
						}
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
								!newViewMap[CellIndicesUtility.CellToIndex(x - newViewRectMinX, z - newViewRectMinZ, newViewWidth)]) {
							mapCompSeenFog.decrementSeen(faction, CellIndicesUtility.CellToIndex(x, z, mapSizeX));
						}
					}
				}
			}

			// Update the view area.
			if (viewMap.Length < newViewArea) {
				viewMap = new bool[newViewArea];
				this.viewMap = viewMap;
			}
			Buffer.BlockCopy(newViewMap, 0, viewMap, 0, sizeof(byte) * newViewArea);

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

			if (viewRect.maxX >= 0 && viewRect.minX >= 0 && viewRect.maxZ >= 0 && viewRect.minZ >= 0 && viewMap.Length > 0) {
				var mapX = map.Size.x;

				int viewWidth = viewRect.Width;
				int viewArea = viewRect.Area;
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						mapCompSeenFog.decrementSeen(faction, CellIndicesUtility.CellToIndex((i % viewWidth) + viewRect.minX, (i / viewWidth) + viewRect.minZ, mapX));
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
