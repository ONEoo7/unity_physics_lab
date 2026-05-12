using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhysicsLab.Core;
using PhysicsLab.Experiments.Chladni;
using PhysicsLab.Framework;
using PhysicsLab.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PhysicsLab.EditorTools
{
    public static class LabScaffolder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenesFolder = ProjectRoot + "/Scenes";
        private const string ExperimentsFolder = ProjectRoot + "/Experiments/Chladni";
        private const string ArtFolder = ProjectRoot + "/Art";
        private const string MaterialsFolder = ArtFolder + "/Materials";
        private const string MeshesFolder = ArtFolder + "/Meshes";
        private const string SettingsFolder = ProjectRoot + "/Settings";

        private const string LabScenePath = ScenesFolder + "/Lab.unity";
        private const string ChladniScenePath = ScenesFolder + "/Experiments/Chladni.unity";
        private const string ChladniDefinitionPath = SettingsFolder + "/ChladniDefinition.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/Physics Lab/Scaffold Scenes", priority = 0)]
        public static void ScaffoldScenes()
        {
            if (!EditorUtility.DisplayDialog(
                    "Scaffold Physics Lab",
                    "This will (re)create Lab.unity, Chladni.unity, the Chladni ExperimentDefinition, "
                    + "and supporting materials/meshes under Assets/_Project. Overwrite existing files?",
                    "Yes, scaffold", "Cancel"))
                return;

            EnsureFolders();

            var definition = CreateOrUpdateDefinition();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
            {
                Debug.LogWarning(
                    $"[LabScaffolder] Could not load InputActionAsset at {InputActionsPath}; "
                    + "controllers will have a null asset reference.");
            }

            BuildLabScene(definition, inputAsset);
            BuildChladniScene(definition, inputAsset);
            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Physics Lab",
                "Lab and Chladni scenes scaffolded. Open Assets/_Project/Scenes/Lab.unity and press Play.",
                "Got it");
        }

        [MenuItem("Tools/Physics Lab/Add Scenes to Build Settings")]
        public static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            EnsureSceneInBuild(scenes, LabScenePath);
            EnsureSceneInBuild(scenes, ChladniScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureSceneInBuild(List<EditorBuildSettingsScene> scenes, string path)
        {
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        // -----------------------------------------------------------------
        // Asset folders, definition, shared assets
        // -----------------------------------------------------------------

        private static void EnsureFolders()
        {
            foreach (var path in new[] {
                ProjectRoot, ScenesFolder, ScenesFolder + "/Experiments",
                ExperimentsFolder, ArtFolder, MaterialsFolder, MeshesFolder, SettingsFolder })
            {
                EnsureFolder(path);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static ExperimentDefinition CreateOrUpdateDefinition()
        {
            var def = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>(ChladniDefinitionPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ExperimentDefinition>();
                AssetDatabase.CreateAsset(def, ChladniDefinitionPath);
            }
            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = "chladni";
            so.FindProperty("title").stringValue = "Chladni Plate";
            so.FindProperty("description").stringValue =
                "Salt on a vibrating plate. Sweep the frequency to see grains migrate to nodal lines.";
            so.FindProperty("sceneName").stringValue = "Chladni";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        // -----------------------------------------------------------------
        // Lab scene
        // -----------------------------------------------------------------

        private static void BuildLabScene(
            ExperimentDefinition chladniDefinition,
            InputActionAsset inputAsset)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting.
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.20f);

            // Room.
            var roomMat = CreateOrLoadMaterial("LabRoom", new Color(0.78f, 0.78f, 0.80f));
            BuildRoom(roomMat, size: new Vector3(12f, 3.2f, 12f));

            // Player.
            var player = CreatePlayer(inputAsset);

            // LabManager.
            var labManagerGo = new GameObject("LabManager");
            labManagerGo.AddComponent<LabManager>();

            // HUD canvas with interaction prompt.
            var hud = CreateLabHud(out var promptUI);

            // Wire prompt to interactor.
            var interactor = player.GetComponentInChildren<Interactor>();
            var soPrompt = new SerializedObject(promptUI);
            soPrompt.FindProperty("interactor").objectReferenceValue = interactor;
            soPrompt.ApplyModifiedPropertiesWithoutUndo();

            // LabPlayer wiring.
            var labPlayer = player.GetComponent<LabPlayer>();
            var soPlayer = new SerializedObject(labPlayer);
            soPlayer.FindProperty("hubHud").objectReferenceValue = hud;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            // Station pedestal.
            CreateStation(chladniDefinition, new Vector3(2.0f, 0f, 2.0f));

            // Skybox / volume — keep default URP volume profile.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LabScenePath);
        }

        private static void BuildRoom(Material mat, Vector3 size)
        {
            // Floor.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(size.x / 10f, 1f, size.z / 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Ceiling.
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.position = new Vector3(0f, size.y, 0f);
            ceiling.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            ceiling.transform.localScale = new Vector3(size.x / 10f, 1f, size.z / 10f);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Walls.
            CreateWall("Wall+X", new Vector3(size.x * 0.5f, size.y * 0.5f, 0f), Quaternion.Euler(0f, 0f, 90f),
                new Vector3(size.y / 10f, 1f, size.z / 10f), mat);
            CreateWall("Wall-X", new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), Quaternion.Euler(0f, 0f, -90f),
                new Vector3(size.y / 10f, 1f, size.z / 10f), mat);
            CreateWall("Wall+Z", new Vector3(0f, size.y * 0.5f, size.z * 0.5f), Quaternion.Euler(-90f, 0f, 0f),
                new Vector3(size.x / 10f, 1f, size.y / 10f), mat);
            CreateWall("Wall-Z", new Vector3(0f, size.y * 0.5f, -size.z * 0.5f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(size.x / 10f, 1f, size.y / 10f), mat);
        }

        private static void CreateWall(string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Plane);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.rotation = rot;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static GameObject CreatePlayer(InputActionAsset inputAsset)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, -3f);

            var controllerComp = player.AddComponent<CharacterController>();
            controllerComp.height = 1.8f;
            controllerComp.radius = 0.3f;
            controllerComp.center = new Vector3(0f, 0.9f, 0f);

            var fps = player.AddComponent<FirstPersonController>();
            var interactor = new GameObject("Interactor").AddComponent<Interactor>();
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            interactor.transform.SetParent(pivot.transform, false);

            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            cam.transform.SetParent(pivot.transform, false);
            cam.transform.localPosition = Vector3.zero;
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;

            var soFps = new SerializedObject(fps);
            soFps.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
            soFps.FindProperty("inputActions").objectReferenceValue = inputAsset;
            soFps.ApplyModifiedPropertiesWithoutUndo();

            var soInt = new SerializedObject(interactor);
            soInt.FindProperty("viewCamera").objectReferenceValue = cam;
            soInt.FindProperty("inputActions").objectReferenceValue = inputAsset;
            soInt.ApplyModifiedPropertiesWithoutUndo();

            var labPlayer = player.AddComponent<LabPlayer>();
            var soLp = new SerializedObject(labPlayer);
            soLp.FindProperty("controller").objectReferenceValue = fps;
            soLp.FindProperty("interactor").objectReferenceValue = interactor;
            soLp.FindProperty("playerView").objectReferenceValue = pivot;
            soLp.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static GameObject CreateLabHud(out InteractionPromptUI promptUI)
        {
            var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Center crosshair.
            CreateUiText(hud.transform, "Crosshair", "·",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                fontSize: 36, alignment: TextAlignmentOptions.Center);

            // Prompt.
            var prompt = new GameObject("Prompt",
                typeof(RectTransform), typeof(CanvasGroup), typeof(InteractionPromptUI));
            prompt.transform.SetParent(hud.transform, false);
            var rt = prompt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.15f);
            rt.anchorMax = new Vector2(0.5f, 0.15f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 60f);

            var label = CreateUiText(prompt.transform, "Label", "[E] Interact",
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero,
                fontSize: 28, alignment: TextAlignmentOptions.Center);

            promptUI = prompt.GetComponent<InteractionPromptUI>();
            var soPrompt = new SerializedObject(promptUI);
            soPrompt.FindProperty("canvasGroup").objectReferenceValue = prompt.GetComponent<CanvasGroup>();
            soPrompt.FindProperty("label").objectReferenceValue = label;
            soPrompt.ApplyModifiedPropertiesWithoutUndo();

            // Make sure an EventSystem exists for UGUI.
            if (Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 0)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
            return hud;
        }

        private static void CreateStation(ExperimentDefinition def, Vector3 position)
        {
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = $"Station_{def.Title}";
            pedestal.transform.position = position + new Vector3(0f, 0.5f, 0f);
            pedestal.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);

            var mat = CreateOrLoadMaterial("Station", new Color(0.22f, 0.45f, 0.78f));
            pedestal.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var station = pedestal.AddComponent<ExperimentStation>();
            var so = new SerializedObject(station);
            so.FindProperty("definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();

            // World-space label above the pedestal.
            var labelGo = new GameObject("Label", typeof(Canvas));
            labelGo.transform.SetParent(pedestal.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            labelGo.transform.localScale = Vector3.one * 0.01f;
            var c = labelGo.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = labelGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 80f);
            CreateUiText(labelGo.transform, "Text", def.Title,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero,
                fontSize: 36, alignment: TextAlignmentOptions.Center);
        }

        // -----------------------------------------------------------------
        // Chladni scene
        // -----------------------------------------------------------------

        private static void BuildChladniScene(
            ExperimentDefinition definition,
            InputActionAsset inputAsset)
        {
            EnsureFolder(Path.GetDirectoryName(ChladniScenePath).Replace("\\", "/"));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Ambient lighting.
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            sun.transform.rotation = Quaternion.Euler(60f, 20f, 0f);

            // Plate visual.
            var plateRoot = new GameObject("Plate");
            plateRoot.transform.position = new Vector3(0f, 0.9f, 0f);

            var plateMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plateMesh.name = "Mesh";
            plateMesh.transform.SetParent(plateRoot.transform, false);
            plateMesh.transform.localScale = new Vector3(1.0f, 0.02f, 1.0f);
            plateMesh.GetComponent<MeshRenderer>().sharedMaterial =
                CreateOrLoadMaterial("Plate", new Color(0.10f, 0.10f, 0.12f));
            Object.DestroyImmediate(plateMesh.GetComponent<BoxCollider>());

            // Salt simulator on plate.
            var simulator = plateRoot.AddComponent<ChladniSaltSimulator>();
            var grainMesh = CreateOrSaveGrainMesh();
            var grainMat = CreateGrainMaterial();
            var soSim = new SerializedObject(simulator);
            soSim.FindProperty("plate").objectReferenceValue = plateMesh.transform;
            soSim.FindProperty("grainMesh").objectReferenceValue = grainMesh;
            soSim.FindProperty("grainMaterial").objectReferenceValue = grainMat;
            soSim.ApplyModifiedPropertiesWithoutUndo();

            // Sine tone on a sibling so it doesn't move with the plate visual wobble.
            var audioGo = new GameObject("SineTone", typeof(AudioSource));
            audioGo.transform.position = plateRoot.transform.position;
            var src = audioGo.GetComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.loop = true;
            // ChladniSineTone.Awake() creates a runtime silent clip and calls Play();
            // we can't serialize a runtime AudioClip into the scene, so it's done at runtime.
            var sine = audioGo.AddComponent<ChladniSineTone>();

            // Experiment root component.
            var experimentGo = new GameObject("ChladniExperiment");
            var experiment = experimentGo.AddComponent<ChladniExperiment>();

            // Camera.
            var cam = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 2.2f, -0.7f);
            cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            cam.GetComponent<Camera>().fieldOfView = 50f;

            // UI for controls + exit.
            var ui = CreateChladniUi(out var slider, out var audioToggle, out var resetButton,
                out var freqLabel, out var modeLabel, out var exitButton, inputAsset);

            var soExp = new SerializedObject(experiment);
            soExp.FindProperty("simulator").objectReferenceValue = simulator;
            soExp.FindProperty("sineTone").objectReferenceValue = sine;
            soExp.FindProperty("plateVisual").objectReferenceValue = plateMesh.transform;
            soExp.FindProperty("frequencySlider").objectReferenceValue = slider;
            soExp.FindProperty("audioToggle").objectReferenceValue = audioToggle;
            soExp.FindProperty("resetButton").objectReferenceValue = resetButton;
            soExp.FindProperty("frequencyLabel").objectReferenceValue = freqLabel;
            soExp.FindProperty("modeLabel").objectReferenceValue = modeLabel;

            // ExperimentBase.definition is private in the base class — set via SerializedObject.
            var defProp = soExp.FindProperty("definition");
            if (defProp != null) defProp.objectReferenceValue = definition;
            soExp.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ChladniScenePath);
        }

        private static GameObject CreateChladniUi(
            out Slider slider, out Toggle audioToggle, out Button resetButton,
            out TMP_Text freqLabel, out TMP_Text modeLabel, out Button exitButton,
            InputActionAsset inputAsset)
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel anchored bottom-center.
            var panel = new GameObject("ControlPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0f);
            prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 32f);
            prt.sizeDelta = new Vector2(800f, 220f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            freqLabel = CreateUiText(panel.transform, "FreqLabel", "220 Hz",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -32f),
                fontSize: 36, alignment: TextAlignmentOptions.Center);
            var frt = (RectTransform)freqLabel.transform;
            frt.sizeDelta = new Vector2(0f, 48f);

            modeLabel = CreateUiText(panel.transform, "ModeLabel", "(1, 2)",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f),
                fontSize: 22, alignment: TextAlignmentOptions.Center);
            var mrt = (RectTransform)modeLabel.transform;
            mrt.sizeDelta = new Vector2(0f, 32f);
            modeLabel.color = new Color(0.85f, 0.85f, 0.85f);

            slider = CreateUiSlider(panel.transform, "FrequencySlider",
                new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.55f));
            slider.minValue = 100f;
            slider.maxValue = 1850f;
            slider.value = 220f;

            audioToggle = CreateUiToggle(panel.transform, "AudioToggle", "Audio",
                new Vector2(0.05f, 0.05f), new Vector2(0.3f, 0.25f));

            resetButton = CreateUiButton(panel.transform, "ResetButton", "Reset",
                new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.25f));

            exitButton = CreateUiButton(panel.transform, "ExitButton", "Back to Lab",
                new Vector2(0.70f, 0.05f), new Vector2(0.95f, 0.25f));
            var exit = exitButton.gameObject.AddComponent<ExitExperimentButton>();
            var soExit = new SerializedObject(exit);
            soExit.FindProperty("button").objectReferenceValue = exitButton;
            soExit.FindProperty("inputActions").objectReferenceValue = inputAsset;
            soExit.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 0)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            return canvasGo;
        }

        // -----------------------------------------------------------------
        // Generic UI helpers
        // -----------------------------------------------------------------

        private static TMP_Text CreateUiText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored,
            int fontSize = 24, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchored;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Slider CreateUiSlider(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            FillParent(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.25f);
            faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = new Vector2(10f, 0f);
            faRt.offsetMax = new Vector2(-10f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            FillParent(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.3f, 0.55f, 0.95f);

            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10f, 0f);
            haRt.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20f, 0f);
            hRt.anchorMin = new Vector2(0f, 0f);
            hRt.anchorMax = new Vector2(0f, 1f);
            handle.GetComponent<Image>().color = Color.white;

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hRt;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle CreateUiToggle(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = new Vector2(16f, 0f);
            bgRt.sizeDelta = new Vector2(24f, 24f);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.22f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            FillParent(check.GetComponent<RectTransform>(), 4f);
            check.GetComponent<Image>().color = new Color(0.3f, 0.55f, 0.95f);

            var lbl = CreateUiText(go.transform, "Label", label,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 0f),
                fontSize: 22, alignment: TextAlignmentOptions.MidlineLeft);
            var lRt = (RectTransform)lbl.transform;
            lRt.offsetMin = new Vector2(40f, 0f);
            lRt.offsetMax = new Vector2(0f, 0f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static Button CreateUiButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            go.GetComponent<Image>().color = new Color(0.25f, 0.27f, 0.31f);

            CreateUiText(go.transform, "Label", label,
                Vector2.zero, Vector2.one, Vector2.zero,
                fontSize: 22, alignment: TextAlignmentOptions.Center);

            return go.AddComponent<Button>();
        }

        private static void FillParent(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        // -----------------------------------------------------------------
        // Materials & meshes
        // -----------------------------------------------------------------

        private static Material CreateOrLoadMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = lit;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateGrainMaterial()
        {
            var path = $"{MaterialsFolder}/SaltGrain.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (mat == null)
            {
                mat = new Material(unlit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = unlit;
            mat.enableInstancing = true;
            var saltColor = new Color(0.98f, 0.97f, 0.92f);
            mat.color = saltColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", saltColor);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh CreateOrSaveGrainMesh()
        {
            var path = $"{MeshesFolder}/SaltGrain.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;

            // Quad facing +Y (so it lies flat on top of the plate).
            mesh = new Mesh { name = "SaltGrain" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3( 0.5f, 0f, -0.5f),
                new Vector3( 0.5f, 0f,  0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
