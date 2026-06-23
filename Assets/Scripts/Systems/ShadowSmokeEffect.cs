using System.Collections;
using UnityEngine;



[RequireComponent(typeof(ParticleSystem))]
public class ShadowSmokeEffect : MonoBehaviour
{
    // =========================================================================
    //  CONFIGURACION
    // =========================================================================

    public enum SmokeMode
    {
        Continuous,   // Humo continuo (adjunto al ShadowEnemy, sigue a la sombra)
        SpawnBurst    // Rafaga de humo puntual al momento del spawn
    }

    [Header("Modo del Efecto")]
    [SerializeField] private SmokeMode smokeMode = SmokeMode.SpawnBurst;

    [Header("Parametros del Humo")]
    // Numero de particulas emitidas en la rafaga de spawn
    [SerializeField, Range(10, 120)] private int burstParticleCount = 55;
    // Tasa de emision continua (solo en modo Continuous)
    [SerializeField, Range(1f, 40f)] private float continuousEmissionRate = 8f;

    [Header("Apariencia")]
    // Color de inicio del humo (oscuro, casi negro con leve tinte purpura)
    [SerializeField] private Color smokeColorStart = new Color(0.05f, 0f, 0.08f, 0.75f);
    // Color de fin del humo al desvanecerse (mas claro y transparente)
    [SerializeField] private Color smokeColorEnd   = new Color(0.15f, 0.1f, 0.2f, 0f);
    // Tamano inicial de cada particula de humo
    [SerializeField] private float minSmokeSize = 0.4f;
    [SerializeField] private float maxSmokeSize = 1.2f;
    // Cuanto se expande cada particula durante su vida (escala final)
    [SerializeField] private float sizeOverLifetimeMultiplier = 2.8f;

    [Header("Movimiento")]
    // Velocidad inicial hacia arriba del humo
    [SerializeField] private float riseSpeed = 0.35f;
    [SerializeField] private float riseSpeedVariation = 0.25f;
    // Cuanto tiempo vive cada particula de humo
    [SerializeField] private float smokeLifetime = 2.8f;
    // Ligera turbulencia para que el humo no sea perfecto
    [SerializeField] private float turbulenceStrength = 0.2f;

    [Header("Forma de Emision")]
    // Radio del area de emision del humo en el suelo
    [SerializeField] private float emissionRadius = 0.5f;


    // =========================================================================
    //  ESTADO INTERNO
    // =========================================================================

    private ParticleSystem smokePS;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.NoiseModule noise;
    private bool isConfigured = false;


    // =========================================================================
    //  INICIALIZACION
    // =========================================================================

    private void Awake()
    {
        smokePS = GetComponent<ParticleSystem>();
        ConfigureParticleSystem();
    }

    private void Start()
    {
        switch (smokeMode)
        {
            case SmokeMode.SpawnBurst:
                // Emitir una rafaga unica inmediatamente y luego detener
                PlaySpawnBurst();
                break;

            case SmokeMode.Continuous:
                // Comenzar emision continua (sigue a la sombra mientras vive)
                smokePS.Play();
                break;
        }
    }

    
    private void ConfigureParticleSystem()
    {
        if (isConfigured) return;

        // --- Modulo Main ---
        main = smokePS.main;
        main.loop            = (smokeMode == SmokeMode.Continuous);
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(smokeLifetime * 0.6f, smokeLifetime);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(riseSpeed, riseSpeed + riseSpeedVariation);
        main.startSize       = new ParticleSystem.MinMaxCurve(minSmokeSize, maxSmokeSize);
        main.startColor      = new ParticleSystem.MinMaxGradient(smokeColorStart, smokeColorEnd);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f, 0f); // Leve flotacion hacia arriba
        main.maxParticles    = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // --- Modulo Emission ---
        emission = smokePS.emission;
        emission.enabled      = true;
        emission.rateOverTime = (smokeMode == SmokeMode.Continuous) ? continuousEmissionRate : 0f;

        // --- Modulo Shape: Circulo plano en el suelo ---
        shapeModule = smokePS.shape;
        shapeModule.enabled   = true;
        shapeModule.shapeType = ParticleSystemShapeType.Circle;
        shapeModule.radius    = emissionRadius;
        shapeModule.rotation  = new Vector3(90f, 0f, 0f); // Emitir desde el plano horizontal

        // --- Modulo Noise (Turbulencia organica del humo) ---
        noise = smokePS.noise;
        noise.enabled    = true;
        noise.strength   = new ParticleSystem.MinMaxCurve(turbulenceStrength * 0.5f, turbulenceStrength);
        noise.frequency  = 0.35f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.12f);
        noise.quality    = ParticleSystemNoiseQuality.Medium;

        // --- Modulo Size Over Lifetime: crece mientras asciende ---
        var sizeOverLifetime = smokePS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, sizeOverLifetimeMultiplier);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // --- Modulo Color Over Lifetime: fade out gradual ---
        var colorOverLifetime = smokePS.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(smokeColorStart.r, smokeColorStart.g, smokeColorStart.b), 0f),
                new GradientColorKey(new Color(smokeColorEnd.r,   smokeColorEnd.g,   smokeColorEnd.b),   1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,               0f),
                new GradientAlphaKey(smokeColorStart.a, 0.08f),
                new GradientAlphaKey(smokeColorStart.a, 0.55f),
                new GradientAlphaKey(0f,               1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        isConfigured = true;
    }


    // =========================================================================
    //  RAFAGA DE SPAWN
    // =========================================================================

    
    private void PlaySpawnBurst()
    {
        smokePS.Emit(burstParticleCount);
        // Iniciar corrutina que destruye este GameObject cuando el sistema termine
        StartCoroutine(DestroyWhenComplete());
    }

    private IEnumerator DestroyWhenComplete()
    {
        // Esperar a que el sistema de particulas termine de emitir y que mueran todas
        yield return new WaitUntil(() => !smokePS.IsAlive(true));
        Destroy(gameObject);
    }


    // =========================================================================
    //  METODO ESTATICO - Invocado desde ShadowSpawnerTrigger o ShadowEnemy
    // =========================================================================

    
    public static void PlayBurstAtPosition(Vector3 position, GameObject smokePrefab = null)
    {
        GameObject smokeObj;

        if (smokePrefab != null)
        {
            // Instanciar el prefab personalizado proporcionado
            smokeObj = Instantiate(smokePrefab, position, Quaternion.identity);
        }
        else
        {
            // Crear un GameObject minimo con ParticleSystem y ShadowSmokeEffect en codigo
            smokeObj = new GameObject("ShadowSpawnSmoke_Burst");
            smokeObj.transform.position = position;
            smokeObj.AddComponent<ParticleSystem>();
            ShadowSmokeEffect effect = smokeObj.AddComponent<ShadowSmokeEffect>();
            effect.smokeMode = SmokeMode.SpawnBurst;
        }
    }

    // =========================================================================
    //  CONTROL DE EMISION CONTINUA (para Modo Continuous)
    // =========================================================================

    
    public void StopSmoke()
    {
        if (smokeMode == SmokeMode.Continuous)
            smokePS.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    
    public void SetIntensity(float normalizedAnxiety)
    {
        if (smokeMode != SmokeMode.Continuous) return;
        emission.rateOverTime = Mathf.Lerp(continuousEmissionRate * 0.3f,
                                            continuousEmissionRate * 1.8f,
                                            normalizedAnxiety);
    }
}