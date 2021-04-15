using System.Linq;
using UnityEngine;

using MediaPipe.BlazeFace;
using MediaPipe.FaceMesh;
using MediaPipe.Iris;

namespace Kao {

sealed class FacePipeline : System.IDisposable
{
    #region Public accessors

    public Matrix4x4 FaceCropMatrix
      => CalculateFaceCropMatrix();

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

    Matrix4x4 CalculateFaceCropMatrix()
    {
        var face = _faceDetector.Detections.FirstOrDefault();

        var scale = face.extent * 1.6f;
        var offset = face.center - scale * 0.5f;

        var angle = Vector2.Angle(Vector2.up, face.nose - face.mouth);
        if (face.nose.x > face.mouth.x) angle = -angle;

        return
          Matrix4x4.Translate(offset) *
          Matrix4x4.Scale(new Vector3(scale.x, scale.y, 1)) *
          Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) *
          Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) *
          Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
    }

    void RunPipeline(Texture input)
    {
        // Face detection
        _faceDetector.ProcessImage(input, 0.5f);

        // Face region cropping
        _cropMaterial.SetMatrix("_Xform", CalculateFaceCropMatrix());
        Graphics.Blit(input, _faceRT, _cropMaterial, 0);

        // Face landmark detection
        _meshBuilder.ProcessImage(_faceRT);
    }

    #endregion
}

} // namespace Kao
