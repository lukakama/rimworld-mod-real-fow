//   Copyright 2017 Luca De Petrillo
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
using RimWorldRealFoW.SectionLayers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	public class RealFoWModSettings : ModSettings {
		public enum FogFadeSpeedEnum { Slow = 5, Medium = 20, Fast = 40, Disabled = 100 }
		public enum FogAlpha { Dark = 127, Medium = 86, Light = 64 }

		public static FogFadeSpeedEnum fogFadeSpeed = FogFadeSpeedEnum.Medium;
		public static FogAlpha fogAlpha = FogAlpha.Medium;

		public static void DoSettingsWindowContents(Rect rect) {
			Listing_Standard list = new Listing_Standard(GameFont.Small);
			list.ColumnWidth = rect.width;
			list.Begin(rect);

			if (list.ButtonTextLabeled("fogAlphaSetting_title".Translate(), ("fogAlphaSetting_" + fogAlpha).Translate())) {
				List<FloatMenuOption> optionList = new List<FloatMenuOption>();
				foreach (FogAlpha enumValue in Enum.GetValues(typeof(FogAlpha))) {
					FogAlpha localValue = enumValue;
					optionList.Add(new FloatMenuOption(("fogAlphaSetting_" + localValue).Translate(), delegate {
						fogAlpha = localValue;

						applySettings();
					}, MenuOptionPriority.Default, null, null, 0f, null, null));
				}
				Find.WindowStack.Add(new FloatMenu(optionList));
			}
			Text.Font = GameFont.Tiny;
			list.Label("fogAlphaSetting_desc".Translate());
			Text.Font = GameFont.Small;

			list.Gap();
			list.GapLine();

			if (list.ButtonTextLabeled("fogFadeSpeedSetting_title".Translate(), ("fogFadeSpeedSetting_" + fogFadeSpeed).Translate())) {
				List<FloatMenuOption> optionList = new List<FloatMenuOption>();
				foreach (FogFadeSpeedEnum enumValue in Enum.GetValues(typeof(FogFadeSpeedEnum))) {
					FogFadeSpeedEnum localValue = enumValue;
					optionList.Add(new FloatMenuOption(("fogFadeSpeedSetting_" + localValue).Translate(), delegate {
						fogFadeSpeed = localValue;

						applySettings();
					}, MenuOptionPriority.Default, null, null, 0f, null, null));
				}
				Find.WindowStack.Add(new FloatMenu(optionList));
			}
			Text.Font = GameFont.Tiny;
			list.Label("fogFadeSpeedSetting_desc".Translate());
			Text.Font = GameFont.Small;

			list.End();
		}

		public static void applySettings() {
			SectionLayer_FoVLayer.prefFadeSpeedMult = (int)fogFadeSpeed / 10f;
			SectionLayer_FoVLayer.prefEnableFade = (int)fogFadeSpeed != 100;

			SectionLayer_FoVLayer.prefFogAlpha = (byte)fogAlpha;

			// If playing, redraw everything.
			if (Current.ProgramState == ProgramState.Playing) {
				foreach (Map map in Find.Maps) {
					if (map.mapDrawer != null) {
						map.mapDrawer.RegenerateEverythingNow();
					}
				}
			}
		}

		public override void ExposeData() {
			base.ExposeData();

			Scribe_Values.Look(ref fogFadeSpeed, "fogFadeSpeed", FogFadeSpeedEnum.Medium);
			Scribe_Values.Look(ref fogAlpha, "fogAlpha", FogAlpha.Medium);

			applySettings();
		}
	}
}
