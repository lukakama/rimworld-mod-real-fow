using RimWorld;
using RimWorldRealFoW.ThingComps;
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.Detours.PatchedDesignators {
	class FoW_Designator_RemoveFloor : Designator_RemoveFloor {

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			MapComponentSeenFog mapCmq = base.Map.getMapComponentSeenFog();
			if (mapCmq != null && c.InBounds(base.Map) && !mapCmq.knownCells[base.Map.cellIndices.CellToIndex(c)]) {
				return false;
			}

			return base.CanDesignateCell(c);
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			CompHiddenable cmp = (CompHiddenable) t.TryGetComp(CompHiddenable.COMP_DEF);
			if (cmp != null && cmp.hidden) {
				return false;
			}

			return base.CanDesignateThing(t);
		}
	}
}
