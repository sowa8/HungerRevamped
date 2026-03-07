#nullable disable
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using Il2Cpp;
using MelonLoader;

namespace HungerRevamped
{
    internal static class SerializationPatches
    {
        private static readonly HungerRevampedSaveDataProxy saveDataProxy = new HungerRevampedSaveDataProxy();

        private static readonly MelonLogger.Instance logger =
            new MelonLogger.Instance(BuildInfo.ModName);

        // ================= LOAD =================

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.RestoreGlobalData))]
        private static class HungerDeserialize
        {
            private static void Postfix(string name)
            {
                DebugHelper.Log("Attempting to load " + name);

                string json = HungerRevampedMod.sdm.LoadData();

                if (string.IsNullOrEmpty(json))
                {
                    logger.Msg("Could not load!");
                    return;
                }

                DebugHelper.Log(json);
                DebugHelper.Log("Loaded Successfully");

                HungerRevampedSaveDataProxy data =
                    JsonConvert.DeserializeObject<HungerRevampedSaveDataProxy>(json);

                if (data == null)
                {
                    logger.Msg("Deserialization failed!");
                    return;
                }

                HungerRevamped hungerRevamped = HungerRevamped.Instance;

                hungerRevamped.storedCalories =
                    data.storedCalories + Tuning.defaultStoredCalories;

                hungerRevamped.wellFedHungerScore =
                    data.wellFedHungerScore;

                hungerRevamped.deferredFoodPoisonings.Clear();

                if (data.deferredFoodPoisonings != null)
                {
                    hungerRevamped.deferredFoodPoisonings
                        .AddRange(data.deferredFoodPoisonings);
                }

                Hunger hunger = GameManager.GetHungerComponent();

                hunger.m_CurrentReserveCalories =
                    Mathf.Min(hunger.m_CurrentReserveCalories,
                              hunger.m_MaxReserveCalories);

                DebugHelper.Log("Current reserve calories: " +
                    hunger.m_CurrentReserveCalories);

                DebugHelper.Log("Max reserve calories: " +
                    hunger.m_MaxReserveCalories);
            }
        }

        // ================= SAVE =================

        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveGlobalData))]
        private static class HungerSerialize
        {
            [HarmonyPostfix]
            private static void Postfix(SlotData slot)
            {
                DebugHelper.Log("Attempting to save");

                HungerRevamped hungerRevamped =
                    HungerRevamped.Instance;

                saveDataProxy.storedCalories =
                    hungerRevamped.storedCalories -
                    Tuning.defaultStoredCalories;

                saveDataProxy.wellFedHungerScore =
                    hungerRevamped.wellFedHungerScore;

                saveDataProxy.deferredFoodPoisonings =
                    hungerRevamped.deferredFoodPoisonings.ToArray();

                string json =
                    JsonConvert.SerializeObject(saveDataProxy);

                DebugHelper.Log(json);

                bool success =
                    HungerRevampedMod.sdm.Save(json);

                if (success)
                {
                    DebugHelper.Log("Success");
                }
                else
                {
                    logger.Msg("Failed to save");
                }
            }
        }
    }
}