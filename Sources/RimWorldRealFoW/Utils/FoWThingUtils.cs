using RimWorld;
using RimWorldRealFoW.ThingComps;
using RimWorldRealFoW.ThingComps.ThingSubComps;
using Verse;

namespace RimWorldRealFoW.Utils {
	public static class FoWThingUtils {
		public static bool fowIsVisible(this Thing _this, bool forRender = false) {
			if (_this.Spawned) {
				if (_this.def.isSaveable && !_this.def.saveCompressible) {
					CompHiddenable comp = _this.TryGetCompHiddenable();
					if (comp != null) {
						return !comp.hidden;
					}
				}
				return forRender || (_this.Map != null && _this.fowInKnownCell());
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
			ThingCategory thingCategory = _this.def.category;
			if (thingCategory == ThingCategory.Pawn ||
					thingCategory == ThingCategory.Building ||
					thingCategory == ThingCategory.Item ||
					thingCategory == ThingCategory.Filth ||
					thingCategory == ThingCategory.Gas ||
					_this.def.IsBlueprint) {

				ThingWithComps thingWithComps = _this as ThingWithComps;
				if (thingWithComps != null) {
					return thingWithComps.GetCompByDef(def);
				}
			}

			return null;
		}

		public static CompHiddenable TryGetCompHiddenable(this Thing _this) {
			CompMainComponent mainComp = (CompMainComponent)_this.TryGetComp(CompMainComponent.COMP_DEF);
			if (mainComp != null) {
				return mainComp.compHiddenable;
			}

			return null;
		}
	}
}
