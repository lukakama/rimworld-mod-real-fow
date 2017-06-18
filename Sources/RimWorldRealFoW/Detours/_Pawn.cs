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
using Verse;

namespace RimWorldRealFoW.Detours {
	static class _Pawn {
		public static void DrawGUIOverlay(this Pawn _this) {
			if (!_this.Spawned || _this.Map.fogGrid.IsFogged(_this.Position)) {
				return;
			}

			if (!_this.RaceProps.Humanlike) {
				switch (Prefs.AnimalNameMode) {
					case AnimalNameDisplayMode.None:
						return;
					case AnimalNameDisplayMode.TameNamed:
						if (_this.Name == null || _this.Name.Numerical) {
							return;
						}
						break;
					case AnimalNameDisplayMode.TameAll:
						if (_this.Name == null) {
							return;
						}
						break;
				}
			}


			if (!_this.fowIsVisible()) {
				return;
			}

			_this.Drawer.ui.DrawPawnGUIOverlay();
		}

	}
}
