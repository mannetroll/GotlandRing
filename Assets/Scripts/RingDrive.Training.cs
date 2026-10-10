using UnityEngine;

public partial class RingDrive
{
    DrivingSurface drivingSurface;
    GameObject trainingGround;
    Texture2D trainingMap;

    void ToggleTrainingArea()
    {
        MakeTrainingArea();
        drivingSurface.Training = !drivingSurface.Training;
        autopilot = new AutopilotController(drivingSurface.Route, dynamics.awdMode ? awd.PilotSettings(dynamics) : dynamics);
        pilotControls = default;
        RecoverCar(0);
        lap = 1; checkpoints = 0; lapStart = paused ? pauseStarted : Time.time;
        UpdateCameraPose();
        Debug.Log("DRIVING_AREA " + (drivingSurface.Training ? "TRAINING" : "GOTLAND_RING"));
    }

    void RecoverTrainingCar()
    {
        var position = DrivingSurface.TrainingCentre + Vector3.back * DrivingSurface.TrainingRadius;
        yaw = 90; nearest = lastIndex = 0;
        if (dynamics.awdMode) awd.ResetCar(position + Vector3.up * .30f, Quaternion.Euler(0, yaw, 0));
        else PlaceCarOnSurface(position, 0);
        velocity = Vector3.zero; reversing = false; steer = throttle = boost = 0; gear = 1; rpm = 900;
        lookYaw = lookPitch = chaseSlipYaw = 0; ResetMouseSteering();
    }

    void MakeTrainingArea()
    {
        if (trainingGround) return;
        trainingGround = new GameObject("Asphalt slide training area");
        trainingGround.transform.position = DrivingSurface.TrainingCentre;
        var contact = trainingGround.AddComponent<BoxCollider>();
        contact.center = new Vector3(0, -.5f, 0); contact.size = new Vector3(3000, 1, 3000);
        TrainingQuad("Training grass surroundings", Vector3.down * .02f, new Vector2(3000, 3000), grass);
        TrainingQuad("800 metre asphalt skidpad", Vector3.zero, Vector2.one * DrivingSurface.TrainingSize, asphalt);
        var paint = Mat("Skidpad markings", new Color(.8f, .8f, .68f));
        foreach (float radius in new[] {25f, 50, DrivingSurface.TrainingRadius}) TrainingCircle(radius, paint);
        float edge = DrivingSurface.TrainingSize * .5f - 2;
        foreach (float side in new[] {-1f, 1f})
        {
            TrainingQuad("Skidpad edge", new Vector3(side * edge, .015f, 0), new Vector2(.20f, edge * 2), paint);
            TrainingQuad("Skidpad edge", new Vector3(0, .015f, side * edge), new Vector2(edge * 2, .20f), paint);
        }
        TrainingQuad("Practice start line", new Vector3(0, .018f, -100), new Vector2(.4f, 12), white);
        var orange = Mat("Training cone orange", new Color(1, .3f, .035f));
        foreach (float z in new[] {-107f, -93f})
        {
            Box("Start marker base", new Vector3(0, .035f, z), new Vector3(.65f, .07f, .65f), black, trainingGround.transform);
            Box("Start marker", new Vector3(0, .35f, z), new Vector3(.25f, .65f, .25f), orange, trainingGround.transform);
        }
        var pixels = new Color[256 * 256];
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        {
            float px = (x - 128) / 240f * DrivingSurface.TrainingSize, pz = (y - 128) / 240f * DrivingSurface.TrainingSize;
            float r = Mathf.Sqrt(px * px + pz * pz);
            bool line = Mathf.Abs(r - 25) < 2 || Mathf.Abs(r - 50) < 2 || Mathf.Abs(r - 100) < 2;
            bool inside = x >= 8 && x <= 248 && y >= 8 && y <= 248;
            pixels[y * 256 + x] = line ? new Color(.7f, .75f, .7f) : inside ? new Color(.12f, .17f, .20f) : new Color(.035f, .065f, .08f);
        }
        trainingMap = new Texture2D(256, 256, TextureFormat.RGBA32, false) { name = "Training skidpad map" };
        trainingMap.SetPixels(pixels); trainingMap.Apply();
    }

    void TrainingQuad(string name, Vector3 centre, Vector2 size, Material material)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = new[] {centre + new Vector3(-size.x, 0, -size.y) * .5f, centre + new Vector3(-size.x, 0, size.y) * .5f,
            centre + new Vector3(size.x, 0, size.y) * .5f, centre + new Vector3(size.x, 0, -size.y) * .5f};
        mesh.uv = new[] {Vector2.zero, new Vector2(0, size.y / 4), size / 4, new Vector2(size.x / 4, 0)};
        mesh.triangles = new[] {0, 1, 2, 0, 2, 3}; mesh.RecalculateNormals();
        var obj = new GameObject(name); obj.transform.SetParent(trainingGround.transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    void TrainingCircle(float radius, Material material)
    {
        const int count = 256;
        var vertices = new Vector3[(count + 1) * 2]; var triangles = new int[count * 6];
        for (int i = 0; i <= count; i++)
        {
            float angle = i * 2 * Mathf.PI / count;
            for (int side = 0; side < 2; side++) vertices[i * 2 + side] =
                new Vector3(Mathf.Sin(angle) * (radius + side * .18f), .016f, Mathf.Cos(angle) * (radius + side * .18f));
            if (i == count) continue;
            int n = i * 2, t = i * 6;
            triangles[t] = n; triangles[t + 1] = n + 1; triangles[t + 2] = n + 2;
            triangles[t + 3] = n + 1; triangles[t + 4] = n + 3; triangles[t + 5] = n + 2;
        }
        var mesh = new Mesh { name = $"{radius} metre practice circle", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        var obj = new GameObject(mesh.name); obj.transform.SetParent(trainingGround.transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    void DrawTrainingMap()
    {
        GUI.DrawTexture(new Rect(1340, 25, 235, 235), trainingMap);
        GUI.Label(new Rect(1352, 30, 215, 22), "SLIDE TRAINING / FLAT ASPHALT", small);
        var offset = car.position - DrivingSurface.TrainingCentre;
        float x = 1457.5f + Mathf.Clamp(offset.x / DrivingSurface.TrainingSize, -.5f, .5f) * 220;
        float y = 142.5f - Mathf.Clamp(offset.z / DrivingSurface.TrainingSize, -.5f, .5f) * 220;
        GUI.color = Color.white; GUI.DrawTexture(new Rect(x - 3, y - 3, 6, 6), Texture2D.whiteTexture);
    }
}
