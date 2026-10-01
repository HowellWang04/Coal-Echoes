# CablewayStationShell

The current model follows [the approved compact reference](References/Approved_CompactLeanTo.png). It has one clad long side, a narrow single slope roof, an entirely open opposite side and two open ends. Only full height columns and upper knee braces remain on the open side. There are no waist rails or infill panels there.

The matte gray beige metal, restrained ochre oxidation, exposed bolts, missing corrugated sheets and weathered concrete follow the approved industrial palette. Steel color incorporates a sampled region of the existing cableway texture. Concrete edges have a small bevel. The approved compact silhouette remains unchanged.

## Current files

The authoritative editable source is [CablewayStationShell.blend](Blender/CablewayStationShell.blend). Packed material images are included. The [FBX](Exports/CablewayStationShell.fbx) and [GLB](Exports/CablewayStationShell.glb) contain only the six model meshes. Studio lights, camera and ground are excluded from exports.

Current previews are [the clad side](Previews/Station_CladSide.png) and [the open side](Previews/Station_OpenSide.png). These are actual Blender renders of the current geometry and materials.

The extents are approximately 11.35 metres long, 3.66 metres deep and 4.93 metres high, including overhangs. These are modeling dimensions rather than measurements recovered from the reference. The asset contains 20,308 triangles and three materials with 2048 pixel textures. Blender uses Z up and metres. FBX and GLB use Y up.

## Materials

Steel, cladding and concrete each have BaseColor, Normal, Roughness, Metallic, ORM and HDRP_Mask images. BaseColor uses sRGB. Other maps use linear data. Normal strength is 0.4 in Blender. HDRP_Mask uses metallic in red, constant white in green and blue, and smoothness in alpha. ORM uses constant white in red, roughness in green and metallic in blue. No baked occlusion map is included.

## Verification

Both exports were reimported into Blender 5.2.2 LTS. Each retained six meshes, 20,308 triangles, UV coordinates and loaded texture references. FBX dimension error was less than 0.000002 metres, and GLB dimensions matched exactly. The source has no nonmanifold edges or zero area faces. Imported seam splits are recorded separately and are not interpreted as source topology defects.

GLB was rendered with the same camera and lighting. Its mean absolute RGB difference from the source was approximately 0.000024 on a zero to one scale. Packed texture bytes were refreshed before final export to prevent stale images from being embedded. Detailed results are in [verification.json](verification.json).

Unity import, scene replacement, collision setup and Play Mode verification remain pending. This asset represents the station shell only and includes no drive machinery or coal bucket.

## Earlier work

The approved gray model remains in Blender/CablewayStationShell_Compact_Gray.blend. The rejected large shed, old exports and previews are retained separately in Previous/RejectedLargeBlockout. Early build scripts record the modeling process and require their original live session helpers. Continue editing from the current blend file.
