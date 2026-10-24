"""The guardhouse wall in its two eras, made from the re-tiled Concrete Wall 007 colour.
Called by break_tiling.py for concrete_wall_007.

aged(): 2026, in the game. The scan's stains are deepened and water stains added, then two kinds
of weathering placed by height: a damp, dusty band along the foot of the wall and dirt streaks
washed down from under the roof.
clean(): 1980, swapped in when the Group Photo background is rendered. The stains are cut to a
third and the plaster lightened a little; the trowel marks stay.

Height can be painted into a tiling texture because the wall material is an object-space
triplanar projection tiled once per `repeat` metres: on a wall, texture v is the guardhouse's
object-space height / repeat, so the bottom row of the image sits at its floor (y = 0). The wall
faces span WALL_FOOT to WALL_TOP; rows above are never seen on a wall. Horizontal faces (window
reveals) take v from depth instead and pick up whatever those rows hold.
"""
import numpy as np

WALL_FOOT, WALL_TOP = .11, 3.18  # object-space heights of the wall faces, metres


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def wrap_blur(img, sigma):
    """Gaussian blur of a 2-D map that wraps at the edges, so the texture stays seamless."""
    ky = np.fft.fftfreq(img.shape[0])[:, None]
    kx = np.fft.rfftfreq(img.shape[1])[None, :]
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (ky ** 2 + kx ** 2))
    return np.fft.irfft2(np.fft.rfft2(img) * g, s=img.shape).astype(np.float32)


def noise(rng, n, sigma):
    """Seamless smooth noise with unit standard deviation, blobs about sigma pixels across."""
    x = wrap_blur(rng.standard_normal((n, n)), sigma)
    return x / x.std()


def noise1d(rng, n, sigma):
    k = np.fft.rfftfreq(n)
    x = np.fft.irfft(np.fft.rfft(rng.standard_normal(n)) * np.exp(-2 * (np.pi * sigma * k) ** 2), n)
    return (x / x.std()).astype(np.float32)


def tint(*rgb):
    return np.array(rgb, np.float32)


def aged(color, repeat):
    rng = np.random.default_rng(2026)
    n = color.shape[0]
    px = n / repeat  # pixels per metre
    # Height of each row above the guardhouse origin; rows above the walls are taken as below
    # the floor, so the foot band carries on smoothly past the bottom edge of the image.
    y = (n - .5 - np.arange(n, dtype=np.float32)) / px
    y = np.where(y > repeat * .7, y - repeat, y)[:, None]

    mean = color.mean(axis=(0, 1), dtype=np.float64).astype(np.float32)  # float32 sums drift
    color = mean + (color - mean) * 1.4  # the scan's own stains, deepened

    # Water stains: soft blotches over about a quarter of the wall, browner than the plaster.
    blot = .7 * noise(rng, n, .3 * px) + .3 * noise(rng, n, .08 * px)
    color *= 1 - smoothstep(.4, 1.8, blot)[:, :, None] * tint(.07, .09, .12)

    # Foot of the wall: rising damp and splashed dust, darkest at the floor, under a ragged edge
    # with a faint tide line.
    h = y - WALL_FOOT
    edge = .42 + .08 * noise1d(rng, n, .4 * px)[None, :] + .025 * noise(rng, n, .025 * px)
    foot = (1 - smoothstep(edge - .14, edge + .01, h)) * smoothstep(-.3, 0, h)
    foot *= .35 + .65 * (1 - np.clip(h / edge, 0, 1)) ** 1.5
    foot *= .8 + .2 * np.clip(noise(rng, n, .015 * px), -1, 1)
    tide = np.exp(-((h - edge) / .01) ** 2) * smoothstep(0, 1, noise(rng, n, .1 * px))
    splash = rng.random((n, n)) < .006 * np.exp(-np.maximum(h, 0) / .1) * smoothstep(-.1, 0, h)
    splash = np.minimum(wrap_blur(splash.astype(np.float32), 1.2) * 2 * np.pi * 1.2 ** 2, 1)
    color *= 1 - (foot * .24 + tide * .05 + splash * .25)[:, :, None] * tint(.9, 1, 1.2)

    # Under the roof: a grimy strip, and streaks washed down from it, bunched where the roof
    # edge drips most. Many are short, a few run past head height.
    d = WALL_TOP - y  # depth below the roof
    grime = np.exp(-np.maximum(d, 0) / .1) * smoothstep(-.4, -.1, d)
    streaks = np.zeros((n, n), np.float32)
    drips = noise1d(rng, n, .5 * px)
    count = 0
    while count < 46:
        x0 = rng.integers(n)
        if rng.random() > smoothstep(-1, 1.5, drips[x0]):
            continue
        count += 1
        width = np.exp(rng.uniform(np.log(.004), np.log(.05))) * px
        length = .2 + 1.6 * rng.random() ** 2
        strength = rng.uniform(.5, 1) * min(1, (4 / width) ** .3)  # wide streaks are fainter
        rows = np.arange(int(n - (WALL_TOP + .1) * px), min(n, int(n - (WALL_TOP - length) * px)))
        dd = WALL_TOP - (n - .5 - rows) / px
        along = (1 - np.clip(dd / length, 0, 1)) ** 1.3 * smoothstep(-.1, 0, dd)
        sway = .006 * px * np.sin(dd * rng.uniform(4, 9) + rng.uniform(0, 2 * np.pi))
        half = int(3 * width + .006 * px) + 2
        cols = np.arange(x0 - half, x0 + half + 1)
        across = np.exp(-((cols[None, :] - x0 - sway[:, None]) / width) ** 2)
        streaks[np.ix_(rows, cols % n)] += strength * along[:, None] * across
    streaks = np.minimum(streaks, 1.2) * (.6 + .4 * smoothstep(-1.5, 1.5, noise(rng, n, .01 * px)))
    color *= 1 - (grime * .12 + streaks * .18)[:, :, None] * tint(.95, 1, 1.08)
    return np.clip(color, 0, 1)


def clean(color, repeat):
    px = color.shape[0] / repeat
    mean = color.mean(axis=(0, 1), dtype=np.float64).astype(np.float32)
    base =np.stack([wrap_blur(color[:, :, i], .01 * px) for i in range(3)], 2)
    # Stains live above a centimetre; below it are the trowel marks and pores of fresh plaster.
    return np.clip(mean * 1.05 + (base - mean) / 3 + (color - base) * .8, 0, 1)
