using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea (o recrea) lo necesario para la ronda de tiros:
///   - ARQUERO: cuerpo y cabeza con Rigidbody kinematico, sobre la linea del arco este.
///   - JUEZ_TIRO: el script que decide GOL / ATAJADA / FALLADO y pinta los circulos.
///
/// Menu:  Penales  >  Crear arquero y juez de tiro
/// Ejecutarlo DESPUES de 'Penales > Crear UI de juego' para que conecte el indicador
/// y el mensaje del resultado.
/// </summary>
public static class CreadorRonda
{
    private const float LineaArqueroX = 15.75f;   // justo delante de los postes (15.95)

    [MenuItem("Penales/Crear arquero y juez de tiro")]
    public static void Crear()
    {
        Material camiseta = MaterialNuevo("M_Arquero", new Color(0.95f, 0.75f, 0.15f, 1f));       // amarillo
        Material piel = MaterialNuevo("M_ArqueroPiel", new Color(0.86f, 0.69f, 0.56f, 1f));
        PhysicsMaterial fisica = FisicaNueva();

        // ---------------- Arquero ----------------
        GameObject viejo = GameObject.Find("Arquero");
        if (viejo != null) Object.DestroyImmediate(viejo);

        GameObject arqueroGO = new GameObject("Arquero");
        arqueroGO.transform.position = new Vector3(LineaArqueroX, 0f, 0f);

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(arqueroGO.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        cuerpo.transform.localScale = new Vector3(0.75f, 0.92f, 0.75f);
        cuerpo.GetComponent<MeshRenderer>().sharedMaterial = camiseta;
        CapsuleCollider colCuerpo = cuerpo.GetComponent<CapsuleCollider>();
        colCuerpo.sharedMaterial = fisica;

        GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabeza.name = "Cabeza";
        cabeza.transform.SetParent(arqueroGO.transform, false);
        cabeza.transform.localPosition = new Vector3(0f, 1.98f, 0f);
        cabeza.transform.localScale = Vector3.one * 0.36f;
        cabeza.GetComponent<MeshRenderer>().sharedMaterial = piel;
        Object.DestroyImmediate(cabeza.GetComponent<SphereCollider>());   // la cabeza no empuja

        Rigidbody rb = arqueroGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Arquero arq = arqueroGO.AddComponent<Arquero>();
        arq.mediaCancha = 1.25f;
        arq.velocidadAcomodo = 2.0f;
        arq.velocidadReaccion = 2.6f;
        arq.retardo = 0.35f;
        arq.errorMaximo = 0.5f;
        arq.lineaDeGolX = LineaArqueroX;
        arq.pelota = BuscarPelota();

        // ---------------- Juez de tiro ----------------
        GameObject viejoJuez = GameObject.Find("Juez_Tiro");
        if (viejoJuez != null) Object.DestroyImmediate(viejoJuez);

        GameObject juezGO = new GameObject("Juez_Tiro");
        JuezTiro juez = juezGO.AddComponent<JuezTiro>();
        juez.arquero = arq;
        juez.pelota = arq.pelota;
        juez.indicador = BuscarIndicador();

        Transform canvas = GameObject.Find("UI_Juego") != null ? GameObject.Find("UI_Juego").transform : null;
        Transform panelResultado = canvas != null ? canvas.Find("Panel_Resultado") : null;
        if (panelResultado != null)
        {
            juez.panelResultado = panelResultado.gameObject;
            Transform t = panelResultado.Find("Texto_Resultado");
            if (t != null) juez.textoResultado = t.GetComponent<Text>();
            panelResultado.gameObject.SetActive(false);
        }
        else Debug.LogWarning("[Penales] No encontre 'UI_Juego/Panel_Resultado': ejecuta antes 'Penales > Crear UI de juego'");

        // Conectar el juez al aviso de la cancha: asi al terminar los 5 tiros se puede
        // repetir la ronda con R (AvisoEntradaCancha llama a IniciarRonda).
        AvisoEntradaCancha aviso = Object.FindFirstObjectByType<AvisoEntradaCancha>();
        if (aviso != null) aviso.juez = juez;
        else Debug.LogWarning("[Penales] No encontre AvisoEntradaCancha: la ronda no se podra repetir con R");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = arqueroGO;
        Debug.Log("[Penales] Arquero y juez de tiro creados. Arquero en x=" + LineaArqueroX +
                  " | juez: pelota=" + (juez.pelota != null) + " arquero=" + (juez.arquero != null) +
                  " indicador=" + (juez.indicador != null) + " mensaje=" + (juez.panelResultado != null));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private static Material MaterialNuevo(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        string ruta = "Assets/Materials/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static PhysicsMaterial FisicaNueva()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Physics")) AssetDatabase.CreateFolder("Assets", "Physics");
        string ruta = "Assets/Physics/ArqueroFisica.asset";
        PhysicsMaterial p = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(ruta);
        if (p == null)
        {
            p = new PhysicsMaterial("ArqueroFisica");
            AssetDatabase.CreateAsset(p, ruta);
        }
        p.bounciness = 0f;
        p.bounceCombine = PhysicsMaterialCombine.Minimum;   // la pelota muere en el arquero
        p.dynamicFriction = 0.8f;
        p.staticFriction = 0.8f;
        EditorUtility.SetDirty(p);
        return p;
    }

    private static Pelota BuscarPelota()
    {
        Pelota[] todas = Resources.FindObjectsOfTypeAll<Pelota>();
        for (int i = 0; i < todas.Length; i++)
            if (todas[i].gameObject.scene.IsValid() && todas[i].gameObject.scene.isLoaded) return todas[i];
        Debug.LogWarning("[Penales] No encontre la Pelota");
        return null;
    }

    private static IndicadorRonda BuscarIndicador()
    {
        IndicadorRonda[] todos = Resources.FindObjectsOfTypeAll<IndicadorRonda>();
        for (int i = 0; i < todos.Length; i++)
            if (todos[i].gameObject.scene.IsValid() && todos[i].gameObject.scene.isLoaded) return todos[i];
        Debug.LogWarning("[Penales] No encontre el IndicadorRonda: ejecuta antes 'Penales > Crear UI de juego'");
        return null;
    }
}
