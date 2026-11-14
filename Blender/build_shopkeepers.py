"""
AWEJ shopkeepers: ten original chibi animal characters, one per shop, built in the scene
"AWEJ_Shopkeepers" and exported to Assets/Resources/Shopkeepers/<shop id>.fbx.

Characters face Blender -Y (Unity -Z, toward the player across the counter) and stand on z=0.

Run inside Blender:
    exec(compile(open(path).read(), path, 'exec'), {'__name__': '__main__', '__file__': path})
Set EXPORT=False in the exec globals to only build and render.
"""
import os, bpy, bmesh, math, random
from mathutils import Vector

HERE = os.path.dirname(__file__)
KIT = os.path.join(HERE, 'awej_kit.py')
exec(compile(open(KIT, encoding='utf-8').read(), KIT, 'exec'), globals())

OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "Assets", "Resources", "Shopkeepers"))

# --------------------------------------------------------------------------- materials
def mat(name, color, rough=0.55, emit=None, strength=0.0, metal=0.0, coat=0.0):
    return plain('SK_' + name, color, rough, metal=metal, emit=emit, emit_strength=strength, coat=coat)

EYE = mat('Eye', (0.03, 0.025, 0.03), 0.15, coat=1.0)
SHINE = mat('EyeShine', (1, 1, 1), 0.2, emit=(1, 1, 1), strength=1.5)
BLUSH = mat('Blush', (1.0, 0.55, 0.6), 0.7)
PINK = mat('EarPink', (1.0, 0.7, 0.72), 0.7)
NOSE = mat('Nose', (0.12, 0.07, 0.07), 0.3, coat=0.6)
BRASS = mat('Brass', (0.9, 0.68, 0.25), 0.3, metal=1.0)
STEEL = mat('Steel', (0.75, 0.78, 0.82), 0.3, metal=1.0)
WOOD = mat('Wood', (0.45, 0.28, 0.14), 0.7)
WHITE = mat('White', (0.97, 0.96, 0.93), 0.6)

# --------------------------------------------------------------------------- shape helpers
def ball(name, r, loc, m, parent, scale=(1, 1, 1), rot=(0, 0, 0), subdiv=3):
    return sphere(name, r, loc, m, C_CHARS, parent, scale=scale, subdiv=subdiv, rot=rot)

def dome(name, r, loc, m, parent, scale=(1, 1, 1), zcut=0.0, rot=(0, 0, 0), segs=32):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=r)
    dead = [v for v in bm.verts if v.co.z < zcut * r - 1e-4]
    if dead:
        bmesh.ops.delete(bm, geom=dead, context='VERTS')
    bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    me = to_mesh(bm, name)
    box_uv(me)
    for p in me.polygons:
        p.use_smooth = p.normal.z > -0.99
    return place(name, me, C_CHARS, loc, rot, parent, m)

def rod(name, r, h, loc, m, parent, rot=(0, 0, 0), r2=None, segs=16):
    return cyl(name, r, h, loc, m, C_CHARS, parent, rot=rot, r2=r2, segs=segs, base=False)

def ring(name, r, t, loc, m, parent, rot=(math.pi / 2, 0, 0), n=20):
    g = empty(name, C_CHARS, loc, rot, parent)
    for k in range(n):
        a = k * 2 * math.pi / n
        ball(name + "Seg", t, (math.cos(a) * r, math.sin(a) * r, 0), m, g, scale=(1.6, 1.6, 1), subdiv=1)
    return g

FWD = math.pi / 2  # rod rotation that points along -Y (toward the player)

# --------------------------------------------------------------------------- base body
def chibi(cid, x, fur, belly=None, muzzle=None, ears='round', ear_tip=None, feet=None, arms=None, head_r=0.62,
          body=None, nose=NOSE, cheeks=True, eyes_z=1.72, has_muzzle=True, tail=None, tail_col=None, tail_tip=None):
    """Returns (root, anchors). Front is -Y. Height ~2.3 m."""
    root = empty(cid, C_CHARS, (x, 0, 0))
    furm = mat(cid + '_Fur', fur)
    bodym = mat(cid + '_Body', body) if body else furm
    bellym = mat(cid + '_Belly', belly) if belly else None
    hz = 1.62
    # legs and feet
    for s in (-1, 1):
        ball("Foot", 0.2, (s * 0.24, -0.12, 0.1), mat(cid + '_Feet', feet) if feet else furm, root, scale=(1, 1.4, 0.65))
    # body
    ball("Body", 0.56, (0, 0, 0.74), bodym, root, scale=(1, 0.9, 1.0))
    if bellym:
        ball("Belly", 0.4, (0, -0.34, 0.7), bellym, root, scale=(1, 0.5, 1.05))
    # arms with mitten hands
    am = mat(cid + '_Arms', arms) if arms else bodym
    for s in (-1, 1):
        ball("Arm", 0.15, (s * 0.56, -0.06, 0.82), am, root, scale=(1, 1, 2.0), rot=(0, s * 0.45, 0))
        ball("Hand", 0.14, (s * 0.66, -0.14, 0.52), mat(cid + '_Arms', arms) if arms else furm, root)
    # head
    ball("Head", head_r, (0, 0, hz), furm, root, scale=(1.08, 1.0, 0.95), subdiv=4)
    fy = -head_r * 0.9
    if has_muzzle:
        ball("Muzzle", 0.24, (0, fy + 0.1, hz - 0.12), mat(cid + '_Muzzle', muzzle) if muzzle else furm, root,
             scale=(1.25, 0.75, 0.8))
        ball("Nose", 0.075, (0, fy - 0.07, hz - 0.04), nose, root, scale=(1.3, 1, 0.85))
    for s in (-1, 1):
        ball("Eye", 0.1, (s * 0.23, fy + 0.03, eyes_z), EYE, root, scale=(0.85, 0.5, 1.2))
        ball("EyeShine", 0.034, (s * 0.2, fy - 0.03, eyes_z + 0.06), SHINE, root, subdiv=2)
        if cheeks:
            ball("Cheek", 0.1, (s * 0.38, fy + 0.1, hz - 0.12), BLUSH, root, scale=(1, 0.4, 0.65), subdiv=2)
    # ears
    for s in (-1, 1):
        if ears == 'round':
            ball("Ear", 0.2, (s * 0.42, 0.02, hz + 0.48), furm, root, scale=(1, 0.55, 1))
            ball("EarIn", 0.12, (s * 0.42, -0.07, hz + 0.47), PINK, root, scale=(1, 0.3, 1))
        elif ears == 'pointed':
            rod("Ear", 0.22, 0.5, (s * 0.36, 0.0, hz + 0.6), furm, root, rot=(0, s * 0.35, 0), r2=0.0, segs=20)
            rod("EarIn", 0.13, 0.32, (s * 0.35, -0.08, hz + 0.56), PINK, root, rot=(0, s * 0.35, 0), r2=0.0, segs=16)
            if ear_tip:
                rod("EarTip", 0.09, 0.16, (s * 0.44, 0.0, hz + 0.8), mat(cid + '_EarTip', ear_tip), root,
                    rot=(0, s * 0.35, 0), r2=0.0, segs=12)
    # tails
    if tail == 'stub':
        ball("Tail", 0.13, (0, 0.5, 0.45), furm, root)
    elif tail == 'fluffy':
        g = empty("Tail", C_CHARS, (0.2, 0.45, 0.45), (-0.7, 0.4, 0), root)
        ball("TailFluff", 0.3, (0, 0, 0.45), furm, g, scale=(0.85, 0.85, 1.7))
        ball("TailTip", 0.2, (0, 0, 0.92), mat(cid + '_TailTip', tail_tip or (1, 1, 1)), g, scale=(1, 1, 1.2))
    elif tail == 'ringed':
        g = empty("Tail", C_CHARS, (0.25, 0.45, 0.4), (-0.8, 0.45, 0), root)
        tm = mat(cid + '_TailRing', tail_col or (0.2, 0.2, 0.2))
        for k in range(6):
            ball("TailSeg", 0.21 - k * 0.012, (0, 0, k * 0.17), furm if k % 2 == 0 else tm, g, scale=(1, 1, 0.75))
    elif tail == 'curl':
        for k in range(7):
            a = k * 0.5
            ball("TailSeg", 0.09, (0.3 + math.sin(a) * 0.25, 0.5, 0.3 + k * 0.1 + (1 - math.cos(a)) * 0.05), furm, root)
    anchors = dict(root=root, head_z=hz, head_r=head_r, face_y=fy, hand_r=Vector((-0.66, -0.14, 0.52)),
                   hand_l=Vector((0.66, -0.14, 0.52)), fur=furm)
    return root, anchors

# --------------------------------------------------------------------------- props
def straw_hat(root, z, color=(0.92, 0.78, 0.45), band=(0.8, 0.2, 0.15)):
    rod("HatBrim", 0.62, 0.05, (0, 0, z), mat('Straw', color, 0.8), root, segs=32)
    rod("HatCrown", 0.33, 0.28, (0, 0, z + 0.15), mat('Straw', color, 0.8), root, r2=0.3, segs=28)
    rod("HatBand", 0.335, 0.08, (0, 0, z + 0.06), mat('HatBand', band), root, segs=28)

def apron(root, color, pocket=None):
    am = mat('Apron_%02x%02x%02x' % tuple(int(c * 255) for c in color), color, 0.75)
    ball("Apron", 0.45, (0, -0.3, 0.66), am, root, scale=(1.0, 0.45, 1.15))
    for s in (-1, 1):
        rod("ApronStrap", 0.04, 0.5, (s * 0.2, -0.38, 1.05), am, root, rot=(0.25, 0, 0), segs=8)
    if pocket:
        box("ApronPocket", (0.32, 0.06, 0.2), (0, -0.52, 0.56), mat('Pocket', pocket, 0.75), C_CHARS, root, bevel=0.03)

def hammer(parent, loc, rot=(0, 0.4, 0)):
    g = empty("Hammer", C_CHARS, loc, rot, parent)
    rod("HammerHandle", 0.045, 0.7, (0, 0, 0.2), WOOD, g, segs=10)
    box("HammerHead", (0.34, 0.16, 0.16), (0, 0, 0.55), STEEL, C_CHARS, g, bevel=0.03)
    return g

def potion(parent, loc, color=(0.3, 0.9, 0.8)):
    g = empty("Potion", C_CHARS, loc, (0, 0, 0), parent)
    ball("PotionBulb", 0.17, (0, 0, 0.14), mat('PotionGlass', color, 0.05, emit=color, strength=1.2), g)
    rod("PotionNeck", 0.06, 0.14, (0, 0, 0.33), mat('Glass', (0.8, 0.95, 1.0), 0.05), g, segs=12)
    rod("PotionCork", 0.065, 0.07, (0, 0, 0.42), WOOD, g, segs=12)
    return g

def flame_tip(parent, loc, s=1.0):
    fm = mat('Flame', (1.0, 0.55, 0.15), 0.5, emit=(1.0, 0.5, 0.1), strength=6)
    core = mat('FlameCore', (1.0, 0.9, 0.5), 0.5, emit=(1.0, 0.85, 0.4), strength=8)
    ball("Flame", 0.16 * s, (loc[0], loc[1], loc[2] + 0.12 * s), fm, parent, scale=(1, 1, 1.9))
    ball("FlameCore", 0.09 * s, (loc[0], loc[1] - 0.04 * s, loc[2] + 0.08 * s), core, parent, scale=(1, 1, 1.7))

# --------------------------------------------------------------------------- the ten shopkeepers
def hamster(x):  # General Store
    r, a = chibi('general_store', x, (0.93, 0.66, 0.36), belly=(0.99, 0.92, 0.78), muzzle=(0.99, 0.92, 0.8),
                 ears='round', feet=(1.0, 0.75, 0.7), tail='stub')
    apron(r, (0.25, 0.55, 0.3), pocket=(0.2, 0.45, 0.25))
    straw_hat(r, a['head_z'] + 0.5)
    for s in (-1, 1):  # chubby cheek pouches
        ball("Pouch", 0.2, (s * 0.42, a['face_y'] + 0.28, a['head_z'] - 0.2), a['fur'], r)
    ball("Teeth", 0.06, (0, a['face_y'] - 0.1, a['head_z'] - 0.24), WHITE, r, scale=(1.4, 0.5, 1))
    return r

def raccoon(x):  # Tinker's Workshop
    r, a = chibi('tinker_workshop', x, (0.56, 0.56, 0.6), belly=(0.85, 0.85, 0.86), muzzle=(0.92, 0.92, 0.92),
                 ears='pointed', ear_tip=(0.25, 0.25, 0.28), arms=(0.3, 0.3, 0.33), feet=(0.3, 0.3, 0.33),
                 tail='ringed', tail_col=(0.22, 0.22, 0.25))
    ball("Mask", 0.3, (0, a['face_y'] + 0.16, 1.72), mat('RaccoonMask', (0.22, 0.22, 0.25)), r, scale=(1.55, 0.45, 0.45))
    for s in (-1, 1):  # re-draw eyes over the mask
        ball("Eye", 0.1, (s * 0.23, a['face_y'] - 0.02, 1.72), EYE, r, scale=(0.85, 0.5, 1.2))
        ball("EyeShine", 0.034, (s * 0.2, a['face_y'] - 0.08, 1.78), SHINE, r, subdiv=2)
    # goggles pushed up on the forehead
    rod("GoggleStrap", 0.66, 0.08, (0, 0.02, 2.0), mat('Leather', (0.35, 0.2, 0.1), 0.7), r, segs=32)
    for s in (-1, 1):
        rod("GoggleRim", 0.16, 0.12, (s * 0.2, -0.56, 2.02), BRASS, r, rot=(FWD + 0.4, 0, 0), segs=20)
        rod("GoggleLens", 0.12, 0.13, (s * 0.2, -0.565, 2.02), mat('Lens', (0.4, 0.85, 0.95), 0.05, emit=(0.3, 0.7, 0.8), strength=0.6),
            r, rot=(FWD + 0.4, 0, 0), segs=20)
    rod("ToolBelt", 0.58, 0.12, (0, 0, 0.5), mat('Leather', (0.35, 0.2, 0.1), 0.7), r, segs=32)
    g = empty("Wrench", C_CHARS, a['hand_r'] + Vector((0, -0.05, 0.05)), (0, -0.3, 0), r)
    box("WrenchHandle", (0.08, 0.05, 0.6), (0, 0, 0.3), STEEL, C_CHARS, g, bevel=0.02)
    ring("WrenchJaw", 0.1, 0.035, (0, 0, 0.66), STEEL, g, rot=(math.pi / 2, 0, 0), n=10)
    return r

def frog(x):  # Herbalist's Hut
    green = (0.45, 0.78, 0.35)
    r, a = chibi('herbalist_hut', x, green, belly=(0.9, 0.95, 0.7), ears='none', has_muzzle=False, eyes_z=1.62,
                 head_r=0.6, cheeks=True)
    for s in (-1, 1):  # bulging eyes on top
        ball("EyeBulge", 0.2, (s * 0.27, -0.22, 2.08), a['fur'], r)
        ball("EyeWhite", 0.15, (s * 0.27, -0.33, 2.12), WHITE, r)
        ball("Pupil", 0.085, (s * 0.27, -0.45, 2.13), EYE, r, scale=(1, 0.6, 1.2))
        ball("PupilShine", 0.03, (s * 0.24, -0.5, 2.17), SHINE, r, subdiv=2)
    for k in range(7):  # wide smile
        t = (k - 3) / 3.0
        ball("Smile", 0.03, (t * 0.28, a['face_y'] - 0.02 + abs(t) * 0.06, 1.5 + t * t * 0.07), NOSE, r, subdiv=1)
    leaf = mat('Leaf', (0.25, 0.6, 0.2), 0.6)
    ball("LeafHat", 0.55, (0.05, 0.05, 2.18), leaf, r, scale=(1.1, 0.8, 0.12), rot=(0.15, 0.2, 0.3))
    rod("LeafStem", 0.03, 0.3, (0.1, 0.05, 2.32), mat('Stem', (0.35, 0.5, 0.15)), r, rot=(0, 0.5, 0), segs=8)
    rod("Satchel strap", 0.03, 1.0, (0.1, -0.42, 0.95), mat('Leather', (0.35, 0.2, 0.1), 0.7), r, rot=(0, 0.8, 0), segs=8)
    box("Satchel", (0.34, 0.16, 0.3), (0.45, -0.32, 0.55), mat('Leather', (0.35, 0.2, 0.1), 0.7), C_CHARS, r, bevel=0.05)
    ball("Sprig", 0.12, (0.45, -0.35, 0.78), leaf, r, subdiv=1)
    potion(r, a['hand_r'] + Vector((0, -0.05, 0.05)), (0.5, 0.95, 0.35))
    return r

def fox(x):  # Ranger Outpost
    r, a = chibi('ranger_outpost', x, (0.95, 0.5, 0.16), belly=(0.99, 0.95, 0.9), muzzle=(0.99, 0.96, 0.92),
                 ears='pointed', ear_tip=(0.2, 0.12, 0.1), arms=(0.25, 0.16, 0.12), feet=(0.25, 0.16, 0.12),
                 tail='fluffy', tail_tip=(0.99, 0.97, 0.93))
    hood = mat('RangerGreen', (0.2, 0.42, 0.22), 0.8)
    dome("Hood", 0.7, (0, 0.12, 1.62), hood, r, scale=(1.05, 1.0, 0.98), zcut=-0.25, rot=(-0.35, 0, 0))
    rod("Cape", 0.62, 1.05, (0, 0.22, 1.25), hood, r, rot=(math.pi + 0.15, 0, 0), r2=0.3, segs=24)
    ball("Clasp", 0.07, (0, -0.48, 1.12), BRASS, r)
    g = empty("Bow", C_CHARS, (-0.35, 0.45, 1.0), (0.3, 0, 0.4), r)
    for s in (-1, 1):
        rod("BowLimb", 0.03, 0.6, (0, 0, s * 0.3), WOOD, g, rot=(s * 0.25, 0, 0), segs=8)
    rod("Bowstring", 0.008, 1.1, (0, 0.12, 0), WHITE, g, segs=6)
    return r

def cat(x):  # Sunset Bazaar
    r, a = chibi('sunset_bazaar', x, (0.98, 0.92, 0.8), belly=(1.0, 0.97, 0.92), muzzle=(1, 0.98, 0.95),
                 ears='pointed', tail='curl', body=(0.55, 0.25, 0.6), arms=(0.98, 0.92, 0.8))
    ball("Patch", 0.25, (0.28, -0.38, 1.85), mat('CatPatch', (0.95, 0.6, 0.25)), r, scale=(1, 0.5, 0.9))
    for s in (-1, 1):
        for k in (-1, 1):
            box("Whisker", (0.35, 0.012, 0.012), (s * 0.42, a['face_y'] - 0.02, 1.48 + k * 0.05), NOSE, C_CHARS, r,
                rot=(0, k * s * 0.15, 0), bevel=0)
    fez = mat('Fez', (0.75, 0.1, 0.12), 0.6)
    rod("Fez", 0.25, 0.32, (0.05, 0.0, 2.2), fez, r, r2=0.2, segs=24)
    rod("FezTassel", 0.015, 0.3, (0.24, 0.0, 2.25), mat('Tassel', (0.95, 0.75, 0.2)), r, rot=(0, 0.6, 0), segs=6)
    ring("Earring", 0.07, 0.018, (0.48, -0.05, 1.42), BRASS, r, rot=(0, math.pi / 2, 0), n=12)
    vest = mat('Vest', (0.85, 0.65, 0.15), 0.5, metal=0.4)
    for s in (-1, 1):
        ball("VestTrim", 0.12, (s * 0.24, -0.45, 0.85), vest, r, scale=(0.4, 0.4, 2.4))
    for k in range(3):
        ball("Coin", 0.07, (0.66 + k * 0.03, -0.25, 0.65 + k * 0.05), vest, r, scale=(1, 0.3, 1))
    return r

def owl(x):  # Oasis Apothecary
    r, a = chibi('oasis_apothecary', x, (0.78, 0.6, 0.4), belly=(0.96, 0.88, 0.72), ears='none', has_muzzle=False,
                 eyes_z=1.68, cheeks=True, feet=(0.95, 0.65, 0.2), arms=(0.65, 0.48, 0.3))
    for s in (-1, 1):
        rod("EyeDisc", 0.22, 0.06, (s * 0.23, a['face_y'] + 0.06, 1.68), WHITE, r, rot=(FWD, 0, 0), segs=28)
        ball("Pupil", 0.12, (s * 0.23, a['face_y'] - 0.01, 1.68), EYE, r, scale=(1, 0.5, 1))
        ball("PupilShine", 0.04, (s * 0.2, a['face_y'] - 0.06, 1.74), SHINE, r, subdiv=2)
        rod("Tuft", 0.12, 0.35, (s * 0.42, 0.0, 2.12), a['fur'], r, rot=(0, s * 0.6, 0), r2=0.0, segs=12)
    rod("Beak", 0.08, 0.2, (0, a['face_y'] - 0.02, 1.52), mat('Beak', (0.98, 0.7, 0.2)), r, rot=(FWD + 0.4, 0, 0), r2=0.0, segs=12)
    ring("Monocle", 0.24, 0.025, (0.23, a['face_y'] - 0.04, 1.68), BRASS, r, n=22)
    rod("MonocleChain", 0.01, 0.6, (0.42, a['face_y'] + 0.05, 1.4), BRASS, r, rot=(0.2, 0.3, 0), segs=6)
    rod("Turban", 0.55, 0.3, (0, 0.03, 2.0), mat('Turban', (0.25, 0.55, 0.7), 0.7), r, r2=0.45, segs=28)
    ball("TurbanGem", 0.07, (0, -0.55, 2.02), mat('Gem', (0.9, 0.2, 0.3), 0.1, emit=(0.9, 0.2, 0.3), strength=1.0), r)
    potion(r, a['hand_r'] + Vector((0, -0.06, 0.04)), (0.95, 0.3, 0.55))
    potion(r, a['hand_l'] + Vector((0, -0.06, 0.04)), (0.3, 0.8, 0.95))
    return r

def penguin(x):  # Igloo Armory
    r, a = chibi('igloo_armory', x, (0.12, 0.13, 0.18), belly=(0.97, 0.97, 0.97), ears='none', has_muzzle=False,
                 feet=(0.98, 0.6, 0.15), cheeks=True)
    ball("FaceMask", 0.46, (0, -0.29, 1.6), WHITE, r, scale=(1.05, 0.62, 0.85))
    for s in (-1, 1):  # eyes on top of the white face
        ball("Eye", 0.1, (s * 0.21, a['face_y'] + 0.01, 1.7), EYE, r, scale=(0.85, 0.5, 1.2))
        ball("EyeShine", 0.034, (s * 0.18, a['face_y'] - 0.05, 1.76), SHINE, r, subdiv=2)
        ball("Cheek", 0.1, (s * 0.36, a['face_y'] + 0.08, 1.5), BLUSH, r, scale=(1, 0.4, 0.65), subdiv=2)
    rod("Beak", 0.11, 0.22, (0, a['face_y'] - 0.02, 1.55), mat('Beak', (0.98, 0.65, 0.15)), r, rot=(FWD, 0, 0), r2=0.0, segs=14)
    dome("Helmet", 0.67, (0, 0.04, 1.88), STEEL, r, scale=(1.08, 1.0, 0.8), zcut=0.0)
    rod("HelmetRim", 0.73, 0.1, (0, 0.04, 1.9), BRASS, r, segs=32)
    for s in (-1, 1):
        rod("Horn", 0.1, 0.45, (s * 0.62, 0, 2.22), mat('Horn', (0.96, 0.92, 0.8)), r, rot=(0, s * 0.9, 0), r2=0.0, segs=12)
    hammer(r, a['hand_r'] + Vector((0, -0.05, 0)), rot=(0, -0.3, 0))
    ball("ShieldBoss", 0.08, (0.72, -0.25, 0.6), BRASS, r)
    rod("Shield", 0.34, 0.06, (0.72, -0.2, 0.6), mat('ShieldBlue', (0.3, 0.55, 0.85), 0.5), r, rot=(FWD, 0, 0), segs=28)
    return r

def polar_bear(x):  # Cocoa Cabin
    r, a = chibi('cocoa_cabin', x, (0.96, 0.96, 0.98), belly=(1, 1, 1), muzzle=(1, 0.98, 0.96), ears='round',
                 tail='stub')
    beanie = mat('Beanie', (0.85, 0.15, 0.2), 0.85)
    dome("Beanie", 0.6, (0, 0.03, 1.86), beanie, r, scale=(1.08, 1.0, 0.9), zcut=0.0)
    rod("BeanieCuff", 0.66, 0.16, (0, 0.03, 1.9), WHITE, r, segs=32)
    ball("Pompom", 0.17, (0, 0.03, 2.43), WHITE, r)
    scarf = mat('Scarf', (0.15, 0.55, 0.3), 0.85)
    rod("Scarf", 0.5, 0.2, (0, 0, 1.15), scarf, r, segs=32)
    box("ScarfTail", (0.2, 0.08, 0.5), (0.22, -0.48, 0.92), scarf, C_CHARS, r, rot=(0.15, 0, 0.1), bevel=0.04)
    g = empty("Mug", C_CHARS, Vector((0, -0.55, 0.62)), (0, 0, 0), r)
    rod("MugBody", 0.17, 0.3, (0, 0, 0), mat('Mug', (0.85, 0.3, 0.25), 0.4), g, segs=24)
    rod("Cocoa", 0.15, 0.02, (0, 0, 0.14), mat('Cocoa', (0.35, 0.18, 0.08), 0.3), g, segs=24)
    ball("Cream", 0.12, (0, 0, 0.17), WHITE, g, scale=(1, 1, 0.6))
    for s in (-1, 1):
        ball("Hand", 0.14, (s * 0.2, -0.45, 0.62), a['fur'], r)
    return r

def red_panda(x):  # Forge of Embers
    r, a = chibi('ember_forge', x, (0.78, 0.33, 0.15), muzzle=(0.98, 0.95, 0.9), ears='round',
                 arms=(0.22, 0.12, 0.1), feet=(0.22, 0.12, 0.1), tail='ringed', tail_col=(0.45, 0.18, 0.08))
    for s in (-1, 1):
        ball("Brow", 0.08, (s * 0.24, a['face_y'] + 0.07, 1.86), WHITE, r, scale=(1.3, 0.4, 0.7), subdiv=2)
        ball("CheekFur", 0.16, (s * 0.36, a['face_y'] + 0.15, 1.5), WHITE, r, scale=(1, 0.5, 0.8))
    apron(r, (0.4, 0.24, 0.13))
    rod("Bandana", 0.66, 0.12, (0, 0.02, 1.98), mat('Bandana', (0.2, 0.2, 0.22), 0.8), r, segs=32)
    hammer(r, a['hand_r'] + Vector((0, -0.05, 0)), rot=(0, -0.4, 0))
    g = empty("Ember", C_CHARS, a['hand_l'] + Vector((0, -0.05, 0.12)), (0, 0, 0), r)
    ball("Ingot", 0.12, (0, 0, 0), mat('HotIngot', (1.0, 0.45, 0.1), 0.4, emit=(1.0, 0.4, 0.08), strength=3), g,
         scale=(1.6, 0.8, 0.6))
    return r

def candle_spirit(x):  # Ash Altar
    root = empty('ash_altar', C_CHARS, (x, 0, 0))
    wax = mat('Wax', (0.98, 0.93, 0.82), 0.4, emit=(1.0, 0.75, 0.45), strength=0.15)
    cloak = mat('Cloak', (0.45, 0.08, 0.1), 0.8)
    rod("CloakBody", 0.62, 1.2, (0, 0.05, 0.6), cloak, root, r2=0.42, segs=28)
    rod("Candle", 0.46, 1.25, (0, 0, 1.25), wax, root, segs=32)
    ball("CandleTop", 0.46, (0, 0, 1.86), wax, root, scale=(1, 1, 0.35))
    dr = random.Random(4)
    for k in range(7):
        a = dr.uniform(0, 2 * math.pi)
        z = dr.uniform(1.25, 1.75)
        ball("Drip", 0.07, (math.cos(a) * 0.45, math.sin(a) * 0.45, z), wax, root, scale=(1, 1, 2.2), subdiv=2)
    dome("Hood", 0.56, (0, 0.12, 1.55), cloak, root, scale=(1.05, 1.0, 1.0), zcut=-0.1, rot=(-0.5, 0, 0))
    rod("Wick", 0.025, 0.18, (0, 0, 2.0), mat('Wick', (0.1, 0.08, 0.08)), root, segs=8)
    flame_tip(root, (0, 0, 2.06), 1.4)
    for s in (-1, 1):
        ball("Eye", 0.09, (s * 0.17, -0.45, 1.5), EYE, root, scale=(0.85, 0.5, 1.25))
        ball("EyeShine", 0.03, (s * 0.14, -0.5, 1.56), SHINE, root, subdiv=2)
        ball("Cheek", 0.08, (s * 0.3, -0.4, 1.38), BLUSH, root, scale=(1, 0.4, 0.65), subdiv=2)
        ball("Sleeve", 0.14, (s * 0.55, -0.1, 0.95), cloak, root, scale=(1, 1, 1.8), rot=(0, s * 0.4, 0))
        ball("Hand", 0.11, (s * 0.62, -0.18, 0.7), wax, root)
    ball("Mouth", 0.05, (0, -0.47, 1.36), NOSE, root, scale=(1.2, 0.5, 0.7), subdiv=2)
    g = empty("Lantern", C_CHARS, (-0.62, -0.25, 0.35), (0, 0, 0), root)
    box("LanternFrame", (0.26, 0.26, 0.34), (0, 0, 0), mat('Iron', (0.15, 0.13, 0.13), 0.5, metal=0.8), C_CHARS, g, bevel=0.03)
    box("LanternGlow", (0.2, 0.28, 0.26), (0, 0, 0), mat('LanternGlow', (1.0, 0.6, 0.25), 0.3, emit=(1.0, 0.55, 0.2), strength=4),
        C_CHARS, g, bevel=0)
    rod("LanternHandle", 0.02, 0.35, (0, 0, 0.3), mat('Iron', (0.15, 0.13, 0.13), 0.5, metal=0.8), g, segs=6)
    return root

SHOPKEEPERS = [hamster, raccoon, frog, fox, cat, owl, penguin, polar_bear, red_panda, candle_spirit]

# --------------------------------------------------------------------------- stage, render, export
def build():
    begin_scene("AWEJ_Shopkeepers")
    roots = []
    spacing = 2.5
    for i, fn in enumerate(SHOPKEEPERS):
        roots.append(fn((i - (len(SHOPKEEPERS) - 1) / 2) * spacing))
    # simple stage for the lineup render
    cyl("Stage", 15, 0.2, (0, 0.5, -0.2), mat('Stage', (0.55, 0.38, 0.25), 0.8), C_TERRAIN, segs=64)
    box("Backdrop", (40, 0.5, 12), (0, 6, 5), mat('Backdrop', (0.55, 0.75, 0.9), 0.9), C_TERRAIN, bevel=0)
    w = bpy.data.worlds.new("Shopkeepers_Sky")
    SC.world = w
    if w.node_tree is None:
        w.use_nodes = True
    w.node_tree.nodes['Background'].inputs['Color'].default_value = (0.6, 0.7, 0.85, 1)
    w.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.6
    for name, loc, rot, e in (("Key", (6, -10, 9), (math.radians(55), 0, math.radians(30)), 3.5),):
        li = bpy.data.lights.new(name, 'SUN'); li.energy = e; li.angle = math.radians(8); li.color = (1.0, 0.95, 0.88)
        place(name, li, C_SKY, loc, rot)
    li = bpy.data.lights.new("Fill", 'AREA'); li.energy = 2500; li.size = 12; li.color = (0.85, 0.9, 1.0)
    place("Fill", li, C_SKY, (-8, -12, 6), (math.radians(65), 0, math.radians(-35)))
    cams = {}
    def cam(name, loc, target, lens):
        cd = bpy.data.cameras.new(name)
        cd.lens = lens
        ob = place(name, cd, C_SKY, loc)
        aim(ob, target)
        cams[name] = ob
    cam("SK_Cam_Lineup", (0, -22, 3.6), (0, 0, 1.2), 32)
    cam("SK_Cam_CloseA", (-6.25, -7.5, 2.4), (-6.25, 0, 1.3), 50)
    cam("SK_Cam_CloseB", (6.25, -7.5, 2.4), (6.25, 0, 1.3), 50)
    SC.camera = cams["SK_Cam_Lineup"]
    render_settings()
    return roots

roots = build()
exported = []
if globals().get('EXPORT', True):
    EXP = os.path.join(HERE, 'export_unity_buildings.py')
    eg = {'__name__': 'awej_export', '__file__': EXP}
    exec(compile(open(EXP, encoding='utf-8').read(), EXP, 'exec'), eg)
    os.makedirs(OUT_DIR, exist_ok=True)
    tmp = bpy.data.scenes.new("AWEJ_Export")
    dg = SC.view_layers[0].depsgraph
    dg.update()
    for rt in roots:
        exported.append(eg['export_building'](rt, rt.name, tmp, dg, OUT_DIR))
    bpy.data.scenes.remove(tmp)
result = {"scene": SC.name, "characters": [r.name for r in roots], "exported": exported}
