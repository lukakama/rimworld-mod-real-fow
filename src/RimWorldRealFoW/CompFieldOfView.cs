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
	class CompFieldOfView : ThingComp {
		public static readonly float NON_MECH_DEFAULT_RANGE = 32f;
		public static readonly float MECH_DEFAULT_RANGE = 40f;

		private bool calculated;
		private IntVec3 lastPosition;
		private float lastSightRange;
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

		private CompHiddenable compHiddenable;
		private CompGlower compGlower;
		private CompPowerTrader compPowerTrader;
		private CompRefuelable compRefuelable;
		private CompFlickable compFlickable;
		private CompMannable mannableComp;

		public ShadowCaster shadowCaster;

		private bool setupDone = false;

		private Pawn pawn;
		private Building building;
		private Building_TurretGun turret;

		private int lastMovementTick;

		private bool disabled;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			setupDone = true;

			shadowCaster = new ShadowCaster();

			calculated = false;
			lastPosition = IntVec3.Invalid;
			lastSightRange = 0f;
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
			mannableComp = parent.GetComp<CompMannable>();

			pawn = parent as Pawn;
			building = parent as Building;
			turret = parent as Building_TurretGun;

			map = parent.Map;
			mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();

			if (parent.def.race == null || !parent.def.race.IsMechanoid) {
				baseViewRange = NON_MECH_DEFAULT_RANGE;
			} else {
				baseViewRange = MECH_DEFAULT_RANGE;
			}

			disabled = false;

			lastMovementTick = Find.TickManager.TicksGame;

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

		public override void CompTick() {
			base.CompTick();

			if (parent != null && parent.Spawned && pawn != null && pawn.pather != null && pawn.pather.MovingNow) {
				lastMovementTick = Find.TickManager.TicksGame;
			}

			// Check every 25 thick.
			if (Find.TickManager.TicksGame % 25 == 0) {
				updateFoV();
			}
		}

		private void initMap() {
			if (map != parent.Map) {
				if (map != null) {
					unseeSeenCells();
				}
				map = parent.Map;
				mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();
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

						float sightRange;
						bool isPeeking = false;
						if (pawn.RaceProps != null && pawn.RaceProps.Animal && (pawn.playerSettings == null || pawn.playerSettings.master == null || pawn.training == null || !pawn.training.IsCompleted(TrainableDefOf.Release))) {
							// If animal, only those with a master set and release training can contribute to the faction FoW.
							sightRange = -1;
						} else {
							sightRange = calcPawnSightRange(pawn.Position, false, false);

							if (pawn.CurJob != null) {
								JobDef jobDef = pawn.CurJob.def;
								if ((pawn.pather == null || !pawn.pather.MovingNow) && (jobDef == JobDefOf.AttackStatic || jobDef == JobDefOf.AttackMelee || jobDef == JobDefOf.WaitCombat || jobDef == JobDefOf.Hunt)) {
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
								lastFaction = thing.Faction;
							}

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, isPeeking);
							} else {
								unseeSeenCells();
							}
						}
					} else if (compGlower != null) {
						// Glowers!

						float sightRange = compGlower.Props.glowRadius;

						if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = -1f;
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || thing.Position != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = thing.Position;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
								lastFaction = thing.Faction;
							}

							if (sightRange != -1) {
								calculateFoV(thing, sightRange, false);
							} else {
								unseeSeenCells();
							}
						}

					} else if (turret != null && mannableComp == null) {
						// Automatic turrets!

						float sightRange = turret.GunCompEq.PrimaryVerb.verbProps.range;

						if ((compPowerTrader != null && !compPowerTrader.PowerOn) ||
								  (compRefuelable != null && !compRefuelable.HasFuel) ||
								  (compFlickable != null && !compFlickable.SwitchIsOn)) {
							sightRange = -1f;
						}

						if (!calculated || forceUpdate || thing.Faction != lastFaction || thing.Position != lastPosition || sightRange != lastSightRange) {
							calculated = true;
							lastPosition = thing.Position;
							lastSightRange = sightRange;

							// Faction change. Unseen and clear old seen cells
							if (lastFaction != null && lastFaction != thing.Faction) {
								unseeSeenCells(lastFaction);
								lastFaction = thing.Faction;
							}

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
						lastFaction = thing.Faction;
					}
				}
			}
		}

		public float calcPawnSightRange(IntVec3 position, bool forTargeting, bool shouldMove) {
			float sightRange = 0f;
			if (pawn == null) {
				Log.Error("calcPawnSightRange performed on non pawn thing");
				return sightRange;
			}

			initMap();

			bool sleeping = (parent.def.race == null || !pawn.def.race.IsMechanoid) && pawn.CurJob != null && pawn.jobs.curDriver.asleep;

			if (!shouldMove && !sleeping && (pawn.pather == null || !pawn.pather.MovingNow)) {
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
			/*else if (!calcOnlyBase && pawn.pather.MovingNow) {
				// When moving, sight reduced to 90%s.
				sightRange *= 0.9f;
			}
			*/

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
				return 1f;
			}

			return sightRange;
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			unseeSeenCells();
		}

		public void calculateFoV(Thing thing, float radius, bool peek) {
			/*if (!(thing is Pawn)) {
				Log.Message("calculateFoV: " + thing.ThingID);
			}*/

			// Local references (C# is slow to access fields...).
			Map map = this.map;
			bool[] viewMap = this.viewMap;
			CellRect viewRect = this.viewRect;
			bool[] newViewMap = this.newViewMap;
			CellRect newViewRect = this.newViewRect;
			IntVec3[] viewPositions = this.viewPositions;

			EdificeGrid edificeGrid = map.edificeGrid;
			CellIndices cellIndices = map.cellIndices;

			int intRadius = Mathf.RoundToInt(radius);

			int viewWidth = viewRect.Width;
			int viewArea = viewRect.Area;

			IntVec3 position = thing.Position;

			Faction faction = parent.Faction;

			int peekMod = (peek ? 1 : 0);

			// Calculate new view rect.
			CellRect occupedRect = thing.OccupiedRect();
			newViewRect.maxX = Math.Max(position.x + intRadius + peekMod, occupedRect.maxX);
			newViewRect.minX = Math.Min(position.x - intRadius - peekMod, occupedRect.minX);
			newViewRect.maxZ = Math.Max(position.z + intRadius + peekMod, occupedRect.maxZ);
			newViewRect.minZ = Math.Min(position.z - intRadius - peekMod, occupedRect.minZ);

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

			// Occupied cells are always visible.
			for (int x = occupedRect.minX; x <= occupedRect.maxX; x++) {
				for (int z = occupedRect.minZ; z <= occupedRect.maxZ; z++) {
					newViewMap[CellIndicesUtility.CellToIndex(x - newViewRect.minX, z - newViewRect.minZ, newViewWidth)] = true;
					// Mark as seen only new cells.
					if (x < viewRect.minX || z < viewRect.minZ || x > viewRect.maxX || z > viewRect.maxZ || viewMap.Length == 0 ||
							!viewMap[CellIndicesUtility.CellToIndex(x - viewRect.minX, z - viewRect.minZ, viewWidth)]) {
						mapCompSeenFog.incrementSeen(faction, new IntVec3(x, 0, z));
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

				for (int i = 0; i < viewPositionsCount; i++) {
					IntVec3 viewPosition = viewPositions[i];
					if (viewPosition.IsInside(thing) || viewPosition.CanBeSeenOver(map)) {
						if (intRadius != 0) {
							shadowCaster.computeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius,
								// isOpaque
								(int x, int y) => {
									// Out of map positions are opaques...
									if (x <= 0 || y <= 0 || x >= mapWitdh || y >= mapHeight) {
										return true;
									}
									Building b = edificeGrid[cellIndices.CellToIndex(x, y)];
									return (b != null && !b.CanBeSeenOver());
								},
								// setFoV
								(int x, int y) => {
									int newIdx = CellIndicesUtility.CellToIndex(x - newViewRect.minX, y - newViewRect.minZ, newViewWidth);
									if (!newViewMap[newIdx]) {
										newViewMap[newIdx] = true;
										// Mark as seen only new cells.
										if (x < viewRect.minX || y < viewRect.minZ || x > viewRect.maxX || y > viewRect.maxZ || viewMap.Length == 0 ||
												!viewMap[CellIndicesUtility.CellToIndex(x - viewRect.minX, y - viewRect.minZ, viewWidth)]) {
											mapCompSeenFog.incrementSeen(faction, new IntVec3(x, 0, y));
										}
									}
								});
						}
					}
				}
			}

			// Mark as unseen old cells not present anymore in the updated FoV.
			if (viewMap.Length > 0) {
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						int x = viewRect.minX + (i % viewWidth);
						int z = viewRect.minZ + (i / viewWidth);
						if (x < newViewRect.minX || z < newViewRect.minZ || x > newViewRect.maxX || z > newViewRect.maxZ ||
								!newViewMap[CellIndicesUtility.CellToIndex(x - newViewRect.minX, z - newViewRect.minZ, newViewWidth)]) {
							mapCompSeenFog.decrementSeen(faction, new IntVec3(x, 0, z));
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
			this.viewRect.maxX = newViewRect.maxX;
			this.viewRect.minX = newViewRect.minX;
			this.viewRect.maxZ = newViewRect.maxZ;
			this.viewRect.minZ = newViewRect.minZ;
		}

		private void unseeSeenCells(Faction faction = null) {
			if (faction == null) {
				faction = parent.Faction;
			}

			if (viewRect.maxX >= 0 && viewRect.minX >= 0 && viewRect.maxZ >= 0 && viewRect.minZ >= 0 && viewMap.Length > 0) {
				int viewWidth = viewRect.Width;
				int viewArea = viewRect.Area;
				for (int i = 0; i < viewArea; i++) {
					if (viewMap[i]) {
						mapCompSeenFog.decrementSeen(faction, new IntVec3((i % viewWidth) + viewRect.minX, 0, (i / viewWidth) + viewRect.minZ));
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
