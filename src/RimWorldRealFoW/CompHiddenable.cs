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
	class CompHiddenable : ThingComp {
		private IntVec3 lastPosition = IntVec3.Invalid;

		public bool hidden = false;

		public void hide() {
			if (!hidden) {
				hidden = true;

				if (parent.def.drawerType != DrawerType.MapMeshOnly) {
					parent.Map.dynamicDrawManager.DeRegisterDrawable(parent);
				}
				if (parent.def.hasTooltip) {
					parent.Map.tooltipGiverList.DeregisterTooltipGiver(parent);
				}
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);

				Selector selector = Find.Selector;
				if (selector.IsSelected(parent)) {
					selector.Deselect(parent);
				}
			}
		}

		public void show() {
			if (hidden) {
				hidden = false;

				if (parent.def.drawerType != DrawerType.MapMeshOnly) {
					parent.Map.dynamicDrawManager.RegisterDrawable(parent);
				}
				if (parent.def.hasTooltip) {
					parent.Map.tooltipGiverList.RegisterTooltipGiver(parent);
				}
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);

			}
		}
	}

}
