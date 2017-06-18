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
using RimWorldRealFoW.Utils;
using Verse;

namespace RimWorldRealFoW.ThingComps.ThingSubComps {
	public class CompComponentsPositionTracker : ThingSubComp {
		private static readonly IntVec3 iv3Invalid = IntVec3.Invalid;
		private static readonly Rot4 r4Invalid = Rot4.Invalid;

		private IntVec2 size;

		private IntVec3 lastPosition;
		private Rot4 lastRotation;
		private bool isOneCell;
		
		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private CompHideFromPlayer compHideFromPlayer;
		private CompAffectVision compAffectVision;

		private bool setupDone = false;
		private bool calculated = false;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			setupDone = true;

			ThingDef def = parent.def;
			size = def.size;
			isOneCell = size.z == 1 && size.x == 1;
			CompMainComponent mainComp = (CompMainComponent) parent.TryGetComp(CompMainComponent.COMP_DEF);
			if (mainComp != null) {
				compHideFromPlayer = mainComp.compHideFromPlayer;
			}
			compAffectVision = parent.TryGetComp<CompAffectVision>();

			lastPosition = iv3Invalid;
			lastRotation = r4Invalid;

			updatePosition();
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			updatePosition();
		}

		public override void CompTick() {
			base.CompTick();

			// Check 5 times per seconds for position and rotation change (nothing should be so fast, but rotation has no cap).
			int currentTick = Find.TickManager.TicksGame;
			if ((currentTick % 12) == 0) {
				updatePosition();
			}
		}

		public void updatePosition() {
			if (!setupDone) {
				return;
			}

			Thing thing = base.parent;
			IntVec3 newPosition = thing.Position;
			Rot4 newRotation = thing.Rotation;
			if (thing != null && thing.Spawned && thing.Map != null && newPosition != iv3Invalid && (isOneCell || newRotation != r4Invalid) && 
					(compHideFromPlayer != null || compAffectVision != null)) {
				if (map != thing.Map) {
					map = thing.Map;
					mapCompSeenFog = thing.Map.getMapComponentSeenFog();

				} else if (mapCompSeenFog == null) {
					mapCompSeenFog = thing.Map.getMapComponentSeenFog();
				}

				if (mapCompSeenFog == null) {
					return;
				}
				
				if (!calculated || newPosition != lastPosition || (!isOneCell && newRotation != lastRotation)) {
					calculated = true;

					if (isOneCell) {
						if (compHideFromPlayer != null) {
							mapCompSeenFog.deregisterCompHideFromPlayerPosition(compHideFromPlayer, lastPosition.x, lastPosition.z);
							mapCompSeenFog.registerCompHideFromPlayerPosition(compHideFromPlayer, newPosition.x, newPosition.z);
						}
						if (compAffectVision != null) {
							mapCompSeenFog.deregisterCompAffectVisionPosition(compAffectVision, lastPosition.x, lastPosition.z);
							mapCompSeenFog.registerCompAffectVisionPosition(compAffectVision, newPosition.x , newPosition.z);
						}

					} else {
						int z;
						int x;
						if (lastPosition != iv3Invalid && lastRotation != r4Invalid) {
							CellRect cellRect = GenAdj.OccupiedRect(lastPosition, lastRotation, size);
							for (z = cellRect.minZ; z <= cellRect.maxZ; z++) {
								for (x = cellRect.minX; x <= cellRect.maxX; x++) {
									if (compHideFromPlayer != null) {
										mapCompSeenFog.deregisterCompHideFromPlayerPosition(compHideFromPlayer, x, z);
									}
									if (compAffectVision != null) {
										mapCompSeenFog.deregisterCompAffectVisionPosition(compAffectVision, x, z);
									}
								}
							}
						}

						if (newPosition != iv3Invalid && newRotation != r4Invalid) {
							CellRect cellRect = GenAdj.OccupiedRect(newPosition, newRotation, size);
							for (z = cellRect.minZ; z <= cellRect.maxZ; z++) {
								for (x = cellRect.minX; x <= cellRect.maxX; x++) {
									if (compHideFromPlayer != null) {
										mapCompSeenFog.registerCompHideFromPlayerPosition(compHideFromPlayer, x, z);
									}
									if (compAffectVision != null) {
										mapCompSeenFog.registerCompAffectVisionPosition(compAffectVision, x, z);
									}
								}
							}
						}
					}

					lastPosition = newPosition;
					if (size.x != 1 || size.z != 1) {
						lastRotation = newRotation;
					}
				}
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);

			if (mapCompSeenFog != null && (compHideFromPlayer != null || compAffectVision != null)) {
				if (isOneCell) {
					mapCompSeenFog.deregisterCompHideFromPlayerPosition(compHideFromPlayer, lastPosition.x, lastPosition.z);
					mapCompSeenFog.deregisterCompAffectVisionPosition(compAffectVision, lastPosition.x, lastPosition.z);

				} else if (lastPosition != iv3Invalid && lastRotation != r4Invalid) {
					int z;
					int x;
					CellRect cellRect = GenAdj.OccupiedRect(lastPosition, lastRotation, size);
					for (z = cellRect.minZ; z <= cellRect.maxZ; z++) {
						for (x = cellRect.minX; x <= cellRect.maxX; x++) {
							if (compHideFromPlayer != null) {
								mapCompSeenFog.deregisterCompHideFromPlayerPosition(compHideFromPlayer, x, z);
							}
							if (compAffectVision != null) {
								mapCompSeenFog.deregisterCompAffectVisionPosition(compAffectVision, x, z);
							}
						}
					}
				}
			}
		}
	}
}
