using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Guia de comandos que se muestra SIEMPRE abajo a la derecha de la pantalla.
/// Las lineas se pueden editar desde el Inspector (una por comando).
/// </summary>
public class GuiaComandos : MonoBehaviour
{
    [Header("Textos")]
    public string titulo = "COMANDOS";

    [Tooltip("Una linea por comando (siempre visible)")]
    public string[] lineas = new string[]
    {
        "·  W A S D / Flechas      -   Moverse",
        "·  Shift                  -   Correr",
        "·  Espacio                -   Saltar",
        "·  Ratón                  -   Girar cámara",
        "·  Rueda del ratón        -   Zoom",
        "·  E                      -   Acción / Entrar",
        "·  Esc                    -   Liberar cursor"
    };

    [Header("Modo ronda (mientras la ronda esta activa)")]
    [Tooltip("Fragmentos de linea que se ocultan durante la ronda")]
    public string[] ocultarEnRonda = new string[] { "Acción / Entrar" };
    [Tooltip("Lineas que SOLO se muestran durante la ronda (disparar, reponer la pelota, salir)")]
    public string[] lineasSoloEnRonda = new string[]
    {
        "·  Clic izquierdo         -   Disparar (mantener para cargar)",
        "·  R                      -   Reponer la pelota",
        "·  E                      -   Salir de la ronda"
    };

    [Header("Referencia")]
    public Text textoGuia;

    private string lineaExtra = "";
    private bool enModoRonda = false;

    void Start()
    {
        Refrescar();
    }

    /// <summary>
    /// Modo ronda: oculta las lineas de 'ocultarEnRonda' (por ejemplo "Acción / Entrar")
    /// y añade las de 'lineasSoloEnRonda' (disparar, reponer la pelota, salir de la ronda).
    /// </summary>
    public void ModoRonda(bool activo)
    {
        enModoRonda = activo;
        Refrescar();
    }

    /// <summary>Añade una linea temporal a la guia.</summary>
    public void AgregarLineaExtra(string linea)
    {
        lineaExtra = linea;
        Refrescar();
    }

    /// <summary>Quita la linea temporal.</summary>
    public void QuitarLineaExtra()
    {
        lineaExtra = "";
        Refrescar();
    }

    /// <summary>Reconstruye el texto de la guia con las lineas actuales.</summary>
    public void Refrescar()
    {
        if (textoGuia == null) return;
        string t = titulo;
        for (int i = 0; i < lineas.Length; i++)
        {
            if (enModoRonda && ContieneAlguno(lineas[i], ocultarEnRonda)) continue;
            t += "\n" + lineas[i];
        }
        if (enModoRonda && lineasSoloEnRonda != null)
        {
            for (int i = 0; i < lineasSoloEnRonda.Length; i++)
            {
                if (string.IsNullOrEmpty(lineasSoloEnRonda[i])) continue;
                t += "\n" + lineasSoloEnRonda[i];
            }
        }
        if (!string.IsNullOrEmpty(lineaExtra)) t += "\n" + lineaExtra;
        textoGuia.text = t;
    }

    private static bool ContieneAlguno(string linea, string[] fragmentos)
    {
        if (fragmentos == null) return false;
        for (int i = 0; i < fragmentos.Length; i++)
        {
            if (string.IsNullOrEmpty(fragmentos[i])) continue;
            if (linea.Contains(fragmentos[i])) return true;
        }
        return false;
    }

    void OnValidate()
    {
        if (textoGuia != null) Refrescar();
    }
}
