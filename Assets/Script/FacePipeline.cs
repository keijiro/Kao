using System.Linq;
using UnityEngine;
using Unity.Mathematics;

using MediaPipe.BlazeFace;
using MediaPipe.FaceMesh;
using MediaPipe.Iris;

namespace Kao {

sealed class FacePipeline : System.IDisposable
{
    #region Public face detection accessors

    public float FaceAngle
      => MathUtil.Angle((float2)Face.nose - (float2)Face.mouth);

    public float FaceCropScale
      => math.length(Face.extent) * 1.2f;

    public float2 FaceCropOffset
      => (float2)Face.center - FaceCropScale / 2;

    public float4x4 FaceCropMatrix
      => MathUtil.CropMatrix(FaceAngle, FaceCropScale, FaceCropOffset);

    public Texture CroppedFaceTexture
      => _faceRT;

    public ComputeBuffer FaceVertexBuffer
      => _meshBuilder.VertexBuffer;

    #endregion

    #region Public eye detection accessors

    public float EyeCropScale
      => math.distance(Face.leftEye, Face.rightEye) * 0.9f;

    public float2 LeftEyeCropOffset
      => (float2)Face.leftEye - EyeCropScale / 2;

    public float2 RightEyeCropOffset
      => (float2)Face.rightEye - EyeCropScale / 2;

    public float4x4 LeftEyeCropMatrix
      => MathUtil.CropMatrix(FaceAngle, EyeCropScale, LeftEyeCropOffset);

    public float4x4 RightEyeCropMatrix
      => MathUtil.CropMatrix(FaceAngle, EyeCropScale, RightEyeCropOffset);

    public ComputeBuffer LeftEyeVertexBuffer
      => _irisDetectorL.VertexBuffer;

    public ComputeBuffer RightEyeVertexBuffer
      => _irisDetectorR.VertexBuffer;

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
    FaceMeshBuilder _meshBuilder;
    IrisDetector _irisDetectorL;
    IrisDetector _irisDetectorR;

    Material _cropMaterial;

    RenderTexture _faceRT;
    RenderTexture _irisRT;

    #endregion

    #region Object allocation/deallocation

    void AllocateObjects(ResourceSet resources)
    {
        _resources = resources;

        _faceDetector = new FaceDetector(_resources.blazeFace);
        _meshBuilder = new FaceMeshBuilder(_resources.faceMesh);
        _irisDetectorL = new IrisDetector(_resources.iris);
        _irisDetectorR = new IrisDetector(_resources.iris);

        _cropMaterial = new Material(_resources.cropShader);

        _faceRT = new RenderTexture(192, 192, 0);
        _irisRT = new RenderTexture(64, 64, 0);
    }

    void DeallocateObjects()
    {
        _faceDetector.Dispose();
        _meshBuilder.Dispose();
        _irisDetectorL.Dispose();
        _irisDetectorR.Dispose();

        Object.Destroy(_cropMaterial);

        Object.Destroy(_faceRT);
        Object.Destroy(_irisRT);
    }

    #endregion

    #region Private methods

    FaceDetector.Detection Face
      => _faceDetector.Detections.FirstOrDefault();

    void RunPipeline(Texture input)
    {
        // Face detection
        _faceDetector.ProcessImage(input);

        // Face region cropping
        _cropMaterial.SetMatrix("_Xform", FaceCropMatrix);
        Graphics.Blit(input, _faceRT, _cropMaterial, 0);

        // Face landmark detection
        _meshBuilder.ProcessImage(_faceRT);

        // Eye region cropping (left)
        _cropMaterial.SetMatrix("_Xform", LeftEyeCropMatrix);
        Graphics.Blit(input, _irisRT, _cropMaterial, 0);

        // Iris landmark detection (left)
        _irisDetectorL.ProcessImage(_irisRT);

        // Eye region cropping (right)
        _cropMaterial.SetMatrix("_Xform", RightEyeCropMatrix);
        Graphics.Blit(input, _irisRT, _cropMaterial, 0);

        // Iris landmark detection (right)
        _irisDetectorR.ProcessImage(_irisRT);
    }

    #endregion
}

} // namespace Kao
