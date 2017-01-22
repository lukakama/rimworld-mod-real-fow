using Verse;

namespace RimWorldRealFoW.ThingComps.Properties {
	public class CompProperties_AffectVision : CompProperties {
		// How much the object affect the base field of view in % (0 = 0%; 0.5 = 50%; 1 = 100%; 1.5 = 150% and so on...).
		public float fovMultiplier;

		// TODO: Deny darkness
		public bool denyDarkness;

		// TODO: Deny wheather.
		public bool denyWeather;

		// TODO: If apply immediately.
		public bool applyImmediately;

		public CompProperties_AffectVision() {
			this.compClass = typeof(CompAffectVision);
		}
	}
}
