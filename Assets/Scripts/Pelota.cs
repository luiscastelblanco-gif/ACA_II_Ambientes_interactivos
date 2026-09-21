using UnityEngine;

/// <summary>
/// Pelota de la ronda de tiros de castigo.
/// Aparece en el punto de castigo al iniciar la ronda y se puede reiniciar
/// (por ejemplo despues de cada tiro).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Pelota : MonoBehaviour
{
    [Header("Punto de saque")]
    [Tooltip("Donde aparece la pelota al iniciar la ronda (por defecto el centro de la cancha)")]
    public Vector3 puntoDeSaque = new Vector3(0f, 0.16f, 0f);

    [Header("Estado (solo lectura)")]
    public bool enJuego = false;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>La pelota aparece en el punto de saque, quieta y en el suelo.</summary>
    public void Aparecer()
    {
        AparecerEn(puntoDeSaque);
    }

    public void AparecerEn(Vector3 posicion)
    {
        gameObject.SetActive(true);
        if (rb == null) rb = GetComponent<Rigidbody>();
        // OJO: hay que mover el Rigidbody (no solo el transform), porque con
        // interpolacion activada la fisica devuelve el objeto a su posicion interna.
        rb.position = posicion;
        rb.rotation = Quaternion.identity;
        transform.SetPositionAndRotation(posicion, Quaternion.identity);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
        enJuego = true;
    }

    /// <summary>La pelota desaparece (al terminar la ronda).</summary>
    public void Desaparecer()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        enJuego = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sonido del bote: suena distinto segun con que fuerza golpeo (suelo, arco o cerco).
    /// Se reproduce en el punto del golpe, asi se escucha de donde viene.
    /// </summary>
    void OnCollisionEnter(Collision choque)
    {
        if (!enJuego) return;

        float fuerza = choque.relativeVelocity.magnitude;
        if (fuerza < 1.2f) return;                       // roces sin importancia

        Vector3 punto = choque.contactCount > 0 ? choque.GetContact(0).point : transform.position;
        GestorAudio.SonarEn("bote", punto, Mathf.Clamp01(fuerza / 12f) * 0.9f);
    }

    /// <summary>Velocidad actual en m/s (util para saber si esta en movimiento).</summary>
    public float RapidezActual
    {
        get { return rb != null ? rb.linearVelocity.magnitude : 0f; }
    }

    /// <summary>Velocidad actual como vector (m/s): lo usan el arquero y el juez de tiro.</summary>
    public Vector3 Velocidad
    {
        get { return rb != null ? rb.linearVelocity : Vector3.zero; }
    }
}
