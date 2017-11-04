using RimWorld;
using RimWorldRealFoW.Utils;

namespace RimWorldRealFoW.Detours {
	public static class _MoteBubble {
		public static bool Draw_Prefix(MoteBubble __instance) {
			if (__instance.link1.Linked && __instance.link1.Target != null && __instance.link1.Target.Thing != null) {
				return __instance.link1.Target.Thing.fowIsVisible();
			}
			return true;
		}
	}
}
