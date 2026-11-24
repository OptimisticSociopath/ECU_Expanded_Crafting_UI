# [V_OS] ECU - Expanded Crafting UI

Version **1.0.0** · Author **Vargor** · SPT **4.1.6**

ECU automatically wraps standard hideout crafting recipes into centred rows of up to seven ingredient/tool tiles. It applies across crafting stations, including Medstation, Workbench, Lavatory and Nutrition Unit, using their shared `ProduceView` panel. Recipes from other mods are detected automatically; there is no recipe-ID or station allowlist.

The original icons, order, controls and actions are preserved. The whole recipe reports its new height to Unity's layout system so later recipes keep their spacing. A final incomplete row is centred, and the controls align around the expanded recipe. Seven or fewer tiles keep the native layout.

ECU is a client-only plugin. It requires neither WMI nor WTT. Enable/disable wrapping in F12 under **[V_OS] ECU - Expanded Crafting UI → Crafting UI → Wrap ingredients**.

- [Installation and migration](docs/INSTALL.txt)
- [How it works and how recipe mods integrate](docs/MODDER_GUIDE.md)
- [Ready-to-paste Forge description](docs/FORGE_DESCRIPTION.md)

Build with `.\Build.ps1 -SptRoot C:\SPT` using .NET SDK 10 and a local SPT 4.1.6 installation. The script writes a new package under `artifacts` without modifying SPT. The source package includes the .NET Standard reference package for offline restore, but no game assemblies.

Run `.\Test-Layout.ps1 -UnityEditor 'C:\Program Files\Unity 2022.3.43f1\Editor\Unity.exe'` for Unity layout regression tests. A valid Unity license is required. Tests use the actual Unity layout engine with a reconstructed game hierarchy; they do not launch EFT.

The working combined predecessor was confirmed in game by the author. This split retains its layout algorithm and gives ECU a separate identity, configuration and package. The old combined plugin must be removed before installing ECU.

The provided `assets/icon.png` is the author's ECU artwork. Upload it as the publication thumbnail; SSPTMM 1.4.3 uses catalog thumbnails rather than local icon files.

License: MIT; see `LICENSE`.
