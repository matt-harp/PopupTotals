using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using StunLocalization;

namespace PopupTotals
{
    [HarmonyPatch]
    public class ItemUtils
    {
        private static Dictionary<LocalizationKey, PrefabGUID> _LocalizationKeyToPrefabLookup = new Dictionary<LocalizationKey, PrefabGUID>();
        public static Dictionary<LocalizationKey, PrefabGUID> LocalizationKeyToPrefabLookup => _LocalizationKeyToPrefabLookup;
        
        private static Dictionary<string, PrefabGUID> _ItemNameToPrefabLookup = new Dictionary<string, PrefabGUID>();
        public static Dictionary<string, PrefabGUID> ItemNameToPrefabLookup => _ItemNameToPrefabLookup;

        [HarmonyPatch(typeof(GameDataSystem), nameof(GameDataSystem.RegisterItems))]
        [HarmonyPostfix]
        private static void GameDataSystem_RegisterItems_Postfix(GameDataSystem __instance)
        {
            if (__instance.ItemHashLookupMap.Count() == 0) return;
            Plugin.Logger.LogInfo("Creating lookup tables...");
            _ItemNameToPrefabLookup.Clear();
            _LocalizationKeyToPrefabLookup.Clear();
            var managed = __instance.ManagedDataRegistry;
            foreach (var kv in __instance.ItemHashLookupMap)
            {
                try
                {
                    var guid = kv.Key;
                    var managedData = managed.GetOrDefault<ManagedItemData>(guid);
                    if (managedData == null) continue;
                    _LocalizationKeyToPrefabLookup.Add(managedData.Name, guid);
                    _ItemNameToPrefabLookup.Add(Localization.Get(managedData.Name, false), guid);
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
        }
    }
}