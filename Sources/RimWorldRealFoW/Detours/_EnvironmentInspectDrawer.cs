using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _EnvironmentInspectDrawer {

		public static void ShouldShow_Postfix(ref bool __result) {
			if (__result) {
				Map map = Find.VisibleMap;
				MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();

				__result = (mapCmq == null || mapCmq.knownCells[map.cellIndices.CellToIndex(UI.MouseCell())]);
			}
		}
	}
}
