using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _LetterStack {
		public static void ReceiveLetter_Prefix(ref GlobalTargetInfo lookTarget) {
			if (lookTarget.HasThing) {
				Thing thing = lookTarget.Thing;
				if (thing.Faction == null || !thing.Faction.IsPlayer) {
					lookTarget = new GlobalTargetInfo(thing.Position, thing.Map);
				}
			}
		}
	}
}
