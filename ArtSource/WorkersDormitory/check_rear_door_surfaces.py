"""Detect overlapping, equally facing planar surfaces around the rear entrance."""
import bpy
import json

asset = bpy.data.collections['WorkersDormitory_Asset']
faces = []
for obj in asset.objects:
    if obj.type != 'MESH' or not obj.name.startswith((
        'RearMasonry', 'RearEntrance', 'FloorSlabs', 'Foundation')):
        continue
    for poly in obj.data.polygons:
        points = [obj.matrix_world @ obj.data.vertices[i].co for i in poly.vertices]
        low = [min(p[k] for p in points) for k in range(3)]
        high = [max(p[k] for p in points) for k in range(3)]
        if high[0] < -1 or low[0] > 1 or high[1] < 4.55 or low[1] > 6 or low[2] > 3.2:
            continue
        axis = next((k for k in range(3) if high[k]-low[k] < .00001), None)
        if axis is not None:
            faces.append((obj.name,poly.index,axis,low,high,poly.normal[axis]))
overlaps = []
for i,a in enumerate(faces):
    for b in faces[i+1:]:
        if a[2] != b[2] or a[5]*b[5] < .99 or abs(a[3][a[2]]-b[3][b[2]]) > .00001:
            continue
        axes = [k for k in range(3) if k != a[2]]
        lengths = [min(a[4][k],b[4][k])-max(a[3][k],b[3][k]) for k in axes]
        if min(lengths) > .001:
            overlaps.append({'a':a[:2], 'b':b[:2], 'area':round(lengths[0]*lengths[1],6)})
print(json.dumps({'overlapping_surface_pairs':overlaps},indent=2))
assert not overlaps, 'Rear entrance contains coplanar overlapping surfaces'
