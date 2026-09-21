using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea el objeto de audio del juego ("Audio_Juego") con el GestorAudio y le conecta
/// los WAV generados en Assets/Audio.
///
/// Menu:  Penales > Crear audio del juego
/// </summary>
public static class CreadorAudio
{
    [MenuItem("Penales/Crear audio del juego")]
    public static void Crear()
    {
        GameObject viejo = GameObject.Find("Audio_Juego");
        if (viejo != null) Object.DestroyImmediate(viejo);

        GameObject go = new GameObject("Audio_Juego");
        GestorAudio g = go.AddComponent<GestorAudio>();

        g.patada = Cargar("sfx_patada");
        g.gol = Cargar("sfx_gol");
        g.atajada = Cargar("sfx_atajada");
        g.fallado = Cargar("sfx_fallado");
        g.silbato = Cargar("sfx_silbato");
        g.porton = Cargar("sfx_porton");
        g.clic = Cargar("sfx_clic");
        g.ding = Cargar("sfx_ding");
        g.bote = Cargar("sfx_bote");
        g.carga = Cargar("sfx_carga");

        // Fuentes listas para los sonidos 2D (el GestorAudio las reutiliza)
        for (int i = 0; i < 3; i++)
        {
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 1f;
        }

        int faltan = 0;
        if (g.patada == null) faltan++;
        if (g.gol == null) faltan++;
        if (g.silbato == null) faltan++;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = go;

        if (faltan > 0)
            Debug.LogWarning("[Penales] Audio creado, pero faltan WAV: ejecuta antes 'Penales > Generar sonidos (WAV)'");
        else
            Debug.Log("[Penales] Audio del juego creado y conectado (10 sonidos + 3 fuentes)");
    }

    private static AudioClip Cargar(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombre + ".wav");
    }
}
