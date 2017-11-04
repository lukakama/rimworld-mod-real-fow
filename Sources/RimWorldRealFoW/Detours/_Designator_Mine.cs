using Harmony;
using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Designator_Mine {
		public static void CanDesignateCell_Postfix(IntVec3 c, ref Designator __instance, ref AcceptanceReport __result) {
			if (!__result.Accepted) {
				Map map = Traverse.Create(__instance).Property("Map").GetValue<Map>();

				if (map.designationManager.DesignationAt(c, DesignationDefOf.Mine) == null) {
					MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();
					if (mapCmq != null && c.InBounds(map) && !mapCmq.knownCells[map.cellIndices.CellToIndex(c)]) {
						__result = true;
					}
				}
			}
		}
	}
}
