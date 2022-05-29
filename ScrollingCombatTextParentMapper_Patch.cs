using System;
using System.Text;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Unity.Collections;

namespace PopupTotals
{
    [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), "OnUpdate")]
    public class ScrollingCombatTextParentMapper_Patch
    {
        private static bool _hooked;
        private static PrefabGUID _sctTypeResourceGain;

        static void Postfix(ScrollingCombatTextParentMapper __instance)
        {
            if (_hooked) return;

            if (!__instance.World.GetExistingSystem<PrefabCollectionSystem>()
                    .PrefabNameToPrefabGuidLookupMap.ContainsKey(new FixedString128("(SCTType) SCT_Type_ResouceGain"))) return;
            _sctTypeResourceGain = __instance.World.GetExistingSystem<PrefabCollectionSystem>()
                .PrefabNameToPrefabGuidLookupMap[new FixedString128("(SCTType) SCT_Type_ResouceGain")];
            if (__instance?._Elements == null) return;
            __instance._Elements.OnEntryUpdate += new Action<SCTText, ScrollingCombatTextParentMapper.EntryData>(
                (text, entry) =>
                {
                    if (entry.Type.GuidHash != _sctTypeResourceGain.GuidHash) return;
                    var before = entry.SourceTypeText.ToString();
                    var str = entry.SourceTypeText.ToString();
                    if (str.Contains("(") && str.Contains(")")) return;
                    var sb = new StringBuilder(str);
                    var prefab = ItemUtils.ItemNameToPrefabLookup[entry.SourceTypeText.ToString()];
                    var total =InventoryUtilities.ItemCount(__instance.World.EntityManager,
                        EntitiesHelper.GetLocalCharacterEntity(__instance.World.EntityManager), prefab);
                    sb.Append(" (");
                    sb.Append(total);
                    sb.Append(")");
                    entry.SourceTypeText = new FixedString128(sb.ToString());
                    text.Text.m_text = text.Text.m_text.Replace(before, sb.ToString());
                });
            _hooked = true;
        }
    }
}