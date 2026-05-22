using UnityEngine;

public class OceanSurface : MonoBehaviour
{
    [Header("Plano océano")]
    public MeshFilter meshFilter;

    [Header("Tamaño")]
    public float oceanSizeX = 100f;
    public float oceanSizeZ = 100f;

    [Header("Ondas Gerstner")]
    [Range(1, 16)]
    public int waveCount = 6;

    public float[] amplitudes;
    public float[] wavelengths;
    public float[] speeds;
    public Vector2[] directions;

    [Header("Visual")]
    public bool animateMesh = true;

    [Header("Suavizado")]
    public bool randomizeOnStart = true;

    private Mesh mesh;

    private Vector3[] baseVertices;
    private Vector3[] vertices;

    private void Start()
    {
        mesh = meshFilter.mesh;

        baseVertices = mesh.vertices;
        vertices = new Vector3[baseVertices.Length];

        InitializeArrays();

        if (randomizeOnStart)
            RandomizeWaves();
    }

    private void LateUpdate()
    {
        if (!animateMesh)
            return;

        AnimateOcean();
    }

    void InitializeArrays()
    {
        if (amplitudes == null || amplitudes.Length != waveCount)
            amplitudes = new float[waveCount];

        if (wavelengths == null || wavelengths.Length != waveCount)
            wavelengths = new float[waveCount];

        if (speeds == null || speeds.Length != waveCount)
            speeds = new float[waveCount];

        if (directions == null || directions.Length != waveCount)
            directions = new Vector2[waveCount];
    }

    void RandomizeWaves()
    {
        for (int i = 0; i < waveCount; i++)
        {
            amplitudes[i] = Random.Range(0.2f, 1.2f);
            wavelengths[i] = Random.Range(6f, 30f);
            speeds[i] = Random.Range(0.5f, 3f);
            directions[i] = Random.insideUnitCircle.normalized;
        }
    }

    void AnimateOcean()
    {
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            Vector3 worldPos = transform.TransformPoint(vertex);

            float height = GetWaveHeight(worldPos);

            Vector3 newWorldPos = new Vector3(worldPos.x, height, worldPos.z);
            vertex.y = transform.InverseTransformPoint(newWorldPos).y;

            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public float GetWaveHeight(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);

        // Límite del océano
        if (Mathf.Abs(localPos.x) > oceanSizeX * 0.5f ||
            Mathf.Abs(localPos.z) > oceanSizeZ * 0.5f)
        {
            return transform.position.y;
        }

        float height = transform.position.y;
        float time = Time.time;

        for (int i = 0; i < waveCount; i++)
        {
            Vector2 dir = directions[i].normalized;
            float wavelength = Mathf.Max(0.001f, wavelengths[i]);
            float amplitude = amplitudes[i];
            float speed = speeds[i];

            float k = 2f * Mathf.PI / wavelength;
            float c = Mathf.Sqrt(9.8f / k) * speed;

            float f =
                k *
                (
                    Vector2.Dot(
                        dir,
                        new Vector2(localPos.x, localPos.z)
                    )
                    - c * time
                );

            height += amplitude * Mathf.Sin(f);
        }

        return height;
    }

    public Vector3 GetWaveNormal(Vector3 worldPosition)
    {
        float offset = 0.25f;

        float hL = GetWaveHeight(worldPosition - Vector3.right * offset);
        float hR = GetWaveHeight(worldPosition + Vector3.right * offset);
        float hD = GetWaveHeight(worldPosition - Vector3.forward * offset);
        float hU = GetWaveHeight(worldPosition + Vector3.forward * offset);

        Vector3 normal = new Vector3(hL - hR, 2f, hD - hU);
        return normal.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(oceanSizeX, 0.1f, oceanSizeZ)
        );
    }
}