using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador de la ronda de tiros de castigo (arriba a la derecha).
/// Muestra cuantos tiros y cuantas atajadas van de la ronda (5 y 5 por defecto).
/// </summary>
public class IndicadorRonda : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Panel que se muestra/oculta")]
    public GameObject panel;
    [Tooltip("Los 5 circulos de los tiros (izquierda a derecha)")]
    public Image[] circulosTiros = new Image[5];
    [Tooltip("Los 5 circulos de las atajadas")]
    public Image[] circulosAtajadas = new Image[5];

    [Header("Colores")]
    [Tooltip("Sin resultado todavia")]
    public Color neutro = new Color(0.75f, 0.75f, 0.78f, 0.85f);
    [Tooltip("Acierto: gol (en tiros) o atajada (en atajadas)")]
    public Color acierto = new Color(0.25f, 0.85f, 0.35f, 1f);
    [Tooltip("Fallo: no fue gol (en tiros) o no atajo (en atajadas)")]
    public Color fallo = new Color(0.90f, 0.25f, 0.20f, 1f);

    [Header("Estado (solo lectura)")]
    public int tiros = 0;
    public int atajadas = 0;

    void Start()
    {
        // OJO: no ocultar el panel aqui (Start de un objeto inactivo se ejecuta al activarlo)
        Refrescar();
    }

    public void Mostrar(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
        Refrescar();
    }

    /// <summary>Cuantos tiros tiene la ronda (5 por defecto).</summary>
    public int TotalTiros
    {
        get { return (circulosTiros != null && circulosTiros.Length > 0) ? circulosTiros.Length : 5; }
    }

    /// <summary>True cuando ya se registraron todos los tiros de la ronda.</summary>
    public bool RondaCompleta
    {
        get { return tiros >= TotalTiros; }
    }

    /// <summary>Pone los 10 circulos en estado neutro (al empezar la ronda).</summary>
    public void Reiniciar()
    {
        tiros = 0;
        atajadas = 0;
        PintarTodos(circulosTiros, neutro);
        PintarTodos(circulosAtajadas, neutro);
    }

    /// <summary>Resultado del siguiente tiro: true = GOL (verde), false = fallado (rojo).</summary>
    public void RegistrarTiro(bool fueGol)
    {
        Pintar(circulosTiros, tiros, fueGol ? acierto : fallo);
        tiros = Mathf.Min(tiros + 1, circulosTiros != null ? circulosTiros.Length : 0);
    }

    /// <summary>Siguiente atajada: true = ATAJO (verde), false = no atajo (rojo).</summary>
    public void RegistrarAtajada(bool fueAtajada)
    {
        Pintar(circulosAtajadas, atajadas, fueAtajada ? acierto : fallo);
        atajadas = Mathf.Min(atajadas + 1, circulosAtajadas != null ? circulosAtajadas.Length : 0);
    }

    public void Refrescar()
    {
        // Solo se usa al arrancar: deja todo neutro si nunca se ha pintado nada
    }

    private void PintarTodos(Image[] circulos, Color color)
    {
        if (circulos == null) return;
        for (int i = 0; i < circulos.Length; i++) Pintar(circulos[i], color, false);
    }

    private void Pintar(Image[] circulos, int indice, Color color)
    {
        if (circulos == null || indice < 0 || indice >= circulos.Length) return;
        Pintar(circulos[indice], color, true);
    }

    /// <summary>
    /// Pinta un circulo. Si <paramref name="avisar"/> es true (cuando se registra un tiro o
    /// una atajada) el circulo hace su animacion de pulso y suena el "ding";
    /// al reiniciar la ronda se pintan los 10 en gris sin animacion ni sonido.
    /// </summary>
    private void Pintar(Image imagen, Color color, bool avisar)
    {
        if (imagen == null) return;
        imagen.color = color;
        if (!avisar) return;

        Animator anim = imagen.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Pulso");
        GestorAudio.Sonar("ding", 0.75f);
    }
}
