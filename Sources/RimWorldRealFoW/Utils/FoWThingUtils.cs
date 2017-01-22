using RimWorld;
using RimWorldRealFoW.ThingComps;
using Verse;

namespace RimWorldRealFoW.Utils {
	public static class FoWThingUtils {
		public static bool fowIsVisible(this Thing _this, bool forRender = false) {
			if (_this.Spawned && _this.Map != null) {
				CompHiddenable comp = _this.TryGetComp<CompHiddenable>();
				if (comp != null && _this.def.isSaveable && !_this.def.saveCompressible) {
					return !comp.hidden;

				} else {
					return forRender || _this.fowInKnownCell();
				}
			}

			return true;
		}

		public static bool fowInKnownCell(this Thing _this) {
			MapComponentSeenFog mapComponent = _this.Map.GetComponent<MapComponentSeenFog>();
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
	}
}
