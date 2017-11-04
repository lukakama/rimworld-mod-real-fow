using Harmony;
using RimWorldRealFoW.ThingComps.ThingSubComps;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Designator_Prefix {
		public static bool CanDesignateCell_Prefix(ref IntVec3 c, ref Designator __instance, ref AcceptanceReport __result) {

			Map map = Traverse.Create(__instance).Property("Map").GetValue<Map>();

			MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();
			if (mapCmq != null && c.InBounds(map) && !mapCmq.knownCells[map.cellIndices.CellToIndex(c)]) {
				__result = false;
				return false;
			}

			return true;
		}

		public static bool CanDesignateThing_Prefix(ref Thing t, ref AcceptanceReport __result) {
			CompHiddenable cmp = t.TryGetCompHiddenable();
			if (cmp != null && cmp.hidden) {
				__result = false;
				return false;
			}

			return true;
		}
	}
}
