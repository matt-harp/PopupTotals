using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using Stunlock.Localization;

namespace PopupTotals
{
    [HarmonyPatch]
    public class ItemUtils
    {
        private static Dictionary<string, PrefabGUID> ItemNameToPrefabLookup { get; } = new();

        private static GameDataSystem System { get; set; }
        private static List<string> Errored { get; } = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameDataSystem), nameof(GameDataSystem.RegisterItems))]
        private static void GameDataSystem_RegisterItems_Postfix(ref GameDataSystem __instance)
        {
            System = __instance;
            if (__instance.ItemHashLookupMap.Count() == 0) return;
            Plugin.Logger.LogInfo("Creating lookup tables...");
            RebuildLut();
        }

        private static void RebuildLut()
        {
            ItemNameToPrefabLookup.Clear();
            Errored.Clear();
            var managed = System.ManagedDataRegistry;
            foreach (var kv in System.ItemHashLookupMap)
            {
                try
                {
                    var guid = kv.Key;
                    var managedData = managed.GetOrDefault<ManagedItemData>(guid);
                    if (managedData == null) continue;
                    if (!managedData.PrefabName.Contains("_Ingredient_") && !managedData.PrefabName.Contains("Item_Consumable") && !managedData.PrefabName.Contains("Item_Building_Plants")) continue;
                    if (managedData.PrefabName is "Item_Ingredient_Kit_Base" or "Item_Ingredient_Gem_Base") continue;
                    var itemName = Localization.Get(managedData.Name, false);
                    Plugin.Logger.LogDebug($"Item: {itemName}, PrefabName: {managedData.PrefabName}, PrefabGUID: {guid.ToString()}");

                    ItemNameToPrefabLookup.TryAdd(itemName, guid);
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
            if (ItemNameToPrefabLookup.TryGetValue(itemName, out var rebuild))
                return rebuild;
            RebuildLut();
            if (ItemNameToPrefabLookup.TryGetValue(itemName, out var orRebuild))
                return orRebuild;
            if (!Errored.Contains(itemName))
            {
                Plugin.Logger.LogError($"Error rebuilding LUT no item name found: {itemName}");
                Errored.Add(itemName);
            }

            return PrefabGUID.Empty;
        }
    }
}