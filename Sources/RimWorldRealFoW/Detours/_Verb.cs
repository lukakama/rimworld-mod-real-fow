﻿//   Copyright 2017 Luca De Petrillo
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
using RimWorldRealFoW.ThingComps;
using RimWorldRealFoW.ThingComps.ThingSubComps;
using RimWorldRealFoW.Utils;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW.Detours {
	static class _Verb {
		private static void CanHitCellFromCellIgnoringRange_Postfix(this Verb __instance, ref bool __result, IntVec3 sourceSq, IntVec3 targetLoc, bool includeCorners = false) {
			if (__result && __instance.verbProps.requireLineOfSight) {
				__result = (__instance.caster.Faction != null && seenByFaction(__instance.caster, targetLoc)) || fovLineOfSight(sourceSq, targetLoc, __instance.caster);
			}
		}

		private static bool seenByFaction(Thing thing, IntVec3 targetLoc) {
			MapComponentSeenFog seenFog = thing.Map.getMapComponentSeenFog();
			if (seenFog != null) {
				return seenFog.isShown(thing.Faction, targetLoc);
			}

			return true;
		}

		private static bool fovLineOfSight(IntVec3 sourceSq, IntVec3 targetLoc, Thing thing) {
			// If the thing is mannable, then use the manning pawn to perform the calculation.
			CompMannable compMannable = thing.TryGetComp<CompMannable>();
			if (compMannable != null) {
				thing = compMannable.ManningPawn;
				// Apply interaction cell offset.
				sourceSq += (thing.Position - thing.InteractionCell);
			}

			// If not a pawn, then doesn't need a fov calculation.
			if (!(thing is Pawn)) {
				return true;
			}

			MapComponentSeenFog seenFog = thing.Map.getMapComponentSeenFog();
			CompMainComponent compMain = (CompMainComponent) thing.TryGetComp(CompMainComponent.COMP_DEF);
			CompFieldOfViewWatcher compFoV = compMain.compFieldOfViewWatcher;
			// If requires moving, calculate only the base sight.
			int sightRange = Mathf.RoundToInt(compFoV.calcPawnSightRange(sourceSq, true, !thing.Position.AdjacentToCardinal(sourceSq)));

			if (!sourceSq.InHorDistOf(targetLoc, sightRange)) {
				// If out of sightRange.
				return false;
			}


			// Limit to needed octant.
			IntVec3 dir = targetLoc - sourceSq;

			byte octant;
			if (dir.x >= 0) {
				if (dir.z >= 0) {
					if (dir.x >= dir.z) {
						octant = 0;
					} else {
						octant = 1;
					}
				} else {
					if (dir.x >= -dir.z) {
						octant = 7;
					} else {
						octant = 6;
					}
				}
			} else {
				if (dir.z >= 0) {
					if (-dir.x >= dir.z) {
						octant = 3;
					} else {
						octant = 2;
					}
				} else {
					if (-dir.x >= -dir.z) {
						octant = 4;
					} else {
						octant = 5;
					}
				}
			}

			Map map = thing.Map;
			bool[] targetFound = new bool[1];
			ShadowCaster.computeFieldOfViewWithShadowCasting(sourceSq.x, sourceSq.z, sightRange,
					seenFog.viewBlockerCells, map.Size.x, map.Size.z, 
					false, null, null, null,
					targetFound, 0, 0, 0,
					null, 0, 0, 0, 0, 0,
					octant, targetLoc.x, targetLoc.z);
			return targetFound[0];
		}
	}
}
