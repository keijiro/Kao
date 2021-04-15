using System.Linq;
using UnityEngine;
using Unity.Mathematics;

using MediaPipe.BlazeFace;
using MediaPipe.FaceMesh;
using MediaPipe.Iris;

namespace Kao {

sealed class FacePipeline : System.IDisposable
{
    #region Public accessors

    public float FaceAngle
      => MathUtil.Angle((float2)Face.nose - (float2)Face.mouth);

    public float2 FaceCropScale
      => (float2)Face.extent * 1.6f;

    public float2 FaceCropOffset
      => (float2)Face.center - FaceCropScale / 2;

    public float4x4 FaceCropMatrix
      => MathUtil.CropMatrix(FaceAngle, FaceCropScale, FaceCropOffset);

    public Texture CroppedFaceTexture
      => _faceRT;

    public ComputeBuffer FaceMeshBuffer
      => _meshBuilder.VertexBuffer;

    #endregion

    #region Public methods

    public FacePipeline(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunPipeline(image);

    #endregion

    #region Internal objects

    ResourceSet _resources;

    FaceDetector _faceDetector;
    IrisDetector _irisDetector;
    FaceMeshBuilder _meshBuilder;

    Material _cropMaterial;

    RenderTexture _faceRT;
    RenderTexture _irisRT;

    #endregion

    #region Object allocation/deallocation

    void AllocateObjects(ResourceSet resources)
    {
        _resources = resources;

        _faceDetector = new FaceDetector(_resources.blazeFace);
        _irisDetector = new IrisDetector(_resources.iris);
        _meshBuilder = new FaceMeshBuilder(_resources.faceMesh);

        _cropMaterial = new Material(_resources.cropShader);

        _faceRT = new RenderTexture(192, 192, 0);
        _irisRT = new RenderTexture(64, 64, 0);
    }

    void DeallocateObjects()
    {
        _faceDetector.Dispose();
        _irisDetector.Dispose();
        _meshBuilder.Dispose();

        Object.Destroy(_cropMaterial);

        Object.Destroy(_faceRT);
        Object.Destroy(_irisRT);
    }

    #endregion

    #region Private methods

    Detection Face
      => _faceDetector.Detections.FirstOrDefault();

    void RunPipeline(Texture input)
    {
        // Face detection
        _faceDetector.ProcessImage(input, 0.5f);

        // Face region cropping
        _cropMaterial.SetMatrix("_Xform", FaceCropMatrix);
        Graphics.Blit(input, _faceRT, _cropMaterial, 0);

        // Face landmark detection
        _meshBuilder.ProcessImage(_faceRT);
    }

    #endregion
}

} // namespace Kao
