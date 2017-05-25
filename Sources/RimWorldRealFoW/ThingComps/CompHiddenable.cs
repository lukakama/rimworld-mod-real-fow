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
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompHiddenable : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompHiddenable));

		private IntVec3 lastPosition = IntVec3.Invalid;

		public bool hidden = false;

		private Map map = null;

		private MapComponentSeenFog mapComp = null;

		public void hide() {
			if (!hidden) {
				hidden = true;

				if (parent.def.drawerType != DrawerType.MapMeshOnly) {
					parent.Map.dynamicDrawManager.DeRegisterDrawable(parent);
				}
				if (parent.def.hasTooltip) {
					parent.Map.tooltipGiverList.Notify_ThingDespawned(parent);
				}

				Selector selector = Find.Selector;
				if (selector.IsSelected(parent)) {
					selector.Deselect(parent);
				}

				// Mark everything to be updated
				updateMeshes();
			}
		}

		public void show() {
			if (hidden) {
				hidden = false;

				if (parent.def.drawerType != DrawerType.MapMeshOnly) {
					parent.Map.dynamicDrawManager.RegisterDrawable(parent);
				}
				if (parent.def.hasTooltip) {
					parent.Map.tooltipGiverList.Notify_ThingSpawned(parent);
				}

				// Mark everything to be updated
				updateMeshes();
			}
		}

		private void updateMeshes() {
			if (map != parent.Map) {
				map = parent.Map;
				mapComp = map.getMapComponentSeenFog();
			}

			// Update meshes only if the map has been already initialized (sometime this is called when the map drawer isn't initialized yet).
			if (mapComp != null && mapComp.initialized) {
				MapMeshFlag allFlags = MapMeshFlag.Buildings | MapMeshFlag.BuildingsDamage | MapMeshFlag.GroundGlow | MapMeshFlag.PowerGrid | MapMeshFlag.Roofs | MapMeshFlag.Snow | MapMeshFlag.Terrain | MapMeshFlag.Things | MapMeshFlag.Zone;
				foreach (IntVec3 cell in parent.OccupiedRect().Cells) {
					if (cell.InBounds(map)) {
						map.mapDrawer.MapMeshDirty(cell, allFlags, false, false);
					}
				}
			}
		}
	}
}
