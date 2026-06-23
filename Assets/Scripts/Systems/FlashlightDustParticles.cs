using System.Collections;
using UnityEngine;


[RequireComponent(typeof(ParticleSystem))]
public class FlashlightDustParticles : MonoBehaviour
{
    // =========================================================================
    //  CONFIGURACION DEL EFECTO
    // =========================================================================

    [Header("Referencia a la Linterna")]
    // FlashlightController del jugador. Si no se asigna, se busca en el padre.
    [SerializeField] private FlashlightController flashlightController;

    [Header("Parametros de Emision")]
    // Cuantas particulas de polvo se emiten por segundo cuando la linterna esta activa
    [SerializeField, Range(5f, 80f)] private float baseEmissionRate = 25f;
    // Multiplicador de emision en niveles de panico (efecto de "polvo perturbado por miedo")
    [SerializeField, Range(1f, 4f)] private float panicEmissionMultiplier = 2.5f;

    [Header("Tamano de las Particulas")]
    [SerializeField] private float minParticleSize = 0.008f;
    [SerializeField] private float maxParticleSize = 0.025f;

    [Header("Color y Transparencia")]
    // Color base del polvo: blanco con muy poco alpha para ser sutil
    [SerializeField] private Color dustColor = new Color(0.95f, 0.92f, 0.85f, 0.18f);
    // Color con maxima ansiedad: ligeramente mas oscuro y opaco (polvo perturbado)
    [SerializeField] private Color dustColorPanic = new Color(0.6f, 0.5f, 0.7f, 0.30f);

    [Header("Movimiento del Polvo")]
    // Velocidad inicial de las particulas: muy baja para simular flotacion
    [SerializeField] private float baseSpeed = 0.04f;
    [SerializeField] private float speedVariation = 0.06f;
    // Turbulencia que hace que el polvo no sea perfectamente recto
    [SerializeField] private float turbulenceStrength = 0.15f;
    // Tiempo de vida de cada particula de polvo (segundos)
    [SerializeField] private float particleLifetime = 3.5f;

    [Header("Forma del Cono")]
    // Angulo del cono de emision de polvo: debe coincidir con el spot angle de la linterna
    [SerializeField, Range(10f, 60f)] private float coneAngle = 28f;
    // Largo del cono de emision (distancia desde la linterna)
    [SerializeField] private float coneLength = 4.5f;

    [Header("Gravedad Simulada")]
    // Gravedad negativa muy baja para que el polvo "flote" en vez de caer
    [SerializeField, Range(-0.05f, 0.05f)] private float gravityModifier = -0.01f;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    private ParticleSystem dustParticleSystem;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;
    private ParticleSystem.ShapeModule shape;
    private ParticleSystem.ColorOverLifetimeModule colorOverLifetime;
    private ParticleSystem.NoiseModule noise;

    private bool wasFlashlightOn = false;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Awake()
    {
        dustParticleSystem = GetComponent<ParticleSystem>();

        // Buscar FlashlightController en el padre si no fue asignado en el Inspector
        if (flashlightController == null)
            flashlightController = GetComponentInParent<FlashlightController>();

        if (flashlightController == null)
            Debug.LogWarning("[FlashlightDustParticles] No se encontro FlashlightController. " +
                             "El efecto no sabra cuando activarse.");

        ConfigureParticleSystem();
    }

    private void Start()
    {
        // Las particulas arrancan detenidas; se activan cuando la linterna se enciende
        dustParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }


    private void ConfigureParticleSystem()
    {
        // --- Modulo Main ---
        main = dustParticleSystem.main;
        main.loop           = true;
        main.playOnAwake    = false;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(baseSpeed, baseSpeed + speedVariation);
        main.startSize      = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
        main.startColor     = new ParticleSystem.MinMaxGradient(dustColor);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(gravityModifier);
        main.maxParticles   = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // --- Modulo Emission ---
        emission = dustParticleSystem.emission;
        emission.rateOverTime = baseEmissionRate;

        // --- Modulo Shape: Cono alineado con el haz de la linterna ---
        shape = dustParticleSystem.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = coneAngle;
        shape.radius    = 0.03f;  // Radio muy pequeno en la punta para que salga del mismo punto
        shape.length    = coneLength;
        shape.radiusThickness = 1f;

        // --- Modulo Noise (Turbulencia organica) ---
        noise = dustParticleSystem.noise;
        noise.enabled   = true;
        noise.strength  = new ParticleSystem.MinMaxCurve(turbulenceStrength);
        noise.frequency = 0.5f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.08f);
        noise.quality   = ParticleSystemNoiseQuality.Medium;

        // --- Modulo Color Over Lifetime: fade in/out para que no "aparezcan" ni "desaparezcan" bruscamente ---
        colorOverLifetime = dustParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(dustColor,  0f), new GradientColorKey(dustColor,  1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(dustColor.a, 0.15f),
                                      new GradientAlphaKey(dustColor.a, 0.80f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }


    // =========================================================================
    //  UPDATE - Sincronizar con linterna y ansiedad
    // =========================================================================

    private void Update()
    {
        if (flashlightController == null) return;

        bool isFlashlightOn = flashlightController.IsOn;

        // Activar/desactivar emision segun el estado de la linterna
        if (isFlashlightOn && !wasFlashlightOn)
        {
            // La linterna se acaba de encender: iniciar particulas
            dustParticleSystem.Play();
        }
        else if (!isFlashlightOn && wasFlashlightOn)
        {
            // La linterna se apago: detener emision (las particulas existentes siguen hasta morir)
            dustParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        wasFlashlightOn = isFlashlightOn;

        // Actualizar la tasa de emision y color segun la ansiedad
        if (isFlashlightOn && AnxietySystem.Instance != null)
        {
            UpdateParticlesWithAnxiety(AnxietySystem.Instance.AnxietyNormalized);
        }
    }

    
    private void UpdateParticlesWithAnxiety(float anxietyNorm)
    {
        // Interpolar tasa de emision: mas ansiedad = mas polvo perturbado
        float targetEmission = Mathf.Lerp(baseEmissionRate, baseEmissionRate * panicEmissionMultiplier, anxietyNorm);
        emission.rateOverTime = targetEmission;

        // Interpolar el color inicial segun el panico
        Color currentDustColor = Color.Lerp(dustColor, dustColorPanic, anxietyNorm);
        main.startColor = new ParticleSystem.MinMaxGradient(currentDustColor);
    }
}