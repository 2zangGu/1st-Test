using UnityEngine;

// [AI] 1차 성취도 평가 - 플레이어 이동/공격 컨트롤러
// WASD 이동, 좌클릭: 팔 늘리기 공격, 우클릭: 다리 늘리기 공격
// 배 밖으로 못 나가게 minX/maxX/minZ/maxZ 범위로 위치를 제한합니다.
// 배가 눈에 보이는 범위랑 안 맞으면 Inspector에서 이 값들을 직접 드래그해서 조정하세요 (Play 중에도 바로 확인 가능).
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 2.0f;
    public float rotateSpeed = 10f;

    [Header("배 밖으로 못 나가게 막는 범위 (여기 숫자를 드래그해서 조정하세요)")]
    public float minX = -1.8f;
    public float maxX = 1.8f;
    public float minZ = -2.5f;
    public float maxZ = 2.0f;
    public bool lockY = true;
    public float fixedY = 0.9f;

    [Header("공격")]
    public float attackDuration = 1.0f; // Setup 스크립트가 애니메이션 길이에 맞춰 자동으로 채워줍니다.

    private Animator animator;
    private bool isAttacking = false;
    private float attackTimer = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (lockY)
        {
            Vector3 p = transform.position;
            fixedY = p.y;
        }
    }

    void Update()
    {
        HandleAttackInput();

        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) isAttacking = false;
            if (animator != null) animator.SetFloat("Speed", 0f);
            return; // 공격 중에는 이동 불가
        }

        HandleMovement();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = new Vector3(h, 0f, v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (move.sqrMagnitude > 0.0001f)
        {
            Vector3 newPos = transform.position + move * moveSpeed * Time.deltaTime;
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
            if (lockY) newPos.y = fixedY;
            transform.position = newPos;

            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

            if (animator != null) animator.SetFloat("Speed", 1f);
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
    }

    void HandleAttackInput()
    {
        if (isAttacking) return;

        if (Input.GetMouseButtonDown(0)) // 왼쪽 버튼: 팔 늘리기 공격
        {
            if (animator != null) animator.SetTrigger("AttackArm");
            StartAttack();
        }
        else if (Input.GetMouseButtonDown(1)) // 오른쪽 버튼: 다리 늘리기 공격
        {
            if (animator != null) animator.SetTrigger("AttackLeg");
            StartAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;
    }
}
