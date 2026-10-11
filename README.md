# Project Coal

English | [中文](README.zh-CN.md)

A first-person narrative exploration game, made as a portfolio piece for a USC game design graduate application. With his grandfather's old diary and a photo of him with his coworkers, the protagonist returns to an abandoned coal washing plant in northwest China. His grandfather and a coworker had always wanted to see the plant's lights from the mountain, but work schedules and restrictions on nighttime access kept postponing their trip until the plant closed. Taking photos and keeping a scrapbook along the way, the protagonist follows the old coal ropeway uphill. At night, he photographs the now largely dark plant from the ridge and places the new photograph in his scrapbook, answering their unfulfilled wish.

Open with Unity 6000.4.6f1. The main scene is `Assets/Scenes/Main Game Scene.unity`.

## Project structure

```
Project Coal/
├── Assets/
│   ├── Scenes/                    Main scene
│   ├── Scripts/
│   │   ├── In-Game Systems/       Camera and photos, movement, input, inspection, echoes, scrapbook
│   │   ├── Player Panel/          Player panel: album, scrapbook, grandpa's diary, backpack
│   │   ├── UI Animation/          DOTween button, tab and panel motion
│   │   ├── Environment/           Cableway movement, sandstorm, heat haze
│   │   └── Terrain/               MapMagic terrain seam fix
│   ├── Narrative/                 Grandpa's diary JSON (read directly by the main scene)
│   ├── Art Resources/             Models, materials, UI, third-party packs
│   ├── Prefabs/                   UI prefabs and scrapbook decoration presets
│   ├── Settings/                  HDRP volumes, heat haze material
│   ├── TerrainData/               Terrain data (Git LFS)
│   ├── SmokePortalTest/           Sandstorm assets and test scene
│   └── Plugins/                   DOTween, MapMagic, TextMesh Pro
├── ArtSource/                     Art source files, mainly Blender projects (.blend) and Python modeling scripts, plus export backups and notes; not imported by Unity
├── TerrainReferences/             Real-world heightmaps and import notes
├── Docs/                          Project docs (Chinese)
├── AGENTS.md                      Working rules for AI assistants
├── CONTEXT.md                     Project glossary
└── README.zh-CN.md                Chinese README
```

## Docs (Chinese)

- [Game design](Docs/game-design.md): gameplay, echo system, levels and items
- [Opening and level one spec](Docs/opening-level-one-spec.md): opening sequence, diary pickup, level progress and exploration records
- [Story](Docs/story-flow.md): narrative layers and finalized text
- [Tech stack](Docs/tech-stack.md)
- [Art assets](Docs/art-assets.md)
- [Todo](Docs/todolist.md)
- [Dev log](Docs/development-log.md)
