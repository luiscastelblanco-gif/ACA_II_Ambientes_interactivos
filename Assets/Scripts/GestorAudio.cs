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

    private AudioSource[] fuentes;
    private int siguiente = 0;

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
            AudioSource.PlayClipAtPoint(clip, posicion, v);   // fuente temporal, se destruye sola
            return;
        }

        if (fuentes == null || fuentes.Length == 0) return;
        AudioSource f = fuentes[siguiente];
        siguiente = (siguiente + 1) % fuentes.Length;
        f.pitch = Random.Range(0.96f, 1.04f);                 // leve variacion: no suena identico siempre
        f.PlayOneShot(clip, v);
    }
}
