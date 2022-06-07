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
        private static List<string> _errored = new();

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
            _errored.Clear();
            var managed = _system.ManagedDataRegistry;
            foreach (var kv in _system.ItemHashLookupMap)
            {
                try
                {
                    var guid = kv.Key;
                    var managedData = managed.GetOrDefault<ManagedItemData>(guid);
                    if (managedData == null) continue;
                    if (!managedData.PrefabName.Contains("_Ingredient_") && !managedData.PrefabName.Contains("Item_Consumable_Water")) continue;
                    if (managedData.PrefabName is "Item_Ingredient_Kit_Base" or "Item_Ingredient_Gem_Base") continue;
                    var itemName = Localization.Get(managedData.Name, false);
                    Plugin.Logger.LogDebug(itemName);
                    
                    _ItemNameToPrefabLookup.Add(itemName, guid);
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
            if(!_errored.Contains(itemName))
            {
                Plugin.Logger.LogError($"Error rebuilding LUT no itemname found: {itemName}");
                _errored.Add(itemName);
            }
            return PrefabGUID.Empty;
        }
    }
}