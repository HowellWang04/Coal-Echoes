"""Shared procedural coal material and one UV atlas for all three piles."""
import bpy
import bmesh

NAMES = ['CoalPile_A', 'CoalPile_B', 'CoalPile_C']


def sock(sockets, name):
    # Mix nodes carry one hidden socket per data type under the same name.
    return next(s for s in sockets if s.name == name and s.enabled)


def node(nodes, kind, location, **values):
    n = nodes.new(kind)
    n.location = location
    for key, value in values.items():
        if key in n.inputs:
            sock(n.inputs, key).default_value = value
        else:
            setattr(n, key, value)
    return n


def mix(nodes, location, data_type='FLOAT', blend_type='MIX'):
    n = nodes.new('ShaderNodeMix')
    n.location = location
    n.data_type = data_type
    n.blend_type = blend_type
    return n


def ramp(nodes, location, a, b):
    n = nodes.new('ShaderNodeValToRGB')
    n.location = location
    n.color_ramp.elements[0].color = a
    n.color_ramp.elements[1].color = b
    return n


def math(nodes, links, location, operation, a, b=None, c=None, clamp=False):
    n = node(nodes, 'ShaderNodeMath', location, operation=operation, use_clamp=clamp)
    for i, value in enumerate((a, b, c)):
        if value is None:
            continue
        if isinstance(value, (int, float)):
            n.inputs[i].default_value = value
        else:
            links.new(value, n.inputs[i])
    return n.outputs[0]


def build_material():
    mat = bpy.data.materials.get('CoalPile') or bpy.data.materials.new('CoalPile')
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = node(nodes, 'ShaderNodeOutputMaterial', (1400, 0))
    bsdf = node(nodes, 'ShaderNodeBsdfPrincipled', (1100, 0))
    links.new(bsdf.outputs[0], out.inputs['Surface'])

    geo = node(nodes, 'ShaderNodeNewGeometry', (-1600, 0))
    attr = nodes.new('ShaderNodeAttribute')
    attr.attribute_name = 'CoalMask'
    attr.location = (-1600, -500)
    mask = node(nodes, 'ShaderNodeSeparateColor', (-1400, -500))
    links.new(attr.outputs['Color'], mask.inputs[0])
    normal = node(nodes, 'ShaderNodeSeparateXYZ', (-1400, -750))
    links.new(geo.outputs['Normal'], normal.inputs[0])

    # Crushed coal: three sizes of chunks stacked, the highest one at each point forms the surface.
    # Chebychev cells give the blocky break of coal; the smallest size is rounder grit.
    def chunks(scale, rotation, metric, top, y):
        mapping = node(nodes, 'ShaderNodeMapping', (-1300, y))
        mapping.inputs['Rotation'].default_value = rotation
        links.new(geo.outputs['Position'], mapping.inputs['Vector'])
        vor = node(nodes, 'ShaderNodeTexVoronoi', (-1100, y), Scale=scale, Randomness=1.0, distance=metric)
        links.new(mapping.outputs['Vector'], vor.inputs['Vector'])
        h = node(nodes, 'ShaderNodeMapRange', (-900, y))
        h.inputs['From Max'].default_value = 0.55
        h.inputs['To Min'].default_value = top
        h.inputs['To Max'].default_value = 0.0
        links.new(vor.outputs['Distance'], h.inputs['Value'])
        return vor, h.outputs['Result']

    big, h1 = chunks(7.0, (0.35, 0.6, 0.17), 'CHEBYCHEV', 1.0, 500)
    mid, h2 = chunks(15.0, (0.9, 0.17, 1.2), 'CHEBYCHEV', 0.75, 200)
    _, h3 = chunks(34.0, (0.0, 0.0, 0.0), 'EUCLIDEAN', 0.45, -100)
    height = math(nodes, links, (-500, 200), 'MAXIMUM', math(nodes, links, (-700, 300), 'MAXIMUM', h1, h2), h3)
    bump = node(nodes, 'ShaderNodeBump', (850, -350), Strength=0.6, Distance=0.03)
    links.new(height, bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    cavity = math(nodes, links, (-300, 0), 'SUBTRACT', 1.0, height)

    # Per-chunk variation from whichever of the two big sizes is on top.
    mid_on_top = math(nodes, links, (-700, 650), 'GREATER_THAN', h2, h1)
    chunk = mix(nodes, (-500, 650), 'RGBA')
    links.new(mid_on_top, sock(chunk.inputs, 'Factor'))
    links.new(big.outputs['Color'], sock(chunk.inputs, 'A'))
    links.new(mid.outputs['Color'], sock(chunk.inputs, 'B'))
    chunk_value = sock(chunk.outputs, 'Result')

    # Dust: toe weight from the mesh, up-facing slopes and cavities, broken into patches; fresh cut faces stay clean.
    patches = node(nodes, 'ShaderNodeTexNoise', (-1100, -900), Scale=0.9, Detail=4.0)
    links.new(geo.outputs['Position'], patches.inputs['Vector'])
    patch = node(nodes, 'ShaderNodeMapRange', (-900, -900))
    patch.inputs['To Min'].default_value = 0.4
    patch.inputs['To Max'].default_value = 1.2
    links.new(patches.outputs['Fac'], patch.inputs['Value'])
    up = math(nodes, links, (-1100, -700), 'POWER', normal.outputs['Z'], 4.0)
    dust = math(nodes, links, (-900, -600), 'MULTIPLY_ADD', up, 0.1, mask.outputs['Red'])
    dust = math(nodes, links, (-700, -600), 'MULTIPLY_ADD', cavity, 0.2, dust)
    dust = math(nodes, links, (-500, -700), 'MULTIPLY', dust, patch.outputs['Result'])
    clean = math(nodes, links, (-500, -900), 'SUBTRACT', 1.0, mask.outputs['Green'])
    dust = math(nodes, links, (-300, -750), 'MULTIPLY', dust, clean, clamp=True)

    # Colour: near-black chunks that vary per chunk, darker in the cavities, grey-brown dust.
    coal = ramp(nodes, (-300, 650), (0.010, 0.010, 0.011, 1), (0.034, 0.033, 0.032, 1))
    links.new(chunk_value, coal.inputs['Fac'])
    shade = node(nodes, 'ShaderNodeMapRange', (-300, 400))
    shade.inputs['To Min'].default_value = 0.5
    links.new(height, shade.inputs['Value'])
    coal_color = mix(nodes, (0, 500), 'RGBA', 'MULTIPLY')
    sock(coal_color.inputs, 'Factor').default_value = 1.0
    links.new(coal.outputs['Color'], sock(coal_color.inputs, 'A'))
    links.new(shade.outputs['Result'], sock(coal_color.inputs, 'B'))
    base = mix(nodes, (500, 250), 'RGBA')
    sock(base.inputs, 'B').default_value = (0.085, 0.078, 0.068, 1)
    links.new(dust, sock(base.inputs, 'Factor'))
    links.new(sock(coal_color.outputs, 'Result'), sock(base.inputs, 'A'))
    links.new(sock(base.outputs, 'Result'), bsdf.inputs['Base Color'])

    # Roughness: glinting facets, rougher cavities and dust; fresh faces a touch glossier.
    facet = node(nodes, 'ShaderNodeMapRange', (-300, 900))
    facet.inputs['To Min'].default_value = 0.36
    facet.inputs['To Max'].default_value = 0.66
    links.new(chunk_value, facet.inputs['Value'])
    rough = math(nodes, links, (0, 850), 'MULTIPLY_ADD', cavity, 0.4, facet.outputs['Result'])
    rough = math(nodes, links, (200, 850), 'MULTIPLY_ADD', mask.outputs['Green'], -0.1, rough)
    dusty = mix(nodes, (500, 600))
    sock(dusty.inputs, 'B').default_value = 0.93
    links.new(dust, sock(dusty.inputs, 'Factor'))
    links.new(rough, sock(dusty.inputs, 'A'))
    links.new(sock(dusty.outputs, 'Result'), bsdf.inputs['Roughness'])
    return mat


def unwrap(objects, mat):
    for ob in objects:
        ob.data.materials.clear()
        ob.data.materials.append(mat)
        if not ob.data.uv_layers:
            ob.data.uv_layers.new(name='UVMap')
    bpy.context.view_layer.objects.active = objects[0]
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:
        ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for ob in objects:
        bm = bmesh.from_edit_mesh(ob.data)
        uv = bm.loops.layers.uv.active
        for f in bm.faces:
            # The smooth heightfield body projects from above; the flat lumps get smart-projected.
            f.select = not f.smooth
            if f.smooth:
                for loop in f.loops:
                    loop[uv].uv = (loop.vert.co.x, loop.vert.co.y)
        bmesh.update_edit_mesh(ob.data)
    bpy.ops.uv.smart_project(angle_limit=1.55, island_margin=0.0)
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.average_islands_scale()
    # Lumps are half buried and small on screen; give the visible heap body most of the atlas.
    for ob in objects:
        bm = bmesh.from_edit_mesh(ob.data)
        uv = bm.loops.layers.uv.active
        for f in bm.faces:
            if not f.smooth:
                for loop in f.loops:
                    loop[uv].uv *= 0.25
        bmesh.update_edit_mesh(ob.data)
    bpy.ops.uv.pack_islands(rotate=True, margin=0.0005)
    bpy.ops.object.mode_set(mode='OBJECT')


objects = [bpy.data.objects[n] for n in NAMES]
unwrap(objects, build_material())
body = sum(p.area for ob in objects for p in ob.data.polygons if p.use_smooth)
lumps = sum(p.area for ob in objects for p in ob.data.polygons if not p.use_smooth)
print('body m2', round(body, 1), 'lumps m2', round(lumps, 1),
      'body texels per metre at 4096 about', round(4096 / (body + lumps * 0.0625) ** 0.5))
