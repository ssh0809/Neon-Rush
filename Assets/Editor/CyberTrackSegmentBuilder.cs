using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CyberTrackSegmentBuilder
{
    private const string KitRoot = "Assets/ThirdParty/Quaternius/CyberpunkGameKit";
    private const string DemoMaterialFolder = "Assets/Cyberpunk Game Kit/Materials";
    private const string MaterialFolder = "Assets/Materials";
    private const string PrefabFolder = "Assets/Prefabs/TrackSegments";
    private const string CyberSegmentPath = PrefabFolder + "/TrackSegment_Cyberpunk.prefab";
    private const string EnvironmentName = "CyberpunkEnvironment";
    private const string GeneratedEnvironmentName = "GeneratedKitEnvironment";
    private const string GameplayName = "Gameplay";
    private const string EndAnchorName = "EndAnchor";
    private const string EnvironmentLayerName = "CyberEnvironment";
    private const float SegmentLength = 48f;
    private const float RunwayClearHalfWidth = 5.4f;

    private static Material roadMaterial;
    private static Material laneMaterial;
    private static Material edgeMaterial;
    private static Material gateRedMaterial;
    private static Material lightMaterial;
    private static Material screenMaterial;
    private static Material warmMaterial;

    [MenuItem("Tools/Neon Rush/Build Cyber Track Segment")]
    public static void BuildCyberTrackSegment()
    {
        EnsureProjectFolders();
        LoadMaterials();

        GameObject root = new GameObject("TrackSegment_Cyberpunk");
        TrackSegment segment = root.AddComponent<TrackSegment>();
        ApplyLayerRecursive(root, EnvironmentLayerName);

        Transform gameplayRoot = CreateChild(root.transform, GameplayName).transform;
        Transform environmentRoot = CreateChild(root.transform, EnvironmentName).transform;
        Transform endAnchor = CreateChild(root.transform, EndAnchorName).transform;
        endAnchor.localPosition = new Vector3(0f, 0f, SegmentLength);

        List<Renderer> themedRenderers = BuildGameplay(gameplayRoot);
        BuildGeneratedEnvironment(environmentRoot);
        ConfigureTrackSegment(segment, endAnchor, themedRenderers.ToArray());

        PrefabUtility.SaveAsPrefabAsset(root, CyberSegmentPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Cyber track segment built: {CyberSegmentPath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(CyberSegmentPath);
    }

    [MenuItem("Tools/Neon Rush/Rebuild Generated Cyberpunk Environment")]
    public static void RebuildGeneratedCyberpunkEnvironment()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CyberSegmentPath);
        if (prefab == null)
        {
            BuildCyberTrackSegment();
            return;
        }

        EnsureProjectFolders();
        LoadMaterials();

        GameObject contents = PrefabUtility.LoadPrefabContents(CyberSegmentPath);
        try
        {
            TrackSegment segment = contents.GetComponent<TrackSegment>();
            if (segment == null)
            {
                segment = contents.AddComponent<TrackSegment>();
            }

            Transform gameplayRoot = FindOrCreateChild(contents.transform, GameplayName);
            Transform environmentRoot = FindOrCreateChild(contents.transform, EnvironmentName);
            Transform endAnchor = FindOrCreateChild(contents.transform, EndAnchorName);

            ClearChildren(gameplayRoot);
            ClearChildren(environmentRoot);

            endAnchor.localPosition = new Vector3(0f, 0f, SegmentLength);
            List<Renderer> themedRenderers = BuildGameplay(gameplayRoot);
            BuildGeneratedEnvironment(environmentRoot);
            ConfigureTrackSegment(segment, endAnchor, themedRenderers.ToArray());
            ApplyLayerRecursive(contents, EnvironmentLayerName);
            PrefabUtility.SaveAsPrefabAsset(contents, CyberSegmentPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated cyberpunk environment rebuilt from CyberpunkGameKit FBX assets.");
    }

    [MenuItem("Tools/Neon Rush/Use Cyber Track Segment In Open Scene")]
    public static void UseCyberTrackSegmentInOpenScene()
    {
        TrackSegment cyberSegment = AssetDatabase.LoadAssetAtPath<TrackSegment>(CyberSegmentPath);
        if (cyberSegment == null)
        {
            BuildCyberTrackSegment();
            cyberSegment = AssetDatabase.LoadAssetAtPath<TrackSegment>(CyberSegmentPath);
        }

        if (cyberSegment == null)
        {
            Debug.LogError($"Could not load cyber segment prefab at {CyberSegmentPath}.");
            return;
        }

        TrackSpawner spawner = Object.FindAnyObjectByType<TrackSpawner>();
        if (spawner == null)
        {
            Debug.LogError("No TrackSpawner found in the open scene.");
            return;
        }

        Undo.RecordObject(spawner, "Use Cyber Track Segment");
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SerializedProperty prefabs = serializedSpawner.FindProperty("segmentPrefabs");
        prefabs.ClearArray();
        prefabs.InsertArrayElementAtIndex(0);
        prefabs.GetArrayElementAtIndex(0).objectReferenceValue = cyberSegment;

        SerializedProperty defaultSegmentLength = serializedSpawner.FindProperty("defaultSegmentLength");
        if (defaultSegmentLength != null)
        {
            defaultSegmentLength.floatValue = SegmentLength;
        }

        serializedSpawner.ApplyModifiedProperties();

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log("TrackSpawner now uses TrackSegment_Cyberpunk.");
    }

    [MenuItem("Tools/Neon Rush/Apply Cyber Lighting To Open Scene")]
    public static void ApplyCyberLightingToOpenScene()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.055f, 0.07f, 0.12f);
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 92f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.16f, 0.19f, 0.26f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.085f, 0.14f);
        RenderSettings.ambientGroundColor = new Color(0.045f, 0.04f, 0.065f);
        RenderSettings.ambientIntensity = 1.05f;

        Light directionalLight = FindDirectionalLight();
        if (directionalLight != null && directionalLight.type == LightType.Directional)
        {
            Undo.RecordObject(directionalLight, "Apply Cyber Lighting");
            directionalLight.color = new Color(0.62f, 0.74f, 1f);
            directionalLight.intensity = 0.35f;
            directionalLight.shadows = LightShadows.None;
            EditorUtility.SetDirty(directionalLight);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Undo.RecordObject(mainCamera, "Apply Cyber Camera Background");
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.012f, 0.016f, 0.034f);
            EditorUtility.SetDirty(mainCamera);

            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData =
                mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                Undo.RecordObject(cameraData, "Enable Camera Post Processing");
                cameraData.renderPostProcessing = true;
                EditorUtility.SetDirty(cameraData);
            }
        }

        UnityEngine.Rendering.Volume globalVolume = FindOrCreateGlobalVolume();
        if (globalVolume != null && globalVolume.sharedProfile != null)
        {
            Undo.RecordObject(globalVolume.sharedProfile, "Tune Cyber Bloom");
            if (globalVolume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
            {
                bloom.active = true;
                bloom.threshold.Override(0.65f);
                bloom.intensity.Override(1.8f);
                bloom.scatter.Override(0.72f);
            }

            if (globalVolume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments))
            {
                colorAdjustments.active = true;
                colorAdjustments.postExposure.Override(0.05f);
                colorAdjustments.contrast.Override(18f);
                colorAdjustments.saturation.Override(10f);
            }

            EditorUtility.SetDirty(globalVolume.sharedProfile);
            EditorUtility.SetDirty(globalVolume);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cyber lighting applied to open scene: fog, cool directional light, post processing, bloom, and color adjustments.");
    }

    private static List<Renderer> BuildGameplay(Transform gameplayRoot)
    {
        List<Renderer> themedRenderers = new List<Renderer>();
        float centerZ = SegmentLength * 0.5f;
        themedRenderers.Add(CreateCube(gameplayRoot, "Ground", new Vector3(0f, -0.1f, centerZ), new Vector3(8f, 0.18f, SegmentLength), roadMaterial, true));
        themedRenderers.Add(CreateCube(gameplayRoot, "LeftLaneLine", new Vector3(-1.25f, 0.015f, centerZ), new Vector3(0.08f, 0.05f, SegmentLength), laneMaterial, false));
        themedRenderers.Add(CreateCube(gameplayRoot, "RightLaneLine", new Vector3(1.25f, 0.015f, centerZ), new Vector3(0.08f, 0.05f, SegmentLength), laneMaterial, false));
        themedRenderers.Add(CreateCube(gameplayRoot, "LeftRunwayEdge", new Vector3(-3.75f, 0.04f, centerZ), new Vector3(0.12f, 0.08f, SegmentLength), edgeMaterial, false));
        themedRenderers.Add(CreateCube(gameplayRoot, "RightRunwayEdge", new Vector3(3.75f, 0.04f, centerZ), new Vector3(0.12f, 0.08f, SegmentLength), edgeMaterial, false));
        return themedRenderers;
    }

    private static void BuildGeneratedEnvironment(Transform environmentRoot)
    {
        GameObject generatedRoot = CreateChild(environmentRoot, GeneratedEnvironmentName);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedRoot.transform.localRotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        Transform leftRoot = CreateChild(generatedRoot.transform, "LeftSide").transform;
        Transform rightRoot = CreateChild(generatedRoot.transform, "RightSide").transform;

        BuildSide(leftRoot, -1f);
        BuildSide(rightRoot, 1f);
        AddOverheadCables(generatedRoot.transform);
        ApplyLayerRecursive(generatedRoot, EnvironmentLayerName);
    }

    private static void BuildSide(Transform parent, float side)
    {
        float faceRoad = side < 0f ? 90f : -90f;

        for (int i = 0; i < 5; i++)
        {
            float z = 5f + i * 10f;
            PlaceKitModel(parent, "Platforms/Platform_4x2.fbx", $"SidePlatform_{SideName(side)}_{i}", new Vector3(side * 5.05f, 0.02f, z), new Vector3(0f, 90f, 0f), Vector3.one * 1.35f, null, 5.7f);
            PlaceKitModel(parent, "Platforms/Rail_Long.fbx", $"Rail_{SideName(side)}_{i}", new Vector3(side * 4.65f, 0.12f, z), new Vector3(0f, 90f, 0f), Vector3.one * 1.35f, null, 4.2f);
        }

        for (int i = 0; i < 6; i++)
        {
            float z = 2.5f + i * 7.5f;
            GameObject light = PlaceKitModel(parent, "Platforms/Light_Street_1.fbx", $"StreetLight_{SideName(side)}_{i}", new Vector3(side * 5.15f, 0f, z), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.55f, lightMaterial, 4.0f);
            AddPointLight(light.transform, "StreetLightGlow", new Vector3(-side * 0.35f, 2.65f, 0f), i % 2 == 0 ? new Color(0.25f, 0.9f, 1f) : new Color(1f, 0.25f, 0.85f), 2.7f, 7f);
        }

        PlaceNeonModel(parent, "Platforms/Light_Square.fbx", $"EdgeLight_{SideName(side)}_A", new Vector3(side * 5.05f, 0.25f, 7f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.45f, lightMaterial, new Color(0.2f, 0.9f, 1f), 2f);
        PlaceNeonModel(parent, "Platforms/Light_Square.fbx", $"EdgeLight_{SideName(side)}_B", new Vector3(side * 5.05f, 0.25f, 23f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.45f, lightMaterial, new Color(1f, 0.25f, 0.8f), 2f);
        PlaceNeonModel(parent, "Platforms/Light_Square.fbx", $"EdgeLight_{SideName(side)}_C", new Vector3(side * 5.05f, 0.25f, 39f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.45f, lightMaterial, new Color(0.2f, 0.9f, 1f), 2f);
        PlaceNeonModel(parent, "Platforms/Sign_1.fbx", $"Sign_{SideName(side)}_A", new Vector3(side * 5.35f, 1.8f, 8f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.55f, screenMaterial, new Color(0.2f, 0.9f, 1f), 3f);
        PlaceNeonModel(parent, "Platforms/Sign_3.fbx", $"Sign_{SideName(side)}_B", new Vector3(side * 5.4f, 2.15f, 25f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.55f, gateRedMaterial, new Color(1f, 0.25f, 0.75f), 3f);
        PlaceNeonModel(parent, "Platforms/TV_1.fbx", $"TV_{SideName(side)}_A", new Vector3(side * 5.3f, 1.3f, 17f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.3f, screenMaterial, new Color(0.25f, 0.85f, 1f), 2.8f);
        PlaceNeonModel(parent, "Platforms/Computer_Large.fbx", $"Computer_{SideName(side)}", new Vector3(side * 5.65f, 0f, 41.5f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.15f, screenMaterial, new Color(0.2f, 0.7f, 1f), 2.8f);

        PlaceKitModel(parent, "Platforms/AC_Stacked.fbx", $"ACStack_{SideName(side)}", new Vector3(side * 5.8f, 0.2f, 4f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.2f, null, 2.7f);
        PlaceKitModel(parent, "Platforms/Pipe_2.fbx", $"Pipe_{SideName(side)}_A", new Vector3(side * 5.45f, 0.4f, 14f), new Vector3(0f, 0f, 90f), new Vector3(1.1f, 1.1f, 2.1f), null, 3.5f);
        PlaceKitModel(parent, "Platforms/Pipe_2.fbx", $"Pipe_{SideName(side)}_B", new Vector3(side * 5.45f, 0.4f, 33f), new Vector3(0f, 0f, 90f), new Vector3(1.1f, 1.1f, 2.1f), null, 3.5f);
        PlaceKitModel(parent, "Platforms/Cable_Long.fbx", $"Cable_{SideName(side)}", new Vector3(side * 5.6f, 2.6f, 24f), new Vector3(0f, 0f, 90f), new Vector3(1.05f, 1.05f, 2.2f), null, 4.6f);
        PlaceKitModel(parent, "Platforms/Support_Long.fbx", $"Support_{SideName(side)}_A", new Vector3(side * 5.9f, 0f, 12f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.2f, null, 3.7f);
        PlaceKitModel(parent, "Platforms/Support_Long.fbx", $"Support_{SideName(side)}_B", new Vector3(side * 5.9f, 0f, 30f), new Vector3(0f, faceRoad, 0f), Vector3.one * 1.2f, null, 3.7f);
    }

    private static void AddOverheadCables(Transform parent)
    {
        PlaceKitModel(parent, "Platforms/Cable_Long.fbx", "OverheadCable_A", new Vector3(0f, 4.6f, 12f), new Vector3(0f, 0f, 90f), new Vector3(1.1f, 1.1f, 2.7f), null, 6.8f, false);
        PlaceKitModel(parent, "Platforms/Cable_Thick.fbx", "OverheadCable_B", new Vector3(0f, 5.0f, 28f), new Vector3(0f, 0f, 90f), new Vector3(1f, 1f, 2.5f), null, 6.8f, false);
    }

    private static void ConfigureTrackSegment(TrackSegment segment, Transform endAnchor, Renderer[] renderers)
    {
        SerializedObject serializedSegment = new SerializedObject(segment);
        serializedSegment.FindProperty("length").floatValue = SegmentLength;
        serializedSegment.FindProperty("endAnchor").objectReferenceValue = endAnchor;
        serializedSegment.FindProperty("configureColorGates").boolValue = false;
        serializedSegment.FindProperty("themeEmissionIntensity").floatValue = 2.2f;
        serializedSegment.FindProperty("tintThemedBaseColor").boolValue = false;
        serializedSegment.FindProperty("applyCyberTrackMaterial").boolValue = false;
        serializedSegment.FindProperty("cyberGridDensity").floatValue = 22f;
        serializedSegment.FindProperty("cyberFlowSpeed").floatValue = 2.8f;
        serializedSegment.FindProperty("buildProceduralScenery").boolValue = false;
        serializedSegment.FindProperty("environmentLayerName").stringValue = EnvironmentLayerName;

        SerializedProperty gateArray = serializedSegment.FindProperty("themedColorGates");
        gateArray.arraySize = 0;

        SerializedProperty rendererArray = serializedSegment.FindProperty("themedRenderers");
        rendererArray.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererArray.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }

        serializedSegment.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject PlaceNeonModel(Transform parent, string relativePath, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material, Color lightColor, float targetSize)
    {
        GameObject instance = PlaceKitModel(parent, relativePath, name, position, rotation, scale, material, targetSize);
        AddPointLight(instance.transform, "NeonGlow", Vector3.up * 0.8f, lightColor, 2.4f, 7f);
        return instance;
    }

    private static GameObject PlaceKitModel(Transform parent, string relativePath, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material overrideMaterial, float targetSize)
    {
        return PlaceKitModel(parent, relativePath, name, position, rotation, scale, overrideMaterial, targetSize, true);
    }

    private static GameObject PlaceKitModel(Transform parent, string relativePath, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material overrideMaterial, float targetSize, bool keepOutsideRunway)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitRoot}/{relativePath}");
        GameObject instance;
        if (asset != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.SetParent(parent, false);
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name + "_MissingModelFallback";
            instance.transform.SetParent(parent, false);
            Debug.LogWarning($"Missing model: {KitRoot}/{relativePath}");
        }

        instance.transform.localPosition = position;
        instance.transform.localEulerAngles = rotation;
        instance.transform.localScale = scale;
        RemoveColliders(instance);
        ConfigureRenderers(instance, overrideMaterial);
        FitLargestRendererDimension(instance, targetSize);
        if (keepOutsideRunway)
        {
            PushOutsideRunway(instance);
        }

        return instance;
    }

    private static Light FindDirectionalLight()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                return light;
            }
        }

        return null;
    }

    private static UnityEngine.Rendering.Volume FindOrCreateGlobalVolume()
    {
        UnityEngine.Rendering.Volume[] volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (UnityEngine.Rendering.Volume volume in volumes)
        {
            if (volume != null && volume.isGlobal)
            {
                EnsureCyberVolumeProfile(volume);
                return volume;
            }
        }

        GameObject volumeObject = new GameObject("Cyber Global Volume");
        Undo.RegisterCreatedObjectUndo(volumeObject, "Create Cyber Global Volume");
        UnityEngine.Rendering.Volume createdVolume = volumeObject.AddComponent<UnityEngine.Rendering.Volume>();
        createdVolume.isGlobal = true;
        createdVolume.priority = 10f;
        EnsureCyberVolumeProfile(createdVolume);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return createdVolume;
    }

    private static void EnsureCyberVolumeProfile(UnityEngine.Rendering.Volume volume)
    {
        if (volume.sharedProfile == null)
        {
            EnsureFolder("Assets", "Settings");
            string profilePath = "Assets/Settings/CyberGlobalVolumeProfile.asset";
            UnityEngine.Rendering.VolumeProfile profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
        }

        if (!volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.Bloom _))
        {
            volume.sharedProfile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        }

        if (!volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments _))
        {
            volume.sharedProfile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        }
    }

    private static void FitLargestRendererDimension(GameObject instance, float targetSize)
    {
        if (targetSize <= 0f)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largestDimension < 0.001f)
        {
            return;
        }

        float multiplier = targetSize / largestDimension;
        instance.transform.localScale *= multiplier;
    }

    private static void PushOutsideRunway(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float side = instance.transform.position.x < 0f ? -1f : 1f;
        Vector3 localPosition = instance.transform.localPosition;
        if (side < 0f && bounds.max.x > -RunwayClearHalfWidth)
        {
            localPosition.x -= bounds.max.x + RunwayClearHalfWidth;
        }
        else if (side > 0f && bounds.min.x < RunwayClearHalfWidth)
        {
            localPosition.x += RunwayClearHalfWidth - bounds.min.x;
        }

        instance.transform.localPosition = localPosition;
    }

    private static Renderer CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null && !keepCollider)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child : CreateChild(parent, name).transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void ConfigureRenderers(GameObject instance, Material overrideMaterial)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (overrideMaterial != null)
            {
                renderer.sharedMaterial = overrideMaterial;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static void RemoveColliders(GameObject instance)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void AddPointLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static void ApplyLayerRecursive(GameObject root, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            return;
        }

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.layer = layer;
        }
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "TrackSegments");
    }

    private static void LoadMaterials()
    {
        roadMaterial = LoadOrCreateMaterial("M_CyberRoad_Readable", new Color(0.055f, 0.062f, 0.075f), new Color(0.08f, 0.45f, 0.75f), 0.55f);
        laneMaterial = LoadOrCreateMaterial("M_CyberLane_Cyan", new Color(0.02f, 0.08f, 0.09f), new Color(0.2f, 1.2f, 1.35f), 2.2f);
        edgeMaterial = LoadOrCreateMaterial("M_CyberEdge_Magenta", new Color(0.085f, 0.035f, 0.075f), new Color(1.2f, 0.2f, 1.0f), 1.9f);
        gateRedMaterial = LoadOrCreateMaterial("M_CyberGate_Red", new Color(0.09f, 0.02f, 0.04f), new Color(1.3f, 0.12f, 0.4f), 2.3f);
        lightMaterial = LoadOrCreateMaterial("M_CyberKit_Light_Emissive", Color.white, Color.white, 2.5f);
        screenMaterial = LoadOrCreateMaterial("M_CyberKit_Screen_Emissive", new Color(0.18f, 0.42f, 0.55f), new Color(0.25f, 0.9f, 1.1f), 2.1f);
        warmMaterial = LoadOrCreateMaterial("M_CyberKit_Warm_Emissive", new Color(0.22f, 0.1f, 0.04f), new Color(1.2f, 0.55f, 0.12f), 1.8f);

        Material demoLight = AssetDatabase.LoadAssetAtPath<Material>($"{DemoMaterialFolder}/Light.mat");
        if (demoLight != null)
        {
            lightMaterial = demoLight;
        }

        Material demoScreen = AssetDatabase.LoadAssetAtPath<Material>($"{DemoMaterialFolder}/Screen.mat");
        if (demoScreen != null)
        {
            screenMaterial = demoScreen;
        }
    }

    private static Material LoadOrCreateMaterial(string name, Color baseColor, Color emissionColor, float emissionIntensity)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("DELTation/Toon Shader");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        SetColor(material, "_BaseColor", baseColor);
        SetColor(material, "_Color", baseColor);
        SetColor(material, "_EmissionColor", emissionColor * emissionIntensity);
        SetFloat(material, "_Emission", 1f);
        SetFloat(material, "_EnvironmentLightingEnabled", 1f);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetColor(Material material, string propertyName, Color color)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static string SideName(float side)
    {
        return side < 0f ? "Left" : "Right";
    }
}
