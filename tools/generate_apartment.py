#!/usr/bin/env python3
"""Генерує scenes/main/Main.tscn: плоский прототип квартири зі схеми GDD.

Запуск з кореня проєкту:  python3 tools/generate_apartment.py
Усі розміри в метрах. Північ = -Z (гравець на старті дивиться на північ).

Кімнати (по осях стін):
  Living   x[-3, 3]    z[1, 7]
  Hall     x[-1.5,1.5] z[-3, 1]
  Bedroom  x[-3, 3]    z[-8,-3]
  Bathroom x[-5.5,-1.5] z[-3, 1]   (темна)
  Storage  x[1.5, 5.5] z[-3, 1]   (темна)
"""
import math

T = 0.2          # товщина стін
H = 3.0          # висота стін
DOOR_W = 1.0
DOOR_H = 2.1

MAIN_UID = "uid://1a5feh3jx6up"   # uid існуючої Main.tscn (прописаний у project.godot)

sub = {}          # (kind, size) -> id
sub_lines = []
node_lines = []

def fmt(v):
    s = f"{v:.4f}".rstrip("0").rstrip(".")
    return s if s not in ("", "-0") else "0"

def vec(x, y, z):
    return f"Vector3({fmt(x)}, {fmt(y)}, {fmt(z)})"

def transform(x, y, z, yaw_deg=0.0):
    a = math.radians(yaw_deg)
    c, s = round(math.cos(a), 6), round(math.sin(a), 6)
    return f"Transform3D({fmt(c)}, 0, {fmt(s)}, 0, 1, 0, {fmt(-s)}, 0, {fmt(c)}, {fmt(x)}, {fmt(y)}, {fmt(z)})"

def get_box(kind, size, material):
    key = (kind, size, material)
    if key not in sub:
        sid = f"{kind}_{len(sub)}"
        sub[key] = sid
        if kind == "mesh":
            sub_lines.append(f'[sub_resource type="BoxMesh" id="{sid}"]\nmaterial = SubResource("{material}")\nsize = {vec(*size)}\n')
        else:
            sub_lines.append(f'[sub_resource type="BoxShape3D" id="{sid}"]\nsize = {vec(*size)}\n')
    return sub[key]

def static_box(name, parent, size, pos, material):
    mesh = get_box("mesh", size, material)
    shape = get_box("shape", size, material)
    node_lines.append(f'[node name="{name}" type="StaticBody3D" parent="{parent}"]\ntransform = {transform(*pos)}\n')
    node_lines.append(f'[node name="MeshInstance3D" type="MeshInstance3D" parent="{parent}/{name}"]\nmesh = SubResource("{mesh}")\n')
    node_lines.append(f'[node name="CollisionShape3D" type="CollisionShape3D" parent="{parent}/{name}"]\nshape = SubResource("{shape}")\n')

wall_index = 0

def wall(axis, fixed, a, b, gaps=()):
    """axis 'x': стіна вздовж X на z=fixed. axis 'z': вздовж Z на x=fixed.
    gaps: центри прорізів під двері (вздовж стіни)."""
    global wall_index
    ext = T / 2 if axis == "x" else 0.0     # X-стіни подовжуємо, щоб закрити кути
    a -= ext
    b += ext
    cur = a
    segments = []
    for g in sorted(gaps):
        lo = g - DOOR_W / 2
        if lo - cur > 0.001:
            segments.append((cur, lo))
        cur = g + DOOR_W / 2
    if b - cur > 0.001:
        segments.append((cur, b))

    def place(name, lo, hi, y, height):
        length = hi - lo
        mid = (lo + hi) / 2
        size = (length, height, T) if axis == "x" else (T, height, length)
        pos = (mid, y, fixed) if axis == "x" else (fixed, y, mid)
        static_box(name, "Environment", size, pos, "mat_wall")

    for lo, hi in segments:
        wall_index += 1
        place(f"Wall_{wall_index}", lo, hi, H / 2, H)
    for g in gaps:   # перемичка над дверима
        wall_index += 1
        place(f"Lintel_{wall_index}", g - DOOR_W / 2, g + DOOR_W / 2,
              DOOR_H + (H - DOOR_H) / 2, H - DOOR_H)

# ---------------- геометрія ----------------
# H-стіни (вздовж X)
wall("x", 7, -3, 3)
wall("x", 1, -5.5, 5.5, gaps=[0])
wall("x", -3, -5.5, 5.5, gaps=[0])
wall("x", -8, -3, 3)
# V-стіни (вздовж Z)
wall("z", -3, 1, 7)
wall("z", 3, 1, 7)
wall("z", -3, -8, -3)
wall("z", 3, -8, -3)
wall("z", -1.5, -3, 1, gaps=[-1])
wall("z", 1.5, -3, 1, gaps=[-1])
wall("z", -5.5, -3, 1)
wall("z", 5.5, -3, 1)

# підлога і стеля на всю будівлю
static_box("Floor", "Environment", (11.4, 0.2, 15.4), (0, -0.1, -0.5), "mat_floor")
static_box("Ceiling", "Environment", (11.4, 0.2, 15.4), (0, H + 0.1, -0.5), "mat_ceiling")

# ---------------- двері ----------------
doors = [
    # name,            hinge x, z,    base yaw, open dir
    ("Door_LivingHall",  -0.5, 1.0,   0,   1),
    ("Door_HallBedroom", -0.5, -3.0,  0,   1),
    ("Door_HallBathroom", -1.5, -1.5, -90, -1),
    ("Door_HallStorage",  1.5, -1.5,  -90, 1),
]
for name, x, z, yaw, direction in doors:
    node_lines.append(
        f'[node name="{name}" parent="Environment" instance=ExtResource("door")]\n'
        f'transform = {transform(x, 0, z, yaw)}\n'
        f'OpenDirection = {direction}\n')

# ---------------- кімнати (зони + світло) ----------------
# id, центр x, z, розмір x, z, світло (energy, range) або None
rooms = [
    ("living",   0.0,  4.0, 5.7, 5.7, (1.0, 9.0)),
    ("hall",     0.0, -1.0, 2.7, 3.7, (0.5, 6.0)),
    ("bedroom",  0.0, -5.5, 5.7, 4.7, (0.8, 8.0)),
    ("bathroom", -3.5, -1.0, 3.7, 3.7, None),
    ("storage",   3.5, -1.0, 3.7, 3.7, None),
]
for rid, cx, cz, sx, sz, light in rooms:
    sid = f"zone_{rid}"
    sub_lines.append(f'[sub_resource type="BoxShape3D" id="{sid}"]\nsize = {vec(sx, 3.0, sz)}\n')
    name = rid.capitalize()
    node_lines.append(
        f'[node name="{name}" type="Area3D" parent="Rooms"]\n'
        f'transform = {transform(cx, 1.5, cz)}\n'
        f'script = ExtResource("zone")\n'
        f'RoomId = "{rid}"\n')
    node_lines.append(
        f'[node name="CollisionShape3D" type="CollisionShape3D" parent="Rooms/{name}"]\n'
        f'shape = SubResource("{sid}")\n')
    if light:
        energy, rng = light
        node_lines.append(
            f'[node name="Light" type="OmniLight3D" parent="Rooms/{name}" groups=["behavior_adaptive_light"]]\n'
            f'transform = {transform(0, 1.1, 0)}\n'
            f'light_energy = {fmt(energy)}\n'
            f'shadow_enabled = true\n'
            f'omni_range = {fmt(rng)}\n'
            f'script = ExtResource("light")\n'
            f'FlickerMinEnergy = 0.02\n'
            f'FlickerDuration = 0.7\n')

# ---------------- предмети, які гра може зрушити ----------------
props = [
    # name, room, size, pos
    ("Table",  "living",   (0.9, 0.75, 0.9), (-1.5, 0.375, 3.5)),
    ("Chair",  "bedroom",  (0.5, 0.9, 0.5),  (1.5, 0.45, -6.0)),
    ("Bucket", "bathroom", (0.4, 0.5, 0.4),  (-4.0, 0.25, -0.5)),
    ("Crate",  "storage",  (0.7, 0.7, 0.7),  (4.0, 0.35, -0.5)),
]
for name, room, size, pos in props:
    mesh = get_box("mesh", size, "mat_prop")
    shape = get_box("shape", size, "mat_prop")
    node_lines.append(
        f'[node name="{name}" type="StaticBody3D" parent="Props"]\n'
        f'transform = {transform(*pos)}\n'
        f'script = ExtResource("prop")\n'
        f'RoomId = "{room}"\n')
    node_lines.append(f'[node name="MeshInstance3D" type="MeshInstance3D" parent="Props/{name}"]\nmesh = SubResource("{mesh}")\n')
    node_lines.append(f'[node name="CollisionShape3D" type="CollisionShape3D" parent="Props/{name}"]\nshape = SubResource("{shape}")\n')

# ---------------- збірка файлу ----------------
header = f'''[gd_scene format=3 uid="{MAIN_UID}"]

[ext_resource type="PackedScene" uid="uid://ceisu5x1yjlhx" path="res://scenes/environment/Door.tscn" id="door"]
[ext_resource type="PackedScene" uid="uid://7vgw5ui7wwuv" path="res://scenes/player/Player.tscn" id="player"]
[ext_resource type="Script" uid="uid://d2jpf53s7lac2" path="res://scripts/environment/AdaptiveLight.cs" id="light"]
[ext_resource type="Script" path="res://scripts/environment/RoomZone.cs" id="zone"]
[ext_resource type="Script" path="res://scripts/environment/MovableProp.cs" id="prop"]

[sub_resource type="StandardMaterial3D" id="mat_wall"]
albedo_color = Color(0.62, 0.6, 0.56, 1)
roughness = 1.0

[sub_resource type="StandardMaterial3D" id="mat_floor"]
albedo_color = Color(0.33, 0.3, 0.27, 1)
roughness = 1.0

[sub_resource type="StandardMaterial3D" id="mat_ceiling"]
albedo_color = Color(0.5, 0.5, 0.48, 1)
roughness = 1.0

[sub_resource type="StandardMaterial3D" id="mat_prop"]
albedo_color = Color(0.45, 0.3, 0.2, 1)
roughness = 1.0

[sub_resource type="Environment" id="env_main"]
background_mode = 1
background_color = Color(0.0627451, 0.06666667, 0.078431375, 1)
ambient_light_source = 2
ambient_light_color = Color(0.1882353, 0.2, 0.22745098, 1)
ambient_light_energy = 0.25

'''

root = '''[node name="Main" type="Node3D"]

[node name="Environment" type="Node3D" parent="."]

[node name="Rooms" type="Node3D" parent="."]

[node name="Props" type="Node3D" parent="."]

'''

tail = f'''[node name="Player" parent="." instance=ExtResource("player")]
transform = {transform(0, 0.9, 5.5)}

[node name="WorldEnvironment" type="WorldEnvironment" parent="."]
environment = SubResource("env_main")
'''

# вузли Environment/Rooms/Props мають бути оголошені до дітей
body = header + "\n".join(sub_lines) + "\n" + root + "\n".join(node_lines) + "\n" + tail
open("scenes/main/Main.tscn", "w", encoding="utf-8", newline="\n").write(body)
print("written scenes/main/Main.tscn:", len(node_lines), "node blocks")
