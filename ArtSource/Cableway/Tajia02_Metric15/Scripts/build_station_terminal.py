"""Return terminal for the foothill station: horizontal return wheel, its support frame and
the U-shaped transfer rail on which buckets leave the rope.

Exec in the namespace of build_tower.py (helpers and materials). Metres, z up.
Origin: on the line axis below the two entry roller frames, at their ground level.
+Y points back towards Tower 01. The ropes run at x = +/-2 (4 m gauge) at ROPE_Z.
"""
import bpy
import math

ROPE_Z = 4.85                 # top of the roller frame sheaves
WHEEL_Y = -8.0                # return wheel centre, 8 m past the roller frames
WHEEL_R = 2.0                 # rope radius on the wheel = half the gauge
RAIL_R = 2.15
RAIL_Z = ROPE_Z - .51         # rail centre; bucket carriage wheels sit .46 below the rope
RAIL_START = -1.0
POST_R = 2.85

term_scene = bpy.data.scenes.new('Tajia02_Terminal')
bpy.context.window.scene = term_scene
parts = bpy.data.collections.new('Terminal_Parts')
term_scene.collection.children.link(parts)

conc = material('Terminal_Concrete', 2)

# ---------------------------------------------------------------- return wheel support
box('Wheel foundation', (0, WHEEL_Y, -.05), (2.0, 2.0, .70), conc, .02)
TOP = ROPE_Z - .45
for sx in (-1, 1):
    for sy in (-1, 1):
        angle('Wheel stand leg', (sx * .65, WHEEL_Y + sy * .65, .30), (sx * .55, WHEEL_Y + sy * .55, TOP), .100, .008, (-sx, -sy))
        box('Wheel stand sole plate', (sx * .65, WHEEL_Y + sy * .65, .3125), (.30, .30, .025), bare, .002)
        for bx in (-.09, .09):
            bolt((sx * .65 + bx, WHEEL_Y + sy * .65 + .09, .325), (0, 0, 1), .024)
for z in (1.6, 2.9, TOP - .05):
    h = .65 - .10 * (z - .3) / (TOP - .3)
    for s in (-1, 1):
        angle('Wheel stand horizontal', (-h, WHEEL_Y + s * h, z), (h, WHEEL_Y + s * h, z), .075, .006, (1, -s), paint, (0, 0, 1))
        angle('Wheel stand horizontal', (s * h, WHEEL_Y - h, z), (s * h, WHEEL_Y + h, z), .075, .006, (1, -s), paint, (0, 0, 1))
for z0, z1 in ((.35, 1.6), (1.6, 2.9), (2.9, TOP - .05)):
    for s in (-1, 1):
        angle('Wheel stand brace', (-.6, WHEEL_Y + s * .6, z0 + .05), (.6, WHEEL_Y + s * .6, z1 - .05), .063, .005, (1, 1), bare)
        angle('Wheel stand brace', (s * .6, WHEEL_Y - .6, z0 + .05), (s * .6, WHEEL_Y + .6, z1 - .05), .063, .005, (1, 1), bare)
box('Bearing platform plate', (0, WHEEL_Y, TOP + .015), (1.5, 1.5, .03), paint, .003)
box('Bearing housing', (0, WHEEL_Y, TOP + .14), (.42, .42, .22), bare, .006)
cylinder('Wheel shaft', (0, WHEEL_Y, ROPE_Z - .10), .085, .65, (0, 0, 1), 16, bare)
# Drive: gearbox and motor on the bearing platform.
box('Reduction gearbox', (.45, WHEEL_Y + .35, TOP + .25), (.50, .40, .45), bare, .01)
cylinder('Drive motor', (.45, WHEEL_Y + .78, TOP + .22), .17, .48, (0, 1, 0), 16, bare)
for dx in (-.15, .15):
    for dy in (-.12, .12):
        bolt((.45 + dx, WHEEL_Y + .35 + dy, TOP + .03), (0, 0, 1), .016)

# ---------------------------------------------------------------- return wheel
cylinder('Wheel hub', (0, WHEEL_Y, ROPE_Z), .26, .32, (0, 0, 1), 20, bare)
SEG = 48
for k in range(SEG):
    a0, a1 = 2 * math.pi * k / SEG, 2 * math.pi * (k + 1) / SEG
    p0 = (WHEEL_R * math.cos(a0), WHEEL_Y + WHEEL_R * math.sin(a0))
    p1 = (WHEEL_R * math.cos(a1), WHEEL_Y + WHEEL_R * math.sin(a1))
    # Grooved rim: two flanges either side of the rope plus a web inside.
    for dz in (-.055, .055):
        prism('Wheel rim flange', [(-.035, -.012), (.035, -.012), (.035, .012), (-.035, .012)],
              (p0[0], p0[1], ROPE_Z + dz), (p1[0], p1[1], ROPE_Z + dz), paint, 0, (0, 0, 1))
    q0 = ((WHEEL_R - .05) * math.cos(a0), WHEEL_Y + (WHEEL_R - .05) * math.sin(a0))
    q1 = ((WHEEL_R - .05) * math.cos(a1), WHEEL_Y + (WHEEL_R - .05) * math.sin(a1))
    prism('Wheel rim web', [(-.012, -.07), (.012, -.07), (.012, .07), (-.012, .07)], (q0[0], q0[1], ROPE_Z), (q1[0], q1[1], ROPE_Z), paint, 0, (0, 0, 1))
for k in range(8):
    a = 2 * math.pi * k / 8
    c, s = math.cos(a), math.sin(a)
    for dz in (-.08, .08):
        rod('Wheel spoke', (.24 * c, WHEEL_Y + .24 * s, ROPE_Z + dz), ((WHEEL_R - .06) * c, WHEEL_Y + (WHEEL_R - .06) * s, ROPE_Z + dz * .4), .032, 8, paint)

# ---------------------------------------------------------------- U-shaped transfer rail
def rail_seg(a, b):
    # Flat-topped bar rail 60 x 100 mm with a top cap for the carriage wheels.
    prism('Transfer rail web', [(-.012, -.05), (.012, -.05), (.012, .03), (-.012, .03)], (*a, RAIL_Z), (*b, RAIL_Z), bare, 0, (0, 0, 1))
    prism('Transfer rail head', [(-.03, .03), (.03, .03), (.03, .05), (-.03, .05)], (*a, RAIL_Z), (*b, RAIL_Z), bare, 0, (0, 0, 1))


path = [(RAIL_R, RAIL_START), (RAIL_R, WHEEL_Y)]
for k in range(1, 25):
    a = math.pi * k / 24
    path.append((RAIL_R * math.cos(a), WHEEL_Y - RAIL_R * math.sin(a)))
path.append((-RAIL_R, RAIL_START))
for a, b in zip(path, path[1:]):
    rail_seg(a, b)
# Entry ramps where the carriage leaves the rope: rail rises slightly to meet the rope line.
for sx in (-1, 1):
    rail_seg((sx * RAIL_R, RAIL_START), (sx * RAIL_R, RAIL_START + .6))


def rail_post(px, py, rx, ry):
    """Square-tube post with a cantilever bracket and hanger plate reaching the rail at (rx, ry)."""
    box('Rail post foot', (px, py, .15), (.45, .45, .30), conc, .015)
    prism('Rail post', [(-.06, -.06), (.06, -.06), (.06, .06), (-.06, .06)], (px, py, .30), (px, py, RAIL_Z + .30), paint, .003)
    channel('Rail bracket', (px, py, RAIL_Z + .24), (rx, ry, RAIL_Z + .24), .100, .048, .0053, paint, (0, 0, 1))
    box('Rail hanger plate', (rx, ry, RAIL_Z + .14), (.012, .12, .20), bare, .002)
    angle('Rail post knee', (px, py, RAIL_Z - .25), ((px * 2 + rx) / 3, (py * 2 + ry) / 3, RAIL_Z + .20), .05, .005, (1, 1), bare)


for y in (-1.6, -4.6, -7.4):
    for sx in (-1, 1):
        rail_post(sx * POST_R, y, sx * RAIL_R, y)
for deg in (45, 90, 135):
    a = math.radians(deg)
    rail_post(POST_R * math.cos(a), WHEEL_Y - POST_R * math.sin(a), RAIL_R * math.cos(a), WHEEL_Y - RAIL_R * math.sin(a))

term_scene['rope_z'] = ROPE_Z
term_scene['wheel_y'] = WHEEL_Y
print('TERMINAL', len(parts.objects), 'parts')
