using UnityEngine;
using Unity.Mathematics;
using UI = UnityEngine.UI;

namespace Kao {

public sealed class Visualizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] WebcamInput _webcam = null;
    [Space]
    [SerializeField] ResourceSet _resources = null;
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
        // Processing on the face pipeline
        _pipeline.ProcessImage(_webcam.Texture);

        // UI update
        _mainUI.texture = _webcam.Texture;
        _faceUI.texture = _pipeline.CroppedFaceTexture;
        _leftEyeUI.texture = _pipeline.CroppedLeftEyeTexture;
        _rightEyeUI.texture = _pipeline.CroppedRightEyeTexture;
    }

    void OnRenderObject()
    {
        // Main view overlay
        // Face mesh
        var mF = float4x4.Translate(math.float3(-0.75f, -0.5f, 0));
        _material.SetBuffer("_Vertices", _pipeline.RefinedFaceVertexBuffer);
        _material.SetPass(1);
        Graphics.DrawMeshNow(_resources.faceLineTemplate, mF);

        // Debug views

        // Face mesh
        var dF = MathUtil.ScaleOffset(0.5f, math.float2(0.25f, 0));
        _material.SetBuffer("_Vertices", _pipeline.RawFaceVertexBuffer);
        _material.SetPass(1);
        Graphics.DrawMeshNow(_resources.faceLineTemplate, dF);

        // Left eye
        var dLE = MathUtil.ScaleOffset(0.25f, math.float2(0.25f, -0.25f));
        _material.SetMatrix("_XForm", dLE);
        _material.SetBuffer("_Vertices", _pipeline.RawLeftEyeVertexBuffer);
        _material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Right eye
        var dRE = MathUtil.ScaleOffset(0.25f, math.float2(0.5f, -0.25f));
        _material.SetMatrix("_XForm", dRE);
        _material.SetBuffer("_Vertices", _pipeline.RawRightEyeVertexBuffer);
        _material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);
    }

    #endregion
}

} // namespace Kao
