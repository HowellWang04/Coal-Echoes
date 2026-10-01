# Sheep Range terrain integration

## Layout decision

Real terrain takes priority. The factory and cableway are moved to fit the terrain. The existing factory layout and interactions move together. Only the factory platform and individual tower footings are locally graded.

## Height data

The original candidate is 4 km square. The final import also samples surrounding valid pixels from the same northern USGS tile, giving a 5.2 by 6 km valid source footprint inside a 6 km square import. The eastern missing area is excluded by the coverage mask. The raster is not stretched to fill the missing area.

The height and coverage imports use 6144 square samples over 6000 metres, with 0.9765625 metre spacing. This slightly resamples the native 1 metre DEM and does not add surveyed detail. Height values are normalized with a 1000 metre output range. The 4 km core keeps its roughly 714 metre source relief, aside from local grading and edge blending. Elevations below the Terrain base outside the core are clamped to the base.

Import offset in MapMagic coordinates is (-2000, -3000), scale is 6. The world origin is (-4924.00049, -3913.999939). The original 4 km core starts 1000 metres inside this square. The coverage mask fades over 700 metres at the actual source boundary.

## Scene changes

Target scene is `Assets/Scenes/Main Game Scene.unity`.

`In-scene Objects`, `Player`, and `Photo Mode Volumes` move by approximately (718, 143.613, -960) metres. Factory local arrangements and serialized object references are retained. Gameplay test objects far outside the original factory are not relocated.

The factory reference point is approximately (-624, -566.387, -2064). The graded footprint is 545 by 380 metres, with a 100 metre outside blend. Maximum platform grading is about 15.1 metres. This is a mountain-front industrial platform, not a claim that the original DEM is level.

Ten existing towers follow a straight 1800 metre route with approximately 239 metres of ground elevation gain. The first two towers are on the factory platform. Other towers receive flat pads with 14 metre core radii and transitions out to 36 metres. Towers are upright and aligned to the new route.

Two static steel cable meshes connect the tower head geometry with 3 metre sag per span. Existing hanging buckets are placed on these lines, and the station model and parked bucket are placed by the factory. These are visual route meshes, not a physical cable simulation or a functional transport system.

## MapMagic settings

Main resolution is 1025 on 1000 metre tiles, approximately 0.977 metre spacing. Draft resolution is 129. Sixteen main tiles cover the 4 km core. Thirty draft tiles retain the outer footprint.

Height output range changes from 250 to 1000 metres. Existing procedural Levels output maximum changes from 1 to 0.25 to compensate for the larger output range. The DEM feeds the existing Height output and texture portal through the coverage Mask. Existing noise, Curve, erosion and material settings otherwise remain as found.

All twelve old location locks are disabled. A new factory lock protects a 335 metre radius with 40 metre transition. Eight new tower locks protect the remaining tower pads, with 14 metre radii and 22 metre transitions. The first two towers are covered by the factory lock.

## Seam correction

Repeated native regeneration left varying metre-scale discrepancies at shared Main tile edges. Switching to direct height application and single-thread generation did not eliminate them, so the original TextureToHeightmap application and three generation threads were restored.

`Assets/Scripts/Terrain/MapMagicTerrainSeamWeld.cs` is attached only to this MapMagic object. It waits until generation finishes, synchronizes heightmaps, averages shared boundary samples, updates the plugin's edge cache, and refreshes surrounding Draft borders. Interior height samples are unchanged by this component. It runs after later regeneration as well. This addresses the observed discrepancy without changing third-party MapMagic source.

## Verification scope

The scene and 46 native TerrainData assets were saved. A fresh check found no unsaved TerrainData references and no scene dirty flag. After the final full regeneration and save, all 24 shared Main edges had zero measured height difference. Final Console sampling showed no errors or compilation failure. The save request exceeded the CLI's five-second main-thread timeout, but independent scene and asset checks confirmed that saving completed.

Editor measurements cover Main tile seams, factory platform samples, tower base placement and terrain clearance beneath the cable route. Unity Console compilation status is checked separately. Play Mode traversal, interaction behavior, lighting rebakes and NavMesh behavior are not verified by these measurements.

Machine-readable layout is in `layout_config.json`. Current measured results are in `verification.json`. The source inspection and comparison images predate integration and are not Unity renderings.
