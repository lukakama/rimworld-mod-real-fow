using RimWorld;
using RimWorldRealFoW.ThingComps;
using RimWorldRealFoW.ThingComps.ThingSubComps;
using Verse;

namespace RimWorldRealFoW.Utils {
	public static class FoWThingUtils {
		public static bool fowIsVisible(this Thing _this, bool forRender = false) {
			if (_this.Spawned) {
				if (_this.def.isSaveable && !_this.def.saveCompressible) {
					CompHiddenable comp = _this.TryGetCompHiddenable();
					if (comp != null) {
						return !comp.hidden;
					}
				}
				return forRender || (_this.Map != null && _this.fowInKnownCell());
			}
			return true;
		}

		private static bool fowInKnownCell(this Thing _this) {
#if InternalProfile
			ProfilingUtils.startProfiling("FoWThingUtils.fowInKnownCell");
			try {
#endif
			MapComponentSeenFog mapComponent = _this.Map.getMapComponentSeenFog();
			if (mapComponent != null) {
				bool[] knownCells = mapComponent.knownCells;
				int mapSizeX = mapComponent.mapSizeX;
				IntVec3 position = _this.Position;

				IntVec2 size = _this.def.size;
				if (size.x == 1 && size.z == 1) {
					return mapComponent.knownCells[(position.z * mapSizeX) + position.x];
				} else {
					CellRect occupiedRect = GenAdj.OccupiedRect(position, _this.Rotation, size);

					for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
						for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
							if (mapComponent.knownCells[(z * mapSizeX) + x]) {
								return true;
							}
						}
					}
				}

				return false;
			}

			return true;
#if InternalProfile
			} finally {
				ProfilingUtils.stopProfiling("FoWThingUtils.fowInKnownCell");
			}
#endif
		}

		public static ThingComp TryGetComp(this Thing _this, CompProperties def) {
			ThingCategory thingCategory = _this.def.category;
			if (thingCategory == ThingCategory.Pawn ||
					thingCategory == ThingCategory.Building ||
					thingCategory == ThingCategory.Item ||
					thingCategory == ThingCategory.Filth ||
					thingCategory == ThingCategory.Gas ||
					_this.def.IsBlueprint) {

				ThingWithComps thingWithComps = _this as ThingWithComps;
				if (thingWithComps != null) {
					return thingWithComps.GetCompByDef(def);
				}
			}

			return null;
		}

		public static CompHiddenable TryGetCompHiddenable(this Thing _this) {
			CompMainComponent mainComp = (CompMainComponent)_this.TryGetComp(CompMainComponent.COMP_DEF);
			if (mainComp != null) {
				return mainComp.compHiddenable;
			}

			return null;
		}
	}
}
