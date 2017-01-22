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
using RimWorld;
using RimWorldRealFoW.Utils;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW.Detours {
	static class _PawnUIOverlay {
		public static void DrawPawnGUIOverlay(this PawnUIOverlay _this) {
			Pawn pawn = ReflectionUtils.getInstancePrivateValue<Pawn>(_this, "pawn");
			if (!pawn.Spawned || pawn.Map.fogGrid.IsFogged(pawn.Position)) {
				return;
			}

			if (!pawn.fowIsVisible()) {
				return;
			}

			if (!pawn.RaceProps.Humanlike) {
				switch (Prefs.AnimalNameMode) {
					case AnimalNameDisplayMode.None:
						return;
					case AnimalNameDisplayMode.TameNamed:
						if (pawn.Name == null || pawn.Name.Numerical) {
							return;
						}
						break;
					case AnimalNameDisplayMode.TameAll:
						if (pawn.Name == null) {
							return;
						}
						break;
				}
			}
			Vector2 pos = GenMapUI.LabelDrawPosFor(pawn, -0.6f);
			GenMapUI.DrawPawnLabel(pawn, pos, 1f, 9999f, null, GameFont.Tiny, true, true);
			if (pawn.CanTradeNow) {
				pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
			}
		}
	}
}
