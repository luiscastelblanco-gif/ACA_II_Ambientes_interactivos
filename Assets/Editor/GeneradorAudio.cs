using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera los sonidos del juego como archivos WAV en Assets/Audio, sintetizados por codigo.
/// Asi no hay que descargar nada ni depender de licencias externas.
///
/// Menu:  Penales > Generar sonidos (WAV)
/// </summary>
public static class GeneradorAudio
{
    private const int SR = 44100;          // frecuencia de muestreo
    private const string Carpeta = "Assets/Audio";

    [MenuItem("Penales/Generar sonidos (WAV)")]
    public static void Generar()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets", "Audio");

        Guardar("sfx_patada", Patada());
        Guardar("sfx_gol", Gol());
        Guardar("sfx_atajada", Atajada());
        Guardar("sfx_fallado", Fallado());
        Guardar("sfx_silbato", Silbato());
        Guardar("sfx_porton", Porton());
        Guardar("sfx_clic", Clic());
        Guardar("sfx_ding", Ding());
        Guardar("sfx_bote", Bote());
        Guardar("sfx_carga", Carga());

        AssetDatabase.Refresh();
        Debug.Log("[Penales] Sonidos generados en " + Carpeta + " (10 archivos WAV)");
    }

    // ------------------------------------------------------------------
    // Utilidades de sintesis
    // ------------------------------------------------------------------
    private static float[] Nuevo(float segundos)
    {
        return new float[Mathf.Max(1, (int)(segundos * SR))];
    }

    /// <summary>Envolvente ataque/caida (0..1).</summary>
    private static float Envolvente(float t, float dur, float ataque, float caida)
    {
        float a = ataque > 0f ? Mathf.Clamp01(t / ataque) : 1f;
        float d = caida > 0f ? Mathf.Clamp01((dur - t) / caida) : 1f;
        return a * d;
    }

    /// <summary>Suma una onda senoidal (con barrido de frecuencia de freqIni a freqFin).</summary>
    private static void Seno(float[] destino, float inicio, float dur, float freqIni, float freqFin,
                             float volumen, float ataque, float caida, float armonico2 = 0f)
    {
        int i0 = (int)(inicio * SR);
        int n = (int)(dur * SR);
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            int idx = i0 + i;
            if (idx < 0) continue;
            if (idx >= destino.Length) break;
            float t = (float)i / SR;
            float f = Mathf.Lerp(freqIni, freqFin, t / Mathf.Max(0.0001f, dur));
            fase += 2f * Mathf.PI * f / SR;
            float env = Envolvente(t, dur, ataque, caida);
            destino[idx] += Mathf.Sin(fase) * volumen * env;
            if (armonico2 > 0f) destino[idx] += Mathf.Sin(fase * 2f) * volumen * armonico2 * env;
        }
    }

    /// <summary>Suma ruido blanco filtrado (filtro paso bajo simple: filtro 0..1).</summary>
    private static void Ruido(float[] destino, float inicio, float dur, float volumen,
                              float ataque, float caida, float filtro)
    {
        System.Random rnd = new System.Random(20260915);
        int i0 = (int)(inicio * SR);
        int n = (int)(dur * SR);
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            int idx = i0 + i;
            if (idx < 0) continue;
            if (idx >= destino.Length) break;
            float t = (float)i / SR;
            float blanco = (float)(rnd.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, blanco, Mathf.Clamp01(filtro));
            destino[idx] += prev * volumen * Envolvente(t, dur, ataque, caida);
        }
    }

    private static float[] Normalizar(float[] s, float pico = 0.92f)
    {
        float max = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float a = Mathf.Abs(s[i]);
            if (a > max) max = a;
        }
        if (max > 0.0001f)
        {
            float k = pico / max;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
        }
        return s;
    }

    /// <summary>Guarda un WAV mono 16 bits.</summary>
    private static void Guardar(string nombre, float[] muestras)
    {
        Normalizar(muestras);
        string ruta = Path.Combine(Carpeta, nombre + ".wav");
        using (FileStream fs = new FileStream(ruta, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int datos = muestras.Length * 2;
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + datos);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);        // PCM
            bw.Write((short)1);        // mono
            bw.Write(SR);
            bw.Write(SR * 2);
            bw.Write((short)2);        // block align
            bw.Write((short)16);       // bits
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(datos);
            for (int i = 0; i < muestras.Length; i++)
            {
                short v = (short)(Mathf.Clamp(muestras[i], -1f, 1f) * 32000f);
                bw.Write(v);
            }
        }
    }

    // ------------------------------------------------------------------
    // Los sonidos (uno por interaccion)
    // ------------------------------------------------------------------
    /// <summary>Patada al balon: golpe seco grave + chasquido de la cuerda.</summary>
    private static float[] Patada()
    {
        float[] s = Nuevo(0.35f);
        Seno(s, 0f, 0.18f, 190f, 55f, 0.95f, 0.002f, 0.16f, 0.35f);
        Ruido(s, 0f, 0.06f, 0.55f, 0.001f, 0.055f, 0.55f);
        Ruido(s, 0.012f, 0.26f, 0.10f, 0.005f, 0.25f, 0.10f);
        return s;
    }

    /// <summary>Gol: fanfarria de tres notas ascendentes + murmullo de la gente.</summary>
    private static float[] Gol()
    {
        float[] s = Nuevo(1.35f);
        Seno(s, 0.00f, 0.26f, 523f, 523f, 0.55f, 0.008f, 0.24f, 0.30f);
        Seno(s, 0.11f, 0.28f, 659f, 659f, 0.55f, 0.008f, 0.26f, 0.30f);
        Seno(s, 0.24f, 0.62f, 784f, 784f, 0.60f, 0.008f, 0.60f, 0.35f);
        Seno(s, 0.24f, 0.62f, 1046f, 1046f, 0.30f, 0.008f, 0.60f, 0.20f);
        Ruido(s, 0.18f, 1.05f, 0.20f, 0.14f, 0.85f, 0.05f);
        return s;
    }

    /// <summary>Atajada: golpe sordo en los guantes + dos notas descendentes.</summary>
    private static float[] Atajada()
    {
        float[] s = Nuevo(0.85f);
        Seno(s, 0f, 0.20f, 150f, 70f, 0.90f, 0.002f, 0.19f, 0.30f);
        Ruido(s, 0f, 0.08f, 0.50f, 0.001f, 0.075f, 0.35f);
        Seno(s, 0.05f, 0.26f, 440f, 440f, 0.35f, 0.008f, 0.25f, 0.25f);
        Seno(s, 0.22f, 0.42f, 330f, 330f, 0.35f, 0.008f, 0.40f, 0.25f);
        return s;
    }

    /// <summary>Fallado: zumbido descendente (dos tonos cercanos que batent).</summary>
    private static float[] Fallado()
    {
        float[] s = Nuevo(0.75f);
        Seno(s, 0f, 0.55f, 300f, 150f, 0.50f, 0.010f, 0.53f);
        Seno(s, 0f, 0.55f, 296f, 147f, 0.50f, 0.010f, 0.53f);
        Ruido(s, 0f, 0.45f, 0.12f, 0.020f, 0.42f, 0.20f);
        return s;
    }

    /// <summary>Silbato del arbitro (inicio y fin de ronda).</summary>
    private static float[] Silbato()
    {
        float[] s = Nuevo(0.95f);
        int n = s.Length;
        float fase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float vib = 1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 26f * t);
            fase += 2f * Mathf.PI * 2350f * vib / SR;
            float env = Envolvente(t, 0.95f, 0.03f, 0.28f);
            s[i] += Mathf.Sin(fase) * 0.60f * env;
            s[i] += Mathf.Sin(fase * 2f) * 0.15f * env;
        }
        Ruido(s, 0f, 0.90f, 0.10f, 0.05f, 0.30f, 0.70f);
        return s;
    }

    /// <summary>Porton: reja metalica corrediza con traqueteo y golpe final.</summary>
    private static float[] Porton()
    {
        float[] s = Nuevo(1.15f);
        Ruido(s, 0f, 0.95f, 0.45f, 0.05f, 0.30f, 0.85f);
        for (int k = 0; k < 9; k++)
        {
            float t0 = 0.05f + k * 0.095f;
            Ruido(s, t0, 0.05f, 0.32f, 0.001f, 0.045f, 0.50f);
            Seno(s, t0, 0.06f, 950f, 700f, 0.16f, 0.001f, 0.055f);
        }
        Seno(s, 0.66f, 0.36f, 130f, 80f, 0.40f, 0.004f, 0.32f, 0.30f);
        return s;
    }

    /// <summary>Clic de interfaz (avisos y decisiones).</summary>
    private static float[] Clic()
    {
        float[] s = Nuevo(0.13f);
        Seno(s, 0f, 0.06f, 1500f, 950f, 0.50f, 0.001f, 0.055f, 0.20f);
        return s;
    }

    /// <summary>Ding del circulo que se pinta en el marcador.</summary>
    private static float[] Ding()
    {
        float[] s = Nuevo(0.55f);
        Seno(s, 0f, 0.50f, 1760f, 1760f, 0.45f, 0.002f, 0.49f, 0.25f);
        Seno(s, 0f, 0.36f, 2640f, 2640f, 0.16f, 0.002f, 0.35f);
        return s;
    }

    /// <summary>Bote del balon contra el suelo, el arco o el cerco.</summary>
    private static float[] Bote()
    {
        float[] s = Nuevo(0.22f);
        Seno(s, 0f, 0.16f, 280f, 120f, 0.75f, 0.001f, 0.15f, 0.25f);
        return s;
    }

    /// <summary>Aviso de que la carga del tiro llego al maximo.</summary>
    private static float[] Carga()
    {
        float[] s = Nuevo(0.38f);
        Seno(s, 0f, 0.10f, 900f, 1450f, 0.35f, 0.002f, 0.09f);
        Seno(s, 0.10f, 0.22f, 1650f, 1650f, 0.40f, 0.002f, 0.21f, 0.30f);
        return s;
    }
}
