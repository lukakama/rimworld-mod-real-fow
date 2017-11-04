using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _SectionLayer_ThingsGeneral {
		public static bool TakePrintFrom_Prefix(Thing t) {
			return t.fowIsVisible(true);
		}
	}
}
