"""Ground mark for the Group Photo retake spot: a warm glowing ring with a faint fill, projected
by an HDRP decal in front of the factory gate. Alpha carries the shape; colour is a pale gold close
to the echo viewfinder's yellow. The ring sits at 0.43 of the texture width, so a 2.8 m projector
draws it about 1.2 m out from the centre, matching the echo's stand radius.
Usage: python make_retake_circle.py <output.png>
"""
import sys
import numpy as np
from PIL import Image

N = 1024
yy, xx = np.mgrid[0:N, 0:N]
r = np.hypot(xx - N / 2 + .5, yy - N / 2 + .5) / N            # 0 at the centre, 0.5 at the edge
ring = .43
core = np.exp(-((r - ring) / .006) ** 2)
glow = np.exp(-((r - ring) / .035) ** 2) * .45
fill = np.clip((ring - r) / .05, 0, 1) * .10                   # faint light inside the ring
alpha = np.clip(core + glow + fill, 0, 1)
alpha *= np.clip((.5 - r) / .02, 0, 1)                         # nothing reaches the projector edge
rgb = np.ones((N, N, 3)) * np.array([255, 218, 150]) / 255
rgb = rgb * (1 - .25 * core[..., None]) + .25 * core[..., None]   # the thin core runs whiter
img = np.dstack([rgb, alpha])
Image.fromarray((img * 255).astype(np.uint8), 'RGBA').save(sys.argv[1])
print('saved', sys.argv[1])
