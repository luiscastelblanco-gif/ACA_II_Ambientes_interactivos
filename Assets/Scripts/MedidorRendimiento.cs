using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Medidor de rendimiento del juego (herramienta de la entrega de optimizacion).
///
/// Mide durante N segundos y deja un informe de texto en <see cref="ultimoInforme"/>:
///   - tiempo de cuadro promedio (ms) y FPS
///   - draw calls, SetPass y triangulos (datos del editor)
///   - objetos creados/destruidos durante la ventana (churn del recolector)
///
/// Uso desde el editor (en Play):
///     var m = miObjeto.AddComponent&lt;MedidorRendimiento&gt;();
///     m.Medir(4f);
/// a los ~4,5 s se lee <c>m.ultimoInforme</c>.
///
/// Los primeros 0,5 s se descartan (calentamiento) para que la medida sea comparable.
/// </summary>
public class MedidorRendimiento : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("True mientras esta midiendo")]
    public bool midiendo = false;
    [Tooltip("Duracion de la ventana de medida (s)")]
    public float duracion = 4f;
    [Tooltip("Segundos que se descartan al principio")]
    public float calentamiento = 0.5f;

    [Header("Resultado (solo lectura)")]
    public string ultimoInforme = "";
    public float msPromedio = 0f;
    public float fps = 0f;
    public int cuadrosMedidos = 0;
    public int objetosInicio = 0;
    public int objetosFin = 0;
    public int drawCalls = 0;
    public int setPassCalls = 0;
    public int triangulos = 0;

    private float tInicio;
    private int cuadros;
    private float sumaMs;

    /// <summary>Arranca la medicion.</summary>
    public void Medir(float segundos)
    {
        duracion = Mathf.Max(0.5f, segundos);
        cuadros = 0;
        sumaMs = 0f;
        msPromedio = 0f;
        fps = 0f;
        ultimoInforme = "(midiendo...)";
        tInicio = Time.unscaledTime;
        objetosInicio = ContarObjetos();
        midiendo = true;
    }

    void Update()
    {
        if (!midiendo) return;

        float transcurrido = Time.unscaledTime - tInicio;
        if (transcurrido < calentamiento) return;     // calentamiento: no se cuenta

        cuadros++;
        sumaMs += Time.unscaledDeltaTime * 1000f;

        if (transcurrido < duracion + calentamiento) return;
        Cerrar(transcurrido - calentamiento);
    }

    private static int ContarObjetos()
    {
        return UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
    }

    private void Cerrar(float segundos)
    {
        midiendo = false;

        objetosFin = ContarObjetos();

        int c = Mathf.Max(1, cuadros);
        msPromedio = sumaMs / c;
        fps = c / Mathf.Max(0.001f, segundos);
        cuadrosMedidos = c;

#if UNITY_EDITOR
        drawCalls = UnityEditor.UnityStats.drawCalls;
        setPassCalls = UnityEditor.UnityStats.setPassCalls;
        triangulos = UnityEditor.UnityStats.triangles;
#endif

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== MEDICION (" + segundos.ToString("F1", CultureInfo.InvariantCulture) + " s, " + c + " cuadros) ===");
        sb.AppendLine("  tiempo de cuadro : " + msPromedio.ToString("F2", CultureInfo.InvariantCulture) + " ms   (" + fps.ToString("F0", CultureInfo.InvariantCulture) + " FPS)");
        sb.AppendLine("  draw calls       : " + drawCalls + "   SetPass: " + setPassCalls + "   triangulos: " + triangulos);
        sb.AppendLine("  objetos en escena: inicio " + objetosInicio + " -> fin " + objetosFin + "   (creados: " + (objetosFin - objetosInicio) + ")");
        ultimoInforme = sb.ToString();
    }
}
