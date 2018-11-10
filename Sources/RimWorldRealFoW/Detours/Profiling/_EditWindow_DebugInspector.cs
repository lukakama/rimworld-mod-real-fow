#if InternalProfile
using RimWorldRealFoW.Utils;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using static RimWorldRealFoW.Utils.ProfilingUtils;

namespace RimWorldRealFoW.Detours.Profiling {
	class _EditWindow_DebugInspector {
		public static void CurrentDebugString_Postfix(ref string __result) {
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("---");
			stringBuilder.AppendLine("RealFoW Mod profiling");
			stringBuilder.AppendLine("Freq: " + Stopwatch.Frequency + "(HiRes: " + Stopwatch.IsHighResolution + ")");

			foreach (string code in ProfilingUtils.profileData.Keys.OrderBy(q => q).ToList()) {
				ProfileInfo pInfo = ProfilingUtils.profileData[code];
				stringBuilder.AppendLine("-" + code + ((pInfo.lastUpdadeTick == Find.TickManager.TicksGame) ? "#########" : ""));
				stringBuilder.AppendLine(" allocatedMemory: " + pInfo.allocatedMemory);
				if (pInfo.count > 0) {
					stringBuilder.AppendLine(" allocatedMemory (avg): " + (pInfo.allocatedMemory / pInfo.count));
				}
				stringBuilder.AppendLine(" cpuTicks: " + pInfo.cpuTicks + " (" + (((float) pInfo.cpuTicks / Stopwatch.Frequency) * 1000).ToString("F3") +  " ms)");
				if (pInfo.count > 0) {
					stringBuilder.AppendLine(" cpuTicks (avg): " + ((float)pInfo.cpuTicks / pInfo.count).ToString("F2") + " (" + ((((float)pInfo.cpuTicks / pInfo.count) / Stopwatch.Frequency) * 1000).ToString("F3") + " ms)");
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