using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Vargor.ExpandedCraftingUi
{
    // One reversible layout transaction per recipe view. All original controls,
    // listeners, tooltip targets and item views remain owned by EFT.
    public sealed class CraftingLayout : IDisposable
    {
        private readonly RectTransform _host, _requirements;
        private readonly HorizontalLayoutGroup _hostGroup, _requirementsGroup;
        private readonly TextAnchor _hostAlignment, _requirementsAlignment;
        private readonly List<RectSnapshot> _tiles = new List<RectSnapshot>();
        private readonly List<RectSnapshot> _buttons = new List<RectSnapshot>();
        private RectTransform _grid;
        private bool _disposed;
        public RectTransform Grid { get { return _grid; } }
        public int Count { get { return _tiles.Count; } }
        public bool IsAlive { get { return !_disposed && _host != null && _grid != null && _tiles.All(x => x.Rect != null && x.Rect.parent == _grid); } }

        public CraftingLayout(RectTransform host, RectTransform requirements,
            IList<RectTransform> tiles, RectTransform favorite, RectTransform buy)
        {
            if (tiles.Count <= CenteredIngredientGrid.Columns) throw new ArgumentException("No wrapping needed.");
            _host = host;
            _requirements = requirements;
            _hostGroup = host.GetComponent<HorizontalLayoutGroup>();
            _requirementsGroup = requirements.GetComponent<HorizontalLayoutGroup>();
            if (_hostGroup == null || !_hostGroup.enabled || _requirementsGroup == null || !_requirementsGroup.enabled
                || requirements.parent != host || !_hostGroup.childControlHeight || !_hostGroup.childControlWidth
                || !_requirementsGroup.childControlHeight || !_requirementsGroup.childControlWidth
                || host.GetComponent<ContentSizeFitter>() is ContentSizeFitter fitter && fitter.enabled
                || tiles.Any(t => t == null || t.parent != requirements)
                || tiles.Distinct().Count() != tiles.Count)
                throw new InvalidOperationException("Unsupported crafting layout hierarchy; original layout preserved.");
            ValidateButton(favorite, requirements);
            ValidateButton(buy, requirements);
            _hostAlignment = _hostGroup.childAlignment;
            _requirementsAlignment = _requirementsGroup.childAlignment;
            foreach (var tile in tiles.OrderBy(t => t.GetSiblingIndex())) _tiles.Add(new RectSnapshot(tile));
            _buttons.Add(new RectSnapshot(favorite));
            _buttons.Add(new RectSnapshot(buy));
            try
            {
                // The grid's preferred height flows through RequirementsPanel,
                // ProduceView and Container to the scroll content automatically.
                _grid = new GameObject("ECU_CenteredIngredients", typeof(RectTransform), typeof(CenteredIngredientGrid)).GetComponent<RectTransform>();
                _grid.SetParent(requirements, false);
                _grid.SetSiblingIndex(_tiles[0].Index);
                var group = _grid.GetComponent<CenteredIngredientGrid>();
                group.ColumnGap = Mathf.Max(0f, _requirementsGroup.spacing);
                group.RowGap = Mathf.Max(8f, group.ColumnGap);
                foreach (var tile in _tiles) tile.Rect.SetParent(_grid, false);
                _hostGroup.childAlignment = (TextAnchor)(3 + ((int)_hostAlignment % 3));
                _requirementsGroup.childAlignment = TextAnchor.MiddleLeft;

                // These two controls explicitly ignore the native layout. Keep
                // their existing vertical stack and X, anchor its centre to 50%.
                float favHeight = favorite.rect.height;
                float buyHeight = buy.rect.height;
                float gap = Mathf.Max(0f, Mathf.Abs(favorite.anchoredPosition.y - buy.anchoredPosition.y)
                    - (favHeight + buyHeight) * 0.5f);
                CenterButton(favorite, (buyHeight + gap) * 0.5f);
                CenterButton(buy, -(favHeight + gap) * 0.5f);
                Dirty();
            }
            catch { Dispose(); throw; }
        }

        private static void ValidateButton(RectTransform rect, RectTransform parent)
        {
            if (rect == null || rect.parent != parent || rect.GetComponent<LayoutElement>()?.ignoreLayout != true)
                throw new InvalidOperationException("Favorite/flea layout contract changed; original layout preserved.");
        }

        private static void CenterButton(RectTransform rect, float centreY)
        {
            var min = rect.anchorMin; min.y = 0.5f; rect.anchorMin = min;
            var max = rect.anchorMax; max.y = 0.5f; rect.anchorMax = max;
            var pos = rect.anchoredPosition; pos.y = centreY + (rect.pivot.y - 0.5f) * rect.rect.height;
            rect.anchoredPosition = pos;
        }

        public void Dirty()
        {
            if (_requirements != null) LayoutRebuilder.MarkLayoutForRebuild(_requirements);
            if (_host != null) LayoutRebuilder.MarkLayoutForRebuild(_host);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_grid != null) _grid.gameObject.SetActive(false);
            foreach (var tile in _tiles) tile.Restore();
            foreach (var button in _buttons) button.Restore();
            if (_hostGroup != null) _hostGroup.childAlignment = _hostAlignment;
            if (_requirementsGroup != null) _requirementsGroup.childAlignment = _requirementsAlignment;
            if (_grid != null)
            {
                _grid.SetParent(null, false);
                UnityEngine.Object.Destroy(_grid.gameObject);
            }
            Dirty();
        }

        private sealed class RectSnapshot
        {
            public readonly RectTransform Rect;
            public readonly int Index;
            private readonly Transform _parent;
            private readonly Vector2 _min, _max, _pivot, _size;
            private readonly Vector3 _position, _scale;
            private readonly Quaternion _rotation;
            public RectSnapshot(RectTransform rect)
            {
                Rect = rect; _parent = rect.parent; Index = rect.GetSiblingIndex();
                _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot;
                _size = rect.sizeDelta; _position = rect.anchoredPosition3D;
                _scale = rect.localScale; _rotation = rect.localRotation;
            }
            public void Restore()
            {
                if (Rect == null || _parent == null) return;
                Rect.SetParent(_parent, false); Rect.SetSiblingIndex(Index);
                Rect.anchorMin = _min; Rect.anchorMax = _max; Rect.pivot = _pivot;
                Rect.sizeDelta = _size; Rect.anchoredPosition3D = _position;
                Rect.localScale = _scale; Rect.localRotation = _rotation;
            }
        }
    }
}
