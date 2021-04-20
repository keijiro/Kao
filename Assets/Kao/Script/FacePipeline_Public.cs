using UnityEngine;

namespace Kao {

//
// Public part of the face pipeline class
//

partial class FacePipeline
{
    #region Accessors for vertex buffers

    public ComputeBuffer RawFaceVertexBuffer
      => _landmarkDetector.face.VertexBuffer;

    public ComputeBuffer RawLeftEyeVertexBuffer
      => _landmarkDetector.eyeL.VertexBuffer;

    public ComputeBuffer RawRightEyeVertexBuffer
      => _landmarkDetector.eyeR.VertexBuffer;

    public ComputeBuffer RefinedFaceVertexBuffer
      => _computeBuffer.post;

    #endregion

    #region Accessors for cropped textures

    public Texture CroppedFaceTexture
      => _cropRT.face;

    public Texture CroppedLeftEyeTexture
      => _cropRT.eyeL;

    public Texture CroppedRightEyeTexture
      => _cropRT.eyeR;

    #endregion

    #region Public methods

    public FacePipeline(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunPipeline(image);

    #endregion
}

} // namespace Kao
