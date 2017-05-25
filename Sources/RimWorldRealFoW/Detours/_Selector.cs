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
using RimWorld.Planet;
using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimWorldRealFoW.Detours {

	public static class _Selector {
		public static void Select(this Selector _this, object obj, bool playSound = true, bool forceDesignatorDeselect = true) {
			if (obj == null) {
				Log.Error("Cannot select null.");
				return;
			}

			Thing thing = obj as Thing;
			if (thing == null && !(obj is Zone)) {
				Log.Error("Tried to select " + obj + " which is neither a Thing nor a Zone.");
				return;
			}
			if (thing != null && thing.Destroyed) {
				Log.Error("Cannot select destroyed thing.");
				return;
			}
			Pawn pawn = obj as Pawn;
			if (pawn != null && pawn.IsWorldPawn()) {
				Log.Error("Cannot select world pawns.");
				return;
			}


			Map map = (thing == null) ? ((Zone) obj).Map : thing.Map;
			if (thing != null && !thing.fowIsVisible()) {
				return;
			}


			if (forceDesignatorDeselect) {
				Find.DesignatorManager.Deselect();
			}
			if (_this.SelectedZone != null && !(obj is Zone)) {
				_this.ClearSelection();
			}
			if (obj is Zone && _this.SelectedZone == null) {
				_this.ClearSelection();
			}
			for (int i = ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected").Count - 1; i >= 0; i--) {
				Thing thing2 = ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected")[i] as Thing;
				Map map2 = (thing2 == null) ? ((Zone) ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected")[i]).Map : thing2.Map;
				if (map2 != map) {
					_this.Deselect(ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected")[i]);
				}
			}
			if (ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected").Count >= 80) {
				return;
			}
			if (!_this.IsSelected(obj)) {
				if (map != Current.Game.VisibleMap) {
					Current.Game.VisibleMap = map;
					SoundDefOf.MapSelected.PlayOneShotOnCamera(null);
					IntVec3 cell = (thing == null) ? ((Zone) obj).Cells[0] : thing.Position;
					Find.CameraDriver.JumpToVisibleMapLoc(cell);
				}
				if (playSound) {
					ReflectionUtils.execInstancePrivate(_this, "PlaySelectionSoundFor", obj);
				}
				ReflectionUtils.getInstancePrivateValue<List<object>>(_this, "selected").Add(obj);
				SelectionDrawer.Notify_Selected(obj);
			}
		}
	}
}

