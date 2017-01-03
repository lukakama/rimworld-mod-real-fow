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

		private bool[] viewMap;
		private CellRect viewRect;
		private bool[] newViewMap;
		private CellRect newViewRect;

		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private CompHiddenable compHiddenable;
		private CompGlower compGlower;
		private CompPowerTrader compPowerTrader;
		private CompRefuelable compRefuelable;
		private CompFlickable compFlickable;
		private CompMannable mannableComp;

		private bool setupDown = false;

		private Pawn pawn;
		private Building_TurretGun turret;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			setupDown = true;

			calculated = false;
			lastPosition = IntVec3.Invalid;
			lastSightRange = 0f;
			lastIsPeeking = false;

			viewMap = new bool[1 + ((int) MAX_RANGE * 2)];
			viewRect = new CellRect();

			newViewMap = new bool[1 + ((int) MAX_RANGE * 2)];
			newViewRect = new CellRect();


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
			if (!setupDown || Current.ProgramState == ProgramState.MapInitializing) {
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

		public void calculateFoV(Thing thing, float radius, bool peek) {
			/*if (!(thing is Pawn)) {
				Log.Message("calculateFoV: " + thing.ThingID);
			}*/

			int intRadius = Mathf.RoundToInt(radius);

			IntVec3 position = thing.Position;

			// Calculate new view rect.
			CellRect occupedRect = thing.OccupiedRect();
			newViewRect.maxX = Mathf.Max(position.x + intRadius, occupedRect.maxX);
			newViewRect.minX = Mathf.Min(position.x - intRadius, occupedRect.minX);
			newViewRect.maxZ = Mathf.Max(position.z + intRadius, occupedRect.maxZ);
			newViewRect.minZ = Mathf.Min(position.z - intRadius, occupedRect.minZ);

			int newViewWidth = newViewRect.Width;
			int newViewArea = newViewRect.Area;

			// Clear or reset the new view map.
			if (newViewMap.Length < newViewArea) {
				newViewMap = new bool[newViewArea];
			} else {
				for (int i = 0; i < newViewArea; i++) {
					newViewMap[i] = false;
				}
			}


			// Occupied cells ar always visible.
			foreach (IntVec3 occupiedCell in occupedRect.Cells) {
				if (occupiedCell.InBounds(map)) {
					newViewMap[CellIndicesUtility.CellToIndex(occupiedCell.x - newViewRect.minX, occupiedCell.z - newViewRect.minZ, newViewWidth)] = true;
					mapCompSeenFog.incrementSeen(occupiedCell);
				}
			}

			// Calculate Field of View (if necessary).
			if (intRadius > 0) {
				IEnumerable<IntVec3> viewPositions = new IntVec3[] { thing.Position };

				if (peek) {
					viewPositions = viewPositions.Concat(GenAdj.CellsAdjacentCardinal(thing));
				}

				foreach (IntVec3 viewPosition in viewPositions) {
					if (viewPosition.IsInside(thing) || viewPosition.CanBeSeenOverFast(map)) {
						if (intRadius != 0) {
							ShadowCaster.ComputeFieldOfViewWithShadowCasting(viewPosition.x, viewPosition.z, intRadius - (viewPosition.IsInside(thing) ? 0 : 1),
								// isOpaque
								(int x, int y) => {
									// Out of map positions are opaques...
									if (x < 0 || y < 0 || x >= map.Size.x || y >= map.Size.z) {
										return true;
									}
									Building b = map.edificeGrid[map.cellIndices.CellToIndex(x, y)];
									return (b != null && !b.CanBeSeenOver());
								},
								// setFoV
								(int x, int y) => {
									if (x >= 0 && y >= 0 && x < map.Size.x && y < map.Size.z) {
										int idx = CellIndicesUtility.CellToIndex(x - newViewRect.minX, y - newViewRect.minZ, newViewWidth);
										if (!newViewMap[idx]) {
											newViewMap[idx] = true;
											mapCompSeenFog.incrementSeen(new IntVec3(x, 0, y));
										}
									}
								});
						}
					}
				}
			}

			unseeSeenCells();

			// Copy the new view area.
			if (viewMap.Length < newViewArea) {
				viewMap = new bool[newViewArea];
			}
			Array.Copy(newViewMap, viewMap, newViewArea);

			// Copy the new view rect.
			viewRect.maxX = newViewRect.maxX;
			viewRect.minX = newViewRect.minX;
			viewRect.maxZ = newViewRect.maxZ;
			viewRect.minZ = newViewRect.minZ;
		}

		private void unseeSeenCells() {
			int viewWidth = viewRect.Width;
			int viewHeight = viewRect.Height;
			int viewArea = viewRect.Area;
			for (int i = 0; i < viewArea; i++) {
				if (viewMap[i]) {
					IntVec3 c = CellIndicesUtility.IndexToCell(i, viewWidth, viewHeight);
					c.x += viewRect.minX;
					c.z += viewRect.minZ;

					mapCompSeenFog.decrementSeen(c);

					viewMap[i] = false;
				}
			}
		}
	}
}
