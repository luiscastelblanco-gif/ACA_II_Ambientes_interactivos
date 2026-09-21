using UnityEngine;

/// <summary>
/// Sonidos del juego. Los clips son WAV generados por codigo (Assets/Audio,
/// menu "Penales > Generar sonidos (WAV)").
///
/// Se usa desde cualquier script asi:   GestorAudio.Sonar("patada");
/// Si el objeto de audio no existe, no pasa nada (nunca rompe el juego).
/// </summary>
public class GestorAudio : MonoBehaviour
{
    public static GestorAudio Instancia;

    [Header("Clips (Assets/Audio)")]
    public AudioClip patada;
    public AudioClip gol;
    public AudioClip atajada;
    public AudioClip fallado;
    public AudioClip silbato;
    public AudioClip porton;
    public AudioClip clic;
    public AudioClip ding;
    public AudioClip bote;
    public AudioClip carga;

    [Header("Volumen")]
    [Range(0f, 1f)]
    public float volumenGeneral = 0.85f;

    [Tooltip("Cuantos sonidos 3D pueden sonar a la vez (botes del balon, porton)")]
    public int voces3D = 6;

    private AudioSource[] fuentes;
    private int siguiente = 0;
    private AudioSource[] fuentes3D;
    private int siguiente3D = 0;

    void Awake()
    {
        Instancia = this;

        fuentes = GetComponents<AudioSource>();
        if (fuentes == null || fuentes.Length == 0)
        {
            fuentes = new AudioSource[4];
            for (int i = 0; i < fuentes.Length; i++)
            {
                fuentes[i] = gameObject.AddComponent<AudioSource>();
                fuentes[i].playOnAwake = false;
                fuentes[i].spatialBlend = 0f;
            }
        }

        // Voces 3D reutilizables (botes del balon, porton).
        // Antes se usaba AudioSource.PlayClipAtPoint, que crea un GameObject con su
        // AudioSource por cada sonido y despues lo destruye (medido: 200 sonidos = 200
        // objetos creados). Ahora son fuentes fijas que se van reutilizando en anillo.
        fuentes3D = new AudioSource[Mathf.Max(1, voces3D)];
        for (int i = 0; i < fuentes3D.Length; i++)
        {
            GameObject voz = new GameObject("Voz3D_" + i);
            voz.transform.SetParent(transform, false);
            AudioSource s = voz.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 1f;                          // 3D
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = 25f;
            s.volume = 1f;
            fuentes3D[i] = s;
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private AudioClip Buscar(string nombre)
    {
        switch (nombre)
        {
            case "patada": return patada;
            case "gol": return gol;
            case "atajada": return atajada;
            case "fallado": return fallado;
            case "silbato": return silbato;
            case "porton": return porton;
            case "clic": return clic;
            case "ding": return ding;
            case "bote": return bote;
            case "carga": return carga;
        }
        return null;
    }

    /// <summary>Sonido 2D: interfaz, silbato, resultados.</summary>
    public static void Sonar(string nombre, float volumen = 1f)
    {
        if (Instancia != null) Instancia.Reproducir(nombre, volumen, Vector3.zero, false);
    }

    /// <summary>Sonido en una posicion del mundo: botes del balon, porton.</summary>
    public static void SonarEn(string nombre, Vector3 posicion, float volumen = 1f)
    {
        if (Instancia != null) Instancia.Reproducir(nombre, volumen, posicion, true);
    }

    private void Reproducir(string nombre, float volumen, Vector3 posicion, bool espacial)
    {
        AudioClip clip = Buscar(nombre);
        if (clip == null) return;

        float v = Mathf.Clamp01(volumen * volumenGeneral);

        if (espacial)
        {
            if (fuentes3D == null || fuentes3D.Length == 0) return;

            AudioSource voz = fuentes3D[siguiente3D];
            siguiente3D = (siguiente3D + 1) % fuentes3D.Length;
            voz.transform.position = posicion;
            voz.pitch = Random.Range(0.96f, 1.04f);
            voz.PlayOneShot(clip, v);
            return;
        }

        if (fuentes == null || fuentes.Length == 0) return;
        AudioSource f = fuentes[siguiente];
        siguiente = (siguiente + 1) % fuentes.Length;
        f.pitch = Random.Range(0.96f, 1.04f);                 // leve variacion: no suena identico siempre
        f.PlayOneShot(clip, v);
    }
}
