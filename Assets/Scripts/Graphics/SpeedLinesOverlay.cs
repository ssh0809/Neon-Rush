using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class SpeedLinesOverlay : MonoBehaviour
{
    [SerializeField] private RunnerController runner;
    [SerializeField] private Shader overlayShader;
    [SerializeField] private Color tint = new Color(0.35f, 0.8f, 1f, 1f);
    [SerializeField] private float minSpeed = 9f;
    [SerializeField] private float fullIntensitySpeed = 17f;
    [SerializeField] private float maxIntensity = 0.55f;
    [SerializeField] private float overlayDistance = 1f;

    private Camera targetCamera;
    private Renderer overlayRenderer;
    private Material overlayMaterial;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (runner == null)
        {
            runner = FindAnyObjectByType<RunnerController>();
        }

        CreateOverlay();
    }

    private void LateUpdate()
    {
        if (overlayRenderer == null || overlayMaterial == null || targetCamera == null)
        {
            return;
        }

        float speed = runner != null ? runner.CurrentSpeed : minSpeed;
        float intensity = Mathf.InverseLerp(minSpeed, fullIntensitySpeed, speed) * maxIntensity;
        overlayMaterial.SetFloat("_Intensity", intensity);
        overlayMaterial.SetColor("_TintColor", tint);

        float height = 2f * overlayDistance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;
        overlayRenderer.transform.localPosition = new Vector3(0f, 0f, overlayDistance);
        overlayRenderer.transform.localRotation = Quaternion.identity;
        overlayRenderer.transform.localScale = new Vector3(width, height, 1f);
    }

    private void CreateOverlay()
    {
        Shader shader = overlayShader != null
            ? overlayShader
            : Shader.Find("NeonRush/SpeedLinesOverlay");
        if (shader == null)
        {
            Debug.LogWarning("[SpeedLinesOverlay] Missing shader: NeonRush/SpeedLinesOverlay");
            enabled = false;
            return;
        }

        overlayMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = "SpeedLinesOverlay";
        overlay.transform.SetParent(transform, false);

        Collider overlayCollider = overlay.GetComponent<Collider>();
        if (overlayCollider != null)
        {
            Destroy(overlayCollider);
        }

        overlayRenderer = overlay.GetComponent<Renderer>();
        overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        overlayRenderer.sharedMaterial = overlayMaterial;
    }
}
