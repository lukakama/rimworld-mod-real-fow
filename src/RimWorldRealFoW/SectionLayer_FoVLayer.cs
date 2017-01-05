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
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorldRealFoW {

	public class SectionLayer_FoVLayer : SectionLayer {
		private MapComponentSeenFog pawnFog;
		private Map map;

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
			this.relevantChangeTypes = SectionLayer_FoVLayer.mapMeshFlag;
		}

		private bool[] vertsCovered = new bool[9];
		private bool[] vertsRevealed = new bool[9];

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
			for (int i = cellRect.minX; i <= cellRect.maxX; i++) {
				for (int j = cellRect.minZ; j <= cellRect.maxZ; j++) {
					sm.verts.Add(new Vector3((float) i, y, (float) j));
					sm.verts.Add(new Vector3((float) i, y, (float) j + 0.5f));
					sm.verts.Add(new Vector3((float) i, y, (float) (j + 1)));
					sm.verts.Add(new Vector3((float) i + 0.5f, y, (float) (j + 1)));
					sm.verts.Add(new Vector3((float) (i + 1), y, (float) (j + 1)));
					sm.verts.Add(new Vector3((float) (i + 1), y, (float) j + 0.5f));
					sm.verts.Add(new Vector3((float) (i + 1), y, (float) j));
					sm.verts.Add(new Vector3((float) i + 0.5f, y, (float) j));
					sm.verts.Add(new Vector3((float) i + 0.5f, y, (float) j + 0.5f));
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
			if (map != section.map) {
				map = section.map;
				pawnFog = base.Map.GetComponent<MapComponentSeenFog>();
			}
			if (pawnFog == null) {
				pawnFog = base.Map.GetComponent<MapComponentSeenFog>();
			}

			if (pawnFog != null) {
				LayerSubMesh subMesh = base.GetSubMesh(MatBases.FogOfWar);
				if (subMesh.mesh.vertexCount == 0) {
					MakeBaseGeometry(this.section, subMesh, AltitudeLayer.FogOfWar);
				}

				int[] shownGrid = pawnFog.getShownCells(Faction.OfPlayer);
				bool[] revealedGrid = pawnFog.revealedCells;

				CellRect cellRect = this.section.CellRect;
				int num = base.Map.Size.z - 1;
				int num2 = base.Map.Size.x - 1;
				subMesh.colors = new List<Color32>(subMesh.mesh.vertexCount);
				bool flag = false;
				CellIndices cellIndices = base.Map.cellIndices;

				for (int i = cellRect.minX; i <= cellRect.maxX; i++) {
					for (int j = cellRect.minZ; j <= cellRect.maxZ; j++) {
						if (shownGrid[cellIndices.CellToIndex(i, j)] == 0) {
							bool revealed = revealedGrid[cellIndices.CellToIndex(i, j)];
							for (int k = 0; k < 9; k++) {
								this.vertsCovered[k] = true;
								this.vertsRevealed[k] = revealed;
							}
						} else {
							for (int l = 0; l < 9; l++) {
								this.vertsCovered[l] = false;
								this.vertsRevealed[l] = false;
							}
							if (j < num && shownGrid[cellIndices.CellToIndex(i, j + 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i, j + 1)];
								this.vertsCovered[2] = true;
								this.vertsRevealed[2] = revealed;
								this.vertsCovered[3] = true;
								this.vertsRevealed[3] = revealed;
								this.vertsCovered[4] = true;
								this.vertsRevealed[4] = revealed;
							}
							if (j > 0 && shownGrid[cellIndices.CellToIndex(i, j - 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i, j - 1)];
								this.vertsCovered[6] = true;
								this.vertsRevealed[6] = revealed;
								this.vertsCovered[7] = true;
								this.vertsRevealed[7] = revealed;
								this.vertsCovered[0] = true;
								this.vertsRevealed[0] = revealed;
							}
							if (i < num2 && shownGrid[cellIndices.CellToIndex(i + 1, j)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i + 1, j)];
								this.vertsCovered[4] = true;
								this.vertsRevealed[4] = revealed;
								this.vertsCovered[5] = true;
								this.vertsRevealed[5] = revealed;
								this.vertsCovered[6] = true;
								this.vertsRevealed[6] = revealed;
							}
							if (i > 0 && shownGrid[cellIndices.CellToIndex(i - 1, j)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i - 1, j)];
								this.vertsCovered[0] = true;
								this.vertsRevealed[0] = revealed;
								this.vertsCovered[1] = true;
								this.vertsRevealed[1] = revealed;
								this.vertsCovered[2] = true;
								this.vertsRevealed[2] = revealed;
							}
							if (j > 0 && i > 0 && shownGrid[cellIndices.CellToIndex(i - 1, j - 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i - 1, j - 1)];
								this.vertsCovered[0] = true;
								this.vertsRevealed[0] = revealed;
							}
							if (j < num && i > 0 && shownGrid[cellIndices.CellToIndex(i - 1, j + 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i - 1, j + 1)];
								this.vertsCovered[2] = true;
								this.vertsRevealed[2] = revealed;
							}
							if (j < num && i < num2 && shownGrid[cellIndices.CellToIndex(i + 1, j + 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i + 1, j + 1)];
								this.vertsCovered[4] = true;
								this.vertsRevealed[4] = revealed;
							}
							if (j > 0 && i < num2 && shownGrid[cellIndices.CellToIndex(i + 1, j - 1)] == 0) {
								bool revealed = revealedGrid[cellIndices.CellToIndex(i + 1, j - 1)];
								this.vertsCovered[6] = true;
								this.vertsRevealed[6] = revealed;
							}
						}
						for (int m = 0; m < 9; m++) {
							byte a;
							if (this.vertsCovered[m]) {
								if (vertsRevealed[m]) {
									a = 128;
								} else {
									a = 255;
								}
								flag = true;
							} else {
								a = 0;
							}
							subMesh.colors.Add(new Color32(255, 255, 255, a));
						}
					}
				}
				if (flag) {
					subMesh.disabled = false;
					subMesh.FinalizeMesh(MeshParts.Colors, false);
				} else {
					subMesh.disabled = true;
				}
			}
		}
	}
}
