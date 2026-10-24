// One-off edit (2026-10-21): smooths the crest and the grade wobble on the gate connection road, 4-84 m outside the gate.
// Run through Unity MCP execute_code. Set apply=false for a dry run that only reports the changes.
// The old profile was level to about 26 m, dropped to an 18% grade within 12 m, eased to 8% around 52 m and steepened
// again. Between stations 4 m and 84 m the centerline profile is replaced with one cubic Hermite curve matching the
// existing height and slope at both ends (end slope fitted over the next 16 m); the change covers the 8.4 m roadbed
// and fades out by 16 m from the centerline. Everything else follows smooth-gate-road-bump.cs.
const bool apply=false;
const float s0=4f,s1=84f,flat=8.4f,fade=7.6f;
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
if(UnityEditor.EditorApplication.isPlaying||mm.IsGenerating())throw new System.Exception("Editor must be idle");
var src=mm.graph.generators.OfType<TerrainHeightSource>().Single();
var a=UnityEngine.GameObject.Find("厂门连接路向内").GetComponent<UnityEngine.Splines.SplineContainer>();
var b=UnityEngine.GameObject.Find("厂门连接路向外").GetComponent<UnityEngine.Splines.SplineContainer>();
var cols=UnityEngine.Terrain.activeTerrains.Select(t=>t.GetComponent<UnityEngine.TerrainCollider>()).ToArray();
System.Func<UnityEngine.Vector3,float> groundY=p=>{foreach(var tc in cols){UnityEngine.RaycastHit hit;if(tc.Raycast(new UnityEngine.Ray(p+UnityEngine.Vector3.up*80,UnityEngine.Vector3.down),out hit,200))return hit.point.y;}return float.NaN;};
float len=a.CalculateLength();
// centerline every 0.25 m from 0 m to 110 m, with the current terrain height
var stations=new System.Collections.Generic.List<float>();var points=new System.Collections.Generic.List<UnityEngine.Vector3>();
for(float s=0;s<=110.001f;s+=.25f){var p=((UnityEngine.Vector3)a.EvaluatePosition(s/len)+(UnityEngine.Vector3)b.EvaluatePosition(s/len))*.5f;p.y=groundY(p);stations.Add(s);points.Add(p);}
System.Func<float,float> oldAt=s=>{int i=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt(s/.25f),0,points.Count-2);float t=(s-stations[i])/.25f;return UnityEngine.Mathf.Lerp(points[i].y,points[i+1].y,t);};
// least-squares slope over the steady grade past s1, so 1 m heightmap noise does not set the end tangent
float fitSlope(float from,float to){float n=0,sx=0,sy=0,sxx=0,sxy=0;for(float s=from;s<=to;s+=.25f){float y=oldAt(s);n++;sx+=s;sy+=y;sxx+=s*s;sxy+=s*y;}return(n*sxy-sx*sy)/(n*sxx-sx*sx);}
float h0=oldAt(s0),h1=oldAt(s1),m0=(oldAt(s0+1)-oldAt(s0-1))*.5f,m1=fitSlope(s1,s1+16),L=s1-s0;
System.Func<float,float> delta=s=>{
    if(s<=s0||s>=s1)return 0;
    float u=(s-s0)/L,u2=u*u,u3=u2*u;
    float h=(2*u3-3*u2+1)*h0+(u3-2*u2+u)*m0*L+(-2*u3+3*u2)*h1+(u3-u2)*m1*L;
    return h-oldAt(s);
};
// nearest centerline segment -> station and distance
System.Func<float,float,float> change=(x,z)=>{
    float best=float.MaxValue,bestS=0;
    for(int i=0;i<points.Count-1;i++){
        var p=points[i];var q=points[i+1];float dx=q.x-p.x,dz=q.z-p.z;
        float t=UnityEngine.Mathf.Clamp01(((x-p.x)*dx+(z-p.z)*dz)/(dx*dx+dz*dz));
        float ex=x-p.x-t*dx,ez=z-p.z-t*dz,d=ex*ex+ez*ez;
        if(d<best){best=d;bestS=UnityEngine.Mathf.Lerp(stations[i],stations[i+1],t);}
    }
    float dist=UnityEngine.Mathf.Sqrt(best);if(dist>=flat+fade)return 0;
    return delta(bestS)*(1-UnityEngine.Mathf.SmoothStep(0,1,(dist-flat)/fade));
};
var seg=points.Where((p,i)=>stations[i]>=s0-1&&stations[i]<=s1+1).ToList();
float minX=seg.Min(p=>p.x)-flat-fade,maxX=seg.Max(p=>p.x)+flat+fade,minZ=seg.Min(p=>p.z)-flat-fade,maxZ=seg.Max(p=>p.z)+flat+fade;
var region=UnityEngine.Rect.MinMaxRect(minX,minZ,maxX,maxZ);
var report=new System.Collections.Generic.List<object>();
var profile=new System.Collections.Generic.List<string>();
for(float s=s0-2;s<=s1+4;s+=2)profile.Add($"s={s} old={oldAt(s):F2} new={oldAt(s)+delta(s):F2} grade={(oldAt(s+1)+delta(s+1)-oldAt(s-1)-delta(s-1))*50:F1}%");
report.Add(new{m0,m1,profile});
if(apply){UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Smooth gate road crest");}
foreach(var item in new[]{(asset:src.main,name:"main"),(asset:src.draft,name:"draft")}){
    int n=item.asset.matrix.rect.size.x;float ox=mm.transform.position.x+src.offset.x,oz=mm.transform.position.z+src.offset.y;
    int x0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minX-ox)/src.size.x*(n-1)),0,n-1),x1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxX-ox)/src.size.x*(n-1)),0,n-1);
    int z0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minZ-oz)/src.size.y*(n-1)),0,n-1),z1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxZ-oz)/src.size.y*(n-1)),0,n-1);
    if(apply)UnityEditor.Undo.RegisterCompleteObjectUndo(item.asset,"Smooth gate road crest height source");
    int changed=0;float fill=0,cut=0;
    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
        float d=change(ox+x*src.size.x/(n-1),oz+z*src.size.y/(n-1));if(UnityEngine.Mathf.Abs(d)<.0001f)continue;
        changed++;fill=UnityEngine.Mathf.Max(fill,d);cut=UnityEngine.Mathf.Min(cut,d);
        if(apply)item.asset.matrix.arr[z*n+x]+=d/1100;
    }
    if(apply)UnityEditor.EditorUtility.SetDirty(item.asset);
    report.Add(new{source=item.name,changed,maxFill=fill,maxCut=cut});
}
// gate grass clumps and the motorcycle move with the ground as whole objects; the Player is left to settle on its own
var moverSet=new System.Collections.Generic.HashSet<UnityEngine.Transform>();
foreach(var t in UnityEngine.GameObject.Find("门口干草").transform.Cast<UnityEngine.Transform>().Append(UnityEngine.GameObject.Find("摩托车").transform)){
    var pos=t.position;if(!region.Contains(new UnityEngine.Vector2(pos.x,pos.z)))continue;
    if(UnityEngine.Mathf.Abs(change(pos.x,pos.z))>=.005f)moverSet.Add(t);
}
report.Add(new{objectsInArea=moverSet.Select(t=>new{name=t.name,delta=change(t.position.x,t.position.z)}).ToList()});
if(apply){
    bool auto=UnityEditor.EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);UnityEditor.EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
    try{
        foreach(var terrain in mm.GetComponentsInChildren<UnityEngine.Terrain>(true)){
            var td=terrain.terrainData;var pos=terrain.transform.position;var size=td.size;int n=td.heightmapResolution;
            if(!new UnityEngine.Rect(pos.x,pos.z,size.x,size.z).Overlaps(region))continue;
            int x0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minX-pos.x)/size.x*(n-1)),0,n-1),x1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxX-pos.x)/size.x*(n-1)),0,n-1);
            int z0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minZ-pos.z)/size.z*(n-1)),0,n-1),z1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxZ-pos.z)/size.z*(n-1)),0,n-1);
            var heights=td.GetHeights(x0,z0,x1-x0+1,z1-z0+1);int changed=0;
            for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++){
                float d=change(pos.x+(x+x0)*size.x/(n-1),pos.z+(z+z0)*size.z/(n-1));
                if(UnityEngine.Mathf.Abs(d)<.0001f)continue;heights[z,x]+=d/size.y;changed++;
            }
            if(changed==0)continue;
            UnityEditor.Undo.RegisterCompleteObjectUndo(td,"Smooth gate road crest");td.SetHeights(x0,z0,heights);terrain.Flush();UnityEditor.EditorUtility.SetDirty(td);
            var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;
            detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));detail.data?.Clear(inSubs:true);UnityEditor.EditorUtility.SetDirty(tile);
            MapMagicTerrainTextureSync.RefreshTerrain(terrain,region,true);
            var alpha=td.GetAlphamaps(0,0,td.alphamapResolution,td.alphamapResolution);if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(alpha,l);
            report.Add(new{terrain=terrain.name,changed});
        }
    }finally{UnityEditor.EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
    foreach(var t in moverSet){
        float d=change(t.position.x,t.position.z);
        UnityEditor.Undo.RecordObject(t,"Smooth gate road crest");t.position+=UnityEngine.Vector3.up*d;
    }
    foreach(var road in UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>())road.Rebuild();
    var storm=mm.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).Single(t=>t.name=="Factory Sandstorm");
    var disabled=new System.Collections.Generic.List<UnityEngine.GameObject>();for(var t=storm;t!=null;t=t.parent)if(!t.gameObject.activeSelf)disabled.Add(t.gameObject);
    var selected=UnityEditor.Selection.activeObject;try{foreach(var g in disabled)g.SetActive(true);FactoryHeatHazeSetup.Refresh();}finally{foreach(var g in disabled)g.SetActive(false);UnityEditor.Selection.activeObject=selected;}
    UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mm.gameObject.scene);
}
return Newtonsoft.Json.JsonConvert.SerializeObject(new{applied=apply,report},Newtonsoft.Json.Formatting.Indented);
