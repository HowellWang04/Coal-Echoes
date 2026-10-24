"""Deterministic PBR textures for the Coal Echoes entrance gate.

Run through build_gate.py in Blender. Color maps contain reflectance only;
lighting is supplied by Blender / Unity. Normals use the OpenGL convention.
"""

from pathlib import Path
import gc

import bpy
import numpy as np

# Steel sheet width of one leaf in metres. The damage layout was tuned on a
# 2.582 m sheet; STRETCH keeps rust, chips and holes at their physical size.
SHEET_WIDTH = 6.119
STRETCH = SHEET_WIDTH / 2.582
SHEET_PIXELS = (3328, 1536)


def noise(size, cells, seed, cells_y=None, width=None):
    rng = np.random.default_rng(seed)
    cy = cells_y or cells
    width = width or size
    grid = rng.random((cy, cells), dtype=np.float32)
    x = np.arange(width, dtype=np.float32) * cells / width
    y = np.arange(size, dtype=np.float32) * cy / size
    ix, iy = x.astype(int), y.astype(int)
    fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a = grid[iy[:, None] % cy, ix[None, :] % cells]
    b = grid[iy[:, None] % cy, (ix[None, :] + 1) % cells]
    c = grid[(iy[:, None] + 1) % cy, ix[None, :] % cells]
    d = grid[(iy[:, None] + 1) % cy, (ix[None, :] + 1) % cells]
    return (a * (1 - fx) + b * fx) * (1 - fy[:, None]) + (c * (1 - fx) + d * fx) * fy[:, None]


def fbm(size, seed, base=4, width=None, stretch=1.0):
    out = np.zeros((size, width or size), dtype=np.float32)
    for i, weight in enumerate((0.48, 0.25, 0.14, 0.08, 0.05)):
        cells = base * 2 ** i
        out += noise(size, round(cells * stretch), seed + i, cells, width) * weight
    return out


def smoothstep(lo, hi, values):
    a = np.clip((values - lo) / (hi - lo), 0, 1)
    return a * a * (3 - 2 * a)


def blend(a, b, mask):
    return a * (1 - mask[..., None]) + b * mask[..., None]


def save_image(path, data, color=False):
    h, w = data.shape[:2]
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[..., :data.shape[2]] = np.clip(data, 0, 1)
    image = bpy.data.images.new(path.stem, width=w, height=h, alpha=True)
    image.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(path)
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def maps(folder, name, rgb, rough, metal, height, span, ao=None):
    ny, nx = rgb.shape[:2]
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * nx / (2 * span[0])
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * ny / (2 * span[1])
    normal = np.stack((-dx, -dy, np.ones_like(dx)), axis=2)
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    normal = normal * 0.5 + 0.5
    rough = np.clip(rough, 0.2, 0.99)
    metal = np.broadcast_to(metal, rough.shape)
    ao = np.ones_like(rough) if ao is None else ao
    save_image(folder / f'{name}_BaseColor.png', rgb, color=True)
    save_image(folder / f'{name}_Normal.png', normal)
    save_image(folder / f'{name}_ORM.png', np.stack((ao, rough, metal), axis=2))
    # HDRP mask R metallic, G occlusion, B neutral detail mask, A smoothness.
    save_image(folder / f'{name}_HDRPMask.png', np.stack((metal, ao, np.ones_like(rough), 1 - rough), axis=2))
    print(f'TEXTURES {name} {nx}x{ny}', flush=True)


def surface_noise(u,v,scale,seed):
    x,y=np.asarray(u)*scale,np.asarray(v)*scale
    ix,iy=np.floor(x),np.floor(y)
    fx,fy=x-ix,y-iy
    fx,fy=fx*fx*(3-2*fx),fy*fy*(3-2*fy)
    def hashed(a,b):
        value=np.sin(a*127.1+b*311.7+seed*19.19)*43758.5453
        return value-np.floor(value)
    a,b=hashed(ix,iy),hashed(ix+1,iy)
    c,d=hashed(ix,iy+1),hashed(ix+1,iy+1)
    return (a*(1-fx)+b*fx)*(1-fy)+(c*(1-fx)+d*fx)*fy


def damage_field(u,v,seed,small_chips=True):
    # Shared by color maps and lifted paint geometry in normalized leaf space.
    us=np.asarray(u)*STRETCH
    wave=(surface_noise(us,v,23,seed)-.5)*.16
    wave+=(surface_noise(us,v,71,seed+1)-.5)*.085
    wave+=(surface_noise(us,v,191,seed+2)-.5)*.035
    wave+=(surface_noise(us,v,487,seed+3)-.5)*.012
    bottom=.087+(surface_noise(us,0,11,seed+4)-.5)*.085
    field=(bottom-v)*2.0+wave
    patches=[(.19,.92,.125,.13),(.29,.78,.065,.09),(.89,.56,.085,.06),(.08,.10,.09,.12)] if seed==104 else [(.19,.57,.065,.06),(.79,.49,.045,.05),(.94,.11,.075,.13)]
    for x,z,rx,rz in patches:
        field=np.maximum(field,(1-((u-x)*STRETCH/rx)**2-((v-z)/rz)**2)*.13+wave)
    edges=.009-np.minimum(u,1-u)*STRETCH+wave*.16
    top=(v-.985)*1.6+wave*.4
    field=np.maximum(np.maximum(field,edges),top)
    if small_chips:
        flecks=(surface_noise(us,v,140,seed+5)-.94)*.30+(surface_noise(us,v,13,seed+6)-.6)*.08
        field=np.maximum(field,flecks)
    return field


def corrosion_holes(seed):
    rng=np.random.default_rng(seed+802)
    return [(float(u),float(rng.uniform(.014,.036)),float(rng.uniform(.006,.020))/STRETCH,float(rng.uniform(.003,.012)),int(rng.integers(10000)))
            for u in (.08,.105,.22,.46,.52,.77,.85,.89)]


def steel_panel(seed):
    nx, n = SHEET_PIXELS
    rng = np.random.default_rng(seed)
    u = np.arange(nx, dtype=np.float32)[None, :] / nx
    v = np.arange(n, dtype=np.float32)[:, None] / n
    broad = fbm(n, seed+43, 7, nx, STRETCH)
    medium = fbm(n, seed+78, 35, nx, STRETCH)
    fine = noise(n, round(650*STRETCH), seed+210, 650, nx)
    grain = rng.random((n, nx), dtype=np.float32)
    field = damage_field(u,v,seed)+(fine-.5)*.009
    rust = smoothstep(-.003,.003,field)
    primer = np.clip(smoothstep(-.012,-.004,field)-rust,0,1)
    halo = np.clip(smoothstep(-.032,-.012,field)-rust-primer,0,1)*.23
    for cx,cz,rx,rz,_ in corrosion_holes(seed):
        rust=np.maximum(rust,1-smoothstep(1.05,1.8,((u-cx)/rx)**2+((v-cz)/rz)**2))
    # Narrow trails run down from the top seam and selected old paint chips.
    drips = np.zeros((n, nx), dtype=np.float32)
    for _ in range(round(110*STRETCH)):
        cx, top = rng.uniform(0, 1), rng.uniform(0.28, 1)
        if rng.random() < 0.58:
            top = 1.0 if rng.random()<.65 else .414
        length, width = rng.uniform(0.025, 0.27), rng.uniform(0.0004, 0.0035) / STRETCH
        dx = (u - cx - 0.0008 / STRETCH * np.sin(v * 30 + cx * 20)) / width
        trail = np.exp(-dx * dx) * np.clip((v - (top - length)) / length, 0, 1)
        trail *= (v < top)
        drips = np.maximum(drips, trail * rng.uniform(0.25, 0.70))
    paint = np.zeros((n, nx, 3), dtype=np.float32) + (0.255, 0.30, 0.325)
    paint *= (0.91 + broad[..., None] * 0.16 + (fine[..., None] - 0.5) * 0.045)
    rust_rgb = np.zeros_like(paint) + (0.20, 0.09, 0.042)
    rust_rgb *= (0.38 + medium[..., None] * 1.3 + (fine[...,None]-.5)*.45)
    rust_rgb = blend(rust_rgb,np.array((.32,.175,.078)),smoothstep(.56,.73,medium)*.65)
    rgb = blend(paint, rust_rgb, rust)
    rgb = blend(rgb,np.array((.34,.30,.25)),primer*.55)
    rgb = blend(rgb,np.array((.25,.17,.11)),halo)
    rgb = blend(rgb, np.array((0.25, 0.15, 0.09), dtype=np.float32), drips)
    dust = (1 - smoothstep(0, 0.19, v)) * (0.04 + medium * 0.1)
    rgb = blend(rgb, np.array((0.24, 0.22, 0.19), dtype=np.float32), dust)
    # Small angular scrapes: metal substrate, distinct from dielectric paint/rust.
    scratch = np.zeros((n, nx), dtype=np.float32)
    for _ in range(round(60*STRETCH)):
        cx, cz = rng.uniform(0.04, 0.96, 2)
        half_len = rng.uniform(0.002, 0.012) / STRETCH
        slope = rng.uniform(-1, 1) * STRETCH
        stripe = np.abs(v - cz - (u - cx) * slope) < rng.uniform(0.00014, 0.00045)
        stripe &= (np.abs(u - cx) < half_len)
        scratch[stripe] = rng.uniform(0.12, 0.42)
    scratch *= (1 - rust)
    rgb = blend(rgb, np.array((0.25, 0.27, 0.275), dtype=np.float32), scratch * 0.8)
    rough = (0.58 + medium * 0.12) * (1 - rust) + (0.84 + fine * 0.12) * rust
    rough -= scratch * 0.22
    metal = scratch * 0.88
    height = (1 - rust) * 0.0008 + (fine - 0.5) * (0.00010 + rust * 0.00065)
    height += (grain - 0.5) * (0.000012 + rust * 0.000055)
    return rgb,rough,metal,height


def steel_panels(folder):
    left,right=steel_panel(104),steel_panel(682)
    combined=[np.concatenate((a,b),axis=1) for a,b in zip(left,right)]
    maps(folder,'GatePaint',*combined,(2*SHEET_WIDTH,2.815))


def mineral(folder, name, color, seed, span, n=1024):
    broad = fbm(n, seed)
    fine = noise(n, 700 if n==2048 else 240, seed + 11)
    rng = np.random.default_rng(seed)
    grain = rng.random((n, n), dtype=np.float32)
    rgb = np.zeros((n, n, 3), dtype=np.float32) + color
    variation = 0.6 + broad * 0.65 + (fine - 0.5) * 0.26 + (grain - 0.5) * 0.12
    rgb *= variation[..., None]
    height = (fine - 0.5) * 0.0018 + (grain - 0.5) * 0.00022
    rough = 0.83 + fine * 0.15
    if name == 'Brick':
        # Muted old clay with pale ochre and dusty grey mineral variation.
        rgb = blend(rgb, np.array((.51, .445, .35)), smoothstep(.40, .66, broad) * .30)
        speck = grain > 0.987
        rgb[speck] = rgb[speck] * 0.7 + np.array((.61, .54, .43)) * 0.3
        height += (noise(n, 280, seed + 19) - 0.5) * 0.0015
    if name in ('Brick','Concrete'):
        z=np.arange(n,dtype=np.float32)[:,None]/n*4.5
        streak=noise(n,170,seed+60,cells_y=5)
        dirt=np.exp(-z/0.65)*(.22+streak*.46)
        for top in (3.45,4.12):
            dirt+=np.exp(-np.maximum(top-z,0)/.27)*(z<top)*(.11+streak*.25)
        rgb*=1-np.clip(dirt,0,.72)[...,None]
    if name == 'Concrete':
        pores = grain < 0.018
        rgb[pores] *= 0.55
        height[pores] -= 0.0004
        stains = smoothstep(0.42, 0.74, fbm(n, seed + 31, 18)) * 0.25
        rgb *= 1 - stains[..., None]
    if name=='Whitewash':
        rgb*=1-smoothstep(.42,.67,broad)[...,None]*.35
        height+=(noise(n,65,seed+18)-.5)*.002
    maps(folder, name, rgb, rough, 0.0, height, span)
    if name=='Brick':
        save_image(folder/'BrickWeathered_BaseColor.png',rgb*np.array((.94,.95,.96)),color=True)


def frame_steel(folder, name, seed, painted):
    n = 1024
    broad, small = fbm(n, seed, 5), fbm(n, seed + 12, 32)
    grain = np.random.default_rng(seed).random((n, n), dtype=np.float32)
    rust = smoothstep(0.63 if painted else 0.50, 0.65 if painted else 0.64, broad * 0.65 + small * 0.35)
    base = np.array((0.105, 0.112, 0.11) if painted else (0.16, 0.165, 0.16), dtype=np.float32)
    rgb = base[None, None, :] * (0.88 + small[..., None] * 0.23)
    rgb = blend(rgb, np.array((0.19, 0.10, 0.058), dtype=np.float32) * (0.7 + small[..., None] * 0.6), rust)
    rough = (0.56 if painted else 0.46) + small * 0.15 + rust * 0.23
    metal = (0.0 if painted else 0.93) * (1 - rust)
    height = (grain - 0.5) * 0.00002 + (small - 0.5) * rust * 0.00025
    maps(folder, name, rgb, rough, metal, height, (1.1, 1.1))


def build(folder):
    folder = Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    steel_panels(folder)
    gc.collect()
    frame_steel(folder, 'FramePaint', 13, True)
    frame_steel(folder, 'Iron', 111, False)
    mineral(folder, 'Brick', (.565, .425, .305), 921, (9, 4.5),2048)
    mineral(folder, 'Concrete', (0.40, 0.39, 0.35), 213, (4.5, 4.5),2048)
    mineral(folder, 'Mortar', (0.29, 0.275, 0.235), 432, (1.3, 1.3), 512)
    mineral(folder, 'Whitewash', (0.66, 0.63, 0.54), 671, (1.1, 1.1), 512)


def tint_individual_bricks(collection):
    """Assign one baked vertex tint to each disconnected brick surface."""
    import hashlib
    palette = ((1.0, 1.0, 1.0, 1), (.95, .98, 1.0, 1),
               (.95, .92, .88, 1), (1.0, .84, .75, 1))
    counts = [0, 0, 0, 0]
    for obj in collection.objects:
        if obj.type != 'MESH' or obj.data.materials[0].name not in ('Brick', 'BrickWeathered'):
            continue
        mesh = obj.data
        parent = list(range(len(mesh.vertices)))
        def root(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        for edge in mesh.edges:
            a, b = (root(i) for i in edge.vertices)
            parent[b] = a
        groups = {}
        for vertex in mesh.vertices:
            groups.setdefault(root(vertex.index), []).append(vertex.index)
        layer = mesh.color_attributes.get('BrickTint')
        if layer:
            mesh.color_attributes.remove(layer)
        layer = mesh.color_attributes.new(name='BrickTint', type='FLOAT_COLOR', domain='POINT')
        mesh.color_attributes.active_color_index = mesh.color_attributes.find('BrickTint')
        mesh.color_attributes.render_color_index = mesh.color_attributes.find('BrickTint')
        for vertices in groups.values():
            key = obj.name + ':' + str(min(vertices))
            number = int.from_bytes(hashlib.sha256(key.encode()).digest()[:4], 'little') / 2**32
            index = 0 if number < .38 else 1 if number < .72 else 2 if number < .86 else 3
            counts[index] += 1
            # A small continuous shift softens the boundaries between the warm families.
            jitter = .96 + (int.from_bytes(hashlib.sha256((key+'tone').encode()).digest()[:4], 'little') / 2**32) * .08
            tint = tuple(c*jitter for c in palette[index][:3]) + (1,)
            for i in vertices:
                layer.data[i].color = tint
    for name in ('Brick', 'BrickWeathered'):
        mat = bpy.data.materials[name]
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        base = next(n for n in nodes if n.type == 'TEX_IMAGE' and n.label == 'BaseColor')
        vertex = nodes.get('Individual Brick Tint') or nodes.new('ShaderNodeVertexColor')
        vertex.name, vertex.layer_name = 'Individual Brick Tint', 'BrickTint'
        old = nodes.get('Brick Color Multiply')
        if old:
            nodes.remove(old)
        mix = nodes.new('ShaderNodeMix')
        mix.name, mix.data_type, mix.blend_type = 'Brick Color Multiply', 'RGBA', 'MULTIPLY'
        mix.inputs[0].default_value = 1
        links.new(base.outputs['Color'], mix.inputs[6])
        links.new(vertex.outputs['Color'], mix.inputs[7])
        links.new(mix.outputs[2], nodes.get('Principled BSDF').inputs['Base Color'])
    return {'pale_neutral': counts[0], 'grey_brown': counts[1],
            'dusty_brown': counts[2], 'orange_red': counts[3],
            'orange_red_fraction': counts[3] / sum(counts)}
