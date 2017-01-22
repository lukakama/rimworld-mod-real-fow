using System.Collections.Generic;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompViewBlockerWatcher : ThingComp {
		public override void PostSpawnSetup() {
			base.PostSpawnSetup();
			if (Current.ProgramState == ProgramState.Playing) {
				// When spawn, if the thing si blocking the view, then trigger a FoV update for everything on the map (if any).
				if (parent.Map != null) {
					Building b = parent as Building;
					if (b != null && !b.CanBeSeenOver()) {
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

			if (Current.ProgramState == ProgramState.Playing) {
				// When de-spawn, if the thing was blocking the view, then trigger a FoV update for everything on the map.
				Building b = parent as Building;
				if (b != null && !b.CanBeSeenOver()) {
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
