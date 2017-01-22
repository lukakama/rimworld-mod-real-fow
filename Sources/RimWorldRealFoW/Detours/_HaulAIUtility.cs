using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.Detours {
	public static class _HaulAIUtility {
		public static Job HaulToStorageJob(Pawn p, Thing t) {
			if (!t.fowIsVisible()) {
				return null;
			}

			StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(t.Position, t);
			IntVec3 storeCell;
			if (!StoreUtility.TryFindBestBetterStoreCellFor(t, p, p.Map, currentPriority, p.Faction, out storeCell, true)) {
				JobFailReason.Is(ReflectionUtils.getInstancePrivateValue<string>(typeof(HaulAIUtility), "NoEmptyPlaceLowerTrans"));
				return null;
			}
			return HaulAIUtility.HaulMaxNumToCellJob(p, t, storeCell, false);
		}
	}
}
