using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UI = UnityEngine.UI;

namespace Kao {

public sealed class WebcamTest : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] WebcamInput _webcam = null;
    [SerializeField] ResourceSet _resources = null;
    [Space]
    [SerializeField] Shader _shader = null;
    [Space]
    [SerializeField] UI.RawImage _mainUI = null;
    [SerializeField] UI.RawImage _faceUI = null;
    [SerializeField] UI.RawImage _leftEyeUI = null;
    [SerializeField] UI.RawImage _rightEyeUI = null;

    #endregion

    #region Private members

    FacePipeline _pipeline;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _pipeline = new FacePipeline(_resources);
        _material = new Material(_shader);
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_material);
    }

    void LateUpdate()
    {
        _pipeline.ProcessImage(_webcam.Texture);

        // UI update
        _mainUI.texture = _webcam.Texture;
        _faceUI.texture = _pipeline.CroppedFaceTexture;
        _leftEyeUI.texture = _pipeline.CroppedLeftEyeTexture;
        _rightEyeUI.texture = _pipeline.CroppedRightEyeTexture;
    }

    void OnRenderObject()
    {
        // Main view overlays

        // Face mesh
        var mF = /*MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.FaceCropScale,
           _pipeline.FaceCropOffset - math.float2(0.75f, 0.5f));*/
           float4x4.Translate(math.float3(-0.75f, -0.5f, 0));
        _material.SetBuffer("_Vertices", _pipeline.RefinedFaceVertexBuffer);
        _material.SetPass(0);
        Graphics.DrawMeshNow(_resources.faceMeshTemplate, mF);

        // Right eye
        var mRE = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.EyeCropScale,
           _pipeline.RightEyeCropOffset - math.float2(0.75f, 0.5f));
        _material.SetMatrix("_XForm", mRE);
        _material.SetBuffer("_Vertices", _pipeline.RightEyeVertexBuffer);
        _material.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Left eye
        var mLE = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.EyeCropScale,
           _pipeline.LeftEyeCropOffset - math.float2(0.75f, 0.5f));
        _material.SetMatrix("_XForm", mLE);
        _material.SetBuffer("_Vertices", _pipeline.LeftEyeVertexBuffer);
        _material.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Debug views

        // Face mesh
        var dF = MathUtil.ScaleOffset(0.5f, math.float2(0.25f, 0));
        _material.SetBuffer("_Vertices", _pipeline.FaceVertexBuffer);
        _material.SetPass(1);
        Graphics.DrawMeshNow(_resources.faceLineTemplate, dF);

        // Right eye
        var dRE = MathUtil.ScaleOffset(0.25f, math.float2(0.25f, -0.25f));
        _material.SetMatrix("_XForm", dRE);
        _material.SetBuffer("_Vertices", _pipeline.RightEyeVertexBuffer);
        _material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Left eye
        var dLE = MathUtil.ScaleOffset(0.25f, math.float2(0.5f, -0.25f));
        _material.SetMatrix("_XForm", dLE);
        _material.SetBuffer("_Vertices", _pipeline.LeftEyeVertexBuffer);
        _material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);
    }

    #endregion
}

} // namespace Kao
