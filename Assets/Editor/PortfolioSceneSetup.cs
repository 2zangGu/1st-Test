using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// [AI] 1차 성취도 평가 포트폴리오 - 해적선 씬 자동 배치
// Tools > Portfolio > Place Pirate Scene (AI) 메뉴로 실행하세요.
public static class PortfolioSceneSetup
{
    private const string DeckPath = "Assets/ImportedAssets/03_ShipDeck.fbx";
    private const string LuffyPath = "Assets/ImportedAssets/01_Luffy_rigged.fbx";
    private const string FlagPath = "Assets/ImportedAssets/02_PirateFlag.fbx";

    [MenuItem("Tools/Portfolio/Place Pirate Scene (AI)")]
    public static void PlaceScene()
    {
        GameObject deckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeckPath);
        GameObject luffyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LuffyPath);
        GameObject flagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlagPath);

        if (deckPrefab == null || luffyPrefab == null || flagPrefab == null)
        {
            Debug.LogError("[AI] 에셋을 찾을 수 없습니다. Assets/ImportedAssets 폴더에 FBX 3개가 있는지 확인하세요.");
            return;
        }

        // 기존 배치 오브젝트가 있으면 정리 (중복 실행 방지)
        foreach (var n in new[] { "ShipDeck_Background", "Player_Luffy", "PirateFlag" })
        {
            GameObject existing = GameObject.Find(n);
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }

        GameObject deck = (GameObject)PrefabUtility.InstantiatePrefab(deckPrefab);
        deck.name = "ShipDeck_Background";
        deck.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(deck, "[AI] Place ShipDeck");

        GameObject luffy = (GameObject)PrefabUtility.InstantiatePrefab(luffyPrefab);
        luffy.name = "Player_Luffy";
        // 갑판 중앙, 돛대와 겹치지 않도록 살짝 오프셋
        luffy.transform.position = new Vector3(0f, 0.9f, -2.5f); // [AI] 2차 보정: 뱃머리 쪽(중앙 갑판)으로 이동
        luffy.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // [AI] 뱃머리 방향 추정, 어색하면 Y값만 돌려서 조정
        Undo.RegisterCreatedObjectUndo(luffy, "[AI] Place Luffy (Player)");

        GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab);
        flag.name = "PirateFlag";
        // 돛대 상단 근처, 갑판 위쪽에 걸린 형태로 배치
        flag.transform.position = new Vector3(-0.9f, 4.6f, 0f); // [AI] 3차 보정: 돛대 꼭대기(크로스네스트 위)
        flag.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        Undo.RegisterCreatedObjectUndo(flag, "[AI] Place PirateFlag");

        Selection.activeGameObject = luffy;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[AI] 해적선 씬 배치 완료: ShipDeck_Background(배경), Player_Luffy(플레이어, 중앙), PirateFlag(돛대 상단). " +
                  "위치가 마음에 들지 않으면 Scene 뷰에서 직접 옮기거나 Ctrl+Z로 되돌릴 수 있습니다.");
    }
}
