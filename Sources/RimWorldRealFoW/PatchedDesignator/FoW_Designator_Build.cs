using RimWorld;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.PatchedDesignators {
	class FoW_Designator_Build : Designator_Build {

		public FoW_Designator_Build(BuildableDef entDef) : base(entDef) {
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			AcceptanceReport baseReport = base.CanDesignateCell(c);
			
			if (baseReport.Accepted) {
				CellRect cellRect = GenAdj.OccupiedRect(c, this.placingRot, entDef.Size);
				MapComponentSeenFog seenFog = base.Map.getMapComponentSeenFog();
				if (seenFog != null) {
					CellRect.CellRectIterator itCellRect = cellRect.GetIterator();
					while (!itCellRect.Done()) {
						if (!seenFog.knownCells[base.Map.cellIndices.CellToIndex(itCellRect.Current)]) {
							return "CannotPlaceInUndiscovered".Translate();
						}
						itCellRect.MoveNext();
					}
				}
			}

			return baseReport;
		}
	}
}
