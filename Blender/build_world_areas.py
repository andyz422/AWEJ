"""
AWEJ - areas 2-5 (Whispering Grove, Sunscorch Dunes, Frostpeak Hollow, Molten Citadel).

Data-driven port of AreaDatabase.cs: each area becomes its own Blender scene "AWEJ_<Name>".
Unity (x, z) -> Blender (x, y); Unity yaw f -> Blender rot Z = -f; doors face local -Y.

Run inside Blender (set ONLY_AREAS in the exec globals to build a subset):
    g = {'__name__': '__main__', '__file__': path, 'ONLY_AREAS': ['Whispering Grove']}
    exec(compile(open(path).read(), path, 'exec'), g)
"""
import os, bpy, bmesh, math, random
from mathutils import Vector, Matrix

KIT = os.path.join(os.path.dirname(__file__), 'awej_kit.py')
exec(compile(open(KIT, encoding='utf-8').read(), KIT, 'exec'), globals())

# --------------------------------------------------------------------------- material factories
def darker(c, f=0.82):
    return tuple(x * f for x in c[:3])

def lighter(c, f=0.25):
    return tuple(x + (1 - x) * f for x in c[:3])

def ck(c):
    return "%02x%02x%02x" % tuple(int(max(0, min(1, x)) * 255) for x in c[:3])

def mat_plaster(c): return textured('Plaster_' + ck(c), 'noise', c, darker(c, 0.9), noise_scale=1.5, bump=0.15)
def mat_stone(c): return textured('Stone_' + ck(c), 'brick', c, darker(c, 0.86), brick_w=0.8, row_h=0.4,
                                  mortar=darker(c, 0.55), mortar_size=0.03, bump=0.6)
def mat_shingle(c): return textured('Shingle_' + ck(c), 'brick', c, darker(c, 0.85), brick_w=0.55, row_h=0.3,
                                    mortar=darker(c, 0.45), mortar_size=0.03, bump=0.8, rough=0.7)
def mat_tiles(c): return textured('Tiles_' + ck(c), 'brick', c, lighter(c, 0.15), brick_w=0.3, row_h=0.3,
                                  mortar=lighter(c, 0.6), mortar_size=0.04, bump=0.4, rough=0.25)
def mat_planks(c): return textured('Planks_' + ck(c), 'brick', c, darker(c, 0.88), brick_w=0.28, row_h=6.0,
                                   mortar=darker(c, 0.5), mortar_size=0.015, bump=0.4, grime=0.3)
def mat_logs(c): return textured('Logs_' + ck(c), 'brick', c, darker(c, 0.85), brick_w=9.0, row_h=0.42,
                                 mortar=darker(c, 0.45), mortar_size=0.06, bump=0.9, grime=0.35)
def mat_cobble(c, scale=1.6): return textured('Cobble_' + ck(c) + '_%g' % scale, 'cobble', darker(c, 0.9), lighter(c, 0.1),
                                              scale=scale, mortar=darker(c, 0.5), bump=0.6)
def mat_gravel(c): return textured('Gravel_' + ck(c), 'gravel', lighter(c, 0.1), darker(c, 0.78), bump=0.35, rough=0.95)
def mat_bark(c): return textured('Bark_' + ck(c), 'thatch', darker(c, 0.7), lighter(c, 0.1), bump=0.7, rough=0.95)
def mat_noise(c, name='Noise', scale=1.0, coords='UV', rough=0.8, bump=0.3):
    return textured('%s_%s' % (name, ck(c)), 'noise', c, darker(c, 0.82), scale=scale, coords=coords, rough=rough, bump=bump)
def mat_flat(c, rough=0.6, metal=0.0): return plain('Flat_%s_%g_%g' % (ck(c), rough, metal), c, rough, metal=metal)
def mat_emit(c, s): return plain('Emit_%s_%g' % (ck(c), s), c, 0.3, emit=c, emit_strength=s)

def custom_mat(name):
    m = bpy.data.materials.get(name)
    if m:
        return m, None, None
    return new_mat(name)

def mat_stripes(c1, c2, width=0.7):
    name = 'Stripes_%s_%s' % (ck(c1), ck(c2))
    m, nt, b = custom_mat(name)
    if nt is None:
        return m
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord'); sep = N.new('ShaderNodeSeparateXYZ')
    L.new(tc.outputs['UV'], sep.inputs[0])
    mul = N.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'; mul.inputs[1].default_value = 1.0 / (2 * width)
    L.new(sep.outputs['X'], mul.inputs[0])
    fr = N.new('ShaderNodeMath'); fr.operation = 'FRACT'
    L.new(mul.outputs[0], fr.inputs[0])
    r = ramp(nt, [(0.0, c1), (0.5, c2)])
    r.color_ramp.interpolation = 'CONSTANT'
    L.new(fr.outputs[0], r.inputs['Fac'])
    L.new(r.outputs['Color'], b.inputs['Base Color'])
    setin(b, 'Roughness', 0.85)
    setin(b, 'Sheen Weight', 0.4)
    m.diffuse_color = rgba(c1)
    return m

def mat_lava():
    m, nt, b = custom_mat('Lava')
    if nt is None:
        return m
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord'); mp = N.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = (0.35, 0.35, 0.35)
    L.new(tc.outputs['Object'], mp.inputs['Vector'])
    nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 2.5
    setin(nz, 'Detail', 8.0); setin(nz, 'Distortion', 0.8)
    L.new(mp.outputs[0], nz.inputs['Vector'])
    r = ramp(nt, [(0.42, (0.05, 0.015, 0.01)), (0.55, (0.9, 0.12, 0.01)), (0.75, (1.0, 0.5, 0.08))])
    L.new(nz.outputs['Fac'], r.inputs['Fac'])
    L.new(r.outputs['Color'], b.inputs['Base Color'])
    L.new(r.outputs['Color'], b.inputs['Emission Color'])
    setin(b, 'Emission Strength', 2.2)
    setin(b, 'Roughness', 0.55)
    bp = N.new('ShaderNodeBump'); bp.inputs['Strength'].default_value = 0.6
    L.new(nz.outputs['Fac'], bp.inputs['Height']); L.new(bp.outputs['Normal'], b.inputs['Normal'])
    m.diffuse_color = (1, 0.3, 0.05, 1)
    return m

def mat_burnt_ground(c):
    m, nt, b = custom_mat('BurntGround_' + ck(c))
    if nt is None:
        return m
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord'); mp = N.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = (0.12, 0.12, 0.12)
    L.new(tc.outputs['Object'], mp.inputs['Vector'])
    nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 3.0; setin(nz, 'Detail', 6.0)
    L.new(mp.outputs[0], nz.inputs['Vector'])
    base = ramp(nt, [(0.3, darker(c, 0.6)), (0.7, lighter(c, 0.08))])
    L.new(nz.outputs['Fac'], base.inputs['Fac'])
    L.new(base.outputs['Color'], b.inputs['Base Color'])
    vo = N.new('ShaderNodeTexVoronoi'); vo.feature = 'DISTANCE_TO_EDGE'; vo.inputs['Scale'].default_value = 1.4
    L.new(mp.outputs[0], vo.inputs['Vector'])
    crack = ramp(nt, [(0.0, (1.0, 0.25, 0.03)), (0.012, (0, 0, 0))])
    L.new(vo.outputs['Distance'], crack.inputs['Fac'])
    mask = ramp(nt, [(0.58, (0, 0, 0)), (0.68, (1, 1, 1))])
    L.new(nz.outputs['Fac'], mask.inputs['Fac'])
    L.new(mix_mult(nt, crack.outputs['Color'], mask.outputs['Color']), b.inputs['Emission Color'])
    setin(b, 'Emission Strength', 2.5)
    setin(b, 'Roughness', 0.9)
    bp = N.new('ShaderNodeBump'); bp.inputs['Strength'].default_value = 0.4
    L.new(vo.outputs['Distance'], bp.inputs['Height']); L.new(bp.outputs['Normal'], b.inputs['Normal'])
    m.diffuse_color = rgba(c)
    return m

def mat_strata(c1, c2):
    m, nt, b = custom_mat('Strata_%s' % ck(c1))
    if nt is None:
        return m
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord')
    wv = N.new('ShaderNodeTexWave'); wv.bands_direction = 'Z'; wv.inputs['Scale'].default_value = 0.08
    setin(wv, 'Distortion', 3.0); setin(wv, 'Detail', 3.0)
    L.new(tc.outputs['Object'], wv.inputs['Vector'])
    r = ramp(nt, [(0.2, c1), (0.5, c2), (0.8, darker(c1, 0.8))])
    L.new(wv.outputs['Fac'], r.inputs['Fac'])
    L.new(r.outputs['Color'], b.inputs['Base Color'])
    setin(b, 'Roughness', 0.9)
    m.diffuse_color = rgba(c1)
    return m

def mat_ice(c=(0.5, 0.8, 1.0)):
    m, nt, b = custom_mat('Ice_' + ck(c))
    if nt is None:
        return m
    setin(b, 'Base Color', rgba(c)); setin(b, 'Roughness', 0.08)
    setin(b, 'Transmission Weight', 0.35); setin(b, 'IOR', 1.31)
    setin(b, 'Emission Color', rgba(c)); setin(b, 'Emission Strength', 0.5)
    setin(b, 'Coat Weight', 0.5)
    m.diffuse_color = rgba(c)
    return m

def mat_rings(c):
    m, nt, b = custom_mat('TreeRings_' + ck(c))
    if nt is None:
        return m
    N, L = nt.nodes, nt.links
    tc = N.new('ShaderNodeTexCoord')
    wv = N.new('ShaderNodeTexWave'); wv.wave_type = 'RINGS'; wv.inputs['Scale'].default_value = 0.35
    setin(wv, 'Distortion', 2.0)
    L.new(tc.outputs['Object'], wv.inputs['Vector'])
    r = ramp(nt, [(0.2, darker(c, 0.75)), (0.8, lighter(c, 0.1))])
    L.new(wv.outputs['Fac'], r.inputs['Fac'])
    L.new(r.outputs['Color'], b.inputs['Base Color'])
    setin(b, 'Roughness', 0.8)
    return m

M['fire'] = plain('Fire', (1.0, 0.45, 0.08), 0.5, emit=(1.0, 0.32, 0.03), emit_strength=3.5)
M['fire_core'] = plain('FireCore', (1.0, 0.85, 0.4), 0.5, emit=(1.0, 0.65, 0.2), emit_strength=5.0)
M['portal'] = plain('PortalCyan', (0.3, 0.85, 1.0), 0.1, emit=(0.3, 0.85, 1.0), emit_strength=3.5)
M['orb_green'] = plain('GateOrbOpen', (0.2, 0.9, 0.4), 0.2, emit=(0.2, 1.0, 0.35), emit_strength=2.5)
M['snow'] = textured('Snow', 'noise', (0.86, 0.9, 0.97), (0.97, 0.98, 1.0), coords='OBJECT', scale=0.25, bump=0.25, rough=0.6)
M['sand'] = textured('Sand', 'noise', (0.74, 0.5, 0.27), (0.84, 0.62, 0.36), c3=(0.9, 0.72, 0.45), coords='OBJECT', scale=0.07,
                     bump=0.25, rough=0.95, noise_scale=4.0)
M['moss_ground'] = textured('ForestFloor', 'noise', (0.12, 0.3, 0.08), (0.22, 0.42, 0.12), c3=(0.35, 0.36, 0.14),
                            coords='OBJECT', scale=0.08, bump=0.2, rough=0.95, noise_scale=3.0)
M['cactus'] = textured('Cactus', 'thatch', (0.2, 0.45, 0.2), (0.32, 0.58, 0.28), bump=0.4, rough=0.6)
M['palm_leaf'] = plain('PalmLeaf', (0.25, 0.55, 0.15), 0.6)
M['obsidian'] = plain('Obsidian', (0.05, 0.04, 0.07), 0.12, coat=0.6)
M['basalt'] = textured('Basalt', 'noise', (0.12, 0.11, 0.11), (0.2, 0.18, 0.17), bump=0.5, rough=0.85)
M['charred'] = textured('CharredWood', 'thatch', (0.06, 0.05, 0.05), (0.16, 0.12, 0.1), bump=0.6)
M['mush_red'] = plain('MushroomRed', (0.85, 0.12, 0.1), 0.45, coat=0.4)
M['mush_glow'] = plain('MushroomGlow', (0.3, 0.7, 1.0), 0.3, emit=(0.3, 0.75, 1.0), emit_strength=3.0)
M['firefly'] = plain('Firefly', (0.9, 1.0, 0.5), 0.3, emit=(0.85, 1.0, 0.4), emit_strength=25.0)
M['ember'] = plain('Ember', (1.0, 0.4, 0.1), 0.3, emit=(1.0, 0.35, 0.05), emit_strength=30.0)
M['snowflake'] = plain('Snowflake', (1, 1, 1), 0.5, emit=(0.9, 0.95, 1.0), emit_strength=1.5)
M['carrot'] = plain('Carrot', (1.0, 0.45, 0.05), 0.5)
M['pot'] = textured('Terracotta', 'noise', (0.72, 0.36, 0.2), (0.62, 0.3, 0.16), bump=0.2)

# --------------------------------------------------------------------------- extra geometry helpers
def cap_sphere(name, r, loc, mat, coll_, parent=None, zcut=0.0, scale=(1, 1, 1), segs=40, fill=True, rot=(0, 0, 0),
               jitter=0.0, seed=0):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=r)
    dead = [v for v in bm.verts if v.co.z < zcut * r - 1e-4]
    if dead:
        bmesh.ops.delete(bm, geom=dead, context='VERTS')
    if fill:
        bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)
    if jitter:
        jr = random.Random(seed)
        for v in bm.verts:
            if v.co.z > zcut * r + 0.05:
                v.co += v.co.normalized() * jr.uniform(-jitter, jitter) * r
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    me = to_mesh(bm, name)
    # spherical UVs in metres
    me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    for p in me.polygons:
        angs = [math.atan2(me.vertices[me.loops[li].vertex_index].co.y, me.vertices[me.loops[li].vertex_index].co.x)
                for li in p.loop_indices]
        if max(angs) - min(angs) > math.pi:
            angs = [a + 2 * math.pi if a < 0 else a for a in angs]
        for li, a in zip(p.loop_indices, angs):
            co = me.vertices[me.loops[li].vertex_index].co
            if abs(p.normal.z) > 0.99 and p.normal.z < 0:
                uv[li].uv = (co.x, co.y)
            else:
                el = math.asin(max(-1, min(1, co.z / max(co.length, 1e-4))))
                uv[li].uv = (a * r * 0.8, el * r)
    for p in me.polygons:
        p.use_smooth = not (p.normal.z < -0.99)
    return place(name, me, coll_, loc, rot, parent, mat)

def hip_roof(name, W, D, rise, mat, coll_, loc, parent, overhang=0.6, thickness=0.14):
    X, Y = W / 2 + overhang, D / 2 + overhang
    rx = max(W / 2 - D / 2, 0.01)
    bm = bmesh.new()
    b0, b1, b2, b3 = [bm.verts.new(p) for p in ((-X, -Y, 0), (X, -Y, 0), (X, Y, 0), (-X, Y, 0))]
    r0, r1 = bm.verts.new((-rx, 0, rise)), bm.verts.new((rx, 0, rise))
    bm.faces.new((b0, b1, r1, r0)); bm.faces.new((b1, b2, r1)); bm.faces.new((b2, b3, r0, r1)); bm.faces.new((b3, b0, r0))
    me = to_mesh(bm, name)
    box_uv(me)
    ob = place(name, me, coll_, loc, (0, 0, 0), parent, mat)
    sd = ob.modifiers.new("Solidify", 'SOLIDIFY'); sd.thickness = thickness; sd.offset = 1.0
    return ob

def flame(parent, coll_, loc, s=1.0, light=True, energy=250):
    g = empty("Flame", coll_, loc, (0, 0, 0), parent)
    cyl("FlameOuter", 0.32 * s, 0.9 * s, (0, 0, 0), M['fire'], coll_, g, r2=0.0, segs=10)
    cyl("FlameInner", 0.18 * s, 0.6 * s, (0, 0, 0.02), M['fire_core'], coll_, g, r2=0.0, segs=8)
    for k in range(3):
        a = k * 2.1
        cyl("FlameLick", 0.14 * s, 0.55 * s, (math.cos(a) * 0.2 * s, math.sin(a) * 0.2 * s, 0), M['fire'], coll_, g,
            r2=0.0, segs=6, rot=(math.cos(a) * 0.25, math.sin(a) * 0.25, 0))
    if light:
        li = bpy.data.lights.new("FireLight", 'POINT')
        li.energy = energy * s
        li.color = (1.0, 0.5, 0.18)
        li.shadow_soft_size = 0.3
        place("FireLight", li, coll_, (0, 0, 0.6 * s), parent=g)
    return g

def brazier(loc, coll_, parent=None, stone=None, s=1.0):
    g = empty("Brazier", coll_, loc, (0, 0, 0), parent)
    cyl("BrazierPedestal", 0.38 * s, 1.1 * s, (0, 0, 0), stone or M['stone_base'], coll_, g, r2=0.28 * s, segs=8, bevel=0.03)
    cyl("BrazierBowl", 0.35 * s, 0.4 * s, (0, 0, 1.1 * s), M['iron'], coll_, g, r2=0.6 * s, segs=16)
    flame(g, coll_, (0, 0, 1.4 * s), 0.9 * s)
    return g

def standing_sign(root, c, b, depth, x_off=None):
    w = max(3.4, len(b['name']) * 0.34 + 1.4)
    x = x_off if x_off is not None else w / 2 + 2.4
    g = empty("SignStand", c, (x, -depth - 1.6, 0), (0, 0, 0), root)
    for sx in (-1, 1):
        cyl("SignPole", 0.1, 3.3, (sx * (w / 2 - 0.3), 0.05, 0), M['wood'], c, g, segs=8)
    signboard(g, c, b['name'], (0, -0.1, 2.8), b['type'] == 'Shop', width=w)
    return g

def snow_cap(name, loc, scale, parent, coll_):
    return sphere(name, 1.0, loc, M['snow'], coll_, parent, scale=scale, subdiv=2, ico=True, jitter=0.1)

# --------------------------------------------------------------------------- building styles
def door_depth(b):
    w, d = b['size']
    s = b['style']
    if s == 'Mushroom': return w * 0.35
    if s == 'Dome': return d * 0.5 + 1.5
    if s in ('Hut', 'Tower', 'Arena', 'Stump'): return w * 0.5
    return d * 0.5

def style_house(b, c, root, theme):
    W, D = b['size']
    base, H = 0.3, (4.6 if W <= 12 else 5.0)
    top = base + H
    snowy = theme == 'Snow'
    body = mat_logs(b['body']) if snowy else mat_plaster(b['body'])
    trim = mat_flat(b['trim'])
    roofm = M['snow'] if snowy else mat_shingle(b['roof'])
    box("Foundation", (W + 0.4, D + 0.4, base), (0, 0, base / 2), M['stone_base'], c, root, bevel=0.06)
    box("Walls", (W, D, H), (0, 0, base + H / 2), body, c, root, bevel=0.04)
    for sx in (-1, 1):
        for sy in (-1, 1):
            box("CornerPost", (0.42, 0.42, H), (sx * W / 2, sy * D / 2, base + H / 2), trim if not snowy else mat_logs(darker(b['body'], 0.8)),
                c, root, bevel=0.05)
    for sy in (-1, 1):
        box("BeamTop", (W + 0.3, 0.3, 0.3), (0, sy * (D / 2 + 0.03), top - 0.15), trim, c, root, bevel=0.03)
    rise = (D / 2) * math.tan(math.radians(35))
    prism("Attic", [(-D / 2, 0), (D / 2, 0), (0, rise)], W, body, c, (0, 0, top), root)
    house_roof("Roof", root, c, D, W, top, [(-D / 2, 0), (0, rise), (D / 2, 0)], roofm, overhang=0.9,
               thickness=0.45 if snowy else 0.3)
    if snowy:
        ir = random.Random(len(b['name']))
        for sy in (-1, 1):
            x = -W / 2 - 0.4
            while x < W / 2 + 0.4:
                h = ir.uniform(0.25, 0.9)
                cyl("Icicle", 0.07, h, (x, sy * (D / 2 + 0.72), top - 0.5), mat_ice(), c, root, r2=0.0, rot=(math.pi, 0, 0), segs=6)
                x += ir.uniform(0.35, 0.8)
        for k in range(8):
            sphere("Drift", 1.0, (ir.uniform(-W / 2, W / 2), ir.choice((-1, 1)) * (D / 2 + 0.5), 0.05), M['snow'], c, root,
                   scale=(ir.uniform(0.8, 1.6), 0.7, 0.35), subdiv=2, ico=True, jitter=0.08, seed=k)
    box("Chimney", (1.0, 1.0, 3.6), (W * 0.28, D * 0.18, top + 2.2), M['stone_base'], c, root, bevel=0.05)
    box("ChimneyCap", (1.3, 1.3, 0.25), (W * 0.28, D * 0.18, top + 4.1), M['snow'] if snowy else M['wall_cap'], c, root, bevel=0.04)
    for x in (-W * 0.32, W * 0.32):
        window(root, c, "WinFront", (x, -D / 2 - 0.05, base + 2.0), (0, -1), 1.5, 1.5, trim)
        window(root, c, "WinBack", (x, D / 2 + 0.05, base + 2.0), (0, 1), 1.5, 1.5, trim)
    for sx in (-1, 1):
        window(root, c, "WinSide", (sx * (W / 2 + 0.05), 0, base + 2.0), (sx, 0), 1.5, 1.5, trim)
        window(root, c, "WinGable", (sx * (W / 2 + 0.05), 0, top + 1.2), (sx, 0), 0.9, 0.9, trim)
    door_assembly(root, c, D / 2, b['type'] == 'Shop', trim, base)
    signboard(root, c, b['name'], (0, -D / 2 - 0.16, base + 4.15), b['type'] == 'Shop',
              width=max(4.2, len(b['name']) * 0.34 + 1.4))
    if b['id'] == 'cocoa_cabin':
        for k in range(14):  # wreath
            a = k * 2 * math.pi / 14
            sphere("Wreath", 0.13, (math.cos(a) * 0.45, -D / 2 - 0.42, base + 2.3 + math.sin(a) * 0.45), M['leaf_c'], c, root, subdiv=1)
        sphere("WreathBow", 0.12, (0, -D / 2 - 0.5, base + 1.85), mat_flat((0.85, 0.1, 0.1)), c, root, subdiv=2)
        for i, (x, y) in enumerate([(-3.0, -6.3), (3.2, -6.0)]):
            barrel(root, c, (x, y, 0))
            snow_cap("BarrelSnow", (x, y, 1.12), (0.45, 0.45, 0.12), root, c)
    if b['id'] == 'yeti_lodge':
        for sx in (-1, 1):
            g = empty("TorchStand", c, (sx * 2.4, -D / 2 - 1.2, 0), (0, 0, 0), root)
            cyl("TorchPole", 0.1, 2.4, (0, 0, 0), M['wood'], c, g, segs=8)
            flame(g, c, (0, 0, 2.4), 0.6)
        for k, (x, y) in enumerate([(-5.0, -7.0), (-6.2, -7.4), (5.4, -7.2)]):  # firewood stacks
            for j in range(3):
                cyl("Firewood", 0.22, 1.4, (x, y + j * 0.42 - 0.4, 0.22), M['trunk'], c, root, rot=(0, math.pi / 2, 0), base=False, segs=10)
            cyl("Firewood", 0.22, 1.4, (x, y - 0.2, 0.6), M['trunk'], c, root, rot=(0, math.pi / 2, 0), base=False, segs=10)

def style_tower(b, c, root, theme):
    R0 = b['size'][0] / 2
    R = R0 - 0.4
    Hb = max(9.5, R * 2.1)
    k = R / 4.6
    body = mat_planks(b['body']) if b['id'] == 'ranger_outpost' else mat_stone(b['body'])
    roofm = mat_shingle(b['roof'])
    capm = mat_stone(lighter(b['body'], 0.25)) if b['id'] != 'ranger_outpost' else M['wood']
    cyl("Plinth", R + 0.6, 0.6, (0, 0, 0), M['stone_base'] if theme != 'Volcano' else M['basalt'], c, root, segs=40, bevel=0.06)
    cyl("Body", R, Hb, (0, 0, 0.6), body, c, root, r2=R * 0.9, segs=40)
    cyl("Band", R * 0.97 + 0.15, 0.35, (0, 0, 0.6 + Hb * 0.4), capm, c, root, segs=40)
    top = 0.6 + Hb
    cyl("Corbel", R * 0.9 + 0.05, 0.6, (0, 0, top), capm, c, root, r2=R * 0.9 + 0.7, segs=40)
    n = int(16 * k)
    for i in range(n):
        a = i * 2 * math.pi / n
        box("Crenel", (1.1, 0.8, 0.7), (math.cos(a) * (R * 0.9 + 0.35), math.sin(a) * (R * 0.9 + 0.35), top + 0.95), body, c,
            root, rot=(0, 0, a + math.pi / 2), bevel=0.05)
        if b['id'] == 'citadel_throne' and i % 2 == 0:
            cyl("Spike", 0.25, 1.6, (math.cos(a) * (R * 0.9 + 0.35), math.sin(a) * (R * 0.9 + 0.35), top + 1.3), M['iron'], c,
                root, r2=0.0, segs=6)
    rh = R * 1.25
    cyl("Roof", R * 0.95 + 0.4, rh, (0, 0, top + 0.6), roofm, c, root, r2=0.15, segs=32)
    if theme == 'Snow':
        cyl("RoofSnow", R * 0.6, rh * 0.45, (0, 0, top + 0.6 + rh * 0.55), M['snow'], c, root, r2=0.1, segs=32)
    cyl("Finial", 0.08 * k + 0.04, 1.4, (0, 0, top + 0.6 + rh - 0.2), M['brass'], c, root)
    glassm = mat_emit((1.0, 0.45, 0.1), 3.0) if theme == 'Volcano' else None
    for i, a in enumerate([math.pi / 2, math.pi * 0.95, 0.05, math.pi * 1.25, math.pi * 1.75]):
        for z in (Hb * 0.6, Hb * 0.85):
            rr = R - (z - 0.6) / Hb * R * 0.1 + 0.05
            w = window(root, c, "TWin", (math.cos(a) * rr, math.sin(a) * rr, z), (math.cos(a), math.sin(a)), 1.0, 1.4, capm)
            if glassm:
                for ch in w.children:
                    if ch.name.startswith("TWin_Glass"):
                        ch.data.materials[0] = glassm
    box("DoorSurround", (3.6, 1.3, 4.0), (0, -R0 + 0.65, 2.6), body, c, root, bevel=0.08)
    box("DoorSurroundCap", (4.0, 1.5, 0.35), (0, -R0 + 0.6, 4.7), capm, c, root, bevel=0.05)
    door_assembly(root, c, R0, b['type'] == 'Shop', capm, 0.6)
    signboard(root, c, b['name'], (0, -R0 - 0.25, 5.55), b['type'] == 'Shop', width=max(4.2, len(b['name']) * 0.34 + 1.4))
    if b['id'] == 'ranger_outpost':
        for sx in (-1, 1):
            box("RangerBanner", (1.0, 0.05, 2.6), (sx * 1.9, -R - 0.05, Hb * 0.75), mat_flat((0.2, 0.42, 0.18), 0.8), c, root, bevel=0)
        g = empty("BowRack", c, (-3.4, -R0 - 1.0, 0), (0, 0, 0), root)
        for sx in (-1, 1):
            cyl("RackPost", 0.08, 1.6, (sx * 0.8, 0, 0), M['wood'], c, g, segs=8)
        box("RackBar", (1.8, 0.1, 0.1), (0, 0, 1.5), M['wood'], c, g, bevel=0.01)
        for j in range(3):
            box("Target", (0.06, 0.05, 1.3), (-0.5 + j * 0.5, -0.05, 0.85), M['wood_light'], c, g, rot=(0, 0.1, 0), bevel=0)
    if b['id'] == 'ember_forge':
        box("ForgeChimney", (1.4, 1.4, Hb + 6), (R * 0.55, R * 0.55, (Hb + 6) / 2), M['basalt'], c, root, bevel=0.06)
        box("ForgeChimneyGlow", (1.0, 1.0, 0.2), (R * 0.55, R * 0.55, Hb + 6.02), M['fire'], c, root, bevel=0)
        g = empty("Anvil", c, (3.2, -R0 - 1.2, 0), (0, 0, 0.4), root)
        box("AnvilBase", (0.5, 0.4, 0.6), (0, 0, 0.3), M['iron'], c, g, bevel=0.03)
        box("AnvilTop", (1.1, 0.45, 0.3), (0, 0, 0.75), M['iron'], c, g, bevel=0.04)
        brazier((-3.2, -R0 - 1.2, 0), c, root, M['basalt'])
    if b['id'] == 'citadel_throne':
        for sx in (-1, 1):
            box("ThroneBanner", (1.6, 0.06, 5.5), (sx * 2.8, -R - 0.1, Hb * 0.6), mat_flat((0.6, 0.06, 0.05), 0.8), c, root, bevel=0)
            brazier((sx * 3.4, -R0 - 2.2, 0), c, root, M['basalt'], 1.3)

def style_hut(b, c, root, theme):
    R0 = b['size'][0] / 2
    R = R0 - 0.5
    stone = mat_cobble(b['body'], 1.8)
    roofm = M['thatch'] if theme != 'Volcano' else mat_shingle(b['roof'])
    cyl("Plinth", R + 0.4, 0.3, (0, 0, 0), M['basalt'] if theme == 'Volcano' else M['stone_base'], c, root, segs=36, bevel=0.05)
    cyl("Body", R, 3.4, (0, 0, 0.3), stone, c, root, r2=R - 0.2, segs=36)
    cyl("RoofRing", R + 0.1, 0.3, (0, 0, 3.7), M['wood'], c, root, segs=36)
    cyl("Roof", R + 1.4, 4.6, (0, 0, 3.8), roofm, c, root, r2=0.5, segs=36)
    cyl("RoofTop", 0.7, 0.9, (0, 0, 8.3), roofm, c, root, r2=0.05, segs=16)
    box("Entry", (3.6, 1.4, 3.7), (0, -R + 0.2, 0.3 + 1.85), stone, c, root, bevel=0.1)
    box("EntryLintel", (3.9, 1.6, 0.4), (0, -R + 0.15, 3.95), M['wall_cap'], c, root, bevel=0.06)
    door_assembly(root, c, R0, b['type'] == 'Shop', M['wall_cap'], 0.3)
    standing_sign(root, c, b, R0)
    if b['id'] == 'ash_altar':
        g = empty("Altar", c, (-3.6, -R0 - 1.6, 0), (0, 0, 0), root)
        box("AltarBlock", (1.6, 1.0, 1.0), (0, 0, 0.5), M['basalt'], c, g, bevel=0.06)
        box("AltarTop", (1.9, 1.2, 0.2), (0, 0, 1.1), M['obsidian'], c, g, bevel=0.04)
        flame(g, c, (0, 0, 1.2), 0.8)
        for k in range(6):
            a = k * 1.05
            cyl("Rune", 0.12, 0.05, (math.cos(a) * 1.4, math.sin(a) * 1.4, 0.0), mat_emit((1.0, 0.25, 0.05), 6), c, g, segs=6)

def style_mushroom(b, c, root, theme):
    R0 = b['size'][0] * 0.35
    capR = b['size'][0] * 0.62
    bodym = mat_plaster(b['body'])
    cyl("Stem", R0, 4.6, (0, 0, 0), bodym, c, root, r2=R0 * 0.82, segs=36)
    cyl("Gills", capR * 0.95, 0.15, (0, 0, 4.45), mat_flat(darker(b['body'], 0.85), 0.9), c, root, segs=48)
    cap_sphere("Cap", capR, (0, 0, 4.4), M['mush_red'] if b['roof'][0] > 0.6 else mat_flat(b['roof'], 0.45), c, root,
               zcut=0.0, scale=(1, 1, 0.62), segs=48)
    sr = random.Random(3)
    for k in range(16):
        th = sr.uniform(0, 2 * math.pi)
        ph = sr.uniform(0.15, 1.25)
        p = Vector((math.sin(ph) * math.cos(th), math.sin(ph) * math.sin(th), math.cos(ph) * 0.62)) * capR
        n = Vector((p.x, p.y, p.z / 0.62 / 0.62)).normalized()
        ob = sphere("Spot", sr.uniform(0.35, 0.7), (p.x, p.y, 4.4 + p.z), M['trim_white'], c, root, scale=(1, 1, 0.25), subdiv=2)
        ob.rotation_euler = n.to_track_quat('Z', 'Y').to_euler()
    for a in (math.pi * 0.2, math.pi * 0.8, math.pi * 1.1):
        window(root, c, "RoundWin", (math.cos(a) * (R0 - 0.2), math.sin(a) * (R0 - 0.2), 2.3), (math.cos(a), math.sin(a)),
               0.9, 0.9, M['wood'])
    door_assembly(root, c, R0, b['type'] == 'Shop', M['wood'], 0.0)
    cyl("Chimney", 0.25, 2.0, (capR * 0.35, capR * 0.25, 7.2), M['iron'], c, root, segs=12)
    standing_sign(root, c, b, R0)
    for k in range(8):  # herb pots
        a = -math.pi / 2 + (k - 3.5) * 0.22
        x, y = math.cos(a) * (R0 + 1.6), math.sin(a) * (R0 + 1.6)
        if abs(x) < 1.8:
            continue
        cyl("HerbPot", 0.3, 0.45, (x, y, 0), M['pot'], c, root, r2=0.38, segs=14)
        sphere("Herb", 0.35, (x, y, 0.6), M['leaf_b'], c, root, subdiv=1, ico=True, jitter=0.2, do_smooth=False, seed=k)

def style_stump(b, c, root, theme):
    R0 = b['size'][0] / 2
    H = 6.5
    bark = mat_bark(b['body'])
    cyl("StumpBody", R0, H, (0, 0, 0), bark, c, root, r2=R0 * 0.88, segs=40)
    cyl("StumpTop", R0 * 0.82, 0.25, (0, 0, H - 0.1), mat_rings(b['roof']), c, root, segs=40)
    jr = random.Random(5)
    for i in range(28):
        a = i * 2 * math.pi / 28
        h = jr.uniform(0.4, 1.6)
        rr = R0 * 0.85
        box("Splinter", (1.2, 0.5, h), (math.cos(a) * rr, math.sin(a) * rr, H + h / 2 - 0.2), bark, c, root,
            rot=(jr.uniform(-0.15, 0.15), jr.uniform(-0.15, 0.15), a + math.pi / 2), bevel=0.05, segs=1)
    for i in range(7):
        a = i * 2 * math.pi / 7 + 0.4
        if abs(math.sin(a) + 1) < 0.35:  # keep the door side clear
            continue
        g = empty("Root", c, (math.cos(a) * (R0 - 0.5), math.sin(a) * (R0 - 0.5), 0.2), (0, 0, a), root)
        cyl("RootLimb", 1.0, 5.0, (0, 0, 0), bark, c, g, r2=0.15, rot=(0, math.radians(72), 0), segs=10)
    for k in range(6):
        a = jr.uniform(0, 2 * math.pi)
        z = jr.uniform(1.0, H - 1.0)
        rr = R0 - (z / H) * R0 * 0.12
        cap_sphere("ShelfFungus", 0.8, (math.cos(a) * rr, math.sin(a) * rr, z), mat_flat((0.85, 0.7, 0.45)), c, root,
                   zcut=0.0, scale=(1, 1, 0.3), segs=16, rot=(0, 0, a))
    for k in range(10):
        a = jr.uniform(0, 2 * math.pi)
        sphere("Moss", 1.0, (math.cos(a) * R0 * 0.95, math.sin(a) * R0 * 0.95, jr.uniform(0, 1.5)), M['leaf_c'], c, root,
               scale=(1.2, 1.2, 0.4), subdiv=2, ico=True, jitter=0.15, seed=k)
    box("DoorArch", (3.6, 1.2, 3.9), (0, -R0 + 0.55, 1.95), M['wood'], c, root, bevel=0.1)
    door_assembly(root, c, R0, b['type'] == 'Shop', M['wood'], 0.0)
    lamp_head(root, c, (1.9, -R0 - 0.5, 3.3))
    standing_sign(root, c, b, R0)

def style_dome(b, c, root, theme):
    R = b['size'][1] / 2
    R0 = R + 1.5
    shop = b['type'] == 'Shop'
    if theme == 'Desert':
        body = mat_plaster(b['body'])
        cyl("Drum", R, 3.6, (0, 0, 0), body, c, root, segs=48)
        cyl("DrumBand", R + 0.15, 0.3, (0, 0, 3.6), M['wall_cap'], c, root, segs=48)
        cap_sphere("Dome", R * 0.92, (0, 0, 3.8), mat_tiles(b['roof']), c, root, scale=(1, 1, 1.1), segs=48)
        cyl("OnionNeck", 0.5, 0.6, (0, 0, 3.8 + R * 1.0), M['brass'], c, root, segs=16)
        sphere("Onion", 0.7, (0, 0, 4.9 + R * 1.0), M['brass'], c, root, scale=(1, 1, 1.2), subdiv=3)
        cyl("Spire", 0.18, 1.2, (0, 0, 5.6 + R * 1.0), M['brass'], c, root, r2=0.0, segs=12)
        for a in (math.pi * 0.15, math.pi * 0.85, math.pi * 0.5):
            window(root, c, "ArchWin", (math.cos(a) * R, math.sin(a) * R, 2.0), (math.cos(a), math.sin(a)), 1.0, 1.6, mat_flat(b['roof']))
        box("Portal", (4.0, 3.2, 4.6), (0, -R - 0.1, 2.3), body, c, root, bevel=0.08)
        cyl("PortalArch", 2.0, 3.2, (0, -R - 0.1, 4.6), body, c, root, rot=(math.pi / 2, 0, 0), base=False, segs=24)
        box("PortalTrim", (4.3, 0.2, 0.3), (0, -R - 1.75, 4.3), mat_flat(b['roof']), c, root, bevel=0.03)
        door_assembly(root, c, R0 + 0.2, shop, mat_flat(b['roof']), 0.0)
        for sx in (-1, 1):
            cyl("Urn", 0.4, 0.9, (sx * 3.0, -R0 - 1.0, 0), M['pot'], c, root, r2=0.25, segs=16)
            sphere("UrnPlant", 0.5, (sx * 3.0, -R0 - 1.0, 1.1), M['palm_leaf'], c, root, subdiv=1, ico=True, jitter=0.3, do_smooth=False)
    else:
        if b['id'] == 'igloo_armory':
            body = textured('IceBlocks', 'brick', (0.93, 0.96, 1.0), (0.85, 0.92, 0.99), brick_w=1.1, row_h=0.55,
                            mortar=(0.62, 0.78, 0.92), mortar_size=0.05, bump=0.6, rough=0.4)
        elif b['id'] == 'ice_cavern':
            body = mat_noise(b['body'], 'IceRock', rough=0.35, bump=0.6)
        else:
            body = mat_noise(b['body'], 'Carapace', rough=0.35, bump=0.3)
        rough_dome = b['id'] == 'ice_cavern'
        cap_sphere("Dome", R, (0, 0, -0.2), body, c, root, scale=(1, 1, 0.85), segs=48 if not rough_dome else 24,
                   jitter=0.06 if rough_dome else 0.0, seed=9)
        cyl("Rim", R + 0.25, 0.5, (0, 0, 0), mat_flat(b['roof'], 0.4) if b['id'] != 'beetle_den' else M['basalt'], c, root, segs=48)
        # entrance tunnel
        cyl("Tunnel", 2.9, R0 - R + 0.95, (0, -R + 1.0, 0), body, c, root, rot=(math.pi / 2, 0, 0), segs=24)
        cyl("TunnelRim", 3.1, 0.4, (0, -R0 + 0.4, 0), mat_flat(b['roof'], 0.4), c, root, rot=(math.pi / 2, 0, 0), segs=24)
        door_assembly(root, c, R0, shop, mat_flat(b['roof'], 0.4), 0.0)
        if b['id'] == 'beetle_den':
            box("ShellSeam", (0.25, R * 2.0, 0.2), (0, 0.3, R * 0.85 - 0.25), M['basalt'], c, root, bevel=0.05)
            cap_sphere("ShellTop", R * 0.75, (0, R * 0.15, R * 0.35), mat_noise(b['roof'], 'Shell', rough=0.2), c, root,
                       scale=(1, 1.15, 0.75), segs=40)
            for sx in (-1, 1):
                cyl("Horn", 0.7, 4.5, (sx * 1.8, -R * 0.65, R * 0.55), M['horn'], c, root, r2=0.0,
                    rot=(math.radians(-55), sx * 0.25, 0), segs=12)
            for k in range(12):
                a = random.Random(k).uniform(0, 2 * math.pi)
                sphere("DenMoss", 1.0, (math.cos(a) * R * 0.95, math.sin(a) * R * 0.95, 0.2), M['leaf_c'], c, root,
                       scale=(1.4, 1.4, 0.6), subdiv=2, ico=True, jitter=0.15, seed=k)
        if b['id'] == 'ice_cavern':
            ir = random.Random(17)
            for k in range(22):
                a = ir.uniform(0, 2 * math.pi)
                el = ir.uniform(0.0, 1.1)
                p = Vector((math.cos(a) * math.cos(el), math.sin(a) * math.cos(el), math.sin(el) * 0.85)) * R
                if p.y < -R * 0.6 and abs(p.x) < 4:
                    continue
                n = Vector((p.x, p.y, p.z / 0.72)).normalized()
                ob = cyl("IceCrystal", ir.uniform(0.4, 0.9), ir.uniform(2.0, 5.0), (p.x, p.y, p.z - 0.4), mat_ice(), c, root,
                         r2=0.0, segs=6)
                ob.rotation_euler = n.lerp(Vector((0, 0, 1)), 0.3).to_track_quat('Z', 'Y').to_euler()
            for k in range(10):
                a = ir.uniform(0, 2 * math.pi)
                snow_cap("CavernDrift", (math.cos(a) * R, math.sin(a) * R, 0), (2.0, 1.4, 0.7), root, c)
        if b['id'] == 'igloo_armory':
            snow_cap("IglooCap", (0, 0, R * 0.85 - 0.6), (R * 0.45, R * 0.45, 0.6), root, c)
            g = empty("WeaponRack", c, (-4.0, -R0 - 1.0, 0), (0, 0, 0), root)
            for sx in (-1, 1):
                cyl("RackPost", 0.08, 1.8, (sx * 0.9, 0, 0), M['wood'], c, g, segs=8)
            box("RackBar", (2.0, 0.1, 0.1), (0, 0, 1.6), M['wood'], c, g, bevel=0.01)
            for j in range(4):
                box("RackSword", (0.08, 0.04, 1.3), (-0.6 + j * 0.4, -0.06, 0.95), plain_steel, c, g, bevel=0)
    standing_sign(root, c, b, R0 if theme != 'Desert' else R0 + 0.2)

def style_tent(b, c, root, theme):
    W, D = b['size']
    stripes = mat_stripes(b['body'], b['roof'], 0.7)
    roof_str = mat_stripes(b['roof'], b['body'], 0.9)
    H = 3.4
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl("TentPole", 0.16, H + 0.4, (sx * W / 2, sy * D / 2, 0), M['wood'], c, root, segs=10)
            sphere("PoleKnob", 0.22, (sx * W / 2, sy * D / 2, H + 0.5), M['brass'], c, root, subdiv=2)
    box("TentWalls", (W - 0.1, D - 0.1, H), (0, 0, H / 2), stripes, c, root, bevel=0.02)
    hip_roof("TentRoof", W, D, 4.2, roof_str, c, (0, 0, H), root, overhang=0.8)
    for sx in (-1, 1):
        x = sx * max(W / 2 - D / 2, 0.01)
        cyl("RidgePole", 0.08, 2.4, (x, 0, H + 4.1), M['wood'], c, root, segs=8)
        prism("Pennant", [(0, 0), (0, 0.6), (1.4, 0.3)], 0.04, mat_flat((0.9, 0.15, 0.1), 0.8), c, (x, 0.0, H + 5.9), root,
              (0, 0, math.pi / 2))
    # scalloped valance
    X, Y = W / 2 + 0.8, D / 2 + 0.8
    edge = [((-X, -Y), (X, -Y)), ((X, -Y), (X, Y)), ((X, Y), (-X, Y)), ((-X, Y), (-X, -Y))]
    k = 0
    for a, bb in edge:
        a, bb = Vector(a), Vector(bb)
        L = (bb - a).length
        n = int(L / 0.9)
        ang = math.atan2(bb.y - a.y, bb.x - a.x)
        for i in range(n):
            p = a + (bb - a) * ((i + 0.5) / n)
            prism("Scallop", [(-0.42, 0), (0.42, 0), (0, -0.55)], 0.04, mat_flat(b['body'] if k % 2 else b['roof'], 0.8),
                  c, (p.x, p.y, H + 0.02), root, (0, 0, ang + math.pi / 2))
            k += 1
    # front awning + market stalls
    hip_roof("Awning", 7.0, 3.0, 0.6, roof_str, c, (0, -D / 2 - 1.7, 2.9), root, overhang=0.2)
    for sx in (-1, 1):
        cyl("AwningPole", 0.1, 2.9, (sx * 3.4, -D / 2 - 3.2, 0), M['wood'], c, root, segs=8)
    door_assembly(root, c, D / 2, b['type'] == 'Shop', M['wood'], 0.15)
    for sx in (-1, 1):
        g = empty("Stall", c, (sx * 4.6, -D / 2 - 2.6, 0), (0, 0, 0), root)
        box("StallTable", (2.2, 1.1, 0.12), (0, 0, 1.0), M['wood_light'], c, g, bevel=0.03)
        for dx in (-1, 1):
            box("StallLeg", (0.1, 0.1, 1.0), (dx * 1.0, 0, 0.5), M['wood'], c, g, bevel=0)
        for j in range(4):
            col = [(0.85, 0.4, 0.15), (0.9, 0.75, 0.2), (0.6, 0.2, 0.5), (0.3, 0.6, 0.3)][j]
            cyl("Jar", 0.17, 0.35, (-0.75 + j * 0.5, 0, 1.06), mat_flat(col, 0.4), c, g, r2=0.12, segs=12)
        box("Rug", (2.4, 1.6, 0.03), (0, -1.4, 0.015), mat_stripes((0.6, 0.15, 0.2), (0.9, 0.7, 0.3), 0.25), c, g, bevel=0)
    standing_sign(root, c, b, D / 2 + 1.2, x_off=-(len(b['name']) * 0.17 + 6.9))

def style_arena(b, c, root, theme):
    R = b['size'][0] / 2
    stone = mat_stone(b['body'])
    accent = mat_flat(b['roof'], 0.7)
    floor = M['sand'] if theme == 'Desert' else (M['basalt'] if theme == 'Volcano' else M['gravel'])
    cyl("ArenaFloor", R - 0.6, 0.08, (0, 0, 0), floor, c, root, segs=48)
    cyl("ArenaBase", R + 0.4, 0.5, (0, 0, 0), M['stone_base'] if theme != 'Volcano' else M['basalt'], c, root, segs=48, bevel=0.05)
    N = int(R * 2.4)
    seg = 2 * math.pi * R / N
    h1, h2 = 4.2, 3.0
    for i in range(N):
        a = i * 2 * math.pi / N
        front = abs(((a + math.pi / 2 + math.pi) % (2 * math.pi)) - math.pi) < (1.2 * 2 * math.pi / N)
        rot = (0, 0, a + math.pi / 2)
        p = lambda r_, z: (math.cos(a) * r_, math.sin(a) * r_, z)
        if not front:
            box("Column", (1.0, 1.4, h1), p(R, 0.5 + h1 / 2), stone, c, root, rot=rot, bevel=0.06)
            box("Column2", (0.9, 1.2, h2), p(R, 0.5 + h1 + 1.0 + h2 / 2), stone, c, root, rot=rot, bevel=0.06)
            box("InnerWall", (seg, 0.6, 1.6), p(R - 1.1, 0.5 + 0.8), stone, c, root, rot=rot, bevel=0.04)
        am = (i + 0.5) * 2 * math.pi / N
        q = lambda r_, z: (math.cos(am) * r_, math.sin(am) * r_, z)
        rot2 = (0, 0, am + math.pi / 2)
        if not front or True:
            box("Lintel", (seg + 0.05, 1.5, 1.0), q(R, 0.5 + h1 + 0.5), stone, c, root, rot=rot2, bevel=0.04)
            box("Rim", (seg + 0.08, 1.7, 0.45), q(R, 0.5 + h1 + 1.0 + h2 + 0.22), accent if theme == 'Desert' else stone, c, root,
                rot=rot2, bevel=0.04)
        if i % 3 == 0 and not front:
            box("ArenaBanner", (1.1, 0.06, 2.6), p(R + 0.75, 0.5 + h1 - 0.6), accent, c, root, rot=rot, bevel=0)
            if theme == 'Volcano':
                brazier(p(R, 0.5 + h1 + 1.0 + h2 + 0.45), c, root, M['basalt'], 0.7)
            else:
                cyl("FlagPole", 0.06, 2.6, p(R, 0.5 + h1 + 1.0 + h2 + 0.45), M['wood'], c, root, segs=6)
                prism("Flag", [(0, 0), (0, 0.8), (1.3, 0.4)], 0.03, accent, c, p(R, 0.5 + h1 + 1.0 + h2 + 2.2), root, (0, 0, a))
    # front gate pylons
    for sx in (-1, 1):
        box("GatePylon", (1.6, 2.2, h1 + h2 + 2.6), (sx * 2.6, -R, (h1 + h2 + 2.6) / 2), stone, c, root, bevel=0.08)
        cyl("PylonTop", 0.9, 0.5, (sx * 2.6, -R, h1 + h2 + 2.6), accent, c, root, segs=12)
    box("GateBeam", (6.8, 2.0, 1.0), (0, -R, h1 + 0.5), stone, c, root, bevel=0.06)
    door_assembly(root, c, R + 0.6, b['type'] == 'Shop', accent, 0.0)
    signboard(root, c, b['name'], (0, -R - 1.1, h1 + 0.5), b['type'] == 'Shop', width=max(4.2, len(b['name']) * 0.34 + 1.4))
    for sx in (-1, 1):
        brazier((sx * 4.4, -R - 1.6, 0), c, root, stone if theme != 'Volcano' else M['basalt'])

STYLES = {'House': style_house, 'Tower': style_tower, 'Hut': style_hut, 'Mushroom': style_mushroom,
          'Stump': style_stump, 'Dome': style_dome, 'Tent': style_tent, 'Arena': style_arena}

def build_building(b, theme):
    c = coll("B_" + b['name'].replace("'", "").replace(" ", ""), C_BUILD)
    root = empty(b['name'], c, (b['pos'][0], b['pos'][1], 0), facing_rot(b['facing']))
    STYLES[b['style']](b, c, root, theme)
    return root

# --------------------------------------------------------------------------- walls, gates, paths, hazards
THEME_WALL = {
    'Forest': dict(body=lambda A: mat_cobble(A['wall'], 1.3), cap=lambda A: M['leaf_c'], merlon='round'),
    'Desert': dict(body=lambda A: mat_plaster(A['wall']), cap=lambda A: mat_plaster(lighter(A['wall'], 0.2)), merlon='step'),
    'Snow': dict(body=lambda A: mat_stone(A['wall']), cap=lambda A: M['snow'], merlon='round'),
    'Volcano': dict(body=lambda A: mat_stone(A['wall']), cap=lambda A: M['basalt'], merlon='spike'),
}

def wall_run2(name, a, b, A):
    th = THEME_WALL[A['theme']]
    body, cap = th['body'](A), th['cap'](A)
    base = M['basalt'] if A['theme'] == 'Volcano' else M['stone_base']
    a, b = Vector(a), Vector(b)
    L = (b - a).length
    if L < 0.5:
        return
    mid = (a + b) / 2
    g = empty(name, C_WALLS, (mid.x, mid.y, 0), (0, 0, math.atan2(b.y - a.y, b.x - a.x)))
    WH, WT = 3.0, 1.0
    box(name + "_Base", (L, WT + 0.5, 0.7), (0, 0, 0.35), base, C_WALLS, g, bevel=0.08)
    box(name + "_Body", (L, WT, WH), (0, 0, WH / 2), body, C_WALLS, g, bevel=0.05)
    if th['merlon'] == 'round':
        box(name + "_Cap", (L, WT + 0.4, 0.35), (0, 0, WH + 0.1), cap, C_WALLS, g, bevel=0.15)
    else:
        box(name + "_Cap", (L, WT + 0.3, 0.3), (0, 0, WH + 0.15), cap, C_WALLS, g, bevel=0.06)
    n = max(1, int(L // 10))
    for i in range(n + 1):
        x = -L / 2 + i * (L / n)
        box(name + "_Pillar", (1.4, WT + 0.7, WH + 0.8), (x, 0, (WH + 0.8) / 2), body, C_WALLS, g, bevel=0.08)
        box(name + "_PillarCap", (1.7, WT + 1.0, 0.35), (x, 0, WH + 0.95), cap, C_WALLS, g, bevel=0.06)
        if th['merlon'] == 'spike':
            cyl(name + "_PillarSpike", 0.4, 1.6, (x, 0, WH + 1.1), M['basalt'], C_WALLS, g, r2=0.0, segs=6)
    m = int(L // 2.5)
    for i in range(m):
        x = -L / 2 + (i + 0.5) * (L / m)
        if th['merlon'] == 'step':
            box(name + "_Merlon", (0.9, WT + 0.1, 0.6), (x, 0, WH + 0.6), body, C_WALLS, g, bevel=0.12)
        elif th['merlon'] == 'spike':
            cyl(name + "_Spike", 0.3, 0.9, (x, 0, WH + 0.3), M['basalt'], C_WALLS, g, r2=0.0, segs=5)
        elif A['theme'] == 'Forest' and i % 2 == 0:
            sphere(name + "_Ivy", 0.8, (x, -0.5, WH - 0.2), M['leaf_a'], C_WALLS, g, scale=(1.3, 0.5, 1.4), subdiv=2, ico=True,
                   jitter=0.2, seed=i)

SIDE_ROT = {'N': 0.0, 'S': math.pi, 'E': -math.pi / 2, 'W': math.pi / 2}

def gate_point(A, side, off):
    hx, hy = A['size'][0] / 2, A['size'][1] / 2
    return {'N': (off, hy), 'S': (off, -hy), 'E': (hx, off), 'W': (-hx, off)}[side]

def build_gate(A, side, off, label, locked):
    th = THEME_WALL[A['theme']]
    body, cap = th['body'](A), th['cap'](A)
    x, y = gate_point(A, side, off)
    n = {'N': (0, 1), 'S': (0, -1), 'E': (1, 0), 'W': (-1, 0)}[side]
    g = empty("Gate_" + ("Exit_" if locked else "Entry_") + label.replace(" ", ""), C_WALLS,
              (x + n[0] * 0.5, y + n[1] * 0.5, 0), (0, 0, SIDE_ROT[side]))
    post_h = 6.0
    orb = M['orb_red'] if locked else M['orb_green']
    for sx in (-1, 1):
        box("GatePost", (1.6, 1.8, post_h), (sx * 2.9, 0, post_h / 2), body, C_WALLS, g, bevel=0.08)
        box("GatePostCap", (2.0, 2.2, 0.4), (sx * 2.9, 0, post_h + 0.2), cap, C_WALLS, g, bevel=0.06)
        cyl("GateOrbStand", 0.35, 0.3, (sx * 2.9, 0, post_h + 0.4), M['iron'], C_WALLS, g)
        sphere("GateOrb", 0.5, (sx * 2.9, 0, post_h + 1.15), orb, C_WALLS, g, subdiv=3)
        li = bpy.data.lights.new("GateOrbLight", 'POINT')
        li.energy = 250
        li.color = (1.0, 0.25, 0.12) if locked else (0.3, 1.0, 0.45)
        place("GateOrbLight", li, C_WALLS, (sx * 2.9, -0.6, post_h + 1.2), parent=g)
        box("Banner", (1.1, 0.05, 2.4), (sx * 2.9, -0.95, post_h - 1.8), M['banner_red'] if locked else mat_flat((0.15, 0.45, 0.6), 0.8),
            C_WALLS, g, bevel=0)
    box("GateLintel", (7.4, 1.6, 1.0), (0, 0, 4.8), body, C_WALLS, g, bevel=0.08)
    box("GateLintelCap", (7.8, 1.9, 0.3), (0, 0, 5.45), cap, C_WALLS, g, bevel=0.05)
    if locked:
        for i in range(5):
            box("GateBar", (0.18, 0.18, 4.2), (-1.6 + i * 0.8, -0.2, 2.1), M['iron'], C_WALLS, g, bevel=0.02)
        for z in (1.2, 2.6, 3.8):
            box("GateRail", (4.2, 0.2, 0.2), (0, -0.25, z), M['iron'], C_WALLS, g, bevel=0.02)
    else:
        cyl("Portal", 1.95, 0.08, (0, 0, 2.05), M['portal'], C_WALLS, g, rot=(math.pi / 2, 0, 0), base=False, segs=40)
        for k in range(24):
            a = k * 2 * math.pi / 24
            box("PortalRing", (0.58, 0.3, 0.22), (math.cos(a) * 2.05, 0, 2.05 + math.sin(a) * 2.05), M['brass'], C_WALLS, g,
                rot=(0, -a + math.pi / 2, 0), bevel=0.03, segs=1)
        box("PortalPad", (4.2, 2.6, 0.04), (0, -1.6, 0.02), M['portal'], C_WALLS, g, bevel=0)
        li = bpy.data.lights.new("PortalLight", 'POINT'); li.energy = 400; li.color = (0.3, 0.85, 1.0)
        place("PortalLight", li, C_WALLS, (0, -1.2, 2.0), parent=g)
    box("GatePlaque", (max(5.4, len(label) * 0.3 + 1.0), 0.15, 0.8), (0, -0.9, 4.8), M['wood'], C_WALLS, g, bevel=0.03)
    text("GateText", label, 0.42, (0, -1.0, 4.78), M['sign_text'], C_WALLS, g)

def build_walls2(A):
    hx, hy = A['size'][0] / 2, A['size'][1] / 2
    o = 0.5
    gaps = {'N': [], 'S': [], 'E': [], 'W': []}
    for side, off, *_ in (A['entry'], A['exit']):
        gaps[side].append(off)
    runs = {'S': ((-hx - o, -hy - o), (hx + o, -hy - o), 0), 'N': ((-hx - o, hy + o), (hx + o, hy + o), 0),
            'W': ((-hx - o, -hy - o), (-hx - o, hy + o), 1), 'E': ((hx + o, -hy - o), (hx + o, hy + o), 1)}
    for side, (a, b, axis) in runs.items():
        cuts = sorted(gaps[side])
        start = a[axis]
        pts = []
        for cpos in cuts:
            pts.append((start, cpos - 3.4))
            start = cpos + 3.4
        pts.append((start, b[axis]))
        for i, (s0, s1) in enumerate(pts):
            if axis == 0:
                wall_run2("Wall%s%d" % (side, i), (s0, a[1]), (s1, a[1]), A)
            else:
                wall_run2("Wall%s%d" % (side, i), (a[0], s0), (a[0], s1), A)
    side, off, label = A['entry']
    build_gate(A, side, off, label, False)
    side, off, label = A['exit']
    build_gate(A, side, off, label, True)

def path_rects(A):
    rects = []
    for (x1, y1), (x2, y2), w in A['paths']:
        if abs(x2 - x1) > abs(y2 - y1):
            rects.append((min(x1, x2) - w / 2, max(x1, x2) + w / 2, y1 - w / 2, y1 + w / 2))
        else:
            rects.append((x1 - w / 2, x1 + w / 2, min(y1, y2) - w / 2, max(y1, y2) + w / 2))
    return rects

def build_paths2(A):
    pm = {'Forest': lambda: mat_gravel(A['path']), 'Desert': lambda: mat_cobble(A['path'], 1.2),
          'Snow': lambda: mat_gravel(A['path']), 'Volcano': lambda: mat_cobble(A['path'], 1.3)}[A['theme']]()
    for i, (x0, x1, y0, y1) in enumerate(path_rects(A)):
        box("Path_%d" % i, (x1 - x0, y1 - y0, 0.08), ((x0 + x1) / 2, (y0 + y1) / 2, 0.002 * i), pm, C_PATHS, bevel=0.04, segs=1)
        if A['theme'] == 'Snow':
            er = random.Random(i)
            for k in range(int((x1 - x0 + y1 - y0) / 3)):
                horiz = (x1 - x0) > (y1 - y0)
                t = er.random()
                for s in (0, 1):
                    px = x0 + t * (x1 - x0) if horiz else (x0 if s == 0 else x1)
                    py = (y0 if s == 0 else y1) if horiz else y0 + t * (y1 - y0)
                    sphere("PathSnowbank", 1.0, (px, py, 0.0), M['snow'], C_PATHS, scale=(er.uniform(0.6, 1.2), er.uniform(0.6, 1.2), 0.3),
                           subdiv=1, ico=True, jitter=0.15, seed=k)

def build_hazards(A):
    for i, h in enumerate(A.get('hazards', [])):
        cx, cy = h['center']
        g = empty("Hazard_%d" % i, C_PATHS, (cx, cy, 0))
        if 'radius' in h:
            r = h['radius']
            cyl("LavaPool", r, 0.1, (0, 0, -0.02), mat_lava(), C_PATHS, g, segs=48)
            hr = random.Random(i)
            for k in range(int(r * 2.2)):
                a = k * 2 * math.pi / int(r * 2.2) + hr.uniform(-0.1, 0.1)
                sphere("LavaRock", hr.uniform(0.6, 1.0), (math.cos(a) * (r + 0.4), math.sin(a) * (r + 0.4), 0.1), M['basalt'], C_PATHS, g,
                       scale=(1.4, 1.1, 0.6), subdiv=1, ico=True, jitter=0.3, do_smooth=False, seed=k + i * 50)
            li = bpy.data.lights.new("LavaGlow", 'POINT'); li.energy = r * 900; li.color = (1.0, 0.35, 0.08); li.shadow_soft_size = r * 0.6
            place("LavaGlow", li, C_PATHS, (0, 0, 2.0), parent=g)
        else:
            sx, sy = h['rect']
            box("LavaStrip", (sx, sy, 0.12), (0, 0, 0.0), mat_lava(), C_PATHS, g, bevel=0)
            for s in (-1, 1):
                hr = random.Random(i + s)
                x = -sx / 2
                while x < sx / 2:
                    sphere("LavaBank", hr.uniform(0.5, 0.9), (x, s * (sy / 2 + 0.3), 0.1), M['basalt'], C_PATHS, g,
                           scale=(1.5, 1.0, 0.55), subdiv=1, ico=True, jitter=0.3, do_smooth=False, seed=int(x * 7) + s)
                    x += hr.uniform(1.2, 2.2)
            x = -sx / 2 + 8
            while x < sx / 2:
                li = bpy.data.lights.new("LavaGlow", 'POINT'); li.energy = 3000; li.color = (1.0, 0.35, 0.08); li.shadow_soft_size = 2.0
                place("LavaGlow", li, C_PATHS, (x, 0, 2.0), parent=g)
                x += 16
            for (bx, by, bw, bh) in h.get('bridges', []):
                bg = empty("Bridge", C_PATHS, (bx + bw / 2, by + bh / 2, 0))
                box("BridgeDeck", (bw, bh, 0.35), (0, 0, 0.18), mat_cobble((0.35, 0.32, 0.3), 1.3), C_PATHS, bg, bevel=0.05)
                for s in (-1, 1):
                    box("BridgeRail", (0.4, bh, 1.0), (s * bw / 2, 0, 0.6), M['basalt'], C_PATHS, bg, bevel=0.06)
                    for t in (-1, 1):
                        cyl("BridgeCap", 0.4, 0.4, (s * bw / 2, t * bh / 2, 1.1), M['basalt'], C_PATHS, bg, segs=8)
                        brazier((s * bw / 2, t * (bh / 2 + 0.9), 0), C_PATHS, bg, M['basalt'], 0.8)

# --------------------------------------------------------------------------- terrain, sky, nature per theme
def build_terrain2(A):
    th = A['theme']
    gm = {'Forest': lambda: M['moss_ground'], 'Desert': lambda: M['sand'], 'Snow': lambda: M['snow'],
          'Volcano': lambda: mat_burnt_ground(A['ground'])}[th]()
    hx, hy = A['size'][0] / 2, A['size'][1] / 2
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=90, y_segments=90, size=520)
    nr = random.Random(A['seed'])
    for v in bm.verts:
        d = max(abs(v.co.x) - hx, abs(v.co.y) - hy)
        if d > 12:
            k = min((d - 12) / 110, 1.0)
            if th == 'Desert':
                z = 5 * math.sin(v.co.x * 0.035 + v.co.y * 0.012) + 4 * math.sin(v.co.y * 0.05) + 6
            elif th == 'Volcano':
                z = 8 + 6 * math.sin(v.co.x * 0.05) * math.cos(v.co.y * 0.04) + nr.uniform(-2.5, 2.5)
            else:
                z = 7 + 9 * math.sin(v.co.x * 0.021) * math.cos(v.co.y * 0.017) + nr.uniform(-1.5, 1.5)
            v.co.z = k * k * z - 0.02
        else:
            v.co.z = -0.02
    me = to_mesh(bm, "Ground")
    smooth(me)
    place("Ground", me, C_TERRAIN, mat=gm)
    hr = random.Random(A['seed'] + 1)
    far = max(hx, hy)
    if th == 'Desert':
        for i in range(12):
            a = hr.uniform(0, 2 * math.pi); d = hr.uniform(far + 200, far + 450)
            r = hr.uniform(40, 90); h = hr.uniform(45, 110)
            ob = cyl("Mesa_%d" % i, r, h, (math.cos(a) * d, math.sin(a) * d, -2), mat_strata((0.7, 0.38, 0.22), (0.86, 0.6, 0.38)),
                     C_TERRAIN, r2=r * 0.8, segs=9)
            ob.data.polygons.foreach_set("use_smooth", [False] * len(ob.data.polygons))
    elif th == 'Volcano':
        cyl("Volcano", 420, 300, (60, 900, -20), M['basalt'], C_TERRAIN, r2=70, segs=14)
        cyl("VolcanoCrater", 66, 4, (60, 900, 278), mat_lava(), C_TERRAIN, segs=14)
        li = bpy.data.lights.new("CraterGlow", 'POINT'); li.energy = 8e6; li.color = (1.0, 0.35, 0.1); li.shadow_soft_size = 40
        place("CraterGlow", li, C_TERRAIN, (60, 900, 330))
        for i in range(10):
            a = hr.uniform(0, 2 * math.pi); d = hr.uniform(far + 150, far + 380)
            ob = cyl("Crag_%d" % i, hr.uniform(25, 60), hr.uniform(40, 120), (math.cos(a) * d, math.sin(a) * d, -5), M['basalt'],
                     C_TERRAIN, r2=hr.uniform(2, 10), segs=7)
            ob.data.polygons.foreach_set("use_smooth", [False] * len(ob.data.polygons))
    else:
        for i, (x, y, r, h) in enumerate([(-700, 1100, 330, 260), (-200, 1250, 380, 330), (350, 1150, 340, 280),
                                          (850, 1000, 300, 230), (-1150, 900, 300, 200), (1000, 300, 300, 220),
                                          (-1000, 200, 300, 220)]):
            if th == 'Snow':
                x, y, r, h = x * 0.6, y * 0.6, r * 0.9, h * 1.2
            ob = sphere("Mountain_%d" % i, 1.0, (x, y, -10), M['mountain'], C_TERRAIN, scale=(r, r * 0.8, h),
                        subdiv=2, ico=True, jitter=0.22, do_smooth=False, seed=300 + i)

def setup_world2(A):
    th = A['theme']
    cfg = {
        'Forest': dict(tint=(0.75, 1.0, 0.85), sky=0.13, sun=3.6, sun_col=(0.95, 1.0, 0.85), elev=34, az=-40, fog=0.0012,
                       fog_col=(0.75, 0.9, 0.8)),
        'Desert': dict(tint=(1.0, 0.92, 0.78), sky=0.15, sun=4.4, sun_col=(1.0, 0.88, 0.68), elev=48, az=-60, fog=0.0,
                       fog_col=(1.0, 0.88, 0.7)),
        'Snow': dict(tint=(0.92, 0.96, 1.05), sky=0.2, sun=3.6, sun_col=(0.88, 0.93, 1.0), elev=26, az=-35, fog=0.0018,
                     fog_col=(0.88, 0.92, 1.0)),
        'Volcano': dict(tint=(0.9, 0.3, 0.2), sky=0.12, sun=2.2, sun_col=(1.0, 0.55, 0.35), elev=20, az=-55, fog=0.0025,
                        fog_col=(0.6, 0.3, 0.25)),
    }[th]
    w = bpy.data.worlds.new("Sky_" + A['name'].replace(" ", ""))
    SC.world = w
    if w.node_tree is None:
        w.use_nodes = True
    nt = w.node_tree
    bg = next(n for n in nt.nodes if n.type == 'BACKGROUND')
    out = next(n for n in nt.nodes if n.type == 'OUTPUT_WORLD')
    sky = nt.nodes.new('ShaderNodeTexSky')
    items = [i.identifier for i in sky.bl_rna.properties['sky_type'].enum_items]
    for pick in ('MULTIPLE_SCATTERING', 'NISHITA', 'SINGLE_SCATTERING'):
        if pick in items:
            sky.sky_type = pick
            break
    for attr, val in (('sun_disc', False), ('sun_elevation', math.radians(cfg['elev'])), ('altitude', 200.0),
                      ('air_density', 1.2), ('dust_density', 3.0 if th == 'Desert' else 1.5)):
        try:
            setattr(sky, attr, val)
        except Exception:
            pass
    tint = nt.nodes.new('ShaderNodeRGB'); tint.outputs[0].default_value = rgba(cfg['tint'])
    nt.links.new(mix_mult(nt, sky.outputs['Color'], tint.outputs[0]), bg.inputs['Color'])
    bg.inputs['Strength'].default_value = cfg['sky']
    if cfg['fog'] > 0:
        # EEVEE world volumes block sun + sky light, so use a bounded fog box instead.
        fm = bpy.data.materials.get('Fog_' + th)
        if fm is None:
            fm, fnt, fb = new_mat('Fog_' + th)
            fout = next(n for n in fnt.nodes if n.type == 'OUTPUT_MATERIAL')
            for l in list(fout.inputs['Surface'].links):
                fnt.links.remove(l)
            vol = fnt.nodes.new('ShaderNodeVolumePrincipled')
            vol.inputs['Density'].default_value = cfg['fog']
            vol.inputs['Color'].default_value = rgba(cfg['fog_col'])
            fnt.links.new(vol.outputs[0], fout.inputs['Volume'])
        fog = box("FogVolume", (900, 900, 140), (0, 150, 69), fm, C_SKY, bevel=0)
        fog.display_type = 'BOUNDS'
        for attr, val in (('volumetric_end', 320.0), ('volumetric_tile_size', '8'), ('volumetric_samples', 32)):
            try:
                setattr(SC.eevee, attr, val)
            except Exception:
                pass
    sun = bpy.data.lights.new("Sun", 'SUN')
    sun.energy = cfg['sun']
    sun.color = cfg['sun_col']
    sun.angle = math.radians(1.5)
    place("Sun", sun, C_SKY, (0, 0, 60), (math.radians(90 - cfg['elev']), 0, math.radians(cfg['az'])))
    if th != 'Volcano':
        cr = random.Random(A['seed'])
        for i in range(8 if th != 'Desert' else 4):
            g = empty("Cloud_%d" % i, C_SKY, (cr.uniform(-320, 320), cr.uniform(180, 480), cr.uniform(70, 130)))
            for k in range(5):
                sphere("Puff", cr.uniform(9, 16), (cr.uniform(-20, 20), cr.uniform(-6, 6), cr.uniform(-3, 5)), M['cloud'], C_SKY, g,
                       scale=(1.4, 1.0, 0.7), subdiv=2, ico=True, jitter=0.08, seed=i * 10 + k)

class Space:
    def __init__(self, A):
        self.A = A
        self.hx, self.hy = A['size'][0] / 2, A['size'][1] / 2
        self.rects = path_rects(A)
        self.circles = []
        for b in A['buildings']:
            self.circles.append((b['pos'], max(b['size']) / 2 + 3.5))
            f = facing_rot(b['facing'])[2]
            dd = door_depth(b) + 3
            self.circles.append(((b['pos'][0] + math.sin(f) * dd, b['pos'][1] - math.cos(f) * dd), 4.5))
        for side, off, _ in (A['entry'], A['exit']):
            self.circles.append((gate_point(A, side, off), 7))
        for h in A.get('hazards', []):
            if 'radius' in h:
                self.circles.append((h['center'], h['radius'] + 2))
            else:
                sx, sy = h['rect']
                cx, cy = h['center']
                self.rects.append((cx - sx / 2, cx + sx / 2, cy - sy / 2 - 1.5, cy + sy / 2 + 1.5))
        self.circles.append((A['start'], 3))
        self.taken = []

    def free(self, x, y, margin=0.0, path_margin=1.5, spacing=0.0):
        if abs(x) > self.hx - 1.5 or abs(y) > self.hy - 1.5:
            return False
        for x0, x1, y0, y1 in self.rects:
            if x0 - path_margin < x < x1 + path_margin and y0 - path_margin < y < y1 + path_margin:
                return False
        for (cx, cy), r in self.circles:
            if math.hypot(x - cx, y - cy) < r + margin:
                return False
        if spacing:
            for (tx, ty) in self.taken:
                if math.hypot(x - tx, y - ty) < spacing:
                    return False
        return True

    def sample(self, rng, n, margin=0.0, path_margin=1.5, spacing=0.0, edge_bias=0.0, tries=6000):
        out = []
        t = 0
        while len(out) < n and t < tries:
            t += 1
            if edge_bias and rng.random() < edge_bias:
                side = rng.randrange(4)
                if side < 2:
                    x = rng.uniform(-self.hx + 2, self.hx - 2); y = (1 if side else -1) * rng.uniform(self.hy - 7, self.hy - 2)
                else:
                    y = rng.uniform(-self.hy + 2, self.hy - 2); x = (1 if side == 2 else -1) * rng.uniform(self.hx - 7, self.hx - 2)
            else:
                x, y = rng.uniform(-self.hx, self.hx), rng.uniform(-self.hy, self.hy)
            if self.free(x, y, margin, path_margin, spacing):
                out.append((x, y))
                if spacing:
                    self.taken.append((x, y))
        return out

def source(fn, name):
    """Build a throwaway object with fn, keep its mesh as an instancing source."""
    ob = fn()
    me = ob.data
    me.name = name
    bpy.data.objects.remove(ob)
    return me

def merged_mesh(name, parts):
    """parts: list of (bmesh-builder fn(bm) returning verts, material index); materials list via return."""
    bm = bmesh.new()
    for fn, mi in parts:
        verts = fn(bm)
        faces = set(f for v in verts for f in v.link_faces)
        for f in faces:
            f.material_index = mi
    me = to_mesh(bm, name)
    box_uv(me)
    return me

def cone_part(r1, r2, h, z, segs=10, off=(0, 0)):
    def fn(bm):
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=segs, radius1=r1, radius2=r2, depth=h)
        for v in res['verts']:
            v.co += Vector((off[0], off[1], z + h / 2))
        return res['verts']
    return fn

def blob_part(r, center, jitter=0.15, seed=0, scale=(1, 1, 1)):
    def fn(bm):
        res = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=r)
        jr = random.Random(seed)
        for v in res['verts']:
            v.co += v.co.normalized() * jr.uniform(-jitter, jitter) * r
            v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2])) + Vector(center)
        return res['verts']
    return fn

def scatter(coll_, meshes, points, rng, z=0.0, smin=0.8, smax=1.3, name="Inst", tilt=0.0):
    for x, y in points:
        s = rng.uniform(smin, smax)
        place(name, rng.choice(meshes), coll_, (x, y, z), (rng.uniform(-tilt, tilt), rng.uniform(-tilt, tilt), rng.uniform(0, 6.28)),
              scale=(s, s, s))

def build_nature2(A):
    th = A['theme']
    rng = random.Random(A['seed'] * 3)
    S = Space(A)
    hx, hy = S.hx, S.hy
    out = coll("OutsideScenery", C_NATURE)

    def outside_points(n, near=14, far=170):
        pts = []
        while len(pts) < n:
            a = rng.uniform(0, 2 * math.pi)
            d = rng.uniform(near, far)
            x, y = math.cos(a) * (hx + d), math.sin(a) * (hy + d)
            ok = True
            for side, off, _ in (A['entry'], A['exit']):
                gx, gy = gate_point(A, side, off)
                if math.hypot(x - gx, y - gy) < 16:
                    ok = False
            if ok:
                pts.append((x, y))
        return pts

    if th == 'Forest':
        trees = []
        for k in range(3):
            parts = [(cone_part(0.9, 0.45, 6.5, 0, 12), 0)]
            jr = random.Random(500 + k)
            for j in range(6):
                ang = jr.uniform(0, 6.28)
                c = (math.cos(ang) * jr.uniform(1.0, 2.6), math.sin(ang) * jr.uniform(1.0, 2.6), 7.0 + jr.uniform(-0.5, 2.5))
                parts.append((blob_part(jr.uniform(2.2, 3.2), c if j else (0, 0, 8.0), seed=j + k * 10), 1))
            me = merged_mesh("AncientTree%d" % k, parts)
            me.materials.append(M['trunk']); me.materials.append([M['leaf_c'], M['leaf_a'], M['leaf_c']][k])
            trees.append(me)
        fern = merged_mesh("Fern", [(cone_part(0.25, 0.0, 1.4, 0, 3), 0)])
        fern_parts = []
        for j in range(7):
            a = j * 0.9
            def fn(bm, a=a):
                res = bmesh.ops.create_cone(bm, cap_ends=False, segments=3, radius1=0.22, radius2=0.0, depth=1.3)
                m = Matrix.Rotation(a, 4, 'Z') @ Matrix.Translation((0.55, 0, 0.35)) @ Matrix.Rotation(math.radians(65), 4, 'Y') \
                    @ Matrix.Scale(0.25, 4, Vector((1, 0, 0)))
                bmesh.ops.transform(bm, matrix=m, verts=res['verts'])
                return res['verts']
            fern_parts.append((fn, 0))
        fern = merged_mesh("FernMesh", fern_parts); fern.materials.append(M['leaf_a'])
        shroom = merged_mesh("Toadstool", [(cone_part(0.09, 0.07, 0.35, 0, 8), 0), (blob_part(0.28, (0, 0, 0.38), 0.0, scale=(1, 1, 0.45)), 1)])
        shroom.materials.append(M['trim_white']); shroom.materials.append(M['mush_red'])
        glow = merged_mesh("GlowShroom", [(cone_part(0.06, 0.05, 0.3, 0, 8), 0), (blob_part(0.2, (0, 0, 0.32), 0.0, scale=(1, 1, 0.5)), 1)])
        glow.materials.append(M['trim_white']); glow.materials.append(M['mush_glow'])
        logm = source(lambda: cyl("Log", 0.55, 4.5, (0, 0, 0.45), M['trunk'], C_NATURE, rot=(0, math.pi / 2, 0), base=False, segs=12), "LogMesh")
        scatter(C_NATURE, trees, S.sample(rng, 22, 3.0, 2.5, 9.0, edge_bias=0.75), rng, smin=0.85, smax=1.25, name="AncientTree")
        scatter(out, trees, outside_points(320, 10, 160), rng, smin=1.0, smax=1.6, name="ForestTree")
        scatter(C_NATURE, [fern], S.sample(rng, 160, 0.0, 0.6), rng, smin=0.7, smax=1.4, name="Fern")
        scatter(C_NATURE, [shroom], S.sample(rng, 40, 0.5, 0.8), rng, smin=0.6, smax=1.4, name="Toadstool")
        pts = S.sample(rng, 30, 0.5, 0.8)
        for (x, y) in pts:
            for k in range(rng.randint(2, 5)):
                place("GlowShroom", glow, C_NATURE, (x + rng.uniform(-0.6, 0.6), y + rng.uniform(-0.6, 0.6), 0),
                      (0, 0, rng.uniform(0, 6)), scale=(rng.uniform(0.7, 1.4),) * 3)
        scatter(C_NATURE, [logm], S.sample(rng, 7, 1.5, 2.0, 6.0), rng, smin=0.8, smax=1.2, name="FallenLog")
        add_ground_cover(A, S, rng, M['blade'], 700)
        fireflies(A, S, rng, M['firefly'], 160, (0.5, 4.0), lights=10, light_col=(0.8, 1.0, 0.4))
    elif th == 'Desert':
        cacti = []
        for k in range(3):
            jr = random.Random(700 + k)
            h = jr.uniform(3.5, 5.5)
            parts = [(cone_part(0.45, 0.42, h, 0, 12), 0), (blob_part(0.43, (0, 0, h), 0.0), 0)]
            for s in (-1, 1):
                if jr.random() < 0.8:
                    z = jr.uniform(1.4, h - 1.5); L = jr.uniform(0.7, 1.0); up = jr.uniform(1.0, 1.8)
                    def arm(bm, s=s, z=z, L=L):
                        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.3, radius2=0.3, depth=L)
                        m = Matrix.Translation((s * L / 2, 0, z)) @ Matrix.Rotation(math.pi / 2, 4, 'Y')
                        bmesh.ops.transform(bm, matrix=m, verts=res['verts'])
                        return res['verts']
                    parts.append((arm, 0))
                    parts.append((cone_part(0.3, 0.28, up, z, 10, (s * L, 0)), 0))
                    parts.append((blob_part(0.29, (s * L, 0, z + up), 0.0), 0))
            me = merged_mesh("Saguaro%d" % k, parts); me.materials.append(M['cactus'])
            for p in me.polygons:
                p.use_smooth = True
            cacti.append(me)
        rockm = [source(lambda k=k: sphere("SR", 1.2, (0, 0, 0), mat_noise((0.75, 0.5, 0.3), 'SandRock', bump=0.5), C_NATURE,
                                          scale=(1.4, 1.0, 0.8), subdiv=1, ico=True, jitter=0.3, do_smooth=False, seed=k), "SandRock%d" % k)
                 for k in range(4)]
        brush = source(lambda: sphere("DB", 0.6, (0, 0, 0), mat_flat((0.55, 0.45, 0.25), 0.9), C_NATURE, scale=(1, 1, 0.7),
                                      subdiv=1, ico=True, jitter=0.35, do_smooth=False), "DryBrush")
        scatter(C_NATURE, cacti, S.sample(rng, 30, 1.5, 2.0, 6.0, edge_bias=0.5), rng, smin=0.8, smax=1.3, name="Cactus")
        scatter(C_NATURE, rockm, S.sample(rng, 28, 1.0, 1.0, 3.0), rng, z=0.1, smin=0.4, smax=1.6, name="SandRock")
        scatter(C_NATURE, [brush], S.sample(rng, 50, 0.3, 0.8), rng, smin=0.6, smax=1.3, name="DryBrush")
        scatter(out, cacti + rockm, outside_points(140, 10, 170), rng, smin=1.0, smax=2.0, name="DesertFar")
        # oasis pond with palms between the two shops
        oasis(C_NATURE, (-36, 0), rng)
    elif th == 'Snow':
        pines = []
        for k in range(2):
            parts = [(cone_part(0.3, 0.2, 1.4, 0, 8), 0)]
            for (r, z, h) in [(2.6, 1.0, 3.0), (2.0, 2.8, 2.7), (1.45, 4.4, 2.4), (0.85, 5.8, 1.8)]:
                parts.append((cone_part(r, 0.05, h, z, 9 + k), 1))
                parts.append((cone_part(r * 0.72, 0.04, h * 0.42, z + h * 0.42, 9 + k), 2))
            me = merged_mesh("SnowPine%d" % k, parts)
            me.materials.append(M['trunk']); me.materials.append(M['leaf_c']); me.materials.append(M['snow'])
            pines.append(me)
        rocks = [merged_mesh("SnowRock%d" % k, [(blob_part(1.0, (0, 0, 0), 0.3, seed=k, scale=(1.4, 1.0, 0.75)), 0),
                                                 (blob_part(0.85, (0, 0, 0.32), 0.1, seed=k + 9, scale=(1.35, 0.95, 0.4)), 1)])
                 for k in range(3)]
        for r in rocks:
            r.materials.append(M['rock']); r.materials.append(M['snow'])
        crystal = merged_mesh("IceCluster", [(cone_part(0.35, 0.0, 2.2, 0, 6), 0), (cone_part(0.25, 0.0, 1.4, 0, 6, (0.5, 0.2)), 0),
                                              (cone_part(0.22, 0.0, 1.2, 0, 6, (-0.4, 0.3)), 0)])
        crystal.materials.append(mat_ice())
        drift = source(lambda: sphere("Dr", 1.6, (0, 0, 0), M['snow'], C_NATURE, scale=(1.4, 1.0, 0.35), subdiv=2, ico=True, jitter=0.1), "Drift")
        scatter(C_NATURE, pines, S.sample(rng, 34, 2.5, 2.0, 6.5, edge_bias=0.8), rng, smin=0.8, smax=1.35, name="SnowPine")
        scatter(out, pines, outside_points(320, 8, 170), rng, smin=1.0, smax=1.8, name="FarPine")
        scatter(C_NATURE, rocks, S.sample(rng, 20, 1.0, 1.0, 3.0), rng, z=0.1, smin=0.5, smax=1.3, name="SnowRock")
        scatter(C_NATURE, [crystal], S.sample(rng, 14, 1.0, 1.5, 4.0), rng, smin=0.7, smax=1.5, name="IceCluster", tilt=0.2)
        scatter(C_NATURE, [drift], S.sample(rng, 40, 0.5, 1.0), rng, smin=0.6, smax=1.5, name="SnowDrift")
        snowman(C_NATURE, (-2.0, -40.0), 0.6)
        frozen_pond(C_NATURE, (-30, -42), 7)
        fireflies(A, S, rng, M['snowflake'], 900, (0.5, 28.0), lights=0, size=0.06, name="Snowflake")
    elif th == 'Volcano':
        cols = []
        for k in range(3):
            jr = random.Random(900 + k)
            parts = []
            for j in range(jr.randint(4, 7)):
                a = jr.uniform(0, 6.28); d = jr.uniform(0, 1.2)
                parts.append((cone_part(0.55, 0.55, jr.uniform(1.0, 4.5), 0, 6, (math.cos(a) * d, math.sin(a) * d)), 0))
            me = merged_mesh("BasaltCols%d" % k, parts); me.materials.append(M['basalt'])
            cols.append(me)
        dead = []
        for k in range(2):
            jr = random.Random(950 + k)
            parts = [(cone_part(0.35, 0.12, 5.0, 0, 8), 0)]
            for j in range(4):
                z = jr.uniform(2.0, 4.2); a = jr.uniform(0, 6.28); L = jr.uniform(1.2, 2.2)
                def br(bm, z=z, a=a, L=L):
                    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.12, radius2=0.03, depth=L)
                    m = Matrix.Translation((0, 0, z)) @ Matrix.Rotation(a, 4, 'Z') @ Matrix.Rotation(math.radians(55), 4, 'Y') \
                        @ Matrix.Translation((0, 0, L / 2))
                    bmesh.ops.transform(bm, matrix=m, verts=res['verts'])
                    return res['verts']
                parts.append((br, 0))
            me = merged_mesh("DeadTree%d" % k, parts); me.materials.append(M['charred'])
            dead.append(me)
        obs = [source(lambda k=k: sphere("Ob", 1.0, (0, 0, 0), M['obsidian'], C_NATURE, scale=(1.3, 1.0, 0.9), subdiv=1, ico=True,
                                         jitter=0.3, do_smooth=False, seed=k), "Obsidian%d" % k) for k in range(3)]
        shard = merged_mesh("EmberShards", [(cone_part(0.3, 0.0, 1.8, 0, 5), 0), (cone_part(0.22, 0.0, 1.2, 0, 5, (0.4, 0.1)), 0)])
        shard.materials.append(mat_emit((1.0, 0.25, 0.05), 5))
        scatter(C_NATURE, cols, S.sample(rng, 18, 2.0, 2.0, 7.0, edge_bias=0.7), rng, smin=0.8, smax=1.6, name="BasaltColumns")
        scatter(C_NATURE, dead, S.sample(rng, 16, 1.5, 1.5, 6.0), rng, smin=0.8, smax=1.3, name="DeadTree")
        scatter(C_NATURE, obs, S.sample(rng, 26, 0.8, 1.0, 3.0), rng, z=0.1, smin=0.4, smax=1.4, name="ObsidianRock")
        scatter(C_NATURE, [shard], S.sample(rng, 14, 1.0, 1.0, 4.0), rng, smin=0.7, smax=1.4, name="EmberShard", tilt=0.25)
        scatter(out, cols + obs, outside_points(150, 8, 170), rng, smin=1.5, smax=3.5, name="FarRock")
        fireflies(A, S, rng, M['ember'], 260, (0.3, 14.0), lights=0, size=0.05, name="Ember")
    return S

def add_ground_cover(A, S, rng, mat, n):
    bm = bmesh.new()
    gr = random.Random(77)
    for i in range(9):
        res = bmesh.ops.create_cone(bm, cap_ends=False, segments=3, radius1=0.05, radius2=0.0, depth=gr.uniform(0.35, 0.6))
        m = Matrix.Translation((gr.uniform(-0.18, 0.18), gr.uniform(-0.18, 0.18), 0.2)) @ \
            Matrix.Rotation(gr.uniform(-0.4, 0.4), 4, 'X') @ Matrix.Rotation(gr.uniform(-0.4, 0.4), 4, 'Y')
        bmesh.ops.transform(bm, matrix=m, verts=res['verts'])
    tuft = to_mesh(bm, "GrassTuft")
    tuft.materials.append(mat)
    c = coll("GroundCover", C_NATURE)
    for x, y in S.sample(rng, n, -1.5, 0.3, tries=n * 6):
        place("Tuft", tuft, c, (x, y, 0), (0, 0, rng.uniform(0, 6)), scale=(rng.uniform(0.7, 1.6),) * 3)

def fireflies(A, S, rng, mat, n, zr, lights=0, light_col=(1, 1, 1), size=0.07, name="Firefly"):
    c = coll(name + "s", C_NATURE)
    me = source(lambda: sphere("FF", size, (0, 0, 0), mat, c, subdiv=1, ico=True), name + "Mesh")
    for i in range(n):
        x, y = rng.uniform(-S.hx, S.hx), rng.uniform(-S.hy, S.hy)
        place(name, me, c, (x, y, rng.uniform(*zr)))
    for i in range(lights):
        x, y = rng.uniform(-S.hx * 0.9, S.hx * 0.9), rng.uniform(-S.hy * 0.9, S.hy * 0.9)
        li = bpy.data.lights.new(name + "Light", 'POINT'); li.energy = 60; li.color = light_col; li.shadow_soft_size = 0.05
        place(name + "Light", li, c, (x, y, rng.uniform(1.0, 3.0)))

def oasis(c, center, rng):
    g = empty("Oasis", c, (center[0], center[1], 0))
    cyl("OasisWater", 4.2, 0.06, (0, 0, 0.01), plain('OasisWater', (0.1, 0.45, 0.5), 0.03, coat=1.0), c, g, segs=40)
    cyl("OasisShore", 4.8, 0.04, (0, 0, 0), mat_noise((0.55, 0.45, 0.3), 'WetSand'), c, g, segs=40)
    for k in range(10):
        a = k * 0.628
        sphere("OasisReed", 0.4, (math.cos(a) * 4.4, math.sin(a) * 4.4, 0.2), M['palm_leaf'], c, g, scale=(0.6, 0.6, 1.6), subdiv=1,
               ico=True, jitter=0.3, do_smooth=False, seed=k)
    for k, a in enumerate((0.6, 2.4, 4.1)):
        palm(c, g, (math.cos(a) * 5.6, math.sin(a) * 5.6, 0), rng.uniform(0, 6.28), k)

def palm(c, parent, loc, yaw, seed):
    g = empty("Palm", c, loc, (0, 0, yaw), parent)
    jr = random.Random(seed)
    p = Vector((0, 0, 0))
    lean = jr.uniform(0.08, 0.16)
    for i in range(8):
        cyl("PalmSeg", 0.3 - i * 0.015, 0.9, tuple(p), M['trunk'], c, g, r2=0.24 - i * 0.015, segs=10, rot=(0, lean * i * 0.4, 0))
        p += Vector((math.sin(lean * i * 0.4) * 0.85, 0, math.cos(lean * i * 0.4) * 0.85))
    for k in range(8):
        a = k * 2 * math.pi / 8
        fl = empty("Frond", c, tuple(p), (0, 0, a), g)
        cyl("FrondLeaf", 0.45, 3.4, (0, 0, 0), M['palm_leaf'], c, fl, r2=0.02, segs=3, rot=(0, math.radians(68 + jr.uniform(-10, 15)), 0))
    for k in range(3):
        sphere("Coconut", 0.17, (p.x + math.cos(k * 2.1) * 0.25, math.sin(k * 2.1) * 0.25, p.z - 0.25), M['trunk'], c, g, subdiv=2)

def snowman(c, loc, yaw):
    g = empty("Snowman", c, (loc[0], loc[1], 0), (0, 0, yaw))
    for r, z in ((0.8, 0.65), (0.6, 1.75), (0.42, 2.55)):
        sphere("SnowBall", r, (0, 0, z), M['snow'], c, g, subdiv=3)
    cyl("CarrotNose", 0.08, 0.45, (0, -0.4, 2.55), M['carrot'], c, g, r2=0.0, rot=(math.pi / 2, 0, 0), segs=8)
    for sx in (-1, 1):
        sphere("Coal", 0.05, (sx * 0.14, -0.38, 2.68), M['eye'], c, g, subdiv=1)
        cyl("StickArm", 0.04, 1.1, (sx * 0.5, 0, 1.85), M['trunk'], c, g, rot=(0, sx * math.radians(60), 0), segs=6)
    cyl("Hat", 0.3, 0.45, (0, 0, 2.85), M['iron'], c, g, segs=16)
    cyl("HatBrim", 0.45, 0.05, (0, 0, 2.85), M['iron'], c, g, segs=16)
    cyl("Scarf", 0.48, 0.15, (0, 0, 2.1), M['scarf'], c, g, segs=16)

def frozen_pond(c, center, r):
    g = empty("FrozenPond", c, (center[0], center[1], 0))
    cyl("PondIce", r, 0.06, (0, 0, 0.01), plain('PondIce', (0.6, 0.78, 0.9), 0.05, coat=1.0), c, g, segs=48)
    for k in range(14):
        a = k * 2 * math.pi / 14
        sphere("PondBank", 1.0, (math.cos(a) * (r + 0.2), math.sin(a) * (r + 0.2), 0), M['snow'], c, g, scale=(1.4, 1.0, 0.4),
               subdiv=2, ico=True, jitter=0.15, seed=k)

def build_lamps(A, S):
    th = A['theme']
    (x1, y1), (x2, y2), w = max(A['paths'], key=lambda p: abs(p[1][0] - p[0][0]) + abs(p[1][1] - p[0][1]))
    L = math.hypot(x2 - x1, y2 - y1)
    dx, dy = (x2 - x1) / L, (y2 - y1) / L
    nx, ny = -dy, dx
    t = 6.0
    while t < L - 4:
        for s in (-1, 1):
            x = x1 + dx * t + nx * s * (w / 2 + 1.6)
            y = y1 + dy * t + ny * s * (w / 2 + 1.6)
            ok = all(math.hypot(x - cx, y - cy) > r - 2 for (cx, cy), r in S.circles)
            if not ok or abs(x) > S.hx - 2 or abs(y) > S.hy - 2:
                continue
            if th in ('Desert', 'Volcano'):
                brazier((x, y, 0), C_PROPS, None, M['basalt'] if th == 'Volcano' else mat_plaster(A['wall']))
            else:
                g = empty("LampPost", C_PROPS, (x, y, 0), (0, 0, math.atan2(-s * ny, -s * nx)))
                cyl("LampBase", 0.28, 0.5, (0, 0, 0), M['iron'], C_PROPS, g, r2=0.18, segs=12)
                cyl("LampPole", 0.1, 4.0, (0, 0, 0.5), M['iron'] if th == 'Snow' else M['wood'], C_PROPS, g, segs=12)
                box("LampArm", (0.9, 0.1, 0.1), (0.4, 0, 4.4), M['iron'], C_PROPS, g, bevel=0)
                lamp_head(g, C_PROPS, (0.8, 0, 3.95))
                if th == 'Snow':
                    snow_cap("LampSnow", (0.8, 0, 4.3), (0.35, 0.35, 0.12), g, C_PROPS)
        t += 13.0

# --------------------------------------------------------------------------- creatures
def slime_c(loc, s, name, mat, yaw=math.pi):
    g = empty(name, C_CHARS, loc, (0, 0, yaw))
    sphere("SlimeBody", 0.75 * s, (0, 0, 0.5 * s), mat, C_CHARS, g, scale=(1.0, 1.0, 0.72), subdiv=4)
    sphere("SlimeTop", 0.3 * s, (0, 0, 1.0 * s), mat, C_CHARS, g, scale=(1, 1, 0.9), subdiv=3)
    for sx in (-1, 1):
        sphere("SlimeEyeW", 0.14 * s, (sx * 0.24 * s, -0.62 * s, 0.62 * s), M['eye_white'], C_CHARS, g, subdiv=2)
        sphere("SlimeEye", 0.07 * s, (sx * 0.24 * s, -0.73 * s, 0.62 * s), M['eye'], C_CHARS, g, subdiv=2)
    return g

def charger_c(loc, yaw, mat, s=1.0, name="Charger"):
    g = empty(name, C_CHARS, loc, (0, 0, yaw))
    g.scale = (s, s, s)
    sphere("Body", 0.9, (0, 0, 0.95), mat, C_CHARS, g, scale=(1.0, 1.25, 0.85), subdiv=4)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl("Leg", 0.16, 0.5, (sx * 0.5, sy * 0.5, 0), mat, C_CHARS, g, segs=10)
        cyl("Horn", 0.16, 0.7, (sx * 0.45, -1.0, 1.35), M['horn'], C_CHARS, g, r2=0.0, rot=(math.radians(-60), sx * 0.3, 0), segs=12)
        sphere("EyeW", 0.11, (sx * 0.3, -1.08, 1.05), M['eye_white'], C_CHARS, g, subdiv=2)
        sphere("Pupil", 0.06, (sx * 0.3, -1.17, 1.05), M['eye'], C_CHARS, g, subdiv=2)
    return g

def spitter_c(loc, yaw, mat, name="Spitter"):
    g = empty(name, C_CHARS, loc, (0, 0, yaw))
    sphere("Body", 0.7, (0, 0, 1.0), mat, C_CHARS, g, scale=(0.9, 0.9, 1.25), subdiv=4)
    cyl("Spout", 0.2, 0.7, (0, -0.55, 1.15), mat, C_CHARS, g, r2=0.28, rot=(math.pi / 2, 0, 0), segs=14)
    cyl("SpoutHole", 0.16, 0.05, (0, -1.25, 1.15), M['eye'], C_CHARS, g, rot=(math.pi / 2, 0, 0), segs=14)
    for sx in (-1, 1):
        cyl("Stalk", 0.05, 0.5, (sx * 0.25, -0.2, 1.75), mat, C_CHARS, g, rot=(0, sx * 0.3, 0), segs=6)
        sphere("EyeW", 0.13, (sx * 0.38, -0.25, 2.25), M['eye_white'], C_CHARS, g, subdiv=2)
        sphere("Pupil", 0.06, (sx * 0.38, -0.36, 2.25), M['eye'], C_CHARS, g, subdiv=2)
        for sy in (-1, 1):
            cyl("Leg", 0.1, 0.6, (sx * 0.45, sy * 0.4, 0), mat, C_CHARS, g, r2=0.14, segs=8)
    return g

def brute_c(loc, yaw, fur, skin, s=1.0, name="Brute", eyes=None, crown=False):
    g = empty(name, C_CHARS, loc, (0, 0, yaw))
    g.scale = (s, s, s)
    for sx in (-1, 1):
        cyl("Leg", 0.32, 1.0, (sx * 0.45, 0, 0), fur, C_CHARS, g, r2=0.36, segs=12)
        cyl("Arm", 0.28, 1.5, (sx * 1.15, -0.1, 0.55), fur, C_CHARS, g, r2=0.36, rot=(0, sx * 0.15, 0), segs=12)
        sphere("Fist", 0.36, (sx * 1.2, -0.1, 0.5), skin, C_CHARS, g, subdiv=2)
    sphere("Torso", 1.0, (0, 0, 1.75), fur, C_CHARS, g, scale=(1.15, 0.85, 1.0), subdiv=3, ico=True, jitter=0.06)
    sphere("Head", 0.5, (0, -0.35, 2.75), fur, C_CHARS, g, subdiv=3)
    sphere("Face", 0.36, (0, -0.62, 2.7), skin, C_CHARS, g, scale=(1, 0.6, 1), subdiv=3)
    em = eyes or M['eye']
    for sx in (-1, 1):
        sphere("Eye", 0.07, (sx * 0.14, -0.85, 2.8), em, C_CHARS, g, subdiv=2)
        cyl("Horn", 0.12, 0.6, (sx * 0.35, -0.3, 3.1), M['horn'] if not crown else M['gold'], C_CHARS, g, r2=0.0,
            rot=(0, sx * 0.5, 0), segs=8)
    if crown:
        for k in range(7):
            a = k * 2 * math.pi / 7
            cyl("CrownSpike", 0.1, 0.5, (math.cos(a) * 0.38, -0.35 + math.sin(a) * 0.38, 3.15), M['gold'], C_CHARS, g, r2=0.0, segs=6)
        sphere("ChestGem", 0.25, (0, -0.85, 1.9), mat_emit((1.0, 0.2, 0.05), 6), C_CHARS, g, subdiv=2)
        for sx in (-1, 1):
            cyl("ShoulderSpike", 0.2, 0.9, (sx * 1.0, 0, 2.5), M['iron'], C_CHARS, g, r2=0.0, rot=(0, sx * 0.6, 0), segs=8)
    return g

def build_characters2(A):
    sx, sy = A['start']
    tx, ty = A['look_at']
    yaw = math.atan2(ty - sy, tx - sx) - math.pi / 2
    build_player((sx, sy, 0), yaw)
    th = A['theme']
    if th == 'Forest':
        gm = plain('SlimeForest', (0.35, 0.85, 0.3), 0.12, coat=0.8)
        slime_c((8, -12, 0), 1.0, "Slime_A", gm); slime_c((10.5, -9.5, 0), 0.7, "Slime_B", gm, 2.6)
        charger_c((20, 21, 0), math.radians(120), plain('BeetleShell', (0.1, 0.25, 0.12), 0.2, coat=1.0), 1.0, "Beetle")
    elif th == 'Desert':
        sm = plain('SpitterSand', (0.85, 0.55, 0.25), 0.4)
        spitter_c((10, -4, 0), math.radians(200), sm, "Spitter_A"); spitter_c((12, -18, 0), math.radians(150), sm, "Spitter_B")
        charger_c((6, 30, 0), math.radians(90), plain('ScorpionRed', (0.7, 0.25, 0.12), 0.3, coat=0.5), 0.9, "Charger")
    elif th == 'Snow':
        brute_c((28, 22, 0), math.radians(160), plain('YetiFur', (0.92, 0.94, 0.98), 0.9), plain('YetiFace', (0.55, 0.65, 0.8), 0.6),
                1.0, "Brute_Yeti")
        bm = plain('SlimeIce', (0.45, 0.75, 1.0), 0.1, coat=1.0)
        slime_c((4, 8, 0), 1.0, "Slime_Ice_A", bm, 3.6); slime_c((7, 11, 0), 0.7, "Slime_Ice_B", bm, 2.6)
    elif th == 'Volcano':
        brute_c((-9, 45, 0), math.radians(-120), plain('BossHide', (0.12, 0.08, 0.1), 0.5), plain('BossFace', (0.3, 0.1, 0.08), 0.5),
                1.8, "Boss", eyes=mat_emit((1.0, 0.15, 0.05), 12), crown=True)
        mm = plain('SlimeMagma', (1.0, 0.35, 0.08), 0.3, emit=(1.0, 0.3, 0.05), emit_strength=1.5)
        slime_c((12, -12, 0), 1.0, "Slime_Magma_A", mm, 3.8); slime_c((14, -16, 0), 0.75, "Slime_Magma_B", mm, 2.8)

def build_cameras2(A):
    hx, hy = A['size'][0] / 2, A['size'][1] / 2
    m = max(hx, hy)
    prefix = A['name'].split()[0] + "_"

    def cam(name, loc, target, lens):
        name = prefix + name
        cd = bpy.data.cameras.new(name)
        cd.lens = lens
        cd.clip_end = 4000
        ob = place(name, cd, C_SKY, loc)
        aim(ob, target)
        return ob
    hero = cam("Cam_Hero", (hx * 0.45, -hy - m * 0.95, m * 0.72), (-hx * 0.04, hy * 0.06, 0), 30 if hx <= hy * 1.2 else 26)
    sx, sy = A['start']
    tx, ty = A['look_at']
    d = Vector((tx - sx, ty - sy, 0)).normalized()
    back = 10.5
    while back > 3 and (abs(sx - d.x * back) > hx - 1.5 or abs(sy - d.y * back) > hy - 1.5):
        back -= 0.5
    cam("Cam_ThirdPerson", (sx - d.x * back, sy - d.y * back, 3.0 + back * 0.2), (sx + d.x * 18, sy + d.y * 18, 2.2),
        32 if back > 8 else 24)
    for b in A['buildings']:
        f = facing_rot(b['facing'])[2]
        fx, fy = math.sin(f), -math.cos(f)
        dist = door_depth(b) + 13 + max(b['size']) * 0.4
        side = Vector((-fy, fx)) * 5
        bx, by = b['pos']
        cam("Cam_" + b['id'], (bx + fx * dist + side.x, by + fy * dist + side.y, 5.5), (bx + fx * 2, by + fy * 2, 4.0), 30)
    SC.camera = hero

# --------------------------------------------------------------------------- area data (AreaDatabase.cs)
def B(id, name, type, style, pos, size, facing, body, roof, trim=(0.35, 0.22, 0.12)):
    return dict(id=id, name=name, type=type, style=style, pos=pos, size=size, facing=facing, body=body, roof=roof, trim=trim)

AREAS = [
    dict(name="Whispering Grove", theme='Forest', size=(130, 90), seed=23,
         ground=(0.25, 0.5, 0.2), wall=(0.35, 0.45, 0.3), path=(0.45, 0.35, 0.25),
         entry=('S', 0, "Dusty Crossroads"), exit=('E', 15, "Sunscorch Dunes"), start=(0, -38), look_at=(0, 15),
         paths=[((0, -45), (0, 15), 6), ((-30, 15), (65, 15), 6), ((25, 15), (25, 25), 4), ((0, -18), (34, -18), 4),
                ((-13, -25), (0, -25), 4)],
         buildings=[B('herbalist_hut', "Herbalist's Hut", 'Shop', 'Mushroom', (-30, 22), (10, 10), 0, (0.95, 0.9, 0.78), (0.85, 0.15, 0.12)),
                    B('ranger_outpost', "Ranger Outpost", 'Shop', 'Tower', (-18, -25), (9, 9), -90, (0.5, 0.38, 0.25), (0.2, 0.4, 0.2)),
                    B('hollow_stump', "Hollow Stump", 'Battle', 'Stump', (25, 32), (14, 14), 0, (0.45, 0.3, 0.18), (0.75, 0.6, 0.4)),
                    B('beetle_den', "Beetle Den", 'Battle', 'Dome', (40, -18), (12, 12), 90, (0.3, 0.45, 0.22), (0.2, 0.3, 0.15))]),
    dict(name="Sunscorch Dunes", theme='Desert', size=(90, 140), seed=37,
         ground=(0.85, 0.72, 0.45), wall=(0.7, 0.45, 0.3), path=(0.7, 0.6, 0.45),
         entry=('W', -40, "Whispering Grove"), exit=('N', 15, "Frostpeak Hollow"), start=(-38, -40), look_at=(0, -40),
         paths=[((-45, -40), (0, -40), 6), ((0, -40), (0, 40), 6), ((0, 40), (15, 40), 6), ((15, 40), (15, 70), 6),
                ((-17, 15), (0, 15), 4), ((-17, -15), (0, -15), 4), ((0, -10), (16, -10), 4), ((-11, 52), (15, 52), 4)],
         buildings=[B('sunset_bazaar', "Sunset Bazaar", 'Shop', 'Tent', (-22, 15), (14, 10), -90, (0.9, 0.3, 0.25), (0.95, 0.85, 0.4)),
                    B('oasis_apothecary', "Oasis Apothecary", 'Shop', 'Dome', (-22, -15), (10, 10), -90, (0.92, 0.85, 0.7), (0.3, 0.6, 0.75)),
                    B('scorpion_pit', "Scorpion Pit", 'Battle', 'Arena', (24, -10), (16, 16), 90, (0.8, 0.65, 0.42), (0.6, 0.2, 0.15)),
                    B('sun_colosseum', "Sun Colosseum", 'Battle', 'Arena', (-20, 52), (18, 18), -90, (0.9, 0.82, 0.62), (0.85, 0.65, 0.15))]),
    dict(name="Frostpeak Hollow", theme='Snow', size=(110, 110), seed=41,
         ground=(0.92, 0.95, 1.0), wall=(0.8, 0.85, 0.9), path=(0.65, 0.7, 0.75),
         entry=('S', 15, "Sunscorch Dunes"), exit=('W', 20, "Molten Citadel"), start=(15, -48), look_at=(15, 20),
         paths=[((15, -55), (15, 20), 6), ((35, 20), (-55, 20), 6), ((15, -20), (29, -20), 4), ((-10, -20), (15, -20), 4),
                ((-20, 20), (-20, 34), 4), ((35, 20), (35, 30), 4)],
         buildings=[B('igloo_armory', "Igloo Armory", 'Shop', 'Dome', (35, -20), (12, 12), 90, (0.95, 0.97, 1.0), (0.6, 0.8, 0.95)),
                    B('cocoa_cabin', "Cocoa Cabin", 'Shop', 'House', (-15, -20), (12, 10), -90, (0.45, 0.3, 0.2), (0.95, 0.97, 1.0),
                      (0.9, 0.2, 0.2)),
                    B('yeti_lodge', "Yeti Lodge", 'Battle', 'House', (-20, 40), (16, 12), 0, (0.4, 0.28, 0.2), (0.95, 0.97, 1.0),
                      (0.3, 0.5, 0.7)),
                    B('ice_cavern', "Ice Cavern", 'Battle', 'Dome', (35, 38), (16, 16), 0, (0.6, 0.8, 0.95), (0.4, 0.6, 0.85))]),
    dict(name="Molten Citadel", theme='Volcano', size=(120, 120), seed=53,
         ground=(0.25, 0.2, 0.18), wall=(0.45, 0.2, 0.12), path=(0.35, 0.32, 0.3),
         entry=('E', -30, "Frostpeak Hollow"), exit=('N', 0, "Dusty Crossroads"), start=(52, -30), look_at=(0, -30),
         paths=[((60, -30), (0, -30), 6), ((0, -30), (0, 60), 6), ((-23, -5), (22, -5), 4), ((-17, 38), (23, 38), 4)],
         hazards=[dict(center=(0, 18), rect=(120, 5), bridges=[(-4, 15, 8, 6)]),
                  dict(center=(-40, -40), radius=7), dict(center=(38, -48), radius=6), dict(center=(45, 48), radius=6)],
         buildings=[B('ember_forge', "Forge of Embers", 'Shop', 'Tower', (-28, -5), (10, 10), -90, (0.2, 0.18, 0.18), (1.0, 0.45, 0.1)),
                    B('ash_altar', "Ash Altar", 'Shop', 'Hut', (28, 38), (10, 10), 90, (0.35, 0.33, 0.33), (0.6, 0.15, 0.12)),
                    B('magma_gauntlet', "Magma Gauntlet", 'Battle', 'Arena', (30, -5), (16, 16), 90, (0.25, 0.22, 0.22), (1.0, 0.4, 0.1)),
                    B('citadel_throne', "Citadel Throne", 'Battle', 'Tower', (-25, 38), (16, 16), -90, (0.15, 0.12, 0.14),
                      (0.85, 0.15, 0.1))]),
]

def build_area(A):
    begin_scene("AWEJ_" + A['name'].replace(" ", ""))
    build_terrain2(A)
    build_walls2(A)
    build_paths2(A)
    build_hazards(A)
    for b in A['buildings']:
        build_building(b, A['theme'])
    S = build_nature2(A)
    build_lamps(A, S)
    build_characters2(A)
    setup_world2(A)
    build_cameras2(A)
    render_settings()
    return SC

only = globals().get('ONLY_AREAS')
built = []
for A in AREAS:
    if only and A['name'] not in only:
        continue
    sc = build_area(A)
    built.append({"scene": sc.name, "objects": len(sc.objects)})
result = {"built": built}
