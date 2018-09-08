using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _BeautyUtility {

		public static void FillBeautyRelevantCells_Postfix(Map map) {
			MapComponentSeenFog mapCmq = map.getMapComponentSeenFog();
			if (mapCmq != null) {
				BeautyUtility.beautyRelevantCells.RemoveAll(c => !mapCmq.knownCells[map.cellIndices.CellToIndex(c)]);
			}
		}
	}
}
