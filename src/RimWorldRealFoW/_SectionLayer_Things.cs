using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW {
	public static class _SectionLayer_Things {
		public static void Regenerate(this SectionLayer_Things _this) {
			Utils.execInstancePrivate(_this, "ClearSubMeshes", MeshParts.All);
			foreach (IntVec3 current in Utils.getInstancePrivateValue<Section>(_this, "section").CellRect) {
				List<Thing> list = Utils.getInstancePrivateProperty<Map>(_this, "Map").thingGrid.ThingsListAt(current);
				int count = list.Count;
				for (int i = 0; i < count; i++) {
					Thing thing = list[i];
					if (thing.def.drawerType != DrawerType.None) {
						if (thing.def.drawerType != DrawerType.RealtimeOnly || !Utils.getInstancePrivateValue<bool>(_this, "requireAddToMapMesh")) {
							if (thing.def.hideAtSnowDepth >= 1f || Utils.getInstancePrivateProperty<Map>(_this, "Map").snowGrid.GetDepth(thing.Position) <= thing.def.hideAtSnowDepth) {
								if (thing.Position.x == current.x && thing.Position.z == current.z) {
									if (thing.fowIsVisible(true)) {
										Utils.execInstancePrivate(_this, "TakePrintFrom", thing);
									}
								}
							}
						}
					}
				}
			}
			Utils.execInstancePrivate(_this, "FinalizeMesh", MeshParts.All);
		}
	}
}
