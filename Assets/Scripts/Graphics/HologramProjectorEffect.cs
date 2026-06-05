using UnityEngine;

[DisallowMultipleComponent]
public sealed class HologramProjectorEffect : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Color beamColor = new Color(0.1f, 0.85f, 1f, 0.65f);
    [SerializeField] private Vector3 localSourceOffset = new Vector3(0f, 2.6f, -2.2f);
    [SerializeField] private float beamLength = 3.4f;
    [SerializeField] private float beamWidth = 0.45f;
    [SerializeField] private float particleRate = 80f;
    [SerializeField] private float particleLifetime = 0.8f;
    [SerializeField] private float flickerSpeed = 7f;
    [SerializeField] private bool createPointLight = true;

    private ParticleSystem beamParticles;
    private Light projectorLight;
    private Transform beamRoot;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        EnsureEffect();
        ApplyColor();
    }

    private void Update()
    {
        if (beamParticles == null)
        {
            return;
        }

        AimBeamAtTarget();

        float flicker = 0.78f + Mathf.Sin(Time.time * flickerSpeed) * 0.22f;
        ParticleSystem.EmissionModule emission = beamParticles.emission;
        emission.rateOverTime = particleRate * flicker;

        if (projectorLight != null)
        {
            projectorLight.intensity = 1.4f * flicker;
        }
    }

    [ContextMenu("Rebuild Effect")]
    private void EnsureEffect()
    {
        Transform source = transform.Find("Hologram Projector Beam");
        if (source == null)
        {
            GameObject sourceObject = new GameObject("Hologram Projector Beam");
            source = sourceObject.transform;
            source.SetParent(transform, false);
        }

        beamRoot = source;
        source.localPosition = localSourceOffset;
        source.localRotation = Quaternion.identity;
        source.localScale = Vector3.one;

        beamParticles = source.GetComponent<ParticleSystem>();
        if (beamParticles == null)
        {
            beamParticles = source.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureBeamParticles(beamParticles);

        if (createPointLight)
        {
            projectorLight = source.GetComponent<Light>();
            if (projectorLight == null)
            {
                projectorLight = source.gameObject.AddComponent<Light>();
            }

            projectorLight.type = LightType.Point;
            projectorLight.range = beamLength * 1.35f;
            projectorLight.intensity = 1.4f;
            projectorLight.shadows = LightShadows.None;
        }
    }

    private void ConfigureBeamParticles(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.65f, particleLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(beamLength * 0.9f, beamLength * 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(beamWidth * 0.08f, beamWidth * 0.18f);
        main.gravityModifier = 0f;
        main.maxParticles = 260;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = particleRate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = beamWidth;
        shape.rotation = new Vector3(0f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(beamLength * 0.6f, beamLength);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.05f))
        );

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 2f;
        renderer.material = CreateParticleMaterial();

        ApplyColor();

        if (!particles.isPlaying)
        {
            particles.Play();
        }
    }

    private void AimBeamAtTarget()
    {
        if (beamRoot == null || target == null)
        {
            return;
        }

        Vector3 direction = target.position + Vector3.up * 0.8f - beamRoot.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            beamRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ApplyColor()
    {
        if (beamParticles != null)
        {
            ParticleSystem.MainModule main = beamParticles.main;
            main.startColor = beamColor;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = beamParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            Color bright = Color.Lerp(beamColor, Color.white, 0.35f);
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(bright, 0f),
                    new GradientColorKey(beamColor, 0.45f),
                    new GradientColorKey(beamColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(beamColor.a, 0.16f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;
        }

        if (projectorLight != null)
        {
            projectorLight.color = beamColor;
        }
    }

    private static Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        Material material = new Material(shader);
        material.name = "Runtime Hologram Beam Particle";
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        return material;
    }
}
