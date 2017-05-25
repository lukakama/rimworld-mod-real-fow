using RimWorld;
using RimWorldRealFoW.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimWorldRealFoW.Detours {
	public static class _WorkGiver_DoBill {
		public static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingAmount> chosen) {
			Type WorkGiver_DoBill_Type = typeof(WorkGiver_DoBill);

			chosen.Clear();
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Clear();
			if (bill.recipe.ingredients.Count == 0) {
				return true;
			}
			IntVec3 rootCell = ReflectionUtils.execStaticPrivate<IntVec3>(WorkGiver_DoBill_Type, "GetBillGiverRootCell", billGiver, pawn);
			Region rootReg = rootCell.GetRegion(pawn.Map, RegionType.Set_Passable);
			if (rootReg == null) {
				return false;
			}
			ReflectionUtils.execStaticPrivate(WorkGiver_DoBill_Type, "MakeIngredientsListInProcessingOrder", ReflectionUtils.getStaticPrivateValue<List<IngredientCount>>(WorkGiver_DoBill_Type, "ingredientsOrdered"), bill);
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings").Clear();
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "processedThings").Clear();
			bool foundAll = false;
			Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (float) (t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any((IngredientCount ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t, 1, -1, null, false) && t.fowIsVisible();
			bool billGiverIsPawn = billGiver is Pawn;
			if (billGiverIsPawn) {
				ReflectionUtils.execStaticPrivate(WorkGiver_DoBill_Type, "AddEveryMedicineToRelevantThings", pawn, billGiver, ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), baseValidator, pawn.Map);
				if (ReflectionUtils.execStaticPrivate<bool>(WorkGiver_DoBill_Type, "TryFindBestBillIngredientsInSet", ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), bill, chosen)) {
					return true;
				}
			}
			TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
			RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, false);
			int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
			int regionsProcessed = 0;
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "processedThings").AddRange(ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"));
			RegionProcessor regionProcessor = delegate (Region r) {
				List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
				for (int i = 0; i < list.Count; i++) {
					Thing thing = list[i];
					if (!ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "processedThings").Contains(thing)) {
						if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn)) {
							if (baseValidator(thing) && (!thing.def.IsMedicine || !billGiverIsPawn)) {
								ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Add(thing);
								ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "processedThings").Add(thing);
							}
						}
					}
				}
				regionsProcessed++;
				if (ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Count > 0 && regionsProcessed > adjacentRegionsAvailable) {
					Comparison<Thing> comparison = delegate (Thing t1, Thing t2) {
						float num = (float) (t1.Position - rootCell).LengthHorizontalSquared;
						float value = (float) (t2.Position - rootCell).LengthHorizontalSquared;
						return num.CompareTo(value);
					};
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Sort(comparison);
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings").AddRange(ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings"));
					ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Clear();
					if (ReflectionUtils.execStaticPrivate<bool>(WorkGiver_DoBill_Type, "TryFindBestBillIngredientsInSet", ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings"), bill, chosen)) {
						foundAll = true;
						return true;
					}
				}
				return false;
			};
			RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "relevantThings").Clear();
			ReflectionUtils.getStaticPrivateValue<List<Thing>>(WorkGiver_DoBill_Type, "newRelevantThings").Clear();
			return foundAll;
		}
	}
}
