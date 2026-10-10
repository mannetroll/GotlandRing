using System.Globalization;
using System.Text;
using UnityEngine;

// The driving route guides the pilot; the surface decides where the tyres find asphalt.
public sealed class DrivingSurface
{
    public static readonly Vector3 TrainingCentre = new Vector3(6000, 0, 0);
    public const float TrainingSize = 800;
    public const float TrainingRadius = 100;
    readonly TrackData circuit;
    readonly QuarryTerrain quarry;
    TrackData trainingRoute;
    public bool Training;
    public DrivingSurface(TrackData circuit, QuarryTerrain quarry) { this.circuit = circuit; this.quarry = quarry; }
    public TrackData Route => Training ? trainingRoute ??= MakeTrainingRoute() : circuit;

    public TrackData.SurfaceSample Sample(float x, float z)
    {
        if (!Training) { var surface = circuit.Sample(x, z); quarry.Sample(x, z, ref surface); return surface; }
        var result = Route.Sample(x, z);
        float offset = Mathf.Max(Mathf.Abs(x - TrainingCentre.x), Mathf.Abs(z - TrainingCentre.z));
        result.Height = TrainingCentre.y; result.Normal = Vector3.up;
        result.OnRoad = offset <= TrainingSize * .5f;
        result.Width = TrainingSize * .5f; result.Offset = offset;
        return result;
    }

    static TrackData MakeTrainingRoute()
    {
        var csv = new StringBuilder("x_m,y_m,z_m,distance_m,forward_x,forward_z,width_left_m,width_right_m,cross_slope_center_percent,cross_curve_per_m,ground_apron_left_dy_m,ground_apron_right_dy_m,width_left_inferred,width_right_inferred\n");
        const int count = 128;
        for (int i = 0; i <= count; i++)
        {
            float angle = (i % count) * 2 * Mathf.PI / count;
            csv.AppendFormat(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R},{3:R},{4:R},{5:R},3,3,0,0,0,0,0,0\n",
                TrainingCentre.x + Mathf.Sin(angle) * TrainingRadius, TrainingCentre.y,
                TrainingCentre.z - Mathf.Cos(angle) * TrainingRadius, i * 2 * Mathf.PI * TrainingRadius / count,
                Mathf.Cos(angle), Mathf.Sin(angle));
        }
        return TrackData.Load(csv.ToString());
    }
}
