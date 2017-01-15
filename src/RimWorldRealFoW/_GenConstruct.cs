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
using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW {
	class _GenConstruct {
		public static AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, bool godMode = false, Thing thingToIgnore = null) {
			CellRect cellRect = GenAdj.OccupiedRect(center, rot, entDef.Size);
			CellRect.CellRectIterator iterator = cellRect.GetIterator();
			while (!iterator.Done()) {
				IntVec3 current = iterator.Current;
				if (!current.InBounds(map)) {
					return new AcceptanceReport("OutOfBounds".Translate());
				}
				if (current.InNoBuildEdgeArea(map) && !DebugSettings.godMode) {
					return "TooCloseToMapEdge".Translate();
				}
				iterator.MoveNext();
			}
			if (center.Fogged(map)) {
				return "CannotPlaceInUndiscovered".Translate();
			}
			MapComponentSeenFog seenFog = map.GetComponent<MapComponentSeenFog>();
			if (seenFog != null) {
				CellRect.CellRectIterator itCellRect = cellRect.GetIterator();
				while (!itCellRect.Done()) {
					if (!seenFog.knownCells[map.cellIndices.CellToIndex(itCellRect.Current)]) {
						return "CannotPlaceInUndiscovered".Translate();
					}
					itCellRect.MoveNext();
				}
			}

			List<Thing> thingList = center.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++) {
				Thing thing = thingList[i];
				if (thing != thingToIgnore) {
					if (thing.Position == center && thing.Rotation == rot) {
						if (thing.def == entDef) {
							return new AcceptanceReport("IdenticalThingExists".Translate());
						}
						if (thing.def.entityDefToBuild == entDef) {
							if (thing is Blueprint) {
								return new AcceptanceReport("IdenticalBlueprintExists".Translate());
							}
							return new AcceptanceReport("IdenticalThingExists".Translate());
						}
					}
				}
			}
			ThingDef thingDef = entDef as ThingDef;
			if (thingDef != null && thingDef.hasInteractionCell) {
				IntVec3 c = Thing.InteractionCellWhenAt(thingDef, center, rot, map);
				if (!c.InBounds(map)) {
					return new AcceptanceReport("InteractionSpotOutOfBounds".Translate());
				}
				List<Thing> list = map.thingGrid.ThingsListAtFast(c);
				for (int j = 0; j < list.Count; j++) {
					if (list[j] != thingToIgnore) {
						if (list[j].def.passability == Traversability.Impassable) {
							return new AcceptanceReport("InteractionSpotBlocked".Translate(new object[]
							{
								list[j].LabelNoCount
							}).CapitalizeFirst());
						}
						Blueprint blueprint = list[j] as Blueprint;
						if (blueprint != null && blueprint.def.entityDefToBuild.passability == Traversability.Impassable) {
							return new AcceptanceReport("InteractionSpotWillBeBlocked".Translate(new object[]
							{
								blueprint.LabelNoCount
							}).CapitalizeFirst());
						}
					}
				}
			}
			if (entDef.passability != Traversability.Standable) {
				foreach (IntVec3 current2 in GenAdj.CellsAdjacentCardinal(center, rot, entDef.Size)) {
					if (current2.InBounds(map)) {
						thingList = current2.GetThingList(map);
						for (int k = 0; k < thingList.Count; k++) {
							Thing thing2 = thingList[k];
							if (thing2 != thingToIgnore) {
								Blueprint blueprint2 = thing2 as Blueprint;
								ThingDef thingDef3;
								if (blueprint2 != null) {
									ThingDef thingDef2 = blueprint2.def.entityDefToBuild as ThingDef;
									if (thingDef2 == null) {
										goto IL_37E;
									}
									thingDef3 = thingDef2;
								} else {
									thingDef3 = thing2.def;
								}
								if (thingDef3.hasInteractionCell && cellRect.Contains(Thing.InteractionCellWhenAt(thingDef3, thing2.Position, thing2.Rotation, thing2.Map))) {
									return new AcceptanceReport("WouldBlockInteractionSpot".Translate(new object[]
									{
										entDef.label,
										thingDef3.label
									}).CapitalizeFirst());
								}
							}
							IL_37E:;
						}
					}
				}
			}
			TerrainDef terrainDef = entDef as TerrainDef;
			if (terrainDef != null) {
				if (map.terrainGrid.TerrainAt(center) == terrainDef) {
					return new AcceptanceReport("TerrainIsAlready".Translate(new object[]
					{
						terrainDef.label
					}));
				}
				if (map.designationManager.DesignationAt(center, DesignationDefOf.SmoothFloor) != null) {
					return new AcceptanceReport("BeingSmoothed".Translate());
				}
			}
			if (!GenConstruct.CanBuildOnTerrain(entDef, center, map, rot, thingToIgnore)) {
				return new AcceptanceReport("TerrainCannotSupport".Translate());
			}
			if (!godMode) {
				CellRect.CellRectIterator iterator2 = cellRect.GetIterator();
				while (!iterator2.Done()) {
					thingList = iterator2.Current.GetThingList(map);
					for (int l = 0; l < thingList.Count; l++) {
						Thing thing3 = thingList[l];
						if (thing3 != thingToIgnore) {
							if (!GenConstruct.CanPlaceBlueprintOver(entDef, thing3.def)) {
								return new AcceptanceReport("SpaceAlreadyOccupied".Translate());
							}
						}
					}
					iterator2.MoveNext();
				}
			}
			if (entDef.PlaceWorkers != null) {
				for (int m = 0; m < entDef.PlaceWorkers.Count; m++) {
					AcceptanceReport result = entDef.PlaceWorkers[m].AllowsPlacing(entDef, center, rot, thingToIgnore);
					if (!result.Accepted) {
						return result;
					}
				}
			}
			return AcceptanceReport.WasAccepted;
		}
	}
}
