# Coal Echoes

English | [中文](README.zh-CN.md)

Coal Echoes (煤火回声) is a first-person narrative exploration game, made as a portfolio piece for a USC game design graduate application. With his grandfather's old diary and a group photo from the day his grandfather joined the plant, the protagonist returns to an abandoned coal washing plant in northwest China. In 1980, his grandfather and a coworker agreed that on a night they were both off, they would ride an empty coal bucket back up the ropeway and watch the plant's lights from the ridge. A daughter's first birthday, a flood that washed out a tower, shifts that never lined up: "next time" kept slipping until the plant closed in 1988. Following the diary, taking photos and keeping a scrapbook, the protagonist finds the route the coworker drew on the back of an old work ticket, gets the ropeway moving again after decades, rides a bucket up the mountain, and on the moonlit ridge photographs the plant under the stars, placing the photo in the same scrapbook as his grandfather's old group photo.

## Opening the project

Open with Unity 6000.4.6f1. The main scene, and the only scene in the build list, is `Assets/Scenes/Main Game Scene.unity`.

## Project structure

```diff
 ./
 ├── Assets/
 │   ├── Scenes/                    Main scene
 │   ├── Scripts/
 │   │   ├── MainMenu/              Newspaper main menu, photo transition, opening sequence
 │   │   ├── In-Game Systems/       Camera and photos, movement, input, inspection, echoes, scrapbook, hints, monologue, cutscene text
 │   │   ├── Player Panel/          Player panel: album, scrapbook, grandpa's diary, backpack
 │   │   ├── UI Animation/          DOTween button, tab and panel motion
 │   │   ├── Environment/           Cableway movement, sandstorm, heat haze, moonlight
 │   │   └── Terrain/               MapMagic terrain height source and seam fix
 │   ├── Narrative/                 Game text JSON: grandpa's diary, inner monologue, hints, cutscene text, newspaper, opening
 │   ├── Art Resources/             Models, materials, UI, third-party packs
 │   ├── Prefabs/                   UI prefabs and scrapbook decoration presets
 │   ├── Settings/                  HDRP volumes, heat haze and sandstorm assets
 │   ├── TerrainData/               Terrain data (Git LFS)
 │   └── Plugins/                   DOTween, MapMagic, TextMesh Pro
 ├── ArtSource/                     Art source files, mainly Blender projects (.blend) and Python modeling and texture scripts
 ├── TerrainReferences/             Real-world heightmaps and import notes
 ├── Docs/                          Design doc, script, game text, level and system specs, tech stack, dev log (Chinese)
 ├── ReadmeBackup/                  Backup of the README before it was rewritten
 ├── AGENTS.md                      Working rules for AI assistants (read by Codex)
 ├── CLAUDE.md                      Imports AGENTS.md for Claude Code
+├── README.md                      Project overview in English and Chinese, the entry point to the docs for people getting to know the project
 └── CONTEXT.md                     Project glossary
```

## Docs (Chinese)

- [Design doc](Docs/design.md): levels, controls, systems and level flow
- [Script](Docs/script.md): the story scene by scene
- [Game text](Docs/game-text.md): every line of in-game text in the order it appears
- [Art resource list](Docs/art-resources.md): art assets needed for each level and what they are for
- [Sound effects](Docs/sound-effects.md): sound effects and ambience for each level
- Level specs: [opening and level one](Docs/Specs/Levels/opening-level-one-spec.md), [level two](Docs/Specs/Levels/level-two-spec.md), [level three](Docs/Specs/Levels/level-three-spec.md), [level four](Docs/Specs/Levels/level-four-spec.md)
- System specs: [echo system](Docs/Specs/Systems/echo-system-spec.md), [echo targeting](Docs/Specs/Systems/echo-targeting-spec.md), [moonlight](Docs/Specs/Systems/moonlight-spec.md)
- [Grandpa's diary](Docs/json-grandpa-diary.md): the diary text, source of the diary JSON
- [Tech stack](Docs/tech-stack.en.md)
- [Dev log](Docs/development-log.md)
