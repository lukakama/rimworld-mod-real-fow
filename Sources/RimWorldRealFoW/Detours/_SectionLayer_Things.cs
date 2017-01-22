using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _SectionLayer_Things {
		public static void Regenerate(this SectionLayer_Things _this) {
			Utils.ReflectionUtils.execInstancePrivate(_this, "ClearSubMeshes", MeshParts.All);
			foreach (IntVec3 current in Utils.ReflectionUtils.getInstancePrivateValue<Section>(_this, "section").CellRect) {
				List<Thing> list = Utils.ReflectionUtils.getInstancePrivateProperty<Map>(_this, "Map").thingGrid.ThingsListAt(current);
				int count = list.Count;
				for (int i = 0; i < count; i++) {
					Thing thing = list[i];
					if (thing.def.drawerType != DrawerType.None) {
						if (thing.def.drawerType != DrawerType.RealtimeOnly || !Utils.ReflectionUtils.getInstancePrivateValue<bool>(_this, "requireAddToMapMesh")) {
							if (thing.def.hideAtSnowDepth >= 1f || Utils.ReflectionUtils.getInstancePrivateProperty<Map>(_this, "Map").snowGrid.GetDepth(thing.Position) <= thing.def.hideAtSnowDepth) {
								if (thing.Position.x == current.x && thing.Position.z == current.z) {
									if (thing.fowIsVisible(true)) {
										Utils.ReflectionUtils.execInstancePrivate(_this, "TakePrintFrom", thing);
									}
								}
							}
						}
					}
				}
			}
			Utils.ReflectionUtils.execInstancePrivate(_this, "FinalizeMesh", MeshParts.All);
		}
	}
}
