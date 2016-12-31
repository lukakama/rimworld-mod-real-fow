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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {
	public static class _MouseoverReadout {
		public static DetourHook hookMouseoverReadoutOnGUI;

		private static readonly Vector2 BotLeft = new Vector2(15f, 65f);

		private static T getInstancePrivateValue<T>(MouseoverReadout _this, string fieldName) {
			return (T)_this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_this);
		}

		private static void setInstancePrivateValue(MouseoverReadout _this, string fieldName, object value) {
			_this.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_this, value);
		}

		private static T execInstancePrivate<T>(MouseoverReadout _this, string methodName, params object[] values) {
			return (T)_this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}

		private static void execInstancePrivate(MouseoverReadout _this, string methodName, params object[] values) {
			_this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_this, values);
		}

		public static void MouseoverReadoutOnGUI(this MouseoverReadout _this) {
			if (Event.current.type != EventType.Repaint) {
				return;
			}
			if (Find.MainTabsRoot.OpenTab != null) {
				return;
			}
			GenUI.DrawTextWinterShadow(new Rect(256f, (float)(UI.screenHeight - 256), -256f, 256f));
			Text.Font = GameFont.Small;
			GUI.color = new Color(1f, 1f, 1f, 0.8f);
			IntVec3 c = UI.MouseCell();
			if (!c.InBounds(Find.VisibleMap)) {
				return;
			}
			float num = 0f;
			Rect rect;
			if (c.Fogged(Find.VisibleMap)) {
				rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
				Widgets.Label(rect, "Undiscovered".Translate());
				GUI.color = Color.white;
				return;
			}
			rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
			int num2 = Mathf.RoundToInt(Find.VisibleMap.glowGrid.GameGlowAt(c) * 100f);
			Widgets.Label(rect, getInstancePrivateValue<string[]>(_this, "glowStrings")[num2]);
			num += 19f;
			rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
			TerrainDef terrain = c.GetTerrain(Find.VisibleMap);
			if (terrain != getInstancePrivateValue<TerrainDef>(_this, "cachedTerrain")) {
				string str = ((double)terrain.fertility <= 0.0001) ? string.Empty : (" " + "FertShort".Translate() + " " + terrain.fertility.ToStringPercent());
				setInstancePrivateValue(_this, "cachedTerrainString", terrain.LabelCap + ((terrain.passability == Traversability.Impassable) ? null : (" (" + "WalkSpeed".Translate(new object[]
				{
					execInstancePrivate<string>(_this, "SpeedPercentString", (float)terrain.pathCost)
				}) + str + ")")));
				setInstancePrivateValue(_this, "cachedTerrain", terrain);
			}
			Widgets.Label(rect, getInstancePrivateValue<string>(_this, "cachedTerrainString"));
			num += 19f;
			Zone zone = c.GetZone(Find.VisibleMap);
			if (zone != null) {
				rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
				string label = zone.label;
				Widgets.Label(rect, label);
				num += 19f;
			}
			float depth = Find.VisibleMap.snowGrid.GetDepth(c);
			if (depth > 0.03f) {
				rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
				SnowCategory snowCategory = SnowUtility.GetSnowCategory(depth);
				string label2 = SnowUtility.GetDescription(snowCategory) + " (" + "WalkSpeed".Translate(new object[]
				{
					execInstancePrivate<string>(_this, "SpeedPercentString", (float)SnowUtility.MovementTicksAddOn(snowCategory))
				}) + ")";
				Widgets.Label(rect, label2);
				num += 19f;
			}
			List<Thing> thingList = c.GetThingList(Find.VisibleMap);
			for (int i = 0; i < thingList.Count; i++) {
				Thing thing = thingList[i];
				CompHiddenable comp = thing.TryGetComp<CompHiddenable>();
				if ((comp == null || !comp.hidden) && thing.def.category != ThingCategory.Mote) {
					rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
					string labelMouseover = thing.LabelMouseover;
					Widgets.Label(rect, labelMouseover);
					num += 19f;
				}
			}
			RoofDef roof = c.GetRoof(Find.VisibleMap);
			if (roof != null) {
				rect = new Rect(_MouseoverReadout.BotLeft.x, (float)UI.screenHeight - _MouseoverReadout.BotLeft.y - num, 999f, 999f);
				Widgets.Label(rect, roof.LabelCap);
				num += 19f;
			}
			GUI.color = Color.white;
		}
	}
}
