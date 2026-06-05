using UnityEngine;

/// <summary>
/// 为障碍物生成赛博朋克霓虹风格的视觉。
/// 用独立材质实例保证颜色正确显示，运行时呼吸脉冲动画。
/// 注：由 Obstacle 上的 [RequireComponent] 自动附加，无需手动添加。
/// </summary>
public sealed class ObstacleVisual : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField] private float emissionIntensity = 2.2f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmplitude = 0.25f;

    [Header("Colors")]
    [SerializeField] private Color dangerColor   = new Color(1f, 0.1f, 0.06f);
    [SerializeField] private Color warningColor  = new Color(1f, 0.55f, 0.05f);
    [SerializeField] private Color techColor     = new Color(0.15f, 0.55f, 1f);
    [SerializeField] private Color boostColor    = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color sentinelColor = new Color(0.2f, 0.75f, 0.9f);
    [SerializeField] private Color bodyColor     = new Color(0.12f, 0.12f, 0.15f); // 调亮，避免"纯黑棍子"

    [Header("Build")]
    [SerializeField] private bool autoBuild = true;

    private Obstacle obstacle;
    private Material neonMaterial;
    private Renderer[] allRenderers;
    private Transform visualRoot;
    private float spawnTime;

    // 用于脉冲时回查每部件原始色
    private Color[] partOriginalColors;

    private void Awake()
    {
        obstacle = GetComponent<Obstacle>();

        if (obstacle == null)
        {
            Debug.LogError($"[ObstacleVisual] 找不到 Obstacle 组件！GameObject: {gameObject.name}", this);
            return;
        }

        spawnTime = Time.time;

        Transform existingVisuals = transform.Find("ObstacleVisuals");
        bool alreadyBuilt = existingVisuals != null;

        if (!alreadyBuilt && autoBuild)
        {
            // 移除 GameObject 自带的默认 Mesh（Cube 原型等）
            MeshRenderer defaultRenderer = GetComponent<MeshRenderer>();
            MeshFilter defaultFilter = GetComponent<MeshFilter>();
            if (defaultRenderer != null) DestroyImmediate(defaultRenderer);
            if (defaultFilter != null) DestroyImmediate(defaultFilter);

            BuildVisuals();
        }

        // 收集所有 Renderer（包括刚程序化生成的）
        allRenderers = GetComponentsInChildren<Renderer>();
        neonMaterial = CreateNeonMaterial();
        ApplyMaterialAndColors();
    }

    private void Update()
    {
        if (allRenderers == null || allRenderers.Length == 0)
        {
            return;
        }

        float age = Time.time - spawnTime;
        float pulse = 0.7f + Mathf.Sin(age * pulseSpeed) * pulseAmplitude;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null || r.material == null)
            {
                continue;
            }

            // 用独立材质做脉冲：在原始色和亮色之间 lerp
            Color original = (partOriginalColors != null && i < partOriginalColors.Length)
                ? partOriginalColors[i]
                : bodyColor;

            Color litColor = Color.Lerp(original, original * emissionIntensity, pulse);
            r.material.SetColor("_BaseColor", litColor);
        }
    }

    // ───────────────────── 程序化建模 ─────────────────────

    private void BuildVisuals()
    {
        GameObject root = new GameObject("ObstacleVisuals");
        root.transform.SetParent(transform, false);
        visualRoot = root.transform;

        switch (obstacle.Type)
        {
            case ObstacleType.LaneBlock:  BuildLaneBlock();  break;
            case ObstacleType.Low:        BuildLow();        break;
            case ObstacleType.High:       BuildHigh();       break;
            case ObstacleType.Moving:     BuildMoving();     break;
            case ObstacleType.Rotating:   BuildRotating();   break;
            case ObstacleType.Slow:       BuildSlow();       break;
            case ObstacleType.Knockback:  BuildKnockback();  break;
        }
    }

    // ── LaneBlock: 霓虹屏障 ──
    private void BuildLaneBlock()
    {
        CreatePart("Body",      new Vector3(0f, 1.2f, 0f),   new Vector3(0.35f, 2.4f, 0.25f), bodyColor);
        CreatePart("NeonLeft",  new Vector3(-0.22f, 1.2f, -0.05f), new Vector3(0.06f, 2.6f, 0.08f), dangerColor);
        CreatePart("NeonRight", new Vector3(0.22f, 1.2f, -0.05f),  new Vector3(0.06f, 2.6f, 0.08f), dangerColor);
        CreatePart("NeonTop",   new Vector3(0f, 2.45f, -0.05f),    new Vector3(0.5f, 0.07f, 0.08f), dangerColor);
        CreatePart("NeonBottom",new Vector3(0f, 0.06f, -0.05f),    new Vector3(0.5f, 0.06f, 0.08f), dangerColor);
        CreatePart("Beacon",    new Vector3(0f, 2.62f, -0.05f),    new Vector3(0.16f, 0.1f, 0.16f), dangerColor * 1.4f);
        Transform d1 = CreatePart("ChevronL", new Vector3(-0.08f, 1.2f, -0.1f), new Vector3(0.03f, 1.8f, 0.03f), dangerColor * 0.7f);
        d1.localEulerAngles = new Vector3(0f, 0f, 25f);
        Transform d2 = CreatePart("ChevronR", new Vector3(0.08f, 1.2f, -0.1f), new Vector3(0.03f, 1.8f, 0.03f), dangerColor * 0.7f);
        d2.localEulerAngles = new Vector3(0f, 0f, -25f);
    }

    // ── Low: 切割射线 ──
    private void BuildLow()
    {
        CreatePart("PostL", new Vector3(-0.9f, 0.35f, 0f), new Vector3(0.12f, 0.7f, 0.15f), bodyColor);
        CreatePart("PostR", new Vector3(0.9f, 0.35f, 0f), new Vector3(0.12f, 0.7f, 0.15f), bodyColor);
        CreatePart("Beam", new Vector3(0f, 0.72f, 0f), new Vector3(1.9f, 0.08f, 0.12f), warningColor);
        CreatePart("FloorStrip", new Vector3(0f, 0.03f, 0f), new Vector3(1.9f, 0.04f, 0.1f), warningColor * 0.6f);
        CreatePart("CapL", new Vector3(-0.9f, 0.72f, 0f), new Vector3(0.16f, 0.06f, 0.16f), warningColor);
        CreatePart("CapR", new Vector3(0.9f, 0.72f, 0f), new Vector3(0.16f, 0.06f, 0.16f), warningColor);
    }

    // ── High: 高位横梁 ──
    private void BuildHigh()
    {
        CreatePart("PostL", new Vector3(-0.9f, 1.3f, 0f), new Vector3(0.14f, 2.6f, 0.18f), bodyColor);
        CreatePart("PostR", new Vector3(0.9f, 1.3f, 0f), new Vector3(0.14f, 2.6f, 0.18f), bodyColor);
        CreatePart("TopBeam", new Vector3(0f, 2.55f, 0f), new Vector3(2f, 0.16f, 0.2f), dangerColor * 0.8f);
        CreatePart("BeamGlow", new Vector3(0f, 2.4f, 0f), new Vector3(1.85f, 0.06f, 0.14f), dangerColor);
        CreatePart("CapL", new Vector3(-0.9f, 2.62f, 0f), new Vector3(0.18f, 0.08f, 0.18f), dangerColor);
        CreatePart("CapR", new Vector3(0.9f, 2.62f, 0f), new Vector3(0.18f, 0.08f, 0.18f), dangerColor);
    }

    // ── Moving: 浮游哨兵 ──
    private void BuildMoving()
    {
        CreatePart("Core", new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 0.4f, 0.4f), sentinelColor);
        CreatePart("FrameTop", new Vector3(0f, 1.48f, 0f), new Vector3(0.7f, 0.05f, 0.05f), sentinelColor * 1.2f);
        CreatePart("FrameBottom", new Vector3(0f, 0.92f, 0f), new Vector3(0.7f, 0.05f, 0.05f), sentinelColor * 1.2f);
        CreatePart("FrameL", new Vector3(-0.35f, 1.2f, 0f), new Vector3(0.05f, 0.56f, 0.05f), sentinelColor * 1.2f);
        CreatePart("FrameR", new Vector3(0.35f, 1.2f, 0f), new Vector3(0.05f, 0.56f, 0.05f), sentinelColor * 1.2f);
        Transform ring = CreatePart("Ring", new Vector3(0f, 1.35f, 0f), new Vector3(0.55f, 0.04f, 0.55f), sentinelColor * 0.5f);
        ring.localEulerAngles = new Vector3(90f, 0f, 0f);
    }

    // ── Rotating: 切割环 ──
    private void BuildRotating()
    {
        CreatePart("Hub", new Vector3(0f, 1.1f, 0f), new Vector3(0.2f, 0.2f, 0.2f), bodyColor);
        CreatePart("BladeN", new Vector3(0f, 1.1f, 0.7f), new Vector3(0.06f, 0.22f, 0.65f), dangerColor);
        CreatePart("BladeS", new Vector3(0f, 1.1f, -0.7f), new Vector3(0.06f, 0.22f, 0.65f), dangerColor);
        CreatePart("BladeE", new Vector3(0.7f, 1.1f, 0f), new Vector3(0.65f, 0.22f, 0.06f), dangerColor);
        CreatePart("BladeW", new Vector3(-0.7f, 1.1f, 0f), new Vector3(0.65f, 0.22f, 0.06f), dangerColor);
        CreatePart("TipN", new Vector3(0f, 1.1f, 1.05f), new Vector3(0.08f, 0.08f, 0.08f), dangerColor * 1.5f);
        CreatePart("TipS", new Vector3(0f, 1.1f, -1.05f), new Vector3(0.08f, 0.08f, 0.08f), dangerColor * 1.5f);
        CreatePart("TipE", new Vector3(1.05f, 1.1f, 0f), new Vector3(0.08f, 0.08f, 0.08f), dangerColor * 1.5f);
        CreatePart("TipW", new Vector3(-1.05f, 1.1f, 0f), new Vector3(0.08f, 0.08f, 0.08f), dangerColor * 1.5f);
        float r = 0.95f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            CreatePart($"Ring{i}", new Vector3(Mathf.Cos(angle) * r, 1.1f, Mathf.Sin(angle) * r),
                new Vector3(0.04f, 0.04f, 0.04f), dangerColor * 0.8f);
        }
    }

    // ── Slow: 减速力场 ──
    private void BuildSlow()
    {
        CreatePart("Floor", new Vector3(0f, 0.02f, 0f), new Vector3(2f, 0.04f, 2.5f), new Color(0.02f, 0.06f, 0.12f));
        CreatePart("EdgeN", new Vector3(0f, 0.04f, 1.25f), new Vector3(2f, 0.03f, 0.06f), techColor);
        CreatePart("EdgeS", new Vector3(0f, 0.04f, -1.25f), new Vector3(2f, 0.03f, 0.06f), techColor);
        CreatePart("EdgeE", new Vector3(1f, 0.04f, 0f), new Vector3(0.06f, 0.03f, 2.5f), techColor);
        CreatePart("EdgeW", new Vector3(-1f, 0.04f, 0f), new Vector3(0.06f, 0.03f, 2.5f), techColor);
        CreatePart("GridC", new Vector3(0f, 0.04f, 0f), new Vector3(2f, 0.02f, 0.04f), techColor * 0.5f);
        CreatePart("GridL", new Vector3(-0.65f, 0.04f, 0f), new Vector3(0.04f, 0.02f, 2.5f), techColor * 0.4f);
        CreatePart("GridR", new Vector3(0.65f, 0.04f, 0f), new Vector3(0.04f, 0.02f, 2.5f), techColor * 0.4f);
        CreatePart("DotNE", new Vector3(1f, 0.05f, 1.25f), new Vector3(0.07f, 0.03f, 0.07f), techColor * 1.2f);
        CreatePart("DotNW", new Vector3(-1f, 0.05f, 1.25f), new Vector3(0.07f, 0.03f, 0.07f), techColor * 1.2f);
        CreatePart("DotSE", new Vector3(1f, 0.05f, -1.25f), new Vector3(0.07f, 0.03f, 0.07f), techColor * 1.2f);
        CreatePart("DotSW", new Vector3(-1f, 0.05f, -1.25f), new Vector3(0.07f, 0.03f, 0.07f), techColor * 1.2f);
    }

    // ── Knockback: 弹射板 ──
    private void BuildKnockback()
    {
        CreatePart("Base", new Vector3(0f, 0.06f, 0f), new Vector3(1.5f, 0.12f, 1f), bodyColor);
        Transform ramp = CreatePart("Ramp", new Vector3(0f, 0.18f, 0.15f), new Vector3(1.3f, 0.06f, 0.7f), boostColor);
        ramp.localEulerAngles = new Vector3(-18f, 0f, 0f);
        CreatePart("ArrowV", new Vector3(0f, 0.22f, -0.1f), new Vector3(0.06f, 0.03f, 0.4f), boostColor * 1.3f);
        CreatePart("ArrowL", new Vector3(-0.15f, 0.22f, -0.2f), new Vector3(0.06f, 0.03f, 0.25f), boostColor * 1.3f);
        CreatePart("ArrowR", new Vector3(0.15f, 0.22f, -0.2f), new Vector3(0.06f, 0.03f, 0.25f), boostColor * 1.3f);
        CreatePart("EdgeN", new Vector3(0f, 0.1f, 0.5f), new Vector3(1.5f, 0.04f, 0.05f), boostColor);
        CreatePart("EdgeS", new Vector3(0f, 0.1f, -0.5f), new Vector3(1.5f, 0.04f, 0.05f), boostColor);
        CreatePart("EdgeE", new Vector3(0.75f, 0.1f, 0f), new Vector3(0.05f, 0.04f, 1f), boostColor);
        CreatePart("EdgeW", new Vector3(-0.75f, 0.1f, 0f), new Vector3(0.05f, 0.04f, 1f), boostColor);
        CreatePart("BoltNE", new Vector3(0.75f, 0.15f, 0.5f), new Vector3(0.1f, 0.06f, 0.1f), boostColor * 1.2f);
        CreatePart("BoltNW", new Vector3(-0.75f, 0.15f, 0.5f), new Vector3(0.1f, 0.06f, 0.1f), boostColor * 1.2f);
        CreatePart("BoltSE", new Vector3(0.75f, 0.15f, -0.5f), new Vector3(0.1f, 0.06f, 0.1f), boostColor * 1.2f);
        CreatePart("BoltSW", new Vector3(-0.75f, 0.15f, -0.5f), new Vector3(0.1f, 0.06f, 0.1f), boostColor * 1.2f);
    }

    // ───────────────────── 建模工具 ─────────────────────

    /// <summary>
    /// 创建一个几何部件（Cube），去掉碰撞体。颜色稍后在 ApplyMaterialAndColors 中设置。
    /// </summary>
    private Transform CreatePart(string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        Collider c = part.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        // 先用默认材质暂存颜色（后面 ApplyMaterialAndColors 会替换材质并写入）
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = null; // 清除默认材质，避免混乱
        }

        // 把颜色存到名字后缀里，供后续使用
        // （实际颜色通过 partOriginalColors 数组按顺序对应）
        return part.transform;
    }

    // ───────────────────── 材质 ─────────────────────

    private Material CreateNeonMaterial()
    {
        // Use a project-owned shader so runtime-created obstacle materials survive Player stripping.
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

        return shader != null ? new Material(shader) : null;
    }

    /// <summary>
    /// 给每个 Renderer 创建独立材质实例并写入颜色。
    /// 同时记录原始色用于 Update 中的呼吸脉冲。
    /// </summary>
    private void ApplyMaterialAndColors()
    {
        if (neonMaterial == null || allRenderers == null)
        {
            return;
        }

        partOriginalColors = new Color[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null)
            {
                continue;
            }

            // 独立材质实例
            r.material = new Material(neonMaterial);

            // 根据部件名决定颜色
            Color color = ResolveColorForPart(r.gameObject.name);
            partOriginalColors[i] = color;
            r.material.SetColor("_BaseColor", color);
            r.material.SetColor("_Color", Color.white);
            r.material.SetColor("_EmissionColor", color * 0.65f);
            r.material.SetFloat("_EmissionPower", 1f);
        }
    }

    private Color ResolveColorForPart(string partName)
    {
        // 根据命名规则匹配颜色
        if (partName.Contains("Neon") || partName.Contains("Beam") || partName.Contains("Blade")
            || partName.Contains("Tip") || partName.Contains("Ring") || partName.Contains("Beacon")
            || partName.Contains("Glow") || partName.Contains("Cap"))
        {
            // 发光部件：根据障碍物类型返回霓虹色
            return obstacle.Type switch
            {
                ObstacleType.Low        => warningColor,
                ObstacleType.Moving     => sentinelColor,
                ObstacleType.Slow       => techColor,
                ObstacleType.Knockback  => boostColor,
                _                       => dangerColor, // LaneBlock / High / Rotating
            };
        }

        if (partName.Contains("Edge") || partName.Contains("Grid") || partName.Contains("Dot")
            || partName.Contains("Arrow") || partName.Contains("Ramp") || partName.Contains("Bolt"))
        {
            return obstacle.Type switch
            {
                ObstacleType.Slow       => techColor,
                ObstacleType.Knockback  => boostColor,
                _                       => warningColor,
            };
        }

        if (partName == "Core" || partName.Contains("Frame"))
        {
            return sentinelColor;
        }

        // 主体 / 柱子 / 底座 → 暗色
        return bodyColor;
    }
}
