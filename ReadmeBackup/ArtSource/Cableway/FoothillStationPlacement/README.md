# Foothill departure station placement

Saved in Main Game Scene under `In-scene Objects/索道群/CablewayFoothillStation` on 2026 September 28.

The station shelter sits beside the lower cableway line near Tower 01. Its open long side faces the line. The independent roller frame is parallel to the line and stands outside the open side. Both retain their authored scale. Only the lower departure station was placed. No cable geometry, collision configuration or terrain grading was added.

The shelter footprint spans 0.425 metres of existing terrain height. Its floor clears the highest ground, with a maximum foundation underside gap of 0.055 metres at the lowest sampled point. The roller foundations embed approximately 0.08 to 0.12 metres. Renderer bounds do not intersect the first tower. The existing tower and bucket scale remains unchanged and requires separate route and clearance calibration before the cableway is operational.

## Factory terrain restoration

The previous factory grading covered a rotated 545 by 380 metre rectangle with a 90 metre transition. The saved USGS DEM crop and elevation conversion were used to remove this recorded grading from the imported heightmap. Existing tower pads were retained. The corresponding restored minus previous input difference was applied to the live Main and Draft terrain heights, preserving other terrain residuals and all samples within 36 metres of tower centres.

Eight TerrainData assets changed across four tiles. All 51 other terrain records matched their previous hashes. Within the affected tiles, zero samples outside the prepared change mask changed. The four tiles have matching Main and Draft boundary heights. Unity height quantization differs from the prepared float data by at most 0.015274 metres. No MapMagic regeneration was requested. The source RAW, Matrix asset, graph import version and terrain edge caches were updated consistently.

The prechange scene, affected TerrainData, input RAW, Matrix and graph are retained in `Before`. Factory buildings were not repositioned to compensate for restored slopes. Their ground fit remains a later layout task.

## Verification

`placement.json` records transforms and ground fit. `terrain_after.json` records the height restoration audit. `verification.json` checks the saved scene, original tower, cable and factory transforms, all 59 terrain hashes after placement, and 17 valid mesh renderers with HDRP materials. The current Unity Console reported zero errors, zero warnings and no compilation failure. Play Mode, collisions, cable connection and bucket passage were not tested.

`Previews/StationFinal.png` and `Previews/FactoryRestored.png` are actual Unity Scene View captures. Temporary copies imported by the capture command remain under `Assets/ArtSource/Cableway/FoothillStationPlacement` after automatic approval declined optional cleanup. All restoration backups remain in this ArtSource directory.
