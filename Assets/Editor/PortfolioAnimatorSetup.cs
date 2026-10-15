using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

// [AI] 1차 성취도 평가 - Luffy 애니메이터 컨트롤러 자동 구성
// Tools > Portfolio > Setup Player Controller (AI) 메뉴로 실행하세요.
// 먼저 Tools > Portfolio > Place Pirate Scene (AI)로 Player_Luffy가 씬에 있어야 합니다.
public static class PortfolioAnimatorSetup
{
    private const string LuffyFbxPath = "Assets/ImportedAssets/01_Luffy_rigged.fbx";
    private const string ControllerPath = "Assets/Animations/Luffy.controller";

    [MenuItem("Tools/Portfolio/Setup Player Controller (AI)")]
    public static void Setup()
    {
        GameObject luffy = GameObject.Find("Player_Luffy");
        if (luffy == null)
        {
            Debug.LogError("[AI] 씬에서 Player_Luffy를 찾을 수 없습니다. 먼저 Place Pirate Scene (AI)를 실행하세요.");
            return;
        }

        // [AI] 유니티가 FBX 클립 이름 앞에 "Luffy_Rig|" 같은 리그 이름을 붙이고,
        // "__preview__"로 시작하는 미리보기용 중복 클립도 같이 들어있어서 이름을 느슨하게 매칭합니다.
        var clips = AssetDatabase.LoadAllAssetsAtPath(LuffyFbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToList();

        System.Func<string, AnimationClip> findClip = (name) =>
            clips.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));

        AnimationClip idle = findClip("Idle");
        AnimationClip walk = findClip("Walk");
        AnimationClip attackArm = findClip("Attack_Arm");
        AnimationClip attackLeg = findClip("Attack_Leg");

        if (idle == null || walk == null || attackArm == null || attackLeg == null)
        {
            Debug.LogError("[AI] 애니메이션 클립을 찾을 수 없습니다. FBX 안의 클립 이름: " +
                string.Join(", ", clips.Select(c => c.name)) +
                " (Idle/Walk/Attack_Arm/Attack_Leg 4개가 모두 있어야 합니다. 01_Luffy_rigged.fbx를 Reimport 해보세요.)");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("AttackArm", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackLeg", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle", new Vector3(0, 0, 0));
        idleState.motion = idle;
        sm.defaultState = idleState;

        var walkState = sm.AddState("Walk", new Vector3(300, 0, 0));
        walkState.motion = walk;

        var attackArmState = sm.AddState("Attack_Arm", new Vector3(0, 180, 0));
        attackArmState.motion = attackArm;

        var attackLegState = sm.AddState("Attack_Leg", new Vector3(300, 180, 0));
        attackLegState.motion = attackLeg;

        var t1 = idleState.AddTransition(walkState);
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t1.hasExitTime = false; t1.duration = 0.1f;

        var t2 = walkState.AddTransition(idleState);
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t2.hasExitTime = false; t2.duration = 0.1f;

        var tArm = sm.AddAnyStateTransition(attackArmState);
        tArm.AddCondition(AnimatorConditionMode.If, 0, "AttackArm");
        tArm.hasExitTime = false; tArm.duration = 0.05f;
        tArm.canTransitionToSelf = false;

        var tLeg = sm.AddAnyStateTransition(attackLegState);
        tLeg.AddCondition(AnimatorConditionMode.If, 0, "AttackLeg");
        tLeg.hasExitTime = false; tLeg.duration = 0.05f;
        tLeg.canTransitionToSelf = false;

        var backFromArm = attackArmState.AddTransition(idleState);
        backFromArm.hasExitTime = true; backFromArm.exitTime = 0.9f; backFromArm.duration = 0.15f;

        var backFromLeg = attackLegState.AddTransition(idleState);
        backFromLeg.hasExitTime = true; backFromLeg.exitTime = 0.9f; backFromLeg.duration = 0.15f;

        Animator animator = luffy.GetComponent<Animator>();
        if (animator == null) animator = luffy.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        PlayerController pc = luffy.GetComponent<PlayerController>();
        if (pc == null) pc = luffy.AddComponent<PlayerController>();
        pc.attackDuration = attackArm.length;

        EditorUtility.SetDirty(luffy);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[AI] 플레이어 컨트롤러 + 애니메이터 설정 완료. Play 버튼으로 테스트하세요. " +
                  "WASD 이동, 좌클릭=팔 공격, 우클릭=다리 공격. 배 밖으로 못 나가는 범위는 Player_Luffy의 " +
                  "PlayerController 컴포넌트에서 minX/maxX/minZ/maxZ 값으로 직접 조정할 수 있습니다.");
    }
}
