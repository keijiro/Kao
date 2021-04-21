using Unity.Mathematics;

namespace Kao {

//
// Eye region calculator class
//
static class EyeRegion
{
    public static float4x4
      CropMatrix(float2 p0, float2 p1, float4x4 rotation, bool flip)
    {
        var box = BoundingBox.CenterExtent
          ((p0 + p1) / 2, math.distance(p0, p1) * 1.2f);

        var mtx = math.mul(box.CropMatrix, rotation);
        if (flip) mtx = math.mul(mtx, MathUtil.HorizontalFlip());

        return mtx;
    }
}

} // namespace Kao
