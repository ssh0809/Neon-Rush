using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EnergyBurst : MonoBehaviour
{
    [SerializeField] private RunnerController runner;
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float invulnerableSeconds = 2f;
    [SerializeField] private float clearDistance = 60f;
    [SerializeField] private float clearWidth = 16f;
    [SerializeField] private float clearHeight = 14f;
    [SerializeField] private LayerMask clearObstacleLayers = ~0;
    [SerializeField, Min(1)] private int maxClearHits = 128;
    [SerializeField] private GameObject burstVfxPrefab;
    [SerializeField] private float burstVfxScale = 9f;
    [SerializeField] private bool spawnProceduralBurstEffect = true;
    [SerializeField] private float proceduralBurstScale = 3.5f;
    [SerializeField] private float proceduralBurstDuration = 3f;
    [SerializeField] private bool spawnHologramBurstEffect = true;
    [SerializeField] private bool tintHologramBurstByEnergyMode = true;
    [SerializeField] private EnergyModeController energyMode;
    [SerializeField] private float hologramBurstDuration = 0.72f;
    [SerializeField] private float hologramBurstRadius = 4.2f;
    [SerializeField] private Color hologramBurstColor = new Color(0.1f, 0.85f, 1f, 0.85f);

    public float Energy { get; private set; }
    public float NormalizedEnergy => maxEnergy <= 0f ? 0f : Energy / maxEnergy;

    public event Action<float> EnergyChanged;

    private Collider[] clearHits;

    private void Awake()
    {
        if (energyMode == null)
        {
            energyMode = GetComponent<EnergyModeController>();
        }

        EnsureClearHitCapacity();
    }

    private void OnValidate()
    {
        maxClearHits = Mathf.Max(1, maxClearHits);
        clearDistance = Mathf.Max(0f, clearDistance);
        clearWidth = Mathf.Max(0f, clearWidth);
        clearHeight = Mathf.Max(0f, clearHeight);
        proceduralBurstDuration = Mathf.Max(0.05f, proceduralBurstDuration);
        hologramBurstDuration = Mathf.Max(0.05f, hologramBurstDuration);
        hologramBurstRadius = Mathf.Max(0.1f, hologramBurstRadius);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame && Energy >= maxEnergy)
        {
            TriggerBurst();
        }
    }

    public void AddEnergy(float amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0f, maxEnergy);
        EnergyChanged?.Invoke(NormalizedEnergy);

        if (Energy >= maxEnergy)
        {
            TriggerBurst();
        }
    }

    public void TriggerBurst()
    {
        Energy = 0f;
        EnergyChanged?.Invoke(NormalizedEnergy);

        if (runner != null)
        {
            runner.SetInvulnerable(invulnerableSeconds);
        }

        if (burstVfxPrefab != null)
        {
            VfxUtility.Spawn(burstVfxPrefab, transform.position, Quaternion.identity, burstVfxScale);
        }

        if (spawnProceduralBurstEffect)
        {
            SpawnProceduralBurstEffect();
        }

        ClearForwardObstacles();
    }

    private void SpawnProceduralBurstEffect()
    {
        GameObject root = new GameObject("Runtime Energy Burst");
        root.transform.position = transform.position + Vector3.up * 0.8f;
        root.transform.localScale = Vector3.one * proceduralBurstScale;

        CreateBurstParticleSystem(root.transform, "Shockwave", true);
        CreateBurstParticleSystem(root.transform, "Energy Sparks", false);

        if (spawnHologramBurstEffect)
        {
            CreateHologramBurstEffect(root.transform);
        }

        Light burstLight = root.AddComponent<Light>();
        burstLight.type = LightType.Point;
        burstLight.color = GetBurstColor();
        burstLight.range = 8f * proceduralBurstScale;
        burstLight.intensity = 5f;
        burstLight.shadows = LightShadows.None;

        StartCoroutine(FadeAndDestroyBurstLight(root, burstLight));
    }

    private void CreateHologramBurstEffect(Transform parent)
    {
        GameObject root = new GameObject("Hologram Burst Visual");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one / Mathf.Max(0.01f, proceduralBurstScale);

        Material shellMaterial = CreateRuntimeHologramMaterial();
        Material sweepMaterial = CreateRuntimeHologramMaterial();
        Color burstColor = GetBurstColor();

        ConfigureHologramBurstMaterial(shellMaterial, burstColor, 0.12f, 0.75f);
        ConfigureHologramBurstMaterial(sweepMaterial, burstColor, 0.18f, 1.15f);

        Transform shell = CreateHologramPrimitive(
            root.transform,
            "Hologram Scan Shell",
            PrimitiveType.Sphere,
            shellMaterial,
            Vector3.up * 0.55f,
            Vector3.one * 0.08f
        );

        Transform sweep = CreateHologramPrimitive(
            root.transform,
            "Hologram Ground Sweep",
            PrimitiveType.Cylinder,
            sweepMaterial,
            Vector3.up * 0.08f,
            new Vector3(0.08f, 0.012f, 0.08f)
        );

        StartCoroutine(AnimateHologramBurst(shell, shellMaterial, Vector3.one * hologramBurstRadius, 0.12f));
        StartCoroutine(AnimateHologramBurst(sweep, sweepMaterial, new Vector3(hologramBurstRadius * 1.65f, 0.012f, hologramBurstRadius * 1.65f), 0.18f));
    }

    private static void ConfigureHologramBurstMaterial(Material material, Color burstColor, float alpha, float intensity)
    {
        if (material == null)
        {
            return;
        }

        Color emission = burstColor * intensity;
        emission.a = 1f;
        material.SetColor("_BaseColor", burstColor);
        material.SetColor("_EmissionColor", emission);
        material.SetFloat("_Alpha", alpha);
        material.SetFloat("_LineDensity", 58f);
        material.SetFloat("_LineStrength", 0.72f);
        material.SetFloat("_ScanSpeed", 4.2f);
        material.SetFloat("_FresnelPower", 2.3f);
        material.SetFloat("_FresnelStrength", 0.9f);
        material.SetFloat("_GlitchStrength", 0.24f);
        material.SetFloat("_ProjectionFade", 0.05f);
    }

    private Transform CreateHologramPrimitive(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Material material,
        Vector3 localPosition,
        Vector3 localScale
    )
    {
        GameObject visual = GameObject.CreatePrimitive(primitiveType);
        visual.name = objectName;
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = localScale;

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return visual.transform;
    }

    private IEnumerator AnimateHologramBurst(Transform visual, Material material, Vector3 targetScale, float startAlpha)
    {
        if (visual == null)
        {
            yield break;
        }

        Vector3 startScale = visual.localScale;
        float elapsed = 0f;

        while (elapsed < hologramBurstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hologramBurstDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (visual != null)
            {
                visual.localScale = Vector3.Lerp(startScale, targetScale, eased);
            }

            if (material != null)
            {
                material.SetFloat("_Alpha", Mathf.Lerp(startAlpha, 0f, t));
                material.SetFloat("_Pulse", Mathf.Lerp(0.9f, 0.08f, t));
                material.SetFloat("_GlitchStrength", Mathf.Lerp(0.24f, 0.03f, t));
            }

            yield return null;
        }

        if (visual != null)
        {
            Destroy(visual.gameObject);
        }
    }

    private void CreateBurstParticleSystem(Transform parent, string objectName, bool shockwave)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        obj.SetActive(false);

        ParticleSystem particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = shockwave ? 0.35f : proceduralBurstDuration;
        main.startLifetime = shockwave ? 0.65f : new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        main.startSpeed = shockwave ? new ParticleSystem.MinMaxCurve(9f, 15f) : new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = shockwave ? new ParticleSystem.MinMaxCurve(0.18f, 0.32f) : new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = shockwave
            ? new Color(0.35f, 1f, 1f, 0.9f)
            : new Color(0.1f, 0.75f, 1f, 1f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = shockwave ? 260 : 700;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = shockwave ? 0f : 180f;
        if (shockwave)
        {
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 220) });
        }

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = shockwave ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
        shape.radius = shockwave ? 0.35f : 0.75f;
        shape.arc = 360f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = !shockwave;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(1.5f, 5.5f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, 2.8f);
        velocity.x = new ParticleSystem.MinMaxCurve(-2.8f, 2.8f);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = shockwave
            ? new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f))
            : new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.15f, 0.95f, 1f), 0.25f),
                new GradientColorKey(new Color(0.35f, 0.15f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = shockwave ? 2f : 1.4f;
        renderer.material = CreateRuntimeParticleMaterial();

        obj.SetActive(true);
        particles.Play();
    }

    private IEnumerator FadeAndDestroyBurstLight(GameObject root, Light burstLight)
    {
        float elapsed = 0f;
        float startIntensity = burstLight.intensity;

        while (elapsed < proceduralBurstDuration)
        {
            elapsed += Time.deltaTime;
            if (burstLight != null)
            {
                burstLight.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / proceduralBurstDuration);
            }

            yield return null;
        }

        if (root != null)
        {
            Destroy(root);
        }
    }

    private static Material CreateRuntimeParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        Material material = new Material(shader);
        material.name = "Runtime Energy Burst Particle";
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        return material;
    }

    private static Material CreateRuntimeHologramMaterial()
    {
        Shader shader = Shader.Find("NeonRush/HologramURP");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        return shader != null ? new Material(shader) { name = "Runtime Hologram Burst" } : null;
    }

    private Color GetBurstColor()
    {
        if (tintHologramBurstByEnergyMode && energyMode != null)
        {
            return ColorGate.GetModeColor(energyMode.CurrentMode);
        }

        return hologramBurstColor;
    }

    private void ClearForwardObstacles()
    {
        EnsureClearHitCapacity();

        Vector3 center = transform.position + Vector3.forward * (clearDistance * 0.5f);
        Vector3 halfExtents = new Vector3(clearWidth * 0.5f, clearHeight * 0.5f, clearDistance * 0.5f);
        int layerMask = clearObstacleLayers.value == 0 ? Physics.AllLayers : clearObstacleLayers.value;
        int hitCount = Physics.OverlapBoxNonAlloc(
            center,
            halfExtents,
            clearHits,
            Quaternion.identity,
            layerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = clearHits[i];
            if (hit == null)
            {
                continue;
            }

            Obstacle obstacle = hit.GetComponentInParent<Obstacle>();
            if (obstacle != null && obstacle.gameObject.activeSelf)
            {
                obstacle.gameObject.SetActive(false);
            }

            clearHits[i] = null;
        }
    }

    private void EnsureClearHitCapacity()
    {
        maxClearHits = Mathf.Max(1, maxClearHits);

        if (clearHits == null || clearHits.Length != maxClearHits)
        {
            clearHits = new Collider[maxClearHits];
        }
    }
}
