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
using HugsLib;
using HugsLib.Settings;
using RimWorldRealFoW.SectionLayers;
using Verse;

namespace RimWorldRealFoW {
	public class RealFoWModSettings : ModBase {
		public override string ModIdentifier {
			get { return "RealFogOfWar"; }
		}
			
		public enum FogFadeSpeedEnum { Slow = 5, Medium = 20, Fast = 40, Disabled = 100 }
		public SettingHandle<FogFadeSpeedEnum> fogFadeSpeed;

		public enum FogAlpha { Dark = 127, Medium = 86, Light = 64 }
		public SettingHandle<FogAlpha> fogAlpha;

		public override void DefsLoaded() {
			fogAlpha = Settings.GetHandle<FogAlpha>("fogAlpha", "fogAlphaSetting_title".Translate(), "fogAlphaSetting_desc".Translate(), FogAlpha.Medium, null, "fogAlphaSetting_");
			fogFadeSpeed = Settings.GetHandle<FogFadeSpeedEnum>("fogFadeSpeed", "fogFadeSpeedSetting_title".Translate(), "fogFadeSpeedSetting_desc".Translate(), FogFadeSpeedEnum.Medium, null, "fogFadeSpeedSetting_");

			applySettings();
		}

		public void applySettings() {
			SectionLayer_FoVLayer.prefFadeSpeedMult = (int) fogFadeSpeed.Value / 10f;
			SectionLayer_FoVLayer.prefEnableFade = (int) fogFadeSpeed.Value != 100;

			SectionLayer_FoVLayer.prefFogAlpha = (byte) fogAlpha.Value;

			// If playing, redraw everything.
			if (Current.ProgramState == ProgramState.Playing) {
				foreach (Map map in Find.Maps) {
					if (map.mapDrawer != null) {
						map.mapDrawer.RegenerateEverythingNow();
					}
				}
			}
		}

		public override void SettingsChanged() {
			base.SettingsChanged();

			applySettings();
		}
	}
}
