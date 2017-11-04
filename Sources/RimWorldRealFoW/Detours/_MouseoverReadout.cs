//   Copyright 2017 Luca De Petrillo
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _MouseoverReadout {
		private static readonly Vector2 BotLeft = new Vector2(15f, 65f);

		public static bool MouseoverReadoutOnGUI_Prefix(MouseoverReadout __instance) {
			if (Event.current.type != EventType.Repaint) {
				return true;
			}
			if (Find.MainTabsRoot.OpenTab != null) {
				return true;
			}
			IntVec3 c = UI.MouseCell();
			if (!c.InBounds(Find.VisibleMap)) {
				return true;
			}

			MapComponentSeenFog seenFog = Find.VisibleMap.getMapComponentSeenFog();
			if (!c.Fogged(Find.VisibleMap) && (seenFog != null && !seenFog.knownCells[Find.VisibleMap.cellIndices.CellToIndex(c)])) {
				GenUI.DrawTextWinterShadow(new Rect(256f, (float)(UI.screenHeight - 256), -256f, 256f));
				Text.Font = GameFont.Small;
				GUI.color = new Color(1f, 1f, 1f, 0.8f);

				Rect rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y, 999f, 999f);
				Widgets.Label(rect, "NotVisible".Translate());
				GUI.color = Color.white;
				return false;
			}

			return true;
		}
	}
}
