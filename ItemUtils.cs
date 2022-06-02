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
        private static Dictionary<string, PrefabGUID> _ItemNameToPrefabLookup = new();
        public static Dictionary<string, PrefabGUID> ItemNameToPrefabLookup => _ItemNameToPrefabLookup;

        private static GameDataSystem _system;

        [HarmonyPatch(typeof(GameDataSystem), nameof(GameDataSystem.RegisterItems))]
        [HarmonyPostfix]
        private static void GameDataSystem_RegisterItems_Postfix(GameDataSystem __instance)
        {
            _system = __instance;
            if (__instance.ItemHashLookupMap.Count() == 0) return;
            Plugin.Logger.LogInfo("Creating lookup tables...");
            RebuildLUT();
        }

        public static void RebuildLUT()
        {
            _ItemNameToPrefabLookup.Clear();
            var managed = _system.ManagedDataRegistry;
            foreach (var kv in _system.ItemHashLookupMap)
            {
                try
                {
                    var guid = kv.Key;
                    var managedData = managed.GetOrDefault<ManagedItemData>(guid);
                    if (managedData == null) continue;
                    if (!managedData.PrefabName.Contains("_Ingredient_")) continue;
                    if (managedData.PrefabName is "Item_Ingredient_Kit_Base" or "Item_Ingredient_Gem_Base") continue;
                    Plugin.Logger.LogDebug(Localization.Get(managedData.Name, false));
                    _ItemNameToPrefabLookup.Add(Localization.Get(managedData.Name, false), guid);
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogError(e);
                    // ignored
                }
            }
        }

        public static PrefabGUID GetOrRebuild(string itemName)
        {
            if (_ItemNameToPrefabLookup.ContainsKey(itemName))
                return _ItemNameToPrefabLookup[itemName];
            RebuildLUT();
            if (_ItemNameToPrefabLookup.ContainsKey(itemName))
                return _ItemNameToPrefabLookup[itemName];
            Plugin.Logger.LogError($"Error rebuilding LUT no itemname found: {itemName}");
            return PrefabGUID.Empty;
        }
    }
}