using RimWorld.Planet;
using System;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _Messages {
		public static void Message(string text, GlobalTargetInfo lookTarget, MessageSound sound) {
			if (!Utils.ReflectionUtils.execStaticPrivate<bool>(typeof(Messages), "AcceptsMessage", text, lookTarget)) {
				return;
			}
			if (lookTarget.HasThing) {
				Thing thing = lookTarget.Thing;
				if (thing.Faction == null || !thing.Faction.IsPlayer) {
					lookTarget = new GlobalTargetInfo(thing.Position, thing.Map);
				}
			}

			object msg = Activator.CreateInstance(typeof(Messages).Assembly.GetType("Verse.Messages+LiveMessage"), text, lookTarget);
			Utils.ReflectionUtils.execStaticPrivate(typeof(Messages), "Message", msg, sound);
		}
	}
}
