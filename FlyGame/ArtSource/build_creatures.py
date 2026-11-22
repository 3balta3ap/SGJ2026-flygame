"""
Little Fly Planet - low-poly creatures (CHI/07).
Builds 4 models with bmesh, exports one FBX each, saves ArtSource/creatures.blend.
Run inside Blender (Blender MCP execute_blender_code):
    FLYGAME_ROOT = "/Users/hyo/Developer/sgj2026-flygame/FlyGame"
    exec(open(FLYGAME_ROOT + "/ArtSource/build_creatures.py").read())

All coordinates below are written in UNITY space (x = right, y = up, z = forward), relative to the
prefab's model origin (fly: BodyRoot = ground point, enemies: their Visual origin). They are converted
to Blender space (forward = -Y, up = +Z) at the end, so the FBX lines up with the old primitive models.
Part names and pivots match what FlyVisual / CreatureMotion animate. Re-running replaces everything.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix, Quaternion

ROOT = globals().get("FLYGAME_ROOT", "/Users/hyo/Developer/sgj2026-flygame/FlyGame")
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Models")
BLEND_PATH = os.path.join(ROOT, "ArtSource", "creatures.blend")
PREVIEW_GAP = 1.6

# Same names (and colours) as the Unity materials, so the FBX import can remap to them by name.
COLORS = {
    "M_FlyBody": (0.95, 0.72, 0.42, 1),
    "M_FlyAbdomen": (0.55, 0.35, 0.20, 1),
    "M_FlyEye": (0.90, 0.12, 0.12, 1),
    "M_FlyWing": (0.90, 0.95, 1.00, 0.45),
    "M_Cheek": (1.00, 0.60, 0.70, 1),
    "M_Shine": (1.00, 1.00, 1.00, 1),
    "M_MateBody": (1.00, 0.82, 0.62, 1),
    "M_Enemy_SpiderHead": (0.165, 0.114, 0.086, 1),
    "M_Enemy_SpiderBody": (0.231, 0.165, 0.125, 1),
    "M_Enemy_SpiderEye": (1.00, 0.251, 0.251, 1),
    "M_Enemy_DragonflyBody": (0.184, 0.561, 0.616, 1),
    "M_Enemy_DragonflyTail": (0.122, 0.435, 0.478, 1),
    "M_Enemy_DragonflyEye": (0.494, 0.851, 0.341, 1),
    "M_Enemy_DragonflyWing": (0.847, 0.941, 0.973, 1),
}

# Unity (x, y, z) -> Blender (-x, -z, y). Determinant -1 (handedness flip): normals are recalculated after.
CONV = Matrix(((-1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))


def to_b(v):
    return Vector((-v[0], -v[2], v[1]))


def rot_y(v, deg):
    """Unity yaw (left-handed: +deg turns +z toward +x)."""
    a = math.radians(deg)
    x, y, z = v
    return Vector((x * math.cos(a) + z * math.sin(a), y, -x * math.sin(a) + z * math.cos(a)))


def get_mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    c = COLORS[name]
    m.diffuse_color = c
    if not m.use_nodes:
        m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = c
        if c[3] < 1:
            bsdf.inputs["Alpha"].default_value = c[3]
    return m


class Geo:
    """Collects bmesh geometry in Unity space for one object. mats = list of material names (slot order)."""

    def __init__(self, mats):
        self.bm = bmesh.new()
        self.mats = list(mats)

    def _idx(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def _finish(self, new_verts, mat, matrix):
        bmesh.ops.transform(self.bm, matrix=matrix, verts=new_verts)
        vs = set(new_verts)
        idx = self._idx(mat)
        for f in self.bm.faces:
            if f.verts[0] in vs:
                f.material_index = idx

    def ellipsoid(self, center, radii, mat, segs=10, rings=6, yaw=0.0, roll=0.0, pitch=0.0):
        r = bmesh.ops.create_uvsphere(self.bm, u_segments=segs, v_segments=rings, radius=1.0)
        rot = (Matrix.Rotation(math.radians(yaw), 4, 'Y') @ Matrix.Rotation(math.radians(pitch), 4, 'X')
               @ Matrix.Rotation(math.radians(roll), 4, 'Z'))
        m = Matrix.Translation(Vector(center)) @ rot @ Matrix.Diagonal((radii[0], radii[1], radii[2], 1))
        self._finish(r["verts"], mat, m)

    def limb(self, p0, p1, r0, r1, mat, sides=5):
        """Tapered tube from p0 (radius r0) to p1 (radius r1), capped."""
        p0, p1 = Vector(p0), Vector(p1)
        d = p1 - p0
        length = d.length
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=sides,
                                  radius1=r0, radius2=max(r1, 0.0001), depth=length)
        q = Vector((0, 0, 1)).rotation_difference(d.normalized())
        m = Matrix.Translation((p0 + p1) / 2) @ q.to_matrix().to_4x4()
        self._finish(r["verts"], mat, m)

    def ball(self, center, radius, mat, segs=6, rings=4):
        self.ellipsoid(center, (radius, radius, radius), mat, segs, rings)

    def mirror_x(self):
        """Duplicate everything mirrored to the other side (x -> -x)."""
        geom = self.bm.verts[:] + self.bm.edges[:] + self.bm.faces[:]
        dup = bmesh.ops.duplicate(self.bm, geom=geom)
        vs = [g for g in dup["geom"] if isinstance(g, bmesh.types.BMVert)]
        bmesh.ops.transform(self.bm, matrix=Matrix.Diagonal((-1, 1, 1, 1)), verts=vs)

    def build(self, name, coll, parent, pivot, scale=1.0):
        bm = self.bm
        if scale != 1.0:
            bmesh.ops.transform(bm, matrix=Matrix.Diagonal((scale, scale, scale, 1)), verts=bm.verts)
            pivot = Vector(pivot) * scale
        bmesh.ops.translate(bm, vec=-Vector(pivot), verts=bm.verts)
        bmesh.ops.transform(bm, matrix=CONV, verts=bm.verts)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        for f in bm.faces:
            f.smooth = False
        me = bpy.data.meshes.new(name)
        bm.to_mesh(me)
        bm.free()
        for m in self.mats:
            me.materials.append(get_mat(m))
        ob = bpy.data.objects.new(name, me)
        coll.objects.link(ob)
        ob.parent = parent
        ob.location = to_b(pivot)
        return ob


def empty(name, coll, parent=None, loc=(0, 0, 0)):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = 0.1
    coll.objects.link(ob)
    ob.parent = parent
    ob.location = to_b(loc)
    return ob


# ─────────────────────────── fruit fly (player / rival share "male", goal = "female") ───────────────────────────

LEG_ROWS = [(0.08, 0.03, 0.08), (-0.04, 0.0, 0.0), (-0.16, -0.03, -0.08)]   # hip z, knee dz, foot dz (front -> back)


def fly(coll, female):
    s = 1.07 if female else 1.0
    body_mat = "M_MateBody" if female else "M_FlyBody"
    root = empty("BodyRoot", coll)

    # Body: head + thorax + abdomen + cheeks + eye shines (+ male tip / female stripes, lashes, bow)
    g = Geo([body_mat, "M_FlyAbdomen", "M_Cheek", "M_Shine"])
    g.ellipsoid((0, 0.30, -0.04), (0.21, 0.18, 0.22), body_mat, 10, 6)          # thorax
    g.ellipsoid((0, 0.45, 0.22), (0.24, 0.22, 0.20), body_mat, 10, 7)           # head
    if female:
        g.ellipsoid((0, 0.29, -0.33), (0.19, 0.165, 0.21), body_mat, 10, 6)     # longer abdomen
        g.limb((0, 0.29, -0.47), (0, 0.27, -0.62), 0.10, 0.0, body_mat, 8)      # pointed tip
        for z, k in ((-0.29, 0.99), (-0.39, 0.93), (-0.47, 0.76)):              # dark stripes
            g.ellipsoid((0, 0.29, z), (0.19 * k + 0.008, 0.165 * k + 0.008, 0.022), "M_FlyAbdomen", 10, 3)
    else:
        g.ellipsoid((0, 0.29, -0.30), (0.18, 0.16, 0.17), body_mat, 10, 6)      # round abdomen
        g.ellipsoid((0, 0.29, -0.36), (0.168, 0.15, 0.028), "M_FlyAbdomen", 10, 3)   # one band
        g.ellipsoid((0, 0.29, -0.42), (0.12, 0.11, 0.075), "M_FlyAbdomen", 8, 5)     # dark round tip (male)
    cheek = (0.06, 0.035, 0.02) if female else (0.05, 0.03, 0.02)
    g.ellipsoid((-0.18, 0.37, 0.36), cheek, "M_Cheek", 6, 4)
    g.ellipsoid((0.18, 0.37, 0.36), cheek, "M_Cheek", 6, 4)
    g.ellipsoid((-0.13, 0.55, 0.40), (0.035, 0.035, 0.02), "M_Shine", 6, 4)
    g.ellipsoid((0.19, 0.55, 0.40), (0.035, 0.035, 0.02), "M_Shine", 6, 4)
    if female:
        for side in (-1, 1):                                                     # eyelashes (outer top of each eye)
            for base, tip in (((0.20, 0.61, 0.36), (0.25, 0.68, 0.38)),
                              ((0.25, 0.57, 0.35), (0.32, 0.62, 0.36)),
                              ((0.27, 0.52, 0.34), (0.34, 0.54, 0.35))):
                g.limb((side * base[0], base[1], base[2]), (side * tip[0], tip[1], tip[2]), 0.02, 0.005, "M_FlyAbdomen", 4)
        g.ellipsoid((0.06, 0.645, 0.15), (0.07, 0.045, 0.03), "M_Cheek", 6, 4, roll=-25)   # bow, top right of the head
        g.ellipsoid((0.20, 0.645, 0.15), (0.07, 0.045, 0.03), "M_Cheek", 6, 4, roll=25)
        g.ball((0.13, 0.64, 0.155), 0.035, "M_Cheek")
    g.build("Body", coll, root, (0, 0, 0), s)

    for side, name in ((-1, "EyeL"), (1, "EyeR")):
        e = Geo(["M_FlyEye"])
        c = (side * 0.16, 0.49, 0.32)
        e.ellipsoid(c, (0.11, 0.13, 0.09), "M_FlyEye", 8, 6)
        e.build(name, coll, root, c, s)

    for row, (hz, kdz, fdz) in enumerate(LEG_ROWS):
        for side, lr in ((-1, "L"), (1, "R")):
            hip = Vector((side * 0.15, 0.17, hz))
            knee = hip + Vector((side * 0.13, 0.03, kdz))
            foot = hip + Vector((side * 0.20, -0.16, fdz))
            lg = Geo(["M_FlyAbdomen"])
            lg.limb(hip + Vector((-side * 0.05, 0.02, 0)), knee, 0.038, 0.032, "M_FlyAbdomen", 5)
            lg.ball(knee, 0.034, "M_FlyAbdomen", 5, 4)
            lg.limb(knee, foot, 0.032, 0.02, "M_FlyAbdomen", 5)
            lg.build(f"Leg{lr}{row + 1}", coll, root, hip, s)

    for side, name in ((-1, "WingL"), (1, "WingR")):
        pivot = Vector((side * 0.10, 0.47, -0.05))
        w = Geo(["M_FlyWing"])
        c = pivot + rot_y(Vector((side * 0.07, 0, -0.18)), side * 20)
        w.ellipsoid(c, (0.12, 0.012, 0.19), "M_FlyWing", 10, 4, yaw=side * 20)
        w.build(name, coll, root, pivot, s)

    for side, name in ((-1, "AntennaL"), (1, "AntennaR")):
        pivot = Vector((side * 0.07, 0.63, 0.30))
        a = Geo(["M_FlyAbdomen"])
        a.limb(pivot + Vector((0, -0.04, -0.01)), pivot + Vector((side * 0.025, 0.12, 0.06)), 0.028, 0.02, "M_FlyAbdomen", 5)
        a.ball(pivot + Vector((side * 0.03, 0.14, 0.07)), 0.045, "M_FlyAbdomen", 6, 4)
        a.build(name, coll, root, pivot, s)

    if not female:   # angry brows: inactive on the player, active on the rival (Enemy_Male)
        b = Geo(["M_FlyAbdomen"])
        b.limb((-0.24, 0.655, 0.39), (-0.08, 0.60, 0.43), 0.032, 0.03, "M_FlyAbdomen", 4)
        b.limb((0.24, 0.655, 0.39), (0.08, 0.60, 0.43), 0.032, 0.03, "M_FlyAbdomen", 4)
        b.build("Brows", coll, root, (0, 0.62, 0.40), s)
    return root


# ─────────────────────────── spider (Enemy_Spider/Visual space: ground at y = -0.25) ───────────────────────────

SPIDER_LEGS = [(0.22, 0.08, 0.22), (0.16, 0.03, 0.08), (0.10, -0.03, -0.08), (0.04, -0.08, -0.22)]


def spider(coll):
    root = empty("BodyRoot", coll)
    g = Geo(["M_Enemy_SpiderHead", "M_Enemy_SpiderBody", "M_Enemy_SpiderEye"])
    g.ellipsoid((0, -0.12, 0.14), (0.15, 0.11, 0.15), "M_Enemy_SpiderHead", 10, 6)        # cephalothorax
    g.ellipsoid((0, -0.06, -0.20), (0.26, 0.22, 0.28), "M_Enemy_SpiderBody", 10, 7)       # abdomen
    for c in ((-0.09, 0.145, -0.17), (0.09, 0.145, -0.17), (0, 0.14, -0.31)):            # red spots on the back
        g.ellipsoid(c, (0.045, 0.02, 0.045), "M_Enemy_SpiderEye", 6, 3)
    for side in (-1, 1):
        g.ball((side * 0.05, -0.06, 0.27), 0.042, "M_Enemy_SpiderEye")                     # big eyes
        g.ball((side * 0.11, -0.08, 0.24), 0.028, "M_Enemy_SpiderEye")                     # small eyes
        g.limb((side * 0.045, -0.15, 0.26), (side * 0.035, -0.235, 0.29), 0.03, 0.008, "M_Enemy_SpiderHead", 4)  # fangs
    g.build("Body", coll, root, (0, 0, 0))

    for i, (hz, kdz, fdz) in enumerate(SPIDER_LEGS):
        for side, lr in ((-1, "L"), (1, "R")):
            hip = Vector((side * 0.10, -0.12, hz))
            knee = hip + Vector((side * 0.17, 0.12, kdz))
            foot = hip + Vector((side * 0.33, -0.13, fdz))
            lg = Geo(["M_Enemy_SpiderHead"])
            lg.limb(hip + Vector((-side * 0.04, 0, 0)), knee, 0.034, 0.028, "M_Enemy_SpiderHead", 5)
            lg.ball(knee, 0.032, "M_Enemy_SpiderHead", 5, 4)
            lg.limb(knee, foot, 0.028, 0.012, "M_Enemy_SpiderHead", 5)
            lg.build(f"Leg{lr}{i + 1}", coll, root, hip)
    return root


# ─────────────────────────── dragonfly (Enemy_Dragonfly/Visual space) ───────────────────────────

def dragonfly(coll):
    root = empty("BodyRoot", coll)
    body, tail = "M_Enemy_DragonflyBody", "M_Enemy_DragonflyTail"
    g = Geo([body, tail, "M_Enemy_DragonflyEye"])
    g.ellipsoid((0, 0.0, 0.18), (0.10, 0.10, 0.14), body, 10, 6)                     # thorax
    g.ellipsoid((0, 0.0, 0.35), (0.08, 0.075, 0.07), body, 8, 5)                      # head
    for side in (-1, 1):
        g.ellipsoid((side * 0.075, 0.035, 0.39), (0.085, 0.085, 0.08), "M_Enemy_DragonflyEye", 8, 6)
    for i in range(7):                                                                 # striped abdomen
        r = 0.05 - i * 0.003
        g.ellipsoid((0, 0.0, 0.02 - i * 0.13), (r, r, 0.078), body if i % 2 == 0 else tail, 8, 5)
    for z in (0.24, 0.17, 0.10):                                                      # tucked legs
        for side in (-1, 1):
            g.limb((side * 0.04, -0.06, z), (side * 0.09, -0.15, z + 0.04), 0.016, 0.01, tail, 4)
    g.build("Body", coll, root, (0, 0, 0))

    for name, side, pz, sweep in (("WingFL", -1, 0.22, 8), ("WingFR", 1, 0.22, 8),
                                  ("WingBL", -1, 0.12, -8), ("WingBR", 1, 0.12, -8)):
        pivot = Vector((side * 0.07, 0.07, pz))
        w = Geo(["M_Enemy_DragonflyWing"])
        c = pivot + rot_y(Vector((side * 0.30, 0, 0)), -side * sweep)
        w.ellipsoid(c, (0.30, 0.01, 0.065), "M_Enemy_DragonflyWing", 10, 4, yaw=-side * sweep)
        w.build(name, coll, root, pivot)
    return root


# ─────────────────────────── build, export, preview ───────────────────────────

CREATURES = [
    ("Mdl_FlyMale", lambda c: fly(c, False)),
    ("Mdl_FlyFemale", lambda c: fly(c, True)),
    ("Mdl_Spider", spider),
    ("Mdl_Dragonfly", dragonfly),
]


def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for coll in list(bpy.data.collections):
        bpy.data.collections.remove(coll)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)


def export(coll, path):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in coll.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = coll.objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type='FACE', use_tspace=False,
        add_leaf_bones=False, bake_anim=False, use_custom_props=False, path_mode='AUTO')


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    clear_scene()
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    report = []
    for i, (fname, make) in enumerate(CREATURES):
        coll = bpy.data.collections.new(fname)
        scene.collection.children.link(coll)
        root = make(coll)
        bpy.context.view_layer.update()
        path = os.path.join(OUT_DIR, fname + ".fbx")
        export(coll, path)
        tris = sum(sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in coll.objects if ob.type == 'MESH')
        report.append(f"{fname}: {len(coll.objects)} objects, {tris} tris -> {path}")
        # Unique names inside the .blend (each FBX already has the plain names Unity needs)
        for ob in coll.objects:
            ob.name = fname[4:] + "." + ob.name
        root.location.x = i * PREVIEW_GAP        # preview row (re-run the script to re-export)
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("\n".join(report))
    return report


REPORT = main()
