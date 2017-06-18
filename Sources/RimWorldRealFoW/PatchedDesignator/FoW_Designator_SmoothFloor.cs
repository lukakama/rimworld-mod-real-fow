using RimWorld;
using RimWorldRealFoW.ThingComps.ThingSubComps;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.PatchedDesignators {
	class FoW_Designator_SmoothFloor : Designator_SmoothFloor {

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			MapComponentSeenFog mapCmq = base.Map.getMapComponentSeenFog();
			if (mapCmq != null && c.InBounds(base.Map) && !mapCmq.knownCells[base.Map.cellIndices.CellToIndex(c)]) {
				return false;
			}

			return base.CanDesignateCell(c);
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			CompHiddenable cmp = t.TryGetCompHiddenable();
			if (cmp != null && cmp.hidden) {
				return false;
			}

			return base.CanDesignateThing(t);
		}
	}
}
