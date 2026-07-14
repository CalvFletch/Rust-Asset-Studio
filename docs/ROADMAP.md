# Rust Asset Studio — Roadmap

Goal: make this the best tool for browsing and exporting Rust's game content. The items below encode Rust-specific domain knowledge; they are ordered roughly by value-per-effort.

## How Rust's content is organized (background)

- Rust is a standard Unity game with plain unencrypted bundles. As of the Unity 6 move (`6000.3.x`), the client is IL2CPP (`GameAssembly.dll`, no `Managed` folder); the Mono-based dedicated server still has `RustDedicated_Data\Managed`. Rust's bundles embed typetrees, so `MonoBehaviour`s deserialize fully without any assemblies — verified against the live install (UnityFS v8, `LargeFilesSupport`, 4,883 MonoBehaviours dumped from `items.preload.bundle`).
- The `Bundles` folder contains a root **AssetBundleManifest** that enumerates every bundle by name. Content is split across bundles (`content.bundle`, `shared\*`, textures, etc.) plus an `assetscenes.bundle` whose serialized files ("CABs") are named like `BuildPlayer-AssetScene-prefabs`; an **`AssetSceneManifest.json`** in the Bundles folder maps those CAB names back to scene names.
- `content.bundle` contains a **`manifest` MonoBehaviour** (Rust's GameManifest): `prefabProperties` maps a uint hash (the network prefab ID) to a prefab path, and `pooledStrings` maps string-pool hashes to strings. This is the key to resolving entities in demos, map prefab placements, and server data back to actual prefabs.
- **PathIDs are scoped per serialized file/scene** and can collide across scenes. Components/transforms must be resolved against the owning scene's objects; materials/textures live in globally-unique space. Any cross-bundle resolution needs composite identity (source file + PathID), not bare PathIDs.
- **Building blocks** (walls, foundations, …) are prefabs with grade (twig/wood/stone/metal/armored) and skin variants; a `modelState` bitmask selects visible children, and resolution goes prefab path + grade + skin → manifest GUID → concrete prefab.
- **`.map` files** contain terrain data (heightmap, splat maps, topology bitmasks, biome data) plus prefab placements, LZ4-compressed. Splat/topology rendering constants come from the game's `MapImageRenderer`; grid cells are ~146.29 world units.
- **`.dem` demo files** are protobuf streams of network entities; entities reference prefabs by the uint hash that the `manifest` MonoBehaviour resolves.
- Many Rust meshes use **rigid skinning**: bone indices without weights. Naive exporters drop skinning; each vertex should get weight 1.0 on its single bone.
- Prefabs carry LOD groups (LOD0–LOD3), and props use components like `DecorScale`/`DecorOffset`/`DecorAlign`, `BiomeSelector`, and `ConditionalModel` that decide what is actually visible in game.

## Phase 1 — Rust as a first-class citizen

- [x] `GameType.Rust` profile, default in GUI and CLI (`--game` now optional).
- [x] **Auto-detect the Rust install**: find the Steam library (registry / `libraryfolders.vdf`), one-click `File → Load Rust bundles` action.
- [x] **Auto-load managed assemblies**: when loading from a path under a game install with a `*_Data\Managed` folder (e.g. the Mono dedicated server), load it into the `AssemblyLoader` automatically. Not applicable to the IL2CPP client, which embeds typetrees anyway.
- [ ] **Manifest-driven loading**: parse the root AssetBundleManifest and present a bundle picker instead of eagerly loading tens of GB. Lazy/selective loading is the single biggest UX win for Rust's content size.
- [ ] Strip the miHoYo-specific UI/requirements from the default experience (no internet-fetched asset indexes, no CN-key prompts) while keeping upstream code mergeable.
- [ ] Remove/neutralize the "File is encrypted !!" logging path for plain games.

## Phase 2 — Rust domain features

- [ ] **GameManifest browser**: locate the `manifest` MonoBehaviour in `content.bundle`, parse `prefabProperties` + `pooledStrings`, and expose search by prefab ID hash ↔ path. Export as JSON.
- [ ] **Scene-aware indexing**: use `AssetSceneManifest.json` to group assets by scene and keep PathID resolution scoped per scene (composite identity keys for dedup across scenes).
- [ ] **Rigid-skinning fix on export**: generate implicit bone weights (1.0 on the indexed bone) when a mesh has blend indices but no weights.
- [ ] **LOD-aware export**: option to export only LOD0 (or a chosen LOD) instead of every LOD mesh in a prefab.
- [ ] Prefab-path-first browsing: Rust containers are meaningful (`assets/prefabs/...`) — make the container tree the primary navigation.

## Phase 3 — Rust file formats

- [ ] **`.map` support**: parse heightmap, splats, topology, and prefab placements; render a 2D map preview (game-accurate splat colors/sun shading); export heightmaps/splats as 16-bit PNG/EXR and terrain as a mesh.
- [ ] **`.dem` demo support**: parse the protobuf entity stream, resolve prefab IDs through the GameManifest, and export a scene of placed entities.
- [ ] **Building-block resolution**: grade/skin → concrete prefab via manifest GUIDs, with `modelState` bitmask filtering of children.
- [ ] **Instanced world export**: load each unique prefab once, dedupe meshes/materials globally, then stamp out per-entity instances with world transforms — required to export a whole map/demo in minutes rather than hours.

## Phase 4 — Export pipeline quality

- [ ] **glTF/GLB export** alongside FBX. Mind the two distinct Unity-LHS→glTF-RHS conversions: hierarchy-local (negate X position, adjust quaternion, flip normals' X, flip UV V, reverse winding) versus world-space placement (negate Z, ZXY-order Euler reconstruction) — they must not be mixed.
- [ ] Terrain layer/texture extraction (splat-blended materials).
- [ ] Performance: a full map export touches 50K+ prefabs and millions of meshes; batch work, cache typetree parses per operation, never re-crawl what's already indexed.

## Non-goals

- Supporting other games beyond what upstream Studio already does — upstream exists for that.
- Anything that interacts with Rust servers or the network protocol at runtime; this tool reads local files only.
