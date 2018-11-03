#if InternalProfile
using RimWorldRealFoW.Utils;
using System.Linq;
using System.Text;
using Verse;
using static RimWorldRealFoW.Utils.ProfilingUtils;

namespace RimWorldRealFoW.Detours {
	class _EditWindow_DebugInspector {
		public static void CurrentDebugString_Postfix(ref string __result) {
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("---");
			stringBuilder.AppendLine("RealFoW Mod profiling");

			foreach (string code in ProfilingUtils.profileData.Keys.OrderBy(q => q).ToList()) {
				ProfileInfo pInfo = ProfilingUtils.profileData[code];
				stringBuilder.AppendLine("-" + code + ((pInfo.lastUpdadeTick == Find.TickManager.TicksGame) ? "#########" : ""));
				stringBuilder.AppendLine(" allocatedMemory: " + pInfo.allocatedMemory);
				if (pInfo.count > 0) {
					stringBuilder.AppendLine(" allocatedMemory (avg): " + (pInfo.allocatedMemory / pInfo.count));
				}
				stringBuilder.AppendLine(" cpuTicks: " + pInfo.cpuTicks);
				if (pInfo.count > 0) {
					stringBuilder.AppendLine(" cpuTicks (avg): " + pInfo.cpuTicks / pInfo.count);
				}
				stringBuilder.AppendLine(" count: " + pInfo.count);
				stringBuilder.AppendLine(" lastUpdadeTick: " + pInfo.lastUpdadeTick);
			}

			stringBuilder.Append(__result);

			__result = stringBuilder.ToString();
		}
	}
}
#endif