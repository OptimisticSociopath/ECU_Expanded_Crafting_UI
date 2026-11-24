# [V_OS] ECU - Expanded Crafting UI

Version: 1.0.0
Author: Vargor
SPT: 4.1.6

ECU automatically wraps standard hideout crafting recipes into centred rows of up to seven ingredient/tool tiles. It applies across crafting stations, including Medstation, Workbench, Lavatory and Nutrition Unit, using their shared `ProduceView` panel. Recipes from other mods are detected automatically; there is no recipe-ID or station allowlist.

ECU is a client-only plugin. It doesn't require WTT Content Backport. Enable/disable wrapping in F12 under [V_OS] ECU - Expanded Crafting UI → Crafting UI → Wrap ingredients.

- [Installation and migration](docs/INSTALL.txt)
- [How it works and how recipe mods integrate](docs/MODDER_GUIDE.md)
- [Ready-to-paste Forge description](docs/FORGE_DESCRIPTION.md)

Build with `.\Build.ps1 -SptRoot C:\SPT` using .NET SDK 10 and a local SPT 4.1.6 installation. The script writes a new package under `artifacts` without modifying SPT. The source package includes the .NET Standard reference package for offline restore, but no game assemblies.

License: MIT; see `LICENSE`.
