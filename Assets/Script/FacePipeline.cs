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
      => _faceCrop;

    public ComputeBuffer FaceVertexBuffer
      => _faceMesh.VertexBuffer;

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

    public Texture CroppedLeftEyeTexture
      => _irisCropL;

    public Texture CroppedRightEyeTexture
      => _irisCropR;

    public ComputeBuffer LeftEyeVertexBuffer
      => _irisMeshL.VertexBuffer;

    public ComputeBuffer RightEyeVertexBuffer
      => _irisMeshR.VertexBuffer;

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
    FaceLandmarkDetector _faceMesh;
    EyeLandmarkDetector _irisMeshL;
    EyeLandmarkDetector _irisMeshR;

    Material _cropMaterial;

    RenderTexture _faceCrop;
    RenderTexture _irisCropL;
    RenderTexture _irisCropR;

    #endregion

    #region Object allocation/deallocation

    void AllocateObjects(ResourceSet resources)
    {
        _resources = resources;

        _faceDetector = new FaceDetector(_resources.blazeFace);
        _faceMesh = new FaceLandmarkDetector(_resources.faceMesh);
        _irisMeshL = new EyeLandmarkDetector(_resources.iris);
        _irisMeshR = new EyeLandmarkDetector(_resources.iris);

        _cropMaterial = new Material(_resources.cropShader);

        _faceCrop = new RenderTexture(192, 192, 0);
        _irisCropL = new RenderTexture(64, 64, 0);
        _irisCropR = new RenderTexture(64, 64, 0);
    }

    void DeallocateObjects()
    {
        _faceDetector.Dispose();
        _faceMesh.Dispose();
        _irisMeshL.Dispose();
        _irisMeshR.Dispose();

        Object.Destroy(_cropMaterial);

        Object.Destroy(_faceCrop);
        Object.Destroy(_irisCropL);
        Object.Destroy(_irisCropR);
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
        Graphics.Blit(input, _faceCrop, _cropMaterial, 0);

        // Face landmark detection
        _faceMesh.ProcessImage(_faceCrop);

        // Eye region cropping (left)
        _cropMaterial.SetMatrix("_Xform", LeftEyeCropMatrix);
        Graphics.Blit(input, _irisCropL, _cropMaterial, 0);

        // Iris landmark detection (left)
        _irisMeshL.ProcessImage(_irisCropL);

        // Eye region cropping (right)
        _cropMaterial.SetMatrix("_Xform", RightEyeCropMatrix);
        Graphics.Blit(input, _irisCropR, _cropMaterial, 0);

        // Iris landmark detection (right)
        _irisMeshR.ProcessImage(_irisCropR);
    }

    #endregion
}

} // namespace Kao
