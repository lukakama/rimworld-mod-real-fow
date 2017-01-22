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
using RimWorld.Planet;
using RimWorldRealFoW.Detours;
using RimWorldRealFoW.PatchedThings;
using RimWorldRealFoW.ThingComps;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
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
			CompProperties comPropsFieldOfView = new CompProperties(typeof(CompFieldOfViewWatcher));
			CompProperties comPropsHiddenable = new CompProperties(typeof(CompHiddenable));
			CompProperties comPropsHideFromPlayer = new CompProperties(typeof(CompHideFromPlayer));
			CompProperties comPropsViewBlockerWatcher = new CompProperties(typeof(CompViewBlockerWatcher));
			

			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
				// Patch motes
				if (typeof(MoteBubble) == def.thingClass) {
					def.thingClass = typeof(FoW_MoteBubble);
				}

				// FoW components don't care about motes.
				if (!typeof(Mote).IsAssignableFrom(def.thingClass)) {
					addComponent(def, comPropsHiddenable);
					addComponent(def, comPropsFieldOfView);
					addComponent(def, comPropsHideFromPlayer);
					addComponent(def, comPropsViewBlockerWatcher);
					Log.Message("Injected components to " + def + ".");
				}
			}

			// Patch designators
			foreach (DesignationCategoryDef def in DefDatabase<DesignationCategoryDef>.AllDefs) {
				bool patched = false;
				for (int i = 0; i < def.specialDesignatorClasses.Count; i++) {
					Type originalType = def.specialDesignatorClasses[i];
					Type patchedType = Type.GetType("RimWorldRealFoW.PatchedDesignators.FoW_" + originalType.Name, false);
					if (patchedType != null) {
						def.specialDesignatorClasses[i] = patchedType;
						patched = true;
						Log.Message("Patched designator from " + originalType + " to " + patchedType + ".");
					}
				}
				if (patched) {
					def.ResolveReferences();
				}
			}
		}

		public static void addComponent(ThingDef def, CompProperties compProperties) {
			if (!def.comps.Contains(compProperties)) {
				def.comps.Add(compProperties);
			}
		}

		public static void injectDetours() {
			detour(typeof(GenConstruct), typeof(_GenConstruct), "CanPlaceBlueprintAt");
			detour(typeof(Verb), typeof(_Verb), "CanHitCellFromCellIgnoringRange");
			detour(typeof(Selector), typeof(_Selector), "Select");
			detour(typeof(MouseoverReadout), typeof(_MouseoverReadout), "MouseoverReadoutOnGUI");
			detour(typeof(PawnUIOverlay), typeof(_PawnUIOverlay), "DrawPawnGUIOverlay");
			detour(typeof(GenMapUI), typeof(_GenMapUI), "DrawThingLabel", typeof(Thing), typeof(string), typeof(Color));
			detour(typeof(SectionLayer_Things), typeof(_SectionLayer_Things), "Regenerate");
			detour(typeof(WorkGiver_DoBill), typeof(_WorkGiver_DoBill), "TryFindBestBillIngredients");

			detour(typeof(Messages), typeof(_Messages), "Message", typeof(string), typeof(GlobalTargetInfo), typeof(MessageSound));
			detour(typeof(LetterStack), typeof(_LetterStack), "ReceiveLetter", typeof(string), typeof(string), typeof(LetterType), typeof(GlobalTargetInfo), typeof(string));
		}

		public static void detour(Type sourceType, Type targetType, string methodName, params Type[] types) {
			MethodInfo method;
			if (types.Length != 0) {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
			} else {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
			}
			if (method != null) {
				MethodInfo newMethod;
				if (types.Length != 0) {
					newMethod = targetType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
					if (newMethod == null) {
						newMethod = targetType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, (new Type[] { sourceType }).Concat(types).ToArray(), null);
					}
				} else {
					newMethod = targetType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
				}
				if (newMethod != null) {
					if (DetoursApplier.TryDetourFromTo(method, newMethod)) {
						Log.Message("Detoured method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					} else {
						Log.Warning("Unable to detoure method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					}
				} else {
					Log.Warning("Target method " + methodName + " not found for detour from source " + sourceType + " to " + targetType + ".");
				}
			} else {
				Log.Warning("Source method " + methodName + " not found for detour from source " + sourceType + " to " + targetType + ".");
			}
		}
	}
}
