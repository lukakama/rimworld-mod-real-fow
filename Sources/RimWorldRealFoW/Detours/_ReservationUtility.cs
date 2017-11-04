using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _ReservationUtility {
		public static void CanReserve_Postfix(this Pawn p, ref bool __result, LocalTargetInfo target) {
			if (__result && p.Faction != null && p.Faction.IsPlayer && target.HasThing && target.Thing.def.category != ThingCategory.Pawn) {
				__result = target.Thing.fowIsVisible();
			}
		}

		public static void  CanReserveAndReach_Postfix(this Pawn p, bool __result, LocalTargetInfo target) {
			if (__result && p.Faction != null && p.Faction.IsPlayer && target.HasThing && target.Thing.def.category != ThingCategory.Pawn) {
				__result = target.Thing.fowIsVisible();
			}
		}
	}
}
