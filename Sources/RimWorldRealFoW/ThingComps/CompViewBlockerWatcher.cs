using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompViewBlockerWatcher : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompViewBlockerWatcher));

		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private bool lastCanBeSeenOver;
		private Building b;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			b = parent as Building;

			if (b != null) {
				// Default as see-through.
				lastCanBeSeenOver = true;

				updateViewBlocker();
			}
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			if (b != null) {
				updateViewBlocker();
			}
		}

		public override void CompTick() {
			base.CompTick();

			if (b != null) {
				updateViewBlocker();
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			// When de-spawn, if the thing was blocking the view, then trigger a FoV update for near objects.
			if (b != null && !lastCanBeSeenOver) {
				bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;

				int mapSizeZ = map.Size.z;
				CellRect occupiedRect = b.OccupiedRect();
				for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
					for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
						viewBlockerCells[(z * mapSizeZ) + x] = false;
					}
				}

				if (Current.ProgramState == ProgramState.Playing) {
					if (parent.Map != null) {
						List<Thing> things = map.listerThings.AllThings;
						for (int i = 0; i < things.Count; i++) {
							ThingWithComps thing = things[i] as ThingWithComps;
							if (thing != null) {
								CompFieldOfViewWatcher cmpFov = thing.GetComp<CompFieldOfViewWatcher>();
								if (cmpFov != null && b.Position.InHorDistOf(thing.Position, cmpFov.sightRange)) {
									cmpFov.updateFoV(true);
								}
							}
						}
					}
				}
			}
		}

		private void updateViewBlocker() {
			if (lastCanBeSeenOver != b.CanBeSeenOver()) {
				lastCanBeSeenOver = b.CanBeSeenOver();

				map = parent.Map;
				mapCompSeenFog = map.getMapComponentSeenFog();

				bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;

				int mapSizeZ = map.Size.z;
				CellRect occupiedRect = b.OccupiedRect();
				for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
					for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
						viewBlockerCells[(z * mapSizeZ) + x] = !b.CanBeSeenOver();
					}
				}

				if (Current.ProgramState == ProgramState.Playing) {
					if (parent.Map != null) {
						List<Thing> things = parent.Map.listerThings.AllThings;
						for (int i = 0; i < things.Count; i++) {
							ThingWithComps thing = things[i] as ThingWithComps;
							if (thing != null) {
								CompFieldOfViewWatcher cmpFov = thing.GetComp<CompFieldOfViewWatcher>();
								if (cmpFov != null && b.Position.InHorDistOf(thing.Position, cmpFov.sightRange)) {
									cmpFov.updateFoV(true);
								}
							}
						}
					}
				}
			}
		}
	}
}
