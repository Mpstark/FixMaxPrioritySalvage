using System;
using System.Reflection;
using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using TMPro;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace FixMaxPrioritySalvage
{
    [HarmonyPatch(typeof(Contract))]
    [HarmonyPatch("FinalPrioritySalvageCount", PropertyMethod.Getter)]
    public static class Contract_FinalPrioritySalvageCount_Patch
    {
        public static void Postfix(Contract __instance, ref int __result)
        {
            __result = Math.Min(7, __result);
        }
    }

    [HarmonyPatch(typeof(SGContractsListItem), "Init")]
    public static class SGContractsListItem_Init_Patch
    {
        public static void Postfix(SGContractsListItem __instance, Contract contract, SimGameState sim)
        {
            var setFieldText = Traverse.Create(__instance).Method("setFieldText", new[] {typeof(TextMeshProUGUI), typeof(string)});

            var maxSalvage = contract.SalvagePotential + sim.Constants.Finances.ContractFloorSalvageBonus;
            var maxPrioritySalvage = Math.Min(7, Mathf.FloorToInt(maxSalvage * sim.Constants.Salvage.PrioritySalvageModifier));

            var contractMaxSalvage = Traverse.Create(__instance).Field("contractMaxSalvage").GetValue<TextMeshProUGUI>();
            setFieldText.GetValue(contractMaxSalvage, $"{maxPrioritySalvage} / {maxSalvage}");

            var actualSalvage = Mathf.FloorToInt(contract.Override.negotiatedSalvage * contract.SalvagePotential) + sim.Constants.Finances.ContractFloorSalvageBonus;
            var actualPrioritySalvage = Math.Min(7, Mathf.FloorToInt(actualSalvage * sim.Constants.Salvage.PrioritySalvageModifier));

            var contractSalvage = Traverse.Create(__instance).Field("contractSalvage").GetValue<TextMeshProUGUI>();
            setFieldText.GetValue(contractSalvage, $"{actualPrioritySalvage} / {actualSalvage}");
        }
    }

    [HarmonyPatch(typeof(Briefing), "SetContractInfo")]
    public static class Briefing_SetContractInfo_Patch
    {
        public static void Postfix(Briefing __instance, Contract contract, SimGameState Sim)
        {
            if (Sim == null || contract.ContractType == ContractType.ArenaSkirmish)
                return;

            var actualSalvage = contract.SalvagePotential;
            if (actualSalvage > 0)
            {
                actualSalvage = Mathf.FloorToInt(contract.Override.salvagePotential * contract.PercentageContractSalvage);
                actualSalvage += Sim.Constants.Finances.ContractFloorSalvageBonus;
            }

            var actualPrioritySalvage = Math.Min(7, Mathf.FloorToInt(actualSalvage * Sim.Constants.Salvage.PrioritySalvageModifier));

            Traverse.Create(__instance).Field("contractSalvageField").GetValue<TextMeshProUGUI>().text = $"{actualPrioritySalvage} / {actualSalvage}";
        }
    }

    [HarmonyPatch(typeof(LanceContractDetailsWidget), "PopulateContract")]
    public static class LanceContractDetailsWidget_PopulateContract_Patch
    {
        public static void Postfix(LanceContractDetailsWidget __instance, LanceConfiguratorPanel LC, Contract contract)
        {
            var actualSalvage = contract.SalvagePotential;
            if (actualSalvage > 0)
            {
                actualSalvage = Mathf.FloorToInt(contract.Override.salvagePotential * contract.PercentageContractSalvage);
                actualSalvage += LC.Sim.Constants.Finances.ContractFloorSalvageBonus;
            }

            var actualPrioritySalvage = Math.Min(7, Mathf.FloorToInt(actualSalvage * LC.Sim.Constants.Salvage.PrioritySalvageModifier));

            Traverse.Create(__instance).Field("MetaMaxSalvageField").GetValue<TextMeshProUGUI>().text = $"{actualPrioritySalvage} / {actualSalvage}";
        }
    }

    [HarmonyPatch(typeof(SGContractsWidget), "PopulateContract")]
    public static class SGContractsWidget_PopulateContract_Patch
    {
        public static void Postfix(SGContractsWidget __instance, Contract contract)
        {
            var sim = Traverse.Create(__instance).Property("Sim").GetValue<SimGameState>();

            var actualSalvage = contract.SalvagePotential;
            if (actualSalvage > 0)
            {
                if (contract.Override.contractDisplayStyle != ContractDisplayStyle.BaseCampaignNormal) actualSalvage = Mathf.FloorToInt(contract.Override.salvagePotential * contract.PercentageContractSalvage);

                actualSalvage += sim.Constants.Finances.ContractFloorSalvageBonus;
            }

            var actualPrioritySalvage = Math.Min(7, Mathf.FloorToInt(actualSalvage * sim.Constants.Salvage.PrioritySalvageModifier));

            Traverse.Create(__instance).Field("MetaMaxSalvageField").GetValue<TextMeshProUGUI>().text = $"{actualPrioritySalvage} / {actualSalvage}";

            // for some reason this is broken, I don't know exactly why this is yielding incorrect values, consider it's identical (I think)
            // to the decompiled version. There must be something I'm missing.
            //Traverse.Create(__instance).Field("MetaStorySalvageField").GetValue<TextMeshProUGUI>().text = $"{actualPrioritySalvage} / {actualSalvage}";
        }
    }

    [HarmonyPatch(typeof(SGContractsWidget), "UpdateCurrentValues")]
    public static class SGContractsWidget_UpdateCurrentValues_Patch
    {
        public static void Postfix(SGContractsWidget __instance)
        {
            var sim = Traverse.Create(__instance).Property("Sim").GetValue<SimGameState>();
            var contract = __instance.SelectedContract;

            var actualSalvage = sim.Constants.Salvage.DefaultSalvagePotential;
            if (contract.Override.salvagePotential > -1)
                actualSalvage = contract.Override.salvagePotential;
            else if (contract.SalvagePotential > -1) actualSalvage = contract.SalvagePotential;
            if (actualSalvage > 0)
            {
                var negSalvageSlider = Traverse.Create(__instance).Field("NegSalvageSlider").GetValue<HBSSliderInput>();
                var negSalvage = negSalvageSlider.Value / negSalvageSlider.ValueMax;
                actualSalvage = Mathf.FloorToInt(actualSalvage * negSalvage);
                actualSalvage += sim.Constants.Finances.ContractFloorSalvageBonus;
            }

            var actualPrioritySalvage = Math.Min(7, Mathf.FloorToInt(actualSalvage * sim.Constants.Salvage.PrioritySalvageModifier));

            Traverse.Create(__instance).Field("NegSalvageCurrent").GetValue<TextMeshProUGUI>().text = $"{actualPrioritySalvage} / {actualSalvage}";
        }
    }

    public static class Patches
    {
        public static void Init()
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.FixMaxPrioritySalvage");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}