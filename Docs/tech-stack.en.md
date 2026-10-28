# Tech Stack

## Engine and Rendering

- Unity 6000.4.6f1, HDRP 17.4.0
- Shader Graph, VFX Graph
- Sandstorm with HDRP Local Volumetric Fog, heat haze with a Custom Pass
- Main menu newspaper photos: custom HLSL halftone shader (halftone part adapted from Daniel Ilett Halftone Shader, MIT), with an HDRP Custom Pass grabbing the camera image
- Opening sky rotation: runtime Volume override of the HDRI Sky orientation
- Dormitory material aging: GPU RenderTexture blending clean and weathered textures
- Terrain anti tiling: HDRP TerrainLit Shader Graph with custom HLSL, Hex Tiling sampling plus an enlarged sample blended in at distance

## Terrain and Scene

- MapMagic manages terrain tiles and LOD, with heights from the USGS Sheep Range 1 m DEM
- Gradient Terrain Authoring solves terrain transitions offline (C++, OpenGL, Python). Paper: Guérin et al., [“Gradient Terrain Authoring”](https://hal.science/hal-03577171/document), Computer Graphics Forum 41(2), Eurographics 2022; code from the authors' repository [eric-guerin/gradient-terrains](https://github.com/eric-guerin/gradient-terrains), MIT license
- Terrain Tools 5.3.2
- Splines 2.9.1 for the factory dirt road routes
- Opening sand accumulation: runtime copy of TerrainData for height animation
- Scene asset packs: Industrial Harbour, RocksGobi, Lands, RockyDesert, Military VOL4 Furniture, Terrain Sample Assets, URP Tree Pack, beton stair
- Terrain textures: ADG ground vol1; gravelly sand, rocky trail and aerial ground rock from Poly Haven; ambientCG Ground104

## Gameplay and UI

- Input System 1.19
- TextMesh Pro; the newspaper menu uses Bitter Regular, Source Han Serif Medium and Ma Shan Zheng, baked into static TMP font assets
- DOTween (free) for UI motion
- UIEffect 5.11.7 for dissolve and page turn lighting
- JsonUtility for save data and diary content

## Art Tools

- Blender 5.2.2 (Steam), parametric modeling and texture baking with Python scripts, exported as FBX or GLB
- glTFast 6.20.0 for importing GLB models
- Logo: Python script using Pillow and fontTools to generate outlines and transparent images from fonts
- Sky HDRI: Poly Haven Kloofendal 48d Partly Cloudy Pure Sky (CC0)

## AI Collaboration

- Claude Code (Claude Opus 5.5) and Codex (GPT 6 Astra, high reasoning) write code and docs, share the AGENTS.md rules and keep progress in repository docs so either can pick up the work
- Unity MCP (CoplayDev/unity-mcp v10.2.0): AI edits scenes, Inspector values and asset references directly

## Version Control

- Git and Git LFS
- Terrain data, DEM, main scenes, .blend files, fonts and most textures and models go through LFS
- The RocksGobi, Lands and RockyDesert packs (about 10 GB) stay local and are not in git

## Licenses and Sources

| Content | License |
| --- | --- |
| Halftone shader halftone part, gradient-terrains | MIT |
| Kloofendal HDRI, Poly Haven and ambientCG terrain textures | CC0 |
| Bitter, Source Han Serif, Ma Shan Zheng | SIL OFL |
| DOTween (free) | Demigiant free license |
| UIEffect | MIT |
| Scene asset packs | Respective store licenses |
