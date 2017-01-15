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
	class CompHideFromPlayer : ThingComp {
		private bool calculated;
		private IntVec3 lastPosition;
		
		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private CompHiddenable compHiddenable;

		private bool setupDone = false;
		private bool seenByPlayer;
		private Pawn pawn;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			setupDone = true;

			calculated = false;
			lastPosition = IntVec3.Invalid;

			pawn = parent as Pawn;

			compHiddenable = parent.GetComp<CompHiddenable>();

			updateVisibility(false);
		}

		public override void PostExposeData() {
			base.PostExposeData();

			Scribe_Values.LookValue<bool>(ref this.seenByPlayer, "seenByPlayer", false, false);
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			updateVisibility(false);
		}

		public override void CompTick() {
			base.CompTick();

			// Check every 25 thick.
			if (Find.TickManager.TicksGame % 25 == 0) {
				updateVisibility(false);
			}
		}

		public void forceSeen() {
			seenByPlayer = true;

			updateVisibility(true, true);
		}

		public void updateVisibility(bool forceCheck, bool forceUpdate = false) {
			if (!setupDone || Current.ProgramState == ProgramState.MapInitializing) {
				return;
			}

			Thing thing = base.parent;

			if (thing != null && thing.Spawned && thing.Map != null && thing.Position != IntVec3.Invalid) {
				if (map != thing.Map) {
					map = thing.Map;
					mapCompSeenFog = thing.Map.GetComponent<MapComponentSeenFog>();

				} else if (mapCompSeenFog == null) {
					mapCompSeenFog = thing.Map.GetComponent<MapComponentSeenFog>();
				}

				if (mapCompSeenFog == null) {
					return;
				}
				
				if (forceCheck || !calculated || thing.Position != lastPosition) {
					calculated = true;
					lastPosition = thing.Position;

					bool belongToPlayer = thing.Faction != null && thing.Faction.IsPlayer;

					if (mapCompSeenFog != null && !map.fogGrid.IsFogged(thing.Position)) {
						if (thing.def.isSaveable && !thing.def.saveCompressible) {
							if (!belongToPlayer) {
								if (pawn != null && !hasPartShownToPlayer()) {
									compHiddenable.hide();
								} else if (pawn == null && !seenByPlayer && !hasPartShownToPlayer()) {
									compHiddenable.hide();
								} else {
									seenByPlayer = true;
									compHiddenable.show();
								}
							} else {
								seenByPlayer = true;
								compHiddenable.show();
							}
						} else if ((forceUpdate || !seenByPlayer) && thing.fowInKnownCell()) {
							seenByPlayer = true;
							compHiddenable.show();
						}
					}
				}
			}
		}

		private bool hasPartShownToPlayer() {
			Faction playerFaction = Faction.OfPlayer;
			foreach (IntVec3 cell in parent.OccupiedRect().Cells) {
				if (mapCompSeenFog.isShown(playerFaction, cell)) {
					return true;
				}
			}
			return false;
		}
	}
}
