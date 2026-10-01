"""Prepare the approved DEM alignment using a live terrain height export."""
import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
WORK = ROOT / "Temp/CodexDemIntegration"
OUT = ROOT / "Assets/TerrainData/ShitanjingDEM"
OUT.mkdir(parents=True, exist_ok=True)
before = json.loads((WORK / "before.json").read_text(encoding="utf-8-sig"))
proposal = json.loads((HERE / "alignment_proposal.json").read_text(encoding="utf-8-sig"))
origin = np.array(before["origin"])[[0, 2]]
offset = np.array([-2000.0, -3000.0])
size = 6000.0
resolution = 1537


def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def sample(arr, u, v):
    u = np.clip(u, 0, arr.shape[1] - 1.000001)
    v = np.clip(v, 0, arr.shape[0] - 1.000001)
    i, j = u.astype(int), v.astype(int)
    f, g = u - i, v - j
    return (arr[j, i] * (1 - f) * (1 - g) + arr[j, i + 1] * f * (1 - g)
            + arr[j + 1, i] * (1 - f) * g + arr[j + 1, i + 1] * f * g)


def rectangle_weight(x, z, rect, fade):
    xmin, xmax, zmin, zmax = rect
    dx = np.maximum(np.maximum(xmin - x, x - xmax), 0)
    dz = np.maximum(np.maximum(zmin - z, z - zmax), 0)
    return 1 - smooth(np.hypot(dx, dz) / fade)


def factory_weight(x, z):
    main = rectangle_weight(x, z, [-1520, -1170, -1215, -985], 110)
    railway = rectangle_weight(x, z, [-1690, -1430, -1250, -1180], 75)
    return np.maximum(main, railway)


def corrections(h, x, z):
    # Factory and railway platform at the current terrain base, world Y = -710.
    h = h * (1 - factory_weight(x, z))
    # Only these two tower footings were materially displaced before this change.
    for tower in proposal["towers"][1:3]:
        tx, _, tz = tower["world_position"]
        weight = 1 - smooth((np.hypot(x - tx, z - tz) - 10) / 35)
        target = (tower["target_ground_y"] - before["origin"][1]) / before["height"]
        h = h * (1 - weight) + target * weight
    return h


def tile_grid(tile):
    n = tile["resolution"]
    p, s = tile["position"], tile["size"]
    return np.meshgrid(np.linspace(p[0], p[0] + s[0], n),
                       np.linspace(p[2], p[2] + s[2], n))


# Export targeted edits separately for live application before Locks are read.
edits = []
tiles = []
for tile in before["terrains"]:
    n = tile["resolution"]
    h = np.fromfile(WORK / tile["filename"], dtype="<f4").reshape(n, n)
    x, z = tile_grid(tile)
    adjusted = corrections(h, x, z).astype("<f4")
    changed = np.abs(adjusted - h) * before["height"] > 0.001
    if changed.any():
        filename = f"adjusted_{tile['index']}.f32"
        adjusted.tofile(WORK / filename)
        edits.append({"index": tile["index"], "filename": filename,
                      "changed_samples": int(changed.sum()),
                      "max_delta_m": float(np.max(np.abs(adjusted - h)) * before["height"])})
    tiles.append((tile, h))

x, z = np.meshgrid(np.linspace(origin[0] + offset[0], origin[0] + offset[0] + size, resolution),
                   np.linspace(origin[1] + offset[1] + size, origin[1] + offset[1], resolution))
t = np.deg2rad(proposal["world_to_source_rotation_deg"])
dx, dz = x - proposal["anchor_world_xz"][0], z - proposal["anchor_world_xz"][1]
east = proposal["anchor_source_east_north_m"][0] + np.cos(t) * dx - np.sin(t) * dz
north = proposal["anchor_source_east_north_m"][1] + np.sin(t) * dx + np.cos(t) * dz
elevation = np.array(Image.open(HERE / "Shitanjing_6km_elevation.tif"), dtype=float)
h = (sample(elevation, east / 24, (6000 - north) / 24) - 1535.319458) / 719.047241

# Preserve an elongated mountain along the cableway, without circular Lock outlines.
old = np.full(h.shape, np.nan)
for tile, adjusted in sorted(tiles, key=lambda item: not item[0]["isDraft"]):
    p, s, n = tile["position"], tile["size"], tile["resolution"]
    valid = (x >= p[0] - 0.001) & (x <= p[0] + s[0] + 0.001) & (z >= p[2] - 0.001) & (z <= p[2] + s[2] + 0.001)
    old[valid] = sample(adjusted, (x[valid] - p[0]) / s[0] * (n - 1),
                        (z[valid] - p[2]) / s[2] * (n - 1))
distance = np.full(h.shape, np.inf)
route = np.array([tower["world_position"] for tower in proposal["towers"]])[:, [0, 2]]
for start, end in zip(route[:-1], route[1:]):
    segment = end - start
    t = np.clip(((x - start[0]) * segment[0] + (z - start[1]) * segment[1]) / np.dot(segment, segment), 0, 1)
    distance = np.minimum(distance, np.hypot(x - start[0] - t * segment[0], z - start[1] - t * segment[1]))
preserve = 1 - smooth((distance - 65) / 200)
if np.any((preserve > 0) & ~np.isfinite(old)):
    raise RuntimeError("Missing terrain samples inside a protected transition collar")
h = h * (1 - preserve) + np.nan_to_num(old) * preserve
h = corrections(h, x, z)

# Both the rotated DEM footprint and the import square fade back to the old graph.
edge = np.minimum.reduce([east, north, 6000 - east, 6000 - north,
                          x - origin[0] - offset[0], origin[0] + offset[0] + size - x,
                          z - origin[1] - offset[1], origin[1] + offset[1] + size - z])
coverage = smooth(edge / 600)
for name, matrix in [("Shitanjing_AlignedHeight", h), ("Shitanjing_Coverage", coverage)]:
    raw = np.rint(np.clip(matrix, 0, 1) * 65535).astype("<u2")
    raw.tofile(OUT / f"{name}.raw")
    assert np.array_equal(raw, np.fromfile(OUT / f"{name}.raw", dtype="<u2").reshape(raw.shape))

config = {"resolution": resolution, "size_m": size, "import_scale": 6,
          "import_offset": offset.tolist(), "world_origin": (origin + offset).tolist(),
          "world_base_y": before["origin"][1], "height_range_m": before["height"],
          "dem_rotation_degrees": proposal["world_to_source_rotation_deg"],
          "cableway_preserve_halfwidth_m": 65, "cableway_outer_blend_m": 200, "dem_boundary_blend_m": 600,
          "factory_core_rect": [-1520, -1170, -1215, -985],
          "railway_core_rect": [-1690, -1430, -1250, -1180],
          "edits": edits, "dem_full_weight_fraction": float(np.mean(coverage > 0.999)),
          "status": "prepared, not yet applied"}
(HERE / "integration_config.json").write_text(json.dumps(config, indent=2), encoding="utf-8")
print(json.dumps(config, indent=2))
