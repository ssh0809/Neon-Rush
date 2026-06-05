using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFollowGlow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private RunnerController runner;
    [SerializeField] private EnergyModeController energyMode;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.65f, -0.9f);

    [Header("Particles")]
    [SerializeField] private float baseEmissionRate = 48f;
    [SerializeField] private float speedEmissionBoost = 32f;
    [SerializeField] private float particleLifetime = 0.75f;
    [SerializeField] private float particleSize = 0.18f;
    [SerializeField] private float sparkleSize = 0.07f;
    [SerializeField] private Color blueGlow = new Color(0.1f, 0.85f, 1f, 1f);
    [SerializeField] private Color redGlow = new Color(1f, 0.18f, 0.35f, 1f);

    [Header("Light")]
    [SerializeField] private bool usePointLight = true;
    [SerializeField] private float lightRange = 3.5f;
    [SerializeField] private float lightIntensity = 1.3f;

    private const string GlowObjectName = "Player Follow Glow";
    private const string LightObjectName = "Player Glow Light";

    private ParticleSystem glowParticles;
    private ParticleSystem sparkleParticles;
    private Light glowLight;
    private Color currentColor;

    private void Awake()
    {
        if (runner == null)
        {
            runner = GetComponent<RunnerController>();
        }

        if (energyMode == null)
        {
            energyMode = GetComponent<EnergyModeController>();
        }

        currentColor = energyMode != null && energyMode.CurrentMode == EnergyMode.Red ? redGlow : blueGlow;
        EnsureEffect();
        ApplyColor(currentColor);
    }

    private void OnEnable()
    {
        if (energyMode != null)
        {
            energyMode.ModeChanged += HandleModeChanged;
        }
    }

    private void OnDisable()
    {
        if (energyMode != null)
        {
            energyMode.ModeChanged -= HandleModeChanged;
        }
    }

    private void Update()
    {
        if (glowParticles == null)
        {
            return;
        }

        bool isPlaying = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        float speedFactor = runner == null ? 1f : Mathf.InverseLerp(8f, 18f, runner.CurrentSpeed);
        float invulnerableBoost = runner != null && runner.IsInvulnerable ? 1.45f : 1f;
        float emissionRate = isPlaying ? (baseEmissionRate + speedEmissionBoost * speedFactor) * invulnerableBoost : 0f;

        ParticleSystem.EmissionModule glowEmission = glowParticles.emission;
        glowEmission.rateOverTime = emissionRate;

        if (sparkleParticles != null)
        {
            ParticleSystem.EmissionModule sparkleEmission = sparkleParticles.emission;
            sparkleEmission.rateOverTime = emissionRate * 0.45f;
        }

        if (glowLight != null)
        {
            glowLight.intensity = isPlaying ? Mathf.Lerp(lightIntensity * 0.75f, lightIntensity * 1.25f, speedFactor) * invulnerableBoost : lightIntensity * 0.35f;
        }
    }

    private void EnsureEffect()
    {
        Transform effectRoot = transform.Find(GlowObjectName);
        if (effectRoot == null)
        {
            GameObject effectObject = new GameObject(GlowObjectName);
            effectRoot = effectObject.transform;
            effectRoot.SetParent(transform);
        }

        effectRoot.localPosition = localOffset;
        effectRoot.localRotation = Quaternion.identity;
        effectRoot.localScale = Vector3.one;

        glowParticles = effectRoot.GetComponent<ParticleSystem>();
        if (glowParticles == null)
        {
            glowParticles = effectRoot.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureGlowParticles(glowParticles);

        Transform sparkleRoot = effectRoot.Find("Sparkles");
        if (sparkleRoot == null)
        {
            GameObject sparkleObject = new GameObject("Sparkles");
            sparkleRoot = sparkleObject.transform;
            sparkleRoot.SetParent(effectRoot);
        }

        sparkleRoot.localPosition = Vector3.zero;
        sparkleRoot.localRotation = Quaternion.identity;
        sparkleRoot.localScale = Vector3.one;

        sparkleParticles = sparkleRoot.GetComponent<ParticleSystem>();
        if (sparkleParticles == null)
        {
            sparkleParticles = sparkleRoot.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureSparkleParticles(sparkleParticles);

        if (!usePointLight)
        {
            return;
        }

        Transform lightRoot = effectRoot.Find(LightObjectName);
        if (lightRoot == null)
        {
            GameObject lightObject = new GameObject(LightObjectName);
            lightRoot = lightObject.transform;
            lightRoot.SetParent(effectRoot);
        }

        lightRoot.localPosition = Vector3.zero;
        lightRoot.localRotation = Quaternion.identity;
        lightRoot.localScale = Vector3.one;

        glowLight = lightRoot.GetComponent<Light>();
        if (glowLight == null)
        {
            glowLight = lightRoot.gameObject.AddComponent<Light>();
        }

        glowLight.type = LightType.Point;
        glowLight.range = lightRange;
        glowLight.intensity = lightIntensity;
        glowLight.shadows = LightShadows.None;
    }

    private void ConfigureGlowParticles(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = particleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.45f, particleSize);
        main.gravityModifier = 0f;
        main.maxParticles = 400;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = baseEmissionRate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.28f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, -3.2f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.45f);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1f;
        renderer.material = CreateParticleMaterial();

        if (!particles.isPlaying)
        {
            particles.Play();
        }
    }

    private void ConfigureSparkleParticles(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(sparkleSize * 0.45f, sparkleSize);
        main.gravityModifier = 0f;
        main.maxParticles = 220;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = baseEmissionRate * 0.45f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 1f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.5f, -3.4f);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 1.2f;
        renderer.material = CreateParticleMaterial();

        if (!particles.isPlaying)
        {
            particles.Play();
        }
    }

    private void ApplyColor(Color color)
    {
        SetParticleColor(glowParticles, color, 0.9f);
        SetParticleColor(sparkleParticles, Color.Lerp(color, Color.white, 0.35f), 1f);

        if (glowLight != null)
        {
            glowLight.color = color;
        }
    }

    private void SetParticleColor(ParticleSystem particles, Color color, float alpha)
    {
        if (particles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.15f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(alpha, 0.12f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
    }

    private void HandleModeChanged(EnergyMode mode)
    {
        currentColor = mode == EnergyMode.Red ? redGlow : blueGlow;
        ApplyColor(currentColor);
    }

    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        Material material = new Material(shader);
        material.name = "Runtime Player Glow Particle";
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        return material;
    }
}
