using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Messages {
		public static void Message_Prefix(string text, ref GlobalTargetInfo lookTarget) {
			if (Utils.ReflectionUtils.execStaticPrivate<bool>(typeof(Messages), "AcceptsMessage", text, lookTarget)) {
				if (lookTarget.HasThing) {
					Thing thing = lookTarget.Thing;
					if (thing.Faction == null || !thing.Faction.IsPlayer) {
						lookTarget = new GlobalTargetInfo(thing.Position, thing.Map);
					}
				}
			}
		}
	}
}
