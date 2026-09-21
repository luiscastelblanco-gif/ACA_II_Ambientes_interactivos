using UnityEngine;

/// <summary>
/// Arquero de la ronda de tiros de castigo.
///
///  - ACOMODO: se mueve CONSTANTEMENTE por su area (balanceo de lado a lado + siguiendo
///    la posicion de la pelota). Si la pelota se acerca, sale un poco a cortar el angulo.
///  - TIRO: calcula por donde va a cruzar su linea, espera su retardo de reaccion y se
///    lanza hacia ese punto (con algo de error, para que no sea perfecto).
///  - PELOTA LENTA hacia su arco: sale a interceptarla.
///  - Si la pelota choca con el, suma 'toques': el juez de tiro lo usa para saber
///    que fue atajada.
///
/// Se mueve con Rigidbody kinematico + MovePosition, asi empuja la pelota de verdad.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Arquero : MonoBehaviour
{
    [Header("Su area (delante del arco)")]
    [Tooltip("Se mueve entre -mediaCancha y +mediaCancha en z (el arco mide 3.60 m: 1.80 de media)")]
    public float mediaCancha = 1.4f;
    [Tooltip("Cuanto puede salir hacia el campo (m) para ACHICAR el arco cuando el balon se acerca")]
    public float avanceMaximo = 4.5f;
    [Tooltip("Velocidad al moverse por su area (m/s)")]
    public float velocidadAcomodo = 4f;
    [Tooltip("Velocidad al lanzarse al tiro (m/s)")]
    public float velocidadReaccion = 5f;
    [Tooltip("Velocidad al salir a interceptar una pelota que rueda hacia su arco (m/s)")]
    public float velocidadSalida = 4.2f;

    [Header("Movimiento constante por el area")]
    [Tooltip("Cuanto se balancea de lado a lado mientras espera (m). 0 = se queda quieto")]
    public float balanceoAmplitud = 0.3f;
    [Tooltip("Veces por segundo que completa el balanceo")]
    public float balanceoFrecuencia = 0.6f;
    [Tooltip("Cuanto sigue la posicion de la pelota de lado a lado (0.85 = cubre casi todo el costado por el que atacan)")]
    public float seguimientoBalon = 0.85f;
    [Tooltip("Desde cuantos metros del arco empieza a salir a achicar (mas grande = sale antes)")]
    public float distanciaCortaAngulo = 20f;
    [Tooltip("Distancia maxima (m) a la que sale a buscar una pelota lenta que va hacia su arco")]
    public float alcanceSalida = 5.5f;

    [Header("Reaccion al tiro")]
    [Tooltip("Retardo antes de lanzarse (s). Mas alto = arquero mas facil")]
    public float retardo = 0.22f;
    [Tooltip("Error de calculo en metros (0 = sabe siempre donde va la pelota)")]
    public float errorMaximo = 0.4f;
    [Tooltip("Velocidad (m/s) desde la que considera que le dispararon")]
    public float velocidadDisparo = 7f;

    [Header("Referencias")]
    public Pelota pelota;
    [Tooltip("X de la linea de su arco (15.75 = justo delante de los postes, que estan en 15.95)")]
    public float lineaDeGolX = 15.75f;

    [Header("Estado (solo lectura)")]
    [Tooltip("Veces que la pelota lo toco (lo usa el juez)")]
    public int toques = 0;
    public bool lanzado = false;
    public bool saliendo = false;
    public float zPredicho = 0f;

    private Rigidbody rb;
    private float tDisparo = -1f;
    private float tBalanceo = 0f;
    private float xReposo;
    private float zReposo;
    private float xActual;
    private float zActual;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;   // movimiento suave con MovePosition
        xReposo = transform.position.x;
        zReposo = transform.position.z;
        xActual = xReposo;
        zActual = zReposo;
    }

    void Update()
    {
        // Toda la IA del arquero va en Update (que siempre corre) y mueve el Rigidbody kinematico
        // directamente: asi nunca se queda quieto, ni siquiera si la fisica no tiene nada despierto.
        if (rb == null) return;
        rb.WakeUp();

        bool hayPelota = pelota != null && pelota.enJuego && pelota.gameObject.activeSelf;
        if (!hayPelota)
        {
            // Sin ronda: vuelve al centro de su area y mira al campo
            tDisparo = -1f;
            lanzado = false;
            saliendo = false;
            IrAXZ(xReposo, zReposo, velocidadAcomodo);
            MirarALaPelota(new Vector3(0f, transform.position.y, 0f));
            return;
        }

        Vector3 pos = pelota.transform.position;
        Vector3 vel = pelota.Velocidad;

        bool disparo = vel.x > velocidadDisparo;                                        // le dispararon
        bool lentaHaciaMi = !disparo && vel.x > 0.3f && pos.x < lineaDeGolX - 0.6f;     // rueda hacia su arco

        if (disparo)
        {
            // 1) TIRO: lo "lee", espera su retardo y se lanza al punto por donde va a cruzar
            saliendo = false;
            if (tDisparo < 0f)
            {
                tDisparo = Time.time;
                zPredicho = PredecirCruce(pos, vel);
            }
            if (Time.time - tDisparo >= retardo)
            {
                // Animacion de lanzamiento: inclina el cuerpo hacia el lado al que va
                if (!lanzado)
                {
                    Animator anim = GetComponent<Animator>();
                    if (anim != null) anim.SetTrigger(zPredicho > rb.position.z ? "Derecha" : "Izquierda");
                }
                lanzado = true;
                IrAXZ(xReposo, zPredicho, velocidadReaccion);
            }
            else
            {
                // Mientras lee el tiro vuelve a su linea (no se queda adelantado)
                IrAXZ(xReposo, zActual, velocidadAcomodo);
            }
        }
        else if (lentaHaciaMi && Distancia(pos) < alcanceSalida)
        {
            // 2) PELOTA LENTA QUE SE ACERCA: sale a interceptarla
            tDisparo = -1f;
            lanzado = false;
            saliendo = true;
            IrAXZ(pos.x + 0.4f, pos.z, velocidadSalida);
        }
        else
        {
            // 3) ACOMODO: se mueve CONSTANTEMENTE por su area (balanceo) siguiendo la pelota
            tDisparo = -1f;
            lanzado = false;
            saliendo = false;

            // El balanceo usa su propio reloj: se mueve igual jugando que en pruebas paso a paso
            tBalanceo += Time.deltaTime;
            float balanceo = Mathf.Sin(tBalanceo * 2f * Mathf.PI * balanceoFrecuencia) * balanceoAmplitud;
            float zObjetivo = pos.z * seguimientoBalon + balanceo;

            // ACHICAR: cuanto mas cerca esta el balon de su arco, mas sale (hasta avanceMaximo).
            // Pero nunca se pasa del balon: siempre le deja 0.6 m por delante.
            float distanciaAlArco = Mathf.Max(0f, lineaDeGolX - pos.x);
            float avance = Mathf.Clamp01(1f - distanciaAlArco / Mathf.Max(1f, distanciaCortaAngulo)) * avanceMaximo;
            float xObjetivo = Mathf.Max(xReposo - avance, pos.x + 0.6f);

            IrAXZ(xObjetivo, zObjetivo, velocidadAcomodo);
        }

        MirarALaPelota(pos);
    }

    /// <summary>Distancia horizontal a la pelota.</summary>
    float Distancia(Vector3 pos)
    {
        Vector3 a = new Vector3(pos.x, 0f, pos.z);
        Vector3 b = new Vector3(rb.position.x, 0f, rb.position.z);
        return Vector3.Distance(a, b);
    }

    /// <summary>Por donde va a cruzar la pelota su linea (con algo de error, para que no sea perfecto).</summary>
    float PredecirCruce(Vector3 pos, Vector3 vel)
    {
        float t = (lineaDeGolX - pos.x) / Mathf.Max(0.2f, vel.x);
        return pos.z + vel.z * t + Random.Range(-errorMaximo, errorMaximo);
    }

    /// <summary>Lo mueve dentro de su area: x entre (linea - avanceMaximo) y la linea, z entre ±mediaCancha.</summary>
    void IrAXZ(float xDestino, float zDestino, float rapidez)
    {
        float paso = rapidez * Time.deltaTime;
        xActual = Mathf.MoveTowards(xActual, Mathf.Clamp(xDestino, xReposo - avanceMaximo, xReposo), paso);
        zActual = Mathf.MoveTowards(zActual, Mathf.Clamp(zDestino, -mediaCancha, mediaCancha), paso);

        // MovePosition mantiene sincronizado el colisionador (si se mueve solo el transform,
        // el Rigidbody kinematico se desincroniza y deja de atajar).
        rb.WakeUp();
        rb.MovePosition(new Vector3(xActual, rb.position.y, zActual));
    }

    void MirarALaPelota(Vector3 pos)
    {
        Vector3 hacia = pos - transform.position;
        hacia.y = 0f;
        if (hacia.sqrMagnitude < 0.05f) return;
        Quaternion objetivo = Quaternion.LookRotation(hacia.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, 10f * Time.fixedDeltaTime);
    }

    /// <summary>Reinicia los toques (se llama al empezar cada tiro).</summary>
    public void Reiniciar()
    {
        toques = 0;
        lanzado = false;
        tDisparo = -1f;
    }

    /// <summary>Lo devuelve al centro de su area.</summary>
    public void ColocarEnElCentro()
    {
        xActual = xReposo;
        zActual = zReposo;
        rb.position = new Vector3(xReposo, rb.position.y, zReposo);
        transform.position = new Vector3(xReposo, transform.position.y, zReposo);
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.GetComponent<Pelota>() != null) toques++;
    }
}
