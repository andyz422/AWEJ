"""
AWEJ - Dusty Crossroads, high-quality Blender draft.

Rebuilds the starting area of the Unity game (AreaDatabase.DustyCrossroads) as a
stylized 3D scene in its own Blender scene called "AWEJ_DustyCrossroads".
Layout matches the game: Unity (x, z) -> Blender (x, y), Unity yaw f -> Blender rot Z = -f.

Run inside Blender:  exec(open(r"<path>/build_dusty_crossroads.py").read())
Re-running deletes and rebuilds only that scene.
"""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix, Euler

SCENE_NAME = "AWEJ_DustyCrossroads"
rng = random.Random(11)

# --------------------------------------------------------------------------- scene
def reset_scene():
    old = bpy.data.scenes.get(SCENE_NAME)
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        def drop(c):
            for ch in list(c.children):
                drop(ch)
                bpy.data.collections.remove(ch)
        drop(old.collection)
        bpy.data.scenes.remove(old)
        bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    sc = bpy.data.scenes.new(SCENE_NAME)
    bpy.context.window.scene = sc
    return sc

SC = reset_scene()
COLLS = {}
def coll(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or SC.collection).children.link(c)
    COLLS[name] = c
    return c

C_TERRAIN = coll("Terrain")
C_WALLS = coll("Walls_Gate")
C_PATHS = coll("Paths")
C_BUILD = coll("Buildings")
C_PROPS = coll("Props")
C_NATURE = coll("Nature")
C_CHARS = coll("Characters")
C_SKY = coll("Sky_Lighting")

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

def plain(name, color, rough=0.6, metal=0.0, emit=None, emit_strength=0.0, coat=0.0):
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

def textured(name, kind, c1, c2, rough=0.8, scale=1.0, bump=0.3, coords='UV',
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

# --------------------------------------------------------------------------- terrain
def build_terrain():
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=60, y_segments=60, size=450)
    nr = random.Random(5)
    for v in bm.verts:
        d = max(abs(v.co.x), abs(v.co.y))
        if d > 70:
            k = min((d - 70) / 120, 1.0)
            v.co.z = k * k * (6 + 10 * math.sin(v.co.x * 0.021) * math.cos(v.co.y * 0.017) + nr.uniform(-1.5, 1.5)) - 0.02
        else:
            v.co.z = -0.02
    me = to_mesh(bm, "Ground")
    smooth(me)
    place("Ground", me, C_TERRAIN, mat=M['grass'])
    # rolling hills and far mountains
    hr = random.Random(7)
    for i in range(18):
        a = hr.uniform(0, 2 * math.pi)
        d = hr.uniform(170, 300)
        rad = hr.uniform(35, 70)
        sphere("Hill_%02d" % i, rad, (math.cos(a) * d, math.sin(a) * d, -rad * 0.62), M['grass_far'], C_TERRAIN,
               scale=(1.3, 1.0, 0.9), subdiv=3, ico=True, jitter=0.05, seed=i)
    for i, (x, y, r, h) in enumerate([(-700, 1100, 330, 260), (-200, 1250, 380, 330), (350, 1150, 340, 280),
                                      (850, 1000, 300, 230), (-1150, 900, 300, 200), (100, 950, 220, 170)]):
        ob = sphere("Mountain_%d" % i, 1.0, (x, y, -10), M['mountain'], C_TERRAIN, scale=(r, r * 0.8, h),
                    subdiv=2, ico=True, jitter=0.22, do_smooth=False, seed=300 + i)
        ob.rotation_euler = (0, 0, hr.uniform(0, 3))

# --------------------------------------------------------------------------- walls and gate
HALF = 50.0
WALL_H = 3.0
WALL_T = 1.0

def wall_run(name, a, b, coll_):
    a, b = Vector(a), Vector(b)
    mid = (a + b) / 2
    L = (b - a).length
    ang = math.atan2(b.y - a.y, b.x - a.x)
    g = empty(name, coll_, (mid.x, mid.y, 0), (0, 0, ang))
    box(name + "_Base", (L, WALL_T + 0.5, 0.7), (0, 0, 0.35), M['stone_base'], coll_, g, bevel=0.08)
    box(name + "_Body", (L, WALL_T, WALL_H), (0, 0, WALL_H / 2), M['wall'], coll_, g, bevel=0.05)
    box(name + "_Cap", (L, WALL_T + 0.3, 0.3), (0, 0, WALL_H + 0.15), M['wall_cap'], coll_, g, bevel=0.06)
    n = max(1, int(L // 10))
    for i in range(n + 1):
        x = -L / 2 + i * (L / n)
        box(name + "_Pillar%02d" % i, (1.4, WALL_T + 0.7, WALL_H + 0.8), (x, 0, (WALL_H + 0.8) / 2),
            M['wall'], coll_, g, bevel=0.08)
        box(name + "_PillarCap%02d" % i, (1.7, WALL_T + 1.0, 0.35), (x, 0, WALL_H + 0.95), M['wall_cap'], coll_, g, bevel=0.06)
    # merlons
    m = int(L // 2.5)
    for i in range(m):
        x = -L / 2 + (i + 0.5) * (L / m)
        box(name + "_Merlon%02d" % i, (0.9, WALL_T + 0.1, 0.5), (x, 0, WALL_H + 0.55), M['wall_cap'], coll_, g, bevel=0.05)

def build_walls():
    o = HALF + WALL_T / 2
    wall_run("WallSouth", (-o, -o), (o, -o), C_WALLS)
    wall_run("WallEast", (o, -o), (o, o), C_WALLS)
    wall_run("WallWest", (-o, o), (-o, -o), C_WALLS)
    wall_run("WallNorthW", (-o, o), (-3.4, o), C_WALLS)
    wall_run("WallNorthE", (3.4, o), (o, o), C_WALLS)

    # North exit gate (locked until the area's battles are won) toward Whispering Grove.
    g = empty("Gate_North", C_WALLS, (0, HALF + 0.5, 0))
    post_h = 6.0
    for sx in (-1, 1):
        box("GatePost%d" % sx, (1.6, 1.8, post_h), (sx * 2.9, 0, post_h / 2), M['wall'], C_WALLS, g, bevel=0.08)
        box("GatePostBase%d" % sx, (2.0, 2.2, 0.9), (sx * 2.9, 0, 0.45), M['stone_base'], C_WALLS, g, bevel=0.08)
        box("GatePostCap%d" % sx, (2.0, 2.2, 0.4), (sx * 2.9, 0, post_h + 0.2), M['wall_cap'], C_WALLS, g, bevel=0.06)
        cyl("GateOrbStand%d" % sx, 0.35, 0.3, (sx * 2.9, 0, post_h + 0.4), M['iron'], C_WALLS, g)
        sphere("GateOrb%d" % sx, 0.5, (sx * 2.9, 0, post_h + 1.15), M['orb_red'], C_WALLS, g, subdiv=3)
        li = bpy.data.lights.new("GateOrbLight%d" % sx, 'POINT')
        li.energy = 300
        li.color = (1.0, 0.25, 0.12)
        li.shadow_soft_size = 0.5
        place("GateOrbLight%d" % sx, li, C_WALLS, (sx * 2.9, -0.5, post_h + 1.2), parent=g)
        # banners
        box("Banner%d" % sx, (1.1, 0.05, 2.4), (sx * 2.9, -0.95, post_h - 1.8), M['banner_red'], C_WALLS, g, bevel=0)
        box("BannerRod%d" % sx, (1.4, 0.08, 0.08), (sx * 2.9, -0.98, post_h - 0.55), M['brass'], C_WALLS, g, bevel=0)
    box("GateLintel", (7.4, 1.6, 1.0), (0, 0, 4.8), M['wall'], C_WALLS, g, bevel=0.08)
    box("GateLintelCap", (7.8, 1.9, 0.3), (0, 0, 5.45), M['wall_cap'], C_WALLS, g, bevel=0.05)
    # portcullis
    for i in range(5):
        box("GateBar%d" % i, (0.18, 0.18, 4.2), (-1.6 + i * 0.8, -0.2, 2.1), M['iron'], C_WALLS, g, bevel=0.02)
    for z in (1.2, 2.6, 3.8):
        box("GateRail%.1f" % z, (4.2, 0.2, 0.2), (0, -0.25, z), M['iron'], C_WALLS, g, bevel=0.02)
    box("GatePlaque", (5.4, 0.15, 0.8), (0, -0.9, 4.8), M['wood'], C_WALLS, g, bevel=0.03)
    text("GateText", "Whispering Grove", 0.42, (0, -1.0, 4.78), M['sign_text'], C_WALLS, g)

# --------------------------------------------------------------------------- paths
PATHS = [((0, -50), (0, 50), 8), ((-17, 18), (0, 18), 4), ((-17, -18), (0, -18), 4),
         ((0, 18), (17, 18), 4), ((0, -18), (17, -18), 4)]

def build_paths():
    for i, (a, b, w) in enumerate(PATHS):
        cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        lx = abs(b[0] - a[0]) or w
        ly = abs(b[1] - a[1]) or w
        if lx == w:
            ly += 0
        box("Path_%d" % i, (lx if lx != w else w, ly if ly != w else w, 0.08), (cx, cy, 0.0 + 0.002 * i),
            M['gravel'], C_PATHS, bevel=0.04, segs=1)
    for y in (18, -18):
        cyl("Plaza_%d" % y, 6.5, 0.12, (0, y, 0), M['cobble'], C_PATHS, segs=48, bevel=0.04)
        cyl("PlazaRim_%d" % y, 6.8, 0.08, (0, y, 0), M['stone_base'], C_PATHS, segs=48)
    # edge stones along the main road
    er = random.Random(3)
    stone_meshes = []
    for k in range(4):
        ob = sphere("EdgeStoneSrc%d" % k, 0.35, (0, 0, -50), M['rock'], C_PATHS, scale=(1.2, 0.9, 0.45),
                    subdiv=1, ico=True, jitter=0.25, do_smooth=False, seed=k)
        stone_meshes.append(ob.data)
        bpy.data.objects.remove(ob)
    y = -49
    while y < 49:
        for sx in (-1, 1):
            if abs(abs(y) - 18) < 7.2:
                continue
            place("EdgeStone", er.choice(stone_meshes), C_PATHS, (sx * (4.2 + er.uniform(-0.1, 0.3)), y, 0.02),
                  (0, 0, er.uniform(0, 3)), scale=(er.uniform(0.7, 1.3),) * 3)
        y += er.uniform(1.2, 2.0)

# --------------------------------------------------------------------------- buildings
def house_roof(name, parent, coll_, span, length, z, rise_pts, mat, thickness=0.3, overhang=0.7, rot_z=0.0, end_overhang=0.6):
    prof = roof_profile(span, rise_pts, thickness, overhang)
    return prism(name, prof, length + 2 * end_overhang, mat, coll_, (0, 0, z), parent, (0, 0, rot_z), bevel=0.03)

def build_general_store():
    c = coll("B_GeneralStore", C_BUILD)
    root = empty("GeneralStore", c, (-24, 18, 0), facing_rot(-90))
    W, D, H, base = 12.0, 10.0, 4.6, 0.3
    box("Foundation", (W + 0.4, D + 0.4, base), (0, 0, base / 2), M['stone_base'], c, root, bevel=0.06)
    box("Walls", (W, D, H), (0, 0, base + H / 2), M['plaster'], c, root, bevel=0.04)
    top = base + H
    # timber frame
    for sx in (-1, 1):
        for sy in (-1, 1):
            box("Post", (0.36, 0.36, H), (sx * (W / 2), sy * (D / 2), base + H / 2), M['trim_brown'], c, root, bevel=0.03)
    for sy in (-1, 1):
        box("BeamTop", (W + 0.3, 0.3, 0.3), (0, sy * (D / 2 + 0.03), top - 0.15), M['trim_brown'], c, root, bevel=0.03)
        box("BeamMid", (W, 0.24, 0.22), (0, sy * (D / 2 + 0.03), base + 2.0), M['trim_brown'], c, root, bevel=0.03)
    for sx in (-1, 1):
        box("BeamSide", (0.3, D + 0.3, 0.3), (sx * (W / 2 + 0.03), 0, top - 0.15), M['trim_brown'], c, root, bevel=0.03)
    # attic gables + roof (ridge along local X, eaves front/back)
    rise = (D / 2) * math.tan(math.radians(35))
    prism("Attic", [(-D / 2, 0), (D / 2, 0), (0, rise)], W, M['plaster'], c, (0, 0, top), root)
    house_roof("Roof", root, c, D, W, top, [(-D / 2, 0), (0, rise), (D / 2, 0)], M['roof_blue'], overhang=0.9)
    box("RidgeCap", (W + 1.4, 0.45, 0.25), (0, 0, top + rise + 0.32), M['trim_brown'], c, root, bevel=0.04)
    # chimney
    box("Chimney", (1.0, 1.0, 3.6), (3.4, 1.8, top + 2.2), M['stone_base'], c, root, bevel=0.05)
    box("ChimneyCap", (1.3, 1.3, 0.25), (3.4, 1.8, top + 4.1), M['wall_cap'], c, root, bevel=0.04)
    # windows (front, sides, back) with blue shutters
    shutter = plain('ShutterBlue', (0.2, 0.38, 0.62), 0.6)
    for x in (-3.8, 3.8):
        window(root, c, "WinFront%.0f" % x, (x, -D / 2 - 0.05, base + 2.0), (0, -1), 1.5, 1.5, M['trim_white'], shutter)
        window(root, c, "WinBack%.0f" % x, (x, D / 2 + 0.05, base + 2.0), (0, 1), 1.5, 1.5, M['trim_white'], shutter)
    for sx in (-1, 1):
        window(root, c, "WinSide%d" % sx, (sx * (W / 2 + 0.05), 0, base + 2.0), (sx, 0), 1.5, 1.5, M['trim_white'], shutter)
        window(root, c, "WinGable%d" % sx, (sx * (W / 2 + 0.05), 0, top + 1.2), (sx, 0), 0.9, 0.9, M['trim_white'])
        # flower boxes
        box("FlowerBox%d" % sx, (1.6, 0.4, 0.3), (sx * 3.8, -D / 2 - 0.4, base + 1.0), M['wood'], c, root, bevel=0.03)
        for k in range(6):
            sphere("Bloom", 0.14, (sx * 3.8 - 0.65 + k * 0.26, -D / 2 - 0.4, base + 1.22), M['flowers'][k % 4], c, root, subdiv=1)
    door_assembly(root, c, D / 2, True, M['trim_brown'], base)
    # awning over the door
    g = empty("Awning", c, (0, -D / 2 - 0.9, base + 3.55), (math.radians(-18), 0, 0), root)
    box("AwningBoard", (3.6, 1.9, 0.12), (0, 0, 0), M['roof_blue'], c, g, bevel=0.03)
    for sx in (-1, 1):
        box("AwningBracket%d" % sx, (0.12, 1.4, 0.12), (sx * 1.6, 0.25, -0.35), M['trim_brown'], c, g,
            rot=(math.radians(38), 0, 0), bevel=0.02)
    signboard(root, c, "General Store", (0, -D / 2 - 0.16, base + 4.15), True, width=4.8)
    # crates and barrels near the door
    for i, (x, y, z, s) in enumerate([(-4.2, -6.2, 0, 1.0), (-4.9, -7.2, 0, 0.9), (-4.4, -6.6, 1.0, 0.8)]):
        box("Crate%d" % i, (s, s, s), (x, y, z + s / 2), M['wood_light'], c, root, rot=(0, 0, 0.2 * i), bevel=0.05)
    for i, (x, y) in enumerate([(4.4, -6.4), (5.3, -6.9)]):
        barrel(root, c, (x, y, 0))
    return root

def barrel(parent, coll_, loc, name="Barrel"):
    g = empty(name, coll_, loc, (0, 0, 0), parent)
    cyl("Stave", 0.42, 1.1, (0, 0, 0), M['wood_light'], coll_, g, segs=20)
    cyl("StaveMid", 0.47, 0.5, (0, 0, 0.3), M['wood_light'], coll_, g, segs=20)
    for z in (0.12, 0.95):
        cyl("Hoop", 0.45, 0.07, (0, 0, z), M['iron'], coll_, g, segs=20)
    for z in (0.32, 0.75):
        cyl("Hoop", 0.49, 0.06, (0, 0, z), M['iron'], coll_, g, segs=20)
    return g

def build_tinker_workshop():
    c = coll("B_TinkersWorkshop", C_BUILD)
    root = empty("TinkersWorkshop", c, (-24, -18, 0), facing_rot(-90))
    cyl("Plinth", 5.2, 0.6, (0, 0, 0), M['stone_base'], c, root, segs=40, bevel=0.06)
    cyl("Body", 4.6, 9.5, (0, 0, 0.6), M['tower_stone'], c, root, r2=4.15, segs=40)
    cyl("Band1", 4.75, 0.35, (0, 0, 4.0), M['wall_cap'], c, root, r2=4.7, segs=40)
    cyl("Corbel", 4.25, 0.6, (0, 0, 10.1), M['wall_cap'], c, root, r2=4.9, segs=40)
    for i in range(16):
        a = i * 2 * math.pi / 16
        box("Crenel%02d" % i, (1.1, 0.8, 0.7), (math.cos(a) * 4.5, math.sin(a) * 4.5, 11.05), M['tower_stone'], c, root,
            rot=(0, 0, a + math.pi / 2), bevel=0.05)
    cyl("Roof", 4.6, 5.6, (0, 0, 10.7), M['roof_orange'], c, root, r2=0.15, segs=32)
    cyl("Finial", 0.08, 1.4, (0, 0, 16.2), M['brass'], c, root)
    sphere("FinialBall", 0.22, (0, 0, 16.3), M['brass'], c, root, subdiv=2)
    # weathervane arrow
    box("VaneArrow", (1.4, 0.05, 0.18), (0.2, 0, 17.3), M['brass'], c, root, bevel=0)
    cyl("VaneTip", 0.18, 0.4, (0.95, 0, 17.3), M['brass'], c, root, rot=(0, math.pi / 2, 0), r2=0, base=False, segs=4)
    # windows around the tower
    for i, a in enumerate([math.pi / 2, math.pi * 0.95, 0.05, math.pi * 1.25, math.pi * 1.75]):
        for z in (6.2, 8.6):
            r = 4.6 - (z - 0.6) / 9.5 * 0.45 + 0.05
            window(root, c, "TWin%d_%.0f" % (i, z), (math.cos(a) * r, math.sin(a) * r, z), (math.cos(a), math.sin(a)),
                   1.0, 1.4, M['wall_cap'], cross=True)
    # door surround on the front (local -Y)
    box("DoorSurround", (3.6, 1.3, 4.0), (0, -4.35, 2.6), M['tower_stone'], c, root, bevel=0.08)
    box("DoorSurroundCap", (4.0, 1.5, 0.35), (0, -4.4, 4.7), M['wall_cap'], c, root, bevel=0.05)
    door_assembly(root, c, 5.0, True, M['wall_cap'], 0.6)
    signboard(root, c, "Tinker's Workshop", (0, -5.25, 5.55), True, width=5.6)
    # gears and pipes (workshop flavour)
    gear(root, c, (2.4, -4.25, 7.3), 1.3, M['brass'])
    gear(root, c, (3.5, -3.1, 5.6), 0.8, M['iron'], rot_z=math.radians(-40))
    for i, (x, y) in enumerate([(-3.0, 3.3), (-3.9, 2.0)]):
        cyl("Pipe%d" % i, 0.22, 12.5, (x, y, 0.6), M['iron'], c, root, segs=12)
        cyl("PipeCap%d" % i, 0.34, 0.4, (x, y, 13.1), M['iron'], c, root, segs=12)
    # crates of parts
    for i, (x, y) in enumerate([(3.6, -6.0), (4.5, -6.6)]):
        box("PartsCrate%d" % i, (0.9, 0.9, 0.9), (x, y, 0.45), M['wood_light'], c, root, rot=(0, 0, 0.3 * i), bevel=0.05)
    for k in range(3):
        gear(root, c, (3.6 + k * 0.2, -6.0, 0.95 + k * 0.08), 0.3, M['brass'], flat=True)
    return root

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

def build_training_barn():
    c = coll("B_TrainingBarn", C_BUILD)
    root = empty("TrainingBarn", c, (24, 18, 0), facing_rot(90))
    W, D, H, base = 14.0, 12.0, 5.2, 0.3
    box("Foundation", (W + 0.4, D + 0.4, base), (0, 0, base / 2), M['stone_base'], c, root, bevel=0.06)
    box("Walls", (W, D, H), (0, 0, base + H / 2), M['barn_red'], c, root, bevel=0.03)
    top = base + H
    for sx in (-1, 1):
        for sy in (-1, 1):
            box("CornerTrim", (0.35, 0.35, H), (sx * W / 2, sy * D / 2, base + H / 2), M['trim_white'], c, root, bevel=0.03)
    for sy in (-1, 1):
        box("EaveTrim", (W + 0.2, 0.25, 0.3), (0, sy * (D / 2 + 0.02), top - 0.15), M['trim_white'], c, root, bevel=0.03)
    # gambrel gable faces front: ridge along local Y -> build along X and rotate 90deg
    h = W / 2
    pts = [(-h, 0), (-h * 0.62, 2.6), (0, 4.2), (h * 0.62, 2.6), (h, 0)]
    prism("Gable", pts, D, M['barn_red'], c, (0, 0, top), root, (0, 0, math.pi / 2))
    house_roof("Roof", root, c, W, D, top, pts, M['roof_slate'], thickness=0.32, overhang=0.8, rot_z=math.pi / 2)
    box("RidgeCap", (0.5, D + 1.4, 0.25), (0, 0, top + 4.2 + 0.36), M['roof_slate'], c, root, bevel=0.04)
    # gable trim lines on the front
    f = -D / 2 - 0.06
    for i in range(len(pts) - 1):
        (s0, z0), (s1, z1) = pts[i], pts[i + 1]
        L = math.hypot(s1 - s0, z1 - z0)
        ang = math.atan2(z1 - z0, s1 - s0)
        box("GableTrim%d" % i, (L + 0.3, 0.2, 0.28), ((s0 + s1) / 2, f, top + (z0 + z1) / 2 + 0.1), M['trim_white'],
            c, root, rot=(0, -ang, 0), bevel=0.02)
    # big barn doors with X braces (the Unity door sits in the middle)
    for sx in (-1, 1):
        cx = sx * 1.6
        box("BarnDoor%d" % sx, (3.1, 0.2, 4.3), (cx, f - 0.02, base + 2.15), M['barn_red'], c, root, bevel=0.03)
        for dx, dz, w_, h_ in [(0, 2.1, 3.1, 0.22), (0, -2.1, 3.1, 0.22), (1.45, 0, 0.22, 4.3), (-1.45, 0, 0.22, 4.3)]:
            box("BarnDoorFrame", (w_, 0.12, h_), (cx + dx, f - 0.16, base + 2.15 + dz), M['trim_white'], c, root, bevel=0.02)
        diag = math.hypot(2.9, 4.1)
        for s in (-1, 1):
            box("BarnDoorX", (0.2, 0.1, diag), (cx, f - 0.18, base + 2.15), M['trim_white'], c, root,
                rot=(0, s * math.atan2(2.9, 4.1), 0), bevel=0.02)
    box("DoorTrack", (7.4, 0.2, 0.25), (0, f - 0.12, base + 4.5), M['iron'], c, root, bevel=0.02)
    door_assembly(root, c, D / 2 + 0.25, False, M['trim_white'], base)
    # hayloft door + hoist beam
    box("Hayloft", (2.2, 0.15, 2.0), (0, f, top + 1.6), M['barn_red'], c, root, bevel=0.03)
    for dx, dz, w_, h_ in [(0, 1.0, 2.4, 0.2), (0, -1.0, 2.4, 0.2), (1.1, 0, 0.2, 2.0), (-1.1, 0, 0.2, 2.0)]:
        box("HayloftFrame", (w_, 0.1, h_), (dx, f - 0.1, top + 1.6 + dz), M['trim_white'], c, root, bevel=0.02)
    box("HoistBeam", (0.3, 1.8, 0.3), (0, f - 0.6, top + 3.3), M['wood'], c, root, bevel=0.03)
    cyl("HoistRope", 0.03, 2.2, (0, f - 1.35, top + 1.1), M['burlap'], c, root, segs=6)
    signboard(root, c, "Training Barn", (0, f - 0.12, top + 0.45), False, width=4.8)
    # side windows
    for sx in (-1, 1):
        for y in (-3, 3):
            window(root, c, "BWin%d_%d" % (sx, y), (sx * (W / 2 + 0.05), y, base + 3.0), (sx, 0), 1.3, 1.3, M['trim_white'])
    # hay bales by the door
    for i, (x, y, z) in enumerate([(-4.6, -7.4, 0), (-5.9, -7.2, 0), (-5.2, -7.3, 1.05)]):
        cyl("HayBale%d" % i, 0.55, 1.3, (x, y, z + 0.55), M['hay'], c, root, rot=(0, math.pi / 2, 0), base=False, segs=20)
    return root

def build_old_cellar():
    c = coll("B_OldCellar", C_BUILD)
    root = empty("OldCellar", c, (24, -18, 0), facing_rot(90))
    cyl("Plinth", 4.9, 0.3, (0, 0, 0), M['stone_base'], c, root, segs=36, bevel=0.05)
    cyl("Body", 4.5, 3.4, (0, 0, 0.3), M['hut_stone'], c, root, r2=4.3, segs=36)
    cyl("RoofRing", 4.6, 0.3, (0, 0, 3.7), M['wood'], c, root, segs=36)
    cyl("Roof", 5.9, 4.6, (0, 0, 3.8), M['thatch'], c, root, r2=0.5, segs=36)
    cyl("RoofTop", 0.7, 0.9, (0, 0, 8.3), M['thatch'], c, root, r2=0.05, segs=16)
    # thatch tie bands
    for z, r in ((5.0, 4.6), (6.6, 2.85)):
        cyl("ThatchBand", r + 0.05, 0.18, (0, 0, z), M['wood'], c, root, r2=r - 0.15, segs=36)
    # entrance porch (stone arch box) at local -Y
    box("Entry", (3.6, 1.4, 3.7), (0, -4.3, 0.3 + 1.85), M['hut_stone'], c, root, bevel=0.1)
    box("EntryLintel", (3.9, 1.6, 0.4), (0, -4.35, 3.95), M['wall_cap'], c, root, bevel=0.06)
    prism("EntryRoof", roof_profile(3.6, [(-1.8, 0), (0, 1.0), (1.8, 0)], 0.25, 0.4), 1.8, M['thatch'],
          c, (0, -4.4, 4.1), root, (0, 0, math.pi / 2))
    door_assembly(root, c, 5.0, False, M['wall_cap'], 0.3)
    # lantern by the door, steps down to the cellar
    lamp_head(root, c, (1.7, -5.2, 3.0), wall=True)
    for sx in (-1, 1):
        window(root, c, "HWin%d" % sx, (sx * 4.45, 0.6, 2.2), (sx, 0.1), 0.8, 0.9, M['wood'], cross=False)
        box("WinBars%d" % sx, (0.1, 0.08, 0.9), (sx * 4.55, 0.6, 2.2), M['iron'], c, root, rot=(0, 0, math.pi / 2), bevel=0)
    # signpost next to the door
    g = empty("SignPost", c, (3.4, -6.6, 0), (0, 0, 0), root)
    cyl("SignPole", 0.12, 3.4, (0, 0, 0), M['wood'], c, g, segs=10)
    signboard(g, c, "Old Cellar", (0, -0.2, 2.9), False, width=3.8)
    # rubble and moss rocks
    rr = random.Random(9)
    for i in range(7):
        a = rr.uniform(0.5, 2.6)
        sphere("Rubble%d" % i, rr.uniform(0.3, 0.6), (math.cos(a) * 5.4, math.sin(a) * 5.4, 0.1), M['rock'], c, root,
               subdiv=1, ico=True, jitter=0.3, do_smooth=False, scale=(1.2, 1, 0.7), seed=i)
    barrel(root, c, (-2.6, -6.2, 0))
    return root

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

def build_props():
    for y in (-42, -30, -6, 6, 30, 42):
        for sx in (-1, 1):
            lamp_post((sx * 5.6, y, 0), -sx)
    # well on the west side of the town center
    g = empty("Well", C_PROPS, (-10, 0, 0))
    cyl("WellRing", 1.4, 1.0, (0, 0, 0), M['stone_base'], C_PROPS, g, segs=32, bevel=0.05)
    cyl("WellLip", 1.5, 0.15, (0, 0, 1.0), M['wall_cap'], C_PROPS, g, segs=32)
    cyl("WellWater", 1.15, 0.05, (0, 0, 0.75), M['water'], C_PROPS, g, segs=32)
    for sx in (-1, 1):
        box("WellPost%d" % sx, (0.22, 0.22, 2.8), (sx * 1.25, 0, 1.4), M['wood'], C_PROPS, g, bevel=0.02)
    prism("WellRoof", roof_profile(2.0, [(-1.0, 0), (0, 0.8), (1.0, 0)], 0.12, 0.35), 3.2, M['roof_blue'],
          C_PROPS, (0, 0, 2.75), g, bevel=0.02)
    cyl("WellCrank", 0.1, 2.5, (-1.25, 0, 2.1), M['wood'], C_PROPS, g, rot=(0, math.pi / 2, 0), base=False, segs=10)
    cyl("WellRope", 0.02, 1.0, (0, 0, 1.15), M['burlap'], C_PROPS, g, segs=6)
    cyl("Bucket", 0.25, 0.4, (0, 0, 1.05), M['wood_light'], C_PROPS, g, r2=0.3, segs=14)
    for k in range(10):
        a = k * 0.63
        sphere("WellFlower", 0.13, (math.cos(a) * 1.9, math.sin(a) * 1.9, 0.12), M['flowers'][k % 4], C_PROPS, g, subdiv=1)
    # crossroads signpost
    g = empty("Signpost", C_PROPS, (5.8, 1.5, 0))
    cyl("SignpostPole", 0.13, 3.8, (0, 0, 0), M['wood'], C_PROPS, g, segs=10)
    for name_, z, ang in [("SHOPS", 3.2, math.pi), ("BATTLES", 2.6, 0.0), ("NORTH GATE", 2.0, math.pi / 2)]:
        a = empty("Arrow_" + name_, C_PROPS, (0, 0, z), (0, 0, ang), g)
        L = 0.3 * len(name_) + 0.6
        box("ArrowBoard", (L, 0.1, 0.45), (L / 2 + 0.1, 0, 0), M['wood_light'], C_PROPS, a, bevel=0.02)
        cyl("ArrowTip", 0.32, 0.1, (L + 0.1, 0, 0), M['wood_light'], C_PROPS, a, rot=(math.pi / 2, 0, 0), base=False, segs=3)
        text("ArrowText", name_, 0.26, (L / 2 + 0.1, -0.07, 0), M['trim_brown'], C_PROPS, a)
        text("ArrowTextB", name_, 0.26, (L / 2 + 0.1, 0.07, 0), M['trim_brown'], C_PROPS, a, rot=(math.pi / 2, 0, math.pi))
    # training pen with dummies east of the barn
    pen = empty("TrainingPen", C_PROPS, (39, 4, 0))
    fence_rect(pen, 12, 9, gap_side='W')
    for i, (x, y) in enumerate([(-2.5, 1.5), (1.0, -1.0), (3.5, 2.0)]):
        dummy(pen, (x, y, 0), rot=math.radians(90 + i * 15))
    for i, (x, y) in enumerate([(4.5, -3.0), (3.2, -3.2)]):
        cyl("PenHay%d" % i, 0.55, 1.3, (x, y, 0.55), M['hay'], C_PROPS, pen, rot=(math.pi / 2, 0, 0.3), base=False, segs=20)

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

# --------------------------------------------------------------------------- nature
BLOCKERS = [((-24, 18), 9.5), ((-24, -18), 8.5), ((24, 18), 10.5), ((24, -18), 8.5), ((-10, 0), 3.0),
            ((5.8, 1.5), 1.5), ((0, 18), 7.5), ((0, -18), 7.5), ((39, 4), 7.5), ((0, -30), 3.0)]

def on_path(x, y, m=0.0):
    if abs(x) < 4 + m:
        return True
    for yy in (18, -18):
        if abs(y - yy) < 2 + m and abs(x) < 19:
            return True
    return False

def free_spot(x, y, margin=0.0, path_margin=1.5):
    if abs(x) > HALF - 1.5 or abs(y) > HALF - 1.5:
        return False
    if on_path(x, y, path_margin):
        return False
    for (cx, cy), r in BLOCKERS:
        if math.hypot(x - cx, y - cy) < r + margin:
            return False
    return True

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

def build_nature():
    srcs, pines = make_tree_meshes()
    trunk_pine = None
    tr = random.Random(21)
    leaf_mats = [M['leaf_a'], M['leaf_b'], M['leaf_a']]
    placed = []

    def plant(x, y, pine=False, s=None, coll_=C_NATURE):
        s = s or tr.uniform(0.8, 1.25)
        rot = (0, 0, tr.uniform(0, 6.28))
        if pine:
            g = place("Pine", pines[tr.randrange(2)], coll_, (x, y, 0), rot, scale=(s, s, s * tr.uniform(0.9, 1.3)))
            t = place("PineTrunk", srcs[0][0], coll_, (0, 0, 0), parent=g, scale=(0.6, 0.6, 0.5))
        else:
            k = tr.randrange(3)
            g = place("Tree", srcs[k][0], coll_, (x, y, 0), rot, scale=(s, s, s))
            cn = place("Canopy", srcs[k][1], coll_, (0, 0, 0), parent=g)
            cn.material_slots and None
            if len(srcs[k][1].materials) == 0:
                srcs[k][1].materials.append(M['leaf_a'])
            cn.material_slots[0].link = 'OBJECT'
            cn.material_slots[0].material = tr.choice(leaf_mats)
        placed.append((x, y))

    def ok(x, y, m):
        return free_spot(x, y, m) and all(math.hypot(x - a, y - b) > 4.5 for a, b in placed)

    # trees inside the walls: perimeter band + clusters
    tries = 0
    while len(placed) < 26 and tries < 4000:
        tries += 1
        if tr.random() < 0.85:
            side = tr.randrange(4)
            t = tr.uniform(-46, 46)
            d = tr.uniform(41, 47)
            x, y = [(t, -d), (t, d), (d, t), (-d, t)][side]
        else:
            x, y = tr.uniform(-46, 46), tr.uniform(-46, 46)
        if ok(x, y, 2.0):
            plant(x, y, pine=tr.random() < 0.3)
    # forest outside the walls
    out = coll("OutsideForest", C_NATURE)
    for i in range(260):
        a = tr.uniform(0, 2 * math.pi)
        d = tr.uniform(58, 170)
        x, y = math.cos(a) * d * 1.05, math.sin(a) * d
        if abs(x) < 9 and y > 50 and y < 75:
            continue
        plant(x, y, pine=tr.random() < 0.55, s=tr.uniform(1.0, 1.8), coll_=out)

    # bushes, rocks, grass tufts, flowers
    bush_src = []
    for k in range(3):
        ob = sphere("BushSrc%d" % k, 0.9, (0, 0, 0), M['leaf_b'], C_NATURE, subdiv=2, ico=True, jitter=0.18,
                    scale=(1.2, 1.0, 0.75), seed=40 + k)
        bush_src.append(ob.data)
        bpy.data.objects.remove(ob)
    rock_src = []
    for k in range(4):
        ob = sphere("RockSrc%d" % k, 0.8, (0, 0, 0), M['rock'], C_NATURE, subdiv=1, ico=True, jitter=0.3,
                    do_smooth=False, scale=(1.3, 1.0, 0.7), seed=60 + k)
        rock_src.append(ob.data)
        bpy.data.objects.remove(ob)
    n = 0
    while n < 70:
        x, y = tr.uniform(-48, 48), tr.uniform(-48, 48)
        if free_spot(x, y, 0.5, 1.0):
            if tr.random() < 0.6:
                place("Bush", tr.choice(bush_src), C_NATURE, (x, y, 0.25), (0, 0, tr.uniform(0, 6)), scale=(tr.uniform(0.6, 1.3),) * 3)
            else:
                place("Rock", tr.choice(rock_src), C_NATURE, (x, y, 0.1), (0, 0, tr.uniform(0, 6)), scale=(tr.uniform(0.4, 1.2),) * 3)
            n += 1
    # grass tuft mesh
    bm = bmesh.new()
    gr = random.Random(77)
    for i in range(9):
        res = bmesh.ops.create_cone(bm, cap_ends=False, segments=3, radius1=0.05, radius2=0.0, depth=gr.uniform(0.35, 0.6))
        m = Matrix.Translation((gr.uniform(-0.18, 0.18), gr.uniform(-0.18, 0.18), 0.2)) @ \
            Matrix.Rotation(gr.uniform(-0.4, 0.4), 4, 'X') @ Matrix.Rotation(gr.uniform(-0.4, 0.4), 4, 'Y')
        bmesh.ops.transform(bm, matrix=m, verts=res['verts'])
    tuft = to_mesh(bm, "GrassTuft")
    tuft.materials.append(M['blade'])
    tufts = coll("GrassTufts", C_NATURE)
    n = 0
    while n < 900:
        x, y = tr.uniform(-49, 49), tr.uniform(-49, 49)
        if free_spot(x, y, -1.5, 0.3):
            place("Tuft", tuft, tufts, (x, y, 0), (0, 0, tr.uniform(0, 6)), scale=(tr.uniform(0.7, 1.6),) * 3)
            n += 1
    flower_src = []
    for k, fm in enumerate(M['flowers']):
        bm = bmesh.new()
        fr = random.Random(200 + k)
        for i in range(7):
            res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.09)
            bmesh.ops.translate(bm, vec=(fr.uniform(-0.4, 0.4), fr.uniform(-0.4, 0.4), fr.uniform(0.15, 0.3)), verts=res['verts'])
        me = to_mesh(bm, "FlowerPatch%d" % k)
        me.materials.append(fm)
        flower_src.append(me)
    n = 0
    while n < 120:
        x, y = tr.uniform(-48, 48), tr.uniform(-48, 48)
        if free_spot(x, y, 0.0, 0.6):
            place("Flowers", tr.choice(flower_src), tufts, (x, y, 0), (0, 0, tr.uniform(0, 6)))
            place("FlowerStems", tuft, tufts, (x, y, 0), scale=(0.8, 0.8, 0.6))
            n += 1

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

def build_characters():
    build_player((0, -30, 0), 0.0)
    random.seed(4)
    build_slime((14.5, -15.5, 0), 1.0, "Slime_A")
    build_slime((12.0, -20.5, 0), 0.75, "Slime_B")
    build_slime((16.2, -21.0, 0), 0.6, "Slime_C")
    build_charger((14.0, 21.5, 0), math.radians(-60))
    for i, (x, y) in enumerate([(9, -13), (10.2, -14.2), (8.6, -15.2), (11.3, -12.6)]):
        cyl("Coin%d" % i, 0.32, 0.07, (x, y, 0.5), M['gold'], C_CHARS, rot=(math.pi / 2, 0, i * 0.7), base=False, segs=24, bevel=0.015)

# --------------------------------------------------------------------------- sky, light, cameras, render
def build_sky():
    w = bpy.data.worlds.new("AWEJ_Sky")
    SC.world = w
    if w.node_tree is None:
        w.use_nodes = True
    nt = w.node_tree
    bg = next(n for n in nt.nodes if n.type == 'BACKGROUND')
    sky = nt.nodes.new('ShaderNodeTexSky')
    items = [i.identifier for i in sky.bl_rna.properties['sky_type'].enum_items]
    for pick in ('MULTIPLE_SCATTERING', 'NISHITA', 'SINGLE_SCATTERING'):
        if pick in items:
            sky.sky_type = pick
            break
    for attr, val in (('sun_disc', False), ('sun_elevation', math.radians(38)), ('sun_rotation', math.radians(140)),
                      ('altitude', 200.0), ('air_density', 1.2), ('dust_density', 1.5)):
        try:
            setattr(sky, attr, val)
        except Exception:
            pass
    nt.links.new(sky.outputs['Color'], bg.inputs['Color'])
    bg.inputs['Strength'].default_value = 0.16

    sun = bpy.data.lights.new("Sun", 'SUN')
    sun.energy = 5.0
    sun.color = (1.0, 0.93, 0.82)
    sun.angle = math.radians(1.5)
    place("Sun", sun, C_SKY, (0, 0, 60), (math.radians(52), 0, math.radians(-50)))

    cr = random.Random(31)
    for i in range(9):
        cx, cy, cz = cr.uniform(-320, 320), cr.uniform(180, 480), cr.uniform(70, 130)
        g = empty("Cloud_%d" % i, C_SKY, (cx, cy, cz))
        for k in range(5):
            sphere("Puff", cr.uniform(9, 16), (cr.uniform(-20, 20), cr.uniform(-6, 6), cr.uniform(-3, 5)), M['cloud'], C_SKY, g,
                   scale=(1.4, 1.0, 0.7), subdiv=2, ico=True, jitter=0.08, seed=i * 10 + k)

def aim(ob, target):
    d = Vector(target) - ob.location
    ob.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

def build_cameras():
    def cam(name, loc, target, lens):
        cd = bpy.data.cameras.new(name)
        cd.lens = lens
        cd.clip_end = 3000
        ob = place(name, cd, C_SKY, loc)
        aim(ob, target)
        return ob
    hero = cam("Cam_Hero", (38, -96, 36), (-2, 4, 0), 30)
    cam("Cam_ThirdPerson", (0, -40.5, 5.2), (0, -22, 2.2), 32)
    cam("Cam_Gate", (-14, 22, 4.5), (0, 48, 4.5), 28)
    cam("Cam_TopDown", (0, -1, 150), (0, 0, 0), 40)
    SC.camera = hero

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

# --------------------------------------------------------------------------- build
build_terrain()
build_walls()
build_paths()
build_general_store()
build_tinker_workshop()
build_training_barn()
build_old_cellar()
build_props()
build_nature()
build_characters()
build_sky()
build_cameras()
render_settings()

result = {"scene": SC.name, "objects": len(SC.objects),
          "materials": len(bpy.data.materials), "camera": SC.camera.name}
