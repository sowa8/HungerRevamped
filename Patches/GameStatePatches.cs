#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.IntBackedUnit;
using MelonLoader;

namespace HungerRevamped {
	internal static class GameStatePatches {

		internal static bool IsApplyingDeferredFoodPoisoning = false;

        /*[HarmonyPatch(typeof(Hunger), nameof(Hunger.Start))]
		private static class HungerStart {
			private static void Prefix(Hunger __instance)
			{
                //if (__instance.m_StartHasBeenCalled)
					//return;
                if (HungerRevamped.HasInstance)
                    return;

                float burnRateScale = GameManager.GetExperienceModeManagerComponent().GetCalorieBurnScale();
				__instance.m_MaxReserveCalories = Tuning.maximumHungerCalories * burnRateScale;
				__instance.m_StarvingCalorieThreshold = Tuning.hungerLevelStarving * __instance.m_MaxReserveCalories;

				HungerRevamped.Instance = new HungerRevamped(__instance);
			}
		}*/

        [HarmonyPatch(typeof(Hunger), nameof(Hunger.Start))]
        private static class HungerStart
        {
            private static void Prefix(Hunger __instance)
            {
                float burnRateScale = GameManager.GetExperienceModeManagerComponent().GetCalorieBurnScale();

                __instance.m_MaxReserveCalories =
                    Tuning.maximumHungerCalories * burnRateScale;

                __instance.m_StarvingCalorieThreshold =
                    Tuning.hungerLevelStarving * __instance.m_MaxReserveCalories;

                if (__instance.m_StartHasBeenCalled)
                    return;

                HungerRevamped.Instance = new HungerRevamped(__instance);
            }
        }

        [HarmonyPatch(typeof(Condition), "Start")]
		private static class ConditionStart {
			private static void Prefix(Condition __instance) {
                /*if (__instance.m_StartHasBeenCalled)
					return;*/
                if (HungerRevamped.HasInstance)
                    return;

                __instance.m_HPDecreasePerDayFromStarving = 0f;
			}
		}

		[HarmonyPatch(typeof(Hunger), "Update")]
		private static class UpdateHungerRevampedAfterHunger {
			private static void Postfix() {
				HungerRevamped.Instance.Update();
			}
		}

		[HarmonyPatch(typeof(Freezing), "CalculateBodyTemperature")]
		private static class ApplyStoredCaloriesWarmthModifier {
			private static void Postfix(ref float __result) {
				__result += HungerRevamped.Instance.GetStoredCaloriesWarmthBonus();
			}
		}

		[HarmonyPatch(typeof(PlayerManager), "CalculateModifiedCalorieBurnRate")]
		private static class ModifyCalorieConsumption {
			private static void Postfix(ref float __result) {
				__result *= HungerRevamped.Instance.GetCalorieBurnRateMultiplier();
			}
		}

		[HarmonyPatch(typeof(PlayerManager), "FirstAidConsumed")]
		private static class ClearDeferredFoodPoisoningsWhenConsumingAntibiotics {
			private static void Postfix(GearItem gi) {
				if (gi && gi.m_FirstAidItem && gi.m_FirstAidItem.m_ProvidesAntibiotics) {
					HungerRevamped.Instance.OnPlayerTookAntibiotics();
				}
			}
		}

		[HarmonyPatch(typeof(WellFed), nameof(WellFed.Update))]
		private static class WellFedNewUpdate
		{
			private static bool Prefix(WellFed __instance)
			{
				__instance.m_MaxConditionBonusPercent = 0f;
				if (GameManager.m_IsPaused)
					return false;

				float currentStoredCalories = HungerRevamped.Instance.GetStoredCalories();

				// 1. KONTROLA BONUSU DO UDŹWIGU (Włącza się powyżej 12 000 kcal)
				float carryBonus = 0f;
				if (currentStoredCalories >= 12000f)
				{
					carryBonus = HungerRevamped.Instance.GetCarryBonus(); 
				}
				__instance.m_CarryCapacityBonus = ItemWeight.FromKilograms(carryBonus);

				// 2. KONTROLA IKONY STATUSU WELL FED (Pojawia się powyżej 3 000 kcal)
				bool active = __instance.HasWellFed();

				if (!active && currentStoredCalories >= 3000f)
				{
					__instance.WellFedStart(__instance.GetCauseLocalizationId(), true, false);
				} 
				else if (active && currentStoredCalories < 3000f)
				{
					__instance.WellFedEnd();
				}
				return false;
			}
		}
	}
}
