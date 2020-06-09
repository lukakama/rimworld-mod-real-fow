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
using HarmonyLib;
using RimWorld;
using RimWorldRealFoW.Detours;
using RimWorldRealFoW.ThingComps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if InternalProfile
using RimWorldRealFoW.Detours.Profiling;
#endif
#if Profile
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW {
	[StaticConstructorOnStartup]
	public class RealFoWModStarter : Mod {

#if Profile
		[DllImport("__Internal")]
		private static extern void mono_profiler_load(string args);
#endif

		static Harmony harmony;
		static RealFoWModStarter() {

#if Profile
			mono_profiler_load(@"default:time,file=d:/rimworld-prof.mprf");
#endif

			harmony = new Harmony("com.github.lukakama.rimworldmodrealfow");
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
			patchMethod(typeof(Verb), typeof(_Verb), "CanHitCellFromCellIgnoringRange");
			patchMethod(typeof(Selector), typeof(_Selector), "Select");
			patchMethod(typeof(MouseoverReadout), typeof(_MouseoverReadout), "MouseoverReadoutOnGUI");
			patchMethod(typeof(BeautyUtility), typeof(_BeautyUtility), "FillBeautyRelevantCells");

			patchMethod(typeof(MainTabWindow_Wildlife), typeof(_MainTabWindow_Wildlife), "get_Pawns");

			patchMethod(typeof(Pawn), typeof(_Pawn), "DrawGUIOverlay");
			
			patchMethod(typeof(GenMapUI), typeof(_GenMapUI), "DrawThingLabel", typeof(Thing), typeof(string), typeof(Color));
			patchMethod(typeof(SectionLayer_ThingsGeneral), typeof(_SectionLayer_ThingsGeneral), "TakePrintFrom");
			patchMethod(typeof(SectionLayer_ThingsPowerGrid), typeof(_SectionLayer_ThingsPowerGrid), "TakePrintFrom");
			patchMethod(typeof(ReservationUtility), typeof(_ReservationUtility), "CanReserve");
			patchMethod(typeof(ReservationUtility), typeof(_ReservationUtility), "CanReserveAndReach");
			patchMethod(typeof(HaulAIUtility), typeof(_HaulAIUtility), "HaulToStorageJob");

			patchMethod(typeof(EnvironmentStatsDrawer), typeof(_EnvironmentStatsDrawer), "ShouldShowWindowNow");
			
			patchMethod(typeof(Messages), typeof(_Messages), "Message", typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(bool));
			patchMethod(typeof(LetterStack), typeof(_LetterStack), "ReceiveLetter", typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(LookTargets), typeof(Faction), typeof(Quest), typeof(List<ThingDef>), typeof(string));

			patchMethod(typeof(MoteBubble), typeof(_MoteBubble), "Draw", new Type[] {});
			patchMethod(typeof(GenView), typeof(_GenView), "ShouldSpawnMotesAt", new Type[]{typeof(IntVec3), typeof(Map)});

			patchMethod(typeof(FertilityGrid), typeof(_FertilityGrid), "CellBoolDrawerGetBoolInt");
			patchMethod(typeof(TerrainGrid), typeof(_TerrainGrid), "CellBoolDrawerGetBoolInt");
			patchMethod(typeof(RoofGrid), typeof(_RoofGrid), "GetCellBool");
			

			// Area only designators:
			patchMethod(typeof(Designator_AreaBuildRoof), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_AreaNoRoof), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_ZoneAdd_Growing), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_ZoneAddStockpile), typeof(_Designator_Prefix), "CanDesignateCell");

			// Area + thing designators:
			patchMethod(typeof(Designator_Claim), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Claim), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_Deconstruct), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Deconstruct), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_Haul), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Haul), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_Hunt), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Hunt), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_Plants), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Plants), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_PlantsHarvest), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_PlantsHarvestWood), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_RemoveFloor), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_SmoothSurface), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Tame), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Tame), typeof(_Designator_Prefix), "CanDesignateThing");
			patchMethod(typeof(Designator_Uninstall), typeof(_Designator_Prefix), "CanDesignateCell");
			patchMethod(typeof(Designator_Uninstall), typeof(_Designator_Prefix), "CanDesignateThing");
			
			// Placing designators:
			patchMethod(typeof(Designator_Build), typeof(_Designator_Place_Postfix), "CanDesignateCell");
			patchMethod(typeof(Designator_Install), typeof(_Designator_Place_Postfix), "CanDesignateCell");
			
			// Specific designatos:
			patchMethod(typeof(Designator_Mine), typeof(_Designator_Mine), "CanDesignateCell");
			
			// Designation
			patchMethod(typeof(Designation), typeof(_Designation), "Notify_Added");
			patchMethod(typeof(Designation), typeof(_Designation), "Notify_Removing");

#if InternalProfile
			// Profiling
			patchMethod(typeof(EditWindow_DebugInspector), typeof(_EditWindow_DebugInspector), "CurrentDebugString");
			patchMethod(typeof(TickManager), typeof(_TickManager), "DoSingleTick");
#endif
		}

		public static void patchMethod(Type sourceType, Type targetType, string methodName) {
			patchMethod(sourceType, targetType, methodName, null);
		}

		public static void patchMethod(Type sourceType, Type targetType, string methodName, params Type[] types) {
			MethodInfo method = null;
			if (types != null) {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll, null, types, null);
			} else {
				method = sourceType.GetMethod(methodName, GenGeneric.BindingFlagsAll);
			}

			if (sourceType != method.DeclaringType) {
				Log.Message("Inconsistent method declaring type for method " + methodName + ": expected " + sourceType + " but found " + method.DeclaringType);
			}

			if (method != null) {
				MethodInfo newMethodPrefix = null;
				if (types != null) {
					newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll, null, types, null);
					if (newMethodPrefix == null) {
						newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll, null, (new Type[] { sourceType }).Concat(types).ToArray(), null);
					}
				}
				if (newMethodPrefix == null) {
					newMethodPrefix = targetType.GetMethod(methodName + "_Prefix", GenGeneric.BindingFlagsAll);
				}

				MethodInfo newMethodPostfix = null;
				if (types != null) {
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
						Log.Message("Patched method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					} else {
						Log.Warning("Unable to patch method " + method.ToString() + " from source " + sourceType + " to " + targetType + ".");
					}
				} else {
					Log.Warning("Target method prefix or suffix " + methodName + " not found for patch from source " + sourceType + " to " + targetType + ".");
				}
			} else {
				Log.Warning("Source method " + methodName + " not found for patch from source " + sourceType + " to " + targetType + ".");
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