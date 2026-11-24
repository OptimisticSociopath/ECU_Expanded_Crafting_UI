# [V_OS] ECU - Expanded Crafting UI

Version **1.0.0**

Author **Vargor**

SPT **4.1.6**

ECU automatically wraps standard hideout crafting recipes into centred rows of up to seven ingredient/tool tiles. It applies across crafting stations, including Medstation, Workbench, Lavatory and Nutrition Unit, using their shared `ProduceView` panel. Recipes from other mods are detected automatically; there is no recipe-ID or station allowlist.

The original icons, order, controls and actions are preserved. The whole recipe reports its new height to Unity's layout system so later recipes keep their spacing. A final incomplete row is centred, and the controls align around the expanded recipe. Seven or fewer tiles keep the native layout.

ECU is a client-only plugin. It requires neither WMI nor WTT. Enable/disable wrapping in F12 under **[V_OS] ECU - Expanded Crafting UI → Crafting UI → Wrap ingredients**.

- [Installation and migration](docs/INSTALL.txt)
- [How it works and how recipe mods integrate](docs/MODDER_GUIDE.md)

Build with `.\Build.ps1 -SptRoot C:\SPT` using .NET SDK 10 and a local SPT 4.1.6 installation. The script writes a new package under `artifacts` without modifying SPT. The source package includes the .NET Standard reference package for offline restore, but no game assemblies.

The provided `assets/icon.png` is the author's ECU artwork. Upload it as the publication thumbnail; SSPTMM 1.4.3 uses catalog thumbnails rather than local icon files.

License: MIT; see `LICENSE`.
