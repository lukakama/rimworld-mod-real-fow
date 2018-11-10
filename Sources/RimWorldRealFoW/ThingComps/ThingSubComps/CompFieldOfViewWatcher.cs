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
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.ThingComps.ThingSubComps {
	public class CompFieldOfViewWatcher : ThingSubComp {
		private static readonly IntVec3 iv3Invalid = IntVec3.Invalid;


		public static readonly float NON_MECH_DEFAULT_RANGE = 32f;
		public static readonly float MECH_DEFAULT_RANGE = 40f;

		private bool calculated;
		private IntVec3 lastPosition;
		public int lastSightRange;

		private bool lastIsPeeking;
		private Faction lastFaction;
		private short[] lastFactionShownCells;

		private float baseViewRange;
		
		private bool[] viewMap1;
		private bool[] viewMap2;

		private CellRect viewRect;

		private bool viewMapSwitch = false;

		private IntVec3[] viewPositions;
		
		private Map map;
		private MapComponentSeenFog mapCompSeenFog;
		private ThingGrid thingGrid;
		private GlowGrid glowGrid;
		private RoofGrid roofGrid;
		private WeatherManager weatherManager;
		private int mapSizeX;
		private int mapSizeZ;

		private CompHiddenable compHiddenable;
		private CompGlower compGlower;
		private CompPowerTrader compPowerTrader;
		private CompRefuelable compRefuelable;
		private CompFlickable compFlickable;
		private CompMannable compMannable;
		private CompProvideVision compProvideVision;

		private bool setupDone = false;

		private Pawn pawn;
		private ThingDef def;
		private bool isMechanoid;
		private PawnCapacitiesHandler capacities;
		private Building building;
		private Building_TurretGun turret;
		private List<Hediff> hediffs;
		private Pawn_PathFollower pawnPather;

		private RaceProperties raceProps;

		private int lastMovementTick;

		private int lastPositionUpdateTick;

		private bool disabled;


		public override void PostSpawnSetup(bool respawningAfterLoad) {
			setupDone = true;

			calculated = false;
			lastPosition = iv3Invalid;
			lastSightRange = 0;
			lastIsPeeking = false;

			viewMap1 = null;
			viewMap2 = null;

			viewRect = new CellRect(-1, -1, 0, 0);

			viewPositions = new IntVec3[5];

			compHiddenable = mainComponent.compHiddenable;
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
				raceProps = pawn.RaceProps;
				hediffs = pawn.health.hediffSet.hediffs;
				capacities = pawn.health.capacities;
				pawnPather = pawn.pather;
			}

			def = parent.def;
			if (def.race != null) {
				isMechanoid = def.race.IsMechanoid;
			} else {
				isMechanoid = false;
			}
			
			if (!isMechanoid) {
				baseViewRange = NON_MECH_DEFAULT_RANGE;
			} else {
				baseViewRange = MECH_DEFAULT_RANGE;
			}

			if ((pawn == null) && !(turret != null && compMannable == null) && (compProvideVision == null) && (building == null)) {
				// Disable the component and remove from the main one (this thing doesn't need the FoV calculation).
				Log.Message("Removing unneeded FoV watcher from " + parent.ThingID);
				disabled = true;
				mainComponent.compFieldOfViewWatcher = null;
			} else {
				disabled = false;
			}

			initMap();

			lastMovementTick = Find.TickManager.TicksGame;
			lastPositionUpdateTick = lastMovementTick;
			updateFoV();
		}

		public override void PostExposeData() {
			Scribe_Values.Look<int>(ref this.lastMovementTick, "fovLastMovementTick", Find.TickManager.TicksGame, false);
			// Scribe_Values.Look<bool>(ref this.disabled, "fovDisabled", false, false);
		}

		public override void ReceiveCompSignal(string signal) {
			updateFoV();
		}

		public override void CompTick() {
#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.tick");
#endif
			if (disabled) {
				return;
			}

			int currentTick = Find.TickManager.TicksGame;

			if (pawn != null) {
				// Update at every position change and then after every 1/2 second from last position change.
				if (pawnPather == null) {
					pawnPather = pawn.pather;
				}
				if (pawnPather != null && pawnPather.Moving) {
					lastMovementTick = currentTick;
				}

				if (lastPosition != iv3Invalid && lastPosition != parent.Position) {
					lastPositionUpdateTick = currentTick;
					updateFoV();
				} else if ((currentTick - lastPositionUpdateTick) % 30 == 0) {
					updateFoV();
				}

			// Non pawns update at most every 30 ticks or when position change.
			} else if ((lastPosition != iv3Invalid && lastPosition != parent.Position) || currentTick % 30 == 0) {
				updateFoV();
			}

#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.tick");
#endif
		}

		private void initMap() {
			if (map != parent.Map) {
				if (map != null && lastFaction != null) {
					unseeSeenCells(lastFaction, lastFactionShownCells);
				}
				if (!disabled && mapCompSeenFog != null) {
					mapCompSeenFog.fowWatchers.Remove(this);
				}
				map = parent.Map;
				mapCompSeenFog = map.getMapComponentSeenFog();
				thingGrid = map.thingGrid;
				glowGrid = map.glowGrid;
				roofGrid = map.roofGrid;
				weatherManager = map.weatherManager;
				lastFactionShownCells = mapCompSeenFog.getFactionShownCells(parent.Faction);

				if (!disabled) {
					mapCompSeenFog.fowWatchers.Add(this);
				}

				mapSizeX = map.Size.x;
				mapSizeZ = map.Size.z;
			}
		}

		public void updateFoV(bool forceUpdate = false) {
			if (disabled || !setupDone || Current.ProgramState == ProgramState.MapInitializing) {
				return;
			}

#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.updateFoV");
#endif
			ThingWithComps thing = base.parent;
			IntVec3 newPosition = thing.Position;

			if (thing != null && thing.Spawned && thing.Map != null && newPosition != iv3Invalid) {
				initMap();

				Faction newFaction = thing.Faction;

				if (newFaction != null && (pawn == null || !pawn.Dead)) {
					// Faction things or alive pawn!

					if (pawn != null) {
						// Alive Pawns!

						int sightRange;
						bool isPeeking = false;
						if (raceProps != null && raceProps.Animal && (pawn.playerSettings == null || pawn.playerSettings.Master == null || pawn.training == null || !pawn.training.HasLearned(TrainableDefOf.Release))) {
							// If animal, only those with a master set and release training can contribute to the faction FoW.
							sightRange = -1;
						} else {
							sightRange = Mathf.RoundToInt(calcPawnSightRange(newPosition, false, false));

							if ((pawnPather == null || !pawnPather.Moving) && pawn.CurJob != null) {
								JobDef jobDef = pawn.CurJob.def;
								if (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.Wait_Combat || jobDef == JobDefOf.Hunt) {
									isPeeking = true;
								}
							}
						}

						if (!calculated || forceUpdate || newFaction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange || isPeeking != lastIsPeeking) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;
							lastIsPeeking = isPeeking;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != newFaction) {
								if (lastFaction != null) {
									unseeSeenCells(lastFaction, lastFactionShownCells);
								}
								lastFaction = newFaction;
								lastFactionShownCells = mapCompSeenFog.getFactionShownCells(newFaction);
							}


							if (sightRange != -1) {
								calculateFoV(thing, sightRange, isPeeking);
							} else {
								unseeSeenCells(lastFaction, lastFactionShownCells);
							}
						}

					} else if (turret != null && compMannable == null) {
						// Automatic turrets!

						int sightRange = Mathf.RoundToInt(turret.GunCompEq.PrimaryVerb.verbProps.range);

						if (Find.Storyteller.difficulty.difficulty >= 4 || // Intense and Extreme difficulties disable FoV from turrets.
									(compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = 0;
						}

						if (!calculated || forceUpdate || newFaction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != newFaction) {
								if (lastFaction != null) {
									unseeSeenCells(lastFaction, lastFactionShownCells);
								}
								lastFaction = newFaction;
								lastFactionShownCells = mapCompSeenFog.getFactionShownCells(newFaction);
							}

							if (sightRange != 0) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells(lastFaction, lastFactionShownCells);
								revealOccupiedCells();
							}
						}

					} else if (compProvideVision != null) {
						// Vision providers!

						// TODO: Calculate range applying dark and weather debufs. 
						int sightRange = Mathf.RoundToInt(compProvideVision.Props.viewRadius);

						if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = 0;
						}

						if (!calculated || forceUpdate || newFaction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != newFaction) {
								if (lastFaction != null) {
									unseeSeenCells(lastFaction, lastFactionShownCells);
								}
								lastFaction = newFaction;
								lastFactionShownCells = mapCompSeenFog.getFactionShownCells(newFaction);
							}

							if (sightRange != 0) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells(lastFaction, lastFactionShownCells);
								revealOccupiedCells();
							}
						}
					} else if (building != null) {
						// Generic building.

						int sightRange = 0;

						if (!calculated || forceUpdate || newFaction != lastFaction || newPosition != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = newPosition;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != newFaction) {
								if (lastFaction != null) {
									unseeSeenCells(lastFaction, lastFactionShownCells);
								}
								lastFaction = newFaction;
								lastFactionShownCells = mapCompSeenFog.getFactionShownCells(newFaction);
							}

							unseeSeenCells(lastFaction, lastFactionShownCells);
							revealOccupiedCells();
						}
					} else {
						Log.Warning("Non disabled thing... " + parent.ThingID);
					}
				} else if (newFaction != lastFaction) {
					// Faction change (from a faction to nothing). Unseen and clear old seen cells
					if (lastFaction != null) {
						unseeSeenCells(lastFaction, lastFactionShownCells);
					}
					lastFaction = newFaction;
					lastFactionShownCells = mapCompSeenFog.getFactionShownCells(newFaction);
				}
			}

#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.updateFoV");
#endif
		}

		public float calcPawnSightRange(IntVec3 position, bool forTargeting, bool shouldMove) {
			if (pawn == null) {
				Log.Error("calcPawnSightRange performed on non pawn thing");
				return 0;
			}

#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.calcPawnSightRange");
#endif
			float sightRange = 0f;

			initMap();

			bool sleeping = !isMechanoid && pawn.CurJob != null && pawn.jobs.curDriver.asleep;

			if (!shouldMove && !sleeping && (pawnPather == null || !pawnPather.Moving)) {
				Verb attackVerb = null;
				if (pawn.CurJob != null) {
					JobDef jobDef = pawn.CurJob.def;
					if (jobDef == JobDefOf.ManTurret) {
						Building_Turret mannedTurret = pawn.CurJob.targetA.Thing as Building_Turret;
						if (mannedTurret != null) {
							attackVerb = mannedTurret.AttackVerb;
						}
					} else if (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.Wait_Combat || jobDef == JobDefOf.Hunt) {
						if (pawn.equipment != null) {
							ThingWithComps primary = pawn.equipment.Primary;
							if (primary != null && primary.def.IsRangedWeapon) {
								attackVerb = primary.GetComp<CompEquippable>().PrimaryVerb;
							}
						}
					}
				}

				if (attackVerb != null && attackVerb.verbProps.range > baseViewRange && attackVerb.verbProps.requireLineOfSight && attackVerb.EquipmentSource.def.IsRangedWeapon) {
					float attackVerbRange = attackVerb.verbProps.range;
					if (baseViewRange < attackVerbRange) {
						int ticksStanding = Find.TickManager.TicksGame - lastMovementTick;

						float statValue = pawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
						int ticksToSearch = (attackVerb.verbProps.warmupTime * statValue).SecondsToTicks() * Mathf.RoundToInt((attackVerbRange - baseViewRange) / 2);

						if (ticksStanding >= ticksToSearch) {
							sightRange = attackVerbRange * capacities.GetLevel(PawnCapacityDefOf.Sight);
						} else {
							int incValue = Mathf.RoundToInt((attackVerbRange - baseViewRange) * ((float) ticksStanding / ticksToSearch));

							sightRange = (baseViewRange + incValue) * capacities.GetLevel(PawnCapacityDefOf.Sight);
						}
					}
				}
			}

			if (sightRange == 0f) {
				sightRange = baseViewRange * capacities.GetLevel(PawnCapacityDefOf.Sight);
			}

			if (!forTargeting && sleeping) {
				// Sleeping: sight reduced to 20% (if not for targeting).
				sightRange *= 0.2f;
			}
			// TODO: Apply moving penality?
			/*else if (!calcOnlyBase && pawnPather.Moving) {
				// When moving, sight reduced to 90%s.
				sightRange *= 0.9f;
			}
			*/

			// Check if standing on an affect view object.
			List<CompAffectVision> compsAffectVision = mapCompSeenFog.compAffectVisionGrid[(position.z * mapSizeX) + position.x];
			int compsCount = compsAffectVision.Count;
			for (int i = 0; i < compsCount; i++) {
				sightRange *= compsAffectVision[i].Props.fovMultiplier;
			}

			// Additional dark and weather debuff.
			if (!isMechanoid) {
				float currGlow = glowGrid.GameGlowAt(position);
				if (currGlow != 1f) {
					float darkModifier = 0.6f;
					// Each bionic eye reduce the dark debuff by 20.
					int hediffsCount = hediffs.Count;
					for (int i = 0; i < hediffsCount; i++) {
						if (hediffs[i].def == HediffDefOf.BionicEye) {
							darkModifier += 0.2f;
						}
					}

					// Apply only if to debuff.
					if (darkModifier < 1f) {
						// Adjusted to glow (100% full light - 60% dark).
						sightRange *= Mathf.Lerp(darkModifier, 1f, currGlow);
					}
				}

				if (!roofGrid.Roofed(position.x, position.z)) {
					float weatherFactor = weatherManager.CurWeatherAccuracyMultiplier;
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

#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.calcPawnSightRange");
#endif
			return sightRange;
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			if (!disabled && mapCompSeenFog != null) {
				mapCompSeenFog.fowWatchers.Remove(this);
			}

			if (lastFaction != null) {
				unseeSeenCells(lastFaction, lastFactionShownCells);
			}
		}

		public void calculateFoV(Thing thing, int intRadius, bool peek) {
			if (!setupDone) {
				return;
			}

#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.calculateFoV");
#endif

			int mapSizeX = this.mapSizeX;
			int mapSizeZ = this.mapSizeZ;

			bool[] oldViewMap = viewMapSwitch ? this.viewMap1 : this.viewMap2;
			bool[] newViewMap = viewMapSwitch ? this.viewMap2 : this.viewMap1;

			IntVec3 position = thing.Position;
			Faction faction = lastFaction;
			short[] factionShownCells = lastFactionShownCells;

			int peekRadius = (peek ? intRadius + 1 : intRadius);

			// Calculate new view rect.
			CellRect occupedRect = thing.OccupiedRect();
			int newViewRectMinX = Math.Min(position.x - peekRadius, occupedRect.minX);
			int newViewRectMaxX = Math.Max(position.x + peekRadius, occupedRect.maxX);
			int newViewRectMinZ = Math.Min(position.z - peekRadius, occupedRect.minZ);
			int newViewRectMaxZ = Math.Max(position.z + peekRadius, occupedRect.maxZ);

			int newViewWidth = newViewRectMaxX - newViewRectMinX + 1;
			int newViewArea = newViewWidth * (newViewRectMaxZ - newViewRectMinZ + 1);


			int oldViewRectMinZ = viewRect.minZ;
			int oldViewRectMaxZ = viewRect.maxZ;
			int oldViewRectMinX = viewRect.minX;
			int oldViewRectMaxX = viewRect.maxX;

			int oldViewWidth = viewRect.Width;
			int oldViewArea = viewRect.Area;


			// Create the new view map if needed.
			if (newViewMap == null || newViewMap.Length < newViewArea) {
				newViewMap = new bool[(int) (newViewArea * 1.20f)];
				if (viewMapSwitch) {
					this.viewMap2 = newViewMap;
				} else {
					this.viewMap1 = newViewMap;
				}
			}


#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.calculateFoV-seen");
#endif
			// Occupied cells are always visible.
			int occupiedX;
			int occupiedZ;
			int oldViewRectIdx;
			for (occupiedX = occupedRect.minX; occupiedX <= occupedRect.maxX; occupiedX++) {
				for (occupiedZ = occupedRect.minZ; occupiedZ <= occupedRect.maxZ; occupiedZ++) {
					newViewMap[((occupiedZ - newViewRectMinZ) * newViewWidth) + (occupiedX - newViewRectMinX)] = true;
					if (oldViewMap == null || occupiedX < oldViewRectMinX || occupiedZ < oldViewRectMinZ || occupiedX > oldViewRectMaxX || occupiedZ > oldViewRectMaxZ) {
						mapCompSeenFog.incrementSeen(faction, factionShownCells, (occupiedZ * mapSizeX) + occupiedX);
					} else {
						oldViewRectIdx = ((occupiedZ - oldViewRectMinZ) * oldViewWidth) + (occupiedX - oldViewRectMinX);
						ref bool oldViewMapValue = ref oldViewMap[oldViewRectIdx];
						if (!oldViewMapValue) {
							// Old cell was not visible. Increment seen counter in global grid.
							mapCompSeenFog.incrementSeen(faction, factionShownCells, (occupiedZ * mapSizeX) + occupiedX);
						} else {
							// Old cell was already visible. Mark it to not be unseen.
							oldViewMapValue = false;
						}
					}
				}
			}

			// Calculate Field of View only if necessary.
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

				for (int i = 0; i < viewPositionsCount; i++) {
					ref IntVec3 viewPosition = ref viewPositions[i];
					if (viewPosition.x >= 0 && viewPosition.z >= 0 && viewPosition.x <= mapWitdh && viewPosition.z <= mapHeight &&
								(i == 0 || viewPosition.IsInside(thing) || !viewBlockerCells[(viewPosition.z * mapSizeX)  + viewPosition.x])) {
						ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
							viewBlockerCells, mapSizeX, mapSizeZ, 
							true, mapCompSeenFog, faction, factionShownCells,
							newViewMap, newViewRectMinX, newViewRectMinZ, newViewWidth,
							oldViewMap, oldViewRectMinX, oldViewRectMaxX, oldViewRectMinZ, oldViewRectMaxZ, oldViewWidth);
					}
				}
			}
#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.calculateFoV-seen");
#endif

#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.calculateFoV-unsee");
#endif
			// Mark old cells not present anymore in the updated FoV.
			int oldX;
			int oldZ;
			if (oldViewMap != null) {
				for (int i = 0; i < oldViewArea; i++) {
					ref bool oldViewMapVisible = ref oldViewMap[i];
					if (oldViewMapVisible) {
						oldViewMapVisible = false;

						oldX = oldViewRectMinX + (i % oldViewWidth);
						oldZ = oldViewRectMinZ + (i / oldViewWidth);
						if (oldZ >= 0 && oldZ <= mapSizeZ && oldX >= 0 && oldX <= mapSizeX) {
							mapCompSeenFog.decrementSeen(faction, factionShownCells, (oldZ * mapSizeX) + oldX);
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
#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.calculateFoV-unsee");
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.calculateFoV");
#endif
		}

		public void refreshFovTarget(ref IntVec3 targetPos) {
			if (!setupDone) {
				return;
			}

#if InternalProfile
			ProfilingUtils.startProfiling("CompFieldOfViewWatcher.refreshFovTarget");
#endif
			Thing thing = parent;

			bool[] oldViewMap = viewMapSwitch ? this.viewMap1 : this.viewMap2;
			bool[] newViewMap = viewMapSwitch ? this.viewMap2 : this.viewMap1;

			if (oldViewMap == null || lastPosition != parent.Position) {
				// View never calculated or position changed: perform full calculation.
				updateFoV(true);

			} else {
				int intRadius = lastSightRange;
				bool peek = lastIsPeeking;

				int mapSizeX = this.mapSizeX;
				int mapSizeZ = this.mapSizeZ;

				IntVec3 position = thing.Position;
				Faction faction = lastFaction;
				short[] factionShownCells = lastFactionShownCells;

				CellRect occupiedRect = thing.OccupiedRect();

				// New view rect is equal to the previous.
				int viewRectMinZ = viewRect.minZ;
				int viewRectMaxZ = viewRect.maxZ;
				int viewRectMinX = viewRect.minX;
				int viewRectMaxX = viewRect.maxX;

				int viewWidth = viewRect.Width;
				int viewArea = viewRect.Area;

				// Create the new view map if needed.
				if (newViewMap == null || newViewMap.Length < viewArea) {
					newViewMap = new bool[(int)(viewArea * 1.20f)];
					if (viewMapSwitch) {
						this.viewMap2 = newViewMap;
					} else {
						this.viewMap1 = newViewMap;
					}
				}

				// Set occupied cells (that should be already visible...) and cleaning old view map.
				int occupiedX;
				int occupiedZ;
				int viewRectIdx;
				for (occupiedX = occupiedRect.minX; occupiedX <= occupiedRect.maxX; occupiedX++) {
					for (occupiedZ = occupiedRect.minZ; occupiedZ <= occupiedRect.maxZ; occupiedZ++) {
						viewRectIdx = ((occupiedZ - viewRectMinZ) * viewWidth) + (occupiedX - viewRectMinX);

						newViewMap[viewRectIdx] = true;
						oldViewMap[viewRectIdx] = false;
					}
				}

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

				// Calculate FOV of the affected quadrant for view positions.
				bool q1updated = false;
				bool q2updated = false;
				bool q3updated = false;
				bool q4updated = false;
				for (int viewPosIdx = 0; viewPosIdx < viewPositionsCount; viewPosIdx++) {
					ref IntVec3 viewPosition = ref viewPositions[viewPosIdx];
					if (viewPosition.x >= 0 && viewPosition.z >= 0 && viewPosition.x <= mapWitdh && viewPosition.z <= mapHeight &&
								(viewPosIdx == 0 || viewPosition.IsInside(thing) || !viewBlockerCells[(viewPosition.z * mapSizeX) + viewPosition.x])) {
						if (viewPosition.x <= targetPos.x) {
							if (viewPosition.z <= targetPos.z) {
								q1updated = true;
							} else {
								q4updated = true;
							}
						} else {
							if (viewPosition.z <= targetPos.z) {
								q2updated = true;
							} else {
								q3updated = true;
							}
						}
					}
				}

				for (int viewPosIdx = 0; viewPosIdx < viewPositionsCount; viewPosIdx++) {
					ref IntVec3 viewPosition = ref viewPositions[viewPosIdx];
					if (viewPosition.x >= 0 && viewPosition.z >= 0 && viewPosition.x <= mapWitdh && viewPosition.z <= mapHeight &&
								(viewPosIdx == 0 || viewPosition.IsInside(thing) || !viewBlockerCells[(viewPosition.z * mapSizeX) + viewPosition.x])) {
						if (q1updated) {
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 0);
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 1);
						}
						if (q2updated) {
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 2);
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 3);
						}
						if (q3updated) {
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 4);
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 5);
						}
						if (q4updated) {
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 6);
							ShadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								viewBlockerCells, mapSizeX, mapSizeZ,
								true, mapCompSeenFog, faction, factionShownCells,
								newViewMap, viewRectMinX, viewRectMinZ, viewWidth,
								oldViewMap, viewRectMinX, viewRectMaxX, viewRectMinZ, viewRectMaxZ, viewWidth, 7);
						}
					}
				}

				// Mark old cells not present anymore in the updated FoV and copy non affected quadrants.
				int x;
				int z;
				byte quadrant;
				for (int i = 0; i < viewArea; i++) {
					ref bool oldViewMapVisible = ref oldViewMap[i];
					if (oldViewMapVisible) {
						oldViewMapVisible = false;

						x = viewRectMinX + (i % viewWidth);
						z = viewRectMinZ + (i / viewWidth);

						if (position.x <= x) {
							if (position.z <= z) {
								quadrant = 1;
							} else {
								quadrant = 4;
							}
						} else {
							if (position.z <= z) {
								quadrant = 2;
							} else {
								quadrant = 3;
							}
						}

						// Copy non affected quadrants.
						if ((!q1updated && quadrant == 1) ||
								(!q2updated && quadrant == 2) ||
								(!q3updated && quadrant == 3) ||
								(!q4updated && quadrant == 4)) {
							newViewMap[i] = true;
						} else if (z >= 0 && z <= mapSizeZ && x >= 0 && x <= mapSizeX) {
							mapCompSeenFog.decrementSeen(faction, factionShownCells, (z * mapSizeX) + x);
						}
					}
				}

				// Use te new view area.
				viewMapSwitch = !viewMapSwitch;
			}

#if InternalProfile
			ProfilingUtils.stopProfiling("CompFieldOfViewWatcher.refreshFovTarget");
#endif
		}

		private void unseeSeenCells(Faction faction, short[] factionShownCells) {
			bool[] viewMap = viewMapSwitch ? this.viewMap1 : this.viewMap2;

			if (viewMap != null) {
				int viewRectMinZ = viewRect.minZ;
				int viewRectMaxZ = viewRect.maxZ;
				int viewRectMinX = viewRect.minX;
				int viewRectMaxX = viewRect.maxX;

				int mapX = map.Size.x;
				int mapZ = map.Size.z;

				int viewWidth = viewRect.Width;
				int viewArea = viewRect.Area;

				int x;
				int z;
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						viewMap[i] = false;

						x = viewRectMinX + (i % viewWidth);
						z = viewRectMinZ + (i / viewWidth);
						if (z >= 0 && z <= mapZ && x >= 0 && x <= mapX) {
							mapCompSeenFog.decrementSeen(faction, factionShownCells, (z * mapX) + x);
						}
					}
				}

				// Clear the view rect.
				viewRect.maxX = -1;
				viewRect.minX = -1;
				viewRect.maxZ = -1;
				viewRect.minZ = -1;
			}
		}

		private void revealOccupiedCells() {
			if (parent.Faction == Faction.OfPlayer) {
				CellRect occupedRect = parent.OccupiedRect();

				int occupiedX;
				int occupiedZ;
				for (occupiedX = occupedRect.minX; occupiedX <= occupedRect.maxX; occupiedX++) {
					for (occupiedZ = occupedRect.minZ; occupiedZ <= occupedRect.maxZ; occupiedZ++) {
						mapCompSeenFog.revealCell((occupiedZ * mapSizeX) + occupiedX);
					}
				}
			}
		}

	}
}
