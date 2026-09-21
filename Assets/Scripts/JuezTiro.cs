using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Juez de los tiros de castigo: mira la pelota y decide el resultado de cada tiro.
///
///   GOL     : la pelota cruza la linea entre los palos y por debajo del travesano
///   ATAJADA : el arquero la toco y no entro
///   FALLADO : ni gol ni atajada (afuera, al palo, se quedo corta...)
///
/// Pinta el circulo que toca (IndicadorRonda), muestra el mensaje del resultado y,
/// al terminar el tiro, repone la pelota en el centro de la cancha para el siguiente.
/// </summary>
public class JuezTiro : MonoBehaviour
{
    private enum Estado { Esperando, EnVuelo, Resultado, FinRonda }

    [Header("Referencias")]
    public Pelota pelota;
    public Arquero arquero;
    public IndicadorRonda indicador;
    [Tooltip("Panel del mensaje grande del resultado")]
    public GameObject panelResultado;
    public Text textoResultado;

    [Header("Medidas del arco (arco este)")]
    [Tooltip("X de la linea de gol")]
    public float lineaDeGolX = 15.95f;
    [Tooltip("Media cancha del arco: |z| <= esto es dentro de los palos (arco de 3.60 m)")]
    public float mediaArcoZ = 1.8f;
    [Tooltip("Altura del travesano")]
    public float alturaTravesano = 2.44f;

    [Header("Tiempos")]
    [Tooltip("Velocidad desde la que se considera que hubo disparo (m/s)")]
    public float velocidadMinimaDisparo = 6f;
    [Tooltip("Cuanto dura el mensaje del resultado y la pausa antes del siguiente tiro (s)")]
    public float pausaTrasResultado = 2.2f;
    [Tooltip("Si el tiro se eterniza, se da por fallado (s)")]
    public float tiempoMaximoTiro = 6f;
    [Tooltip("Por debajo de esta rapidez la pelota se considera quieta (m/s)")]
    public float rapidezQuieta = 1.2f;
    [Tooltip("Cuanto tiene que estar quieta para dar el tiro por terminado (s)")]
    public float tiempoQuieta = 0.5f;

    [Header("Mensajes")]
    public string textoGol = "¡GOL!";
    public string textoAtajada = "¡ATAJADA!";
    public string textoFallado = "¡FALLADO!";
    public Color colorGol = new Color(0.30f, 0.95f, 0.40f, 1f);
    public Color colorAtajada = new Color(1f, 0.60f, 0.20f, 1f);
    public Color colorFallado = new Color(0.95f, 0.30f, 0.25f, 1f);
    public float tamanoMensajeFinal = 34f;

    [Header("Estado (solo lectura)")]
    public string ultimoResultado = "";
    public int goles = 0;
    [Tooltip("Tiros que NO fueron gol (atajados por el arquero o tiros fallados)")]
    public int atajadasDeLaRonda = 0;
    [Tooltip("True si en el ultimo tiro la pelota llego a tocar al arquero")]
    public bool ultimoToqueDelArquero = false;
    [Tooltip("Z por la que entro el ultimo gol (para comprobar que la deteccion es fina)")]
    public float ultimoZCruce = 0f;

    private Estado estado = Estado.Esperando;
    private float tTiro = 0f;
    private float tResultado = 0f;
    private float tQuieta = -1f;
    private int toquesAlTirar = 0;
    private bool estabaEnJuego = false;
    private Vector3 posicionAnterior;

    /// <summary>True cuando ya se lanzaron los 5 tiros: la ronda se puede repetir (R).</summary>
    public bool RondaTerminada
    {
        get { return estado == Estado.FinRonda; }
    }

    void Update()
    {
        if (pelota == null) return;

        bool enJuego = pelota.enJuego && pelota.gameObject.activeSelf;
        if (enJuego && !estabaEnJuego) Reiniciar();          // empezo la ronda
        estabaEnJuego = enJuego;

        if (!enJuego)
        {
            if (panelResultado != null && panelResultado.activeSelf) panelResultado.SetActive(false);
            return;
        }

        posicionAnterior = pelota.transform.position - pelota.Velocidad * Time.deltaTime;

        if (estado == Estado.Esperando)
        {
            if (pelota.Velocidad.x > velocidadMinimaDisparo) EmpezarTiro();
        }
        else if (estado == Estado.EnVuelo) Vigilar();
        else if (estado == Estado.Resultado)
        {
            if (Time.time - tResultado >= pausaTrasResultado) SiguienteTiro();
        }
    }

    /// <summary>Deja todo listo para una ronda nueva.</summary>
    public void Reiniciar()
    {
        estado = Estado.Esperando;
        goles = 0;
        atajadasDeLaRonda = 0;
        ultimoResultado = "";
        tQuieta = -1f;
        toquesAlTirar = 0;
        if (arquero != null) arquero.Reiniciar();
        if (panelResultado != null) panelResultado.SetActive(false);
    }

    void EmpezarTiro()
    {
        estado = Estado.EnVuelo;
        tTiro = Time.time;
        tQuieta = -1f;
        toquesAlTirar = arquero != null ? arquero.toques : 0;
        ultimoResultado = "";                                 // se limpia para saber que el tiro esta en vuelo
        if (panelResultado != null) panelResultado.SetActive(false);
    }

    void Vigilar()
    {
        Vector3 p = pelota.transform.position;
        Vector3 v = pelota.Velocidad;
        float rapidez = v.magnitude;

        // 1) ¿Gol? Se comprueba el tramo recorrido en el fotograma: a 21 m/s la pelota
        //    avanza ~0.35 m por fotograma y no se puede mirar solo la posicion actual.
        Vector3 a = posicionAnterior;
        if (a.x < lineaDeGolX && p.x >= lineaDeGolX)
        {
            float f = (lineaDeGolX - a.x) / Mathf.Max(0.0001f, p.x - a.x);
            float zCruce = Mathf.Lerp(a.z, p.z, f);
            float yCruce = Mathf.Lerp(a.y, p.y, f);
            if (Mathf.Abs(zCruce) <= mediaArcoZ && yCruce <= alturaTravesano)
            {
                ultimoZCruce = zCruce;
                Resolver(true);
                return;
            }
        }

        // 2) ¿Paso la linea de gol pero por fuera de los palos?
        if (p.x > lineaDeGolX && Mathf.Abs(p.z) > mediaArcoZ) { Resolver(false); return; }

        // 2b) ¿Se fue bien lejos detras del arco?
        if (p.x > lineaDeGolX + 2.5f) { Resolver(false); return; }

        // 2c) El arquero la toco y la pelota ya no va hacia el arco: ATAJADA al instante
        //     (asi no hay que esperar a que ruede por todo el campo)
        if (arquero != null && arquero.toques > toquesAlTirar && v.x <= 0.5f) { Resolver(false); return; }

        // 3) ¿Se quedo quieta (en cualquier parte) o se acabo el tiempo?
        bool quieta = rapidez <= rapidezQuieta;
        if (quieta) { if (tQuieta < 0f) tQuieta = Time.time; }
        else tQuieta = -1f;
        if ((tQuieta > 0f && Time.time - tQuieta >= tiempoQuieta) || Time.time - tTiro >= tiempoMaximoTiro)
            Resolver(false);
    }

    void Resolver(bool fueGol)
    {
        // ATAJADA = todo lo que no fue gol: si la atajo el arquero o si el tiro se fue afuera.
        // Asi la fila de atajadas queda como "tiros no convertidos".
        bool atajada = !fueGol;
        ultimoToqueDelArquero = arquero != null && arquero.toques > toquesAlTirar;

        if (fueGol) goles++;
        if (atajada) atajadasDeLaRonda++;
        ultimoResultado = fueGol ? "GOL" : (ultimoToqueDelArquero ? "ATAJADA" : "FALLADO");

        // Sonido del resultado: refuerza el mensaje que aparece en pantalla
        if (fueGol) GestorAudio.Sonar("gol");
        else if (ultimoToqueDelArquero) GestorAudio.Sonar("atajada");
        else GestorAudio.Sonar("fallado");

        if (indicador != null)
        {
            indicador.RegistrarTiro(fueGol);                 // circulo de tiros: verde si fue gol
            indicador.RegistrarAtajada(atajada);             // circulo de atajadas: verde si atajo
        }

        MostrarMensaje(ultimoResultado,
            fueGol ? colorGol : (atajada ? colorAtajada : colorFallado));

        estado = Estado.Resultado;
        tResultado = Time.time;
        Debug.Log("[Penales] Tiro " + (indicador != null ? indicador.tiros.ToString() : "?") + ": " + ultimoResultado +
                  (ultimoToqueDelArquero ? " (la toco el arquero)" : "") +
                  (fueGol ? " (entro por z=" + ultimoZCruce.ToString("F2") + ")" : " (no convertido)"));
    }

    void SiguienteTiro()
    {
        if (indicador != null && indicador.RondaCompleta)
        {
            estado = Estado.FinRonda;
            GestorAudio.Sonar("silbato");
            MostrarMensaje("RONDA TERMINADA\nGoles: " + goles + "   ·   Atajadas: " + atajadasDeLaRonda +
                           "\nF · Repetir la ronda          E · Salir", Color.white, tamanoMensajeFinal);
            if (arquero != null) arquero.Reiniciar();
            return;
        }

        if (pelota != null) pelota.Aparecer();       // vuelve al centro de la cancha
        if (arquero != null) arquero.Reiniciar();
        estado = Estado.Esperando;
    }

    void MostrarMensaje(string texto, Color color)
    {
        MostrarMensaje(texto, color, 0f);
    }

    void MostrarMensaje(string texto, Color color, float tamano)
    {
        if (textoResultado != null)
        {
            textoResultado.text = texto;
            textoResultado.color = color;
            if (tamano > 0f) textoResultado.fontSize = Mathf.RoundToInt(tamano);
        }
        if (panelResultado != null) panelResultado.SetActive(true);
    }
}
