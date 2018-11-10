#if InternalProfile

using RimWorldRealFoW.Utils;

namespace RimWorldRealFoW.Detours.Profiling {
	class _TickManager {
		public static void DoSingleTick_Prefix() {
			ProfilingUtils.startProfiling("1-DoSingleTick");
		}
		public static void DoSingleTick_Postfix() {
			ProfilingUtils.stopProfiling("1-DoSingleTick");
		}
	}
}
#endif