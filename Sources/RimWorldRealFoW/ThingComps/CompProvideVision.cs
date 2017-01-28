using RimWorldRealFoW.ThingComps.Properties;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompProvideVision : ThingComp {
		public CompProperties_ProvideVision Props {
			get {
				return (CompProperties_ProvideVision) this.props;
			}
		}
	}
}
