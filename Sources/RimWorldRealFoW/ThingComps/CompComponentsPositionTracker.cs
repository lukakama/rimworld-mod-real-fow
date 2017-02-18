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
using Verse;

namespace RimWorldRealFoW.ThingComps {
	public class CompComponentsPositionTracker : ThingComp {
		public static readonly CompProperties COMP_DEF = new CompProperties(typeof(CompComponentsPositionTracker));

		IntVec2 size;

		private IntVec3 lastPosition;
		private Rot4 lastRotation;
		private bool isOneCell;
		
		private Map map;
		private MapComponentSeenFog mapCompSeenFog;

		private CompHideFromPlayer compHideFromPlayer;
		private CompAffectVision compAffectVision;

		private bool setupDone = false;
		private bool calculated = false;

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();

			setupDone = true;

			ThingDef def = parent.def;
			size = def.size;
			isOneCell = size.z == 1 && size.x == 1;

			compHideFromPlayer = parent.TryGetComp<CompHideFromPlayer>();
			compAffectVision = parent.TryGetComp<CompAffectVision>();

			lastPosition = IntVec3.Invalid;
			lastRotation = Rot4.Invalid;
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
			if (thing != null && thing.Spawned && thing.Map != null && newPosition != IntVec3.Invalid && (isOneCell || newRotation != Rot4.Invalid) && 
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
						if (lastPosition != IntVec3.Invalid && lastRotation != Rot4.Invalid) {
							CellRect cellRect = GenAdj.OccupiedRect(lastPosition, lastRotation, size);
							for (int z = cellRect.minZ; z <= cellRect.maxZ; z++) {
								for (int x = cellRect.minX; x <= cellRect.maxX; x++) {
									if (compHideFromPlayer != null) {
										mapCompSeenFog.deregisterCompHideFromPlayerPosition(compHideFromPlayer, x, z);
									}
									if (compAffectVision != null) {
										mapCompSeenFog.deregisterCompAffectVisionPosition(compAffectVision, x, z);
									}
								}
							}
						}

						if (newPosition != IntVec3.Invalid && newRotation != Rot4.Invalid) {
							CellRect cellRect = GenAdj.OccupiedRect(newPosition, newRotation, size);
							for (int z = cellRect.minZ; z <= cellRect.maxZ; z++) {
								for (int x = cellRect.minX; x <= cellRect.maxX; x++) {
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

				} else if (lastPosition != IntVec3.Invalid && lastRotation != Rot4.Invalid) {
					CellRect cellRect = GenAdj.OccupiedRect(lastPosition, lastRotation, size);
					for (int z = cellRect.minZ; z <= cellRect.maxZ; z++) {
						for (int x = cellRect.minX; x <= cellRect.maxX; x++) {
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
