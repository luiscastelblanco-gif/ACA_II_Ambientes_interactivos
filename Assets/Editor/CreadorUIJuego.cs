using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea (o recrea) la interfaz de juego:
///   - Panel con la GUIA DE COMANDOS abajo a la derecha (siempre visible).
///   - Panel de AVISO al entrar a la cancha (E = iniciar ronda de tiros de castigo / Q = no).
///
/// Menu:  Penales  >  Crear UI de juego
/// </summary>
public static class CreadorUIJuego
{
    [MenuItem("Penales/Crear UI de juego")]
    public static void Crear()
    {
        // Limpieza de una version anterior
        GameObject viejo = GameObject.Find("UI_Juego");
        if (viejo != null) Object.DestroyImmediate(viejo);
        GameObject viejoAviso = GameObject.Find("Aviso_Cancha");
        if (viejoAviso != null) Object.DestroyImmediate(viejoAviso);

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // ---------------- Canvas ----------------
        GameObject canvasGO = new GameObject("UI_Juego", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ---------------- Guia de comandos (abajo derecha) ----------------
        RectTransform panelGuia = CrearPanel(canvas.transform, "Panel_Guia", new Color(0f, 0f, 0f, 0.55f),
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 26f), new Vector2(470f, 250f));
        Text textoGuia = CrearTexto(panelGuia, "Texto_Guia", fuente, 19, TextAnchor.UpperLeft, new Color(1f, 0.96f, 0.85f));
        GuiaComandos guia = panelGuia.gameObject.AddComponent<GuiaComandos>();
        guia.textoGuia = textoGuia;
        guia.Refrescar();

        // ---------------- Panel de aviso (centro, un poco arriba del borde inferior) ----------------
        RectTransform panelAviso = CrearPanel(canvas.transform, "Panel_Aviso", new Color(0f, 0f, 0f, 0.75f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(780f, 160f));
        Text textoAviso = CrearTexto(panelAviso, "Texto_Aviso", fuente, 26, TextAnchor.MiddleCenter, Color.white);
        panelAviso.gameObject.SetActive(false);

        // ---------------- Script que detecta la entrada a la cancha ----------------
        GameObject avisoGO = new GameObject("Aviso_Cancha");
        AvisoEntradaCancha aviso = avisoGO.AddComponent<AvisoEntradaCancha>();
        aviso.jugador = null;
        GameObject jugador = GameObject.Find("Jugador");
        if (jugador != null) aviso.jugador = jugador.transform;
        aviso.panelAviso = panelAviso.gameObject;
        aviso.textoAviso = textoAviso;
        aviso.guia = guia;

        // ---------------- Indicador de la ronda (arriba derecha): 5 + 5 circulos ----------------
        Sprite circulo = CrearSpriteCirculo();
        RectTransform panelRonda = CrearPanel(canvas.transform, "Panel_Ronda", new Color(0f, 0f, 0f, 0.6f),
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(420f, 156f));
        Text tituloRonda = CrearTextoArea(panelRonda, "Texto_Titulo", fuente, 19, TextAnchor.MiddleCenter,
            new Color(1f, 0.93f, 0.7f), new Vector2(0f, 0.62f), new Vector2(1f, 1f));
        tituloRonda.text = "RONDA DE TIROS DE CASTIGO";

        Text etiquetaTiros = CrearTextoArea(panelRonda, "Etiqueta_Tiros", fuente, 18, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, 0.31f), new Vector2(0.36f, 0.62f));
        etiquetaTiros.text = "Tiros";
        Image[] circulosTiros = CrearFilaCirculos(panelRonda, "Circulo_Tiro", circulo, 0.36f, 1f, 0.31f, 0.62f);

        Text etiquetaAtajadas = CrearTextoArea(panelRonda, "Etiqueta_Atajadas", fuente, 18, TextAnchor.MiddleLeft,
            Color.white, new Vector2(0f, 0f), new Vector2(0.36f, 0.31f));
        etiquetaAtajadas.text = "Atajadas";
        Image[] circulosAtajadas = CrearFilaCirculos(panelRonda, "Circulo_Atajada", circulo, 0.36f, 1f, 0f, 0.31f);

        IndicadorRonda indicador = panelRonda.gameObject.AddComponent<IndicadorRonda>();
        indicador.panel = panelRonda.gameObject;
        indicador.circulosTiros = circulosTiros;
        indicador.circulosAtajadas = circulosAtajadas;
        indicador.Reiniciar();
        panelRonda.gameObject.SetActive(false);

        aviso.indicador = indicador;

        // ---------------- Barra de fuerza del disparo (abajo centro) ----------------
        RectTransform panelFuerza = CrearPanel(canvas.transform, "Panel_Fuerza", new Color(0f, 0f, 0f, 0.65f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(420f, 30f));
        RectTransform relleno = CrearPanel(panelFuerza, "Relleno", new Color(0.35f, 0.9f, 0.35f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
        relleno.anchorMin = new Vector2(0f, 0f);
        relleno.anchorMax = new Vector2(0.02f, 1f);
        relleno.offsetMin = new Vector2(4f, 4f);
        relleno.offsetMax = new Vector2(-4f, -4f);
        BarraFuerza barra = canvasGO.AddComponent<BarraFuerza>();
        barra.panel = panelFuerza.gameObject;
        barra.relleno = relleno;
        GameObject jugadorGO = GameObject.Find("Jugador");
        if (jugadorGO != null)
        {
            barra.conductor = jugadorGO.GetComponent<ConductorPelota>();
            if (barra.conductor == null) Debug.LogWarning("[Penales] El Jugador no tiene ConductorPelota: la barra de fuerza no se llenara");
        }
        panelFuerza.gameObject.SetActive(false);

        // ---------------- Mensaje grande del resultado del tiro (centro) ----------------
        RectTransform panelResultado = CrearPanel(canvas.transform, "Panel_Resultado", new Color(0f, 0f, 0f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 240f));
        panelResultado.GetComponent<Image>().raycastTarget = false;
        Text textoResultado = CrearTexto(panelResultado, "Texto_Resultado", fuente, 64, TextAnchor.MiddleCenter, Color.white);
        textoResultado.fontStyle = FontStyle.Bold;
        textoResultado.horizontalOverflow = HorizontalWrapMode.Overflow;
        textoResultado.verticalOverflow = VerticalWrapMode.Overflow;
        panelResultado.gameObject.SetActive(false);

        // Pelota de la ronda (ojo: esta desactivada, hay que buscarla de forma robusta)
        Pelota pelota = null;
        Pelota[] candidatas = Resources.FindObjectsOfTypeAll<Pelota>();
        for (int i = 0; i < candidatas.Length; i++)
        {
            if (candidatas[i].gameObject.scene.IsValid() && candidatas[i].gameObject.scene.isLoaded)
            {
                pelota = candidatas[i];
                break;
            }
        }
        if (pelota != null) aviso.pelota = pelota;
        else Debug.LogWarning("[Penales] No encontre la Pelota (no aparecera en la ronda)");

        // Si ya existe el juez de tiro ('Penales > Crear arquero y juez de tiro'), se le
        // conectan las referencias de la UI recien creada (indicador y mensaje del resultado).
        JuezTiro juez = Object.FindFirstObjectByType<JuezTiro>();
        if (juez != null)
        {
            juez.indicador = indicador;
            juez.pelota = pelota;
            juez.panelResultado = panelResultado.gameObject;
            juez.textoResultado = textoResultado;
            aviso.juez = juez;                       // para poder repetir la ronda con R
            EditorUtility.SetDirty(juez);
        }

        // Porton que cierra la cancha durante la ronda
        GameObject porton = GameObject.Find("Cancha/Cerco/Porton");
        if (porton != null) aviso.porton = porton;
        else Debug.LogWarning("[Penales] No encontre 'Cancha/Cerco/Porton' (el cierre de la cancha no funcionara)");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        Debug.Log("[Penales] UI de juego creada: guia abajo a la derecha + aviso de entrada a la cancha" +
                  (jugador == null ? "  (OJO: no encontre el objeto 'Jugador', asignalo a mano)" : ""));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private static RectTransform CrearPanel(Transform padre, string nombre, Color color,
        Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 tamano)
    {
        GameObject go = new GameObject(nombre, typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    private static Text CrearTextoArea(Transform padre, string nombre, Font fuente, int tamano, TextAnchor alineacion,
        Color color, Vector2 anclaMin, Vector2 anclaMax)
    {
        Text t = CrearTexto(padre, nombre, fuente, tamano, alineacion, color);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.offsetMin = new Vector2(18f, 6f);
        rt.offsetMax = new Vector2(-14f, -6f);
        return t;
    }

    /// <summary>Fila de 5 circulos (imagenes) repartidos horizontalmente en el panel.</summary>
    private static Image[] CrearFilaCirculos(Transform padre, string prefijo, Sprite sprite,
        float x0, float x1, float y0, float y1)
    {
        int cantidad = 5;
        Image[] fila = new Image[cantidad];
        float ancho = (x1 - x0) / cantidad;
        for (int i = 0; i < cantidad; i++)
        {
            GameObject go = new GameObject(prefijo + "_" + (i + 1), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(padre, false);
            rt.anchorMin = new Vector2(x0 + i * ancho, y0);
            rt.anchorMax = new Vector2(x0 + (i + 1) * ancho, y1);
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;              // que no se deformen en ovalos
            img.color = new Color(0.75f, 0.75f, 0.78f, 0.85f);   // neutro
            img.raycastTarget = false;
            fila[i] = img;
        }
        return fila;
    }

    /// <summary>Genera un sprite circular blanco (se tinta desde Image.color).</summary>
    private static Sprite CrearSpriteCirculo()
    {
        const int TAM = 64;
        Texture2D tex = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false);
        tex.name = "CirculoUI";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[TAM * TAM];
        float radio = TAM * 0.5f - 1.5f;
        float cx = TAM * 0.5f - 0.5f;
        for (int y = 0; y < TAM; y++)
        {
            for (int x = 0; x < TAM; x++)
            {
                float dx = x - cx;
                float dy = y - cx;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alfa = Mathf.Clamp01(radio - d + 0.5f);      // borde suavizado
                px[y * TAM + x] = new Color(1f, 1f, 1f, alfa);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, TAM, TAM), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Text CrearTexto(Transform padre, string nombre, Font fuente, int tamano, TextAnchor alineacion, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(Text));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 12f);
        rt.offsetMax = new Vector2(-16f, -12f);
        Text t = go.GetComponent<Text>();
        t.font = fuente;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
