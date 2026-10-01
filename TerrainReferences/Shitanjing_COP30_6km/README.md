# Shitanjing terrain crop

Prepared crop covering approximately 6 km by 6 km, selected toward the southern ridge and adjacent valley.

## Files

Shitanjing_6km_elevation.tif preserves geographic coordinates and elevation in metres on an interpolated 251 by 251 grid.

Shitanjing_6km_3073_u16le.raw is square unsigned 16 bit RAW in PC byte order, north row first, suitable for the installed MapMagic Imported Map loader. Its 3073 samples span six 512-interval tiles. Interpolation does not add measured detail to the original roughly 30 metre data.

crop_preview.png shows the chosen footprint and a 6 by 6 grid of 1 km tiles.

metadata.json records coordinates, normalization, dimensions, and file hashes.

## Height mapping

The crop spans 1535.319 to 2254.367 metres of source elevation, a difference of 719.047 metres. RAW zero represents 1535.319 metres and RAW maximum represents 2254.367 metres. This is not yet assigned to the game's world Y coordinates. Using a global Height of 250 directly would compress this relief to 250 metres.

## Integration status

No scene or graph has been modified. Factory and cableway placement, Import offset, and a transition mask to surrounding procedural terrain remain to be configured. The installed Import scale is relative to one terrain tile, so a 1000 metre tile needs scale 6 for this full map, not scale 1. Existing Noise can provide outer terrain through a world-space feathered mask.

## Source

Copernicus GLO 30 distributed by OpenTopography, supplied by the user as rasters_COP30.tar.gz. Original dataset and attribution terms are at https://portal.opentopography.org/datasetMetadata?otCollectionID=OT.032021.4326.1 .
