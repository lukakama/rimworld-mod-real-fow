using RimWorld;
using RimWorldRealFoW.PatchedDesignators;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _InstallationDesignatorDatabase {
		public static Designator_Install NewDesignatorFor(ThingDef artDef) {
			return new FoW_Designator_Install {
				hotKey = KeyBindingDefOf.Misc1
			};
		}
	}
}
