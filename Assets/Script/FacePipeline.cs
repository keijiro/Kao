using System.Linq;
using UnityEngine;
using Unity.Mathematics;

using MediaPipe.BlazeFace;
using MediaPipe.FaceMesh;
using MediaPipe.Iris;

//
// Heavily under construction. Cluttered like hell. Don't try to read.
//

namespace Kao {

sealed class FacePipeline : System.IDisposable
{
    #region Public face accessors

    public float FaceAngle
      => _usePrev ? _prevFaceAngle : MathUtil.Angle((float2)Face.nose - (float2)Face.mouth) - math.PI / 2;

    public float FaceCropScale
      => _usePrev ? _prevFaceScale : math.length(Face.extent) * 1.2f;

    public float2 FaceCropOffset
      => (float2)Face.center - FaceCropScale / 2;

    public float4x4 FaceCropMatrix
      => MathUtil.CropMatrix(FaceAngle, FaceCropScale, FaceCropOffset);

    public Texture CroppedFaceTexture
      => _faceCrop;

    public ComputeBuffer FaceVertexBuffer
      => _faceMesh.VertexBuffer;

    public ComputeBuffer RefinedFaceVertexBuffer
      => _refineBuffer;

    #endregion

    public float2 NoseTip
      => math.mul(FaceCropMatrix, math.float4(_faceMesh.VertexArray.ElementAt(1))).xy;

    public float2 MidwayBetweenEyes
      => math.mul(FaceCropMatrix, math.float4(_faceMesh.VertexArray.ElementAt(168))).xy;

    public float2 GetLeftEyeBoundaryPoint(int i)
      => math.mul(FaceCropMatrix, math.float4(_faceMesh.VertexArray.ElementAt(i == 0 ? 33 : 133))).xy;

    public float2 GetRightEyeBoundaryPoint(int i)
      => math.mul(FaceCropMatrix, math.float4(_faceMesh.VertexArray.ElementAt(i == 0 ? 362 : 263))).xy;

    #region Public left eye accessors

    public float LeftEyeAngle
      => FaceAngle;
      //=> MathUtil.Angle(GetLeftEyeBoundaryPoint(1) -
                        //GetLeftEyeBoundaryPoint(0));

    public float LeftEyeCropScale
      => math.distance(GetLeftEyeBoundaryPoint(0),
                       GetLeftEyeBoundaryPoint(1)) * 2.5f;

    public float2 LeftEyeCropOffset
      => (GetLeftEyeBoundaryPoint(0) + GetLeftEyeBoundaryPoint(1)) / 2 - LeftEyeCropScale / 2;

    #endregion

    #region Public right eye accessors

    public float RightEyeAngle
      => -FaceAngle;
      //=> MathUtil.Angle(GetRightEyeBoundaryPoint(1) -
       //                 GetRightEyeBoundaryPoint(0));

    public float RightEyeCropScale
      => math.distance(GetRightEyeBoundaryPoint(0),
                       GetRightEyeBoundaryPoint(1)) * 2.5f;

    public float2 RightEyeCropOffset
      => (GetRightEyeBoundaryPoint(0) + GetRightEyeBoundaryPoint(1)) / 2 - RightEyeCropScale / 2;

    #endregion

    #region Public eye detection accessors

    public float4x4 LeftEyeCropMatrix
      => MathUtil.CropMatrix(LeftEyeAngle, LeftEyeCropScale, LeftEyeCropOffset);

    public float4x4 RightEyeCropMatrix
      => math.mul(
                  MathUtil.CropMatrix(-RightEyeAngle, RightEyeCropScale, RightEyeCropOffset),
math.mul(float4x4.Translate(math.float3(1, 0, 0)), float4x4.Scale(math.float3(-1, 1, 1)))
                  );

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

    ComputeBuffer _refineBuffer;
    ComputeBuffer _eyeToFace;

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

        _refineBuffer = new ComputeBuffer(FaceLandmarkDetector.VertexCount,
                                          sizeof(float) * 4);
        _eyeToFace = IndexTable.CreateEyeToFaceLandmarkBuffer();
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

        _refineBuffer.Dispose();
        _eyeToFace.Dispose();
    }

    #endregion

    #region Private methods

    FaceDetector.Detection Face
      => _faceDetector.Detections.FirstOrDefault();

    bool _usePrev;
    float _prevFaceAngle;
    float _prevFaceScale;

    void RunPipeline(Texture input)
    {
        if (Time.frameCount < 10) _usePrev = false;

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

        // Face landmark refinement
        var refine = _resources.refinementCompute;

        refine.SetBuffer(0, "_FaceVertices", _faceMesh.VertexBuffer);
        refine.SetBuffer(0, "_RefineBuffer", _refineBuffer);
        refine.SetMatrix("_FaceXForm", FaceCropMatrix);
        refine.Dispatch(0, FaceLandmarkDetector.VertexCount / 52, 1, 1);

        refine.SetBuffer(1, "_EyeToFaceTable", _eyeToFace);
        refine.SetBuffer(1, "_EyeVerticesL", _irisMeshL.VertexBuffer);
        refine.SetBuffer(1, "_EyeVerticesR", _irisMeshR.VertexBuffer);
        refine.SetMatrix("_EyeXFormL", LeftEyeCropMatrix);
        refine.SetMatrix("_EyeXFormR", RightEyeCropMatrix);
        refine.SetBuffer(1, "_RefineBuffer", _refineBuffer);
        refine.Dispatch(1, 1, 1, 1);

        var angle = MathUtil.Angle(MidwayBetweenEyes - NoseTip) - math.PI / 2;
        var scale = 1 / FaceCropScale * math.distance(MidwayBetweenEyes.xy, NoseTip.xy) * 3.0f;
        if (_usePrev)
        {
            _prevFaceAngle = math.lerp(_prevFaceAngle, angle, 0.2f);
            _prevFaceScale = math.lerp(_prevFaceScale, scale, 0.2f);
        }
        else
        {
            _prevFaceAngle = angle;
            _prevFaceScale = scale;
            _usePrev = true;
        }




        /*

        refine.SetMatrix("_EyeXFormL", 
          math.mul(math.mul(float4x4.Translate(math.float3(-FaceCropOffset, 0)),
                            float4x4.Scale(1 / FaceCropScale)),
                   math.mul(float4x4.Translate(math.float3(LeftEyeCropOffset, 0)),
                            float4x4.Scale(EyeCropScale))));

          //MathUtil.CropMatrix(0, EyeCropScale / FaceCropScale,
                              //LeftEyeCropOffset - FaceCropOffset));

        //refine.SetMatrix("_EyeXFormR", 
          //MathUtil.CropMatrix(0, EyeCropScale / FaceCropScale,
           //                   RightEyeCropOffset - FaceCropOffset));
        */
    }

    #endregion
}

} // namespace Kao
