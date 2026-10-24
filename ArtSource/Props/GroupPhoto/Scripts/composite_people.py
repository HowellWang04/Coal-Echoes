"""Put the generated workers back onto the original render, so that the gate, the banner, the
guardhouse and the dormitory in the Group Photo are the game's own pixels and only the people and
their contact shadows come from the image generator.
The generated image is a lower-resolution redraw of the whole frame. It is aligned to the render
with SIFT and a homography, brought to the render's tones with a smooth gain map measured outside
the group, and the people are cut out where it differs from the render. Nothing above BANNER_CLEAR
is taken from it, so the banner always stays the render's.
Usage: python composite_people.py <generated.png> <render.png> <output.png>
"""
import sys
import cv2
import numpy as np
from scipy.ndimage import binary_fill_holes

WORK = (1920, 1080)          # matching and masking resolution
BANNER_CLEAR = 470           # rows (at WORK) that always keep the render: the banner ends at ~430
PEOPLE_DIFF = 28             # the redrawn ground differs by up to ~21, people by far more
SHADOW_DARKEN = 7            # how much darker than the render the ground must be to count as shadow
src, render, out = sys.argv[1], sys.argv[2], sys.argv[3]

gen = cv2.imread(src)
ref = cv2.imread(render)
full = ref.shape[1], ref.shape[0]
ref_w = cv2.resize(ref, WORK, interpolation=cv2.INTER_AREA)
gen_w = cv2.resize(gen, WORK, interpolation=cv2.INTER_CUBIC)

# ---- alignment ----
sift = cv2.SIFT_create(8000)
k1, d1 = sift.detectAndCompute(cv2.cvtColor(gen_w, cv2.COLOR_BGR2GRAY), None)
k2, d2 = sift.detectAndCompute(cv2.cvtColor(ref_w, cv2.COLOR_BGR2GRAY), None)
good = [a for a, b in cv2.BFMatcher().knnMatch(d1, d2, k=2) if a.distance < .75 * b.distance]
p1 = np.float32([k1[a.queryIdx].pt for a in good])
p2 = np.float32([k2[a.trainIdx].pt for a in good])
H, inliers = cv2.findHomography(p1, p2, cv2.RANSAC, 2.0)
inliers = inliers.ravel().astype(bool)
err = np.linalg.norm(cv2.perspectiveTransform(p1[inliers][None], H)[0] - p2[inliers], axis=1)
print('aligned on %d of %d matches, mean error %.2f px at %dx%d' % (inliers.sum(), len(good), err.mean(), *WORK))
gen_w = cv2.warpPerspective(gen_w, H, WORK, flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)
# The same mapping straight from the generated pixels to the full-size render.
to_work = np.diag([WORK[0] / gen.shape[1], WORK[1] / gen.shape[0], 1])
to_full = np.diag([full[0] / WORK[0], full[1] / WORK[1], 1])
gen_full = cv2.warpPerspective(gen, to_full @ H @ to_work, full, flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)

# ---- people ----
gen_f, ref_f = gen_w.astype(np.float32), ref_w.astype(np.float32)
diff = np.abs(cv2.GaussianBlur(gen_f, (0, 0), 2) - cv2.GaussianBlur(ref_f, (0, 0), 2)).max(2)
core = (diff > PEOPLE_DIFF).astype(np.uint8)
core[:BANNER_CLEAR] = 0
core = cv2.morphologyEx(core, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
# The group is the largest blob once neighbours are merged.
merged = cv2.morphologyEx(core, cv2.MORPH_CLOSE, np.ones((41, 41), np.uint8))
n, labels, stats, _ = cv2.connectedComponentsWithStats(merged)
main = 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])
x, y, w, h = stats[main, :4]
y = max(BANNER_CLEAR, y - 10)
h = stats[main, 1] + h - y
print('group at x %d-%d, y %d-%d (%dx%d)' % (x, x + w, y, y + h, *WORK))
# A worker in a grey jacket in front of the grey dormitory barely differs from it, so a plain
# threshold loses heads and shoulders. Take each column from the group's highest differing pixel
# down to its lowest instead: the gaps between people are then the generated background, which is
# aligned and only a little softer than the render.
people = np.zeros_like(core)
for cx in range(x, x + w):
    rows = np.flatnonzero(core[y:y + h, cx])
    if len(rows):
        people[y + rows[0] - 2:y + rows[-1] + 1, cx] = 1
people = cv2.morphologyEx(people, cv2.MORPH_OPEN, np.ones((1, 5), np.uint8))   # lone columns
people = binary_fill_holes(people).astype(np.uint8)

# ---- tones ----
# The generator redrew the ground a little brighter. Measure render / generated over the ground
# around the group, spread it smoothly under the group, and scale the generated image by it so its
# people and shadows sit on the render's ground.
outside = cv2.dilate(people, np.ones((31, 31), np.uint8)) == 0
weight = cv2.GaussianBlur(outside.astype(np.float32), (0, 0), 40)
gain = np.dstack([cv2.GaussianBlur(ref_f[..., c] * outside, (0, 0), 40) /
                  np.maximum(cv2.GaussianBlur(gen_f[..., c] * outside, (0, 0), 40), 1e-3) for c in range(3)])
gain = np.where(weight[..., None] > 1e-3, gain, 1)
gen_f *= gain

# ---- contact shadows ----
# Ground under and just around the group that the generator made darker than the render.
lum = lambda im: im @ np.array([.114, .587, .299], np.float32)
dark = cv2.GaussianBlur(lum(ref_f) - lum(gen_f), (0, 0), 3)
zone = np.zeros_like(people)
zone[y + h // 2:y + h + 30, max(0, x - 40):x + w + 120] = 1
shadow = np.clip((dark - SHADOW_DARKEN) / 10, 0, 1) * zone
shadow = cv2.GaussianBlur(shadow, (0, 0), 2)

alpha = np.maximum(cv2.GaussianBlur(people.astype(np.float32), (0, 0), 2), shadow)
alpha[:BANNER_CLEAR] = 0

# ---- composite at full size ----
alpha_full = cv2.resize(alpha, full, interpolation=cv2.INTER_LINEAR)[..., None]
gain_full = cv2.resize(gain, full, interpolation=cv2.INTER_LINEAR)
result = ref.astype(np.float32) * (1 - alpha_full) + gen_full.astype(np.float32) * gain_full * alpha_full
cv2.imwrite(out, np.clip(result + .5, 0, 255).astype(np.uint8))
print('saved', out, full)
