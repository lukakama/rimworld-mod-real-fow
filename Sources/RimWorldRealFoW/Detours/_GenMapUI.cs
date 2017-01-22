using RimWorldRealFoW.Utils;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _GenMapUI {
		public static void DrawThingLabel(Thing thing, string text, Color textColor) {
			if (thing.fowIsVisible()) {
				GenMapUI.DrawThingLabel(GenMapUI.LabelDrawPosFor(thing, -0.4f), text, textColor);
			}
		}
	}
}
