using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Messages {
		public static void Message_Prefix(string text, ref LookTargets lookTargets) {
			if (Traverse.Create(typeof(Messages)).Method("AcceptsMessage", text, lookTargets).GetValue<bool>()) { 
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
}
