using Verse;

namespace RimWorldRealFoW {
	public static class CompHiddenableUtils {
		public static bool isVisible(this Thing _this) {
			CompHiddenable comp = _this.TryGetComp<CompHiddenable>();
			return comp == null || !comp.hidden;
		}
	}
}
