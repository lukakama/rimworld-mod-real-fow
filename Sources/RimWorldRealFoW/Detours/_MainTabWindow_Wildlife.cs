using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _MainTabWindow_Wildlife {

		public static bool get_Pawns_Prefix(ref IEnumerable<Pawn> __result) {
			__result = from p in Find.CurrentMap.mapPawns.AllPawns
						  where p.Spawned && p.Faction == null && p.AnimalOrWildMan() && !p.Position.Fogged(p.Map) && p.fowIsVisible()
						  select p;

			return false;
		}
	}
}
