using Unity.Mathematics;

namespace Kao {

//
// Axis aligned bounding box structure used to track the face region
//
readonly struct BoundingBox
{
    public readonly float2 Min { get; }
    public readonly float2 Max { get; }

    public BoundingBox(float2 min, float2 max)
      => (Min, Max) = (min, max);

    public BoundingBox(in MediaPipe.BlazeFace.FaceDetector.Detection d)
      => (Min, Max) = (d.center - d.extent / 2, d.center + d.extent / 2);

    public float4x4 CropMatrix
      => math.mul(float4x4.Translate(math.float3(Min, 0)),
                  float4x4.Scale(math.float3(Max - Min, 1)));

    public static BoundingBox CenterExtent(float2 center, float2 extent)
      => new BoundingBox(center - extent, center + extent);

    public static BoundingBox operator * (BoundingBox b, float scale)
      => CenterExtent((b.Min + b.Max) / 2, (b.Max - b.Min) * scale / 2);

    public static float CalculateIOU(BoundingBox b1, BoundingBox b2)
    {
        var area0 = (b1.Max.x - b1.Min.x) * (b1.Max.y - b1.Min.y);
        var area1 = (b2.Max.x - b2.Min.x) * (b2.Max.y - b2.Min.y);

        var p0 = math.max(b1.Min, b2.Min);
        var p1 = math.min(b1.Max, b2.Max);
        float areaInner = math.max(0, p1.x - p0.x) * math.max(0, p1.y - p0.y);

        return areaInner / (area0 + area1 - areaInner);
    }
}

} // namespace Kao
