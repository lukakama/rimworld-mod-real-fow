using Harmony;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Designator_Place_Postfix {
		public static void CanDesignateCell_Postfix(ref IntVec3 c, ref Designator __instance, ref AcceptanceReport __result) {
			if (__result.Accepted) {
				Traverse traverse = Traverse.Create(__instance);
				CellRect cellRect = GenAdj.OccupiedRect(c, traverse.Field("placingRot").GetValue<Rot4>(),  traverse.Property("PlacingDef").GetValue<BuildableDef>().Size);

				Map map = traverse.Property("Map").GetValue<Map>();
				MapComponentSeenFog seenFog = map.getMapComponentSeenFog();
				if (seenFog != null) {
					CellRect.CellRectIterator itCellRect = cellRect.GetIterator();
					while (!itCellRect.Done()) {
						if (!seenFog.knownCells[map.cellIndices.CellToIndex(itCellRect.Current)]) {
							__result = "CannotPlaceInUndiscovered".Translate();
							return;
						}
						itCellRect.MoveNext();
					}
				}
			}
		}
	}
}
