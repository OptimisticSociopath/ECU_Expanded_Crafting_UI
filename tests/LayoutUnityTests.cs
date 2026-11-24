using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vargor.ExpandedCraftingUi;

// Executes inside Unity 2022.3, using its real layout engine. Fixture settings
// come from sharedassets44.assets/ProduceView and its Container, not mocks of
// the wrapping algorithm. Item icons stand in for EFT's dynamically created views.
public static class LayoutUnityTests
{
    private static int _checks;
    private static readonly List<string> Results = new List<string>();
    public static void Run()
    {
        try
        {
            foreach (int count in new[] { 8, 9, 14, 15, 21, 22, 49 })
                foreach (float scale in new[] { 0.75f, 1f, 1.5f })
                    RunCase(count, scale);
            Results.Add("PASS: " + _checks + " Unity layout assertions. No live EFT session was run.");
            File.WriteAllLines(Path.Combine(Application.dataPath, "../../audit/unity-layout-tests.txt"), Results);
            Debug.Log(Results.Last());
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Results.Add("FAIL: " + ex);
            File.WriteAllLines(Path.Combine(Application.dataPath, "../../audit/unity-layout-tests.txt"), Results);
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }
    private static void Check(bool value, string description)
    { _checks++; if (!value) throw new Exception(description); }
    private static bool Near(float a, float b) { return Mathf.Abs(a - b) < 0.02f; }
    private static RectTransform Rect(string name, Transform parent, float width = 0, float height = 0)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.sizeDelta = new Vector2(width, height); return r;
    }
    private static T Group<T>(RectTransform rect, TextAnchor alignment, float spacing = 0) where T : HorizontalOrVerticalLayoutGroup
    {
        var g = rect.gameObject.AddComponent<T>();
        g.childControlWidth = g.childControlHeight = true;
        g.childForceExpandWidth = g.childForceExpandHeight = false;
        g.childAlignment = alignment; g.spacing = spacing; return g;
    }
    private static LayoutElement Size(RectTransform r, float w, float h)
    {
        var e = r.gameObject.AddComponent<LayoutElement>();
        e.minWidth = e.preferredWidth = w; e.minHeight = e.preferredHeight = h; return e;
    }
    private static Rect Bounds(RectTransform root, RectTransform child)
    {
        var corners = new Vector3[4]; child.GetWorldCorners(corners);
        var min = root.InverseTransformPoint(corners[0]); var max = root.InverseTransformPoint(corners[2]);
        return UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static void RunCase(int count, float scale)
    {
        var canvas = Rect("Canvas", null, 1920, 1080);
        canvas.gameObject.AddComponent<Canvas>(); canvas.localScale = Vector3.one * scale;
        var list = Rect("Container", canvas, 1600, 2000);
        Group<VerticalLayoutGroup>(list, TextAnchor.UpperCenter, 18);
        var host = Rect("ProduceView", list, 396.98f, 64);
        var rootGroup = Group<HorizontalLayoutGroup>(host, TextAnchor.UpperCenter);
        var requirements = Rect("RequirementsPanel", host);
        requirements.pivot = new Vector2(0, 1);
        var requiredGroup = Group<HorizontalLayoutGroup>(requirements, TextAnchor.UpperLeft, 10);
        var favorite = Rect("FavoriteButton", requirements, 30, 30);
        var buy = Rect("BuyButton", requirements, 30, 30);
        foreach (var b in new[] { favorite, buy })
        {
            b.anchorMin = b.anchorMax = new Vector2(0, 1);
            b.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
        favorite.anchoredPosition = new Vector2(-25, -15);
        buy.anchoredPosition = new Vector2(-25, -49);
        var inactiveTemplate = Rect("InactiveTemplate", requirements); inactiveTemplate.gameObject.SetActive(false);
        var time = Rect("TimePanel", host); Size(time, 154, 64);
        var output = Rect("ResultItemViewFactory", host); Size(output, 64, 84);
        var actions = Rect("HorizontalPanel", host); Size(actions, 150, 64);
        var actionGroup = Group<HorizontalLayoutGroup>(actions, TextAnchor.MiddleLeft);
        var start = Rect("StartButton", actions); Size(start, 140, 36);
        var button = start.gameObject.AddComponent<Button>();
        int clicks = 0; button.onClick.AddListener(() => clicks++);
        var timer = Rect("ProductionStatus", actions); Size(timer, 140, 36); timer.gameObject.SetActive(false);
        var get = Rect("GetItemsButton", actions); Size(get, 140, 36); get.gameObject.SetActive(false);
        var next = Rect("NextRecipe", list); Size(next, 600, 84);
        var tiles = new List<RectTransform>();
        var infos = new List<RectTransform>();
        for (int i = 0; i < count; i++)
        {
            var tile = Rect("Ingredient" + i, requirements, 300, 300);
            Group<VerticalLayoutGroup>(tile, TextAnchor.UpperCenter);
            var icon = Rect("ItemView", tile); Size(icon, 64, 64);
            var info = Rect("Info", tile); Size(info, i % 3 == 0 ? 93 : 64, 20);
            tiles.Add(tile); infos.Add(info);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        var originalIndices = tiles.Select(x => x.GetSiblingIndex()).ToArray();
        var originalPositions = tiles.Select(x => x.anchoredPosition).ToArray();
        var originalSize = tiles.Select(x => x.sizeDelta).ToArray();
        var originalHostHeight = host.rect.height;
        var layout = new CraftingLayout(host, requirements, tiles, favorite, buy);
        for (int state = 0; state < 3; state++)
        {
            start.gameObject.SetActive(state == 0); timer.gameObject.SetActive(state == 1); get.gameObject.SetActive(state == 2);
            foreach (var info in infos) info.gameObject.SetActive(state == 0);
            for (int repeat = 0; repeat < 3; repeat++) LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            int rows = (count + 6) / 7;
            float tileHeight = state == 0 ? 84 : 64;
            float expectedHeight = rows * tileHeight + (rows - 1) * 10;
            Check(Near(host.rect.height, expectedHeight), "Height propagation " + count + "/" + state + ": " + host.rect.height);
            var grid = Bounds(host, layout.Grid);
            Check(Near(grid.center.y, Bounds(host, time).center.y), "Duration centre");
            Check(Near(grid.center.y, Bounds(host, output).center.y), "Output centre");
            Check(Near(grid.center.y, Bounds(host, actions).center.y), "Actions centre");
            Check(Near(grid.center.y, (Bounds(host, favorite).center.y + Bounds(host, buy).center.y) / 2), "Favorite/flea stack centre");
            Check(Bounds(list, next).yMax <= Bounds(list, host).yMin - 17.99f, "Next recipe must not overlap");
            for (int row = 0; row < rows; row++)
            {
                int a = row * 7, z = Mathf.Min(count, a + 7) - 1;
                var first = Bounds(layout.Grid, tiles[a]); var last = Bounds(layout.Grid, tiles[z]);
                Check(Near((first.xMin + last.xMax) / 2, layout.Grid.rect.center.x), "Row horizontally centred");
                for (int i = a; i <= z; i++)
                {
                    var b = Bounds(layout.Grid, tiles[i]);
                    Check(Near(b.center.y, first.center.y), "Row Y");
                    Check(b.yMin >= layout.Grid.rect.yMin - 0.02f && b.yMax <= layout.Grid.rect.yMax + 0.02f, "Tile contained vertically");
                    Check(tiles[i].localScale == Vector3.one, "No item scaling");
                    if (i > a) Check(b.xMin >= Bounds(layout.Grid, tiles[i - 1]).xMax + 9.99f, "Horizontal spacing");
                    if (row > 0) Check(b.yMax <= Bounds(layout.Grid, tiles[a - 7]).yMin - 9.99f, "Rows must not overlap");
                }
            }
        }
        button.onClick.Invoke(); Check(clicks == 1, "Original action listener retained");
        foreach (var info in infos) info.gameObject.SetActive(true);
        layout.Dispose(); layout.Dispose();
        for (int i = 0; i < count; i++)
        {
            Check(tiles[i].parent == requirements && tiles[i].GetSiblingIndex() == originalIndices[i], "Restored hierarchy/order");
            Check(tiles[i].anchoredPosition == originalPositions[i] && tiles[i].sizeDelta == originalSize[i], "Restored rect snapshot");
        }
        Check(favorite.anchorMin == new Vector2(0, 1) && favorite.anchoredPosition == new Vector2(-25, -15), "Restored favorite");
        Check(buy.anchorMin == new Vector2(0, 1) && buy.anchoredPosition == new Vector2(-25, -49), "Restored flea");
        Check(rootGroup.childAlignment == TextAnchor.UpperCenter && requiredGroup.childAlignment == TextAnchor.UpperLeft, "Restored native groups");
        LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        Check(Near(host.rect.height, originalHostHeight), "Restored original card height");
        Results.Add("PASS ingredients=" + count + " scale=" + scale + " states=idle/producing/ready restoration=OK");
        UnityEngine.Object.DestroyImmediate(canvas.gameObject);
    }
}

