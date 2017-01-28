using RimWorldRealFoW.ThingComps.Properties;
using System;
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompAffectVision : ThingComp {
		public static readonly Type COMP_CLASS = typeof(CompAffectVision);
		public CompProperties_AffectVision Props {
			get {
				return (CompProperties_AffectVision) this.props;
			}
		}
	}
}
