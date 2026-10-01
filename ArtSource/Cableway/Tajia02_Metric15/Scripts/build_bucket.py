"""Real-size monocable coal bucket (about 1 m3): rope grip, rail carriage, hanger and tipping trough.

Exec in the namespace of build_tower.py (helpers and materials). Metres, z up.
Origin is the rope centre at the grip; +Y runs along the rope. The carriage wheels sit
0.46 m below the rope so they can run onto the station transfer rail (build_station_terminal.py).
Overall height about 2.7 m; the trough is 1.5 m long, 1.1 m wide and 0.9 m deep.
"""
import bpy
import math

bucket_scene = bpy.data.scenes.new('Tajia02_Bucket')
bpy.context.window.scene = bucket_scene
parts = bpy.data.collections.new('Bucket_Parts')
bucket_scene.collection.children.link(parts)

# ---------------------------------------------------------------- grip and carriage
box('Grip jaw body', (0, 0, -.02), (.10, .30, .12), bare, .008)
box('Grip jaw lip', (-.045, 0, .035), (.025, .26, .05), bare, .004)
cylinder('Grip spring housing', (.07, 0, -.05), .045, .26, (0, 1, 0), 12, bare)
box('Carriage side plate', (.065, 0, -.27), (.012, .46, .34), paint, .003)
box('Carriage side plate', (-.065, 0, -.27), (.012, .46, .34), paint, .003)
for y in (-.17, .17):
    cylinder('Carriage rail wheel', (0, y, -.40), .060, .045, (1, 0, 0), 16, bare)
    cylinder('Carriage wheel axle', (0, y, -.40), .014, .16, (1, 0, 0), 8, bare)
    bolt((.072, y, -.40), (1, 0, 0), .016)
    bolt((-.072, y, -.40), (-1, 0, 0), .016)
# ---------------------------------------------------------------- trough size (about 1 m3)
LEN = 1.50
DEPTH = .90
HALF_TOP_W, HALF_BOT_W, T = .55, .29, .006
PIVOT_Z = -1.72
TOP_Z = PIVOT_Z + .04
ARM_Y = LEN / 2 + .05          # yoke drop arms just outside the trough end plates

# ---------------------------------------------------------------- hanger
prism('Hanger tube', [(-.035, -.045), (.035, -.045), (.035, .045), (-.035, .045)], (0, 0, -.44), (0, 0, PIVOT_Z + .30), paint, .003)
for s in (-1, 1):
    angle('Hanger yoke strut', (0, 0, PIVOT_Z + .62), (0, s * (ARM_Y - .04), PIVOT_Z + .28), .05, .005, (1, 1), paint)
channel('Hanger yoke beam', (0, -ARM_Y - .04, PIVOT_Z + .25), (0, ARM_Y + .04, PIVOT_Z + .25), .10, .05, .006, paint, (0, 0, 1))
for s in (-1, 1):
    box('Yoke drop arm', (0, s * ARM_Y, PIVOT_Z + .12), (.08, .012, .30), paint, .003)
    cylinder('Trough pivot pin', (0, s * (LEN / 2 + .025), PIVOT_Z), .025, .08, (0, 1, 0), 10, bare)
    bolt((0, s * (ARM_Y + .006), PIVOT_Z), (0, s, 0), .020)

# ---------------------------------------------------------------- tipping trough
outer = [(TOP_Z, -HALF_TOP_W), (TOP_Z - DEPTH, -HALF_BOT_W), (TOP_Z - DEPTH, HALF_BOT_W), (TOP_Z, HALF_TOP_W)]
inner = [(TOP_Z, HALF_TOP_W - T * 1.2), (TOP_Z - DEPTH + T, HALF_BOT_W - T), (TOP_Z - DEPTH + T, -HALF_BOT_W + T), (TOP_Z, -HALF_TOP_W + T * 1.2)]
# Profile is (z, x) because prism maps profile axes to (Z, X) for a rope-aligned (Y) axis.
prism('Trough shell', outer + inner, (0, -LEN / 2, 0), (0, LEN / 2, 0), paint, .002)
for s in (-1, 1):
    y = s * (LEN / 2 + .003)
    pts = [(-HALF_TOP_W, TOP_Z), (HALF_TOP_W, TOP_Z), (HALF_BOT_W, TOP_Z - DEPTH), (-HALF_BOT_W, TOP_Z - DEPTH)]
    mesh_obj('Trough end plate', [(x, y + dy, z) for dy in (-.003, .003) for x, z in pts],
             [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], paint, .002)
    box('Pivot boss plate', (0, s * (LEN / 2 + .012), PIVOT_Z), (.18, .012, .18), bare, .003)
# Rolled top rim and stiffening ribs.
for sx in (-1, 1):
    rod('Trough top rim', (sx * HALF_TOP_W, -LEN / 2, TOP_Z), (sx * HALF_TOP_W, LEN / 2, TOP_Z), .016, 8, paint)
for y in (-.45, -.15, .15, .45):
    for sx in (-1, 1):
        prism('Trough rib', [(-.03, -.005), (.03, -.005), (.03, .005), (-.03, .005)],
              (sx * (HALF_TOP_W + .007), y, TOP_Z - .02), (sx * (HALF_BOT_W + .007), y, TOP_Z - DEPTH + .02), paint, 0, (0, 1, 0))
    prism('Trough rib', [(-.03, -.005), (.03, -.005), (.03, .005), (-.03, .005)],
          (-HALF_BOT_W, y, TOP_Z - DEPTH - .007), (HALF_BOT_W, y, TOP_Z - DEPTH - .007), paint, 0, (0, 1, 0))
# Tipping latch: lever on one end that holds the trough level until it is tripped.
angle('Latch lever', (.18, LEN / 2 + .03, TOP_Z + .04), (.44, LEN / 2 + .03, TOP_Z - .34), .04, .004, (1, 1), bare)
box('Latch catch', (.17, LEN / 2 + .025, TOP_Z + .06), (.06, .02, .06), bare, .002)
print('BUCKET', len(parts.objects), 'parts')
