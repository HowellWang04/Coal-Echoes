var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();var ts=mm.GetComponentsInChildren<Terrain>(true);
var r=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("TerrainReferences/FactoryHighway/route.json"));
var gate=new Vector2((float)r["gate"][0],(float)r["gate"][1]);var f=new Vector2((float)r["forward"][0],(float)r["forward"][1]);var side=new Vector2((float)r["side"][0],(float)r["side"][1]);int updates=0;
var regions=new System.Collections.Generic.List<Rect>();
for(float station=-800;station<280;station+=80){var a=gate+f*350+side*station;var b=gate+f*350+side*(station+80);regions.Add(Rect.MinMaxRect(Mathf.Min(a.x,b.x)-45,Mathf.Min(a.y,b.y)-45,Mathf.Max(a.x,b.x)+45,Mathf.Max(a.y,b.y)+45));}
var points=r["approach"].Select(p=>new Vector2((float)p[0],(float)p[2])).ToArray();
for(int i=1;i<points.Length;i++){var a=points[i-1];var b=points[i];regions.Add(Rect.MinMaxRect(Mathf.Min(a.x,b.x)-45,Mathf.Min(a.y,b.y)-45,Mathf.Max(a.x,b.x)+45,Mathf.Max(a.y,b.y)+45));}
foreach(var region in regions)foreach(var t in ts){var p=t.transform.position;var sz=t.terrainData.size;if(!new Rect(p.x,p.z,sz.x,sz.z).Overlaps(region))continue;updates+=MapMagicTerrainTextureSync.RefreshTerrain(t,region,true);}
foreach(var t in ts){var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;var td=t.terrainData;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution));var a=td.GetAlphamaps(0,0,td.alphamapResolution,td.alphamapResolution);if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(a,l);EditorUtility.SetDirty(tile);}
var storm=mm.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Factory Sandstorm");
var disabled=new System.Collections.Generic.List<GameObject>();for(var t=storm;t!=null;t=t.parent)if(!t.gameObject.activeSelf)disabled.Add(t.gameObject);var selected=Selection.activeObject;try{foreach(var g in disabled)g.SetActive(true);FactoryHeatHazeSetup.Refresh();}finally{foreach(var g in disabled)g.SetActive(false);Selection.activeObject=selected;}AssetDatabase.SaveAssets();return new{updates};

