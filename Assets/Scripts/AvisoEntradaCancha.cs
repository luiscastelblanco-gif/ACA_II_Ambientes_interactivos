using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Detecta cuando el jugador entra a la cancha y le pregunta si quiere iniciar
/// una ronda de tiros de castigo (E = si, Q = no).
///
/// Cuando existan la pelota y el arquero, la ronda se arranca desde <see cref="IniciarRonda"/>.
/// </summary>
public class AvisoEntradaCancha : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El jugador que se mueve por el barrio")]
    public Transform jugador;

    [Tooltip("Panel que contiene la pregunta")]
    public GameObject panelAviso;

    [Tooltip("Texto de la pregunta")]
    public Text textoAviso;

    [Header("Cierre de la cancha")]
    [Tooltip("Porton que se activa mientras la ronda esta en curso")]
    public GameObject porton;

    [Tooltip("Guia de comandos: durante la ronda oculta 'Accion / Entrar' y muestra 'Salir de la ronda'")]
    public GuiaComandos guia;

    [Tooltip("Indicador de la ronda (arriba a la derecha): tiros y atajadas")]
    public IndicadorRonda indicador;

    [Tooltip("Pelota que aparece en el centro de la cancha durante la ronda")]
    public Pelota pelota;

    [Tooltip("Juez de tiro: sabe cuando la ronda ya termino (para poder repetirla con R)")]
    public JuezTiro juez;

    [Header("Zona de la cancha (malla perimetral)")]
    public float medioLargo = 18f;      // |x| <= 18
    public float medioAncho = 12f;      // |z| <= 12
    public float alturaMaxima = 4f;     // para no dispararlo desde arriba

    [Header("Textos de entrada")]
    public string pregunta = "¿Quieres iniciar una ronda de tiros de castigo?";
    public string pistaSi = "E   ·   Sí, iniciar ronda";
    public string pistaNo = "Q   ·   Ahora no";

    [Header("Textos de salida")]
    public string preguntaSalir = "¿Quieres salir de la ronda de tiros de castigo?";
    public string pistaSalirSi = "E   ·   Sí, salir de la ronda";
    public string pistaSalirNo = "Q   ·   No, seguir jugando";

    [Header("Mensajes")]
    public string mensajeInicio = "¡Ronda de tiros de castigo lista para jugar!";
    public string mensajeFin = "Ronda de tiros finalizada";
    public string mensajeAunNoSePuedeRepetir = "Termina los 5 tiros para poder repetir la ronda";
    public float segundosMensaje = 2.5f;

    [Header("Estado (solo lectura)")]
    public bool rondaActiva = false;

    private bool estabaDentro = false;
    private bool avisoVisible = false;
    private bool preguntandoSalir = false;
    private bool mostrandoMensaje = false;
    private bool eAntes = false;
    private bool qAntes = false;
    private bool fAntes = false;

    void Start()
    {
        if (panelAviso != null) panelAviso.SetActive(false);
        // El porton ya no se esconde: se ABRE y se CIERRA con su animacion (hundido = abierto).
        // Solo se oculta si todavia no tiene animacion (plan B).
        if (porton != null && porton.GetComponent<Animator>() == null) porton.SetActive(false);
    }

    void Update()
    {
        if (jugador == null) return;

        // 1) ¿Esta dentro de la cancha?
        Vector3 p = jugador.position;
        bool ahoraDentro = Mathf.Abs(p.x) <= medioLargo && Mathf.Abs(p.z) <= medioAncho && p.y <= alturaMaxima;
        bool acabaDeEntrar = ahoraDentro && !estabaDentro;
        estabaDentro = ahoraDentro;

        // 2) Al entrar (y si no hay ronda) se pregunta
        if (acabaDeEntrar && !rondaActiva)
        {
            preguntandoSalir = false;
            MostrarAviso(true);
        }

        // 3) Lectura de teclas (deteccion de flanco propia)
        Keyboard k = Keyboard.current;
        if (k == null) return;
        bool e = k.eKey.isPressed;
        bool q = k.qKey.isPressed;
        bool f = k.fKey.isPressed;
        bool pulsoE = k.eKey.wasPressedThisFrame || (e && !eAntes);
        bool pulsoQ = k.qKey.wasPressedThisFrame || (q && !qAntes);
        bool pulsoF = k.fKey.wasPressedThisFrame || (f && !fAntes);
        eAntes = e;
        qAntes = q;
        fAntes = f;
        if (!pulsoE && !pulsoQ && !pulsoF) return;

        // 4) F = repetir la ronda. Se comprueba ANTES de la pregunta para que funcione
        //    aunque el aviso este abierto (antes la F se interpretaba como "Q").
        if (pulsoF && IntentarRepetirRonda()) return;

        // 5) Si hay una pregunta abierta, responder
        if (avisoVisible && !mostrandoMensaje)
        {
            if (preguntandoSalir)
            {
                if (pulsoE) SalirDeRonda();
                else MostrarAviso(false);            // Q = seguir jugando
            }
            else
            {
                if (pulsoE) IniciarRonda();
                else MostrarAviso(false);            // Q = no iniciar
            }
            return;
        }

        // 6) Sin pregunta abierta: si esta en la ronda, E pregunta si quiere salir
        if (rondaActiva && pulsoE)
        {
            preguntandoSalir = true;
            MostrarAviso(true);
        }
    }

    /// <summary>
    /// Repite la ronda (tecla F). Solo funciona cuando ya se lanzaron los 5 tiros:
    /// borra el marcador anterior y deja la ronda nueva lista para jugar.
    /// Devuelve true si reinicio la ronda; si no, avisa de que hay que terminar los tiros.
    /// Esta separado de la lectura del teclado para poder probarlo igual que Disparar().
    /// </summary>
    public bool IntentarRepetirRonda()
    {
        if (!rondaActiva || juez == null) return false;
        GestorAudio.Sonar("clic", 0.8f);

        if (juez.RondaTerminada)
        {
            IniciarRonda();
            Debug.Log("[Penales] Ronda REPETIDA (F): marcador anterior borrado, ronda nueva lista");
            return true;
        }

        preguntandoSalir = false;
        MostrarAviso(false);
        Mensaje(mensajeAunNoSePuedeRepetir, segundosMensaje);
        return false;
    }

    public void MostrarAviso(bool mostrar)
    {
        avisoVisible = mostrar;
        if (mostrar)
        {
            CancelInvoke("OcultarMensaje");
            mostrandoMensaje = false;
            GestorAudio.Sonar("clic", 0.6f);        // cuando aparece la pregunta
        }
        if (panelAviso != null) panelAviso.SetActive(mostrar);
        if (mostrar && textoAviso != null)
            textoAviso.text = preguntandoSalir
                ? preguntaSalir + "\n\n" + pistaSalirSi + "            " + pistaSalirNo
                : pregunta + "\n\n" + pistaSi + "            " + pistaNo;
    }

    /// <summary>
    /// Arranca la ronda: CIERRA la cancha con el porton y deja al jugador adentro.
    /// Aqui se conectaran despues la pelota, el arquero y el marcador de tiros.
    /// </summary>
    public void IniciarRonda()
    {
        rondaActiva = true;
        preguntandoSalir = false;
        MostrarAviso(false);

        // Si el jugador quedo justo en la entrada, lo pasamos un poco hacia adentro
        // para que el porton no lo deje atrapado.
        if (jugador != null && Mathf.Abs(jugador.position.z - 12f) < 1.8f && Mathf.Abs(jugador.position.x) < 3.6f)
        {
            System.Type tipo = System.Type.GetType("JugadorTerceraPersona, Assembly-CSharp");
            if (tipo != null)
            {
                Component comp = jugador.GetComponent(tipo);
                System.Reflection.MethodInfo m = tipo.GetMethod("ColocarEn");
                if (comp != null && m != null)
                    m.Invoke(comp, new object[] { new Vector3(jugador.position.x, jugador.position.y, 10.2f), jugador.eulerAngles.y });
            }
        }

        CerrarPorton(true);                                    // CIERRA la cancha (con animacion)
        GestorAudio.Sonar("silbato");
        if (guia != null) guia.ModoRonda(true);
        if (indicador != null) { indicador.Reiniciar(); indicador.Mostrar(true); }
        if (juez != null) juez.Reiniciar();                   // contadores y mensaje del tiro a cero
        if (pelota != null) pelota.Aparecer();
        Mensaje(mensajeInicio, segundosMensaje);
        Debug.Log("[Penales] Ronda INICIADA: cancha cerrada + pelota en el centro de la cancha");
    }

    /// <summary>Termina la ronda y vuelve a ABRIR la cancha.</summary>
    public void SalirDeRonda()
    {
        rondaActiva = false;
        preguntandoSalir = false;
        CerrarPorton(false);                                   // ABRE la cancha (con animacion)
        GestorAudio.Sonar("silbato");
        if (guia != null) guia.ModoRonda(false);
        if (indicador != null) indicador.Mostrar(false);
        if (pelota != null) pelota.Desaparecer();
        Mensaje(mensajeFin, segundosMensaje);
        Debug.Log("[Penales] Ronda FINALIZADA: cancha abierta y pelota retirada");
    }

    /// <summary>
    /// Cierra (true) o abre (false) el porton de la entrada: lanza su animacion y
    /// suena la reja metalica. Si todavia no tiene animacion, lo muestra/oculta.
    /// </summary>
    public void CerrarPorton(bool cerrar)
    {
        if (porton == null) return;

        Animator anim = porton.GetComponent<Animator>();
        if (anim != null)
        {
            porton.SetActive(true);
            anim.SetTrigger(cerrar ? "Cerrar" : "Abrir");
        }
        else
        {
            porton.SetActive(cerrar);
        }

        GestorAudio.SonarEn("porton", porton.transform.position, 0.9f);
    }

    private void Mensaje(string texto, float segundos)
    {
        mostrandoMensaje = true;
        if (panelAviso != null) panelAviso.SetActive(true);
        if (textoAviso != null) textoAviso.text = texto;
        CancelInvoke("OcultarMensaje");
        Invoke("OcultarMensaje", segundos);
    }

    void OcultarMensaje()
    {
        if (panelAviso != null) panelAviso.SetActive(false);
        avisoVisible = false;
        mostrandoMensaje = false;
    }
}
