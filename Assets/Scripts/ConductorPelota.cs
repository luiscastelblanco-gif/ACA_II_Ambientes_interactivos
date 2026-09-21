using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Conduce y dispara la pelota (tiro de castigo).
///
///  - CONDUCIR: si la pelota esta delante del jugador y dentro de la zona de control,
///    se lleva pegada a los pies (se mueve con el jugador).
///  - DISPARAR: clic izquierdo. Mantener para cargar fuerza, soltar para tirar.
///  - REPONER: tecla R (devuelve la pelota al punto de castigo).
///
/// Va en el mismo objeto que el jugador (JugadorTerceraPersona) y funciona tambien
/// si un dia el jugador lo controla una IA: Disparar() se puede llamar desde fuera.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ConductorPelota : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La pelota de la ronda")]
    public Pelota pelota;

    [Tooltip("Camara de tercera persona (de ella sale la direccion del disparo). Si se deja vacia se usa hacia donde mira el jugador")]
    public Transform camara;

    [Header("Zona de control (delante del jugador)")]
    [Tooltip("A que distancia por delante del jugador empieza la zona de control (m)")]
    public float adelante = 0.45f;
    [Tooltip("Radio de la zona de control principal (m)")]
    public float radioControl = 0.9f;
    [Tooltip("Radio de RESCATE: si la pelota se escapa, se sigue controlando hasta esta distancia del jugador (m)")]
    public float radioRescate = 1.45f;
    [Tooltip("A que distancia de los pies se lleva la pelota al conducir (m)")]
    public float distanciaConduccion = 0.7f;
    [Tooltip("Cuanto mas rapida que el jugador puede ir la pelota al conducirla")]
    public float margenConduccion = 1.6f;
    [Tooltip("Velocidad minima con la que la pelota vuelve a los pies (m/s)")]
    public float rapidezMinimaConduccion = 3.5f;
    [Tooltip("Que tan rapido corrige la velocidad de la pelota (mas alto = mas pegada al pie)")]
    public float aceleracionConduccion = 70f;
    [Tooltip("Que tan rapido se reorienta la conduccion al girar (mas alto = gira antes con el jugador)")]
    public float giroConduccion = 6f;
    [Tooltip("Si la pelota va mas rapida que esto (m/s) se suelta. Sube automaticamente al correr")]
    public float rapidezMaximaControl = 9f;

    [Header("Disparo")]
    [Tooltip("Segundos de carga para la fuerza maxima")]
    public float tiempoCargaCompleta = 0.9f;
    [Tooltip("Velocidad de la pelota con carga minima (m/s)")]
    public float fuerzaMinima = 7f;
    [Tooltip("Velocidad de la pelota con carga maxima (m/s)")]
    public float fuerzaMaxima = 21f;
    [Tooltip("Cuanto se levanta la pelota al disparar con carga maxima (m/s). Mas alto = el tiro fuerte sale muy alto y lejos")]
    public float elevacionMaxima = 7.5f;
    [Tooltip("Punto de salida del disparo, medido desde los pies (m)")]
    public float alturaDisparo = 0.28f;
    [Tooltip("Segundos en los que NO se puede conducir la pelota despues de disparar (para que el tiro no se apague)")]
    public float bloqueoTrasDisparo = 0.45f;

    [Header("Estado (solo lectura)")]
    [Tooltip("Carga actual del disparo: 0 = nada, 1 = cargada del todo")]
    public float carga = 0f;
    [Tooltip("True mientras se mantiene pulsado el boton de disparo")]
    public bool cargando = false;
    [Tooltip("True si la pelota esta en la zona de control (se puede conducir o disparar)")]
    public bool pelotaControlada = false;

    private Vector3 posicionAnterior;
    private float rapidezJugador;
    private Vector3 direccionConduccion = Vector3.forward;
    private float bloqueoRestante = 0f;

    void Awake()
    {
        posicionAnterior = transform.position;
    }

    void Update()
    {
        if (bloqueoRestante > 0f) bloqueoRestante -= Time.deltaTime;
        // Rapidez real del jugador (midiendo el desplazamiento: sirve igual para humano o IA)
        Vector3 delta = transform.position - posicionAnterior;
        delta.y = 0f;
        rapidezJugador = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        posicionAnterior = transform.position;

        pelotaControlada = PelotaEnZonaDeControl();
        LeerDisparo();
    }

    /// <summary>True si el jugador tiene la pelota bajo control (zona principal o zona de rescate).</summary>
    public bool PelotaEnZonaDeControl()
    {
        if (pelota == null || !pelota.enJuego || !pelota.gameObject.activeSelf) return false;
        if (bloqueoRestante > 0f) return false;                     // acaba de disparar: ni la toques
        if (pelota.RapidezActual > RapidezMaximaControlable()) return false;
        return DistanciaAlPuntoDeControl() <= radioControl || DistanciaAlJugador() <= radioRescate;
    }

    /// <summary>
    /// Velocidad a partir de la cual la pelota se suelta. Sube con la velocidad del jugador,
    /// asi al correr no se escapa la pelota sola.
    /// </summary>
    public float RapidezMaximaControlable()
    {
        return Mathf.Max(rapidezMaximaControl, rapidezJugador * margenConduccion + 1.5f);
    }

    /// <summary>Distancia horizontal de la pelota al punto de control (delante de los pies).</summary>
    public float DistanciaAlPuntoDeControl()
    {
        Vector3 centro = PuntoDeControl();
        Vector3 hacia = pelota.transform.position - centro;
        hacia.y = 0f;
        return hacia.magnitude;
    }

    /// <summary>Distancia horizontal de la pelota al jugador.</summary>
    public float DistanciaAlJugador()
    {
        Vector3 hacia = pelota.transform.position - transform.position;
        hacia.y = 0f;
        return hacia.magnitude;
    }

    Vector3 PuntoDeControl()
    {
        return transform.position + Vector3.up * alturaDisparo + transform.forward * adelante;
    }

    void LeerDisparo()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse m = Mouse.current;
        bool pulsando = m != null && m.leftButton.isPressed;
        bool recienPulsado = m != null && m.leftButton.wasPressedThisFrame;
        bool recienSoltado = m != null && m.leftButton.wasReleasedThisFrame;

        ActualizarCarga(pulsando, recienPulsado, recienSoltado);

        Keyboard k = Keyboard.current;
        if (k != null && k.rKey.wasPressedThisFrame && pelota != null && pelota.enJuego)
            pelota.Aparecer();
#endif
    }

    /// <summary>
    /// Actualiza la carga del disparo a partir del estado del boton.
    /// Esta separado de la lectura del raton para poder reutilizarlo desde una IA
    /// (o desde pruebas automatizadas) igual que <see cref="Disparar"/>.
    ///   pulsando      = el boton esta apretado ahora
    ///   recienPulsado = se acaba de apretar en este fotograma
    ///   recienSoltado = se acaba de soltar en este fotograma
    /// </summary>
    public void ActualizarCarga(bool pulsando, bool recienPulsado, bool recienSoltado)
    {
        if (recienPulsado && PelotaEnZonaDeControl())
        {
            cargando = true;
            carga = 0f;
        }
        if (cargando && pulsando)
        {
            float antes = carga;
            carga = Mathf.Min(carga + Time.deltaTime / Mathf.Max(0.01f, tiempoCargaCompleta), 1f);
            // Aviso sonoro cuando la carga llega al maximo
            if (antes < 1f && carga >= 1f) GestorAudio.Sonar("carga", 0.8f);
        }
        if (cargando && recienSoltado)
        {
            Disparar(carga);
            cargando = false;
            carga = 0f;
        }
    }

    /// <summary>Direccion horizontal del disparo (la camara si existe, si no hacia donde mira el jugador).</summary>
    public Vector3 DireccionDisparo()
    {
        Vector3 d = camara != null ? camara.forward : transform.forward;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) d = Vector3.forward;
        return d.normalized;
    }

    /// <summary>Dispara la pelota con la carga indicada (0..1). Devuelve false si no habia nada que disparar.</summary>
    public bool Disparar(float cargaDisparo)
    {
        if (!PelotaEnZonaDeControl()) return false;

        Rigidbody rb = pelota.GetComponent<Rigidbody>();
        if (rb == null) return false;

        cargaDisparo = Mathf.Clamp01(cargaDisparo);
        Vector3 direccion = DireccionDisparo();
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, cargaDisparo);

        // El cuerpo se orienta hacia donde se tira (si el jugador estaba quieto, se queda mirando al arco)
        transform.rotation = Quaternion.LookRotation(direccion, Vector3.up);

        rb.WakeUp();
        rb.linearVelocity = direccion * fuerza + Vector3.up * (elevacionMaxima * cargaDisparo);
        rb.angularVelocity = new Vector3(direccion.z, 0f, -direccion.x) * (fuerza * 1.2f);
        pelotaControlada = false;

        // OJO: sin esto la conduccion devolvia la velocidad al fotograma siguiente y el tiro
        // se "apagaba": la pelota salia pero se quedaba pegada al jugador.
        bloqueoRestante = bloqueoTrasDisparo;

        // Sonido de la patada: mas fuerte cuanto mas cargado el tiro
        GestorAudio.SonarEn("patada", pelota.transform.position, 0.55f + 0.45f * cargaDisparo);
        return true;
    }

    void FixedUpdate()
    {
        // Conducir: llevar la pelota pegada a los pies mientras este bajo control
        if (pelota == null || !pelota.enJuego || !pelota.gameObject.activeSelf) return;

        Rigidbody rb = pelota.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (bloqueoRestante > 0f) return;      // acaba de disparar: deja que el tiro salga

        bool enZona = DistanciaAlPuntoDeControl() <= radioControl || DistanciaAlJugador() <= radioRescate;
        if (!enZona) return;
        if (pelota.RapidezActual > RapidezMaximaControlable() && DistanciaAlPuntoDeControl() > radioControl) return;

        // 1) La direccion de conduccion gira SUAVE: si el jugador gira de golpe,
        //    el punto de destino no salta de lado y la pelota no sale volando.
        Vector3 adelanteJugador = transform.forward;
        adelanteJugador.y = 0f;
        if (adelanteJugador.sqrMagnitude < 0.0001f) adelanteJugador = Vector3.forward;
        else adelanteJugador.Normalize();
        direccionConduccion = Vector3.Slerp(direccionConduccion, adelanteJugador, giroConduccion * Time.fixedDeltaTime);
        direccionConduccion.y = 0f;
        if (direccionConduccion.sqrMagnitude < 0.0001f) direccionConduccion = adelanteJugador;
        else direccionConduccion.Normalize();

        // 2) Punto objetivo: siempre delante del jugador
        Vector3 objetivo = transform.position + Vector3.up * pelota.transform.position.y
                           + direccionConduccion * distanciaConduccion;
        Vector3 destino = objetivo - pelota.transform.position;
        destino.y = 0f;
        float distancia = destino.magnitude;

        // 3) La pelota SIEMPRE vuelve a los pies: velocidad segun la distancia, con un minimo
        float rapidezDeseada = Mathf.Min(distancia * 12f, Mathf.Max(rapidezJugador * margenConduccion, rapidezMinimaConduccion));
        Vector3 velocidadDeseada = distancia > 0.01f ? destino / distancia * rapidezDeseada : Vector3.zero;

        // 4) Correccion rapida (pero no instantanea)
        Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        horizontal = Vector3.MoveTowards(horizontal, velocidadDeseada, aceleracionConduccion * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontal.x, rb.linearVelocity.y, horizontal.z);

        // 5) Que ruede de verdad (giro acorde a la velocidad)
        float radio = Mathf.Max(0.05f, pelota.transform.localScale.x * 0.5f);
        rb.angularVelocity = Vector3.Cross(Vector3.up, horizontal) / radio;
    }
}
