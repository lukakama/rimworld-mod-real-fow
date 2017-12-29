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
using RimWorldRealFoW.ThingComps.ThingSubComps;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompMainComponent : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompMainComponent));

		private bool setup = false;

		public CompComponentsPositionTracker compComponentsPositionTracker = null;
		public CompFieldOfViewWatcher compFieldOfViewWatcher = null;
		public CompHiddenable compHiddenable = null;
		public CompHideFromPlayer compHideFromPlayer = null;
		public CompViewBlockerWatcher compViewBlockerWatcher = null;

		private void performSetup() {
			if (!setup) {
				setup = true;

				// Init sub-components.
				ThingCategory thingCategory = parent.def.category;

				compComponentsPositionTracker = new CompComponentsPositionTracker();
				compComponentsPositionTracker.parent = parent;
				compComponentsPositionTracker.mainComponent = this;

				compHiddenable = new CompHiddenable();
				compHiddenable.parent = parent;
				compHiddenable.mainComponent = this;

				compHideFromPlayer = new CompHideFromPlayer();
				compHideFromPlayer.parent = parent;
				compHideFromPlayer.mainComponent = this;

				if (thingCategory == ThingCategory.Building) {
					compViewBlockerWatcher = new CompViewBlockerWatcher();
					compViewBlockerWatcher.parent = parent;
					compViewBlockerWatcher.mainComponent = this;
				}
				if (thingCategory == ThingCategory.Pawn ||
						thingCategory == ThingCategory.Building) {
					compFieldOfViewWatcher = new CompFieldOfViewWatcher();
					compFieldOfViewWatcher.parent = parent;
					compFieldOfViewWatcher.mainComponent = this;
				}
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);

			if (!setup) {
				performSetup();
			}

			compComponentsPositionTracker.PostSpawnSetup(false);
			compHiddenable.PostSpawnSetup(false);
			compHideFromPlayer.PostSpawnSetup(false);

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.PostSpawnSetup(false);
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.PostSpawnSetup(false);
			}
		}

		public override void CompTick() {
			base.CompTick();

			if (!setup) {
				performSetup();
			}

			compComponentsPositionTracker.CompTick();
			compHiddenable.CompTick();
			compHideFromPlayer.CompTick();

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.CompTick();
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.CompTick();
			}
		}

		public override void CompTickRare() {
			base.CompTickRare();

			if (!setup) {
				performSetup();
			}
			
			compComponentsPositionTracker.CompTickRare();
			compHiddenable.CompTickRare();
			compHideFromPlayer.CompTickRare();

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.CompTickRare();
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.CompTickRare();
			}
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			if (!setup) {
				performSetup();
			}

			compComponentsPositionTracker.ReceiveCompSignal(signal);
			compHiddenable.ReceiveCompSignal(signal);
			compHideFromPlayer.ReceiveCompSignal(signal);

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.ReceiveCompSignal(signal);
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.ReceiveCompSignal(signal);
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			if (!setup) {
				performSetup();
			}

			compComponentsPositionTracker.PostDeSpawn(map);
			compHiddenable.PostDeSpawn(map);
			compHideFromPlayer.PostDeSpawn(map);

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.PostDeSpawn(map);
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.PostDeSpawn(map);
			}
		}

		public override void PostExposeData() {
			base.PostExposeData();
			
			if (!setup) {
				performSetup();
			}

			compComponentsPositionTracker.PostExposeData();
			compHiddenable.PostExposeData();
			compHideFromPlayer.PostExposeData();

			if (compViewBlockerWatcher != null) {
				compViewBlockerWatcher.PostExposeData();
			}
			if (compFieldOfViewWatcher != null) {
				compFieldOfViewWatcher.PostExposeData();
			}

			if (Scribe.saver.savingForDebug) {
				bool hasCompComponentsPositionTracker = compComponentsPositionTracker != null;
				bool hasCompHiddenable = compHiddenable != null;
				bool hasCompHideFromPlayer = compHideFromPlayer != null;
				bool hasCompViewBlockerWatcher = compViewBlockerWatcher != null;
				bool hasCompFieldOfViewWatcher = compFieldOfViewWatcher != null;

				Scribe_Values.Look<bool>(ref hasCompComponentsPositionTracker, "hasCompComponentsPositionTracker");
				Scribe_Values.Look<bool>(ref hasCompHiddenable, "hasCompHiddenable");
				Scribe_Values.Look<bool>(ref hasCompHideFromPlayer, "hasCompHideFromPlayer");
				Scribe_Values.Look<bool>(ref hasCompViewBlockerWatcher, "hasCompViewBlockerWatcher");
				Scribe_Values.Look<bool>(ref hasCompFieldOfViewWatcher, "hasCompFieldOfViewWatcher");
			}
		}
	}
}
