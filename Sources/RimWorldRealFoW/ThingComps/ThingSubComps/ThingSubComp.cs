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
using Verse;

namespace RimWorldRealFoW.ThingComps.ThingSubComps {
	public abstract class ThingSubComp {
		public ThingWithComps parent;
		public CompMainComponent mainComponent;

		public virtual void CompTick() {

		}
		public virtual void CompTickRare() {

		}
		public virtual void PostDeSpawn(Map map) {

		}
		public virtual void PostExposeData() {

		}
		public virtual void PostSpawnSetup(bool respawningAfterLoad) {

		}
		public virtual void ReceiveCompSignal(string signal) {

		}
	}
}
