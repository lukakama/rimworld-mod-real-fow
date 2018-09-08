using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _LetterStack {
		public static void ReceiveLetter_Prefix(ref LookTargets lookTargets) {
			// TODO: Handle multiple targets...
			if (lookTargets.PrimaryTarget.HasThing) {
				Thing thing = lookTargets.PrimaryTarget.Thing;
				if (thing.Faction == null || !thing.Faction.IsPlayer) {
					lookTargets = new GlobalTargetInfo(thing.Position, thing.Map);
				}
			}
		}
	}
}
