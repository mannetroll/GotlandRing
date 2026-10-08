using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// CSV coordinates are metres: X east, Y up, Z north. Banking is encoded in
// the cross-section heights; do not apply a second rotation to the road.
public static class GotlandRingImporter
{
    private const int Across = 16;
    private const float ApronWidth = 3f; // Modeled corridor extent, not a runoff boundary.
    private const float PaintWidth = 0.12f; // Visual assumption, not measured.
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    private struct Row
    {
        public Vector3 center, right;
        public float distance, leftWidth, rightWidth, crossSlope, crossCurve;
        public float groundLeft, groundRight;
        public bool leftInferred, rightInferred;

        public Vector3 Surface(float offset)
        {
            return center + right * offset
                + Vector3.up * (crossSlope * offset + crossCurve * offset * offset);
        }
    }

    [MenuItem("Tools/Gotland Ring/Build selected surface CSV", true)]
    private static bool CanBuild() => Selection.activeObject is TextAsset;

    [MenuItem("Tools/Gotland Ring/Build selected surface CSV")]
    public static void Build()
    {
        var source = Selection.activeObject as TextAsset;
        if (source == null) throw new InvalidOperationException("Select gotland_ring_full_surface_3m.csv.");
        var rows = Parse(source.text);
        var parent = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)).Replace('\\', '/');
        var folder = parent + "/GotlandRingGenerated";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, "GotlandRingGenerated");
        var asphalt = MaterialAsset(folder, "Asphalt", new Color(0.18f, 0.19f, 0.19f));
        var ground = MaterialAsset(folder, "PaleGround", new Color(0.59f, 0.57f, 0.50f));
        var paint = MaterialAsset(folder, "WhitePaint", new Color(0.86f, 0.86f, 0.82f));
        var root = new GameObject("Gotland Ring — estimated surface");
        try
        {
            var road = Grid(rows, Across, (row, u) => row.Surface(u <= .5f
                ? Mathf.Lerp(-row.leftWidth, 0, u * 2)
                : Mathf.Lerp(0, row.rightWidth, (u - .5f) * 2)));
            Add(root, "Asphalt", SaveMesh(road, folder + "/Asphalt.asset"), asphalt, true);
            for (int side = -1; side <= 1; side += 2)
            {
                string sideName = side < 0 ? "Left" : "Right";
                var apron = Grid(rows, 3, (row, u) =>
                {
                    float width = side < 0 ? row.leftWidth : row.rightWidth;
                    float dy = side < 0 ? row.groundLeft : row.groundRight;
                    float outward = side < 0 ? 1 - u : u;
                    return row.Surface(side * width) + row.right * (side * ApronWidth * outward)
                        + Vector3.up * (dy * outward);
                });
                Add(root, sideName + " ground apron (estimated)",
                    SaveMesh(apron, folder + "/" + sideName + "Apron.asset"), ground, true);
                var line = Paint(rows, side);
                Add(root, sideName + " edge paint (estimated)",
                    SaveMesh(line, folder + "/" + sideName + "Paint.asset"), paint, false);
            }
            var origin = new GameObject("Centerline row 0");
            origin.transform.SetParent(root.transform, false);
            origin.transform.localPosition = rows[0].center;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "/GotlandRing.prefab");
            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            Debug.Log("Built Gotland Ring prefab. Width/banking are estimates; see SURFACE_README.md. "
                + "Add your vehicle and calibrated tire model. Road collider is static and non-convex.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static List<Row> Parse(string text)
    {
        using (var reader = new StringReader(text))
        {
            var header = reader.ReadLine().TrimStart('\uFEFF').Split(',');
            var columns = new Dictionary<string, int>();
            for (int j = 0; j < header.Length; j++) columns.Add(header[j], j);
            var rows = new List<Row>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = line.Split(',');
                float Get(string name)
                {
                    float value = float.Parse(fields[columns[name]], CI);
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        throw new FormatException("Non-finite CSV value: " + name);
                    return value;
                }
                var row = new Row
                {
                    center = new Vector3(Get("x_m"), Get("y_m"), Get("z_m")),
                    right = new Vector3(Get("forward_z"), 0, -Get("forward_x")).normalized,
                    distance = Get("distance_m"), leftWidth = Get("width_left_m"),
                    rightWidth = Get("width_right_m"), crossSlope = Get("cross_slope_center_percent") / 100,
                    crossCurve = Get("cross_curve_per_m"), groundLeft = Get("ground_apron_left_dy_m"),
                    groundRight = Get("ground_apron_right_dy_m"),
                    leftInferred = Get("width_left_inferred") > .5f,
                    rightInferred = Get("width_right_inferred") > .5f
                };
                if (row.leftWidth <= 0 || row.rightWidth <= 0)
                    throw new FormatException("Road widths must be positive.");
                if (rows.Count > 0 && row.distance <= rows[rows.Count - 1].distance)
                    throw new FormatException("Chainage must increase.");
                rows.Add(row);
            }
            if (rows.Count < 4 || Vector3.Distance(rows[0].center, rows[rows.Count - 1].center) > .005f)
                throw new FormatException("Expected a closed lap including the final duplicate row.");
            return rows;
        }
    }

    private static Mesh Grid(List<Row> rows, int across, Func<Row, float, Vector3> position)
    {
        var vertices = new Vector3[rows.Count * (across + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>((rows.Count - 1) * across * 6);
        for (int i = 0; i < rows.Count; i++)
            for (int j = 0; j <= across; j++)
            {
                int index = i * (across + 1) + j;
                vertices[index] = position(rows[i], (float)j / across);
                uv[index] = new Vector2(j / (float)across * (rows[i].leftWidth + rows[i].rightWidth) / 4,
                    rows[i].distance / 4);
                if (i < rows.Count - 1 && j < across) Quad(triangles, index, index + across + 1);
            }
        var mesh = Finish(vertices, uv, triangles);
        var normals = mesh.normals;
        for (int j = 0; j <= across; j++)
        {
            int last = (rows.Count - 1) * (across + 1) + j;
            normals[j] = normals[last] = (normals[j] + normals[last]).normalized;
        }
        mesh.normals = normals;
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Mesh Paint(List<Row> rows, int side)
    {
        var vertices = new Vector3[rows.Count * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new List<int>();
        for (int i = 0; i < rows.Count; i++)
        {
            float edge = side < 0 ? -rows[i].leftWidth + .05f : rows[i].rightWidth - .05f - PaintWidth;
            for (int j = 0; j < 2; j++)
            {
                vertices[i * 2 + j] = rows[i].Surface(edge + j * PaintWidth) + Vector3.up * .012f;
                uv[i * 2 + j] = new Vector2(j, rows[i].distance);
            }
            if (i == rows.Count - 1) continue;
            bool unknown = side < 0 ? rows[i].leftInferred || rows[i + 1].leftInferred
                : rows[i].rightInferred || rows[i + 1].rightInferred;
            if (!unknown) Quad(triangles, i * 2, (i + 1) * 2);
        }
        return Finish(vertices, uv, triangles);
    }

    private static void Quad(List<int> triangles, int a, int next)
    {
        triangles.Add(a); triangles.Add(next); triangles.Add(a + 1);
        triangles.Add(a + 1); triangles.Add(next); triangles.Add(next + 1);
    }

    private static Mesh Finish(Vector3[] vertices, Vector2[] uv, List<int> triangles)
    {
        var mesh = new Mesh { indexFormat = IndexFormat.UInt32, vertices = vertices, uv = uv };
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh SaveMesh(Mesh mesh, string path)
    {
        mesh.name = Path.GetFileNameWithoutExtension(path);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        return existing;
    }

    private static Material MaterialAsset(string folder, string name, Color color)
    {
        string path = folder + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        string shaderName = GraphicsSettings.currentRenderPipeline == null ? "Standard"
            : GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline") ? "HDRP/Lit"
            : "Universal Render Pipeline/Lit";
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Shader not found: " + shaderName);
        var material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .18f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .18f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Add(GameObject root, string name, Mesh mesh, Material material, bool collider)
    {
        var child = new GameObject(name); child.transform.SetParent(root.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider) child.AddComponent<MeshCollider>().sharedMesh = mesh;
        child.isStatic = true;
    }
}
