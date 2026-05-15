using UnityEngine;

public class Wind : MonoBehaviour, IForceGenerator
{
    [Header("Conexi?n")]
    // Aqu? va lo que tu fuerza necesita: una part?cula, un transform, etc.
    public float ImpactRange = 10f; // Rango de impacto del viento y(o) turbulencia
    public bool GlobalImpact = false; // Define si el impacto ser? a nivel global o no, inicialmente est? en falso

    // SE DAN LAS 2 OPCIONES PARA QUE EN UNITY SE PUEDA AJUSTAR A LA QUE SE PREFIERA

    [Header("Par?metros")]
    // Aqu? van los floats/vectores que controlan tu fuerza.
    public Vector3 windDirection = Vector3.right; // Inicialmente, a pesar de los ajustes posteriores, empezara la fuerza hacia la derecha
    public float strength = 5f; // Fuerza del viento
    public float turbulenceIntensity = 2f; // Indicar? la intensidad de las r?fagas de viento (turbulencias)
    public float turbulenceFrequency = 1.5f; // Indica que tan frecuentes ser?n las turbulencias

    private void OnEnable() { ParticleWorld.Register((IForceGenerator)this); } // Cuando el componente se active, indice al ForceGenerator que realice la acci?n

    private void OnDisable() { ParticleWorld.Unregister((IForceGenerator)this); } // Cuando el component se desactive o se destruya la escena, le indica al ForceGenerator que deje de llamar a la acci?n

    public void ApplyForces(float dt)
    {
        // Calcular la fuerza y aplicarla con particula.AddForce(...)

        //Teniendo en cuenta que se implementar?n tambien turbulencias, primero se calcula la fuerza del viento
        Vector3 WildForceBase = windDirection * strength; // Para implementar la fuerza del viento en la simulaci?n, se multiplica la direcci?n del viento por la fuerza que este est? aplicando en este momento

        // Para aplicar turbulencias, se genera un ruido sinusoidal (Mathf.PerlinNoise(Time.time, ...)) para realizar una variaci?n natural y suave a lo largo del tiempo, para simular este tipo de fen?menos naturales
        // Preferiblemente, para tener una mayor presici?n se utilizar?a como variable float
        float noiseOfTheWild = Mathf.PerlinNoise(Time.time * turbulenceFrequency, 0); // Mathf.PerlinNoise(x, y) genera una curva suave, usando las coordenadas X,Y:
                                                                                      // Siendo la X para que el viento cambie seg?n el paso del tiempo
                                                                                      // Y no es necesaria en este caso debido a que la simulaci?n no es en 2D, por tal raz?n se deja como 0
                                                                                      // Para que el viento no perman?sca est?tico, se le agrega al valor de X el Time.time,
                                                                                      // ya que este usa el tiempo transcurrido desde el inicio de la simulaci?n como una variable continua, por esa raz?n tambien se multiplica este valor con la frecuencia que se quieren presentar las turbulencias, si no, aumentar?an de 1 en 1 c/segundo con frecuencia de 0.1

        float realSensation = noiseOfTheWild * turbulenceIntensity; // Para presentar la sensaci?n real de una turbulencia se multiplica el ruido de esta por el valor de la intensidad de la turbulencia

        // Para obtener la fuerza del viento se utiliza la formula F = Fbase + Fvariable(t)
        //Siendo Fbase la fuerza del viento y Fvariable la turbulencia y la (t) significa "en funcion del tiempo", esta esta reflejada con Mathf.PerlinNoise(Time.time...)

        // F = Fbase + Fvariable(t)
        // F = (windDirection * strength) + (windDirection * realSensation)

        Vector3 WindForceRealist = WildForceBase + (windDirection * realSensation); // Se utiliza (windDirection * realSensation) debido a que si se pone solo el valor de real sensation, al ser este un float y el otro un vector, no se pueden sumar
                                                                                    // Adem?s, el vector de la direccion del viento es unitario, entonces esto evita que se altere la fuerza de las turbulencias, las cuales estan en el float de realSensation

        foreach (Particle p in ParticleWorld.All) // Por cada particula que haya en el mundo...
        {
            // Fuerza del viento, con los valores que ya se tenian anteriormente hacen que funcione

            float distance = Vector3.Distance(transform.position, p.Position);

            if (GlobalImpact || distance <= ImpactRange) // si el impacto es global o la distancia es menor o igual al rango de impacto (a su alcance)...
            {
                p.AddForce(WindForceRealist);
                // p.AddForce() no cambia la posici?n de la particula autom?ticamente, sino que suma el vector de viento junto con su operaci?n(windDirection * strength) teniendo en cuenta tambien las otras fuerzas que tiene anteriormente la particula (gravedad, resorte, ...)
                // En pocas palabras, p.AddForce aplica la segunda ley de newton (F = m*a), la aceleraci?n de un objeto es directamente proporcional a la fuerza neta que act?a sobre ?l e inversamente proporcional a su masa. Indicando que la fuerza y la aceleraci?n comparten direcci?n
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Visualizaci?n para debug.

        Gizmos.color = Color.yellow;
        if (!GlobalImpact) // Si el impacto global es true...
        {
            Gizmos.DrawWireSphere(transform.position, ImpactRange); // Entonces con amarillo se observar? el rango de impacto de este
        }
    }
}