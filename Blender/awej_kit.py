"""
AWEJ shared Blender kit: materials, primitives and building pieces used by the area scripts.
Extracted from build_dusty_crossroads.py. Load with exec() into the caller's globals.
Materials are reused by name if they already exist in the file.
"""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix, Euler

def reset_scene(name):
    old = bpy.data.scenes.get(name)
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        def drop(c):
            for ch in list(c.children):
                drop(ch)
                bpy.data.collections.remove(ch)
        drop(old.collection)
        if len(bpy.data.scenes) == 1:
            bpy.data.scenes.new("Scene_tmp")
        bpy.data.scenes.remove(old)
        # keep materials alive: the kit caches them in M across rebuilds
        keep = [m for m in bpy.data.materials if not m.use_fake_user]
        for m in keep:
            m.use_fake_user = True
        bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
        for m in keep:
            m.use_fake_user = False
    sc = bpy.data.scenes.new(name)
    if bpy.context.window:  # None when Blender runs headless (-b)
        bpy.context.window.scene = sc
    return sc

def coll(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or SC.collection).children.link(c)
    return c

def begin_scene(name):
    global SC, C_TERRAIN, C_WALLS, C_PATHS, C_BUILD, C_PROPS, C_NATURE, C_CHARS, C_SKY
    SC = reset_scene(name)
    C_TERRAIN = coll("Terrain"); C_WALLS = coll("Walls_Gates"); C_PATHS = coll("Paths_Hazards")
    C_BUILD = coll("Buildings"); C_PROPS = coll("Props"); C_NATURE = coll("Nature")
    C_CHARS = coll("Characters"); C_SKY = coll("Sky_Lighting")
    return SC

# --------------------------------------------------------------------------- materials
def sock(sockets, ident):
    for s in sockets:
        if s.identifier == ident:
            return s
    return sockets[ident]

def new_mat(name):
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    b = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    return m, nt, b

def setin(node, name, val):
    try:
        node.inputs[name].default_value = val
    except Exception:
        pass

def rgba(c):
    return (c[0], c[1], c[2], 1.0)

def ramp(nt, colors):
    r = nt.nodes.new('ShaderNodeValToRGB')
    els = r.color_ramp.elements
    while len(els) < len(colors):
        els.new(0.5)
    for i, (pos, col) in enumerate(colors):
        els[i].position = pos
        els[i].color = rgba(col)
    return r

def mix_mult(nt, a, b, fac=1.0):
    m = nt.nodes.new('ShaderNodeMix')
    m.data_type = 'RGBA'
    m.blend_type = 'MULTIPLY'
    sock(m.inputs, 'Factor_Float').default_value = fac
    nt.links.new(a, sock(m.inputs, 'A_Color'))
    nt.links.new(b, sock(m.inputs, 'B_Color'))
    return sock(m.outputs, 'Result_Color')

def _plain(name, color, rough=0.6, metal=0.0, emit=None, emit_strength=0.0, coat=0.0):
    m, nt, b = new_mat(name)
    setin(b, 'Base Color', rgba(color))
    setin(b, 'Roughness', rough)
    setin(b, 'Metallic', metal)
    setin(b, 'Coat Weight', coat)
    if emit:
        setin(b, 'Emission Color', rgba(emit))
        setin(b, 'Emission Strength', emit_strength)
    m.diffuse_color = rgba(color)
    return m

def _textured(name, kind, c1, c2, rough=0.8, scale=1.0, bump=0.3, coords='UV',
             c3=None, brick_w=0.5, row_h=0.25, mortar=None, mortar_size=0.02,
             grime=0.25, metal=0.0, noise_scale=3.0):
    m, nt, b = new_mat(name)
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord')
    mp = N.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = (scale, scale, scale)
    L.new(tc.outputs['UV' if coords == 'UV' else 'Object'], mp.inputs['Vector'])
    vec = mp.outputs['Vector']
    noise = N.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = noise_scale
    setin(noise, 'Detail', 6.0)
    L.new(vec, noise.inputs['Vector'])
    gr = ramp(nt, [(0.3, (1 - grime,) * 3), (0.7, (1.0, 1.0, 1.0))])
    L.new(noise.outputs['Fac'], gr.inputs['Fac'])
    grime_col = gr.outputs['Color']
    height = None

    if kind == 'noise':
        r = ramp(nt, [(0.25, c1), (0.75, c2)] + ([(1.0, c3)] if c3 else []))
        L.new(noise.outputs['Fac'], r.inputs['Fac'])
        L.new(r.outputs['Color'], b.inputs['Base Color'])
        height = noise.outputs['Fac']
    elif kind == 'brick':
        br = N.new('ShaderNodeTexBrick')
        br.offset = 0.5
        br.inputs['Color1'].default_value = rgba(c1)
        br.inputs['Color2'].default_value = rgba(c2)
        br.inputs['Mortar'].default_value = rgba(mortar or tuple(x * 0.55 for x in c1))
        br.inputs['Scale'].default_value = 1.0
        br.inputs['Mortar Size'].default_value = mortar_size
        br.inputs['Mortar Smooth'].default_value = 0.3
        br.inputs['Bias'].default_value = 0.0
        br.inputs['Brick Width'].default_value = brick_w
        br.inputs['Row Height'].default_value = row_h
        L.new(vec, br.inputs['Vector'])
        L.new(mix_mult(nt, br.outputs['Color'], grime_col), b.inputs['Base Color'])
        inv = N.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'
        inv.inputs[0].default_value = 1.0
        L.new(br.outputs['Fac'], inv.inputs[1])
        add = N.new('ShaderNodeMath'); add.operation = 'MULTIPLY_ADD'
        L.new(noise.outputs['Fac'], add.inputs[0]); add.inputs[1].default_value = 0.25
        L.new(inv.outputs[0], add.inputs[2])
        height = add.outputs[0]
    elif kind == 'gravel':
        vo = N.new('ShaderNodeTexVoronoi')
        vo.inputs['Scale'].default_value = 7.0
        L.new(vec, vo.inputs['Vector'])
        r = ramp(nt, [(0.0, c2), (0.6, c1)])
        L.new(vo.outputs['Distance'], r.inputs['Fac'])
        L.new(mix_mult(nt, r.outputs['Color'], grime_col), b.inputs['Base Color'])
        height = vo.outputs['Distance']
    elif kind == 'cobble':
        vo = N.new('ShaderNodeTexVoronoi')
        vo.feature = 'DISTANCE_TO_EDGE'
        vo.inputs['Scale'].default_value = 1.6
        L.new(vec, vo.inputs['Vector'])
        vc = N.new('ShaderNodeTexVoronoi')
        vc.inputs['Scale'].default_value = 1.6
        L.new(vec, vc.inputs['Vector'])
        r = ramp(nt, [(0.2, c1), (0.8, c2)])
        L.new(vc.outputs['Color'], r.inputs['Fac'])
        edge = ramp(nt, [(0.0, mortar or (0.25, 0.22, 0.2)), (0.08, (1, 1, 1))])
        L.new(vo.outputs['Distance'], edge.inputs['Fac'])
        L.new(mix_mult(nt, mix_mult(nt, r.outputs['Color'], edge.outputs['Color']), grime_col), b.inputs['Base Color'])
        height = vo.outputs['Distance']
    elif kind == 'thatch':
        wv = N.new('ShaderNodeTexWave')
        wv.bands_direction = 'X'
        wv.inputs['Scale'].default_value = 6.0
        setin(wv, 'Distortion', 7.0)
        setin(wv, 'Detail', 4.0)
        L.new(vec, wv.inputs['Vector'])
        r = ramp(nt, [(0.2, c1), (0.8, c2)])
        L.new(wv.outputs['Fac'], r.inputs['Fac'])
        L.new(mix_mult(nt, r.outputs['Color'], grime_col), b.inputs['Base Color'])
        height = wv.outputs['Fac']
    elif kind == 'snowrock':
        sep = N.new('ShaderNodeSeparateXYZ')
        L.new(tc.outputs['Object'], sep.inputs[0])
        add = N.new('ShaderNodeMath'); add.operation = 'MULTIPLY_ADD'
        L.new(noise.outputs['Fac'], add.inputs[0]); add.inputs[1].default_value = 0.35
        L.new(sep.outputs['Z'], add.inputs[2])
        mr = N.new('ShaderNodeMapRange')
        mr.inputs['From Min'].default_value = 0.9
        mr.inputs['From Max'].default_value = 0.95
        L.new(add.outputs[0], mr.inputs['Value'])
        r = ramp(nt, [(0.0, c1), (1.0, c2)])
        L.new(mr.outputs['Result'], r.inputs['Fac'])
        L.new(mix_mult(nt, r.outputs['Color'], grime_col), b.inputs['Base Color'])
        height = noise.outputs['Fac']

    setin(b, 'Roughness', rough)
    setin(b, 'Metallic', metal)
    if height is not None and bump > 0:
        bp = N.new('ShaderNodeBump')
        bp.inputs['Strength'].default_value = bump
        setin(bp, 'Distance', 0.05)
        L.new(height, bp.inputs['Height'])
        L.new(bp.outputs['Normal'], b.inputs['Normal'])
    m.diffuse_color = rgba(c1)
    return m

def plain(name, *a, **k):
    return bpy.data.materials.get(name) or _plain(name, *a, **k)

def textured(name, *a, **k):
    return bpy.data.materials.get(name) or _textured(name, *a, **k)

M = {}
M['grass'] = textured('Grass', 'noise', (0.16, 0.34, 0.07), (0.27, 0.47, 0.1), c3=(0.42, 0.5, 0.14),
                      coords='OBJECT', scale=0.08, noise_scale=2.5, bump=0.15, rough=0.9)
M['grass_far'] = textured('GrassHills', 'noise', (0.14, 0.3, 0.08), (0.24, 0.42, 0.11), coords='OBJECT',
                          scale=0.02, noise_scale=3, bump=0.1, rough=0.95)
M['mountain'] = textured('MountainRockSnow', 'snowrock', (0.2, 0.22, 0.28), (0.95, 0.97, 1.0),
                         coords='OBJECT', scale=0.05, bump=0.4, rough=0.85)
M['wall'] = textured('SandstoneWall', 'brick', (0.78, 0.64, 0.46), (0.7, 0.56, 0.4),
                     brick_w=0.9, row_h=0.42, mortar=(0.52, 0.44, 0.34), mortar_size=0.035, bump=0.5)
M['wall_cap'] = textured('WallCapStone', 'noise', (0.66, 0.6, 0.52), (0.78, 0.72, 0.62), bump=0.3)
M['stone_base'] = textured('FieldStone', 'cobble', (0.46, 0.44, 0.42), (0.6, 0.57, 0.53), scale=1.4,
                           mortar=(0.3, 0.28, 0.26), bump=0.6)
M['gravel'] = textured('GravelPath', 'gravel', (0.66, 0.6, 0.5), (0.5, 0.44, 0.36), scale=1.0, bump=0.35, rough=0.95)
M['cobble'] = textured('PlazaCobble', 'cobble', (0.58, 0.55, 0.5), (0.72, 0.68, 0.6), scale=0.9, bump=0.5)
M['plaster'] = textured('PlasterCream', 'noise', (0.95, 0.88, 0.7), (0.9, 0.82, 0.64), noise_scale=1.5, bump=0.15)
M['roof_blue'] = textured('ShinglesBlue', 'brick', (0.22, 0.42, 0.68), (0.18, 0.36, 0.6), brick_w=0.55,
                          row_h=0.3, mortar=(0.1, 0.18, 0.3), mortar_size=0.03, bump=0.8, rough=0.7)
M['roof_orange'] = textured('ShinglesOrange', 'brick', (0.85, 0.55, 0.15), (0.75, 0.45, 0.12), brick_w=0.5,
                            row_h=0.28, mortar=(0.4, 0.22, 0.06), bump=0.8, rough=0.7)
M['roof_slate'] = textured('ShinglesSlate', 'brick', (0.36, 0.31, 0.29), (0.3, 0.26, 0.25), brick_w=0.6,
                           row_h=0.3, mortar=(0.16, 0.14, 0.13), bump=0.8)
M['thatch'] = textured('Thatch', 'thatch', (0.42, 0.3, 0.17), (0.62, 0.48, 0.28), bump=0.6, rough=0.95)
M['barn_red'] = textured('BarnBoardsRed', 'brick', (0.7, 0.18, 0.15), (0.62, 0.15, 0.13), brick_w=0.28,
                         row_h=6.0, mortar=(0.35, 0.08, 0.07), mortar_size=0.015, bump=0.4, grime=0.3)
M['tower_stone'] = textured('TowerStone', 'brick', (0.62, 0.6, 0.58), (0.54, 0.52, 0.5), brick_w=0.8,
                            row_h=0.4, mortar=(0.36, 0.35, 0.34), mortar_size=0.03, bump=0.6)
M['hut_stone'] = textured('HutRubble', 'cobble', (0.5, 0.46, 0.42), (0.6, 0.55, 0.48), scale=1.8,
                          mortar=(0.3, 0.27, 0.24), bump=0.7)
M['wood'] = textured('WoodDark', 'brick', (0.35, 0.22, 0.12), (0.3, 0.19, 0.1), brick_w=3.0, row_h=0.18,
                     mortar=(0.18, 0.1, 0.05), mortar_size=0.012, bump=0.3, grime=0.35)
M['wood_light'] = textured('WoodLight', 'brick', (0.62, 0.45, 0.28), (0.55, 0.39, 0.23), brick_w=3.0,
                           row_h=0.2, mortar=(0.3, 0.2, 0.1), mortar_size=0.012, bump=0.3)
M['door'] = textured('DoorPlanks', 'brick', (0.38, 0.24, 0.13), (0.32, 0.2, 0.1), brick_w=0.3, row_h=4.0,
                     mortar=(0.15, 0.08, 0.04), mortar_size=0.02, bump=0.4)
M['trim_white'] = plain('TrimWhite', (0.92, 0.9, 0.86), 0.5)
M['trim_brown'] = plain('TrimBrown', (0.35, 0.22, 0.12), 0.6)
M['iron'] = plain('Iron', (0.12, 0.12, 0.13), 0.45, metal=0.9)
M['brass'] = plain('Brass', (0.85, 0.62, 0.22), 0.3, metal=1.0)
M['gold'] = plain('CoinGold', (1.0, 0.76, 0.2), 0.2, metal=1.0, emit=(1.0, 0.7, 0.2), emit_strength=0.3)
M['glass'] = plain('WindowGlass', (0.12, 0.18, 0.26), 0.08, emit=(1.0, 0.72, 0.4), emit_strength=0.35)
M['lantern'] = plain('LanternGlow', (1.0, 0.65, 0.3), 0.3, emit=(1.0, 0.55, 0.2), emit_strength=1.6)
M['orb_red'] = plain('GateOrbLocked', (0.8, 0.08, 0.05), 0.2, emit=(1.0, 0.06, 0.03), emit_strength=2.5)
M['mat_shop'] = textured('MatShopGold', 'noise', (0.78, 0.6, 0.18), (0.7, 0.52, 0.14), bump=0.4, rough=0.95)
M['mat_battle'] = textured('MatBattleRed', 'noise', (0.62, 0.15, 0.12), (0.52, 0.11, 0.09), bump=0.4, rough=0.95)
M['banner_red'] = plain('BannerRed', (0.62, 0.1, 0.08), 0.8)
M['sign_text'] = plain('SignText', (1.0, 0.88, 0.55), 0.4, emit=(1.0, 0.85, 0.5), emit_strength=0.4)
M['water'] = plain('WellWater', (0.05, 0.12, 0.14), 0.02)
M['hay'] = textured('Hay', 'thatch', (0.75, 0.6, 0.3), (0.88, 0.75, 0.42), bump=0.5, rough=1.0)
M['burlap'] = textured('Burlap', 'noise', (0.62, 0.5, 0.33), (0.7, 0.58, 0.4), bump=0.3, rough=1.0)
M['target_red'] = plain('TargetRed', (0.75, 0.1, 0.08), 0.6)
M['rock'] = textured('Rock', 'noise', (0.45, 0.43, 0.41), (0.6, 0.58, 0.54), bump=0.5, coords='OBJECT', scale=1.0)
M['trunk'] = textured('Bark', 'thatch', (0.28, 0.18, 0.1), (0.4, 0.27, 0.15), bump=0.6)
M['leaf_a'] = textured('LeavesA', 'noise', (0.13, 0.34, 0.07), (0.24, 0.47, 0.1), coords='OBJECT', scale=0.6, bump=0.3)
M['leaf_b'] = textured('LeavesB', 'noise', (0.22, 0.42, 0.07), (0.36, 0.55, 0.1), coords='OBJECT', scale=0.6, bump=0.3)
M['leaf_c'] = textured('LeavesC', 'noise', (0.12, 0.3, 0.12), (0.2, 0.42, 0.18), coords='OBJECT', scale=0.6, bump=0.3)
M['blade'] = plain('GrassBlade', (0.3, 0.52, 0.16), 0.8)
M['flowers'] = [plain('Flower_' + n, c, 0.5) for n, c in
                [('Yellow', (1.0, 0.82, 0.2)), ('Pink', (0.95, 0.45, 0.6)), ('White', (0.95, 0.95, 0.92)),
                 ('Violet', (0.55, 0.4, 0.9))]]
M['cloud'] = plain('Cloud', (1.0, 1.0, 1.0), 1.0, emit=(0.85, 0.9, 1.0), emit_strength=0.25)
# characters
M['skin'] = plain('Skin', (0.95, 0.74, 0.58), 0.55)
M['jacket'] = plain('JacketTeal', (0.1, 0.45, 0.55), 0.6)
M['pants'] = plain('PantsNavy', (0.12, 0.14, 0.25), 0.7)
M['boots'] = plain('Boots', (0.3, 0.18, 0.1), 0.6)
M['hair'] = plain('Hair', (0.2, 0.12, 0.07), 0.6)
M['scarf'] = plain('ScarfOrange', (0.95, 0.5, 0.12), 0.7)
M['eye'] = plain('EyeBlack', (0.02, 0.02, 0.02), 0.1)
M['eye_white'] = plain('EyeWhite', (1, 1, 1), 0.2)
M['blaster_glow'] = plain('BlasterGlow', (0.3, 0.9, 1.0), 0.2, emit=(0.3, 0.9, 1.0), emit_strength=5)
slime = plain('SlimeGreen', (0.35, 0.85, 0.3), 0.12, coat=0.8)
setin(slime.node_tree.nodes['Principled BSDF'], 'Subsurface Weight', 0.6)
setin(slime.node_tree.nodes['Principled BSDF'], 'Subsurface Radius', (0.5, 1.0, 0.3))
M['slime'] = slime
M['charger'] = plain('ChargerRed', (0.75, 0.2, 0.15), 0.35, coat=0.4)
M['horn'] = plain('Horn', (0.92, 0.88, 0.78), 0.4)

# --------------------------------------------------------------------------- geometry helpers
def box_uv(me):
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    for p in me.polygons:
        n = p.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for li in p.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            if ax == 0:
                uv[li].uv = (co.y, co.z)
            elif ax == 1:
                uv[li].uv = (co.x, co.z)
            else:
                uv[li].uv = (co.x, co.y)

def cyl_uv(me):
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    for p in me.polygons:
        if abs(p.normal.z) > 0.85:
            for li in p.loop_indices:
                co = me.vertices[me.loops[li].vertex_index].co
                uv[li].uv = (co.x, co.y)
            continue
        angs = []
        for li in p.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            angs.append(math.atan2(co.y, co.x))
        if max(angs) - min(angs) > math.pi:
            angs = [a + 2 * math.pi if a < 0 else a for a in angs]
        for li, a in zip(p.loop_indices, angs):
            co = me.vertices[me.loops[li].vertex_index].co
            r = max(Vector((co.x, co.y)).length, 0.3)
            uv[li].uv = (a * 4.0, co.z)

def to_mesh(bm, name):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return me

def place(name, data, collection, loc=(0, 0, 0), rot=(0, 0, 0), parent=None, mat=None, scale=None):
    ob = bpy.data.objects.new(name, data)
    collection.objects.link(ob)
    if parent:
        ob.parent = parent
    ob.location = loc
    ob.rotation_euler = rot
    if scale:
        ob.scale = scale
    if mat is not None and data is not None and hasattr(data, 'materials') and len(data.materials) == 0:
        data.materials.append(mat)
    return ob

def empty(name, collection, loc=(0, 0, 0), rot=(0, 0, 0), parent=None):
    ob = place(name, None, collection, loc, rot, parent)
    ob.empty_display_size = 1.0
    return ob

def smooth(me, sides_only=False):
    for p in me.polygons:
        p.use_smooth = (abs(p.normal.z) < 0.98) if sides_only else True

def box(name, size, loc, mat, coll_, parent=None, rot=(0, 0, 0), bevel=0.03, segs=2):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    me = to_mesh(bm, name)
    box_uv(me)
    ob = place(name, me, coll_, loc, rot, parent, mat)
    if bevel > 0:
        md = ob.modifiers.new("Bevel", 'BEVEL')
        md.width = min(bevel, min(size) * 0.45)
        md.segments = segs
        md.limit_method = 'ANGLE'
        try:
            md.harden_normals = True
            smooth(me)
        except Exception:
            pass
    return ob

def cyl(name, r, h, loc, mat, coll_, parent=None, rot=(0, 0, 0), r2=None, segs=24, base=True, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r,
                          radius2=r if r2 is None else r2, depth=h)
    if base:
        for v in bm.verts:
            v.co.z += h / 2
    me = to_mesh(bm, name)
    cyl_uv(me)
    smooth(me, sides_only=True)
    ob = place(name, me, coll_, loc, rot, parent, mat)
    if bevel > 0:
        md = ob.modifiers.new("Bevel", 'BEVEL')
        md.width = bevel
        md.segments = 2
        md.limit_method = 'ANGLE'
        try:
            md.harden_normals = True
        except Exception:
            pass
    return ob

def sphere(name, r, loc, mat, coll_, parent=None, scale=(1, 1, 1), subdiv=3, ico=False, jitter=0.0,
           do_smooth=True, rot=(0, 0, 0), seed=0):
    bm = bmesh.new()
    if ico:
        bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r)
    else:
        bmesh.ops.create_uvsphere(bm, u_segments=8 * subdiv, v_segments=4 * subdiv + 2, radius=r)
    if jitter > 0:
        jr = random.Random(seed)
        for v in bm.verts:
            v.co += v.co.normalized() * jr.uniform(-jitter, jitter) * r
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    me = to_mesh(bm, name)
    box_uv(me)
    if do_smooth:
        smooth(me)
    return place(name, me, coll_, loc, rot, parent, mat)

def prism(name, profile, length, mat, coll_, loc=(0, 0, 0), parent=None, rot=(0, 0, 0), bevel=0.0):
    """Extrude a 2D polygon in (s, z) along local X from -length/2 to length/2; s maps to local Y."""
    bm = bmesh.new()
    a = [bm.verts.new((-length / 2, s, z)) for s, z in profile]
    b = [bm.verts.new((length / 2, s, z)) for s, z in profile]
    n = len(profile)
    bm.faces.new(list(reversed(a)))
    bm.faces.new(b)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = to_mesh(bm, name)
    box_uv(me)
    ob = place(name, me, coll_, loc, rot, parent, mat)
    if bevel > 0:
        md = ob.modifiers.new("Bevel", 'BEVEL')
        md.width = bevel
        md.segments = 1
        md.limit_method = 'ANGLE'
    return ob

def roof_profile(span, rise_pts, thickness, overhang):
    """rise_pts: bottom-surface points (s, z) from left eave to right eave (z=0 at both ends)."""
    p0, p1 = Vector(rise_pts[0]), Vector(rise_pts[1])
    d = (p0 - p1).normalized()
    left = p0 + d * overhang
    q0, q1 = Vector(rise_pts[-1]), Vector(rise_pts[-2])
    d2 = (q0 - q1).normalized()
    right = q0 + d2 * overhang
    bottom = [tuple(left)] + rise_pts + [tuple(right)]
    top = [(s, z + thickness) for s, z in reversed(bottom)]
    return bottom + top

def text(name, body, size, loc, mat, coll_, parent=None, rot=(math.pi / 2, 0, 0), extrude=0.02):
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = body
    cu.size = size
    cu.align_x = 'CENTER'
    cu.align_y = 'CENTER'
    cu.extrude = extrude
    try:
        cu.bevel_depth = 0.005
    except Exception:
        pass
    ob = place(name, cu, coll_, loc, rot, parent)
    cu.materials.append(mat)
    return ob

def facing_rot(facing_deg):
    return (0, 0, math.radians(-facing_deg))

# --------------------------------------------------------------------------- reusable pieces
def window(parent, coll_, name, pos, normal, w=1.4, h=1.6, frame_mat=None, shutters=None, cross=True):
    ang = math.atan2(normal[1], normal[0]) + math.pi / 2
    g = empty(name, coll_, pos, (0, 0, ang), parent)
    g.empty_display_size = 0.3
    fm = frame_mat or M['trim_white']
    box(name + "_Glass", (w, 0.08, h), (0, 0.02, 0), M['glass'], coll_, g, bevel=0)
    t = 0.14
    box(name + "_FrameT", (w + 2 * t, 0.2, t), (0, -0.04, h / 2 + t / 2), fm, coll_, g, bevel=0.02)
    box(name + "_FrameB", (w + 2 * t, 0.2, t), (0, -0.04, -h / 2 - t / 2), fm, coll_, g, bevel=0.02)
    box(name + "_FrameL", (t, 0.2, h), (-w / 2 - t / 2, -0.04, 0), fm, coll_, g, bevel=0.02)
    box(name + "_FrameR", (t, 0.2, h), (w / 2 + t / 2, -0.04, 0), fm, coll_, g, bevel=0.02)
    box(name + "_Sill", (w + 0.5, 0.32, 0.1), (0, -0.14, -h / 2 - t - 0.04), fm, coll_, g, bevel=0.02)
    if cross:
        box(name + "_MullV", (0.07, 0.1, h), (0, -0.02, 0), fm, coll_, g, bevel=0)
        box(name + "_MullH", (w, 0.1, 0.07), (0, -0.02, 0), fm, coll_, g, bevel=0)
    if shutters:
        for sx in (-1, 1):
            box(name + "_Shutter%d" % sx, (w * 0.55, 0.08, h + 0.1), (sx * (w / 2 + t + w * 0.3), -0.06, 0),
                shutters, coll_, g, bevel=0.02)
    return g

def door_assembly(parent, coll_, depth, is_shop, trim_mat, base_z=0.3):
    # Matches Unity: door 2.4 x 3, trim above, welcome mat 2.6 x 2 at 1.2 m out.
    f = -depth
    box("Door", (2.4, 0.25, 3.0), (0, f - 0.1, base_z + 1.5), M['door'], coll_, parent, bevel=0.03)
    box("DoorTrim", (2.9, 0.3, 0.3), (0, f - 0.15, base_z + 3.1), trim_mat, coll_, parent, bevel=0.04)
    for sx in (-1, 1):
        box("DoorJamb%d" % sx, (0.25, 0.3, 3.0), (sx * 1.32, f - 0.15, base_z + 1.5), trim_mat, coll_, parent, bevel=0.03)
        box("DoorBand%d" % sx, (2.2, 0.05, 0.12), (0, f - 0.25, base_z + 0.7 + (sx + 1) * 0.8), M['iron'], coll_, parent, bevel=0)
    sphere("DoorKnob", 0.08, (0.85, f - 0.3, base_z + 1.4), M['brass'], coll_, parent, subdiv=2)
    box("DoorStep", (3.0, 1.0, base_z), (0, f - 0.55, base_z / 2), M['stone_base'], coll_, parent, bevel=0.04)
    box("WelcomeMat", (2.6, 2.0, 0.04), (0, f - 2.1, 0.02), M['mat_shop'] if is_shop else M['mat_battle'],
        coll_, parent, bevel=0.01)

def icon(parent, coll_, loc, is_shop):
    if is_shop:
        c = cyl("IconCoin", 0.42, 0.08, loc, M['gold'], coll_, parent, rot=(math.pi / 2, 0, 0), base=False, segs=32, bevel=0.02)
        box("IconCoinMark", (0.12, 0.06, 0.42), (loc[0], loc[1] - 0.06, loc[2]), M['brass'], coll_, parent, bevel=0.01)
    else:
        for s in (-1, 1):
            g = empty("IconSword%d" % s, coll_, loc, (0, s * math.radians(40), 0), parent)
            box("Blade", (0.1, 0.05, 0.9), (0, -0.05, 0.12), M['brass'] if False else plain_steel, coll_, g, bevel=0.01)
            box("Guard", (0.34, 0.07, 0.07), (0, -0.05, -0.33), M['brass'], coll_, g, bevel=0.01)
            box("Grip", (0.07, 0.06, 0.22), (0, -0.05, -0.47), M['trim_brown'], coll_, g, bevel=0.01)

plain_steel = plain('Steel', (0.8, 0.82, 0.86), 0.25, metal=1.0)

def signboard(parent, coll_, name, loc, is_shop, width=None):
    width = width or max(3.4, len(name) * 0.34 + 1.4)
    g = empty("Sign_" + name, coll_, loc, (0, 0, 0), parent)
    box("Board", (width, 0.14, 0.9), (0, 0, 0), M['wood'], coll_, g, bevel=0.04)
    box("BoardFrame", (width + 0.16, 0.1, 1.06), (0, 0.04, 0), M['trim_brown'], coll_, g, bevel=0.03)
    text("SignText", name, 0.42, (0.25, -0.09, -0.02), M['sign_text'], coll_, g)
    icon(g, coll_, (-width / 2 + 0.55, -0.12, 0), is_shop)
    return g

# --------------------------------------------------------------------------- buildings
def house_roof(name, parent, coll_, span, length, z, rise_pts, mat, thickness=0.3, overhang=0.7, rot_z=0.0, end_overhang=0.6):
    prof = roof_profile(span, rise_pts, thickness, overhang)
    return prism(name, prof, length + 2 * end_overhang, mat, coll_, (0, 0, z), parent, (0, 0, rot_z), bevel=0.03)

def barrel(parent, coll_, loc, name="Barrel"):
    g = empty(name, coll_, loc, (0, 0, 0), parent)
    cyl("Stave", 0.42, 1.1, (0, 0, 0), M['wood_light'], coll_, g, segs=20)
    cyl("StaveMid", 0.47, 0.5, (0, 0, 0.3), M['wood_light'], coll_, g, segs=20)
    for z in (0.12, 0.95):
        cyl("Hoop", 0.45, 0.07, (0, 0, z), M['iron'], coll_, g, segs=20)
    for z in (0.32, 0.75):
        cyl("Hoop", 0.49, 0.06, (0, 0, z), M['iron'], coll_, g, segs=20)
    return g

def gear(parent, coll_, loc, r, mat, rot_z=0.0, flat=False):
    rot = (0, 0, rot_z) if flat else (math.pi / 2, 0, rot_z)
    g = empty("Gear", coll_, loc, rot, parent)
    cyl("GearHub", r * 0.78, 0.22, (0, 0, -0.11), mat, coll_, g, segs=32, bevel=0.02)
    cyl("GearAxle", r * 0.2, 0.32, (0, 0, -0.16), M['iron'], coll_, g, segs=16)
    teeth = max(8, int(r * 10))
    for i in range(teeth):
        a = i * 2 * math.pi / teeth
        box("Tooth", (r * 0.28, r * 0.3, 0.22), (math.cos(a) * r * 0.86, math.sin(a) * r * 0.86, 0), mat, coll_, g,
            rot=(0, 0, a), bevel=0.01, segs=1)
    return g

# --------------------------------------------------------------------------- props
def lamp_head(parent, coll_, loc, wall=False):
    g = empty("Lantern", coll_, loc, (0, 0, 0), parent)
    box("LanternCage", (0.42, 0.42, 0.55), (0, 0, 0), M['iron'], coll_, g, bevel=0.02)
    box("LanternGlass", (0.34, 0.5, 0.45), (0, 0, 0), M['lantern'], coll_, g, bevel=0.0)
    box("LanternGlass2", (0.5, 0.34, 0.45), (0, 0, 0), M['lantern'], coll_, g, bevel=0.0)
    cyl("LanternTop", 0.32, 0.25, (0, 0, 0.27), M['iron'], coll_, g, r2=0.05, segs=4, rot=(0, 0, math.pi / 4))
    li = bpy.data.lights.new("LanternLight", 'POINT')
    li.energy = 80
    li.color = (1.0, 0.72, 0.4)
    li.shadow_soft_size = 0.15
    place("LanternLight", li, coll_, (0, 0, -0.05), parent=g)
    return g

def lamp_post(loc, side):
    g = empty("LampPost", C_PROPS, loc)
    cyl("LampBase", 0.28, 0.5, (0, 0, 0), M['iron'], C_PROPS, g, r2=0.18, segs=12)
    cyl("LampPole", 0.1, 4.0, (0, 0, 0.5), M['iron'], C_PROPS, g, segs=12)
    box("LampArm", (0.9, 0.1, 0.1), (side * 0.4, 0, 4.4), M['iron'], C_PROPS, g, bevel=0)
    lamp_head(g, C_PROPS, (side * 0.8, 0, 3.95))

def fence_rect(parent, w, d, gap_side=None):
    pts = []
    corners = [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)]
    sides = ['S', 'E', 'N', 'W']
    for i in range(4):
        a, b = Vector(corners[i]), Vector(corners[(i + 1) % 4])
        L = (b - a).length
        n = int(L // 2)
        ang = math.atan2(b.y - a.y, b.x - a.x)
        for k in range(n + 1):
            p = a + (b - a) * (k / n)
            if sides[i] == gap_side and 0.3 < k / n < 0.7:
                continue
            cyl("FencePost", 0.1, 1.3, (p.x, p.y, 0), M['wood'], C_PROPS, parent, segs=8)
            if k < n and not (sides[i] == gap_side and 0.2 < (k + 0.5) / n < 0.8):
                m = a + (b - a) * ((k + 0.5) / n)
                for z in (0.55, 1.05):
                    box("FenceRail", (L / n, 0.08, 0.14), (m.x, m.y, z), M['wood_light'], C_PROPS, parent,
                        rot=(0, 0, ang), bevel=0.02, segs=1)

def dummy(parent, loc, rot=0.0):
    g = empty("TrainingDummy", C_PROPS, loc, (0, 0, rot), parent)
    cyl("DummyPost", 0.09, 1.0, (0, 0, 0), M['wood'], C_PROPS, g, segs=8)
    cyl("DummyBody", 0.38, 1.0, (0, 0, 0.9), M['burlap'], C_PROPS, g, r2=0.32, segs=16)
    sphere("DummyHead", 0.3, (0, 0, 2.15), M['burlap'], C_PROPS, g, subdiv=2)
    cyl("DummyArms", 0.07, 1.5, (-0.75, 0, 1.6), M['wood'], C_PROPS, g, rot=(0, math.pi / 2, 0), base=False, segs=8)
    for r, mm, off in ((0.3, M['trim_white'], 0.0), (0.2, M['target_red'], 0.01), (0.1, M['trim_white'], 0.02)):
        cyl("Target", r, 0.03, (0, -0.37 - off, 1.4), mm, C_PROPS, g, rot=(math.pi / 2, 0, 0), base=False, segs=24)

def make_tree_meshes():
    srcs = []
    for k in range(3):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.42, radius2=0.22, depth=3.2)
        for v in bm.verts:
            v.co.z += 1.6
        me = to_mesh(bm, "TrunkMesh%d" % k)
        cyl_uv(me)
        smooth(me, True)
        me.materials.append(M['trunk'])
        blobs = []
        jr = random.Random(100 + k)
        for j in range(4 + k):
            ang = jr.uniform(0, 2 * math.pi)
            off = (math.cos(ang) * jr.uniform(0.6, 1.3), math.sin(ang) * jr.uniform(0.6, 1.3), 3.8 + jr.uniform(-0.3, 1.6))
            if j == 0:
                off = (0, 0, 4.4)
            bm = bmesh.new()
            bmesh.ops.create_icosphere(bm, subdivisions=2, radius=jr.uniform(1.4, 2.0) if j else 2.3)
            for v in bm.verts:
                v.co += v.co.normalized() * jr.uniform(-0.15, 0.15)
                v.co += Vector(off)
            blobs.append(to_mesh(bm, "tmp"))
        bm = bmesh.new()
        for b in blobs:
            bm.from_mesh(b)
            bpy.data.meshes.remove(b)
        me2 = to_mesh(bm, "CanopyMesh%d" % k)
        box_uv(me2)
        srcs.append((me, me2))
    # pines
    pines = []
    for k in range(2):
        bm = bmesh.new()
        for i, (r, z, h) in enumerate([(2.4, 1.2, 3.0), (1.9, 2.9, 2.7), (1.35, 4.5, 2.4), (0.8, 5.9, 1.8)]):
            res = bmesh.ops.create_cone(bm, cap_ends=True, segments=9 + k, radius1=r, radius2=0.05, depth=h)
            for v in res['verts']:
                v.co.z += z + h / 2
        me = to_mesh(bm, "PineMesh%d" % k)
        box_uv(me)
        me.materials.append(M['leaf_c'])
        pines.append(me)
    return srcs, pines

# --------------------------------------------------------------------------- characters
def build_player(loc, yaw=0.0):
    g = empty("Player", C_CHARS, loc, (0, 0, yaw))
    for sx in (-1, 1):
        cyl("Leg%d" % sx, 0.13, 0.75, (sx * 0.16, 0, 0.15), M['pants'], C_CHARS, g, r2=0.15, segs=12)
        box("Boot%d" % sx, (0.24, 0.36, 0.22), (sx * 0.16, 0.05, 0.11), M['boots'], C_CHARS, g, bevel=0.07)
    sphere("Torso", 0.34, (0, 0, 1.18), M['jacket'], C_CHARS, g, scale=(1.0, 0.8, 1.15), subdiv=3)
    cyl("Belt", 0.3, 0.1, (0, 0, 0.9), M['boots'], C_CHARS, g, segs=20)
    cyl("Scarf", 0.2, 0.14, (0, 0, 1.52), M['scarf'], C_CHARS, g, r2=0.18, segs=20)
    box("ScarfTail", (0.12, 0.05, 0.4), (0.12, -0.24, 1.35), M['scarf'], C_CHARS, g, rot=(0.3, 0, 0.2), bevel=0.02)
    sphere("Head", 0.3, (0, 0, 1.86), M['skin'], C_CHARS, g, subdiv=3)
    sphere("Hair", 0.32, (0, -0.03, 1.94), M['hair'], C_CHARS, g, scale=(1.0, 1.0, 0.8), subdiv=3)
    for sx in (-1, 1):
        sphere("Eye%d" % sx, 0.05, (sx * 0.11, 0.27, 1.86), M['eye'], C_CHARS, g, scale=(1, 0.6, 1.3), subdiv=2)
    box("Backpack", (0.42, 0.24, 0.5), (0, -0.33, 1.2), M['boots'], C_CHARS, g, bevel=0.06)
    # arms: right arm holding the blaster forward
    cyl("ArmL", 0.09, 0.6, (-0.4, 0, 0.85), M['jacket'], C_CHARS, g, rot=(0, 0.15, 0), segs=10)
    sphere("HandL", 0.08, (-0.44, 0, 0.82), M['skin'], C_CHARS, g, subdiv=2)
    cyl("ArmR", 0.09, 0.55, (0.36, 0.0, 1.32), M['jacket'], C_CHARS, g, rot=(-math.radians(80), 0, 0), segs=10)
    sphere("HandR", 0.08, (0.36, 0.56, 1.24), M['skin'], C_CHARS, g, subdiv=2)
    bl = empty("Blaster", C_CHARS, (0.36, 0.62, 1.3), (0, 0, 0), g)
    box("BlasterBody", (0.14, 0.42, 0.18), (0, 0.05, 0), M['iron'], C_CHARS, bl, bevel=0.03)
    box("BlasterGrip", (0.1, 0.1, 0.2), (0, -0.08, -0.13), M['boots'], C_CHARS, bl, rot=(0.3, 0, 0), bevel=0.02)
    cyl("BlasterBarrel", 0.05, 0.22, (0, 0.26, 0.02), M['iron'], C_CHARS, bl, rot=(-math.pi / 2, 0, 0), segs=12)
    box("BlasterCell", (0.16, 0.18, 0.06), (0, 0.02, 0.1), M['blaster_glow'], C_CHARS, bl, bevel=0.02)
    return g

def build_slime(loc, s=1.0, name="Slime"):
    g = empty(name, C_CHARS, loc, (0, 0, math.radians(180 + random.uniform(-30, 30))))
    sphere("SlimeBody", 0.75 * s, (0, 0, 0.5 * s), M['slime'], C_CHARS, g, scale=(1.0, 1.0, 0.72), subdiv=4)
    sphere("SlimeTop", 0.3 * s, (0, 0, 1.0 * s), M['slime'], C_CHARS, g, scale=(1, 1, 0.9), subdiv=3)
    for sx in (-1, 1):
        sphere("SlimeEyeW", 0.14 * s, (sx * 0.24 * s, -0.62 * s, 0.62 * s), M['eye_white'], C_CHARS, g, subdiv=2)
        sphere("SlimeEye", 0.07 * s, (sx * 0.24 * s, -0.73 * s, 0.62 * s), M['eye'], C_CHARS, g, subdiv=2)
    return g

def build_charger(loc, yaw):
    g = empty("Charger", C_CHARS, loc, (0, 0, yaw))
    sphere("ChargerBody", 0.9, (0, 0, 0.95), M['charger'], C_CHARS, g, scale=(1.0, 1.25, 0.85), subdiv=4)
    for sx in (-1, 1):
        cyl("ChargerLeg", 0.16, 0.5, (sx * 0.5, sx * 0.5, 0), M['charger'], C_CHARS, g, segs=10)
        cyl("ChargerLegB", 0.16, 0.5, (sx * 0.5, -sx * 0.5, 0), M['charger'], C_CHARS, g, segs=10)
        cyl("Horn", 0.16, 0.7, (sx * 0.45, -1.0, 1.35), M['horn'], C_CHARS, g, r2=0.0, rot=(math.radians(-60), sx * 0.3, 0), segs=12)
        sphere("ChargerEye", 0.11, (sx * 0.3, -1.08, 1.05), M['eye_white'], C_CHARS, g, subdiv=2)
        sphere("ChargerPupil", 0.06, (sx * 0.3, -1.17, 1.05), M['eye'], C_CHARS, g, subdiv=2)

def aim(ob, target):
    d = Vector(target) - ob.location
    ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

def render_settings():
    r = SC.render
    r.engine = 'BLENDER_EEVEE'
    r.resolution_x, r.resolution_y = 1920, 1080
    r.resolution_percentage = 100
    ee = SC.eevee
    for attr, val in (('taa_render_samples', 64), ('use_raytracing', True), ('use_shadows', True),
                      ('shadow_ray_count', 2), ('shadow_step_count', 8), ('use_gtao', True),
                      ('fast_gi_method', 'GLOBAL_ILLUMINATION'), ('use_bloom', True)):
        try:
            setattr(ee, attr, val)
        except Exception:
            pass
    try:
        ee.ray_tracing_options.resolution_scale = '2'
    except Exception:
        pass
    vs = SC.view_settings
    try:
        vs.view_transform = 'AgX'
        for look in ('AgX - Punchy', 'Punchy', 'AgX - Medium High Contrast', 'Medium High Contrast'):
            try:
                vs.look = look
                break
            except Exception:
                pass
    except Exception:
        pass
    vs.exposure = 0.0

