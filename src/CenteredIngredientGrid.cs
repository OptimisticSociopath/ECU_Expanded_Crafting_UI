using UnityEngine;
using UnityEngine.UI;

namespace Vargor.ExpandedCraftingUi
{
    // Uses the same layout pass as EFT's containing horizontal/vertical groups.
    // Nothing writes screen positions after layout or changes the item-view scale.
    public sealed class CenteredIngredientGrid : LayoutGroup
    {
        public const int Columns = 7;
        public float ColumnGap = 10f;
        public float RowGap = 10f;
        private float _width, _height, _cellHeight;
        private float[] _widths = new float[0];
        public int RowCount { get { return (rectChildren.Count + Columns - 1) / Columns; } }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            if (_widths.Length != rectChildren.Count) _widths = new float[rectChildren.Count];
            _width = 0f;
            for (int start = 0; start < rectChildren.Count; start += Columns)
            {
                float width = 0f;
                int end = Mathf.Min(start + Columns, rectChildren.Count);
                for (int i = start; i < end; i++)
                {
                    _widths[i] = Mathf.Max(1f, LayoutUtility.GetPreferredWidth(rectChildren[i]));
                    width += _widths[i] + (i == start ? 0f : ColumnGap);
                }
                _width = Mathf.Max(_width, width);
            }
            SetLayoutInputForAxis(_width + padding.horizontal, _width + padding.horizontal, 0f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            _cellHeight = 0f;
            foreach (RectTransform child in rectChildren)
                _cellHeight = Mathf.Max(_cellHeight, LayoutUtility.GetPreferredHeight(child));
            _height = RowCount * _cellHeight + Mathf.Max(0, RowCount - 1) * RowGap;
            SetLayoutInputForAxis(_height + padding.vertical, _height + padding.vertical, 0f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            for (int start = 0; start < rectChildren.Count; start += Columns)
            {
                int end = Mathf.Min(start + Columns, rectChildren.Count);
                float width = (end - start - 1) * ColumnGap;
                for (int i = start; i < end; i++) width += _widths[i];
                float x = padding.left + (rectTransform.rect.width - padding.horizontal - width) * 0.5f;
                for (int i = start; i < end; i++)
                {
                    SetChildAlongAxis(rectChildren[i], 0, x, _widths[i]);
                    x += _widths[i] + ColumnGap;
                }
            }
        }

        public override void SetLayoutVertical()
        {
            float top = padding.top + (rectTransform.rect.height - padding.vertical - _height) * 0.5f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                float height = LayoutUtility.GetPreferredHeight(rectChildren[i]);
                SetChildAlongAxis(rectChildren[i], 1,
                    top + (i / Columns) * (_cellHeight + RowGap) + (_cellHeight - height) * 0.5f, height);
            }
        }
    }
}
