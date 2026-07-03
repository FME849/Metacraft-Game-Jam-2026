using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>
/// Builds Assets/Scenes/YarnSpinnerTest.unity: two colored squares whose
/// conversation is driven by the Yarn Spinner Dialogue System prefab.
/// Run from the menu (Tools > Build Yarn Spinner Test Scene) or via
/// -executeMethod YarnSpinnerTestSceneBuilder.BuildScene in batch mode.
/// </summary>
public static class YarnSpinnerTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/YarnSpinnerTest.unity";
    const string SquareSpritePath = "Assets/Sprites/WhiteSquare.png";
    const string YarnProjectPath = "Assets/Dialogue/GameDialogue.yarnproject";
    const string DialogueSystemPrefabPath = "Packages/dev.yarnspinner.unity/Prefabs/Dialogue System.prefab";

    [MenuItem("Tools/Build Yarn Spinner Test Scene")]
    public static void BuildScene()
    {
        try
        {
            Build();
            Debug.Log("SCENE_BUILD_OK");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("SCENE_BUILD_FAILED: " + e);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.13f, 0.13f, 0.17f);
        camera.GetUniversalAdditionalCameraData();

        var squareSprite = GetOrCreateSquareSprite();
        CreateSquare("Red Square", squareSprite, new Vector3(-2.5f, 0f, 0f), new Color(0.9f, 0.3f, 0.3f));
        CreateSquare("Blue Square", squareSprite, new Vector3(2.5f, 0f, 0f), new Color(0.3f, 0.55f, 0.95f));

        // The Dialogue System prefab already includes its own EventSystem +
        // InputSystemUIInputModule child object, so we must not add a second
        // one here — a duplicate EventSystem silently breaks all UI input.
        var dialogueSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);
        if (dialogueSystemPrefab == null)
        {
            throw new FileNotFoundException($"Yarn Spinner prefab not found at {DialogueSystemPrefabPath}. Is the package installed?");
        }
        var dialogueSystem = (GameObject)PrefabUtility.InstantiatePrefab(dialogueSystemPrefab, scene);

        var yarnProject = AssetDatabase.LoadAssetAtPath<YarnProject>(YarnProjectPath);
        if (yarnProject == null)
        {
            throw new FileNotFoundException($"Yarn Project not found at {YarnProjectPath}.");
        }

        var runner = dialogueSystem.GetComponentInChildren<DialogueRunner>();
        var serializedRunner = new SerializedObject(runner);
        serializedRunner.FindProperty("yarnProject").objectReferenceValue = yarnProject;
        serializedRunner.ApplyModifiedPropertiesWithoutUndo();
        runner.autoStart = true;
        runner.startNode = "YarnSpinnerTest";
        EditorUtility.SetDirty(runner);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new IOException($"Failed to save scene to {ScenePath}");
        }
        AssetDatabase.SaveAssets();
    }

    static void CreateSquare(string name, Sprite sprite, Vector3 position, Color color)
    {
        var square = new GameObject(name);
        square.transform.position = position;
        square.transform.localScale = new Vector3(2f, 2f, 1f);
        var renderer = square.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
    }

    static Sprite GetOrCreateSquareSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SquareSpritePath));
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color32[64 * 64];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(SquareSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
    }
}
