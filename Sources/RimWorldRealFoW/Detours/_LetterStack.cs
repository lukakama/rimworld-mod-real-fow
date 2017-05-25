using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _LetterStack {
		public static void ReceiveLetter(this LetterStack _this, string label, string text, LetterDef textLetterDef, GlobalTargetInfo lookTarget, string debugInfo = null) {
			if (lookTarget.HasThing) {
				Thing thing = lookTarget.Thing;
				if (thing.Faction == null || !thing.Faction.IsPlayer) {
					lookTarget = new GlobalTargetInfo(thing.Position, thing.Map);
				}
			}

			ChoiceLetter let = LetterMaker.MakeLetter(label, text, textLetterDef, lookTarget);
			_this.ReceiveLetter(let, debugInfo);
		}
	}
}
