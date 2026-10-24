const bool apply=false;
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
if(UnityEditor.EditorApplication.isPlaying||mm.IsGenerating())throw new System.Exception("Editor must be idle");
var src=mm.graph.generators.OfType<TerrainHeightSource>().Single();
var frame=UnityEngine.GameObject.Find("工厂道路向内").transform;
var a=UnityEngine.GameObject.Find("厂门连接路向内").GetComponent<UnityEngine.Splines.SplineContainer>();
var b=UnityEngine.GameObject.Find("厂门连接路向外").GetComponent<UnityEngine.Splines.SplineContainer>();
var terrainColliders=UnityEngine.Terrain.activeTerrains.Select(t=>t.GetComponent<UnityEngine.TerrainCollider>()).ToArray();
var points=new System.Collections.Generic.List<UnityEngine.Vector3>();
for(int i=0;i<=400;i++){
    var p=((UnityEngine.Vector3)a.EvaluatePosition(i/400f)+(UnityEngine.Vector3)b.EvaluatePosition(i/400f))*.5f;
    foreach(var tc in terrainColliders){UnityEngine.RaycastHit hit;if(tc.Raycast(new UnityEngine.Ray(p+UnityEngine.Vector3.up*50,UnityEngine.Vector3.down),out hit,100)){p.y=hit.point.y;break;}}
    points.Add(p);
}
float minX=points.Min(p=>p.x)-16,maxX=points.Max(p=>p.x)+16,minZ=points.Min(p=>p.z)-16,maxZ=points.Max(p=>p.z)+16;
var region=UnityEngine.Rect.MinMaxRect(minX,minZ,maxX,maxZ);
System.Func<float,float,float,float> grade=(x,z,old)=>{
    float station=frame.InverseTransformPoint(new UnityEngine.Vector3(x,frame.position.y,z)).x;
    if(station<=132||station>=455)return old;
    float best=16*16,target=old;
    for(int i=0;i<points.Count-1;i++){
        var p=points[i];var q=points[i+1];float dx=q.x-p.x,dz=q.z-p.z;
        float t=UnityEngine.Mathf.Clamp01(((x-p.x)*dx+(z-p.z)*dz)/(dx*dx+dz*dz));
        float ex=x-p.x-t*dx,ez=z-p.z-t*dz,dist=ex*ex+ez*ez;
        if(dist<best){best=dist;target=UnityEngine.Mathf.Lerp(p.y,q.y,t);}
    }
    float weight=(1-UnityEngine.Mathf.SmoothStep(0,1,(UnityEngine.Mathf.Sqrt(best)-8.4f)/7.6f))*UnityEngine.Mathf.SmoothStep(0,1,(station-132)/6)*(1-UnityEngine.Mathf.SmoothStep(0,1,(station-445)/10));
    return UnityEngine.Mathf.Lerp(old,target,weight);
};
var report=new System.Collections.Generic.List<object>();
if(apply){UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Grade widened approach road shoulders");}
foreach(var item in new[]{(asset:src.main,name:"main"),(asset:src.draft,name:"draft")}){
    int n=item.asset.matrix.rect.size.x;float ox=mm.transform.position.x+src.offset.x,oz=mm.transform.position.z+src.offset.y;
    int x0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minX-ox)/src.size.x*(n-1)),0,n-1),x1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxX-ox)/src.size.x*(n-1)),0,n-1);
    int z0=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.FloorToInt((minZ-oz)/src.size.y*(n-1)),0,n-1),z1=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((maxZ-oz)/src.size.y*(n-1)),0,n-1);
    if(apply)UnityEditor.Undo.RegisterCompleteObjectUndo(item.asset,"Widen roadbed height source");
    int changed=0;float fill=0,cut=0;
    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
        int index=z*n+x;float old=item.asset.matrix.arr[index]*1100+mm.transform.position.y;
        float value=grade(ox+x*src.size.x/(n-1),oz+z*src.size.y/(n-1),old);float delta=value-old;
        if(UnityEngine.Mathf.Abs(delta)<.0001f)continue;changed++;fill=UnityEngine.Mathf.Max(fill,delta);cut=UnityEngine.Mathf.Min(cut,delta);
        if(apply)item.asset.matrix.arr[index]=(value-mm.transform.position.y)/1100;
    }
    if(apply)UnityEditor.EditorUtility.SetDirty(item.asset);
    report.Add(new{source=item.name,changed,maxFill=fill,maxCut=cut});
}
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
                float old=heights[z,x]*size.y+pos.y;float value=grade(pos.x+(x+x0)*size.x/(n-1),pos.z+(z+z0)*size.z/(n-1),old);
                if(UnityEngine.Mathf.Abs(value-old)<.0001f)continue;heights[z,x]=(value-pos.y)/size.y;changed++;
            }
            if(changed==0)continue;
            UnityEditor.Undo.RegisterCompleteObjectUndo(td,"Widen approach roadbed");td.SetHeights(x0,z0,heights);terrain.Flush();UnityEditor.EditorUtility.SetDirty(td);
            var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;
            detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));detail.data?.Clear(inSubs:true);UnityEditor.EditorUtility.SetDirty(tile);
            for(int i=0;i<points.Count-1;i+=20){var p=points[i];var q=points[UnityEngine.Mathf.Min(i+20,points.Count-1)];MapMagicTerrainTextureSync.RefreshTerrain(terrain,UnityEngine.Rect.MinMaxRect(UnityEngine.Mathf.Min(p.x,q.x)-16,UnityEngine.Mathf.Min(p.z,q.z)-16,UnityEngine.Mathf.Max(p.x,q.x)+16,UnityEngine.Mathf.Max(p.z,q.z)+16),true);}
            var alpha=td.GetAlphamaps(0,0,td.alphamapResolution,td.alphamapResolution);if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(alpha,l);
            report.Add(new{terrain=terrain.name,changed});
        }
    }finally{UnityEditor.EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
    foreach(var road in UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>(UnityEngine.FindObjectsSortMode.None))road.Rebuild();
    var storm=mm.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).Single(t=>t.name=="Factory Sandstorm");
    var disabled=new System.Collections.Generic.List<UnityEngine.GameObject>();for(var t=storm;t!=null;t=t.parent)if(!t.gameObject.activeSelf)disabled.Add(t.gameObject);
    var selected=UnityEditor.Selection.activeObject;try{foreach(var g in disabled)g.SetActive(true);FactoryHeatHazeSetup.Refresh();}finally{foreach(var g in disabled)g.SetActive(false);UnityEditor.Selection.activeObject=selected;}
    UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mm.gameObject.scene);
}
System.IO.File.WriteAllText("Library/RoadGateAlignment/grading.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{applied=apply,report},Newtonsoft.Json.Formatting.Indented));
return new{applied=apply,report};
