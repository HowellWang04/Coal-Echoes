# CablewayStationRollerFrame

Standalone station entrance roller frame modeled from the [approved concept](References/Approved_Concept.png), with the user's subsequent orientation correction. The lower two-column frame now runs parallel to the row of three sheaves along Y, instead of crossing it. The asset has upper knee braces, bolted connections and two concrete footings. Each sheave is a separate object with its origin on the axle and can rotate about local X. No rotation animation is included. Cable geometry is deliberately omitted so the line can be connected and configured separately.

Created with Blender 5.2.2 LTS using background Python on 2026 September 28. No Blender MCP calls were used.

## Deliverables

[Editable Blender source](Blender/CablewayStationRollerFrame.blend)

[FBX model](Exports/CablewayStationRollerFrame.fbx)

[GLB model with embedded materials](Exports/CablewayStationRollerFrame.glb)

[Overall render](Previews/RollerFrame_Overall.png)

[Wheel detail render](Previews/RollerFrame_WheelDetail.png)

[Front render](Previews/RollerFrame_Front.png)

[Reimported GLB render](Previews/RollerFrame_GLB_Roundtrip.png)

These previews are actual Blender renders of the model. The studio camera, lights and ground are kept in the source file and excluded from exports.

## Geometry and materials

The model contains 11 mesh objects and 30,028 triangles. Overall nominal dimensions in Blender are 0.88 m across X, 3.62 m along Y and 4.835 m high in Z. The footings rest at Z zero. Y follows both the lower frame and the wheel row. These are modeling dimensions chosen for the compact station shell, not historical survey measurements.

Four materials are used. Painted steel and dark sheave steel have procedural wear saved into ordinary 2048 pixel PBR textures. The restrained gray beige paint and brown oxidation follow the station palette. Concrete uses copies of the existing station concrete textures. The third texture set is also 2048 pixels. Oxidized fabrication edges use a simple fourth material without textures. Each texture set includes BaseColor, Normal, Roughness, Metallic, ORM and HDRP_Mask. ORM packs ambient occlusion as white, roughness and metallic. HDRP_Mask packs metallic, white ambient occlusion, white detail mask and smoothness. No ambient occlusion bake is claimed.

The source is configured for Cycles GPU Compute with OptiX, 48 render samples and 32 viewport samples. It opens in Material Preview. CPU threads were limited to six for the background task.

## Validation and assembly status

Source meshes have UVs, zero nonmanifold edges and zero zero-area faces. FBX and GLB were reimported into Blender. Both retained mesh count, triangle count, dimensions, UVs, wheel origins and loaded material images. The line joining the footing centers was checked against the line joining the wheel centers to verify parallel alignment. A reimported GLB was rendered from the source camera and compared with the source render. Results and delivery hashes are in [verification.json](verification.json).

This is a static visual asset. Unity import, colliders, station placement, rope contact and bucket passage have not been tested. No Unity scene or existing model was changed. It does not include a cable, drive, tension system, return bullwheel or a complete station route.

The existing saved Unity tower verification reports a bucket height of approximately 12.37 m, while the compact station shell is approximately 4.93 m high. That inherited scene scale must be reconciled before placing this asset. The longitudinal support beam and roller cradle must be checked against the actual carrying rope, hauling rope and hanger path during assembly. The open frame alone is not proof of bucket clearance.

## Reproduction

The local scripts build and verify the asset in a fresh background Blender process. The build script reads the sibling station texture folder. It does not edit a running Blender session. The editable source and exported files are the deliverables; source images are packed into the blend file.
