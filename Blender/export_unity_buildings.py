"""
Export every AWEJ building from AWEJ_World.blend as one FBX per building for Unity.

Output: Assets/Resources/Buildings/<building id>.fbx (ids match AreaDatabase.cs).
Each FBX holds a single merged mesh whose origin is the building's footprint center and whose
door faces Unity's local -Z, so AreaBuilder can drop it straight into the building group.

Procedural Blender materials can't travel through FBX, so each one is swapped for a flat
"U_<name>" material using its viewport colour (plus emission for glowing parts).

Run inside Blender with AWEJ_World.blend open:
    exec(compile(open(path).read(), path, 'exec'), {'__name__': '__main__', '__file__': path})
"""
import os, bpy
from mathutils import Matrix

OUT_DIR = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Buildings"))

# Blender root empty name -> AreaDatabase building id
BUILDINGS = {
    "GeneralStore": "general_store", "TinkersWorkshop": "tinker_workshop",
    "TrainingBarn": "training_barn", "OldCellar": "old_cellar",
    "Herbalist's Hut": "herbalist_hut", "Ranger Outpost": "ranger_outpost",
    "Hollow Stump": "hollow_stump", "Beetle Den": "beetle_den",
    "Sunset Bazaar": "sunset_bazaar", "Oasis Apothecary": "oasis_apothecary",
    "Scorpion Pit": "scorpion_pit", "Sun Colosseum": "sun_colosseum",
    "Igloo Armory": "igloo_armory", "Cocoa Cabin": "cocoa_cabin",
    "Yeti Lodge": "yeti_lodge", "Ice Cavern": "ice_cavern",
    "Forge of Embers": "ember_forge", "Ash Altar": "ash_altar",
    "Magma Gauntlet": "magma_gauntlet", "Citadel Throne": "citadel_throne",
}

def export_material(src):
    if src is None:
        return None
    name = "U_" + src.name
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    color = tuple(src.diffuse_color)
    rough, metal, emit, emit_strength = 0.7, 0.0, None, 0.0
    if src.node_tree:
        sb = next((n for n in src.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if sb:
            if not sb.inputs['Base Color'].is_linked:
                color = tuple(sb.inputs['Base Color'].default_value)
            if not sb.inputs['Roughness'].is_linked:
                rough = sb.inputs['Roughness'].default_value
            metal = sb.inputs['Metallic'].default_value
            emit_strength = sb.inputs['Emission Strength'].default_value
            ec = sb.inputs['Emission Color']
            if emit_strength > 0.05:
                emit = tuple(ec.default_value) if not ec.is_linked else color
    b.inputs['Base Color'].default_value = color
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    if emit:
        b.inputs['Emission Color'].default_value = emit
        b.inputs['Emission Strength'].default_value = min(emit_strength, 2.0)
    m.diffuse_color = color
    return m

def export_building(root, bid, tmp, dg):
    for ob in list(tmp.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    inv = root.matrix_world.inverted()
    # an identity-transform target so the join keeps root-local coordinates
    target = bpy.data.objects.new(bid, bpy.data.meshes.new(bid))
    tmp.collection.objects.link(target)
    parts = [target]
    for ob in root.children_recursive:
        if ob.type not in {'MESH', 'FONT', 'CURVE'} or ob.hide_render:
            continue
        ev = ob.evaluated_get(dg)
        me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
        if len(me.polygons) == 0:
            bpy.data.meshes.remove(me)
            continue
        for i, src in enumerate(me.materials):
            me.materials[i] = export_material(src)
        part = bpy.data.objects.new(ob.name, me)
        tmp.collection.objects.link(part)
        part.matrix_world = inv @ ob.matrix_world
        parts.append(part)
    with bpy.context.temp_override(scene=tmp, view_layer=tmp.view_layers[0], active_object=target, object=target,
                                   selected_objects=parts, selected_editable_objects=parts):
        bpy.ops.object.join()
    me = target.data
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    path = os.path.join(OUT_DIR, bid + ".fbx")
    for ob in tmp.objects:
        ob.select_set(ob == target, view_layer=tmp.view_layers[0])
    with bpy.context.temp_override(scene=tmp, view_layer=tmp.view_layers[0], active_object=target, object=target,
                                   selected_objects=[target]):
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={'MESH'},
            apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
            # Blender -Y (door side) -> Unity -Z, Blender Z -> Unity Y
            axis_forward='Z', axis_up='Y', bake_space_transform=True,
            use_mesh_modifiers=True, mesh_smooth_type='OFF', use_custom_props=False,
            add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False)
    n_mats = len(me.materials)
    bpy.data.objects.remove(target, do_unlink=True)
    bpy.data.meshes.remove(me)
    return {"id": bid, "tris": tris, "materials": n_mats, "kb": round(os.path.getsize(path) / 1024)}

def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    tmp = bpy.data.scenes.get("AWEJ_Export") or bpy.data.scenes.new("AWEJ_Export")
    report = []
    only = globals().get('ONLY_SCENES')
    for sc in bpy.data.scenes:
        if not sc.name.startswith("AWEJ_") or sc == tmp or (only and sc.name not in only):
            continue
        dg = sc.view_layers[0].depsgraph
        dg.update()
        for ob in sc.objects:
            if ob.type == 'EMPTY' and ob.parent is None and ob.name in BUILDINGS:
                report.append(export_building(ob, BUILDINGS[ob.name], tmp, dg))
    bpy.data.scenes.remove(tmp)
    for m in list(bpy.data.materials):
        if m.name.startswith("U_") and m.users == 0:
            bpy.data.materials.remove(m)
    return report

result = {"out": OUT_DIR, "exported": main()}
