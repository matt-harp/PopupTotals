using System;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace PopupTotals;

[HarmonyPatch]
public class ScrollingCombatTextParentMapperPatch
{
    private static Action<SCTText, ScrollingCombatTextParentMapper.EntryData> _handler;

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
        _handler = OnEntryUpdate;
        __instance._Elements.OnEntryUpdate += _handler;
        _hooked = true;
    }

    private static void OnEntryUpdate(SCTText text, ScrollingCombatTextParentMapper.EntryData entry)
    {
        if (_sctTypeResourceGain != null && entry.Type.GuidHash != _sctTypeResourceGain.Value.GuidHash) return;
        var str = entry.SourceTypeText.ToString();
        if (str.Contains('(') && str.Contains(')')) return;
        var prefab = ItemUtils.GetOrRebuild(str);
        ConsoleShared.TryGetLocalCharacter(out var character, _world);
        var total = InventoryUtilities.GetItemAmount(_world.EntityManager, character, prefab);
        var newStr = $"{str} ({total})";
        entry.SourceTypeText = new FixedString128(newStr);
        text.Text.m_text = text.Text.m_text.Replace(str, newStr);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ScrollingCombatTextParentMapper), nameof(ScrollingCombatTextParentMapper.OnDestroy))]
    private static void OnDestroy_Postfix(ref ScrollingCombatTextParentMapper __instance)
    {
        if (_handler == null) return;
        __instance._Elements.OnEntryUpdate -= _handler;
        _hooked = false;
    }
}