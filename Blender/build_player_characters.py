"""
AWEJ player characters: four cute pill-shaped heroes in the player's blue (Unity colour 0.2, 0.4, 0.9),
built in the scene "AWEJ_Players" and exported to Assets/Resources/Players/<id>.fbx.

The pill matches the old player capsule: 2 m tall, 0.5 m radius, standing on z=0.
Faces point Blender +Y, which the exporter maps to Unity +Z (the player's forward).

Run headless:  blender -b AWEJ_World.blend --python build_player_characters.py
"""
import os, bpy, bmesh, math
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
KIT = os.path.join(HERE, 'awej_kit.py')
exec(compile(open(KIT, encoding='utf-8').read(), KIT, 'exec'), globals())

OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Players"))
BLUE = (0.2, 0.4, 0.9)  # exported as-is so Unity shows the same colour as the old capsule

def mat(name, color, rough=0.5, emit=None, strength=0.0, metal=0.0, coat=0.0):
    return plain('PL_' + name, color, rough, metal=metal, emit=emit, emit_strength=strength, coat=coat)

BODY = mat('BodyBlue', BLUE, 0.4, coat=0.4)
BODY_DARK = mat('BlueDark', (0.12, 0.26, 0.7), 0.45)
EYE = mat('Eye', (0.02, 0.02, 0.03), 0.12, coat=1.0)
SHINE = mat('EyeShine', (1, 1, 1), 0.2, emit=(1, 1, 1), strength=1.0)
BLUSH = mat('Blush', (1.0, 0.5, 0.6), 0.7)
PINK = mat('Pink', (1.0, 0.68, 0.75), 0.6)
WHITE = mat('White', (0.97, 0.97, 0.97), 0.5)
MOUTH = mat('Mouth', (0.15, 0.05, 0.08), 0.4)

R = 0.5          # pill radius
H = 2.0          # pill height
FACE_Y = R       # front surface of the pill
EYE_Z = 1.42

# --------------------------------------------------------------------------- helpers
def ball(name, r, loc, m, parent, scale=(1, 1, 1), rot=(0, 0, 0), subdiv=3):
    return sphere(name, r, loc, m, C_CHARS, parent, scale=scale, subdiv=subdiv, rot=rot)

def rod(name, r, h, loc, m, parent, rot=(0, 0, 0), r2=None, segs=16):
    return cyl(name, r, h, loc, m, C_CHARS, parent, rot=rot, r2=r2, segs=segs, base=False)

def pill(name, parent):
    """Capsule like Unity's: hemispheres on a cylinder, smooth, UV'd."""
    bm = bmesh.new()
    rings, segs = 24, 40
    verts = []
    for i in range(rings + 1):
        t = i / rings
        # map t over the profile: bottom hemisphere, cylinder, top hemisphere
        if t < 0.35:
            a = -math.pi / 2 + (t / 0.35) * math.pi / 2
            z, rr = R + math.sin(a) * R, math.cos(a) * R
        elif t < 0.65:
            z, rr = R + (t - 0.35) / 0.3 * (H - 2 * R), R
        else:
            a = ((t - 0.65) / 0.35) * math.pi / 2
            z, rr = H - R + math.sin(a) * R, math.cos(a) * R
        ring = [bm.verts.new((math.cos(k * 2 * math.pi / segs) * rr, math.sin(k * 2 * math.pi / segs) * rr, z))
                for k in range(segs)]
        verts.append(ring)
    for a, b in zip(verts[:-1], verts[1:]):
        for k in range(segs):
            bm.faces.new((a[k], a[(k + 1) % segs], b[(k + 1) % segs], b[k]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)
    me = to_mesh(bm, name)
    box_uv(me)
    smooth(me)
    return place(name, me, C_CHARS, (0, 0, 0), (0, 0, 0), parent, BODY)

def face(root, eye_spacing=0.17, eye_size=1.0, smile=True):
    for s in (-1, 1):
        x = s * eye_spacing
        y = math.sqrt(max(0.0, R * R - x * x)) - 0.02
        ball("Eye", 0.085 * eye_size, (x, y, EYE_Z), EYE, root, scale=(0.85, 0.45, 1.25))
        ball("EyeShine", 0.03 * eye_size, (x - s * 0.02 + 0.015, y + 0.035, EYE_Z + 0.05), SHINE, root, subdiv=2)
        bx = s * 0.29
        by = math.sqrt(max(0.0, R * R - bx * bx)) - 0.03
        ball("Blush", 0.075, (bx, by, EYE_Z - 0.13), BLUSH, root, scale=(1.2, 0.35, 0.7), subdiv=2)
    if smile:  # a smooth little "u" built from overlapping beads
        for k in range(15):
            t = (k - 7) / 7.0
            x = t * 0.065
            ball("Smile", 0.014, (x, math.sqrt(R * R - x * x) - 0.003, EYE_Z - 0.17 + t * t * 0.035), MOUTH, root, subdiv=1)

def nubs(root, m=BODY_DARK):
    for s in (-1, 1):
        ball("Foot", 0.13, (s * 0.2, 0.08, 0.07), m, root, scale=(1, 1.35, 0.55))
        ball("Hand", 0.11, (s * 0.53, 0.02, 0.8), BODY, root, scale=(0.85, 0.85, 1.1))

def base(cid, x):
    root = empty(cid, C_CHARS, (x, 0, 0))
    pill("Body", root)
    nubs(root)
    return root

# --------------------------------------------------------------------------- the four heroes
def bean(x):
    root = base('bean', x)
    face(root)
    # a single springy curl on top
    for k in range(7):
        a = k * 0.55
        ball("Curl", 0.045 - k * 0.003, (math.sin(a) * 0.09, -0.02, H + 0.02 + k * 0.035 + (1 - math.cos(a)) * 0.02), BODY, root, subdiv=2)
    return root

def mochi(x):
    root = base('mochi', x)
    face(root, eye_size=1.05)
    for s in (-1, 1):
        rod("Ear", 0.2, 0.38, (s * 0.27, 0.0, H - 0.06), BODY, root, rot=(0, s * 0.42, 0), r2=0.0, segs=20)
        rod("EarIn", 0.12, 0.24, (s * 0.26, 0.07, H - 0.08), PINK, root, rot=(0, s * 0.42, 0), r2=0.0, segs=16)
        for k in (-1, 1):  # whiskers: two per cheek, fanning slightly out from the face
            box("Whisker", (0.2, 0.012, 0.012), (s * 0.4, 0.33, EYE_Z - 0.13 + k * 0.04), MOUTH, C_CHARS, root,
                rot=(0, -s * k * 0.14, 0), bevel=0)
    ball("Nose", 0.03, (0, R + 0.005, EYE_Z - 0.08), PINK, root, scale=(1.3, 0.7, 0.9), subdiv=2)
    for k in range(9):  # curly tail behind
        a = k * 0.42
        ball("Tail", 0.06, (0.1 + math.sin(a) * 0.14, -0.55 - k * 0.012, 0.45 + k * 0.07 + (1 - math.cos(a)) * 0.04), BODY, root, subdiv=2)
    return root

def sprout(x):
    root = base('sprout', x)
    face(root)
    leaf = mat('Leaf', (0.3, 0.75, 0.25), 0.45)
    rod("Stem", 0.025, 0.22, (0, 0, H + 0.08), mat('Stem', (0.25, 0.55, 0.2)), root, segs=10)
    for s in (-1, 1):
        ball("Leaf", 0.16, (s * 0.13, 0, H + 0.2), leaf, root, scale=(1.0, 0.45, 0.22), rot=(0, s * -0.5, 0))
    scarf = mat('Scarf', (1.0, 0.78, 0.2), 0.7)
    rod("Scarf", R + 0.03, 0.12, (0, 0, 1.1), scarf, root, segs=40)
    box("ScarfTail", (0.12, 0.05, 0.3), (0.2, 0.47, 0.93), scarf, C_CHARS, root, rot=(0.2, 0, -0.15), bevel=0.03)
    return root

def pip(x):
    root = base('pip', x)
    face(root)
    band = mat('Headband', (0.85, 0.15, 0.15), 0.6)
    rod("Headband", R + 0.025, 0.1, (0, 0, 1.68), band, root, segs=40)
    star = mat('Star', (1.0, 0.82, 0.2), 0.3, emit=(1.0, 0.75, 0.2), strength=0.6)
    g = empty("Star", C_CHARS, (0, R + 0.04, 1.68), (math.pi / 2, 0, 0), root)
    for k in range(5):  # five-point star from small cones
        a = k * 2 * math.pi / 5
        rod("StarPoint", 0.045, 0.12, (math.sin(a) * 0.05, math.cos(a) * 0.05, 0), star, g,
            rot=(-math.cos(a) * math.pi / 2, math.sin(a) * math.pi / 2, 0), r2=0.0, segs=4)
    ball("StarCore", 0.05, (0, 0, 0), star, g, scale=(1, 1, 0.5), subdiv=2)
    for s in (-1, 1):  # headband tails flutter behind
        box("BandTail", (0.08, 0.3, 0.035), (s * 0.08, -0.62, 1.62), band, C_CHARS, root, rot=(0.35, s * 0.25, 0), bevel=0.01)
    cape = mat('Cape', (0.8, 0.12, 0.15), 0.7)
    bm = bmesh.new()  # cape: a curved sheet behind the shoulders
    cols, rows = 9, 6
    grid = []
    for i in range(rows + 1):
        t = i / rows
        z = 1.25 - t * 0.95
        spread = 0.38 + t * 0.22
        row = []
        for j in range(cols + 1):
            u = (j / cols - 0.5) * 2
            a = u * 1.2
            row.append(bm.verts.new((math.sin(a) * (R + 0.04) * spread / 0.38 * 0.9, -math.cos(a) * (R + 0.05) - t * 0.12, z)))
        grid.append(row)
    for i in range(rows):
        for j in range(cols):
            bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
    me = to_mesh(bm, "Cape")
    box_uv(me)
    smooth(me)
    c = place("Cape", me, C_CHARS, (0, 0, 0), (0, 0, 0), root, cape)
    sd = c.modifiers.new("Solidify", 'SOLIDIFY'); sd.thickness = 0.025
    ball("CapeClasp", 0.045, (0, 0.47, 1.2), star, root, scale=(1, 0.6, 1))
    return root

HEROES = [bean, mochi, sprout, pip]

# --------------------------------------------------------------------------- build + export
begin_scene("AWEJ_Players")
roots = [fn((i - 1.5) * 1.6) for i, fn in enumerate(HEROES)]

EXP = os.path.join(HERE, 'export_unity_buildings.py')
eg = {'__name__': 'awej_export', '__file__': EXP}
exec(compile(open(EXP, encoding='utf-8').read(), EXP, 'exec'), eg)
os.makedirs(OUT_DIR, exist_ok=True)
tmp = bpy.data.scenes.new("AWEJ_Export")
dg = SC.view_layers[0].depsgraph
dg.update()
exported = [eg['export_building'](rt, rt.name, tmp, dg, OUT_DIR) for rt in roots]
bpy.data.scenes.remove(tmp)

# a lineup camera for quick review in Blender
cd = bpy.data.cameras.new("PL_Cam")
cd.lens = 45
cam = place("PL_Cam", cd, C_SKY, (0, 9.5, 1.6))
aim(cam, (0, 0, 1.1))
SC.camera = cam
sun = bpy.data.lights.new("PL_Sun", 'SUN'); sun.energy = 3.5
place("PL_Sun", sun, C_SKY, (0, 0, 10), (math.radians(50), 0, math.radians(160)))
w = bpy.data.worlds.new("PL_World"); SC.world = w
if w.node_tree is None:
    w.use_nodes = True
w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.8
render_settings()
bpy.ops.wm.save_mainfile()
print("RESULT", exported)
