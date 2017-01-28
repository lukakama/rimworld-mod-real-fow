using RimWorld;
using RimWorldRealFoW.ThingComps;
using System;
using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.Utils {
	public static class FoWThingUtils {
		public static bool fowIsVisible(this Thing _this, bool forRender = false) {
			if (_this.Spawned && _this.Map != null) {
				CompHiddenable comp = (CompHiddenable) _this.TryGetComp(CompHiddenable.COMP_DEF);
				if (comp != null && _this.def.isSaveable && !_this.def.saveCompressible) {
					return !comp.hidden;

				} else {
					return forRender || _this.fowInKnownCell();
				}
			}

			return true;
		}

		public static bool fowInKnownCell(this Thing _this) {
			MapComponentSeenFog mapComponent = _this.Map.getMapComponentSeenFog();
			if (mapComponent != null) {
				Faction playerFaction = Faction.OfPlayer;
				Map map = _this.Map;
				CellIndices cellIndices = map.cellIndices;

				foreach (IntVec3 cell in _this.OccupiedRect().Cells) {
					if (cell.InBounds(map) && mapComponent.knownCells[cellIndices.CellToIndex(cell)]) {
						return true;
					}
				}

				return false;
			}

			return true;
		}

		public static ThingComp TryGetComp(this Thing _this, CompProperties def) {
			ThingWithComps thingWithComps = _this as ThingWithComps;
			if (thingWithComps == null) {
				return null;
			}
			return thingWithComps.GetCompByDef(def);
		}

		public static ThingComp TryGetComp(this Thing _this, Type compType) {
			ThingWithComps thingWithComps = _this as ThingWithComps;
			if (thingWithComps == null) {
				return null;
			}
			List<ThingComp> allComps = thingWithComps.AllComps;
			for (int i = 0; i < allComps.Count; i++) {
				if (allComps[i].props.compClass == compType) {
					return allComps[i];
				}
			}
			return null;
		}


	}
}
