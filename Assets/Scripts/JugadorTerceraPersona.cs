using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Jugador en tercera persona para la cancha de penales.
/// Movimiento WASD relativo a la camara, correr con Shift, saltar con Espacio.
///
/// Requiere el paquete Input System y "Active Input Handling = Input System"
/// (este proyecto ya esta configurado asi, por eso NO se usa el viejo Input.GetAxis).
///
/// Pensado para reutilizarse: <see cref="LeerEntrada"/> es virtual, de modo que un futuro
/// "JugadorIA" puede heredar de esta clase y decidir el movimiento por su cuenta,
/// reutilizando toda la fisica y el CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class JugadorTerceraPersona : MonoBehaviour
{
    [Header("Velocidades")]
    [Tooltip("Velocidad al caminar (m/s)")]
    public float velocidadCaminar = 4.5f;

    [Tooltip("Velocidad al correr con Shift (m/s)")]
    public float velocidadCorrer = 7.5f;

    [Tooltip("Que tan rapido acelera y frena")]
    public float aceleracion = 25f;

    [Tooltip("Que tan rapido gira el cuerpo hacia donde se mueve")]
    public float velocidadGiro = 14f;

    [Header("Salto y gravedad")]
    public float gravedad = -22f;
    public float alturaSalto = 1.0f;

    [Header("Referencias")]
    [Tooltip("Camara de tercera persona. El movimiento se orienta respecto a ella (WASD = adelante/atras/lados de la camara)")]
    public Transform camara;

    [Header("Estado (solo lectura)")]
    [Tooltip("Se puede congelar desde otro script (por ejemplo durante el tiro de castigo)")]
    public bool puedeMoverse = true;

    [SerializeField] private Vector3 velocidadActual;
    [SerializeField] private bool enSuelo;

    private CharacterController cc;
    private float velocidadVertical;
    private Vector3 direccionMovimiento;
    private bool espacioAntes;

    private Animator animador;
    private float desplazamiento = 0f;
    private float desplazamientoEscrito = -1f;
    private static readonly int HashDesplazamiento = Animator.StringToHash("Desplazamiento");

    /// <summary>Velocidad horizontal actual en m/s (util para animaciones o para saber si corre).</summary>
    public float RapidezActual { get { return new Vector2(velocidadActual.x, velocidadActual.z).magnitude; } }

    /// <summary>True cuando el CharacterController toca el suelo.</summary>
    public bool EnSuelo { get { return enSuelo; } }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animador = GetComponent<Animator>();          // Blend Tree de locomocion (JugadorMovimiento)
    }

    void Update()
    {
        Vector2 entrada; bool correr; bool saltar;
        LeerEntrada(out entrada, out correr, out saltar);

        // 1) Ejes de referencia segun la camara (asi WASD siempre es "hacia donde miro")
        Vector3 adelante = Vector3.forward;
        Vector3 derecha = Vector3.right;
        if (camara != null)
        {
            adelante = camara.forward; adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f) adelante = Vector3.forward; else adelante.Normalize();
            derecha = camara.right; derecha.y = 0f;
            if (derecha.sqrMagnitude < 0.0001f) derecha = Vector3.right; else derecha.Normalize();
        }

        // 2) Direccion deseada
        Vector3 deseada = Vector3.zero;
        if (puedeMoverse)
        {
            deseada = adelante * entrada.y + derecha * entrada.x;
            if (deseada.sqrMagnitude > 1f) deseada.Normalize();
        }
        float rapidez = correr ? velocidadCorrer : velocidadCaminar;

        // 3) Aceleracion / frenado suaves sobre el plano
        Vector3 planoActual = new Vector3(velocidadActual.x, 0f, velocidadActual.z);
        planoActual = Vector3.MoveTowards(planoActual, deseada * rapidez, aceleracion * Time.deltaTime);
        velocidadActual = new Vector3(planoActual.x, 0f, planoActual.z);
        direccionMovimiento = planoActual;

        // 4) Gravedad y salto
        enSuelo = cc.isGrounded;
        if (enSuelo && velocidadVertical <= 0f) velocidadVertical = -2f;
        if (puedeMoverse && saltar && enSuelo)
            velocidadVertical = Mathf.Sqrt(-2f * gravedad * alturaSalto);
        velocidadVertical += gravedad * Time.deltaTime;

        // 5) Mover
        cc.Move((velocidadActual + Vector3.up * velocidadVertical) * Time.deltaTime);

        // 6) Girar el cuerpo hacia donde se mueve
        if (direccionMovimiento.sqrMagnitude > 0.04f)
        {
            Quaternion objetivo = Quaternion.LookRotation(direccionMovimiento.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
        }

        // 7) Animacion: alimenta el Blend Tree de locomocion con la velocidad real
        ActualizarAnimacion();
    }

    /// <summary>
    /// Pasa la velocidad real al Blend Tree "Locomocion" (parametro float "Desplazamiento":
    /// 0 = quieto, 0.45 = caminando, 1 = corriendo). El valor se suaviza y solo se escribe
    /// cuando cambia de verdad, para no llamar a SetFloat en todos los cuadros al pepe.
    /// </summary>
    private void ActualizarAnimacion()
    {
        if (animador == null) return;

        float objetivo = Mathf.Clamp01(RapidezActual / Mathf.Max(0.01f, velocidadCorrer));
        desplazamiento = Mathf.MoveTowards(desplazamiento, objetivo, 4f * Time.deltaTime);

        if (Mathf.Abs(desplazamiento - desplazamientoEscrito) < 0.01f) return;

        desplazamientoEscrito = desplazamiento;
        animador.SetFloat(HashDesplazamiento, desplazamiento);
    }

    /// <summary>
    /// Lectura de entrada. Es virtual para que una IA (JugadorIA) pueda sobreescribirla
    /// y decidir el movimiento sin teclado, reutilizando todo lo demas.
    /// </summary>
    protected virtual void LeerEntrada(out Vector2 entrada, out bool correr, out bool saltar)
    {
        entrada = Vector2.zero;
        correr = false;
        saltar = false;

        Keyboard k = Keyboard.current;
        if (k == null) return;

        if (k.wKey.isPressed || k.upArrowKey.isPressed) entrada.y += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) entrada.y -= 1f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) entrada.x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) entrada.x += 1f;
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();

        correr = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;

        // Deteccion de flanco propia: ademas de wasPressedThisFrame comprobamos el cambio de estado
        // respecto al frame anterior. Asi el salto sigue respondiendo aunque el sistema de entrada
        // no llegue a actualizarse en el mismo frame (por ejemplo al avanzar la simulacion
        // fotograma a fotograma en pruebas automatizadas).
        bool espacioAhora = k.spaceKey.isPressed;
        saltar = k.spaceKey.wasPressedThisFrame || (espacioAhora && !espacioAntes);
        espacioAntes = espacioAhora;
    }

    /// <summary>Teletransporta al jugador sin pelearse con el CharacterController.</summary>
    public void ColocarEn(Vector3 posicion, float gradosY)
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = posicion;
        transform.rotation = Quaternion.Euler(0f, gradosY, 0f);
        cc.enabled = true;
        velocidadActual = Vector3.zero;
        velocidadVertical = 0f;
    }
}
