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
using RimWorld;
using RimWorldRealFoW.Utils;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW.SectionLayers {

	public class SectionLayer_FoVLayer : SectionLayer {
		// TODO: Link to preferences.
		private bool prefEnableFade = true;
		private int prefFadeSpeedMult = 2;


		private MapComponentSeenFog pawnFog;
		
		public static MapMeshFlag mapMeshFlag = MapMeshFlag.None;

		static SectionLayer_FoVLayer() {
			// Probably not needed... check just in case the static constructor is executed more than one time (extensions, etc)...
			if (mapMeshFlag == MapMeshFlag.None) {
				// Inject new flag.
				List<MapMeshFlag> allFlags = (List<MapMeshFlag>) typeof(MapDrawer).Assembly.GetType("Verse.MapMeshFlagUtility").GetField("allFlags", GenGeneric.BindingFlagsAll).GetValue(null);
				MapMeshFlag maxMapMeshFlag = MapMeshFlag.None;
				foreach (MapMeshFlag mapMeshFlag in allFlags) {
					if (mapMeshFlag > maxMapMeshFlag) {
						maxMapMeshFlag = mapMeshFlag;
					}
				}
				SectionLayer_FoVLayer.mapMeshFlag = (MapMeshFlag) (((int) maxMapMeshFlag) * 2);
				allFlags.Add(SectionLayer_FoVLayer.mapMeshFlag);

				Log.Message("Injected new mapMeshFlag: " + SectionLayer_FoVLayer.mapMeshFlag);
			}
		}


		public SectionLayer_FoVLayer(Section section) : base(section) {
			this.relevantChangeTypes = SectionLayer_FoVLayer.mapMeshFlag | MapMeshFlag.FogOfWar;
		}

		bool activeFogTransitions = false;
		
		private bool[] vertsNotShown = new bool[9];
		private bool[] vertsSeen = new bool[9];

		private byte[] targetAlphas = new byte[0];
		private long[] alphaChangeTick = new long[0];
		private Color32[] meshColors = new Color32[0];

		public override bool Visible {
			get {
				return DebugViewSettings.drawFog;
			}
		}

		public static void MakeBaseGeometry(Section section, LayerSubMesh sm, AltitudeLayer altitudeLayer) {
			CellRect cellRect = new CellRect(section.botLeft.x, section.botLeft.z, 17, 17);
			cellRect.ClipInsideMap(section.map);
			float y = Altitudes.AltitudeFor(altitudeLayer);
			sm.verts.Capacity = cellRect.Area * 9;
			for (int x = cellRect.minX; x <= cellRect.maxX; x++) {
				for (int z = cellRect.minZ; z <= cellRect.maxZ; z++) {
					sm.verts.Add(new Vector3((float) x, y, (float) z));
					sm.verts.Add(new Vector3((float) x, y, (float) z + 0.5f));
					sm.verts.Add(new Vector3((float) x, y, (float) (z + 1)));
					sm.verts.Add(new Vector3((float) x + 0.5f, y, (float) (z + 1)));
					sm.verts.Add(new Vector3((float) (x + 1), y, (float) (z + 1)));
					sm.verts.Add(new Vector3((float) (x + 1), y, (float) z + 0.5f));
					sm.verts.Add(new Vector3((float) (x + 1), y, (float) z));
					sm.verts.Add(new Vector3((float) x + 0.5f, y, (float) z));
					sm.verts.Add(new Vector3((float) x + 0.5f, y, (float) z + 0.5f));
				}
			}
			int num = cellRect.Area * 8 * 3;
			sm.tris.Capacity = num;
			int num2 = 0;
			while (sm.tris.Count < num) {
				sm.tris.Add(num2 + 7);
				sm.tris.Add(num2);
				sm.tris.Add(num2 + 1);
				sm.tris.Add(num2 + 1);
				sm.tris.Add(num2 + 2);
				sm.tris.Add(num2 + 3);
				sm.tris.Add(num2 + 3);
				sm.tris.Add(num2 + 4);
				sm.tris.Add(num2 + 5);
				sm.tris.Add(num2 + 5);
				sm.tris.Add(num2 + 6);
				sm.tris.Add(num2 + 7);
				sm.tris.Add(num2 + 7);
				sm.tris.Add(num2 + 1);
				sm.tris.Add(num2 + 8);
				sm.tris.Add(num2 + 1);
				sm.tris.Add(num2 + 3);
				sm.tris.Add(num2 + 8);
				sm.tris.Add(num2 + 3);
				sm.tris.Add(num2 + 5);
				sm.tris.Add(num2 + 8);
				sm.tris.Add(num2 + 5);
				sm.tris.Add(num2 + 7);
				sm.tris.Add(num2 + 8);
				num2 += 9;
			}
			sm.FinalizeMesh(MeshParts.Verts | MeshParts.Tris, false);
		}

		public override void Regenerate() {
			if (pawnFog == null) {
				pawnFog = base.Map.getMapComponentSeenFog();
			}

			if (pawnFog != null) {
				LayerSubMesh subMesh = base.GetSubMesh(MatBases.FogOfWar);
				bool firstGeneration;
				if (subMesh.mesh.vertexCount == 0) {
					firstGeneration = true;

					subMesh.mesh.MarkDynamic();

					MakeBaseGeometry(this.section, subMesh, AltitudeLayer.FogOfWar);

					//subMesh.colors = new List<Color32>(subMesh.mesh.vertexCount);

					if (prefEnableFade) {
						targetAlphas = new byte[subMesh.mesh.vertexCount];
						alphaChangeTick = new long[subMesh.mesh.vertexCount];
						meshColors = new Color32[subMesh.mesh.vertexCount];
					}
				} else {
					firstGeneration = false;
				}

				int colorIdx = 0;

				bool[] fogGrid = base.Map.fogGrid.fogGrid;
				int baseIdx = pawnFog.getBaseIdx(Faction.OfPlayer);
				int[] factionsShownGrid = pawnFog.factionsShownCells;

				bool[] knownGrid = pawnFog.knownCells;

				int mapSizeX = base.Map.Size.x;

				CellRect cellRect = this.section.CellRect;
				int mapHeight = base.Map.Size.z - 1;
				int mapWidth = mapSizeX - 1;
				
				
				bool hasFoggedVerts = false;
				long fogTransitionTick = Find.TickManager.TicksGame * prefFadeSpeedMult;

				int cellIdx;
				int cellIdxN;
				int cellIdxS;
				int cellIdxE;
				int cellIdxW;
				int cellIdxSW;
				int cellIdxNW;
				int cellIdxNE;
				int cellIdxSE;

				bool cellKnown;
				bool adjCellKnown;

				byte alpha;
				for (int x = cellRect.minX; x <= cellRect.maxX; x++) {
					for (int z = cellRect.minZ; z <= cellRect.maxZ; z++) {
						cellIdx = z * mapSizeX + x;
						if (!fogGrid[cellIdx]) {
							if (factionsShownGrid[baseIdx + cellIdx] == 0) {
								cellKnown = knownGrid[cellIdx];
								for (int n = 0; n < 9; n++) {
									this.vertsNotShown[n] = true;
									this.vertsSeen[n] = cellKnown;
								}
								if (cellKnown) {
									cellIdxN = (z + 1) * mapSizeX + x;
									cellIdxS = (z - 1) * mapSizeX + x;
									cellIdxE = z * mapSizeX + (x + 1);
									cellIdxW = z * mapSizeX + (x - 1);
									cellIdxSW = (z - 1) * mapSizeX + (x - 1);
									cellIdxNW = (z + 1) * mapSizeX + (x - 1);
									cellIdxNE = (z + 1) * mapSizeX + (x + 1);
									cellIdxSE = (z - 1) * mapSizeX + (x + 1);

									if (z < mapHeight && !knownGrid[cellIdxN]) {
										this.vertsSeen[2] = false;
										this.vertsSeen[3] = false;
										this.vertsSeen[4] = false;
									}
									if (z > 0 && !knownGrid[cellIdxS]) {
										this.vertsSeen[6] = false;
										this.vertsSeen[7] = false;
										this.vertsSeen[0] = false;
									}
									if (x < mapWidth && !knownGrid[cellIdxE]) {
										this.vertsSeen[4] = false;
										this.vertsSeen[5] = false;
										this.vertsSeen[6] = false;
									}
									if (x > 0 && !knownGrid[cellIdxW]) {
										this.vertsSeen[0] = false;
										this.vertsSeen[1] = false;
										this.vertsSeen[2] = false;
									}
									if (z > 0 && x > 0 && !knownGrid[cellIdxSW]) {
										this.vertsSeen[0] = false;
									}
									if (z < mapHeight && x > 0 && !knownGrid[cellIdxNW]) {
										this.vertsSeen[2] = false;
									}
									if (z < mapHeight && x < mapWidth && !knownGrid[cellIdxNE]) {
										this.vertsSeen[4] = false;
									}
									if (z > 0 && x < mapWidth && !knownGrid[cellIdxSE]) {
										this.vertsSeen[6] = false;
									}
								}
							} else {
								for (int l = 0; l < 9; l++) {
									this.vertsNotShown[l] = false;
									this.vertsSeen[l] = false;
								}

								cellIdxN = (z + 1) * mapSizeX + x;
								cellIdxS = (z - 1) * mapSizeX + x;
								cellIdxE = z * mapSizeX + (x + 1);
								cellIdxW = z * mapSizeX + (x - 1);
								cellIdxSW = (z - 1) * mapSizeX + (x - 1);
								cellIdxNW = (z + 1) * mapSizeX + (x - 1);
								cellIdxNE = (z + 1) * mapSizeX + (x + 1);
								cellIdxSE = (z - 1) * mapSizeX + (x + 1);

								if (z < mapHeight && factionsShownGrid[baseIdx + cellIdxN] == 0) {
									adjCellKnown = knownGrid[cellIdxN];
									this.vertsNotShown[2] = true;
									this.vertsSeen[2] = adjCellKnown;
									this.vertsNotShown[3] = true;
									this.vertsSeen[3] = adjCellKnown;
									this.vertsNotShown[4] = true;
									this.vertsSeen[4] = adjCellKnown;
								}
								if (z > 0 && factionsShownGrid[baseIdx + cellIdxS] == 0) {
									adjCellKnown = knownGrid[cellIdxS];
									this.vertsNotShown[6] = true;
									this.vertsSeen[6] = adjCellKnown;
									this.vertsNotShown[7] = true;
									this.vertsSeen[7] = adjCellKnown;
									this.vertsNotShown[0] = true;
									this.vertsSeen[0] = adjCellKnown;
								}
								if (x < mapWidth && factionsShownGrid[baseIdx + cellIdxE] == 0) {
									adjCellKnown = knownGrid[cellIdxE];
									this.vertsNotShown[4] = true;
									this.vertsSeen[4] = adjCellKnown;
									this.vertsNotShown[5] = true;
									this.vertsSeen[5] = adjCellKnown;
									this.vertsNotShown[6] = true;
									this.vertsSeen[6] = adjCellKnown;
								}
								if (x > 0 && factionsShownGrid[baseIdx + cellIdxW] == 0) {
									adjCellKnown = knownGrid[cellIdxW];
									this.vertsNotShown[0] = true;
									this.vertsSeen[0] = adjCellKnown;
									this.vertsNotShown[1] = true;
									this.vertsSeen[1] = adjCellKnown;
									this.vertsNotShown[2] = true;
									this.vertsSeen[2] = adjCellKnown;
								}
								if (z > 0 && x > 0 && factionsShownGrid[baseIdx + cellIdxSW] == 0) {
									adjCellKnown = knownGrid[cellIdxSW];
									this.vertsNotShown[0] = true;
									this.vertsSeen[0] = adjCellKnown;
								}
								if (z < mapHeight && x > 0 && factionsShownGrid[baseIdx + cellIdxNW] == 0) {
									adjCellKnown = knownGrid[cellIdxNW];
									this.vertsNotShown[2] = true;
									this.vertsSeen[2] = adjCellKnown;
								}
								if (z < mapHeight && x < mapWidth && factionsShownGrid[baseIdx + cellIdxNE] == 0) {
									adjCellKnown = knownGrid[cellIdxNE];
									this.vertsNotShown[4] = true;
									this.vertsSeen[4] = adjCellKnown;
								}
								if (z > 0 && x < mapWidth && factionsShownGrid[baseIdx + cellIdxSE] == 0) {
									adjCellKnown = knownGrid[cellIdxSE];
									this.vertsNotShown[6] = true;
									this.vertsSeen[6] = adjCellKnown;
								}
							}
						} else {
							for (int k = 0; k < 9; k++) {
								this.vertsNotShown[k] = true;
								this.vertsSeen[k] = false;
							}
						}
						for (int m = 0; m < 9; m++) {
							if (this.vertsNotShown[m]) {
								if (vertsSeen[m]) {
									alpha = 127;
								} else {
									alpha = 255;
								}

								hasFoggedVerts = true;
							} else { 
								alpha = 0;
							}

							if (!prefEnableFade || firstGeneration) {
								if (firstGeneration || meshColors[colorIdx].a != alpha) {
									meshColors[colorIdx] = new Color32(255, 255, 255, alpha);
								}
								if (prefEnableFade) {
									targetAlphas[colorIdx] = alpha;
									alphaChangeTick[colorIdx] = fogTransitionTick;
								}
							} else if (targetAlphas[colorIdx] != alpha) {
								activeFogTransitions = true;
								targetAlphas[colorIdx] = alpha;
								alphaChangeTick[colorIdx] = fogTransitionTick;
							}

							colorIdx++;
						}
					}
				}

				if (!prefEnableFade || firstGeneration) {
					if (hasFoggedVerts) {
						subMesh.disabled = false;
						subMesh.mesh.colors32 = meshColors;
					} else {
						subMesh.disabled = true;
					}
				}
			}
		}

		public override void DrawLayer() {
			if (prefEnableFade && this.Visible && activeFogTransitions) {
				long fogTransitionTick = Find.TickManager.TicksGame * prefFadeSpeedMult;

				bool alphaUpdated = false;

				bool hasFoggedVerts = false;

				Color32[] colors = meshColors;

				byte alpha;
				byte targetAlpha;
				for (int i = 0; i < targetAlphas.Length; i++) {
					targetAlpha = targetAlphas[i];
					alpha = colors[i].a;
					if (alpha > targetAlpha) {
						alphaUpdated = true;
						alpha = (byte) Mathf.Max(targetAlpha, alpha - (fogTransitionTick - alphaChangeTick[i]));
						colors[i] = new Color32(255, 255, 255, alpha);
					} else if (alpha < targetAlpha) {
						alphaUpdated = true;
						alpha = (byte) Mathf.Min(targetAlpha, alpha + (fogTransitionTick - alphaChangeTick[i]));
						colors[i] = new Color32(255, 255, 255, alpha);
					}
					if (alpha != 0) {
						hasFoggedVerts = true;
					}
				}
				if (alphaUpdated) {
					LayerSubMesh subMesh = base.GetSubMesh(MatBases.FogOfWar);

					if (hasFoggedVerts) {
						subMesh.disabled = false;
						subMesh.mesh.colors32 = colors;
					} else {
						subMesh.disabled = true;
					}
				} else {
					activeFogTransitions = false;
				}
			}

			base.DrawLayer();
		}
	}
}
