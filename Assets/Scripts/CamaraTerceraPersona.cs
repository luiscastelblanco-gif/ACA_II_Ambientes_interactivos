using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camara de tercera persona: orbita con el raton, zoom con la rueda y evita atravesar
/// la malla, porterias y tribunas.
///
/// Se coloca este componente en la Main Camera y se asigna "objetivo".
/// Si algun dia quieres ver desde el arquero, basta con cambiar "objetivo" al transform
/// del arquero: la camara ya queda lista para ambas vistas.
/// </summary>
public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("A quien sigue la camara (el jugador)")]
    public Transform objetivo;

    [Tooltip("Punto al que mira, relativo al objetivo (0, 1.5, 0) = pecho del jugador")]
    public Vector3 puntoDeMira = new Vector3(0f, 1.5f, 0f);

    [Header("Orbita")]
    public float distancia = 5f;
    [Tooltip("Sensibilidad del raton")]
    public float sensibilidad = 0.14f;
    [Tooltip("Limite para mirar hacia abajo (grados)")]
    public float anguloMinimo = -20f;
    [Tooltip("Limite para mirar hacia arriba (grados)")]
    public float anguloMaximo = 70f;
    [Tooltip("Angulo vertical inicial. Positivo = camara elevada mirando hacia abajo")]
    public float anguloInicial = 14f;
    [Tooltip("Mayor = la camara sigue mas pegada al objetivo (menos flotante)")]
    public float suavizado = 18f;

    [Header("Zoom")]
    public float distanciaMinima = 2f;
    public float distanciaMaxima = 10f;
    public float sensibilidadZoom = 0.4f;

    [Header("Colision")]
    [Tooltip("Evita que la camara atraviese malla, porterias o tribunas")]
    public bool evitarObstaculos = true;
    public LayerMask capasObstaculo = ~0;
    public float margenColision = 0.3f;

    [Header("Cursor")]
    public bool bloquearCursor = true;

    private float yaw;
    private float pitch;
    private Vector3 velocidadSuavizado;

    /// <summary>Angulo horizontal actual (util para orientar al jugador o al futuro arquero).</summary>
    public float Yaw { get { return yaw; } }

    void Start()
    {
        if (objetivo != null) yaw = objetivo.eulerAngles.y;
        pitch = anguloInicial;
        ApartarCursor(bloquearCursor);
    }

    void LateUpdate()
    {
        Mouse raton = Mouse.current;
        Keyboard teclado = Keyboard.current;

        // Esc alterna entre bloquear y liberar el cursor
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            ApartarCursor(Cursor.lockState != CursorLockMode.Locked);

        if (raton != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = raton.delta.ReadValue() * sensibilidad;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, anguloMinimo, anguloMaximo);

            float rueda = raton.scroll.ReadValue().y;
            if (Mathf.Abs(rueda) > 0.01f)
                distancia = Mathf.Clamp(distancia - rueda * sensibilidadZoom, distanciaMinima, distanciaMaxima);
        }

        if (objetivo == null) return;

        Vector3 pivote = objetivo.position + puntoDeMira;
        Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 deseada = pivote - rotacion * Vector3.forward * distancia;

        // Evitar que la camara entre en los obstaculos (excluyendo al propio objetivo)
        if (evitarObstaculos)
        {
            Vector3 direccion = deseada - pivote;
            float largo = direccion.magnitude;
            if (largo > 0.01f)
            {
                RaycastHit[] impactos = Physics.RaycastAll(pivote, direccion.normalized, largo, capasObstaculo, QueryTriggerInteraction.Ignore);
                float distanciaLibre = largo;
                for (int i = 0; i < impactos.Length; i++)
                {
                    if (impactos[i].collider == null) continue;
                    if (objetivo != null && impactos[i].collider.transform.root == objetivo.root) continue;
                    if (impactos[i].distance < distanciaLibre) distanciaLibre = impactos[i].distance;
                }
                if (distanciaLibre < largo)
                    deseada = pivote + direccion.normalized * Mathf.Max(0.6f, distanciaLibre - margenColision);
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, deseada, ref velocidadSuavizado, 1f / Mathf.Max(1f, suavizado));
        transform.rotation = rotacion;
    }

    /// <summary>Coloca la camara con un angulo concreto (util al iniciar un tiro o al cambiar de rol).</summary>
    public void MirarConAngulo(float nuevoYaw, float nuevoPitch, float nuevaDistancia = -1f)
    {
        yaw = nuevoYaw;
        pitch = Mathf.Clamp(nuevoPitch, anguloMinimo, anguloMaximo);
        if (nuevaDistancia > 0f) distancia = Mathf.Clamp(nuevaDistancia, distanciaMinima, distanciaMaxima);
    }

    private void ApartarCursor(bool bloquear)
    {
        bloquearCursor = bloquear;
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
