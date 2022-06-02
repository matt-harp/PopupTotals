using System;
using System.Text;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace PopupTotals
{
    [HarmonyPatch]
    public class ScrollingCombatTextParentMapper_Patch
    {
        private static bool _hooked;
        private static PrefabGUID _sctTypeResourceGain;
        private static World _world;

        [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), nameof(ScrollingCombatTextParentMapper.OnUpdate))]
        [HarmonyPrefix]
        static void OnUpdate_Prefix(ScrollingCombatTextParentMapper __instance)
        {
            if (_hooked) return;
            if (!__instance.World.GetExistingSystem<PrefabCollectionSystem>()
                    .PrefabNameToPrefabGuidLookupMap.ContainsKey(new FixedString128("(SCTType) SCT_Type_ResouceGain"))) return;
            _sctTypeResourceGain = __instance.World.GetExistingSystem<PrefabCollectionSystem>()
                .PrefabNameToPrefabGuidLookupMap[new FixedString128("(SCTType) SCT_Type_ResouceGain")];
            if (__instance._Elements == null) return;
            _world = __instance.World;
            __instance._Elements.OnEntryUpdate += new Action<SCTText, ScrollingCombatTextParentMapper.EntryData>(OnEntryUpdate);
            _hooked = true;
        }

        static void OnEntryUpdate(SCTText text, ScrollingCombatTextParentMapper.EntryData entry)
        {
            if (entry.Type.GuidHash != _sctTypeResourceGain.GuidHash) return;
            var before = entry.SourceTypeText.ToString();
            var str = entry.SourceTypeText.ToString();
            if (str.Contains("(") && str.Contains(")")) return;
            var sb = new StringBuilder(str);
            var prefab = ItemUtils.GetOrRebuild(entry.SourceTypeText.ToString());
            var total = InventoryUtilities.ItemCount(_world.EntityManager,
                EntitiesHelper.GetLocalCharacterEntity(_world.EntityManager), prefab);
            sb.Append(" (");
            sb.Append(total);
            sb.Append(")");
            entry.SourceTypeText = new FixedString128(sb.ToString());
            text.Text.m_text = text.Text.m_text.Replace(before, sb.ToString());
        }
        
        [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), nameof(ScrollingCombatTextParentMapper.OnDestroy))]
        [HarmonyPostfix]
        static void OnDestroy_Postfix(ScrollingCombatTextParentMapper __instance)
        {
            __instance._Elements.OnEntryUpdate -= new Action<SCTText, ScrollingCombatTextParentMapper.EntryData>(OnEntryUpdate);
            _hooked = false;
        }
    }
}