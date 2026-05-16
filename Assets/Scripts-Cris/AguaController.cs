using UnityEngine;

/// <summary>
/// Anima el material del agua con un scroll UV y
/// genera pequenas olas desplazando vertices (requiere mesh modificable).
/// Asigna este script al plano de agua.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AguaController : MonoBehaviour
{
    [Header("Scroll del agua")]
    public Vector2 scrollSpeed = new Vector2(0.02f, 0.01f);

    [Header("Olas (desplazamiento de vertices)")]
    public float waveAmplitude  = 0.05f;
    public float waveFrequency  = 1.2f;
    public float waveSpeed      = 1.0f;

    private Material  _mat;
    private Mesh      _mesh;
    private Vector3[] _baseVertices;

    void Start()
    {
        _mat = GetComponent<Renderer>().material;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            _mesh = mf.mesh;
            _baseVertices = _mesh.vertices;
        }
    }

    void Update()
    {
        ScrollUV();
        AnimateWaves();
    }

    private void ScrollUV()
    {
        Vector2 offset = _mat.mainTextureOffset;
        offset += scrollSpeed * Time.deltaTime;
        _mat.mainTextureOffset = offset;
    }

    private void AnimateWaves()
    {
        if (_mesh == null || _baseVertices == null) return;

        Vector3[] verts = _mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 b = _baseVertices[i];
            verts[i].y = b.y + Mathf.Sin(
                (b.x + b.z) * waveFrequency + Time.time * waveSpeed
            ) * waveAmplitude;
        }
        _mesh.vertices = verts;
        _mesh.RecalculateNormals();
    }
}
