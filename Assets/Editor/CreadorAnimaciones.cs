using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea las animaciones del juego (clips .anim + controladores) y las conecta a los
/// elementos interactivos:
///   - Porton de la entrada  -> se hunde para abrir y sube para cerrar
///   - Mensaje de resultado  -> aparece con un golpe de escala (pop)
///   - Panel de aviso        -> entrada suave cuando aparece la pregunta
///   - Circulos de la ronda  -> pulsan cuando se pintan
///   - Arquero               -> se inclina cuando se lanza al tiro
///
/// Menu:  Penales > Crear animaciones
/// </summary>
public static class CreadorAnimaciones
{
    private const string Carpeta = "Assets/Animations";

    [MenuItem("Penales/Crear animaciones")]
    public static void Crear()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.Refresh();
        }

        // ---------------- 1) PORTON: se hunde para abrir, sube para cerrar ----------------
        GameObject cerco = GameObject.Find("Cancha/Cerco");
        Transform porton = cerco != null ? cerco.transform.Find("Porton") : null;

        // El porton va en la entrada sur de la cancha (x=0, z=12): hundido = abierto.
        // Se fija ANTES de crear los clips, para que la animacion guarde la x y la z reales.
        Vector3 basePorton = new Vector3(0f, -2.6f, 12f);
        if (porton != null) porton.localPosition = basePorton;

        AnimationClip abrir = ClipPosY("Porton_Abrir", basePorton, 0f, -2.6f, 0.9f);
        AnimationClip cerrar = ClipPosY("Porton_Cerrar", basePorton, -2.6f, 0f, 0.9f);
        AnimatorController cPorton = DosEstados("Porton", abrir, "Abierto", cerrar, "Cerrado", "Abrir", "Cerrar", true);

        if (porton != null)
        {
            PonAnimator(porton.gameObject, cPorton, false);
            porton.gameObject.SetActive(true);
            // El porton traia un colisionador de tamano 0: le ponemos el de la reja (5 x 2.5 m)
            BoxCollider bc = porton.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bc.center = new Vector3(0f, 1.25f, 0f);
                bc.size = new Vector3(5f, 2.5f, 0.2f);
            }
        }

        // ---------------- 2) MENSAJE DE RESULTADO: pop al aparecer ----------------
        AnimationClip pop = ClipEscala("Resultado_Pop", 0.72f, 1.12f, 0.42f, true);
        AnimatorController cResultado = UnEstado("Resultado", "Aparece", pop);

        // ---------------- 3) PANEL DE AVISO: entrada suave ----------------
        AnimationClip entrar = ClipEscala("Aviso_Entrar", 0.90f, 1.04f, 0.30f, true);
        AnimatorController cAviso = UnEstado("Aviso", "Entra", entrar);

        // ---------------- 4) CIRCULOS DE LA RONDA: pulso al pintarse ----------------
        AnimationClip pulso = ClipEscala("Circulo_Pulso", 0.80f, 1.45f, 0.35f, true);
        AnimationClip quieto = ClipEscalaPlano("Circulo_Quieto", true);
        AnimatorController cCirculo = TriggerConVuelta("Circulo", quieto, pulso, "Pulso");

        // ---------------- 5) ARQUERO: se inclina al lanzarse ----------------
        AnimationClip lanzaIzq = ClipArquero("Arquero_LanzaIzq", 30f, 0.55f);
        AnimationClip lanzaDer = ClipArquero("Arquero_LanzaDer", -30f, 0.55f);
        AnimationClip arqQuieto = ClipArqueroQuieto();
        AnimatorController cArquero = TresEstados("ArqueroCuerpo", arqQuieto,
            lanzaIzq, "LanzaIzquierda", "Izquierda",
            lanzaDer, "LanzaDerecha", "Derecha");

        // ---------------- 6) JUGADOR: Blend Tree de locomocion ----------------
        // Los clips van EN BUCLE y se mezclan por el parametro float "Desplazamiento"
        // (0 = quieto, 0.45 = caminando, 1 = corriendo). El jugador lo alimenta con su
        // velocidad real desde JugadorTerceraPersona.
        GameObject jugador = GameObject.Find("Jugador");
        Transform cuerpoJugador = jugador != null ? jugador.transform.Find("Cuerpo") : null;
        AnimatorController cJugador = null;
        if (cuerpoJugador != null)
        {
            Vector3 baseCuerpo = cuerpoJugador.localPosition;
            AnimationClip jQuieto = ClipJugador("Jugador_Quieto", baseCuerpo, 0.80f, 0.000f, 0f, 0f);
            AnimationClip jCaminar = ClipJugador("Jugador_Caminar", baseCuerpo, 0.75f, 0.045f, 3f, 2.5f);
            AnimationClip jCorrer = ClipJugador("Jugador_Correr", baseCuerpo, 0.45f, 0.100f, 8f, 4.5f);
            cJugador = BlendTreeLocomocion("JugadorMovimiento", jQuieto, jCaminar, jCorrer);
        }

        // ---------------- Conectar los Animator en la escena ----------------
        GameObject ui = GameObject.Find("UI_Juego");
        int conectados = 0;
        if (ui != null)
        {
            Transform t = ui.transform.Find("Panel_Resultado");
            if (t != null) { PonAnimator(t.gameObject, cResultado, true); conectados++; }

            t = ui.transform.Find("Panel_Aviso");
            if (t != null) { PonAnimator(t.gameObject, cAviso, true); conectados++; }

            t = ui.transform.Find("Panel_Ronda");
            if (t != null)
            {
                for (int i = 0; i < t.childCount; i++)
                {
                    Transform hijo = t.GetChild(i);
                    if (hijo.name.StartsWith("Circulo_Tiro_") || hijo.name.StartsWith("Circulo_Atajada_"))
                    {
                        PonAnimator(hijo.gameObject, cCirculo, true);
                        conectados++;
                    }
                }
            }
        }

        GameObject arq = GameObject.Find("Arquero");
        if (arq != null) { PonAnimator(arq, cArquero, false); conectados++; }

        if (jugador != null && cJugador != null) { PonAnimator(jugador, cJugador, false); conectados++; }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Penales] Animaciones creadas y conectadas (" + conectados + " objetos): " +
                  "porton, mensaje de resultado, panel de aviso, circulos de la ronda y arquero");
    }

    // ------------------------------------------------------------------
    // Clips
    // ------------------------------------------------------------------
    /// <summary>
    /// Crea el asset .anim vacio y devuelve el clip YA IMPORTADO.
    /// OJO: AnimationUtility.SetEditorCurve necesita trabajar sobre el clip del asset
    /// (si se le pasa el clip recien creado, en la interfaz la curva se pierde).
    /// </summary>
    private static AnimationClip NuevoAsset(string nombre)
    {
        string ruta = Carpeta + "/" + nombre + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta) != null) AssetDatabase.DeleteAsset(ruta);

        AnimationClip c = new AnimationClip();
        c.name = nombre;
        c.frameRate = 60f;
        c.wrapMode = WrapMode.Once;
        AssetDatabase.CreateAsset(c, ruta);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
    }

    private static AnimationCurve Suave(params Keyframe[] claves)
    {
        AnimationCurve c = new AnimationCurve(claves);
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    /// <summary>
    /// Pone UNA sola curva en el clip. Se usa AnimationUtility y NO clip.SetCurve por dos motivos:
    ///   1) SetCurve rellena las OTRAS componentes del mismo vector con 0: animar
    ///      "m_LocalPosition.y" del porton tambien grababa x=0 y z=0, y como el porton esta
    ///      en z=12 se iba al medio de la cancha.
    ///   2) Para la interfaz la curva necesita el classID de RectTransform (224).
    /// </summary>
    private static void Curva(AnimationClip c, string ruta, System.Type tipo, string propiedad, AnimationCurve curva)
    {
        AnimationUtility.SetEditorCurve(c, EditorCurveBinding.FloatCurve(ruta, tipo, propiedad), curva);
    }

    private static void Cerrar(AnimationClip c, bool bucle = false)
    {
        // OJO: el bucle de un clip se controla con AnimationClipSettings, NO con wrapMode.
        // Los clips de los interactivos NO van en bucle (si el porton lo hiciera repetiria el
        // cierre y el jugador quedaria encerrado). Los de locomocion del jugador SI van en bucle:
        // son los que alimentan el Blend Tree.
        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(c);
        ajustes.loopTime = bucle;
        AnimationUtility.SetAnimationClipSettings(c, ajustes);
        EditorUtility.SetDirty(c);
    }

    private static AnimationCurve Fijo(float valor)
    {
        return new AnimationCurve(new Keyframe(0f, valor));
    }

    /// <summary>
    /// Posicion local del porton (el que sube y baja).
    /// OJO: el Animator escribe SIEMPRE el vector completo, aunque el clip solo tenga curva en
    /// una componente. Si solo se anima la Y, la X y la Z se escriben como 0 y el porton
    /// (que vive en z=12) aparece en el MEDIO de la cancha. Por eso se guardan las 3
    /// componentes: la X y la Z fijas en su valor real y la Y animada.
    /// </summary>
    private static AnimationClip ClipPosY(string nombre, Vector3 origen, float desde, float hasta, float dur)
    {
        AnimationClip c = NuevoAsset(nombre);
        Curva(c, "", typeof(Transform), "m_LocalPosition.x", Fijo(origen.x));
        Curva(c, "", typeof(Transform), "m_LocalPosition.y",
              Suave(new Keyframe(0f, desde), new Keyframe(dur * 0.70f, hasta), new Keyframe(dur, hasta)));
        Curva(c, "", typeof(Transform), "m_LocalPosition.z", Fijo(origen.z));
        Cerrar(c);
        return c;
    }

    /// <summary>Escala con "pop": arranca chico, pasa por el pico y termina en 1.</summary>
    private static AnimationClip ClipEscala(string nombre, float desde, float pico, float dur, bool esUI)
    {
        AnimationClip c = NuevoAsset(nombre);
        AnimationCurve curva = Suave(new Keyframe(0f, desde), new Keyframe(dur * 0.45f, pico), new Keyframe(dur, 1f));
        Escala(c, esUI, curva);
        return c;
    }

    /// <summary>Escala fija en 1 (estado de reposo de los circulos).</summary>
    private static AnimationClip ClipEscalaPlano(string nombre, bool esUI)
    {
        AnimationClip c = NuevoAsset(nombre);
        Escala(c, esUI, new AnimationCurve(new Keyframe(0f, 1f)));
        return c;
    }

    private static void Escala(AnimationClip c, bool esUI, AnimationCurve ejeXY)
    {
        System.Type t = esUI ? typeof(RectTransform) : typeof(Transform);
        Curva(c, "", t, "m_LocalScale.x", ejeXY);
        Curva(c, "", t, "m_LocalScale.y", ejeXY);
        Curva(c, "", t, "m_LocalScale.z", new AnimationCurve(new Keyframe(0f, 1f)));
        Cerrar(c);
    }

    /// <summary>
    /// Inclinacion del arquero: hijos "Cuerpo" y "Cabeza".
    /// Se guardan las 3 componentes de cada vector (las que no cambian, fijas): el Animator
    /// escribe el vector completo y si no, pondria 0 en x/z y hundiria el cuerpo o la cabeza.
    /// </summary>
    private static AnimationClip ClipArquero(string nombre, float grados, float dur)
    {
        AnimationClip c = NuevoAsset(nombre);
        AnimationCurve inclinacion = Suave(new Keyframe(0f, 0f),
                                           new Keyframe(dur * 0.30f, grados),
                                           new Keyframe(dur, grados * 0.35f));
        AnimationCurve baja = Suave(new Keyframe(0f, 0.92f),
                                    new Keyframe(dur * 0.30f, 0.70f),
                                    new Keyframe(dur, 0.80f));
        // Cuerpo
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.x", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.y", baja);
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.z", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.x", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.y", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.z", inclinacion);
        // Cabeza (solo se inclina; se queda en su sitio)
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.x", Fijo(0f));
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.y", Fijo(0f));
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.z", inclinacion);
        Cerrar(c);
        return c;
    }

    /// <summary>Pose de reposo del arquero (derecho y a su altura normal).</summary>
    private static AnimationClip ClipArqueroQuieto()
    {
        AnimationClip c = NuevoAsset("Arquero_Quieto");
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.x", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.y", Fijo(0.92f));
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.z", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.x", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.y", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.z", Fijo(0f));
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.x", Fijo(0f));
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.y", Fijo(0f));
        Curva(c, "Cabeza", typeof(Transform), "localEulerAnglesRaw.z", Fijo(0f));
        Cerrar(c);
        return c;
    }

    /// <summary>
    /// Clip de locomocion del jugador: mueve el hijo "Cuerpo" (arriba/abajo) y lo inclina.
    /// Va EN BUCLE porque alimenta el Blend Tree y se mezcla con los otros dos clips.
    /// Se guardan las 3 componentes de cada vector para que el Animator no ponga 0 donde
    /// no debe (la misma leccion que con el porton).
    /// </summary>
    private static AnimationClip ClipJugador(string nombre, Vector3 baseCuerpo, float dur,
                                             float amplitudY, float inclinacionX, float balanceoZ)
    {
        AnimationClip c = NuevoAsset(nombre);
        AnimationCurve sube = Suave(new Keyframe(0f, baseCuerpo.y),
                                    new Keyframe(dur * 0.25f, baseCuerpo.y + amplitudY),
                                    new Keyframe(dur * 0.50f, baseCuerpo.y),
                                    new Keyframe(dur * 0.75f, baseCuerpo.y + amplitudY),
                                    new Keyframe(dur, baseCuerpo.y));
        AnimationCurve balanceo = Suave(new Keyframe(0f, 0f),
                                        new Keyframe(dur * 0.25f, balanceoZ),
                                        new Keyframe(dur * 0.50f, 0f),
                                        new Keyframe(dur * 0.75f, -balanceoZ),
                                        new Keyframe(dur, 0f));
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.x", Fijo(baseCuerpo.x));
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.y", sube);
        Curva(c, "Cuerpo", typeof(Transform), "m_LocalPosition.z", Fijo(baseCuerpo.z));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.x", Fijo(inclinacionX));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.y", Fijo(0f));
        Curva(c, "Cuerpo", typeof(Transform), "localEulerAnglesRaw.z", balanceo);
        Cerrar(c, true);                     // EN BUCLE: es un clip de locomocion
        return c;
    }

    /// <summary>
    /// Blend Tree 1D del jugador: mezcla quieto / caminar / correr segun el parametro
    /// float "Desplazamiento" (0 = quieto, 0.45 = caminando, 1 = corriendo).
    /// Un Blend Tree sirve para esto: una sola animacion continua en vez de saltar
    /// entre estados, y el umbral lo maneja la velocidad real del jugador.
    /// </summary>
    private static AnimatorController BlendTreeLocomocion(string nombre, AnimationClip quieto,
                                                          AnimationClip caminar, AnimationClip correr)
    {
        AnimatorController ctrl = NuevoControl(nombre);
        ctrl.AddParameter("Desplazamiento", AnimatorControllerParameterType.Float);

        BlendTree arbol = new BlendTree();
        arbol.name = "Locomocion";
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Desplazamiento";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(quieto, 0f);
        arbol.AddChild(caminar, 0.45f);
        arbol.AddChild(correr, 1f);
        AssetDatabase.AddObjectToAsset(arbol, ctrl);          // queda guardado dentro del .controller

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState st = Estado(sm, "Locomocion", arbol);
        sm.defaultState = st;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // ------------------------------------------------------------------
    // Controladores de animacion
    // ------------------------------------------------------------------
    private static AnimatorController NuevoControl(string nombre)
    {
        string ruta = Carpeta + "/" + nombre + ".controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) != null) AssetDatabase.DeleteAsset(ruta);
        return AnimatorController.CreateAnimatorControllerAtPath(ruta);
    }

    /// <summary>
    /// Crea un estado con "Write Defaults" APAGADO.
    /// OJO: con Write Defaults encendido el Animator escribe el valor por defecto (0) en las
    /// propiedades que el clip NO anima. Como el clip del porton solo anima la Y, el Animator
    /// ponia x=0 y z=0 y el porton (que vive en z=12) aparecia en el MEDIO de la cancha.
    /// </summary>
    private static AnimatorState Estado(AnimatorStateMachine sm, string nombre, Motion movimiento)
    {
        AnimatorState st = sm.AddState(nombre);
        st.motion = movimiento;
        st.writeDefaultValues = false;
        return st;
    }

    /// <summary>Un solo estado: se reproduce cada vez que el objeto se activa (paneles y mensaje).</summary>
    private static AnimatorController UnEstado(string nombre, string estado, AnimationClip clip)
    {
        AnimatorController ctrl = NuevoControl(nombre);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        sm.defaultState = Estado(sm, estado, clip);
        return ctrl;
    }

    /// <summary>Dos estados, uno por trigger (porton: Abrir / Cerrar).</summary>
    private static AnimatorController DosEstados(string nombre, AnimationClip a, string nomA,
                                                AnimationClip b, string nomB,
                                                string trigA, string trigB, bool aInicial)
    {
        AnimatorController ctrl = NuevoControl(nombre);
        ctrl.AddParameter(trigA, AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter(trigB, AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sA = Estado(sm, nomA, a);
        AnimatorState sB = Estado(sm, nomB, b);
        sm.defaultState = aInicial ? sA : sB;

        AnimatorStateTransition tAB = sA.AddTransition(sB);
        tAB.AddCondition(AnimatorConditionMode.If, 0f, trigB);
        tAB.hasExitTime = true; tAB.exitTime = 1f; tAB.duration = 0.05f; tAB.hasFixedDuration = true;

        AnimatorStateTransition tBA = sB.AddTransition(sA);
        tBA.AddCondition(AnimatorConditionMode.If, 0f, trigA);
        tBA.hasExitTime = true; tBA.exitTime = 1f; tBA.duration = 0.05f; tBA.hasFixedDuration = true;

        return ctrl;
    }

    /// <summary>Reposo + accion que se dispara con un trigger y vuelve sola (circulos).</summary>
    private static AnimatorController TriggerConVuelta(string nombre, AnimationClip quieto, AnimationClip accion, string trigger)
    {
        AnimatorController ctrl = NuevoControl(nombre);
        ctrl.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sQ = Estado(sm, "Quieto", quieto);
        AnimatorState sA = Estado(sm, "Accion", accion);
        sm.defaultState = sQ;

        AnimatorStateTransition t = sQ.AddTransition(sA);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.hasExitTime = false; t.duration = 0.05f; t.hasFixedDuration = true;

        AnimatorStateTransition v = sA.AddTransition(sQ);
        v.hasExitTime = true; v.exitTime = 1f; v.duration = 0.05f; v.hasFixedDuration = true;

        return ctrl;
    }

    /// <summary>Reposo + dos acciones (arquero: se lanza a la izquierda o a la derecha).</summary>
    private static AnimatorController TresEstados(string nombre, AnimationClip quieto,
        AnimationClip accA, string nomA, string trigA,
        AnimationClip accB, string nomB, string trigB)
    {
        AnimatorController ctrl = NuevoControl(nombre);
        ctrl.AddParameter(trigA, AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter(trigB, AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sQ = Estado(sm, "Quieto", quieto);
        AnimatorState sA = Estado(sm, nomA, accA);
        AnimatorState sB = Estado(sm, nomB, accB);
        sm.defaultState = sQ;

        AnimatorStateTransition tA = sQ.AddTransition(sA);
        tA.AddCondition(AnimatorConditionMode.If, 0f, trigA);
        tA.hasExitTime = false; tA.duration = 0.05f; tA.hasFixedDuration = true;

        AnimatorStateTransition tB = sQ.AddTransition(sB);
        tB.AddCondition(AnimatorConditionMode.If, 0f, trigB);
        tB.hasExitTime = false; tB.duration = 0.05f; tB.hasFixedDuration = true;

        AnimatorStateTransition vA = sA.AddTransition(sQ);
        vA.hasExitTime = true; vA.exitTime = 1f; vA.duration = 0.10f; vA.hasFixedDuration = true;

        AnimatorStateTransition vB = sB.AddTransition(sQ);
        vB.hasExitTime = true; vB.exitTime = 1f; vB.duration = 0.10f; vB.hasFixedDuration = true;

        return ctrl;
    }

    /// <summary>Pone (o reutiliza) el Animator con su controlador.</summary>
    private static Animator PonAnimator(GameObject go, AnimatorController ctrl, bool esUI)
    {
        Animator a = go.GetComponent<Animator>();
        if (a == null) a = go.AddComponent<Animator>();
        a.runtimeAnimatorController = ctrl;
        a.applyRootMotion = false;
        // La interfaz no se puede "cullear": tiene que animarse aunque el canvas no se vea
        a.cullingMode = esUI ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;
        return a;
    }
}
