using RimWorld;
using RimWorldRealFoW.ThingComps;
using Verse;

namespace RimWorldRealFoW.Detours.PatchedDesignators {
	class FoW_Designator_Hunt : Designator_Hunt {

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			MapComponentSeenFog mapCmq = base.Map.GetComponent<MapComponentSeenFog>();
			if (mapCmq != null && c.InBounds(base.Map) && !mapCmq.knownCells[base.Map.cellIndices.CellToIndex(c)]) {
				return false;
			}

			return base.CanDesignateCell(c);
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			CompHiddenable cmp = t.TryGetComp<CompHiddenable>();
			if (cmp != null && cmp.hidden) {
				return false;
			}

			return base.CanDesignateThing(t);
		}
	}
}
