using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace Kao {

//
// Image processing part of the face pipeline class
//

partial class FacePipeline
{
    // Face region (also used to refer the previous frame region)
    BoundingBox _faceRegion;

    // Vertex retrieval from the face landmark detector
    float4 GetFaceVertex(int index)
      => _landmarkDetector.face.VertexArray.ElementAt(index);

    void RunPipeline(Texture input)
    {
        // Face detection
        _faceDetector.ProcessImage(input);

        // Cancel if the face detection score is too low.
        var face = _faceDetector.Detections.FirstOrDefault();
        if (face.score < 0.5f) return;

        // Face region from the detection
        var fromDetection = new BoundingBox(face).Squarified * 1.75f;

        // We prefer using the face region based on the previous face landmark
        // detection, but we have to use the one from the detection if the IOU
        // is too low.
        if (BoundingBox.CalculateIOU(_faceRegion, fromDetection) < 0.5f)
            _faceRegion = fromDetection;

        // Face angle
        var face_angle = MathUtil.Angle(face.nose - face.mouth) - math.PI / 2;

        // Face region matrix
        var face_mtx = math.mul(_faceRegion.CropMatrix,
                                MathUtil.ZRotateAtCenter(face_angle));

        // Face region cropping
        _preprocess.SetMatrix("_Xform", face_mtx);
        Graphics.Blit(input, _cropRT.face, _preprocess, 0);

        // Face landmark detection
        _landmarkDetector.face.ProcessImage(_cropRT.face);

        // Key points from the face landmark
        var nose_tip = math.mul(face_mtx, GetFaceVertex(1)).xy;
        var mid_eyes = math.mul(face_mtx, GetFaceVertex(168)).xy;
        var eye_l0 = math.mul(face_mtx, GetFaceVertex(33)).xy;
        var eye_l1 = math.mul(face_mtx, GetFaceVertex(133)).xy;
        var eye_r0 = math.mul(face_mtx, GetFaceVertex(362)).xy;
        var eye_r1 = math.mul(face_mtx, GetFaceVertex(263)).xy;

        // Eye regions
        var eye_l_box = BoundingBox.CenterExtent
          ((eye_l0 + eye_l1) / 2, math.distance(eye_l0, eye_l1) * 1.2f);

        var eye_r_box = BoundingBox.CenterExtent
          ((eye_r0 + eye_r1) / 2, math.distance(eye_r0, eye_r1) * 1.2f);

        var eye_l_mtx = math.mul(eye_l_box.CropMatrix,
                                 MathUtil.ZRotateAtCenter(face_angle));

        var eye_r_mtx = MathUtil.Mul(eye_r_box.CropMatrix,
                                     MathUtil.ZRotateAtCenter(face_angle),
                                     MathUtil.HorizontalFlip());

        // Eye region cropping
        _preprocess.SetMatrix("_Xform", eye_l_mtx);
        Graphics.Blit(input, _cropRT.eyeL, _preprocess, 0);

        _preprocess.SetMatrix("_Xform", eye_r_mtx);
        Graphics.Blit(input, _cropRT.eyeR, _preprocess, 0);

        // Eye landmark detection
        _landmarkDetector.eyeL.ProcessImage(_cropRT.eyeL);
        _landmarkDetector.eyeR.ProcessImage(_cropRT.eyeR);

        // Postprocess for face mesh construction
        var post = _resources.postprocessCompute;

        post.SetMatrix("_fx_xform", face_mtx);
        post.SetBuffer(0, "_fx_input", _landmarkDetector.face.VertexBuffer);
        post.SetBuffer(0, "_fx_output", _computeBuffer.post);
        post.SetBuffer(0, "_fx_bbox", _computeBuffer.bbox);
        post.Dispatch(0, 1, 1, 1);

        post.SetBuffer(1, "_e2f_index_table", _computeBuffer.eyeToFace);
        post.SetBuffer(1, "_e2f_eye_l", _landmarkDetector.eyeL.VertexBuffer);
        post.SetBuffer(1, "_e2f_eye_r", _landmarkDetector.eyeR.VertexBuffer);
        post.SetMatrix("_e2f_xform_l", eye_l_mtx);
        post.SetMatrix("_e2f_xform_r", eye_r_mtx);
        post.SetBuffer(1, "_e2f_face", _computeBuffer.post);
        post.Dispatch(1, 1, 1, 1);

        // Face region update
        _faceRegion = _computeBuffer.bbox.GetBoundingBoxData().Squarified * 1.5f;
    }
}

} // namespace Kao
