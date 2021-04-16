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
    [SerializeField] Shader _surfaceShader = null;
    [SerializeField] Shader _wireShader = null;
    [Space]
    [SerializeField] UI.RawImage _mainUI = null;
    [SerializeField] UI.RawImage _faceUI = null;
    [SerializeField] UI.RawImage _leftEyeUI = null;
    [SerializeField] UI.RawImage _rightEyeUI = null;

    #endregion

    #region Private members

    FacePipeline _pipeline;
    Material _surfaceMaterial;
    Material _wireMaterial;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _pipeline = new FacePipeline(_resources);
        _surfaceMaterial = new Material(_surfaceShader);
        _wireMaterial = new Material(_wireShader);
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_surfaceMaterial);
        Destroy(_wireMaterial);
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
        // Face mesh (main)
        var faceMatrix = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.FaceCropScale,
           _pipeline.FaceCropOffset - math.float2(0.75f, 0.5f));
        _surfaceMaterial.SetBuffer("_Vertices", _pipeline.FaceVertexBuffer);
        _surfaceMaterial.SetPass(0);
        Graphics.DrawMeshNow(_resources.faceMeshTemplate, faceMatrix);

        // Left eye (main)
        var leftEyeMatrix = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.EyeCropScale,
           _pipeline.LeftEyeCropOffset - math.float2(0.75f, 0.5f));

        _surfaceMaterial.SetMatrix("_EyeXForm", leftEyeMatrix);
        _surfaceMaterial.SetBuffer("_Vertices", _pipeline.LeftEyeVertexBuffer);
        _surfaceMaterial.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Right eye (main)
        var rightEyeMatrix = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.EyeCropScale,
           _pipeline.RightEyeCropOffset - math.float2(0.75f, 0.5f));

        _surfaceMaterial.SetMatrix("_EyeXForm", rightEyeMatrix);
        _surfaceMaterial.SetBuffer("_Vertices", _pipeline.RightEyeVertexBuffer);
        _surfaceMaterial.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 64, 1);

        // Wireframe face mesh
        // Over
        var wireFaceMtx = MathUtil.ScaleOffset(0.5f, math.float2(0.25f, 0));
        _wireMaterial.SetBuffer("_Vertices", _pipeline.FaceVertexBuffer);
        _wireMaterial.SetPass(0);
        Graphics.DrawMeshNow(_resources.faceLineTemplate, wireFaceMtx);

        // Wireframe 
    }

    #endregion
}

} // namespace Kao
