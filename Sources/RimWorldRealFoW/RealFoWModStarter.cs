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
using Harmony;
using RimWorld;
using RimWorld.Planet;
using RimWorldRealFoW.Detours;
using RimWorldRealFoW.SectionLayers;
using RimWorldRealFoW.ThingComps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if Profile
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using Verse;
using Verse.AI;
using static RimWorldRealFoW.RealFoWModSettings;

namespace RimWorldRealFoW {
	[StaticConstructorOnStartup]
	public class RealFoWModStarter : Mod {

#if Profile
		[DllImport("__Internal")]
		private static extern void mono_profiler_load(string args);
#endif

		static HarmonyInstance harmony;
		static RealFoWModStarter() {

#if Profile
			mono_profiler_load(@"default:time,file=d:/rimworld-prof.mprf");
#endif

			harmony = HarmonyInstance.Create("com.github.lukakama.rimworldmodrealfow");
			injectDetours();
			harmony = null;
		}

		public RealFoWModStarter(ModContentPack content) : base(content) {
			//LongEventHandler.QueueLongEvent(injectDetours, "Real Fog of War - Init.", false, null);
			LongEventHandler.QueueLongEvent(injectComponents, "Real Fog of War - Init.", false, null);

			GetSettings<RealFoWModSettings>();
		}

		public override string SettingsCategory() {
			return Content.Name;
		}

		public override void DoSettingsWindowContents(Rect rect) {
			RealFoWModSettings.DoSettingsWindowContents(rect);
		}
		
		public static void injectComponents() {
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs) {
				ThingCategory thingCategory = def.category;
				if (typeof(ThingWithComps).IsAssignableFrom(def.thingClass) && 
						(thingCategory == ThingCategory.Pawn ||
							thingCategory == ThingCategory.Building ||
							thingCategory == ThingCategory.Item ||
							thingCategory == ThingCategory.Filth ||
							thingCategory == ThingCategory.Gas ||
							def.IsBlueprint)) {
					addComponentAsFirst(def, CompMainComponent.COMP_DEF);
				}
			}
		}

		public static void addComponentAsFirst(ThingDef def, CompProperties compProperties) {
			if (!def.comps.Contains(compProperties)) {
				def.comps.Insert(0, compProperties);
			}
		}

		public static void injectDetours() {
			detour(typeof(Verb), typeof(_Verb), "CanHitCellFromCellIgnoringRange");
			detour(typeof(Selector), typeof(_Selector), "Select");
			detour(typeof(MouseoverReadout), typeof(_MouseoverReadout), "MouseoverReadoutOnGUI");
			
			detour(typeof(Pawn), typeof(_Pawn), "DrawGUIOverlay");

			detour(typeof(GenMapUI), typeof(_GenMapUI), "DrawThingLabel", typeof(Thing), typeof(string), typeof(Color));
			detour(typeof(SectionLayer_ThingsGeneral), typeof(_SectionLayer_ThingsGeneral), "TakePrintFrom");
			detour(typeof(SectionLayer_ThingsPowerGrid), typeof(_SectionLayer_ThingsPowerGrid), "TakePrintFrom");
			detour(typeof(ReservationUtility), typeof(_ReservationUtility), "CanReserve");
			detour(typeof(ReservationUtility), typeof(_ReservationUtility), "CanReserveAndReach");
			detour(typeof(HaulAIUtility), typeof(_HaulAIUtility), "HaulToStorageJob");

			detour(typeof(HaulAIUtility).Assembly.GetType("Verse.EnvironmentInspectDrawer"), typeof(_EnvironmentInspectDrawer), "ShouldShow");
			
			detour(typeof(Messages), typeof(_Messages), "Message", typeof(string), typeof(GlobalTargetInfo), typeof(MessageSound));
			detour(typeof(LetterStack), typeof(_LetterStack), "ReceiveLetter", typeof(string), typeof(string), typeof(LetterDef), typeof(GlobalTargetInfo), typeof(string));

			detour(typeof(MoteBubble), typeof(_MoteBubble), "Draw");

			// Area only designators:
			detour(typeof(Designator_AreaBuildRoofExpand), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_AreaHomeExpand), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_AreaNoRoofExpand), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_ZoneAdd_Growing), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_ZoneAddStockpile_Dumping), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_ZoneAddStockpile_Resources), typeof(_Designator_Prefix), "CanDesignateCell");

			// Area + thing designators:
			detour(typeof(Designator_Claim), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Claim), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_Deconstruct), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Deconstruct), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_Haul), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Haul), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_Hunt), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Hunt), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_PlantsCut), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_PlantsCut), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_PlantsHarvest), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_PlantsHarvest), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_PlantsHarvestWood), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_PlantsHarvestWood), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_RemoveFloor), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_RemoveFloor), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_SmoothFloor), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_SmoothFloor), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_Tame), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Tame), typeof(_Designator_Prefix), "CanDesignateThing");
			detour(typeof(Designator_Uninstall), typeof(_Designator_Prefix), "CanDesignateCell");
			detour(typeof(Designator_Uninstall), typeof(_Designator_Prefix), "CanDesignateThing");
			
			// Placing designators:
			detour(typeof(Designator_Build), typeof(_Designator_Place_Postfix), "CanDesignateCell");
			detour(typeof(Designator_Install), typeof(_Designator_Place_Postfix), "CanDesignateCell");
			
			// Specific designatos:
			detour(typeof(Designator_Mine), typeof(_Designator_Mine), "CanDesignateCell");
		}

		public static void detour(Type sourceType, Type targetType, string methodName, params Type[] types) {
			MethodInfo method = null;
			if (types.Length != 0) {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
			} else {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
			}

			if (method != null) {
				MethodInfo newMethodPrefix = null;
				if (types.Length != 0) {
					newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll, null, types, null);
					if (newMethodPrefix == null) {
						newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll, null, (new Type[] { sourceType }).Concat(types).ToArray(), null);
					}
				}
				if (newMethodPrefix == null) {
					newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll);
				}

				MethodInfo newMethodPostfix = null;
				if (types.Length != 0) {
					newMethodPostfix = targetType.GetMethod(methodName + "_Postfix", GenGeneric.BindingFlagsAll, null, types, null);
					if (newMethodPostfix == null) {
						newMethodPostfix = targetType.GetMethod(methodName + "_Postfix", GenGeneric.BindingFlagsAll, null, (new Type[] { sourceType }).Concat(types).ToArray(), null);
					}
				}
				if (newMethodPostfix == null) {
					newMethodPostfix = targetType.GetMethod(methodName + "_Postfix", GenGeneric.BindingFlagsAll);
				}

				if (newMethodPrefix != null || newMethodPostfix != null) {
					if (patchWithHarmony(method, newMethodPrefix, newMethodPostfix)) {
						Log.Message("Detoured method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					} else {
						Log.Warning("Unable to detoure method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					}
				} else {
					Log.Warning("Target method prefix or suffix " + methodName + " not found for detour from source " + sourceType + " to " + targetType + ".");
				}
			} else {
				Log.Warning("Source method " + methodName + " not found for detour from source " + sourceType + " to " + targetType + ".");
			}
		}

		public static bool patchWithHarmony(MethodInfo original, MethodInfo prefix, MethodInfo postfix) {
			try {
				HarmonyMethod harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
				HarmonyMethod harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

				harmony.Patch(original, harmonyPrefix, harmonyPostfix);

				return true;
			} catch (Exception ex) {
				Log.Warning("Error patching with Harmony: " + ex.Message);
				Log.Warning(ex.StackTrace);
				return false;
			}
		}
	}
}
