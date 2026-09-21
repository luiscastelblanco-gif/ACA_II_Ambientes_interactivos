using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de fuerza del disparo: aparece abajo al mantener el clic izquierdo y se
/// llena de verde a rojo segun la carga (<see cref="ConductorPelota.carga"/>).
/// Si el jugador esta quieto y no carga, la barra queda oculta.
///
/// OJO: este script va en un objeto SIEMPRE ACTIVO (el canvas), nunca en el propio
/// panel de la barra: un objeto inactivo no ejecuta Update y la barra no apareceria.
/// </summary>
public class BarraFuerza : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("De donde sale la carga del disparo")]
    public ConductorPelota conductor;
    [Tooltip("Panel que se muestra/oculta")]
    public GameObject panel;
    [Tooltip("Rectangulo que se estira con la carga (de 0 a 1)")]
    public RectTransform relleno;

    [Header("Colores")]
    public Color colorBaja = new Color(0.35f, 0.9f, 0.35f, 1f);
    public Color colorAlta = new Color(0.95f, 0.35f, 0.20f, 1f);

    private Image imgRelleno;

    void Awake()
    {
        if (relleno != null) imgRelleno = relleno.GetComponent<Image>();
    }

    void Update()
    {
        bool cargando = conductor != null && conductor.cargando;
        if (panel != null && panel.activeSelf != cargando) panel.SetActive(cargando);
        if (!cargando || relleno == null) return;

        float c = Mathf.Clamp01(conductor.carga);
        relleno.anchorMin = new Vector2(0f, 0f);
        relleno.anchorMax = new Vector2(Mathf.Max(0.02f, c), 1f);
        relleno.offsetMin = new Vector2(4f, 4f);
        relleno.offsetMax = new Vector2(-4f, -4f);

        if (imgRelleno != null) imgRelleno.color = Color.Lerp(colorBaja, colorAlta, c);
    }
}
