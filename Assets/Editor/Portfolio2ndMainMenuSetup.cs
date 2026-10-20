using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// [AI] 게임 메인(타이틀) 화면 씬을 자동으로 만들어주는 에디터 스크립트.
// "게임 시작" 버튼 -> 2nd(게임) 씬으로 이동, "나가기" 버튼 -> 게임 종료.
// 여러 번 실행해도 안전하게(idempotent) 기존 MainMenu 씬을 열어서 다시 구성한다.
public static class Portfolio2ndMainMenuSetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplaySceneName = "2nd";
    private const string GameplayScenePath = "Assets/Scenes/2nd.unity";

    [MenuItem("Tools/Portfolio/2nd - Setup Main Menu (AI)")]
    public static void Setup()
    {
        // 지금 열려있는 씬에 저장 안 한 변경사항이 있으면 먼저 물어봐서 작업을 잃지 않게 한다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[AI] 저장하지 않아 메인 화면 설정을 취소했습니다.");
            return;
        }

        Scene scene;
        bool sceneExists = System.IO.File.Exists(MainMenuScenePath);

        if (sceneExists)
        {
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        SetupCamera();
        BuildUI();

        if (!EditorSceneManager.SaveScene(scene, MainMenuScenePath))
        {
            Debug.LogError("[AI] MainMenu 씬 저장에 실패했습니다: " + MainMenuScenePath);
            return;
        }

        UpdateBuildSettings();

        Debug.Log("[AI] 메인 화면(MainMenu) 씬 구성 완료! Build Settings에 MainMenu가 " +
                   "첫 번째 씬으로, 2nd가 두 번째 씬으로 등록되었습니다. " +
                   "지금 이 MainMenu 씬에서 Play를 눌러 확인해보세요: " +
                   "\"게임 시작\" -> 2nd 씬으로 이동, \"나가기\" -> 게임 종료(에디터에서는 Play 정지).");
    }

    private static void SetupCamera()
    {
        GameObject camGO = GameObject.Find("Main Camera");
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
        }

        Camera cam = camGO.GetComponent<Camera>();
        if (cam == null) cam = camGO.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
    }

    private static void BuildUI()
    {
        // 다시 실행해도 깨끗하게 되도록, 기존에 만들어둔 Canvas가 있으면 지우고 새로 만든다.
        GameObject existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ---- 제목 ----
        GameObject titleGO = new GameObject("TitleText", typeof(Text));
        titleGO.transform.SetParent(canvasGO.transform, false);
        Text titleText = titleGO.GetComponent<Text>();
        titleText.font = uiFont;
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = "똥 피하기";
        RectTransform rtTitle = titleGO.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0.5f, 0.68f);
        rtTitle.anchorMax = new Vector2(0.5f, 0.68f);
        rtTitle.pivot = new Vector2(0.5f, 0.5f);
        rtTitle.sizeDelta = new Vector2(900f, 150f);

        // ---- 버튼 동작을 담당할 컨트롤러 오브젝트 ----
        GameObject gmGO = GameObject.Find("MainMenuController");
        if (gmGO == null) gmGO = new GameObject("MainMenuController");
        MainMenuController controller = gmGO.GetComponent<MainMenuController>();
        if (controller == null) controller = gmGO.AddComponent<MainMenuController>();
        controller.gameSceneName = GameplaySceneName;

        // ---- 게임 시작 버튼 ----
        Button startButton = CreateButton(canvasGO.transform, uiFont, "StartButton", "게임 시작",
            new Vector2(0.5f, 0.45f), new Color(0.2f, 0.6f, 0.2f, 1f));
        UnityEventTools.AddVoidPersistentListener(startButton.onClick, controller.OnClickStart);

        // ---- 나가기 버튼 ----
        Button quitButton = CreateButton(canvasGO.transform, uiFont, "QuitButton", "나가기",
            new Vector2(0.5f, 0.3f), new Color(0.6f, 0.2f, 0.2f, 1f));
        UnityEventTools.AddVoidPersistentListener(quitButton.onClick, controller.OnClickQuit);
    }

    private static Button CreateButton(Transform parent, Font font, string name, string label,
        Vector2 anchor, Color color)
    {
        GameObject btnGO = new GameObject(name, typeof(Image), typeof(Button));
        btnGO.transform.SetParent(parent, false);

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 90f);

        Image img = btnGO.GetComponent<Image>();
        img.color = color;

        Button btn = btnGO.GetComponent<Button>();

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(btnGO.transform, false);
        Text text = textGO.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 36;
        RectTransform rtText = textGO.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = Vector2.zero;
        rtText.offsetMax = Vector2.zero;

        return btn;
    }

    private static void UpdateBuildSettings()
    {
        // 기존에 Build Settings에 등록해둔 씬들은 그대로 두고,
        // MainMenu를 0번, 2nd를 1번으로만 맞춰준다(중복은 제거).
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == MainMenuScenePath || s.path == GameplayScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(GameplayScenePath, true));
        scenes.Insert(0, new EditorBuildSettingsScene(MainMenuScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
