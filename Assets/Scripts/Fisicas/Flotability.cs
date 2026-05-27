using UnityEngine;

/*
 Generador de fuerza de flotación que implementa el principio de Arquímedes.
 Aplica una fuerza de flotación hacia arriba a las partículas que se encuentran por debajo del nivel del agua.

 Fórmula física: F_buoy = -? * V_sumergido * g

 Parámetros y su efecto:
     - waterLevel: altura de la superficie del agua. Partículas por encima NO reciben fuerza.
     - density: densidad del agua (?). Mayor densidad ? mayor fuerza de flotación.
     - drag: amortiguamiento en el agua (opcional). Mayor drag ? movimiento más lento dentro del agua.
     - volumeScaleFactor: factor que relaciona el volumen de la partícula con su tamaño visual.
       (Por defecto, volumen = tamaño³, lo que genera equilibrio realista.)
*/
public class Flotability : MonoBehaviour, IForceGenerator
{
    [Header("Conexión")]
    // Este componente se aplica a todas las partículas automáticamente

    [Header("Parámetros del agua")]
    [Tooltip("Altura Y del nivel del agua.")]
    public float waterLevel = 0f;

    [Tooltip("Densidad del agua. Determina la magnitud de la fuerza de flotación.")]
    [Min(0.1f)] public float density = 10f;

    [Tooltip("Coeficiente de arrastre en el agua. Aumenta la resistencia.")]
    [Min(0f)] public float drag = 0.5f;

    [Header("Visualización")]
    [Tooltip("Color de la línea de agua en Gizmos.")]
    public Color waterLineColor = new Color(0.2f, 0.6f, 1f, 0.7f);

    private void OnEnable()
    {
        ParticleWorld.Register((IForceGenerator)this);
    }

    private void OnDisable()
    {
        ParticleWorld.Unregister((IForceGenerator)this);
    }

    public void ApplyForces(float dt)
    {
        // Iterar todas las partículas del mundo
        foreach (Particle particle in ParticleWorld.All)
        {

            // Solo aplicar si la partícula está bajo el agua
            if (particle.Position.y >= waterLevel)
                continue;

            // Calcular el volumen sumergido
            // Profundidad: qué tan lejos está bajo la superficie
            float depth = waterLevel - particle.Position.y;

            // Volumen aproximado de la partícula (radio ? volumen esférico)
            float radius = particle.Radius;
            float particleVolume = (4f / 3f) * Mathf.PI * radius * radius * radius;

            // Volumen sumergido (simplificado: suponemos que es proporcional a la profundidad)
            // Para profundidades pequeñas, V_sumergido ? depth
            // Para profundidades mayores que el radio, V_sumergido = particleVolume
            float submergedVolume = Mathf.Min(depth / (2f * radius), 1f) * particleVolume;

            // Fuerza de flotación: F = -? * V * g (vertical, hacia arriba)
            // Nota: el signo negativo es porque la gravedad apunta hacia abajo (-Y)
            float g = Mathf.Abs(particle.gravity.y); // Magnitud de la gravedad
            Vector3 buoyantForce = new Vector3(0f, density * particleVolume * g, 0f);

            // Aplicar la fuerza de flotación
            particle.AddForce(buoyantForce);

            // DEBUG
            Debug.Log($"Partícula: {particle.name} | Profundidad: {depth:F2} | Volumen sumergido: {submergedVolume:F4} | Fuerza flotación: {buoyantForce.y:F2} | Masa: {particle.Mass:F2}");

            // Aplicar fricción del agua: F_drag = -drag * velocidad
            // (La fricción solo actúa dentro del agua, para evitar afectar partículas en aire)
            if (drag > 0f)
            {
                Vector3 dragForce = -drag * particle.Velocity;
                particle.AddForce(dragForce);
            }

        }
    }

    private void OnDrawGizmos()
    {
        // Visualizar la línea del nivel del agua
        Gizmos.color = waterLineColor;

        // Dibujar una línea horizontal en el nivel del agua
        Vector3 center = transform.position + Vector3.up * waterLevel;
        float lineLength = 20f;
        Gizmos.DrawLine(center - Vector3.right * lineLength, center + Vector3.right * lineLength);
        Gizmos.DrawLine(center - Vector3.forward * lineLength, center + Vector3.forward * lineLength);

        // Dibujar una caja que represente el área del agua (por debajo)
        Vector3 waterBoxCenter = center - Vector3.up * lineLength;
        Vector3 waterBoxSize = new Vector3(lineLength * 2f, lineLength, lineLength * 2f);
        Gizmos.DrawWireCube(waterBoxCenter, waterBoxSize);
    }
}
