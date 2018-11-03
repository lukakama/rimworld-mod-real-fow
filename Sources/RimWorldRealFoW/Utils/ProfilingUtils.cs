#if InternalProfile
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;
using Verse;

namespace RimWorldRealFoW.Utils {
	class ProfilingUtils {
		public class ProfileInfo {
			public long lastUpdadeTick = 0;
			public long count = 0;

			public Stopwatch cpuTicksStopwatch = new Stopwatch();
			public long cpuTicks;

			public long allocatedMemoryStart = 0;
			public long allocatedMemory = 0;
		}

		public static Dictionary<string, ProfileInfo> profileData = new Dictionary<string, ProfileInfo>(100);

		public static void startProfiling(string code) {

			int currentTick = Find.TickManager.TicksGame;

			ProfileInfo profileInfo;
			if (!profileData.ContainsKey(code)) {
				profileInfo = new ProfileInfo();

				profileData[code] = profileInfo;
			} else {
				profileInfo = profileData[code];
			}

			if (currentTick != profileInfo.lastUpdadeTick) {
				profileInfo.lastUpdadeTick = currentTick;

				profileInfo.allocatedMemoryStart = 0;
				profileInfo.allocatedMemory = 0;
				profileInfo.cpuTicksStopwatch.Reset();
				profileInfo.cpuTicks = 0;
				profileInfo.count = 0;
			}


			if (profileInfo.allocatedMemoryStart != 0) {
				Log.Warning("Nested profiling for: " + code);
			}

			profileInfo.allocatedMemoryStart = Profiler.GetTotalAllocatedMemoryLong();
			profileInfo.cpuTicksStopwatch.Start();
		}

		public static void stopProfiling(string code) {
			ProfileInfo profileInfo = profileData[code];

			profileInfo.allocatedMemory += Profiler.GetTotalAllocatedMemoryLong() - profileInfo.allocatedMemoryStart;


			profileInfo.cpuTicksStopwatch.Stop();
			profileInfo.cpuTicks += profileInfo.cpuTicksStopwatch.ElapsedTicks;

			profileInfo.count++;

			profileInfo.allocatedMemoryStart = 0;
			profileInfo.cpuTicksStopwatch.Reset();

			//activeProfilers.Remove(code);
		}

		public static void recordPrevProfiling(string code) {
			if (profileData.ContainsKey(code)) {
				string codeLast = code + "-last";

				ProfileInfo profileInfo = profileData[code];
				ProfileInfo lastProfileInfo;
				if (!profileData.ContainsKey(codeLast)) {
					lastProfileInfo = new ProfileInfo();

					profileData[codeLast] = lastProfileInfo;
				} else {
					lastProfileInfo = profileData[codeLast];
				}

				lastProfileInfo.lastUpdadeTick = profileInfo.lastUpdadeTick;

				lastProfileInfo.allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - profileInfo.allocatedMemoryStart;

				profileInfo.cpuTicksStopwatch.Stop();
				lastProfileInfo.cpuTicks = profileInfo.cpuTicksStopwatch.ElapsedTicks;

				profileInfo.allocatedMemoryStart = 0;
				profileInfo.cpuTicksStopwatch.Reset();

			}
		}
	}
}
#endif