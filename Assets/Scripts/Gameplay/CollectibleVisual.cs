using UnityEngine;

/// <summary>
/// 为收集物生成赛博朋克风格的"能量核心"视觉 —
/// 发光球体 + 陀螺仪轨道环 + 浮动动画。
/// 温暖金色调，与障碍物的暗色霓虹形成鲜明对比。
/// 由 Collectible 上的 [RequireComponent] 自动附加。
/// </summary>
[RequireComponent(typeof(Collectible))]
public sealed class CollectibleVisual : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Color coreColor = new Color(1f, 0.82f, 0.18f);
    [SerializeField] private float coreEmission = 0.7f;
    [SerializeField] private float coreRadius = 0.28f;

    [Header("Orbit Rings")]
    [SerializeField] private Color ringColor = new Color(1f, 0.92f, 0.45f);
    [SerializeField] private float ringEmission = 1.1f;
    [SerializeField] private float ringRadius = 0.5f;
    [SerializeField] private int ringCount = 3;

    [Header("Glow Particles")]
    [SerializeField] private Color particleColor = new Color(1f, 0.95f, 0.7f);
    [SerializeField] private float particleEmission = 1.5f;
    [SerializeField] private int particleCount = 4;

    [Header("Animation")]
    [SerializeField] private float baseRotationSpeed = 40f;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatFrequency = 1.8f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmplitude = 0.15f;

    [Header("Build")]
    [SerializeField] private bool autoBuild = true;

    private Transform visualRoot;
    private Transform[] ringTransforms;
    private float[] ringSpeeds;
    private Material[] ringMaterials;
    private Material coreMaterial;
    private Material[] particleMaterials;
    private float spawnTime;

    private void Awake()
    {
        Transform existingVisuals = transform.Find("CollectibleVisuals");
        bool alreadyBuilt = existingVisuals != null;

        if (!alreadyBuilt && autoBuild)
        {
            MeshRenderer defaultRenderer = GetComponent<MeshRenderer>();
            MeshFilter defaultFilter = GetComponent<MeshFilter>();
            if (defaultRenderer != null) DestroyImmediate(defaultRenderer);
            if (defaultFilter != null) DestroyImmediate(defaultFilter);

            spawnTime = Time.time;
            BuildVisual();
        }
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        float age = Time.time - spawnTime;

        // ── 主体旋转 ──
        visualRoot.Rotate(Vector3.up, baseRotationSpeed * Time.deltaTime, Space.Self);

        // ── 浮动动画 ──
        float bob = Mathf.Sin(age * floatFrequency) * floatAmplitude;
        visualRoot.localPosition = new Vector3(0f, bob, 0f);

        // ── 各环独立旋转（陀螺仪效果） ──
        for (int i = 0; i < ringTransforms.Length; i++)
        {
            if (ringTransforms[i] != null)
            {
                ringTransforms[i].Rotate(Vector3.up, ringSpeeds[i] * Time.deltaTime, Space.Self);
            }
        }

        // ── 发光脉冲 ──
        float pulse = 1f + Mathf.Sin(age * pulseSpeed) * pulseAmplitude;

        if (coreMaterial != null)
        {
            coreMaterial.SetColor("_EmissionColor", coreColor * coreEmission * pulse);
        }

        for (int i = 0; i < ringMaterials.Length; i++)
        {
            if (ringMaterials[i] != null)
            {
                ringMaterials[i].SetColor("_EmissionColor", ringColor * ringEmission * pulse);
            }
        }

        for (int i = 0; i < particleMaterials.Length; i++)
        {
            if (particleMaterials[i] != null)
            {
                // 粒子与主脉冲错开相位，产生交替闪烁
                float particlePulse = 1f + Mathf.Sin(age * pulseSpeed + Mathf.PI * 0.5f) * pulseAmplitude * 1.3f;
                particleMaterials[i].SetColor("_EmissionColor", particleColor * particleEmission * particlePulse);
            }
        }
    }

    // ───────────────────── 程序化建模 ─────────────────────

    private void BuildVisual()
    {
        GameObject root = new GameObject("CollectibleVisuals");
        root.transform.SetParent(transform, false);
        visualRoot = root.transform;

        BuildCore();
        BuildOrbitRings();
        BuildGlowParticles();
    }

    /// <summary>
    /// 中央能量球 — 温暖的发光核心。
    /// </summary>
    private void BuildCore()
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "EnergyCore";
        core.transform.SetParent(visualRoot, false);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = Vector3.one * coreRadius * 2f;

        Collider c = core.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        Renderer renderer = core.GetComponent<Renderer>();
        if (renderer != null)
        {
            coreMaterial = CreateMaterial(coreColor, coreColor * coreEmission, "CollectibleCore");
            renderer.material = coreMaterial;
        }

        // 内核光晕 — 更小的亮球
        GameObject innerGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerGlow.name = "InnerGlow";
        innerGlow.transform.SetParent(core.transform, false);
        innerGlow.transform.localPosition = Vector3.zero;
        innerGlow.transform.localScale = Vector3.one * 0.55f;

        Collider ic = innerGlow.GetComponent<Collider>();
        if (ic != null) DestroyImmediate(ic);

        Renderer innerRenderer = innerGlow.GetComponent<Renderer>();
        if (innerRenderer != null)
        {
            Material innerMat = CreateMaterial(
                particleColor,
                particleColor * particleEmission * 1.3f,
                "CollectibleInnerGlow"
            );
            innerRenderer.material = innerMat;
        }
    }

    /// <summary>
    /// 陀螺仪轨道环 — 围绕核心旋转的发光环。
    /// 每环倾斜不同角度，产生科幻能量场效果。
    /// </summary>
    private void BuildOrbitRings()
    {
        ringTransforms = new Transform[ringCount];
        ringSpeeds = new float[ringCount];
        ringMaterials = new Material[ringCount];

        // 每环的倾斜角度和旋转速度
        float[] tiltAngles = { 0f, 62f, -55f };
        float[] speeds = { 55f, 72f, 65f };
        float[] radii = { ringRadius, ringRadius * 0.85f, ringRadius * 0.92f };

        for (int i = 0; i < ringCount; i++)
        {
            // 倾斜容器（不旋转）
            GameObject tiltContainer = new GameObject($"RingTilt_{i}");
            tiltContainer.transform.SetParent(visualRoot, false);
            tiltContainer.transform.localPosition = Vector3.zero;
            tiltContainer.transform.localEulerAngles = new Vector3(tiltAngles[i], 0f, 0f);

            // 环本身（绕自身 Y 旋转 → 在倾斜平面上产生陀螺仪效果）
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"OrbitRing_{i}";
            ring.transform.SetParent(tiltContainer.transform, false);
            ring.transform.localPosition = Vector3.zero;
            float r = radii[i];
            ring.transform.localScale = new Vector3(r * 2f, 0.025f, r * 2f);

            Collider c = ring.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            ringTransforms[i] = ring.transform;
            ringSpeeds[i] = speeds[i];

            Renderer renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
            {
                float brightness = 1f + i * 0.2f;
                ringMaterials[i] = CreateMaterial(
                    ringColor * brightness,
                    ringColor * ringEmission * brightness,
                    $"CollectibleRing_{i}"
                );
                renderer.material = ringMaterials[i];
            }
        }
    }

    /// <summary>
    /// 环绕微光粒子 — 漂浮在轨道环外的小光点。
    /// </summary>
    private void BuildGlowParticles()
    {
        particleMaterials = new Material[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.name = $"GlowParticle_{i}";
            particle.transform.SetParent(visualRoot, false);

            // 分布在环的外缘
            float angle = i * (360f / particleCount) * Mathf.Deg2Rad;
            float radius = ringRadius * 1.2f;
            float height = (i % 2 == 0 ? 1f : -1f) * ringRadius * 0.5f;
            particle.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius
            );
            particle.transform.localScale = Vector3.one * 0.08f;

            Collider c = particle.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            Renderer renderer = particle.GetComponent<Renderer>();
            if (renderer != null)
            {
                particleMaterials[i] = CreateMaterial(
                    particleColor,
                    particleColor * particleEmission,
                    $"CollectibleParticle_{i}"
                );
                renderer.material = particleMaterials[i];
            }
        }
    }

    // ───────────────────── 材质 ─────────────────────

    private static Material CreateMaterial(Color baseColor, Color emissionColor, string name)
    {
        Shader shader = Shader.Find("NeonRush/RuntimeEmissiveUnlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("DELTation/Toon Shader");
        }

        if (shader == null)
        {
            return null;
        }

        Material mat = new Material(shader);
        mat.name = name;
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", Color.white);
        mat.SetColor("_EmissionColor", emissionColor);
        mat.SetFloat("_EmissionPower", 1f);
        return mat;
    }
}
