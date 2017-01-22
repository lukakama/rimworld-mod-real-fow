using Verse;

namespace RimWorldRealFoW.ThingComps.Properties {
	public class CompProperties_ProvideVision : CompProperties {
		public float viewRadius;

		public CompProperties_ProvideVision() {
			this.compClass = typeof(CompProvideVision);
		}
	}
}
