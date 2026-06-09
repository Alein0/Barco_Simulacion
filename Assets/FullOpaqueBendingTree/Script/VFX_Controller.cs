using UnityEngine;

namespace MyGame.VFX
{
    [ExecuteAlways]
    public class VFXController : MonoBehaviour
    {
        [Header("Modifiable Parameters")]
        [SerializeField] private Color particleColor = Color.white;
        [SerializeField, Range(0f, 4f)] private float intensity = 1f;

        [Header("Wind Source")]
        [SerializeField] private Wind_Modificado windSource;
        [SerializeField] private Vector3 windDirection = Vector3.left;

        private ParticleSystem[] particleSystems;
        private float[] defaultRateOverTimeValues;
        private bool initialized;

        private void OnEnable()
        {
            Initialize();
            SubscribeToWind();
            ApplySettings();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Wind_Modificado.OnWindDirectionChanged -= OnWindDirectionChanged;
        }

        private void OnDestroy()
        {
            Wind_Modificado.OnWindDirectionChanged -= OnWindDirectionChanged;
        }

        private void OnValidate()
        {
            if (!initialized)
                Initialize();

            ApplySettings();
        }

        private void Initialize()
        {
            initialized = true;

            if (windSource == null)
                windSource = FindFirstObjectByType<Wind_Modificado>();

            FindParticles();

            if (windSource != null)
                windDirection = windSource.CurrentWindDirection;
        }

        private void SubscribeToWind()
        {
            Wind_Modificado.OnWindDirectionChanged -= OnWindDirectionChanged;
            Wind_Modificado.OnWindDirectionChanged += OnWindDirectionChanged;
        }

        private void FindParticles()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            if (particleSystems == null || particleSystems.Length == 0)
                return;

            defaultRateOverTimeValues = new float[particleSystems.Length];

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var emission = particleSystems[i].emission;
                defaultRateOverTimeValues[i] = emission.rateOverTime.constant;
            }
        }

        private void ApplySettings()
        {
            if (particleSystems == null || particleSystems.Length == 0 || defaultRateOverTimeValues == null)
                FindParticles();

            if (particleSystems == null || defaultRateOverTimeValues == null)
                return;

            Vector3 dir = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.left;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                if (ps == null) continue;

                var main = ps.main;
                var emission = ps.emission;
                var velocityOverLifetime = ps.velocityOverLifetime;

                main.startColor = particleColor;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                float baseRate = defaultRateOverTimeValues[i];
                if (emission.rateOverTime.mode == ParticleSystemCurveMode.Constant)
                {
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * intensity);
                }
                else
                {
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate * intensity, baseRate * intensity);
                }

                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.xMultiplier = dir.x * intensity;
                velocityOverLifetime.yMultiplier = dir.y * intensity;
                velocityOverLifetime.zMultiplier = dir.z * intensity;
            }
        }

        private void OnWindDirectionChanged(Vector3 newWindDirection)
        {
            windDirection = newWindDirection;
            ApplySettings();
        }

        public void SetParticleColor(Color newColor)
        {
            particleColor = newColor;
            ApplySettings();
        }

        public void SetIntensity(float newIntensity)
        {
            intensity = Mathf.Clamp(newIntensity, 0f, 4f);
            ApplySettings();
        }

        public void SetWindDirection(Vector3 newWindDirection)
        {
            windDirection = newWindDirection;
            ApplySettings();
        }

        public Color GetParticleColor()
        {
            return particleColor;
        }

        public float GetIntensity()
        {
            return intensity;
        }

        public Vector3 GetWindDirection()
        {
            return windDirection;
        }
    }
}