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

namespace RimWorldRealFoW.ThingComps.ThingSubComps {
	public class CompHideFromPlayer : ThingSubComp {
		private static readonly IntVec3 iv3Invalid = IntVec3.Invalid;
		private static readonly Rot4 r4Invalid = Rot4.Invalid;

		private bool calculated;
		private IntVec3 lastPosition;
		private Rot4 lastRotation;
		private bool isOneCell;

		private Map map;
		private FogGrid fogGrid;
		private MapComponentSeenFog mapCompSeenFog;
		
		private CompHiddenable compHiddenable;

		private bool setupDone = false;
		private bool seenByPlayer;
		private bool isPawn;
		private IntVec2 size;

		private bool isSaveable;
		private bool saveCompressible;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);

			setupDone = true;

			calculated = false;
			lastPosition = iv3Invalid;
			lastRotation = r4Invalid;

			isPawn = parent.def.category == ThingCategory.Pawn;
			size = parent.def.size;
			isOneCell = size.z == 1 && size.x == 1;

			isSaveable = parent.def.isSaveable;
			saveCompressible = parent.def.saveCompressible;

			compHiddenable = mainComponent.compHiddenable;

			updateVisibility(false);
		}

		public override void PostExposeData() {
			base.PostExposeData();

			Scribe_Values.Look<bool>(ref this.seenByPlayer, "seenByPlayer", false, false);
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);

			updateVisibility(false);
		}

		public override void CompTick() {
			base.CompTick();

			// Check 5 times per seconds for position and rotation change (nothing should be so fast, but rotation has no cap).
			int currentTick = Find.TickManager.TicksGame;
			if ((currentTick % 12) == 0) {
				updateVisibility(false);
			}
		}

		public void forceSeen() {
			seenByPlayer = true;

			updateVisibility(true, true);
		}

		public void updateVisibility(bool forceCheck, bool forceUpdate = false) {
			if (!setupDone || Current.ProgramState == ProgramState.MapInitializing) {
				return;
			}

			Thing thing = base.parent;
			IntVec3 newPosition = thing.Position;
			Rot4 newRotation = thing.Rotation;
			if (thing != null && thing.Spawned && thing.Map != null && newPosition != iv3Invalid && (isOneCell || newRotation != r4Invalid)) {
				if (map != thing.Map) {
					map = thing.Map;
					fogGrid = map.fogGrid;
					mapCompSeenFog = thing.Map.getMapComponentSeenFog();

				} else if (mapCompSeenFog == null) {
					mapCompSeenFog = thing.Map.getMapComponentSeenFog();
				}

				if (mapCompSeenFog == null) {
					return;
				}
				
				if (forceCheck || !calculated || newPosition != lastPosition || (!isOneCell && newRotation != lastRotation)) {
					calculated = true;
					lastPosition = newPosition;
					lastRotation = newRotation;

					bool belongToPlayer = thing.Faction != null && thing.Faction.IsPlayer;

					if (mapCompSeenFog != null && !fogGrid.IsFogged(lastPosition)) {
						if (isSaveable && !saveCompressible) {
							if (!belongToPlayer) {
								if (isPawn && !hasPartShownToPlayer()) {
									compHiddenable.hide();
								} else if (!isPawn && !seenByPlayer && !hasPartShownToPlayer()) {
									compHiddenable.hide();
								} else {
									seenByPlayer = true;
									compHiddenable.show();
								}
							} else {
								seenByPlayer = true;
								compHiddenable.show();
							}
						} else if ((forceUpdate || !seenByPlayer) && thing.fowInKnownCell()) {
							seenByPlayer = true;
							compHiddenable.show();
						}
					}
				}
			}
		}

		private bool hasPartShownToPlayer() {
			Faction playerFaction = Faction.OfPlayer;
			if (isOneCell) {
				return mapCompSeenFog.isShown(playerFaction, lastPosition.x, lastPosition.z);

			} else {
				CellRect occupiedRect = GenAdj.OccupiedRect(lastPosition, lastRotation, size);

				for (int x = occupiedRect.minX; x <= occupiedRect.maxX; x++) {
					for (int z = occupiedRect.minZ; z <= occupiedRect.maxZ; z++) {
						if (mapCompSeenFog.isShown(playerFaction, x, z)) {
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
