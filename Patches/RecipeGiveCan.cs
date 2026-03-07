#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Cooking;
using Il2CppTLD.Gear;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

// This code gives you back a recycled can after you cooked a Recipe which requires a canned food.

namespace HungerRevamped.Patches
{
    public static class CookingStateTracker
    {
        public static RecipeData PendingRecipe;
    }

    [HarmonyPatch(typeof(Panel_Cooking), "OnCookRecipe")]
    public static class PanelCooking_OnCookRecipe_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(RecipeData recipe)
        {
            if (recipe == null)
            {
                return;
            }

            CookingStateTracker.PendingRecipe = recipe;
        }
    }

    [HarmonyPatch(typeof(Panel_Cooking), "OnRecipePrepSuccess")]
    public static class PanelCooking_OnRecipePrepSuccess_Patch
    {
        private static readonly List<string> RecipesThatReturnCan =
            new List<string>()
            {
                "RECIPE_PiePeach",
                "RECIPE_PorridgeFruit",
                "RECIPE_PancakeAcorn",
                "RECIPE_StewVegetables"
            };

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                RecipeData recipe = CookingStateTracker.PendingRecipe;

                if (recipe == null)
                {
                    return;
                }

                string recipeAssetName = recipe.name;

                CookingStateTracker.PendingRecipe = null;

                if (string.IsNullOrEmpty(recipeAssetName))
                {
                    return;
                }

                bool shouldSpawnCan = false;

                foreach (string allowedRecipe in RecipesThatReturnCan)
                {
                    if (recipeAssetName.Equals(
                        allowedRecipe,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        shouldSpawnCan = true;
                        break;
                    }
                }

                if (!shouldSpawnCan)
                {
                    return;
                }

                PlayerManager playerManager =
                    GameManager.GetPlayerManagerComponent();

                if (playerManager == null)
                {
                    return;
                }

                SpawnGear(playerManager, "RecycledCan", 1);

                DebugHelper.Log("RecycledCan granted after recipe: " + recipeAssetName);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Error in OnRecipePrepSuccess patch: " + e);
            }
        }

        private static void SpawnGear(
            PlayerManager playerManager,
            string nameFragment,
            int count)
        {
            GearItemData[] allGear =
                Resources.FindObjectsOfTypeAll<GearItemData>();

            if (allGear == null)
            {
                return;
            }

            GearItemData found = null;

            foreach (GearItemData gearData in allGear)
            {
                if (gearData == null)
                {
                    continue;
                }

                if (gearData.name.IndexOf(
                        nameFragment,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = gearData;
                    break;
                }
            }

            if (found == null)
            {
                DebugHelper.Log("RecycledCan GearItemData not found.");
                return;
            }

            playerManager.InstantiateItemInPlayerInventory(
                found.PrefabReference,
                count);
        }
    }

    [HarmonyPatch(typeof(Panel_Cooking), "OnCancelRecipePreparation")]
    public static class PanelCooking_OnCancelRecipePreparation_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            CookingStateTracker.PendingRecipe = null;
        }
    }
}