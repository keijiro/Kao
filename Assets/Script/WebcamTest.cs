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
    [SerializeField] Texture _faceTexture = null;
    [SerializeField] Shader _faceShader = null;
    [SerializeField] Shader _wireShader = null;
    [Space]
    [SerializeField] UI.RawImage _mainUI = null;
    [SerializeField] UI.RawImage _cropUI = null;
    [SerializeField] UI.RawImage _previewUI = null;

    #endregion

    #region Private members

    FacePipeline _pipeline;
    Material _faceMaterial;
    Material _wireMaterial;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _pipeline = new FacePipeline(_resources);
        _faceMaterial = new Material(_faceShader);
        _wireMaterial = new Material(_wireShader);

        _faceMaterial.mainTexture = _faceTexture;
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_faceMaterial);
        Destroy(_wireMaterial);
    }

    void LateUpdate()
    {
        _pipeline.ProcessImage(_webcam.Texture);

        // Face mesh (textured surface)
        var faceMatrix = MathUtil.CropMatrix
          (_pipeline.FaceAngle, _pipeline.FaceCropScale,
           _pipeline.FaceCropOffset - math.float2(0.75f, 0.5f));

        _faceMaterial.SetBuffer("_Vertices", _pipeline.FaceMeshBuffer);

        Graphics.DrawMesh
          (_resources.faceMeshTemplate, faceMatrix, _faceMaterial, 0);

        // Face mesh (wire)
        var wireMatrix = MathUtil.ScaleOffset(0.5f, math.float2(0.25f, -0.5f));

        _wireMaterial.SetBuffer("_Vertices", _pipeline.FaceMeshBuffer);

        Graphics.DrawMesh
          (_resources.faceLineTemplate, wireMatrix, _wireMaterial, 0);

        // UI update
        _mainUI.texture = _webcam.Texture;
        _cropUI.texture = _pipeline.CroppedFaceTexture;
        _previewUI.texture = _webcam.Texture;
    }

    #endregion
}

} // namespace Kao
