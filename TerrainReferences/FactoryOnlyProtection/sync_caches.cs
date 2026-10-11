var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();int count=0;
foreach(var t in mm.GetComponentsInChildren<Terrain>(true)){
 var td=t.terrainData;var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
 detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution));
 var a=td.GetAlphamaps(0,0,td.alphamapResolution,td.alphamapResolution);
 if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(a,l);
 EditorUtility.SetDirty(tile);count++;
}
return new{cacheCount=count};
