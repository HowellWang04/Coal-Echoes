// Samples the terrain under the dunes that reach past the factory walls, where the ground falls away.
// Run in the editor through Unity MCP execute_code. Writes Ground/<dune>.json for build_dunes.py: heights in metres
// relative to the factory floor (y -430.653) on a 0.5 m grid in the factory frame (u along the main road, v across it).
// Domains must match DUNES in build_dunes.py.
var dir = "ArtSource/SandDune/Ground/";
System.IO.Directory.CreateDirectory(dir);
var inv = System.Globalization.CultureInfo.InvariantCulture;
var gate = GameObject.Find("FactoryGate").transform;
Vector3 O = gate.position, U = gate.forward, V = gate.right;
const float Floor = -430.653f, Cell = 0.5f;
var tiles = Terrain.activeTerrains.Where(t => t.name == "Main Terrain").ToArray();
System.Func<Vector3, float> TH = w => { foreach (var t in tiles) { var p = t.GetPosition(); var s = t.terrainData.size; if (w.x >= p.x && w.x < p.x + s.x && w.z >= p.z && w.z < p.z + s.z) return t.SampleHeight(w) + p.y; } return Floor; };
var domains = new (string name, float u0, float u1, float v0, float v1)[] {
    ("SandDune_Left", 14f, 54f, -60f, -38f),
    ("SandDune_Right", 14f, 54f, 34f, 58f),
};
var report = new System.Text.StringBuilder();
foreach (var d in domains) {
    int nu = Mathf.RoundToInt((d.u1 - d.u0) / Cell) + 1, nv = Mathf.RoundToInt((d.v1 - d.v0) / Cell) + 1;
    var rows = new System.Collections.Generic.List<string>();
    float lo = 1e9f, hi = -1e9f;
    for (int i = 0; i < nu; i++) {
        var row = new float[nv];
        for (int j = 0; j < nv; j++) {
            var w = O + U * (d.u0 + i * Cell) + V * (d.v0 + j * Cell);
            row[j] = TH(w) - Floor; lo = Mathf.Min(lo, row[j]); hi = Mathf.Max(hi, row[j]);
        }
        rows.Add("[" + string.Join(",", row.Select(h => h.ToString("F3", inv))) + "]");
    }
    System.IO.File.WriteAllText(dir + d.name + ".json", string.Format(inv,
        "{{\"u0\":{0},\"v0\":{1},\"cell\":{2},\"heights\":[\n{3}\n]}}\n", d.u0, d.v0, Cell, string.Join(",\n", rows)));
    report.AppendLine($"{d.name}: {nu}x{nv} samples, height {lo:F2}..{hi:F2}");
}
return report.ToString();
