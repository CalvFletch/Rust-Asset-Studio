# Rust Asset Studio

A fork of [RazTools/Studio](https://github.com/RazTools/Studio) (the AssetStudio family) focused on browsing, previewing, and exporting assets from **Rust** by Facepunch Studios.

Rust ships as a standard Unity game with unencrypted asset bundles, but its content is organized in ways generic Unity tools don't understand: a bundle manifest, prefab-ID hashes, scene-scoped assets, building-block variants, `.map` terrain files, and `.dem` demo recordings. This fork aims to make all of that first-class.

## Status

Early days, but working: current Rust (Unity 6, `6000.3.x`) bundles load and parse, including full `MonoBehaviour` data via the embedded typetrees. See [docs/ROADMAP.md](docs/ROADMAP.md) for where it's headed.

## Quick start (GUI)

1. Launch `AssetStudio.GUI`. The game selector (Options → Specify Game) defaults to **Rust**.
2. `File → Load Rust bundles` auto-detects your Steam install and loads its `Bundles` folder. Or use `File → Load folder`/`Load file` to pick bundles manually — loading everything takes a while and a lot of RAM, so start with a single bundle such as `shared\items.preload.bundle` if you're exploring.
3. Browse the asset list or scene hierarchy, then export via the `Export` menu.

## Quick start (CLI)

```
AssetStudioCLI <input_path> <output_path> [options]
```

`--game` defaults to `Rust`, so the minimal invocation is just an input and output path. Run with `--help` for the full option list.

## Building

Requires the .NET 8 SDK (targets `net7.0-windows` and `net8.0-windows`).

```
dotnet build AssetStudio.sln -c Release
```

## Credits

- [Perfare](https://github.com/Perfare/AssetStudio): original AssetStudio author.
- [Razmoth](https://github.com/RazTools/Studio): Studio, the base of this fork.
- [Ds5678](https://github.com/AssetRipper/AssetRipper): AssetRipper, information about asset formats and parsing.
- [mafaca](https://github.com/mafaca/UtinyRipper): uTinyRipper, YAML and AnimationClipConverter.

Licensed under the [MIT License](LICENSE).

This project is not affiliated with Facepunch Studios. Use it only with game files you legitimately own, and respect Facepunch's terms of service.
