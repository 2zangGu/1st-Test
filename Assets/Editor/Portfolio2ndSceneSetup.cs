using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

// [AI] 2차(Unity) 평가용 새 씬에 1차에서 쓰던 Player_Luffy와 PirateFlag만 우선 배치.
// "2nd" 씬을 열어둔 상태에서 Tools > Portfolio > 2nd - Add Player And Flag 실행하세요.
public static class Portfolio2ndSceneSetup
{
    private const string LuffyFbxPath = "Assets/ImportedAssets/01_Luffy_rigged.fbx";
    private const string FlagFbxPath = "Assets/ImportedAssets/02_PirateFlag.fbx";
    private const string ControllerPath = "Assets/Animations/Luffy.controller";

    [MenuItem("Tools/Portfolio/2nd - Add Player And Flag (AI)")]
    public static void Setup()
    {
        if (GameObject.Find("Player_Luffy") != null || GameObject.Find("PirateFlag") != null)
        {
            Debug.LogWarning("[AI] 이미 Player_Luffy 또는 PirateFlag가 현재 씬에 있습니다. 중복 생성을 막기 위해 중단합니다.");
            return;
        }

        // ---- Player_Luffy ----
        GameObject luffyAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LuffyFbxPath);
        if (luffyAsset == null)
        {
            Debug.LogError("[AI] " + LuffyFbxPath + " 를 찾을 수 없습니다.");
            return;
        }
        GameObject luffy = (GameObject)PrefabUtility.InstantiatePrefab(luffyAsset);
        luffy.name = "Player_Luffy";
        luffy.transform.localPosition = new Vector3(4.39f, -6.24f, 7.51f);
        luffy.transform.localRotation = new Quaternion(0f, -0.08715578f, 0f, 0.9961947f);
        luffy.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        Animator animator = luffy.GetComponent<Animator>();
        if (animator == null) animator = luffy.AddComponent<Animator>();
        AnimatorController controllerAsset = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controllerAsset != null) animator.runtimeAnimatorController = controllerAsset;
        else Debug.LogWarning("[AI] " + ControllerPath + " 를 찾을 수 없어 Animator Controller는 비워뒀습니다. 1차 씬에서 복사해오거나 Setup Player Controller (AI)를 이 씬에서 다시 실행하세요.");

        PlayerController pc = luffy.GetComponent<PlayerController>();
        if (pc == null) pc = luffy.AddComponent<PlayerController>();

        Undo.RegisterCreatedObjectUndo(luffy, "Add Player_Luffy");

        // ---- PirateFlag ----
        GameObject flagAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FlagFbxPath);
        if (flagAsset == null)
        {
            Debug.LogError("[AI] " + FlagFbxPath + " 를 찾을 수 없습니다.");
            return;
        }
        GameObject flag = (GameObject)PrefabUtility.InstantiatePrefab(flagAsset);
        flag.name = "PirateFlag";
        flag.transform.localPosition = new Vector3(5.96f, 0.87f, 5.57f);
        flag.transform.localRotation = new Quaternion(-0.698206f, -0.11184138f, -0.11184138f, 0.6982058f);

        Undo.RegisterCreatedObjectUndo(flag, "Add PirateFlag");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[AI] Player_Luffy, PirateFlag를 현재 씬(" + EditorSceneManager.GetActiveScene().name +
                  ")에 1차 작업 당시 위치 그대로 배치했습니다. 배/갑판이 없어서 허공에 떠 있는 것처럼 보일 수 있어요 — 배경은 다음 단계에서 추가하면 됩니다.");
    }
}
