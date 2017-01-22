using RimWorld.Planet;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _LetterStack {
		public static void ReceiveLetter(this LetterStack _this, string label, string text, LetterType type, GlobalTargetInfo letterLookTarget, string debugText = null) {
			if (letterLookTarget.HasThing) {
				Thing thing = letterLookTarget.Thing;
				if (thing.Faction == null || !thing.Faction.IsPlayer) {
					letterLookTarget = new GlobalTargetInfo(thing.Position, thing.Map);
				}
			}

			Letter let = new Letter(label, text, type, letterLookTarget);
			_this.ReceiveLetter(let, debugText);
		}
	}
}
