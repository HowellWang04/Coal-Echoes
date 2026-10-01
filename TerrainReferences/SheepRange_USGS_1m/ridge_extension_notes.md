# Ridge route extension

Applied and saved on 2026 September 25.

The route follows the user marked red and green ridge direction and ends at the live position of `tajia03 (8)`. The horizontal coordinates of `tajia03 (10)`, `tajia03 (7)`, `tajia03 (11)` and `tajia03 (8)` are unchanged. The first three also retained their existing vertical positions after ground sampling. The new endpoint moved upward by 2.637268 metres to match its footing.

Eleven existing towers now span 1199.48 metres in plan view. The connected route previously covered 629.47 metres with ten towers. The current horizontal spans are approximately 90, 97, 166, 120, 153, 132, 120, 104, 116 and 101 metres. Lower route towers `tajia03 (1)`, `tajia01` and `tajia03 (3)` were redistributed uphill. Factory objects and platform placement were not moved during this extension.

The DEM import was rebuilt from the original north USGS raster with the existing factory grading and the eleven current footing masks. Previous tower grading is absent from this new source except where current footing masks overlap. Footings have a 14 metre flat radius and 22 metre transition. Existing footing locks were disabled for regeneration and reused at their new positions. A new footing lock was added for the endpoint. Twelve locks are enabled, comprising eleven footings and the existing factory lock.

The two existing static cable meshes were rebuilt between tower heads. Cable offsets were read in world space and converted into the first tower rotation frame before moving towers. Both suspended bucket objects were repositioned onto the cable paths. This remains a visual cable route rather than a validated mechanical cableway with turning equipment or moving transport simulation.

## Verification

All four horizontal anchor displacements are zero. Tower renderer lower bounds match sampled footing heights within 0.000016 metres. This checks the model bounds rather than independently identifying each modeled foundation contact point.

Cable sampling at 4820 points found a minimum ground clearance of 15.7777405 metres. The platform was checked at 300 points, with maximum height deviation of 0.002380371 metres.

The terrain was generated again with the final locks enabled. Explicit final seam welding produced zero height mismatch across 33 neighboring Main terrain edges. The scene and 58 terrain assets were saved. Console reported zero errors and zero warnings. Play Mode was not run.

At 95 samples around vacated tower positions outside current grading masks, live terrain differs from the native DEM by a mean absolute 0.871 metres and a maximum 4.673 metres. The staged RAW at the largest discrepancy matches the native elevation within 0.060 metres, showing that the larger difference occurs downstream in terrain import or sampling. The exact downstream cause was not isolated in this placement task. These comparisons do not claim pixel identical restoration of native elevations. The old flat footing shapes were replaced by regenerated terrain.

## Records

`ridge_extension_before.json` records live tower transforms before this operation.

`ridge_extension.json` records the applied route and cable checks.

`ridge_extension_verification.json`, `ridge_extension_restoration_check.json` and `ridge_extension_save_result.json` record the final checks.

`ridge_extension_layout.png` is a calculated native DEM relief map with the applied route and tower names. It does not depict the edited foundation pads.

`Assets/TerrainReferences/SheepRange_USGS_1m/ridge_extended.png` is the final Unity Scene view capture.

The prepare, apply, finish, save and verify extension scripts record this operation. Do not rerun the preparation or application against a later user edited scene without taking a fresh survey and reviewing the route.
