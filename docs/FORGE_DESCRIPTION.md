# [V_OS] ECU - Expanded Crafting UI

**More ingredients. A readable crafting menu.**

ECU automatically expands the crafting interface when a recipe has more than seven ingredient or tool tiles. Ingredients wrap into additional rows, so larger custom recipes no longer have to squeeze into a single line.

- Up to **7 tiles per row**: 8 becomes **7+1**, 9 becomes **7+2**, 15 becomes **7+7+1**.
- Incomplete rows are centred automatically.
- Recipe height grows with the number of rows, keeping the next recipe below it.
- Favourite/flea controls, the duration/arrow, output and action panel stay vertically centred around the expanded recipe.
- Original icons, ingredient order, tooltips, counters and crafting actions are retained.
- Recipes with seven or fewer tiles keep their original layout.

**Works automatically with custom recipes.** No recipe IDs, special tags or ECU integration code are required. Any recipe shown through the game's standard crafting view receives wrapping once it exceeds seven ingredient/tool tiles. Tools count as tiles; the quantity of a single ingredient does not create extra tiles.

ECU uses Unity's normal layout system to calculate the additional rows and their height. It preserves the game's existing controls and event handlers. It does not add recipes or change crafting costs, output, duration or success rules.

**Requirements:** SPT 4.1.6. Client plugin only. WMI and WTT are not required.

**Settings:** F12 → **[V_OS] ECU - Expanded Crafting UI** → **Wrap ingredients**. Enabled by default.

**Installation:** Close the game/server and install `ECU-v1.0.0-SSPTMM.zip` through SSPTMM, or extract it into your SPT game folder.

**Compatibility:** Covers standard crafting recipe panels (`EFT.Hideout.ProduceView`). Completely custom crafting screens and other mods replacing the same layout may need separate integration. Remove the older combined **RPD Modernization Included** mod before installing ECU; install WMI too if you want to keep its RPD recipes and Heavy Gunner item.

**Author:** Vargor
