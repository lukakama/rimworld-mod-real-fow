using RimWorldRealFoW.Utils;
using System;
using Verse;

namespace RimWorldRealFoW.Detours {
	public static class _SectionLayer_ThingsGeneral {
		public static void TakePrintFrom(this SectionLayer_ThingsGeneral _this, Thing t) {
			if (t.fowIsVisible(true)) {
				try {
					t.Print(_this);
				} catch (Exception ex) {
					Log.Error(string.Concat(new object[]
					{
						"Exception printing ",
						t,
						" at ",
						t.Position,
						": ",
						ex.ToString()
					}));
				}
			}
		}
	}
}
