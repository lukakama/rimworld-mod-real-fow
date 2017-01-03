﻿//   Copyright 2017 Luca De Petrillo
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
using System;
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
				if (typeof(MoteBubble) == def.thingClass) {
					def.thingClass = typeof(MoteBubbleExt);
				}

				def.comps.Add(comPropsPawnFoV);
				def.comps.Add(comPropsThingHiddeable);

			}
		}

		public static void injectDetours() {
			detour(typeof(Selector), typeof(_Selector), "Select");
			detour(typeof(MouseoverReadout), typeof(_MouseoverReadout), "MouseoverReadoutOnGUI");
			detour(typeof(Verb), typeof(_Verb), "CanHitCellFromCellIgnoringRange");
			detour(typeof(PawnUIOverlay), typeof(_PawnUIOverlay), "DrawPawnGUIOverlay");
			detour(typeof(GenConstruct), typeof(_GenConstruct), "CanPlaceBlueprintAt");
		}

		public static void detour(Type sourceType, Type targetType, string methodName) {
			MethodInfo method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
			if (method != null) {
				MethodInfo newMethod = targetType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
				if (newMethod != null) {
					if (Detours.TryDetourFromTo(method, newMethod)) {
						Log.Message("Detoured method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					}
				}
			}
		}

		public static void detour(Type sourceType, Type targetType, string methodName, params Type[] types) {
			MethodInfo method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
			if (method != null) {
				MethodInfo newMethod = targetType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
				if (newMethod != null) {
					if (Detours.TryDetourFromTo(method, newMethod)) {
						Log.Message("Detoured method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					}
				}
			}
		}
	}
}
