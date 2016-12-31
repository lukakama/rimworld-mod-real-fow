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
using Verse;

namespace RimWorldRealFoW {

	public static class _Selector {
		public static DetourHook hookSelect;

		public static void Select(this Selector _this, object obj, bool playSound = true, bool forceDesignatorDeselect = true) {
			Thing thing = obj as Thing;
			if (thing != null) {
				CompHiddenable comp = thing.TryGetComp<CompHiddenable>();
				if (comp != null && comp.hidden) {
					return;
				}
			}

			hookSelect.callOriginal(_this, new object[] { obj, playSound, forceDesignatorDeselect });
		}
	}
}
