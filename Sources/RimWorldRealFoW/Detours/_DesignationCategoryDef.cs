using RimWorldRealFoW.PatchedDesignators;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _DesignationCategoryDef {

		public static void ResolveDesignators(this DesignationCategoryDef _this) {
			List<Designator> resolvedDesignators = ReflectionUtils.getInstancePrivateValue<List<Designator>>(_this, "resolvedDesignators");

			resolvedDesignators.Clear();

			foreach (Type current in _this.specialDesignatorClasses) {
				Designator designator = null;
				try {
					designator = (Designator) Activator.CreateInstance(current);
				} catch (Exception ex) {
					Log.Error(string.Concat(new object[]
					{
						"DesignationCategoryDef",
						_this.defName,
						" could not instantiate special designator from class ",
						current,
						".\n Exception: \n",
						ex.ToString()
					}));
				}
				if (designator != null) {
					resolvedDesignators.Add(designator);
				}
			}
			IEnumerable<BuildableDef> enumerable = from tDef in DefDatabase<ThingDef>.AllDefs.Cast<BuildableDef>().Concat(DefDatabase<TerrainDef>.AllDefs.Cast<BuildableDef>())
																where tDef.designationCategory == _this
																select tDef;
			foreach (BuildableDef current2 in enumerable) {
				resolvedDesignators.Add(new FoW_Designator_Build(current2));
			}
		}
	}
}
