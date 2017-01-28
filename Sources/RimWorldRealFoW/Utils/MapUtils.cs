using Verse;

namespace RimWorldRealFoW.Utils {
	public static class MapUtils {
		public static MapComponentSeenFog getMapComponentSeenFog(this Map map) {
			MapComponentSeenFog mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();

			// If still null, initialize it (old save).
			if (mapCompSeenFog == null) {
				mapCompSeenFog = new MapComponentSeenFog(map);
				map.components.Add(mapCompSeenFog);
			}

			return mapCompSeenFog;
		}
	}
}
