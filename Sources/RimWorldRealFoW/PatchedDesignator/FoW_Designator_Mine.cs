using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.PatchedDesignators {
	class FoW_Designator_Mine : Designator_Mine {

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			AcceptanceReport baseReport = base.CanDesignateCell(c);

			if (!baseReport.Accepted && base.Map.designationManager.DesignationAt(c, DesignationDefOf.Mine) == null) {
				MapComponentSeenFog mapCmq = base.Map.getMapComponentSeenFog();
				if (mapCmq != null && c.InBounds(base.Map) && !mapCmq.knownCells[base.Map.cellIndices.CellToIndex(c)]) {
					return true;
				}
			}

			return baseReport;
		}
	}
}
