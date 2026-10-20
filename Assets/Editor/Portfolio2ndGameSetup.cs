using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// [AI] 2차(Unity) 평가용 "2nd" 씬을 2D 횡스크롤 "똥피하기" 게임으로 구성.
// "2nd" 씬을 열어둔 상태에서 Tools > Portfolio > 2nd - Setup Dodge Game (AI) 실행하세요.
// Player_Luffy / PirateFlag가 아직 없으면 자동으로 생성하고, 있으면 재배치/컴포넌트 교체만 합니다.
// 여러 번 실행해도 안전하도록(idempotent) 만들어 뒀습니다.
public static class Portfolio2ndGameSetup
{
    private const string LuffyFbxPath = "Assets/ImportedAssets/01_Luffy_rigged.fbx";
    private const string FlagFbxPath = "Assets/ImportedAssets/02_PirateFlag.fbx";
    private const string PoopSpritePath = "Assets/Sprites/Poop.png";
    private const string ObstaclePrefabPath = "Assets/Prefabs/Obstacle.prefab";

    // 땅(플레이어가 서 있는 바닥) 윗면의 월드 Y좌표. 플레이어도 이 높이에 맞춰 배치한다.
    private const float GroundTopY = -4f;
    private const float GroundThickness = 1f;

    [MenuItem("Tools/Portfolio/2nd - Setup Dodge Game (AI)")]
    public static void Setup()
    {
        if (EditorSceneManager.GetActiveScene().name != "2nd")
        {
            Debug.LogWarning("[AI] 현재 활성 씬이 \"2nd\"가 아닙니다 (현재: " +
                EditorSceneManager.GetActiveScene().name + "). 2nd 씬을 열고 다시 실행하는 것을 권장합니다. 계속 진행합니다.");
        }

        SetupCamera();
        CreateGround();
        GameObject luffy = SetupPlayer();
        GameObject flag = SetupFlag();
        GameObject obstaclePrefabAsset = SetupObstaclePrefab();
        BuildUI(out Text timerText, out Text livesText, out GameObject endPanel, out Text endPanelText, out Button restartButton);
        SetupGameManager(luffy, obstaclePrefabAsset, timerText, livesText, endPanel, endPanelText, restartButton);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AI] 2D 똥피하기 게임 구성 완료! Play 버튼을 눌러 테스트해보세요. " +
                  "(A/D 키로 이동, 목숨 3개 모두 잃으면 실패, 제한시간 없이 최대한 오래 생존)");
    }

    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = GameObject.Find("Main Camera");
            if (camGO != null) cam = camGO.GetComponent<Camera>();
        }
        if (cam == null)
        {
            Debug.LogWarning("[AI] Main Camera를 찾지 못해 카메라 설정을 건너뜁니다.");
            return;
        }
        Undo.RecordObject(cam, "Setup 2D Camera");
        Undo.RecordObject(cam.transform, "Setup 2D Camera Transform");
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }

        // 충돌 판정은 전부 거리 계산(Vector2.Distance)으로 처리하므로 콜라이더는 필요 없음
        Collider col = ground.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        ground.transform.position = new Vector3(0f, GroundTopY - GroundThickness * 0.5f, 0f);
        ground.transform.rotation = Quaternion.identity;
        ground.transform.localScale = new Vector3(16f, GroundThickness, 2f);

        Renderer rend = ground.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = rend.sharedMaterial != null ? rend.sharedMaterial.shader : Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = new Color(0.45f, 0.3f, 0.15f); // 단순한 흙바닥 색
            rend.sharedMaterial = mat;
        }
    }

    private static GameObject SetupPlayer()
    {
        GameObject luffy = GameObject.Find("Player_Luffy");
        if (luffy == null)
        {
            GameObject luffyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LuffyFbxPath);
            if (luffyAsset == null)
            {
                Debug.LogError("[AI] " + LuffyFbxPath + " 를 찾을 수 없습니다. Player_Luffy를 생성하지 못했습니다.");
                return null;
            }
            luffy = (GameObject)PrefabUtility.InstantiatePrefab(luffyAsset);
            luffy.name = "Player_Luffy";
            Undo.RegisterCreatedObjectUndo(luffy, "Add Player_Luffy");
        }

        // 땅 가운데(X=0), 땅 윗면 높이에 배치
        luffy.transform.position = new Vector3(0f, GroundTopY, 0f);
        luffy.transform.rotation = Quaternion.identity;
        luffy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        // 늘어나는 모션/공격 모션 등 기존 애니메이션을 전부 제거 -> 좌우 이동만 하는 단순한 동작으로 교체
        Animator animator = luffy.GetComponent<Animator>();
        if (animator != null)
        {
            Object.DestroyImmediate(animator);
            Debug.Log("[AI] 기존 Animator(늘어나는 모션/공격 모션 포함)를 제거했습니다.");
        }

        PlayerController oldPc = luffy.GetComponent<PlayerController>();
        if (oldPc != null)
        {
            Object.DestroyImmediate(oldPc);
            Debug.Log("[AI] 기존 PlayerController(3D용)를 제거했습니다.");
        }

        DodgeController dc = luffy.GetComponent<DodgeController>();
        if (dc == null) dc = luffy.AddComponent<DodgeController>();
        dc.minX = -4f;
        dc.maxX = 4f;
        dc.moveSpeed = 5f;

        return luffy;
    }

    private static GameObject SetupFlag()
    {
        GameObject flag = GameObject.Find("PirateFlag");
        if (flag == null)
        {
            GameObject flagAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FlagFbxPath);
            if (flagAsset == null)
            {
                Debug.LogWarning("[AI] " + FlagFbxPath + " 를 찾을 수 없어 PirateFlag를 생성하지 못했습니다.");
                return null;
            }
            flag = (GameObject)PrefabUtility.InstantiatePrefab(flagAsset);
            flag.name = "PirateFlag";
            Undo.RegisterCreatedObjectUndo(flag, "Add PirateFlag");
        }

        // 배/갑판이 없으므로 화면 한쪽 구석의 장식용 소품으로 배치
        flag.transform.position = new Vector3(-6.5f, -3.5f, 5f);
        flag.transform.rotation = Quaternion.identity;
        flag.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        return flag;
    }

    private static GameObject SetupObstaclePrefab()
    {
        TextureImporter ti = AssetImporter.GetAtPath(PoopSpritePath) as TextureImporter;
        if (ti == null)
        {
            Debug.LogError("[AI] " + PoopSpritePath + " 를 찾을 수 없습니다. 똥 이미지가 해당 경로에 있는지 확인하세요.");
            return null;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.spritePixelsPerUnit = 400f;
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();

        Sprite poopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PoopSpritePath);
        if (poopSprite == null)
        {
            Debug.LogError("[AI] " + PoopSpritePath + " 를 Sprite로 불러오지 못했습니다.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject obstacleGO = new GameObject("Obstacle", typeof(SpriteRenderer));
        SpriteRenderer sr = obstacleGO.GetComponent<SpriteRenderer>();
        sr.sprite = poopSprite;
        sr.sortingOrder = 5;

        Obstacle obstacleComp = obstacleGO.AddComponent<Obstacle>();
        obstacleComp.fallSpeed = 4f;
        obstacleComp.destroyBelowY = -7f;

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(obstacleGO, ObstaclePrefabPath);
        Object.DestroyImmediate(obstacleGO);

        return prefabAsset;
    }

    private static void BuildUI(out Text timerText, out Text livesText, out GameObject endPanel, out Text endPanelText, out Button restartButton)
    {
        GameObject existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // ---- TimerText (상단 중앙) ----
        GameObject timerGO = new GameObject("TimerText", typeof(Text));
        timerGO.transform.SetParent(canvasGO.transform, false);
        timerText = timerGO.GetComponent<Text>();
        timerText.font = uiFont;
        timerText.fontSize = 36;
        timerText.alignment = TextAnchor.UpperCenter;
        timerText.color = Color.white;
        timerText.text = "생존 시간: 0:00";
        RectTransform rtTimer = timerGO.GetComponent<RectTransform>();
        rtTimer.anchorMin = new Vector2(0.5f, 1f);
        rtTimer.anchorMax = new Vector2(0.5f, 1f);
        rtTimer.pivot = new Vector2(0.5f, 1f);
        rtTimer.anchoredPosition = new Vector2(0f, -20f);
        rtTimer.sizeDelta = new Vector2(500f, 60f);

        // ---- LivesText (상단 좌측) ----
        GameObject livesGO = new GameObject("LivesText", typeof(Text));
        livesGO.transform.SetParent(canvasGO.transform, false);
        livesText = livesGO.GetComponent<Text>();
        livesText.font = uiFont;
        livesText.fontSize = 36;
        livesText.alignment = TextAnchor.UpperLeft;
        livesText.color = Color.white;
        livesText.text = "♥ ♥ ♥";
        RectTransform rtLives = livesGO.GetComponent<RectTransform>();
        rtLives.anchorMin = new Vector2(0f, 1f);
        rtLives.anchorMax = new Vector2(0f, 1f);
        rtLives.pivot = new Vector2(0f, 1f);
        rtLives.anchoredPosition = new Vector2(20f, -20f);
        rtLives.sizeDelta = new Vector2(300f, 60f);

        // ---- EndPanel (결과 화면, 기본 비활성) ----
        GameObject panelGO = new GameObject("EndPanel", typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImg = panelGO.GetComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform rtPanel = panelGO.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero;
        rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero;
        rtPanel.offsetMax = Vector2.zero;

        GameObject endTextGO = new GameObject("EndPanelText", typeof(Text));
        endTextGO.transform.SetParent(panelGO.transform, false);
        endPanelText = endTextGO.GetComponent<Text>();
        endPanelText.font = uiFont;
        endPanelText.fontSize = 64;
        endPanelText.alignment = TextAnchor.MiddleCenter;
        endPanelText.color = Color.white;
        endPanelText.text = "";
        RectTransform rtEndText = endTextGO.GetComponent<RectTransform>();
        rtEndText.anchorMin = new Vector2(0.5f, 0.6f);
        rtEndText.anchorMax = new Vector2(0.5f, 0.6f);
        rtEndText.pivot = new Vector2(0.5f, 0.5f);
        rtEndText.sizeDelta = new Vector2(700f, 150f);

        GameObject btnGO = new GameObject("RestartButton", typeof(Image), typeof(Button));
        btnGO.transform.SetParent(panelGO.transform, false);
        RectTransform rtBtn = btnGO.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.4f);
        rtBtn.anchorMax = new Vector2(0.5f, 0.4f);
        rtBtn.pivot = new Vector2(0.5f, 0.5f);
        rtBtn.sizeDelta = new Vector2(240f, 70f);
        Image btnImg = btnGO.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        restartButton = btnGO.GetComponent<Button>();

        GameObject btnTextGO = new GameObject("Text", typeof(Text));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        Text btnText = btnTextGO.GetComponent<Text>();
        btnText.font = uiFont;
        btnText.text = "다시 시작";
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.fontSize = 28;
        RectTransform rtBtnText = btnTextGO.GetComponent<RectTransform>();
        rtBtnText.anchorMin = Vector2.zero;
        rtBtnText.anchorMax = Vector2.one;
        rtBtnText.offsetMin = Vector2.zero;
        rtBtnText.offsetMax = Vector2.zero;

        panelGO.SetActive(false);
        endPanel = panelGO;
    }

    private static void SetupGameManager(GameObject luffy, GameObject obstaclePrefabAsset,
        Text timerText, Text livesText, GameObject endPanel, Text endPanelText, Button restartButton)
    {
        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO == null)
        {
            gmGO = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gmGO, "Create GameManager");
        }

        DodgeGameManager gm = gmGO.GetComponent<DodgeGameManager>();
        if (gm == null) gm = gmGO.AddComponent<DodgeGameManager>();

        if (luffy != null)
        {
            gm.player = luffy.transform;
            gm.playerController = luffy.GetComponent<DodgeController>();
        }
        else
        {
            Debug.LogWarning("[AI] Player_Luffy가 없어 GameManager에 플레이어 참조를 연결하지 못했습니다.");
        }

        gm.timerText = timerText;
        gm.livesText = livesText;
        gm.endPanel = endPanel;
        gm.endPanelText = endPanelText;
        gm.restartButton = restartButton;
        gm.obstaclePrefab = obstaclePrefabAsset;

        gm.startingLives = 3;
        gm.spawnInterval = 1.0f;
        gm.spawnMinX = -4f;
        gm.spawnMaxX = 4f;
        gm.spawnY = 8f;
        gm.hitRadius = 1.0f;
        gm.invincibleDuration = 1.0f;
    }

    // [AI] Ground/플레이어 위치·크기는 이미 직접 수정하셨다고 해서, 전체 Setup을 다시 돌리지 않고
    // 요청하신 두 가지만 콕 집어 고치는 별도 메뉴. 카메라/오브젝트/UI 등 다른 건 전혀 건드리지 않음.
    [MenuItem("Tools/Portfolio/2nd - Fix Ground Fill And Player Bounds (AI)")]
    public static void FixGroundFillAndPlayerBounds()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogError("[AI] Ground 오브젝트를 찾을 수 없습니다. 먼저 기본 Setup을 실행해주세요.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = GameObject.Find("Main Camera");
            if (camGO != null) cam = camGO.GetComponent<Camera>();
        }

        Vector3 pos = ground.transform.position;
        Vector3 scale = ground.transform.localScale;

        // 현재 바닥의 윗면 높이는 그대로 유지 (이미 맞춰둔 플레이어 서는 위치를 바꾸지 않기 위해)
        float currentTopY = pos.y + scale.y * 0.5f;

        float cameraBottomY = currentTopY - 20f; // 카메라 정보를 못 구할 때의 안전한 기본값
        if (cam != null && cam.orthographic)
        {
            cameraBottomY = cam.transform.position.y - cam.orthographicSize;
        }

        float buffer = 5f; // 화면 밖까지 넉넉하게 채우기 위한 여유분
        float newBottomY = cameraBottomY - buffer;
        float newHeight = currentTopY - newBottomY;
        if (newHeight < scale.y) newHeight = scale.y; // 혹시라도 기존보다 작아지지 않도록 보호

        Undo.RecordObject(ground.transform, "Fill Ground Below");
        // X/Z 위치와 가로·깊이 크기는 그대로 둔다 (직접 수정하신 부분, 건드리지 않음)
        ground.transform.position = new Vector3(pos.x, (currentTopY + newBottomY) * 0.5f, pos.z);
        ground.transform.localScale = new Vector3(scale.x, newHeight, scale.z);

        // 플레이어가 Ground 좌우 바깥으로 못 나가게, Ground의 실제 가로 범위에 맞춰 이동 한계만 갱신
        GameObject luffy = GameObject.Find("Player_Luffy");
        if (luffy != null)
        {
            DodgeController dc = luffy.GetComponent<DodgeController>();
            if (dc != null)
            {
                float halfWidth = ground.transform.localScale.x * 0.5f;
                float groundLeft = ground.transform.position.x - halfWidth;
                float groundRight = ground.transform.position.x + halfWidth;
                float margin = 0.5f; // 캐릭터가 바닥 끝에 반쯤 걸치지 않도록 하는 여유분

                Undo.RecordObject(dc, "Update Player Bounds");
                dc.minX = groundLeft + margin;
                dc.maxX = groundRight - margin;

                Vector3 luffyPos = luffy.transform.position;
                luffyPos.x = Mathf.Clamp(luffyPos.x, dc.minX, dc.maxX);
                luffy.transform.position = luffyPos;
            }
            else
            {
                Debug.LogWarning("[AI] Player_Luffy에서 DodgeController를 찾지 못해 이동 범위를 설정하지 못했습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[AI] Player_Luffy를 찾지 못해 이동 범위를 설정하지 못했습니다.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AI] 바닥 아래 빈 공간을 채우고, 플레이어가 바닥 밖으로 못 나가게 이동 범위를 맞췄습니다. (다른 크기/위치는 건드리지 않았습니다)");
    }

    // [AI] 이미 만들어둔 게임 화면(Canvas)에 "최고 기록" UI 하나만 추가하는 메뉴.
    // Ground/Player/기존 UI 배치 등 직접 맞춰두신 다른 것들은 전혀 건드리지 않는다.
    // PlayerPrefs에 저장된 역대 최고 생존 시간을 오른쪽 위에 표시하고, DodgeGameManager와 자동으로 연결한다.
    [MenuItem("Tools/Portfolio/2nd - Add High Score UI (AI)")]
    public static void AddHighScoreUI()
    {
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("[AI] Canvas를 찾을 수 없습니다. 먼저 \"2nd - Setup Dodge Game (AI)\"를 실행해 UI를 만들어주세요.");
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject hsGO = GameObject.Find("HighScoreText");
        if (hsGO == null)
        {
            hsGO = new GameObject("HighScoreText", typeof(Text));
            hsGO.transform.SetParent(canvasGO.transform, false);
            Undo.RegisterCreatedObjectUndo(hsGO, "Create HighScoreText");
        }

        Text hsText = hsGO.GetComponent<Text>();
        if (hsText == null) hsText = hsGO.AddComponent<Text>();
        hsText.font = uiFont;
        hsText.fontSize = 32;
        hsText.alignment = TextAnchor.UpperRight;
        hsText.color = Color.white;
        hsText.text = "최고 기록: 0:00";

        // 오른쪽 위 모서리에 붙인다
        RectTransform rtHs = hsGO.GetComponent<RectTransform>();
        rtHs.anchorMin = new Vector2(1f, 1f);
        rtHs.anchorMax = new Vector2(1f, 1f);
        rtHs.pivot = new Vector2(1f, 1f);
        rtHs.anchoredPosition = new Vector2(-20f, -20f);
        rtHs.sizeDelta = new Vector2(360f, 60f);

        GameObject gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            DodgeGameManager gm = gmGO.GetComponent<DodgeGameManager>();
            if (gm != null)
            {
                gm.highScoreText = hsText;
            }
            else
            {
                Debug.LogWarning("[AI] GameManager 오브젝트에 DodgeGameManager 컴포넌트가 없어 자동 연결하지 못했습니다. " +
                    "Inspector에서 GameManager의 High Score Text 필드에 HighScoreText를 직접 연결해주세요.");
            }
        }
        else
        {
            Debug.LogWarning("[AI] GameManager 오브젝트를 찾지 못해 자동 연결하지 못했습니다. " +
                "Inspector에서 HighScoreText를 GameManager의 High Score Text 필드에 직접 연결해주세요.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[AI] 오른쪽 위에 \"최고 기록\" UI를 추가했습니다. Play 하면 기기에 저장된 역대 최고 생존 시간이 표시되고, " +
                   "그 기록을 넘기는 순간 실시간으로 갱신/저장됩니다.");
    }
}
