using UnityEngine;
using Unity.Barracuda;

namespace Kao {

//
// ScriptableObject class used to hold references to internal assets
//
[CreateAssetMenu(fileName = "ResourceSet",
                 menuName = "ScriptableObjects/Kao/Resource Set")]
public sealed class ResourceSet : ScriptableObject
{
    public MediaPipe.BlazeFace.ResourceSet blazeFace;
    public MediaPipe.FaceMesh.ResourceSet faceMesh;
    public MediaPipe.Iris.ResourceSet iris;

    public Shader preprocessShader;
    public ComputeShader postprocessCompute;

    public Mesh faceMeshTemplate;
    public Mesh faceLineTemplate;
}

} // namespace Kao
