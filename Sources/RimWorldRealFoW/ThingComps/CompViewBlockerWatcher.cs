using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompViewBlockerWatcher : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompViewBlockerWatcher));

		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private bool viewBloker;
		private Building b;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			b = parent as Building;

			if (b != null && !b.CanBeSeenOver()) {
				viewBloker = true;

				map = parent.Map;
				mapCompSeenFog = map.GetComponent<MapComponentSeenFog>();

				bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;
				
				int mapSizeZ = map.Size.z;
				CellRect occupiedRect = b.OccupiedRect();
				for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
					for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
						viewBlockerCells[(z * mapSizeZ) + x] = true;
					}
				}

				if (Current.ProgramState == ProgramState.Playing) {
					// When spawn, if the thing si blocking the view, then trigger a FoV update for everything on the map (if any).
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

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);
			if (viewBloker) {
				bool[] viewBlockerCells = mapCompSeenFog.viewBlockerCells;

				int mapSizeZ = map.Size.z;
				CellRect occupiedRect = b.OccupiedRect();
				for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
					for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
						viewBlockerCells[(z * mapSizeZ) + x] = false;
					}
				}

				if (Current.ProgramState == ProgramState.Playing) {
					// When de-spawn, if the thing was blocking the view, then trigger a FoV update for everything on the map.
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
}
