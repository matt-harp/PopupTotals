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
    public class ScrollingCombatTextParentMapperPatch
    {
        private static bool _hooked;
        private static PrefabGUID? _sctTypeResourceGain;
        private static World _world;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), nameof(ScrollingCombatTextParentMapper.OnUpdate))]
        private static void OnUpdate_Prefix(ref ScrollingCombatTextParentMapper __instance)
        {
            if (_hooked) return;
            var pcs = __instance.World.GetExistingSystem<PrefabCollectionSystem>();
            if (pcs == null || __instance._Elements == null)
            {
                _hooked = false;
                return;
            }

            var gain = pcs.NameToPrefabGuidDictionary.TryGetValue("(SCTType) SCT_Type_ResouceGain", out var guid);
            if (gain)
            {
                _sctTypeResourceGain = guid;
            }

            if (_sctTypeResourceGain == null)
            {
                _hooked = false;
                return;
            }

            _world = __instance.World;
            __instance._Elements.OnEntryUpdate += new Action<SCTText, ScrollingCombatTextParentMapper.EntryData>(OnEntryUpdate);
            _hooked = true;
        }

        private static void OnEntryUpdate(SCTText text, ScrollingCombatTextParentMapper.EntryData entry)
        {
            if (_sctTypeResourceGain != null && entry.Type.GuidHash != _sctTypeResourceGain.Value.GuidHash) return;
            var before = entry.SourceTypeText.ToString();
            var str = entry.SourceTypeText.ToString();
            if (str.Contains('(') && str.Contains(')')) return;
            var sb = new StringBuilder(str);
            var prefab = ItemUtils.GetOrRebuild(entry.SourceTypeText.ToString());
            ConsoleShared.TryGetLocalCharacter(out var character, _world);
            var total = InventoryUtilities.GetItemAmount(_world.EntityManager,
                character, prefab);
            sb.Append(" (");
            sb.Append(total);
            sb.Append(')');
            entry.SourceTypeText = new FixedString128(sb.ToString());
            text.Text.m_text = text.Text.m_text.Replace(before, sb.ToString());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), nameof(ScrollingCombatTextParentMapper.OnDestroy))]
        private static void OnDestroy_Postfix(ref ScrollingCombatTextParentMapper __instance)
        {
            __instance._Elements.OnEntryUpdate -= new Action<SCTText, ScrollingCombatTextParentMapper.EntryData>(OnEntryUpdate);
            _hooked = false;
        }
    }
}