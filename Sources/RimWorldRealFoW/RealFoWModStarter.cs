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
using RimWorldRealFoW.PatchedDesignators;
using RimWorldRealFoW.PatchedThings;
using RimWorldRealFoW.ThingComps;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW {
	[StaticConstructorOnStartup]
	public class RealFoWModStarter : Def {
		static RealFoWModStarter() {
			// NO-OP (here for future uses)
		}

		public RealFoWModStarter() {
#if Profile
			Profiler.enabled = true;
#endif

			LongEventHandler.QueueLongEvent(injectDetours, "Real Fog of War - Init.", false, null);
			LongEventHandler.QueueLongEvent(injectComponents, "Real Fog of War - Init..", false, null);
		}

		public static void injectComponents() {
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
				// Patch motes
				if (typeof(MoteBubble) == def.thingClass) {
					def.thingClass = typeof(FoW_MoteBubble);
				}

				if (typeof(ThingWithComps).IsAssignableFrom(def.thingClass)
						&& !typeof(Mote).IsAssignableFrom(def.thingClass)) {
					// This must be the first component triggered on ticks (other component behaviours depend on his trackings),
					// so it must be added first.
					addComponent(def, CompComponentsPositionTracker.COMP_DEF);

					if (typeof(Building).IsAssignableFrom(def.thingClass) 
							|| typeof(Pawn).IsAssignableFrom(def.thingClass)) {
						addComponent(def, CompFieldOfViewWatcher.COMP_DEF);
					}
					if (typeof(Building).IsAssignableFrom(def.thingClass)) {
						addComponent(def, CompViewBlockerWatcher.COMP_DEF);
					}

					addComponent(def, CompHiddenable.COMP_DEF);
					addComponent(def, CompHideFromPlayer.COMP_DEF);
				}
			}
		}

		public static void patchDesignators() {
			// Patch designators
			long patchedDesignators = 0;
			long totDesignators = 0;

			foreach (DesignationCategoryDef def in DefDatabase<DesignationCategoryDef>.AllDefs) {
				// Experienced some null reference, probably due some other mods.
				if (def != null && ReflectionUtils.hasInstancePrivateField(def, "resolvedDesignators")) {
					List<Designator> resolvedDesignators = ReflectionUtils.getInstancePrivateValue<List<Designator>>(def, "resolvedDesignators");

					for (int i = 0; i < resolvedDesignators.Count; i++) {
						totDesignators++;

						Type originalType = resolvedDesignators[i].GetType();
						Type patchedType = Type.GetType("RimWorldRealFoW.PatchedDesignators.FoW_" + originalType.Name, false);

						if (originalType == typeof(Designator_Build)) {
							Designator_Build des = (Designator_Build) resolvedDesignators[i];
							resolvedDesignators[i] = new FoW_Designator_Build(des.PlacingDef);

							patchedDesignators++;
						} else if (originalType == typeof(Designator_Install)) {
							Designator_Install des = (Designator_Install) resolvedDesignators[i];
							resolvedDesignators[i] = new FoW_Designator_Install {
								hotKey = des.hotKey
							};

							patchedDesignators++;
						} else if (patchedType != null) {
							resolvedDesignators[i] = (Designator) Activator.CreateInstance(patchedType);

							patchedDesignators++;
						}
					}
				}
			}

			Log.Message("Patched " + patchedDesignators + " designators on " + totDesignators + ".");
		}

		public static void addComponent(ThingDef def, CompProperties compProperties) {
			if (!def.comps.Contains(compProperties)) {
				def.comps.Add(compProperties);
			}
		}

		public static void injectDetours() {
			detour(typeof(Verb), typeof(_Verb), "CanHitCellFromCellIgnoringRange");
			detour(typeof(Selector), typeof(_Selector), "Select");
			detour(typeof(MouseoverReadout), typeof(_MouseoverReadout), "MouseoverReadoutOnGUI");
			detour(typeof(PawnUIOverlay), typeof(_PawnUIOverlay), "DrawPawnGUIOverlay");
			detour(typeof(GenMapUI), typeof(_GenMapUI), "DrawThingLabel", typeof(Thing), typeof(string), typeof(Color));
			detour(typeof(SectionLayer_Things), typeof(_SectionLayer_Things), "Regenerate");
			detour(typeof(WorkGiver_DoBill), typeof(_WorkGiver_DoBill), "TryFindBestBillIngredients");
			detour(typeof(HaulAIUtility), typeof(_HaulAIUtility), "HaulToStorageJob");

			detour(typeof(InstallationDesignatorDatabase), typeof(_InstallationDesignatorDatabase), "NewDesignatorFor");

			detour(typeof(HaulAIUtility).Assembly.GetType("Verse.EnvironmentInspectDrawer"), typeof(_EnvironmentInspectDrawer), "ShouldShow");
			
			detour(typeof(Messages), typeof(_Messages), "Message", typeof(string), typeof(GlobalTargetInfo), typeof(MessageSound));
			detour(typeof(LetterStack), typeof(_LetterStack), "ReceiveLetter", typeof(string), typeof(string), typeof(LetterDef), typeof(GlobalTargetInfo), typeof(string));
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
