# Sheep Range terrain inspection

Current cableway placement is recorded in [ridge_extension_notes.md](ridge_extension_notes.md) and `ridge_extension.json`. The preceding factory move is recorded in [ridge_revision_notes.md](ridge_revision_notes.md). Earlier layout files below are historical.

For the subsequent Unity integration and layout changes, see [integration_notes.md](integration_notes.md). The inspection below records the initial download review.

Inspected the two user supplied USGS GeoTIFF files on 2026-09-24.

Both files decode successfully as 10012 by 10012 float32 rasters in EPSG 26911, with 1 metre pixel spacing. Nodata is -999999. The nominal 10 km tiles contain a 6 pixel border. The north tile is x65y408; x65y407 is immediately south. The overview removes the borders and joins their nominal cores without stretching them.

The north file has 54.594 percent valid coverage and elevations from 1245.756 to 2156.620 m. The south file has 52.782 percent valid coverage and elevations from 1485.152 to 2821.245 m. Western parts contain nodata. These files do not provide a complete axis aligned 6 km square. Black areas in overview.png are missing data, not low terrain.

Candidate A is a fully valid 4 km square at UTM bounds west 655800, south 4073000, east 659800, north 4077000. Elevations range from 1363.600 to 2077.573 m, a relief of 713.973 m. It includes a mountain front and adjoining gentler terrain. Factory suitability and cableway alignment have not been tested.

candidate_and_detail.png shows a 4 km overview sampled every 5 m and a 1 km detail rendered from native 1 m samples. The lower comparison is a simulated 30 m version of that same patch made using area averaging and bilinear resampling, not an actual COP30 comparison. The gold rectangle identifies the detail area. These are calculated hillshade previews, not satellite imagery or Unity renders. The overview is sampled at 10 m for inspection only.

At the initial inspection stage, no Unity scene, graph, locks or terrain import had been changed. Integration was subsequently authorized and is documented separately in integration_notes.md.

## Agreed layout direction

The user prioritizes the real terrain shape and allows relocating the factory area and cableway towers to fit it. Existing world positions are no longer fixed layout constraints.

Proposed placement puts the factory on the gentler eastern mountain front and routes the cableway toward suitable ridges. The factory should be relocated as a coherent group while preserving its internal layout and associated interactions. Cableway towers and cable geometry need to be planned together. The initial candidate required position, slope and route checks. Subsequent measurements and the implemented placement are recorded in layout_config.json and verification.json.

Prefer local foundation grading after placement. Existing factory and tower locks belong to the old layout and must be reassessed when implementing the new layout. The eastern terrain still requires local grading. Existing source assets are retained. See the integration record for the subsequent object moves.
