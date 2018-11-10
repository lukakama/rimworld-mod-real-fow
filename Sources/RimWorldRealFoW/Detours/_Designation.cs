using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Designation {

		public static void Notify_Added_Postfix(ref Designation __instance) {
			if (__instance.def == DesignationDefOf.Mine && !__instance.target.HasThing) {
				MapComponentSeenFog mapCmq = __instance.designationManager.map.getMapComponentSeenFog();
				if (mapCmq != null) {
					mapCmq.registerMineDesignation(__instance);
				}
			}
		}

		public static void Notify_Removing_Postfix(ref Designation __instance) {
			if (__instance.def == DesignationDefOf.Mine && !__instance.target.HasThing) {
				MapComponentSeenFog mapCmq = __instance.designationManager.map.getMapComponentSeenFog();
				if (mapCmq != null) {
					mapCmq.deregisterMineDesignation(__instance);
				}
			}
		}
	}
}
