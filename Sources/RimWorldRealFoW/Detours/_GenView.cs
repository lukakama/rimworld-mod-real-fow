using RimWorld;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _GenView {
		private static MapComponentSeenFog lastUsedMapComponent;
		private static Map lastUsedMap;

		public static void ShouldSpawnMotesAt_Postfix(IntVec3 loc, Map map, ref bool __result)
		{
			if (!__result) return;
			// Cache map component for fast repeated retrieval
			var comp = lastUsedMapComponent;
			if (map != lastUsedMap)
			{
				lastUsedMap = map;
				comp = lastUsedMapComponent = map.GetComponent<MapComponentSeenFog>();
			}

			__result = comp == null || comp.isShown(Faction.OfPlayer, loc.x, loc.z);
		}
	}
}
