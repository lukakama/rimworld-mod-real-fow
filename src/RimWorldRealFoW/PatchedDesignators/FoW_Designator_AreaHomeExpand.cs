using RimWorld;
using Verse;

namespace RimWorldRealFoW.PatchedDesignators {
	class FoW_Designator_AreaHomeExpand : Designator_AreaHomeExpand {

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			MapComponentSeenFog mapCmq = base.Map.GetComponent<MapComponentSeenFog>();
			if (mapCmq != null && c.InBounds(base.Map) && !mapCmq.knownCells[base.Map.cellIndices.CellToIndex(c)]) {
				return false;
			}

			return base.CanDesignateCell(c);
		}
	}
}
