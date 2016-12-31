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
using System.Reflection;
using Verse;

namespace RimWorldRealFoW {
	[StaticConstructorOnStartup]
	class RealFoWModStarter : Def {
		static RealFoWModStarter() {
			// NO-OP (here for future uses)
		}

		public RealFoWModStarter() {
			LongEventHandler.QueueLongEvent(injectComponents, "Real Fog of War - Init.", false, null);
			LongEventHandler.QueueLongEvent(injectDetours, "Real Fog of War - Init..", false, null);
		}

		public static void injectComponents() {
			CompProperties comPropsPawnFoV = new CompProperties(typeof(CompFieldOfView));
			CompProperties comPropsThingHiddeable = new CompProperties(typeof(CompHiddenable));

			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
				def.comps.Add(comPropsPawnFoV);
				def.comps.Add(comPropsThingHiddeable);
			}
		}

		public static void injectDetours() {
			detourFloodFillerFog();
			detourSelector();
			detourMouseoverReadout();
		}

		public static void detourFloodFillerFog() {
			MethodInfo method = typeof(FloodFillerFog).GetMethod("FloodUnfog", BindingFlags.Static | BindingFlags.Public);
			if (method != null) {
				MethodInfo newMethod = typeof(_FloodFillerFog).GetMethod("FloodUnfog", BindingFlags.Static | BindingFlags.Public);
				if (newMethod != null) {
					_FloodFillerFog.hookFloodUnfog = new DetourHook(method, newMethod);
					Log.Message("Detoured FloodFillerFog.FloodUnfog");
				}
			}
		}

		public static void detourSelector() {
			MethodInfo method = typeof(Selector).GetMethod("Select", BindingFlags.Instance | BindingFlags.Public);
			if (method != null) {
				MethodInfo newMethod = typeof(_Selector).GetMethod("Select", BindingFlags.Static | BindingFlags.Public);
				if (newMethod != null) {
					_Selector.hookSelect = new DetourHook(method, newMethod);
					Log.Message("Detoured Selector.Select");
				}
			}
		}

		public static void detourMouseoverReadout() {
			MethodInfo method = typeof(MouseoverReadout).GetMethod("MouseoverReadoutOnGUI", BindingFlags.Instance | BindingFlags.Public);
			if (method != null) {
				MethodInfo newMethod = typeof(_MouseoverReadout).GetMethod("MouseoverReadoutOnGUI", BindingFlags.Static | BindingFlags.Public);
				if (newMethod != null) {
					_MouseoverReadout.hookMouseoverReadoutOnGUI = new DetourHook(method, newMethod);
					Log.Message("Detoured MouseoverReadout.MouseoverReadoutOnGUI");
				}
			}
		}
	}
}
