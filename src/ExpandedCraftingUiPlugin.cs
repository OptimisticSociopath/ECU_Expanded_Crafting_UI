using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Vargor.ExpandedCraftingUi
{
    [BepInPlugin(PluginId, "[V_OS] ECU - Expanded Crafting UI", "1.0.0")]
    // The combined predecessor must be removed to avoid two layout owners.
    [BepInIncompatibility("com.optimisticsociopath.rpd-modernization-included")]
    public sealed class ExpandedCraftingUiPlugin : BaseUnityPlugin
    {
        public const string PluginId = "com.optimisticsociopath.expanded-crafting-ui";
        private static ExpandedCraftingUiPlugin? _instance;
        private Harmony? _harmony;
        private ConfigEntry<bool> _enabled = null!;
        private Type _viewType = null!, _requirementType = null!;
        private FieldInfo _container = null!, _favorite = null!, _buy = null!, _scheme = null!;
        private readonly Dictionary<Component, CraftingLayout> _layouts = new();
        private readonly Dictionary<Component, Coroutine> _pending = new();
        private readonly HashSet<Component> _shown = new();
        private readonly HashSet<string> _warnings = new();

        private void Awake()
        {
            _instance = this;
            _enabled = Config.Bind("Crafting UI", "Wrap ingredients", true,
                "All crafting stations: recipes with more than seven ingredient/tool tiles wrap into centred rows of at most seven. Existing recipes and craft actions are unchanged.");
            try
            {
                _viewType = AccessTools.TypeByName("EFT.Hideout.ProduceView") ?? throw new MissingMemberException("ProduceView");
                _requirementType = AccessTools.TypeByName("EFT.Hideout.HideoutProductionRequirementView") ?? throw new MissingMemberException("HideoutProductionRequirementView");
                _container = RequiredField("_requiredItemsContainer", typeof(Transform));
                _favorite = RequiredField("_favoriteButton", typeof(Component));
                _buy = RequiredField("_buyButton", typeof(Component));
                _scheme = AccessTools.Field(_viewType, "_scheme") ?? throw new MissingFieldException("_scheme");
                // Resolve every method before applying any hook. No name-only
                // fallback to another overload after a game update.
                var show = UniqueMethod(_viewType, "Show", 7);
                var update = UniqueMethod(_viewType, "UpdateView", 0);
                var close = UniqueMethod(_viewType, "Close", 0);
                var requirementShow = UniqueMethod(_requirementType, "Show", 6);
                _harmony = new Harmony(PluginId + ".layout");
                _harmony.Patch(show, prefix: Hook(nameof(BeforeShow)), postfix: Hook(nameof(AfterShow)));
                _harmony.Patch(update, postfix: Hook(nameof(AfterUpdate)));
                _harmony.Patch(close, prefix: Hook(nameof(BeforeClose)));
                _harmony.Patch(requirementShow, postfix: Hook(nameof(AfterRequirementShow)));
                _enabled.SettingChanged += OnSettingChanged;
                Logger.LogInfo("[ECU 1.0.0] READY | generic wrapping | maxColumns=7 | native layout and actions retained");
            }
            catch (Exception error)
            {
                _harmony?.UnpatchSelf();
                Logger.LogError("[ECU 1.0.0] UI hooks disabled: " + error);
            }
        }

        private FieldInfo RequiredField(string name, Type expected)
        {
            var field = AccessTools.Field(_viewType, name);
            if (field == null || !expected.IsAssignableFrom(field.FieldType)) throw new MissingFieldException(name);
            return field;
        }
        private static MethodInfo UniqueMethod(Type type, string name, int parameters)
        {
            var found = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(x => x.Name == name && x.ReturnType == typeof(void) && x.GetParameters().Length == parameters).ToArray();
            if (found.Length != 1) throw new MissingMethodException(type.FullName, name);
            return found[0];
        }
        private static HarmonyMethod Hook(string name) => new(typeof(ExpandedCraftingUiPlugin).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        private static void BeforeShow(Component __instance) => _instance?.Forget(__instance);
        private static void AfterShow(Component __instance)
        {
            if (_instance == null) return;
            _instance._shown.Add(__instance);
            _instance.Schedule(__instance);
        }
        private static void AfterUpdate(Component __instance)
        {
            if (_instance != null && _instance._shown.Contains(__instance)) _instance.Schedule(__instance);
        }
        private static void BeforeClose(Component __instance) => _instance?.Forget(__instance);
        private static void AfterRequirementShow(Component __instance)
        {
            if (_instance == null || __instance == null) return;
            var view = __instance.GetComponentInParent(_instance._viewType);
            if (view != null && _instance._shown.Contains(view)) _instance.Schedule(view);
        }

        private void Schedule(Component view)
        {
            if (view == null || !view.gameObject.activeInHierarchy || _pending.ContainsKey(view)) return;
            _pending.Add(view, StartCoroutine(RefreshAfterLayout(view)));
        }

        private IEnumerator RefreshAfterLayout(Component view)
        {
            // Native Show/UpdateView creates item icons and replaces their inner
            // layout groups. Wait for that frame's changes to settle first.
            yield return null;
            _pending.Remove(view);
            if (view == null || !_shown.Contains(view)) yield break;
            try { Refresh(view); }
            catch (Exception error)
            {
                Restore(view);
                WarnOnce("Layout restored: " + error.GetType().Name + " | " + error.Message);
            }
        }

        private void Refresh(Component view)
        {
            if (!_enabled.Value) { Restore(view); return; }
            var requirements = _container.GetValue(view) as RectTransform;
            if (requirements == null) throw new InvalidOperationException("Missing requirements RectTransform.");
            var tiles = requirements.GetComponentsInChildren(_requirementType, false)
                .Where(c => c != null && c.gameObject.activeSelf)
                .Select(c => c.transform as RectTransform).Where(t => t != null).Cast<RectTransform>()
                .ToList();
            if (tiles.Count <= CenteredIngredientGrid.Columns) { Restore(view); return; }
            if (_layouts.TryGetValue(view, out var current))
            {
                if (current.IsAlive && current.Count == tiles.Count && tiles.All(t => t.parent == current.Grid))
                { current.Dirty(); return; }
                Restore(view);
            }
            var host = view.transform as RectTransform ?? throw new InvalidOperationException("Missing recipe RectTransform.");
            var favorite = (_favorite.GetValue(view) as Component)?.transform as RectTransform;
            var buy = (_buy.GetValue(view) as Component)?.transform as RectTransform;
            var layout = new CraftingLayout(host, requirements, tiles, favorite!, buy!);
            _layouts.Add(view, layout);
            // Rebuild from the containing list: it must reserve the extra height
            // for following recipes too, not just reposition this card's tiles.
            LayoutRebuilder.ForceRebuildLayoutImmediate(host.parent as RectTransform ?? host);
            string id = AccessTools.Field(_scheme.FieldType, "_id")?.GetValue(_scheme.GetValue(view))?.ToString() ?? "unknown";
            Logger.LogInfo($"[ECU 1.0.0] WRAPPED | recipe={id} | tiles={tiles.Count} | rows={(tiles.Count + 6) / 7} | grid={layout.Grid.rect.width:F1}x{layout.Grid.rect.height:F1} | cardHeight={host.rect.height:F1}");
        }

        private void OnSettingChanged(object sender, EventArgs args)
        {
            foreach (var view in _shown.ToArray())
            {
                if (view == null) { Forget(view!); continue; }
                if (!_enabled.Value) Restore(view);
                else Schedule(view);
            }
        }
        private void Restore(Component view)
        {
            if (_layouts.TryGetValue(view, out var layout))
            { _layouts.Remove(view); layout.Dispose(); }
        }
        private void Forget(Component view)
        {
            if (_pending.TryGetValue(view, out var coroutine))
            { _pending.Remove(view); StopCoroutine(coroutine); }
            Restore(view);
            _shown.Remove(view);
        }
        private void WarnOnce(string message)
        {
            if (_warnings.Add(message)) Logger.LogWarning("[ECU 1.0.0] " + message);
        }
        private void OnDestroy()
        {
            _enabled.SettingChanged -= OnSettingChanged;
            StopAllCoroutines(); _pending.Clear();
            foreach (var layout in _layouts.Values) layout.Dispose();
            _layouts.Clear(); _shown.Clear();
            _harmony?.UnpatchSelf();
            _instance = null;
        }
    }
}
