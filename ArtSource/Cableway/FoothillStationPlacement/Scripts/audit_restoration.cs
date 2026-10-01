var root="ArtSource/Cableway/FoothillStationPlacement/";
var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(root+"terrain_before.json"));
string Sha(byte[] b)=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-","");
var result=new System.Collections.Generic.List<object>();
foreach(var row in rows){
 var td=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainData>((string)row["path"]);int n=td.heightmapResolution;var h=td.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];System.Buffer.BlockCopy(h,0,bytes,0,bytes.Length);
 var hash=Sha(bytes);int outsideChanges=0,changed=0;float max=0,patchError=0;
 if((bool)row["affected"]){
  var before=new float[n*n];var expected=new float[n*n];System.Buffer.BlockCopy(System.IO.File.ReadAllBytes(root+"Before/"+(string)row["file"]+".f32"),0,before,0,bytes.Length);System.Buffer.BlockCopy(System.IO.File.ReadAllBytes(root+(string)row["file"]+"_restored.f32"),0,expected,0,bytes.Length);
  for(int i=0;i<n*n;i++){float now=h[i/n,i%n];if(now!=before[i]){changed++;max=UnityEngine.Mathf.Max(max,UnityEngine.Mathf.Abs(now-before[i])*td.size.y);if(before[i]==expected[i])outsideChanges++;}patchError=UnityEngine.Mathf.Max(patchError,UnityEngine.Mathf.Abs(now-expected[i])*td.size.y);}
  System.IO.File.WriteAllBytes(root+(string)row["file"]+"_actual.f32",bytes);
 }
 result.Add(new{path=(string)row["path"],affected=(bool)row["affected"],unchanged=hash==(string)row["sha256"],sha256=hash,changed,outsideChanges,maxChangeMetres=max,patchErrorMetres=patchError});
}
System.IO.File.WriteAllText(root+"terrain_after.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
