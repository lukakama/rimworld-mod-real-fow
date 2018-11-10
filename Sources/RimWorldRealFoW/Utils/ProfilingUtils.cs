#if InternalProfile
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;
using Verse;

namespace RimWorldRealFoW.Utils {
	[StaticConstructorOnStartup]
	class ProfilingUtils {
		public class ProfileInfo {
			public long lastUpdadeTick = 0;
			public long count = 0;

			public long cpuTicksStart = 0;
			public long cpuTicks = 0;

			public long allocatedMemoryStart = 0;
			public long allocatedMemory = 0;

			public ProfileInfo parentProfiling = null;
			public long profilingCpuTicks = 0;
		}

		static Stopwatch profilingStopwatch = new Stopwatch();
		static ProfileInfo lastProfileInfo = null;

		public static Dictionary<string, ProfileInfo> profileData = new Dictionary<string, ProfileInfo>(100);
		static ProfilingUtils() {
			profilingStopwatch.Start();
		}

		public static void startProfiling(string code) {
			long profilingTicks = profilingStopwatch.ElapsedTicks;

			int currentTick = Find.TickManager.TicksGame;

			ProfileInfo profileInfo;
			if (!profileData.ContainsKey(code)) {
				profileInfo = profileData[code] = new ProfileInfo();
			} else {
				profileInfo = profileData[code];
			}

			if (currentTick != profileInfo.lastUpdadeTick) {
				profileInfo.lastUpdadeTick = currentTick;
				profileInfo.allocatedMemoryStart = 0;
				profileInfo.allocatedMemory = 0;
				profileInfo.cpuTicksStart = 0;
				profileInfo.cpuTicks = 0;
				profileInfo.count = 0;
			}


			if (profileInfo.allocatedMemoryStart != 0) {
				Log.Warning("Nested profiling for: " + code);
			}

			profileInfo.parentProfiling = lastProfileInfo;
			
			profileInfo.allocatedMemoryStart = Profiler.GetTotalAllocatedMemoryLong();
			profileInfo.cpuTicksStart = profilingTicks;

			lastProfileInfo = profileInfo;

			profileInfo.profilingCpuTicks = profilingStopwatch.ElapsedTicks - profilingTicks;
		}

		public static void stopProfiling(string code) {
			long profilingTicks = profilingStopwatch.ElapsedTicks;

			ProfileInfo profileInfo = profileData[code];

			profileInfo.allocatedMemory += Profiler.GetTotalAllocatedMemoryLong() - profileInfo.allocatedMemoryStart;
			profileInfo.cpuTicks += (profilingTicks - profileInfo.cpuTicksStart) - profileInfo.profilingCpuTicks;

			profileInfo.count++;

			profileInfo.allocatedMemoryStart = 0;
			profileInfo.cpuTicksStart = 0;

			lastProfileInfo = profileInfo.parentProfiling;
			if (lastProfileInfo != null) {
				lastProfileInfo.profilingCpuTicks += (profilingStopwatch.ElapsedTicks - profilingTicks) + profileInfo.profilingCpuTicks;
			}
		}
	}
}
#endif