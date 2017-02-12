using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _EnvironmentInspectDrawer {

		public static bool ShouldShow() {
			Map map = Find.VisibleMap;
			MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();

			return Find.PlaySettings.showEnvironment && !Mouse.IsInputBlockedNow && UI.MouseCell().InBounds(Find.VisibleMap) && !UI.MouseCell().Fogged(Find.VisibleMap) && (mapCmq == null || mapCmq.knownCells[map.cellIndices.CellToIndex(UI.MouseCell())]);
		}
	}
}
