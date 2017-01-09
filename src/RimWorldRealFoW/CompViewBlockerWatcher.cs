using Verse;

namespace RimWorldRealFoW {
	public class CompViewBlockerWatcher : ThingComp {
		public override void PostSpawnSetup() {
			base.PostSpawnSetup();
			if (Current.ProgramState == ProgramState.Playing) {
				// When spawn, if the thing si blocking the view, then trigger a FoV update for everything on the map (if any).
				if (parent.Map != null) {
					Building b = parent as Building;
					if (b != null && !b.CanBeSeenOver()) {
						foreach (Thing thing in parent.Map.listerThings.AllThings) {
							CompFieldOfView cmpFov = thing.TryGetComp<CompFieldOfView>();
							if (cmpFov != null) {
								// TODO: Limit only to things in range.
								cmpFov.updateFoV(true);
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
					foreach (Thing thing in map.listerThings.AllThings) {
						CompFieldOfView cmpFov = thing.TryGetComp<CompFieldOfView>();
						if (cmpFov != null) {
							// TODO: Limit only to things in range.
							cmpFov.updateFoV(true);
						}
					}
				}
			}
		}
	}
}
