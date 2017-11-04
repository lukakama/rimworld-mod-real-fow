using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _GenMapUI {
		public static bool DrawThingLabel_Prefix(Thing thing) {
			return thing.fowIsVisible();
		}
	}
}
