using UnityEngine;

public class OceanSurface1 : MonoBehaviour
{
    [Header("Plano océano")]
    public MeshFilter meshFilter;

    [Header("Tamaño")]
    public float oceanSizeX = 100f;
    public float oceanSizeZ = 100f;

    [Header("Estado del mar")]
    public OceanState currentState = OceanState.Normal;

    [System.Serializable]
    public class WaveSettings
    {
        public float amplitude = 1f;
        public float wavelength = 10f;
        public float speed = 1f;
        public Vector2 direction = Vector2.right;
    }

    [System.Serializable]
    public class OceanPreset
    {
        public WaveSettings[] waves;
    }

    public enum OceanState
    {
        Bajo,
        Normal,
        Fuerte
    }

    [Header("Preset Bajo (2 olas)")]
    public OceanPreset lowState;

    [Header("Preset Normal (3 olas)")]
    public OceanPreset normalState;

    [Header("Preset Fuerte (5 olas)")]
    public OceanPreset strongState;

    [Header("Visual")]
    public bool animateMesh = true;

    private Mesh mesh;

    private Vector3[] baseVertices;
    private Vector3[] vertices;

    private OceanPreset activePreset;

    private void Start()
    {
        mesh = meshFilter.mesh;

        baseVertices = mesh.vertices;
        vertices = new Vector3[baseVertices.Length];

        ApplyState(currentState);
    }

    private void LateUpdate()
    {
        if (!animateMesh)
            return;

        AnimateOcean();
    }

    public void ApplyState(OceanState state)
    {
        currentState = state;

        switch (state)
        {
            case OceanState.Bajo:
                activePreset = lowState;
                break;

            case OceanState.Normal:
                activePreset = normalState;
                break;

            case OceanState.Fuerte:
                activePreset = strongState;
                break;
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

        if (activePreset == null || activePreset.waves == null)
            return height;

        for (int i = 0; i < activePreset.waves.Length; i++)
        {
            WaveSettings wave = activePreset.waves[i];

            Vector2 dir = wave.direction.normalized;

            float wavelength = Mathf.Max(0.001f, wave.wavelength);
            float amplitude = wave.amplitude;
            float speed = wave.speed;

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