using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompViewBlockerWatcher : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompViewBlockerWatcher));

		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private bool lastIsViewBlocker = false;

		private bool blockLight = false;

		private Building b = null;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			blockLight = parent.def.blockLight;

			b = parent as Building;

			if (blockLight && (b != null)) {
				updateIsViewBlocker();
			}
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			if (blockLight && (b != null)) {
				updateIsViewBlocker();
			}
		}

		public override void CompTick() {
			base.CompTick();

			if (blockLight && (b != null) && (Find.TickManager.TicksGame % 30 == 0)) {
				updateIsViewBlocker();
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			// When de-spawn, if the thing was blocking the view, then trigger a FoV update for near objects.
			if (lastIsViewBlocker) {
				if (this.map != map) {
					this.map = map;
					mapCompSeenFog = map.getMapComponentSeenFog();
				}

				updateViewBlockerCells(false);
			}
		}

		private void updateIsViewBlocker() {
			bool isViewBlocker = blockLight && !b.CanBeSeenOver();

			if (lastIsViewBlocker != isViewBlocker) {
				lastIsViewBlocker = isViewBlocker;

				if (map != parent.Map) {
					map = parent.Map;
					mapCompSeenFog = map.getMapComponentSeenFog();
				}

				updateViewBlockerCells(isViewBlocker);
			}
		}

		private void updateViewBlockerCells(bool blockView) {
			bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;

			int mapSizeZ = map.Size.z;
			int mapSizeX = map.Size.x;

			CellRect occupiedRect = parent.OccupiedRect();
			for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
				for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
					if (x >= 0 && z >= 0 && x <= mapSizeX && z <= mapSizeZ) {
						viewBlockerCells[(z * mapSizeZ) + x] = blockView;
					}
				}
			}

			if (Current.ProgramState == ProgramState.Playing) {
				if (map != null) {
					List<Thing> things = map.listerThings.AllThings;
					for (int i = 0; i < things.Count; i++) {
						ThingWithComps thing = things[i] as ThingWithComps;
						if (thing != null) {
							CompFieldOfViewWatcher cmpFov = (CompFieldOfViewWatcher) thing.TryGetComp(CompFieldOfViewWatcher.COMP_DEF);
							if (cmpFov != null && parent.Position.InHorDistOf(thing.Position, cmpFov.sightRange)) {
								cmpFov.updateFoV(true);
							}
						}
					}
				}
			}
		}
	}
}
