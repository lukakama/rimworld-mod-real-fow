using RimWorldRealFoW.Utils;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.Detours {
	public static class _HaulAIUtility {
		public static bool HaulToStorageJob_Prefix(Thing t, Job __result) {
			if (!t.fowIsVisible()) {
				__result = null;
				return false;
			}

			return true;
		}
	}
}
