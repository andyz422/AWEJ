"""
Quality test: a realistic forest clearing with an original ruined stone tower, rendered in Cycles.
Fully procedural (no downloaded assets). Builds into an empty file and saves
Blender/TestScene_RuinedTower.blend.

Run headless:
    blender -b --factory-startup --python test_scene_ruined_tower.py -- [preview|final] [out.png]
"""
import bpy, bmesh, math, os, random, sys, addon_utils
from mathutils import Vector, Matrix, noise

HERE = os.path.dirname(os.path.abspath(__file__))
ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MODE = ARGS[0] if ARGS else "preview"
OUT = ARGS[1] if len(ARGS) > 1 else os.path.join(HERE, "renders", "TestScene_RuinedTower.png")

bpy.ops.wm.read_factory_settings(use_empty=True)
addon_utils.enable("cycles", default_set=True)
SC = bpy.context.scene
SC.name = "RuinedTower"

def coll(name):
    c = bpy.data.collections.new(name)
    SC.collection.children.link(c)
    return c

C_TERRAIN, C_TOWER, C_TREES, C_SRC, C_LIGHT = (coll(n) for n in ("Terrain", "Tower", "Forest", "ScatterSources", "Lighting"))
C_SRC.hide_render = True
C_SRC.hide_viewport = True

def obj(name, me, c, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    o = bpy.data.objects.new(name, me)
    c.objects.link(o)
    o.location, o.rotation_euler, o.scale = loc, rot, scale
    return o

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def fbm(x, y, octaves=5, seed=0.0):
    v, amp, f = 0.0, 1.0, 1.0
    for _ in range(octaves):
        v += noise.noise(Vector((x * f + seed, y * f - seed, seed * 0.37))) * amp
        amp *= 0.5
        f *= 2.03
    return v

# --------------------------------------------------------------------------- layout
TOWER = Vector((0.0, 10.0))
TOWER_R = 3.6
CAM = Vector((-6.0, -15.0))
CLEAR_C = Vector((0.0, 3.0))
DOOR_ANG = math.radians(-100)
COLLAPSE_ANG = math.radians(-40)
DOOR_PT = TOWER + Vector((math.cos(DOOR_ANG), math.sin(DOOR_ANG))) * (TOWER_R + 0.5)
PATH = [Vector((-5.0, -24.0)), Vector((-3.5, -10.0)), Vector((-1.4, 0.5)), DOOR_PT]

def height(x, y):
    r = (Vector((x, y)) - CLEAR_C).length
    rim = smoothstep(16, 45, r)
    h = fbm(x * 0.018, y * 0.018, 4, 3.1) * 5.0 * rim + rim * 2.0
    h += max(0.0, y - 22) * 0.12 * rim
    h += fbm(x * 0.09, y * 0.09, 3, 7.7) * 0.32
    h += fbm(x * 0.6, y * 0.6, 2, 1.3) * 0.04
    return h

def dist_to_path(p):
    best = 1e9
    for a, b in zip(PATH[:-1], PATH[1:]):
        ab = b - a
        t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
        best = min(best, (p - (a + ab * t)).length)
    return best

# --------------------------------------------------------------------------- materials
def mat(name):
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    return m, nt.nodes, nt.links, nt.nodes["Principled BSDF"], nt.nodes["Material Output"]

def sock(sockets, ident):
    for s in sockets:
        if s.identifier == ident:
            return s
    return sockets[ident]

def mix_rgb(N, L, a, b, fac, blend='MIX'):
    m = N.new('ShaderNodeMix'); m.data_type = 'RGBA'; m.blend_type = blend
    if isinstance(fac, float):
        sock(m.inputs, 'Factor_Float').default_value = fac
    else:
        L.new(fac, sock(m.inputs, 'Factor_Float'))
    for s, v in (('A_Color', a), ('B_Color', b)):
        if isinstance(v, tuple):
            sock(m.inputs, s).default_value = v + (1,) if len(v) == 3 else v
        else:
            L.new(v, sock(m.inputs, s))
    return sock(m.outputs, 'Result_Color')

def ramp(N, L, fac, stops, interp='LINEAR'):
    r = N.new('ShaderNodeValToRGB')
    r.color_ramp.interpolation = interp
    els = r.color_ramp.elements
    while len(els) < len(stops):
        els.new(0.5)
    for e, (p, c) in zip(els, stops):
        e.position = p
        e.color = c + (1,) if len(c) == 3 else c
    L.new(fac, r.inputs['Fac'])
    return r.outputs['Color']

def noise_tex(N, L, vec, scale, detail=6.0, rough=0.55, dist=0.0):
    n = N.new('ShaderNodeTexNoise')
    n.inputs['Scale'].default_value = scale
    n.inputs['Detail'].default_value = detail
    n.inputs['Roughness'].default_value = rough
    n.inputs['Distortion'].default_value = dist
    if vec is not None:
        L.new(vec, n.inputs['Vector'])
    return n

def math_node(N, L, op, a, b=None, c=None, clamp=False):
    m = N.new('ShaderNodeMath'); m.operation = op; m.use_clamp = clamp
    for i, v in enumerate((a, b, c)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            m.inputs[i].default_value = v
        else:
            L.new(v, m.inputs[i])
    return m.outputs[0]

def bump(N, L, height_sock, strength, dist=0.1, normal=None):
    b = N.new('ShaderNodeBump')
    b.inputs['Strength'].default_value = strength
    b.inputs['Distance'].default_value = dist
    L.new(height_sock, b.inputs['Height'])
    if normal is not None:
        L.new(normal, b.inputs['Normal'])
    return b.outputs['Normal']

def stone_material():
    m, N, L, B, O = mat("WeatheredStone")
    tc = N.new('ShaderNodeTexCoord')
    obj_co = tc.outputs['Object']
    attr = N.new('ShaderNodeAttribute'); attr.attribute_name = "stonerand"
    tint = ramp(N, L, attr.outputs['Fac'], [(0.0, (0.1, 0.095, 0.085)), (0.4, (0.16, 0.15, 0.13)),
                                           (0.75, (0.2, 0.17, 0.13)), (1.0, (0.13, 0.13, 0.135))])
    n1 = noise_tex(N, L, obj_co, 3.0, 8, 0.6)
    vor = N.new('ShaderNodeTexVoronoi'); vor.inputs['Scale'].default_value = 7.0
    L.new(obj_co, vor.inputs['Vector'])
    grain = ramp(N, L, n1.outputs['Fac'], [(0.25, (0.55, 0.55, 0.55)), (0.75, (1.25, 1.22, 1.18))])
    base = mix_rgb(N, L, tint, grain, 1.0, 'MULTIPLY')
    pits = ramp(N, L, vor.outputs['Distance'], [(0.0, (0.55, 0.55, 0.55)), (0.12, (1, 1, 1))])
    base = mix_rgb(N, L, base, pits, 1.0, 'MULTIPLY')
    # crevice dirt from ambient occlusion
    ao = N.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value = 0.35; ao.samples = 8
    dirt = ramp(N, L, ao.outputs['AO'], [(0.25, (0.22, 0.2, 0.17)), (0.85, (1, 1, 1))])
    base = mix_rgb(N, L, base, dirt, 1.0, 'MULTIPLY')
    # moss on upward faces and low down, broken up by noise
    geo = N.new('ShaderNodeNewGeometry')
    sep = N.new('ShaderNodeSeparateXYZ'); L.new(geo.outputs['Normal'], sep.inputs[0])
    up = math_node(N, L, 'MULTIPLY_ADD', sep.outputs['Z'], 1.6, -0.3, clamp=True)
    sepp = N.new('ShaderNodeSeparateXYZ'); L.new(geo.outputs['Position'], sepp.inputs[0])
    low = math_node(N, L, 'SUBTRACT', 1.0, math_node(N, L, 'MULTIPLY', sepp.outputs['Z'], 0.22), clamp=True)
    mn = noise_tex(N, L, obj_co, 1.6, 6, 0.65)
    mossy = math_node(N, L, 'MULTIPLY', math_node(N, L, 'ADD', up, low), mn.outputs['Fac'])
    mmask = ramp(N, L, mossy, [(0.38, (0, 0, 0)), (0.52, (1, 1, 1))])
    mcol = ramp(N, L, noise_tex(N, L, obj_co, 9.0).outputs['Fac'], [(0.3, (0.12, 0.2, 0.05)), (0.7, (0.25, 0.32, 0.08))])
    sepm = N.new('ShaderNodeSeparateColor'); L.new(mmask, sepm.inputs[0])
    final = mix_rgb(N, L, base, mcol, sepm.outputs['Red'])
    L.new(final, B.inputs['Base Color'])
    L.new(math_node(N, L, 'MULTIPLY_ADD', sepm.outputs['Red'], 0.1, 0.82), B.inputs['Roughness'])
    h = math_node(N, L, 'ADD', n1.outputs['Fac'], math_node(N, L, 'MULTIPLY', vor.outputs['Distance'], 0.8))
    L.new(bump(N, L, h, 1.0, 0.05), B.inputs['Normal'])
    return m

def ground_material():
    m, N, L, B, O = mat("ForestGround")
    tc = N.new('ShaderNodeTexCoord'); co = tc.outputs['Object']
    big = noise_tex(N, L, co, 0.08, 5, 0.6)
    small = noise_tex(N, L, co, 1.4, 8, 0.6)
    grass = ramp(N, L, big.outputs['Fac'], [(0.3, (0.07, 0.11, 0.025)), (0.55, (0.13, 0.16, 0.04)), (0.75, (0.2, 0.18, 0.07))])
    soil = ramp(N, L, small.outputs['Fac'], [(0.35, (0.09, 0.065, 0.04)), (0.65, (0.16, 0.12, 0.08))])
    litter = ramp(N, L, noise_tex(N, L, co, 4.0, 4).outputs['Fac'], [(0.4, (0.16, 0.1, 0.05)), (0.62, (0.28, 0.18, 0.08))])
    # ground cover vs bare soil
    cover = ramp(N, L, small.outputs['Fac'], [(0.42, (0, 0, 0)), (0.58, (1, 1, 1))])
    sepc = N.new('ShaderNodeSeparateColor'); L.new(cover, sepc.inputs[0])
    col = mix_rgb(N, L, soil, grass, sepc.outputs['Red'])
    # leaf litter under the forest (attribute 'forest'), dirt path (attribute 'path')
    fa = N.new('ShaderNodeAttribute'); fa.attribute_name = "forest"
    col = mix_rgb(N, L, col, litter, math_node(N, L, 'MULTIPLY', fa.outputs['Fac'], 0.75))
    pa = N.new('ShaderNodeAttribute'); pa.attribute_name = "path"
    path_col = mix_rgb(N, L, (0.17, 0.13, 0.09), (0.26, 0.21, 0.15), small.outputs['Fac'])
    col = mix_rgb(N, L, col, path_col, pa.outputs['Fac'])
    # rock on steep slopes
    geo = N.new('ShaderNodeNewGeometry')
    sep = N.new('ShaderNodeSeparateXYZ'); L.new(geo.outputs['Normal'], sep.inputs[0])
    steep = ramp(N, L, sep.outputs['Z'], [(0.78, (1, 1, 1)), (0.88, (0, 0, 0))])
    seps = N.new('ShaderNodeSeparateColor'); L.new(steep, seps.inputs[0])
    col = mix_rgb(N, L, col, (0.3, 0.29, 0.27), seps.outputs['Red'])
    L.new(col, B.inputs['Base Color'])
    B.inputs['Roughness'].default_value = 0.93
    L.new(bump(N, L, small.outputs['Fac'], 0.35, 0.05), B.inputs['Normal'])
    return m

def foliage_material(name, colors, translucency=0.35, attr=None, rough=0.5, use_h=True):
    m, N, L, B, O = mat(name)
    if attr:
        a = N.new('ShaderNodeAttribute'); a.attribute_name = attr
        fac = a.outputs['Fac']
    else:
        oi = N.new('ShaderNodeObjectInfo'); fac = oi.outputs['Random']
    col = ramp(N, L, fac, colors)
    if use_h:  # darker toward the base of blades and the inside of crowns
        h = N.new('ShaderNodeAttribute'); h.attribute_name = "h"
        shade = ramp(N, L, h.outputs['Fac'], [(0.0, (0.35, 0.35, 0.35)), (0.6, (1, 1, 1))])
        col = mix_rgb(N, L, col, shade, 1.0, 'MULTIPLY')
    L.new(col, B.inputs['Base Color'])
    B.inputs['Roughness'].default_value = rough
    tr = N.new('ShaderNodeBsdfTranslucent')
    L.new(mix_rgb(N, L, col, (1.0, 1.0, 0.6), 0.25), tr.inputs['Color'])
    mx = N.new('ShaderNodeMixShader'); mx.inputs[0].default_value = translucency
    L.new(B.outputs[0], mx.inputs[1]); L.new(tr.outputs[0], mx.inputs[2])
    L.new(mx.outputs[0], O.inputs['Surface'])
    return m

def bark_material():
    m, N, L, B, O = mat("Bark")
    tc = N.new('ShaderNodeTexCoord')
    mp = N.new('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (9, 9, 1.2)
    L.new(tc.outputs['Object'], mp.inputs['Vector'])
    n = noise_tex(N, L, mp.outputs['Vector'], 2.5, 10, 0.7, 1.5)
    col = ramp(N, L, n.outputs['Fac'], [(0.3, (0.07, 0.06, 0.05)), (0.6, (0.2, 0.18, 0.15)), (0.8, (0.3, 0.28, 0.24))])
    geo = N.new('ShaderNodeNewGeometry')
    sep = N.new('ShaderNodeSeparateXYZ'); L.new(geo.outputs['Position'], sep.inputs[0])
    moss = math_node(N, L, 'SUBTRACT', 1.0, math_node(N, L, 'MULTIPLY', sep.outputs['Z'], 0.5), clamp=True)
    mm = math_node(N, L, 'MULTIPLY', moss, noise_tex(N, L, tc.outputs['Object'], 3.0).outputs['Fac'])
    col = mix_rgb(N, L, col, (0.13, 0.2, 0.05), math_node(N, L, 'GREATER_THAN', mm, 0.3))
    L.new(col, B.inputs['Base Color'])
    B.inputs['Roughness'].default_value = 0.9
    L.new(bump(N, L, n.outputs['Fac'], 0.9, 0.06), B.inputs['Normal'])
    return m

def wood_material():
    m, N, L, B, O = mat("OldTimber")
    tc = N.new('ShaderNodeTexCoord')
    mp = N.new('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (0.4, 0.4, 14)
    L.new(tc.outputs['Object'], mp.inputs['Vector'])
    n = noise_tex(N, L, mp.outputs['Vector'], 3.0, 8, 0.6)
    L.new(ramp(N, L, n.outputs['Fac'], [(0.3, (0.08, 0.06, 0.045)), (0.7, (0.2, 0.16, 0.11))]), B.inputs['Base Color'])
    B.inputs['Roughness'].default_value = 0.85
    L.new(bump(N, L, n.outputs['Fac'], 0.5, 0.03), B.inputs['Normal'])
    return m

M_STONE = stone_material()
M_GROUND = ground_material()
M_GRASS = foliage_material("Grass", [(0.0, (0.06, 0.13, 0.02)), (0.5, (0.13, 0.2, 0.03)), (0.8, (0.2, 0.22, 0.05)),
                                     (1.0, (0.33, 0.28, 0.1))], 0.35)
M_LEAF = foliage_material("Leaves", [(0.0, (0.05, 0.11, 0.02)), (0.5, (0.1, 0.17, 0.03)), (0.85, (0.18, 0.22, 0.04)),
                                     (1.0, (0.3, 0.25, 0.06))], 0.4, attr="leafrand", rough=0.42)
M_IVY = foliage_material("Ivy", [(0.0, (0.03, 0.08, 0.02)), (1.0, (0.08, 0.14, 0.03))], 0.25, rough=0.35, use_h=False)
M_FERN = foliage_material("Fern", [(0.0, (0.07, 0.15, 0.025)), (1.0, (0.15, 0.22, 0.04))], 0.4)
M_BARK = bark_material()
M_WOOD = wood_material()
M_FLOWER = [bpy.data.materials.new(n) for n in ("PetalWhite", "PetalYellow", "PetalViolet")]
for fm, c in zip(M_FLOWER, ((0.8, 0.8, 0.75), (0.85, 0.65, 0.1), (0.35, 0.25, 0.6))):
    if fm.node_tree is None:
        fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs['Base Color'].default_value = c + (1,)
    fm.node_tree.nodes["Principled BSDF"].inputs['Roughness'].default_value = 0.5

# --------------------------------------------------------------------------- terrain
def build_terrain():
    size, segs = 180.0, 300
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=segs, y_segments=segs, size=size / 2)
    lay = {n: bm.verts.layers.float.new(n) for n in ("grassdens", "ferndens", "rockdens", "path", "forest")}
    for v in bm.verts:
        x, y = v.co.x, v.co.y
        v.co.z = height(x, y)
        p = Vector((x, y))
        r = (p - CLEAR_C).length
        dcam = (p - CAM).length
        dp = dist_to_path(p)
        dt = (p - TOWER).length
        forest = smoothstep(17, 26, r)
        path = 1.0 - smoothstep(0.55, 1.35, dp + fbm(x * 0.5, y * 0.5, 2) * 0.35)
        near = 1.0 - smoothstep(50, 68, dcam)
        tower_clear = smoothstep(TOWER_R + 0.2, TOWER_R + 1.4, dt)
        ca = math.atan2(y - TOWER.y, x - TOWER.x) - COLLAPSE_ANG
        rubble_zone = math.exp(-(math.atan2(math.sin(ca), math.cos(ca)) / 0.7) ** 2) * (1 - smoothstep(TOWER_R + 3, TOWER_R + 8, dt))
        tower_clear *= 1 - 0.75 * rubble_zone
        patchy = 0.55 + 0.45 * smoothstep(-0.3, 0.3, fbm(x * 0.15, y * 0.15, 3, 9.0))
        v[lay["grassdens"]] = near * (1 - path) * tower_clear * (1 - 0.7 * forest) * patchy
        v[lay["ferndens"]] = near * (1 - path) * tower_clear * (smoothstep(14, 22, r) + smoothstep(TOWER_R + 2.5, TOWER_R + 0.8, dt) * 0.8)
        v[lay["rockdens"]] = near * tower_clear * (0.08 + 0.9 * (1 - smoothstep(TOWER_R + 1, TOWER_R + 9, dt)))
        v[lay["path"]] = path
        v[lay["forest"]] = forest
    me = bpy.data.meshes.new("Ground")
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(M_GROUND)
    return obj("Ground", me, C_TERRAIN)

# --------------------------------------------------------------------------- scatter (geometry nodes)
def live(sockets, name):
    """The socket called `name` that is in use for the node's current data type."""
    for s in sockets:
        if s.name == name and getattr(s, 'enabled', True) and not getattr(s, 'is_unavailable', False):
            return s
    raise KeyError(name)

def random_value(N, data_type, lo, hi, seed):
    rv = N.new('FunctionNodeRandomValue')
    rv.data_type = data_type
    live(rv.inputs, 'Min').default_value = lo
    live(rv.inputs, 'Max').default_value = hi
    live(rv.inputs, 'Seed').default_value = seed
    return live(rv.outputs, 'Value')

def scatter_group(name, density, attr, collection, smin, smax, seed, align=False, tilt=0.15):
    ng = bpy.data.node_groups.new(name, 'GeometryNodeTree')
    ng.interface.new_socket(name="Geometry", in_out='INPUT', socket_type='NodeSocketGeometry')
    ng.interface.new_socket(name="Geometry", in_out='OUTPUT', socket_type='NodeSocketGeometry')
    N, L = ng.nodes, ng.links
    gi, go = N.new('NodeGroupInput'), N.new('NodeGroupOutput')
    dist = N.new('GeometryNodeDistributePointsOnFaces')
    dist.distribute_method = 'RANDOM'
    dist.inputs['Seed'].default_value = seed
    na = N.new('GeometryNodeInputNamedAttribute'); na.data_type = 'FLOAT'; na.inputs['Name'].default_value = attr
    L.new(gi.outputs[0], dist.inputs['Mesh'])
    # Random mode takes a density field (per point): base density x the attribute mask.
    dm = N.new('ShaderNodeMath'); dm.operation = 'MULTIPLY'
    L.new(na.outputs[0], dm.inputs[0])
    dm.inputs[1].default_value = density
    L.new(dm.outputs[0], dist.inputs['Density'])
    ci = N.new('GeometryNodeCollectionInfo')
    ci.inputs['Collection'].default_value = collection
    ci.inputs['Separate Children'].default_value = True
    ci.inputs['Reset Children'].default_value = True
    iop = N.new('GeometryNodeInstanceOnPoints')
    L.new(dist.outputs['Points'], iop.inputs['Points'])
    L.new(ci.outputs[0], iop.inputs['Instance'])
    iop.inputs['Pick Instance'].default_value = True
    ri = random_value(N, 'INT', 0, 999, seed + 1)
    L.new(ri, iop.inputs['Instance Index'])
    rr = random_value(N, 'FLOAT_VECTOR', (-tilt, -tilt, 0.0), (tilt, tilt, 6.283), seed + 2)
    er = N.new('FunctionNodeEulerToRotation')
    L.new(rr, er.inputs[0])
    if align:
        rot = N.new('FunctionNodeRotateRotation')
        L.new(dist.outputs['Rotation'], rot.inputs['Rotation'])
        L.new(er.outputs[0], rot.inputs['Rotate By'])
        L.new(rot.outputs[0], iop.inputs['Rotation'])
    else:
        L.new(er.outputs[0], iop.inputs['Rotation'])
    rs = random_value(N, 'FLOAT', smin, smax, seed + 3)
    L.new(rs, iop.inputs['Scale'])
    j = N.new('GeometryNodeJoinGeometry')
    L.new(iop.outputs['Instances'], j.inputs[0])
    L.new(gi.outputs[0], j.inputs[0])
    L.new(j.outputs[0], go.inputs[0])
    return ng

def add_scatter(target, ng):
    md = target.modifiers.new(ng.name, 'NODES')
    md.node_group = ng
    return md

def grass_clump(name, rng, blades=18, hmin=0.25, hmax=0.6, spread=0.09, dry=False):
    bm = bmesh.new()
    hl = bm.verts.layers.float.new("h")
    for _ in range(blades):
        base = Vector((rng.gauss(0, spread), rng.gauss(0, spread), 0))
        h = rng.uniform(hmin, hmax)
        ang = rng.uniform(0, 6.283)
        lean = Vector((math.cos(ang), math.sin(ang), 0)) * rng.uniform(0.05, 0.35)
        side = Vector((-math.sin(ang + 1.2), math.cos(ang + 1.2), 0))
        w = rng.uniform(0.006, 0.012)
        prev = None
        segs = 4
        for i in range(segs + 1):
            t = i / segs
            p = base + Vector((0, 0, h * t)) + lean * h * t * t
            ww = w * (1 - t * 0.92)
            a = bm.verts.new(p - side * ww); b = bm.verts.new(p + side * ww)
            a[hl] = b[hl] = t
            if prev:
                bm.faces.new((prev[0], prev[1], b, a))
            prev = (a, b)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(M_GRASS)
    return obj(name, me, C_SRC)

def flower_clump(name, rng, petal_mat):
    o = grass_clump(name, rng, blades=7, hmin=0.15, hmax=0.35)
    bm = bmesh.new(); bm.from_mesh(o.data)
    for _ in range(rng.randint(2, 4)):
        c = Vector((rng.gauss(0, 0.06), rng.gauss(0, 0.06), rng.uniform(0.3, 0.45)))
        res = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.025)
        for v in res['verts']:
            v.co = Vector((v.co.x * 1.6, v.co.y * 1.6, v.co.z * 0.6)) + c
        for f in set(f for v in res['verts'] for f in v.link_faces):
            f.material_index = 1
    bm.to_mesh(o.data); bm.free()
    o.data.materials.append(petal_mat)
    return o

def fern(name, rng):
    bm = bmesh.new()
    hl = bm.verts.layers.float.new("h")
    for k in range(rng.randint(6, 9)):
        az = k * 6.283 / 8 + rng.uniform(-0.3, 0.3)
        L_ = rng.uniform(0.5, 0.85)
        d = Vector((math.cos(az), math.sin(az), 0))
        side = Vector((-d.y, d.x, 0))
        prev_pts = []
        n = 10
        for i in range(n + 1):
            t = i / n
            p = d * L_ * t * 0.85 + Vector((0, 0, L_ * (0.7 * t - 0.75 * t * t)))
            prev_pts.append(p)
        for i in range(1, n):
            t = i / n
            lw = 0.13 * math.sin(math.pi * t) + 0.02
            for s in (-1, 1):
                a = bm.verts.new(prev_pts[i]); b = bm.verts.new(prev_pts[i + 1])
                c = bm.verts.new(prev_pts[i] + side * s * lw - Vector((0, 0, lw * 0.25)) + d * lw * 0.3)
                a[hl] = b[hl] = c[hl] = 0.4 + 0.6 * t
                bm.faces.new((a, b, c))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(M_FERN)
    return obj(name, me, C_SRC)

def rock_mesh(name, rng, r=0.4, flat=0.55, detail=4):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=r)
    seed = rng.uniform(0, 100)
    sl = bm.faces.layers.float.new("stonerand")
    for v in bm.verts:
        n = v.co.normalized()
        d = noise.fractal(n * 1.8 + Vector((seed, 0, 0)), 0.6, 2.0, 5) * 0.28 + noise.noise(n * 6 + Vector((0, seed, 0))) * 0.05
        v.co = n * r * (1 + d)
        v.co.z *= flat
        if v.co.z < -r * 0.15:
            v.co.z = -r * 0.15 + (v.co.z + r * 0.15) * 0.2
    val = rng.random()
    for f in bm.faces:
        f[sl] = val
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(M_STONE)
    return me

# --------------------------------------------------------------------------- trees
def tube(bm, pts, radii, sides):
    rings = []
    u = None
    n = len(pts)
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        if u is None:
            ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
            u = t.cross(ref).normalized()
        u = (u - t * t.dot(u)).normalized()
        v = t.cross(u)
        rings.append([bm.verts.new(p + (u * math.cos(a) + v * math.sin(a)) * radii[i])
                      for a in [k * 2 * math.pi / sides for k in range(sides)]])
    for a, b in zip(rings[:-1], rings[1:]):
        for k in range(sides):
            bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]))

def rand_unit(rng):
    v = Vector((rng.gauss(0, 1), rng.gauss(0, 1), rng.gauss(0, 1)))
    return v.normalized()

def make_tree(name, seed, height_m=14.0, bush=False):
    rng = random.Random(seed)
    wood = bmesh.new()
    leaves = bmesh.new()
    lr = leaves.faces.layers.float.new("leafrand")
    lh = leaves.verts.layers.float.new("h")
    max_level = 2 if bush else 3

    def add_leaf(c, n):
        a = (n.cross(rand_unit(rng))).normalized()
        b = n.cross(a)
        s = rng.uniform(0.16, 0.28) * (1.2 if bush else 1.0)
        v0 = leaves.verts.new(c - a * s * 0.5)
        v1 = leaves.verts.new(c + b * s * 0.32 + n * s * 0.08)
        v2 = leaves.verts.new(c + a * s * 0.55)
        v3 = leaves.verts.new(c - b * s * 0.32 + n * s * 0.08)
        val = rng.random()
        for v in (v0, v1, v2, v3):
            v[lh] = 0.55 + 0.45 * min(1.0, c.z / (height_m * 0.9))
        for f in (leaves.faces.new((v0, v1, v2)), leaves.faces.new((v0, v2, v3))):
            f[lr] = val

    def grow(start, d, length, radius, level):
        nseg = max(3, min(14, int(length / 0.4)))
        pts = [start]
        for i in range(nseg):
            d = (d + rand_unit(rng) * (0.12 + 0.06 * level) + Vector((0, 0, 0.05 if level < 2 else -0.03))).normalized()
            pts.append(pts[-1] + d * length / nseg)
        radii = [max(0.01, radius * (1 - 0.8 * i / nseg)) for i in range(nseg + 1)]
        if level == 0:
            radii[0] *= 1.6
            radii[1] *= 1.2
        tube(wood, pts, radii, max(4, 10 - 2 * level))
        if level < max_level:
            kids = {0: rng.randint(5, 7), 1: rng.randint(3, 5), 2: rng.randint(3, 4)}[level]
            for k in range(kids):
                t = rng.uniform(0.45 if level == 0 else 0.25, 0.95)
                idx = min(nseg - 1, int(t * nseg))
                pd = (pts[idx + 1] - pts[idx]).normalized()
                perp = pd.cross(rand_unit(rng)).normalized()
                ang = math.radians(rng.uniform(30, 60) if level else rng.uniform(35, 65))
                cd = (pd * math.cos(ang) + perp * math.sin(ang) + Vector((0, 0, 0.25 if level == 0 else 0.05))).normalized()
                grow(pts[idx], cd, length * rng.uniform(0.5, 0.75) * (1.15 if level == 0 else 1.0), radii[idx] * 0.6, level + 1)
        if level >= max_level - 1:
            count = 90 if level == max_level else 24
            for i in range(count):
                tt = rng.uniform(0.35, 1.0)
                idx = min(nseg, int(tt * nseg))
                c = pts[idx] + rand_unit(rng) * rng.uniform(0.05, 0.9)
                n = (rand_unit(rng) + Vector((0, 0, 0.8))).normalized()
                add_leaf(c, n)

    trunk_len = height_m * (0.3 if bush else 0.5)
    grow(Vector((0, 0, -0.3)), (Vector((0, 0, 1)) + rand_unit(rng) * 0.08).normalized(), trunk_len,
         height_m * (0.012 if bush else 0.024), 0)
    me_w = bpy.data.meshes.new(name + "_Wood")
    wood.to_mesh(me_w); wood.free()
    for p in me_w.polygons:
        p.use_smooth = True
    me_w.materials.append(M_BARK)
    me_l = bpy.data.meshes.new(name + "_Leaves")
    leaves.to_mesh(me_l); leaves.free()
    me_l.materials.append(M_LEAF)
    # one object per tree: join wood + leaves
    bm = bmesh.new()
    bm.from_mesh(me_w)
    n_wood = len(bm.faces)
    bm.from_mesh(me_l)
    bm.faces.ensure_lookup_table()
    for f in bm.faces[n_wood:]:
        f.material_index = 1
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(M_BARK); me.materials.append(M_LEAF)
    bpy.data.meshes.remove(me_w); bpy.data.meshes.remove(me_l)
    return me

# --------------------------------------------------------------------------- the ruined tower
def tower_top(a):
    """Broken height of the wall at angle a."""
    base = 11.2 + fbm(math.cos(a) * 1.5, math.sin(a) * 1.5, 3, 4.2) * 1.4
    collapse = math.exp(-((math.atan2(math.sin(a - COLLAPSE_ANG), math.cos(a - COLLAPSE_ANG))) / 0.6) ** 2)
    return base - collapse * 6.5

def stone_block(bm, sl, center, tangent, radial, size, rng):
    w, h, d = size
    up = Vector((0, 0, 1))
    res = bmesh.ops.create_cube(bm, size=1.0)
    verts = res['verts']
    rot = Matrix((tangent, radial, up)).transposed()
    jitter = Matrix.Rotation(rng.uniform(-0.03, 0.03), 3, 'Z') @ Matrix.Rotation(rng.uniform(-0.025, 0.025), 3, 'X')
    for v in verts:
        local = Vector((v.co.x * w, v.co.y * d, v.co.z * h))
        local += local.normalized() * 0.0 + Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.015
        v.co = center + rot @ (jitter @ local)
    edges = list({e for v in verts for e in v.link_edges})
    bmesh.ops.bevel(bm, geom=verts + edges, offset=min(w, h, d) * 0.12, segments=2, affect='EDGES', profile=0.6)
    faces = list({f for v in bm.verts[-200:] for f in v.link_faces}) if False else None
    val = rng.random()
    return val

def roughen(bm, amp, scale):
    """Chip and weather the stones: push every vertex along its normal by layered noise."""
    bm.normal_update()
    for v in bm.verts:
        p = v.co * scale
        d = noise.fractal(p, 0.6, 2.1, 4) * amp + noise.noise(p * 4.0) * amp * 0.35
        v.co += v.normal * d

SLITS = ((math.radians(-70), 5.2), (math.radians(140), 5.6), (math.radians(-150), 8.4), (math.radians(60), 8.0))

def opening(a, rel):
    """True inside the arched doorway or an arrow slit (angle a, height rel above the base)."""
    s = math.atan2(math.sin(a - DOOR_ANG), math.cos(a - DOOR_ANG)) * TOWER_R
    if abs(s) < 0.75 and rel < 2.3 + math.sqrt(max(0.0, 0.75 ** 2 - s * s)) * 0.9:
        return True
    for sa, sz in SLITS:
        ss = math.atan2(math.sin(a - sa), math.cos(a - sa)) * TOWER_R
        if abs(ss) < 0.18 and sz - 0.8 < rel < sz + 0.8:
            return True
    return False

def build_mortar_core(base_z):
    """Lime mortar between the stones: a recessed shell inside the wall, cut to the broken top and openings."""
    m, N, L, B, O = mat("Mortar")
    tc = N.new('ShaderNodeTexCoord')
    n = noise_tex(N, L, tc.outputs['Object'], 6.0, 6)
    L.new(ramp(N, L, n.outputs['Fac'], [(0.3, (0.14, 0.13, 0.11)), (0.7, (0.24, 0.22, 0.19))]), B.inputs['Base Color'])
    B.inputs['Roughness'].default_value = 0.97
    L.new(bump(N, L, n.outputs['Fac'], 0.6, 0.02), B.inputs['Normal'])
    bm = bmesh.new()
    na, dz = 120, 0.25
    for r in (TOWER_R - 0.12, TOWER_R - 0.78):
        for i in range(na):
            a0, a1 = i * 2 * math.pi / na, (i + 1) * 2 * math.pi / na
            am = (a0 + a1) / 2
            top = tower_top(am) - 0.45
            z = 0.0
            while z + dz <= top:
                if not opening(am, z + dz / 2):
                    q = [Vector((TOWER.x + math.cos(a) * r, TOWER.y + math.sin(a) * r, base_z + zz))
                         for a, zz in ((a0, z), (a1, z), (a1, z + dz), (a0, z + dz))]
                    bm.faces.new([bm.verts.new(p) for p in q])
                z += dz
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.001)
    me = bpy.data.meshes.new("MortarCore")
    bm.to_mesh(me); bm.free()
    me.materials.append(m)
    obj("MortarCore", me, C_TOWER)

def build_tower(base_z):
    rng = random.Random(77)
    bm = bmesh.new()
    sl = bm.faces.layers.float.new("stonerand")
    T = TOWER.to_3d()
    z = base_z
    course = 0
    stones = 0
    rubble_sources = []
    while z < 13.5:
        h = rng.uniform(0.32, 0.48)
        a = rng.uniform(0, 0.4) + course * 0.37
        circ = 2 * math.pi * TOWER_R
        end = a + 2 * math.pi
        while a < end:
            w = rng.uniform(0.5, 0.95)
            da = w / TOWER_R
            mid = a + da / 2
            zc = z + h / 2
            rel = zc - base_z
            top = tower_top(mid)
            skip = False
            # broken, ragged top: blocks thin out near the break line
            if rel > top or (rel > top - 1.4 and rng.random() < (rel - top + 1.4) / 2.2):
                skip = True
                if rel < top + 1.5 and rng.random() < 0.18:
                    rubble_sources.append((w, h))
            if opening(mid, rel):
                skip = True
            # occasional missing stones
            if rel > 2 and rng.random() < 0.025:
                skip = True
            if not skip:
                tangent = Vector((-math.sin(mid), math.cos(mid), 0))
                radial = Vector((math.cos(mid), math.sin(mid), 0))
                inset = rng.uniform(-0.09, 0.07) + (0.08 if rel > top - 2 else 0)
                center = T + radial * (TOWER_R - 0.45 + inset) + Vector((0, 0, zc))
                n_before = len(bm.faces)
                stone_block(bm, sl, center, tangent, radial, (w - 0.035, h - 0.03, rng.uniform(0.82, 0.95)), rng)
                val = rng.random()
                bm.faces.ensure_lookup_table()
                for f in bm.faces[n_before:]:
                    f[sl] = val
                stones += 1
            a += da
        z += h
        course += 1
    # plinth course of large stones
    for k in range(26):
        mid = k * 2 * math.pi / 26
        tangent = Vector((-math.sin(mid), math.cos(mid), 0))
        radial = Vector((math.cos(mid), math.sin(mid), 0))
        s = math.atan2(math.sin(mid - DOOR_ANG), math.cos(mid - DOOR_ANG)) * TOWER_R
        if abs(s) < 0.9:
            continue
        n_before = len(bm.faces)
        stone_block(bm, sl, T + radial * (TOWER_R - 0.2) + Vector((0, 0, base_z + 0.25)), tangent, radial,
                    (0.95, 0.6, 1.25), rng)
        bm.faces.ensure_lookup_table()
        val = rng.random()
        for f in bm.faces[n_before:]:
            f[sl] = val
    roughen(bm, 0.05, 3.2)
    me = bpy.data.meshes.new("TowerStones")
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(M_STONE)
    tower = obj("RuinedTower", me, C_TOWER)
    # ivy density on the wall: patches, favouring the lower half and the shaded side
    ivy = me.attributes.new("ivydens", 'FLOAT', 'POINT')
    vals = []
    for v in me.vertices:
        p = v.co
        ang = math.atan2(p.y - TOWER.y, p.x - TOWER.x)
        patch = smoothstep(0.05, 0.35, fbm(math.cos(ang) * 2.2, (p.z - base_z) * 0.35, 3, 11.0))
        lowbias = 1 - smoothstep(4, 10, p.z - base_z)
        outward = 1.0 if (Vector((p.x, p.y)) - TOWER).length > TOWER_R - 0.2 else 0.0
        vals.append(patch * (0.35 + 0.65 * lowbias) * outward)
    ivy.data.foreach_set("value", vals)
    return tower, stones, rubble_sources

def build_rubble(base_z, sources):
    rng = random.Random(5)
    bm = bmesh.new()
    sl = bm.faces.layers.float.new("stonerand")
    collapse_a = COLLAPSE_ANG
    count = 0
    for i in range(170):
        a = collapse_a + rng.gauss(0, 0.55)
        r = TOWER_R + abs(rng.gauss(0.8, 2.6))
        p = TOWER + Vector((math.cos(a), math.sin(a))) * r
        if (p - DOOR_PT).length < 1.6 or dist_to_path(p) < 0.9:
            continue
        w, h = sources[i % len(sources)] if sources else (0.7, 0.4)
        z = height(p.x, p.y) + h * 0.25
        tangent = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.4, 0.4))).normalized()
        up = Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), 1)).normalized()
        radial = up.cross(tangent).normalized()
        tangent = radial.cross(up).normalized()
        n_before = len(bm.faces)
        res_val = stone_block(bm, sl, Vector((p.x, p.y, z)), tangent, radial, (w * rng.uniform(0.6, 1.0), h, rng.uniform(0.5, 0.9)), rng)
        # tilt the block by rotating its verts around its center
        bm.faces.ensure_lookup_table()
        val = rng.random()
        for f in bm.faces[n_before:]:
            f[sl] = val
        count += 1
    roughen(bm, 0.06, 3.2)
    me = bpy.data.meshes.new("Rubble")
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(M_STONE)
    obj("Rubble", me, C_TOWER)
    return count

def build_beams(base_z):
    rng = random.Random(9)
    for i, (a, z, L, tilt) in enumerate([(math.radians(-25), 7.0, 3.4, -0.25), (math.radians(-170), 6.9, 2.6, 0.1),
                                          (math.radians(-55), 0.2, 4.2, 0.08)]):
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector((v.co.x * L, v.co.y * 0.24, v.co.z * 0.22))
            if v.co.x > 0:  # splintered end
                v.co.y += rng.uniform(-0.05, 0.05); v.co.z += rng.uniform(-0.06, 0.04)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.02, segments=1, affect='EDGES')
        me = bpy.data.meshes.new("Beam%d" % i)
        bm.to_mesh(me); bm.free()
        me.materials.append(M_WOOD)
        if i < 2:
            loc = TOWER.to_3d() + Vector((math.cos(a), math.sin(a), 0)) * (TOWER_R - 1.0) + Vector((0, 0, base_z + z))
            obj("Beam%d" % i, me, C_TOWER, loc, (0, tilt, a))
        else:  # fallen beam on the ground outside the collapse
            p = TOWER + Vector((math.cos(a), math.sin(a))) * (TOWER_R + 3.2)
            obj("FallenBeam", me, C_TOWER, (p.x, p.y, height(p.x, p.y) + 0.12), (0.06, tilt, a + 1.2))

# --------------------------------------------------------------------------- forest placement
def place_forest(trees, bushes):
    rng = random.Random(21)
    placed = []
    view_dir = (TOWER - CAM).normalized()
    def ok(p, spacing):
        if (p - TOWER).length < TOWER_R + 6 or dist_to_path(p) < 2.5 or (p - CAM).length < 5:
            return False
        return all((p - q).length > spacing for q in placed)
    # framing tree, left foreground
    hero = [(Vector((-12.5, -5.0)), 0, 17.0), (Vector((9.5, 19.0)), 1, 16.0), (Vector((-9.0, 21.0)), 2, 15.0)]
    for p, k, s in hero:
        o = obj("HeroTree", trees[k], C_TREES, (p.x, p.y, height(p.x, p.y)), (0, 0, rng.uniform(0, 6.28)), (s / 14,) * 3)
        placed.append(p)
    tries = 0
    while len(placed) < 230 and tries < 20000:
        tries += 1
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(19, 85)
        p = CLEAR_C + Vector((math.cos(a), math.sin(a))) * r
        to_p = (p - CAM)
        if to_p.length > 15 and to_p.normalized().dot(view_dir) < -0.2:
            continue  # nothing far behind the camera
        if not ok(p, 4.2 if r < 40 else 5.5):
            continue
        s = rng.uniform(0.85, 1.3)
        obj("Tree", trees[rng.randrange(len(trees))], C_TREES, (p.x, p.y, height(p.x, p.y) - 0.1),
            (rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03), rng.uniform(0, 6.28)), (s,) * 3)
        placed.append(p)
    n_trees = len(placed)
    for i in range(140):
        a = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(15, 40)
        p = CLEAR_C + Vector((math.cos(a), math.sin(a))) * r
        if (p - CAM).length < 6 or dist_to_path(p) < 2 or (p - TOWER).length < TOWER_R + 3:
            continue
        s = rng.uniform(0.7, 1.3)
        obj("Bush", bushes[rng.randrange(len(bushes))], C_TREES, (p.x, p.y, height(p.x, p.y)), (0, 0, rng.uniform(0, 6.28)), (s,) * 3)
    return n_trees

# --------------------------------------------------------------------------- lighting, camera, render
def build_lighting():
    w = bpy.data.worlds.new("GoldenHourSky")
    SC.world = w
    if w.node_tree is None:
        w.use_nodes = True
    N, L = w.node_tree.nodes, w.node_tree.links
    bg = N["Background"]
    sky = N.new('ShaderNodeTexSky')
    items = [i.identifier for i in sky.bl_rna.properties['sky_type'].enum_items]
    for pick in ('MULTIPLE_SCATTERING', 'NISHITA', 'SINGLE_SCATTERING'):
        if pick in items:
            sky.sky_type = pick
            break
    for attr, val in (('sun_elevation', math.radians(9)), ('sun_rotation', math.radians(60)), ('sun_disc', True),
                      ('sun_size', math.radians(1.2)), ('air_density', 1.4), ('dust_density', 3.5), ('altitude', 50.0)):
        try:
            setattr(sky, attr, val)
        except Exception:
            pass
    L.new(sky.outputs['Color'], bg.inputs['Color'])
    bg.inputs['Strength'].default_value = 0.42
    sun = bpy.data.lights.new("Sun", 'SUN')
    sun.energy = 4.5
    sun.color = (1.0, 0.72, 0.45)
    sun.angle = math.radians(1.2)
    obj("Sun", sun, C_LIGHT, (0, 0, 50), (math.radians(81), 0, math.radians(150)))
    # mist for light shafts
    m, Nn, Ll, B, O = mat("Mist")
    Nn.remove(B)
    vol = Nn.new('ShaderNodeVolumePrincipled')
    vol.inputs['Density'].default_value = 0.009
    vol.inputs['Anisotropy'].default_value = 0.55
    vol.inputs['Color'].default_value = (0.95, 0.9, 0.85, 1)
    Ll.new(vol.outputs[0], O.inputs['Volume'])
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    me = bpy.data.meshes.new("MistVolume")
    bm.to_mesh(me); bm.free()
    me.materials.append(m)
    o = obj("MistVolume", me, C_LIGHT, (0, 20, 10), (0, 0, 0), (200, 200, 24))
    o.display_type = 'BOUNDS'

def build_camera():
    cd = bpy.data.cameras.new("Camera")
    cd.lens = 32
    cd.clip_end = 1000
    cd.dof.use_dof = True
    cd.dof.aperture_fstop = 5.6
    cam = obj("Camera", cd, C_LIGHT, (CAM.x, CAM.y, height(CAM.x, CAM.y) + 1.65))
    target = Vector((TOWER.x - 2.6, TOWER.y, height(TOWER.x, TOWER.y) + 5.2))
    d = target - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    cd.dof.focus_distance = (TOWER.to_3d() - cam.location).length - 2
    SC.camera = cam

def render_settings(final):
    SC.render.engine = 'CYCLES'
    prefs = bpy.context.preferences.addons['cycles'].preferences
    for t in ('OPTIX', 'CUDA'):
        try:
            prefs.compute_device_type = t
            prefs.get_devices()
            for d in prefs.devices:
                d.use = d.type == t
            break
        except Exception:
            continue
    cy = SC.cycles
    cy.device = 'GPU'
    cy.samples = 128 if final else 32
    cy.use_adaptive_sampling = True
    cy.adaptive_threshold = 0.02 if final else 0.08
    cy.use_denoising = True
    try:
        cy.denoiser = 'OPENIMAGEDENOISE'
    except Exception:
        pass
    cy.max_bounces = 6
    cy.diffuse_bounces = 3
    cy.glossy_bounces = 2
    cy.transmission_bounces = 4
    cy.volume_bounces = 0
    cy.volume_step_rate = 4.0 if final else 8.0
    cy.caustics_reflective = cy.caustics_refractive = False
    SC.render.resolution_x, SC.render.resolution_y = (1600, 900) if final else (960, 540)
    SC.render.resolution_percentage = 100
    SC.render.filepath = OUT
    vs = SC.view_settings
    vs.view_transform = 'AgX'
    for look in ('AgX - Medium High Contrast', 'Medium High Contrast'):
        try:
            vs.look = look
            break
        except Exception:
            pass
    vs.exposure = -0.15

# --------------------------------------------------------------------------- build
ground = build_terrain()
grng = random.Random(3)
grass_c = bpy.data.collections.new("GrassSources"); C_SRC.children.link(grass_c)
for i in range(8):
    o = grass_clump("GrassClump%d" % i, grng, blades=grng.randint(16, 28), hmax=grng.uniform(0.5, 0.85))
    C_SRC.objects.unlink(o); grass_c.objects.link(o)
flower_c = bpy.data.collections.new("FlowerSources"); C_SRC.children.link(flower_c)
for i in range(3):
    o = flower_clump("Flowers%d" % i, grng, M_FLOWER[i])
    C_SRC.objects.unlink(o); flower_c.objects.link(o)
fern_c = bpy.data.collections.new("FernSources"); C_SRC.children.link(fern_c)
for i in range(3):
    o = fern("Fern%d" % i, grng)
    C_SRC.objects.unlink(o); fern_c.objects.link(o)
rock_c = bpy.data.collections.new("RockSources"); C_SRC.children.link(rock_c)
for i in range(5):
    rock_c.objects.link(bpy.data.objects.new("Rock%d" % i, rock_mesh("RockMesh%d" % i, grng, r=0.25, detail=3)))
ivy_c = bpy.data.collections.new("IvySources"); C_SRC.children.link(ivy_c)
for i in range(3):
    bm = bmesh.new()
    for k in range(6):
        c = Vector((grng.uniform(-0.15, 0.15), grng.uniform(-0.15, 0.15), grng.uniform(0, 0.05)))
        s = grng.uniform(0.05, 0.08)
        v = [bm.verts.new(c + Vector(p) * s) for p in ((0, -1, 0), (0.9, 0.1, 0.15), (0, 1.1, 0), (-0.9, 0.1, 0.15))]
        bm.faces.new(v)
    me = bpy.data.meshes.new("IvyCluster%d" % i)
    bm.to_mesh(me); bm.free()
    me.materials.append(M_IVY)
    ivy_c.objects.link(bpy.data.objects.new("IvyCluster%d" % i, me))

add_scatter(ground, scatter_group("ScatterGrass", 80.0, "grassdens", grass_c, 0.6, 1.35, 11))
add_scatter(ground, scatter_group("ScatterFlowers", 1.6, "grassdens", flower_c, 0.7, 1.2, 55))
add_scatter(ground, scatter_group("ScatterFerns", 0.9, "ferndens", fern_c, 0.7, 1.5, 22))
add_scatter(ground, scatter_group("ScatterRocks", 0.35, "rockdens", rock_c, 0.4, 1.8, 33, tilt=0.5))

tower_base = min(height(TOWER.x + math.cos(a) * TOWER_R, TOWER.y + math.sin(a) * TOWER_R)
                 for a in [k * 0.3 for k in range(21)]) - 0.25
tower, n_stones, sources = build_tower(tower_base)
build_mortar_core(tower_base)
add_scatter(tower, scatter_group("ScatterIvy", 55.0, "ivydens", ivy_c, 0.7, 1.3, 44, align=True, tilt=0.4))
n_rubble = build_rubble(tower_base, sources)
build_beams(tower_base)

trees = [make_tree("TreeMesh%d" % i, 100 + i, height_m=14.0) for i in range(4)]
bushes = [make_tree("BushMesh%d" % i, 200 + i, height_m=3.0, bush=True) for i in range(3)]
n_trees = place_forest(trees, bushes)
# big boulders
brng = random.Random(8)
for i, (x, y, r) in enumerate([(-8.5, -2.0, 1.1), (6.5, 2.0, 0.8), (-3.0, 14.5, 0.9), (11.0, -6.0, 1.3)]):
    obj("Boulder%d" % i, rock_mesh("BoulderMesh%d" % i, brng, r=r, flat=0.6, detail=5), C_TERRAIN,
        (x, y, height(x, y) + r * 0.05), (0, 0, brng.uniform(0, 6.28)))

build_lighting()
build_camera()
render_settings(MODE == "final")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "TestScene_RuinedTower.blend"))
print("STATS stones=%d rubble=%d trees=%d" % (n_stones, n_rubble, n_trees))
if MODE in ("preview", "final"):
    bpy.ops.render.render(write_still=True)
    print("RENDERED", OUT)
