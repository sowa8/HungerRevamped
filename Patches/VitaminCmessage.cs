#nullable disable
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gameplay;
using MelonLoader;
using System;
using System.Collections;
using UnityEngine;

namespace HungerRevamped
{
    internal static class VitaminCDebugPatch
    {
        [HarmonyPatch(
            typeof(PlayerManager),
            nameof(PlayerManager.OnEatingComplete),
            new Type[] { typeof(bool), typeof(bool), typeof(float) })]
        internal static class OnEatingCompletePatch
        {
            private static void Postfix(
                bool success,
                bool playerCancel,
                float progress)
            {
                if (!MenuSettings.settings.PrintVitaminC)
                    return;

                if (!success || playerCancel)
                    return;

                MelonCoroutines.Start(PrintVitaminCLater());
            }

            private static IEnumerator PrintVitaminCLater()
            {
                yield return new WaitForSeconds(1f);

                ScurvyManager scurvy = GameManager.GetScurvyComponent();

                if (scurvy == null)
                {
                    MelonLogger.Msg("[VitaminC] ScurvyManager is null.");
                    yield break;
                }

                float vitaminC = scurvy.GetVitaminCNormalized();

                MelonLogger.Msg(
                    "[VitaminC] Current level: " +
                    vitaminC.ToString("P1"));
            }
        }
    }
}