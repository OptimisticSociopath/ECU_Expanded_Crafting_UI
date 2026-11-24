# ECU: modder and implementation guide

## Adding recipes that use ECU

Create your recipe through the usual SPT server recipe system. Use valid item template IDs and ordinary item/tool requirements, and register the recipe in its intended crafting station. There is no ECU-specific server API, JSON property, ID allowlist or recipe registration call.

Players install ECU on the client. When the standard `EFT.Hideout.ProduceView` displays your recipe, ECU counts the active `HideoutProductionRequirementView` tiles under that view's requirements container. More than seven tiles activates wrapping. ECU therefore works independently of the mod that supplied the recipe.

| Visible ingredient/tool tiles | Layout |
| --- | --- |
| 1–7 | Native, unchanged |
| 8 | 7 + 1 |
| 9 | 7 + 2 |
| 14 | 7 + 7 |
| 15 | 7 + 7 + 1 |
| 22 | 7 + 7 + 7 + 1 |

A requirement for 20 screws is one tile, not 20. Tool tiles count too. Station level and other requirements without ingredient tiles do not count. ECU does not create, duplicate, reorder or merge recipe requirements. It preserves the order rendered by the game.

ECU supplies a presentation change only. Your server mod remains responsible for valid requirements, output items/presets, quantities, crafting times, unlock rules and dependencies. It does not make an invalid recipe valid or remove server-side restrictions. Do not increase a recipe's requirements just to force ECU to run.

## Source map

| File | Responsibility |
| --- | --- |
| `src/ExpandedCraftingUiPlugin.cs` | BepInEx configuration, checked Harmony hooks, per-view lifecycle and deferred refresh |
| `src/CraftingLayout.cs` | Reversible wrapping transaction, parent alignment and favourite/flea anchors |
| `src/CenteredIngredientGrid.cs` | Unity `LayoutGroup` implementation: width, height and tile placement |
| `tests/LayoutUnityTests.cs` | Tests using Unity's actual layout engine on a reconstructed native hierarchy |

Plugin GUID: `com.optimisticsociopath.expanded-crafting-ui`.
Harmony owner: `com.optimisticsociopath.expanded-crafting-ui.layout`.
Target: SPT 4.1.6, the audited game's Unity UI API, `netstandard2.1`.

## Hook lifecycle

ECU resolves the expected fields and unambiguous methods before installing its hooks:

1. `ProduceView.Show` prefix restores any previous transaction for a reused view. Its postfix tracks the view and schedules refresh.
2. `ProduceView.UpdateView` postfix schedules refresh for an already tracked view.
3. `HideoutProductionRequirementView.Show` postfix schedules refresh when ingredient widgets are shown/refreshed under a tracked recipe.
4. Refresh is coalesced to one pending coroutine per view and deferred by one frame. This lets the game finish creating item icons and replacing their inner layout groups.
5. `ProduceView.Close` prefix restores the native hierarchy **before** the game clears its UI disposables.

ECU does not scan the whole scene every frame. Its refresh work is triggered by these UI events. Unity remains responsible for subsequent layout rebuilds when content changes.

## The hierarchy change

Only the ingredient tile roots are reparented. The original game controls and their event handlers remain in place:

```text
ProduceView                         native HorizontalLayoutGroup
  RequirementsPanel                native HorizontalLayoutGroup
    FavoriteButton                 native, ignoreLayout
    BuyButton                      native, ignoreLayout
    inactive native template       untouched
    ECU_CenteredIngredients        CenteredIngredientGrid
      ingredient/tool tile 0       native widget
      ingredient/tool tile 1       native widget
      ...
  TimePanel                        native arrow/time/progress content
  ResultItemViewFactory            native output widget
  HorizontalPanel                  native start/timer/collect controls
```

The surrounding recipe list is a native vertical layout group. The new grid reports its preferred size, which propagates through `RequirementsPanel` and `ProduceView` to that list. This reserves space for the additional rows and moves later recipes down. Moving icons visually without reporting this height would leave the list's spacing incorrect.

## How row geometry is calculated

The central count in `CenteredIngredientGrid` is:

```csharp
public const int Columns = 7;
public int RowCount
{
    get { return (rectChildren.Count + Columns - 1) / Columns; }
}
```

`CalculateLayoutInputHorizontal()` first calls the base implementation to collect active layout children. It reads each child's preferred width through `LayoutUtility`, sums widths plus gaps for each group of up to seven and reports the widest row. The column gap is inherited from the native requirements group; the audited prefab uses 10 Unity UI units. The row gap is `Mathf.Max(8f, ColumnGap)`.

`SetLayoutHorizontal()` centres each row independently within the grid. The following excerpt is the row's starting position, where `width` is that row's sum of tile widths and gaps:

```csharp
float x = padding.left
    + (rectTransform.rect.width - padding.horizontal - width) * 0.5f;
```

It then uses `SetChildAlongAxis(child, 0, x, childWidth)` to place each tile. This is why a final row containing one or two tiles is centred under a full row. Icons are not scaled down to fit.

The vertical pass uses the largest preferred tile height as a common row height. Its total is:

```csharp
_height = RowCount * _cellHeight
    + Mathf.Max(0, RowCount - 1) * RowGap;
SetLayoutInputForAxis(
    _height + padding.vertical,
    _height + padding.vertical,
    0f,
    1);
```

`SetLayoutVertical()` assigns a row with integer division `i / Columns`, then places each tile within that row through `SetChildAlongAxis`. Preferred heights include native counters and status content, so changing those widgets can change the measured height through the normal Unity layout process.

## Keeping controls centred and interactive

`CraftingLayout` changes the main horizontal group's vertical alignment to the middle while preserving its horizontal alignment. This centres the original time panel, output factory and action panel around the whole ingredient grid. It also keeps the native requirements group enabled.

Favourite and flea controls have `LayoutElement.ignoreLayout = true` in the audited prefab, so normal group alignment cannot position them. ECU preserves their X position and vertical stack spacing, sets their Y anchors to 0.5 and centres the pair around the requirements panel. Their hierarchy, children and listeners are retained.

The code does not recreate START/collect buttons, replace click handlers, edit the recipe, change production timers or write post-render screen-space offsets. It does not use a `Canvas.willRenderCanvases` compensation loop.

## Restoration and compatibility

Before changing anything, the transaction checks the expected layout groups, controlled child dimensions, ingredient parents and ignored favourite/flea controls. Unsupported structures are rejected before mutation. Changes inside the transaction are rolled back if construction fails.

Each tile and anchor-controlled button has a snapshot of its parent, sibling index, anchors, pivot, size, position, scale and rotation. Original group alignments are saved too. Closing/reusing a view, disabling ECU in F12 or unloading the plugin restores these snapshots and removes the wrapper. The restoration is idempotent.

The plugin resolves all hook methods before patching; an initialization failure removes its hooks. A refresh failure restores that view and logs a warning. These checks limit damage from incompatible structures; they do not guarantee compatibility with every UI replacement or future EFT build.

Scope is the game's standard `ProduceView` crafting UI. Special production panels or entirely custom screens using other view types are outside this implementation. Other mods changing the same hierarchy/layout can conflict. The combined predecessor's GUID is explicitly declared incompatible to prevent two layout implementations running together.

## Building and testing

Run `Build.ps1 -SptRoot C:\SPT` with .NET SDK 10 and your own SPT 4.1.6 installation. The build uses local game/BepInEx references and the bundled .NET Standard reference package. Game binaries are not distributed. The script creates an install ZIP in a fresh `artifacts/build-*` folder and does not install it into your game.

To run the Unity regression tests, use `Test-Layout.ps1 -UnityEditor "C:\Program Files\Unity 2022.3.43f1\Editor\Unity.exe"`. It creates a separate test project under `artifacts`, copies the two layout source files and test runner, and launches Unity in batch mode. A valid Unity license and uGUI are required. Tests use reconstructed native layout settings with substitute icons/state controls, not a running EFT session.

The working combined implementation was confirmed in game by its author before this split. Verify your own recipe in game with missing/available ingredients, during production and at collection; also test favourite/flea actions, tooltips, scrolling, reopening the station and the player's UI scale. No automatic test starts production or changes the profile.

Client logs: `BepInEx/LogOutput.log`. Look for `[ECU 1.0.0] READY`, `WRAPPED` and `Layout restored`. For an issue report, include the full panel screenshot, resolution, UI scale, recipe state and relevant log entries.
