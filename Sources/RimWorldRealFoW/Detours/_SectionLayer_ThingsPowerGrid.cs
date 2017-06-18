using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _SectionLayer_ThingsPowerGrid {
		public static void TakePrintFrom(this SectionLayer_ThingsPowerGrid _this, Thing t) {
			if (t.fowIsVisible(true)) {
				Building building = t as Building;
				if (building != null) {
					building.PrintForPowerGrid(_this);
				}
			}
		}
	}
}
