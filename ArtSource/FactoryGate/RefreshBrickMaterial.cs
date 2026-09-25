using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RefreshBrickMaterial
{
    public static string Frame()
    {
        var gate=GameObject.Find("In-scene Objects/Factory Entrance/FactoryGate");
        var view=SceneView.lastActiveSceneView;
        view.LookAt(gate.transform.position+Vector3.up*2.15f,
            Quaternion.LookRotation(gate.transform.forward-gate.transform.right*.18f+Vector3.down*.08f),3.8f,false,true);
        view.Repaint();
        return "Framed the updated gate from the lit rear side";
    }
    public static string Main()
    {
        const string folder="Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/";
        foreach(var path in new[]{"Textures/Brick_BaseColor.png","Textures/BrickWeathered_BaseColor.png","FactoryGate_Rebuilt.glb"})
            AssetDatabase.ImportAsset(folder+path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"FactoryGate_Rebuilt.glb");
        var gate=GameObject.Find("In-scene Objects/Factory Entrance/FactoryGate");
        var sourceMaterials=asset.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var materialStatus=gate.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct()
            .Where(m=>m.name=="Brick"||m.name=="BrickWeathered")
            .Select(m=>new {m.name,shader=m.shader.name,texture=m.GetTexture("baseColorTexture").name,
                matchesImportedAsset=sourceMaterials.Contains(m)}).ToArray();
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {
            playing=EditorApplication.isPlaying,scene=gate.scene.path,sceneDirty=gate.scene.isDirty,
            position=gate.transform.position.ToString("F4"),rotation=gate.transform.eulerAngles.ToString("F4"),scale=gate.transform.localScale.ToString(),
            materials=materialStatus,triangles=gate.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),
            brickVertexColors=gate.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>().sharedMaterial.name.StartsWith("Brick"))
                .Select(f=>new {f.name,hasColor=f.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)}).ToArray()
        },Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("ArtSource/FactoryGate/brick_color_unity.json",result);
        return result;
    }
}
