# Coal Echoes

English | [中文](README.zh-CN.md)

Coal Echoes (煤火回声) is a first-person narrative exploration game, made as a portfolio piece for a USC game design graduate application. With his grandfather's old diary and a photo of him with his coworkers, the protagonist returns to an abandoned coal washing plant in northwest China. His grandfather and a coworker had always wanted to see the plant's lights from the mountain, but work schedules and restrictions on nighttime access kept postponing their trip until the plant closed. Taking photos and keeping a scrapbook along the way, the protagonist follows the old coal ropeway uphill. At night, he photographs the now largely dark plant from the ridge and places the new photograph in his scrapbook, answering their unfulfilled wish.

Open with Unity 6000.4.6f1. The main scene is `Assets/Scenes/Main Game Scene.unity`.

## Project structure

```
./
├── Assets/
│   ├── Scenes/                    Main scene
│   ├── Scripts/
│   │   ├── MainMenu/              Main menu, newspaper photo transition, opening sequence
│   │   ├── In-Game Systems/       Camera and photos, movement, input, inspection, echoes, scrapbook
│   │   ├── Player Panel/          Player panel: album, scrapbook, grandpa's diary, backpack
│   │   ├── UI Animation/          DOTween button, tab and panel motion
│   │   ├── Environment/           Cableway movement, sandstorm, heat haze
│   │   └── Terrain/               MapMagic terrain height source and seam fix
│   ├── Narrative/                 Grandpa's diary JSON (read directly by the main scene)
│   ├── Art Resources/             Models, materials, UI, third-party packs
│   ├── Prefabs/                   UI prefabs and scrapbook decoration presets
│   ├── Settings/                  HDRP volumes, heat haze and sandstorm assets
│   ├── TerrainData/               Terrain data (Git LFS)
│   └── Plugins/                   DOTween, MapMagic, TextMesh Pro
├── ArtSource/                     Art source files, mainly Blender projects (.blend) and Python modeling scripts, plus export backups and notes; not imported by Unity
├── TerrainReferences/             Real-world heightmaps and import notes
├── Docs/                          Design doc, specs, tech stack, dev log (Chinese)
├── AGENTS.md                      Working rules for AI assistants (read by Codex)
├── CLAUDE.md                      Imports AGENTS.md for Claude Code
├── CONTEXT.md                     Project glossary
└── README.zh-CN.md                Chinese README
```

## Docs (Chinese)

- [Design doc](Docs/design.md): levels, controls, systems, level flow and missing art
- [Opening and level one spec](Docs/Specs/Levels/opening-level-one-spec.md): implementation checklist for the opening and level one against the design document
- [Echo targeting spec](Docs/Specs/Systems/echo-targeting-spec.md): echo viewfinder prompts and detection rules
- [Grandpa diary content](Docs/json-grandpa-diary.md): editable diary text, synced to the JSON
- [Tech stack](Docs/tech-stack.en.md)
- [Dev log](Docs/development-log.md)
