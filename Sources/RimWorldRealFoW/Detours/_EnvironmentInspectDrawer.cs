using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _EnvironmentStatsDrawer {

		public static void ShouldShowWindowNow_Postfix(ref bool __result) {
			if (__result) {
				Map map = Find.CurrentMap;
				MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();

				__result = (mapCmq == null || mapCmq.knownCells[map.cellIndices.CellToIndex(UI.MouseCell())]);
			}
		}
	}
	
}
