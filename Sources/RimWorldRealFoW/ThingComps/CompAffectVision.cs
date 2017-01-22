using RimWorldRealFoW.ThingComps.Properties;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompAffectVision : ThingComp {
		public CompProperties_AffectVision Props {
			get {
				return (CompProperties_AffectVision) this.props;
			}
		}
	}
}
