using UnityEngine;

// [AI] 2D 횡스크롤 "똥피하기" 게임용 플레이어 이동 스크립트.
// 좌우로 움직여도 옆을 보지 않고 항상 정면을 본 채로 A/D 키로만 이동한다.
// Y/Z 높이는 Inspector의 Transform Position에 설정해둔 값을 그대로 고정해서 쓴다
// (Ground를 자동으로 찾아 높이를 다시 계산하던 로직은 혼란만 줘서 제거함).
//
// 좌우 이동 범위(minX/maxX)는 "Ground" 오브젝트의 "현재" 실제 가로 폭을 기준으로
// Play를 시작할 때마다 자동으로 다시 계산한다. 나중에 Ground 크기/위치를 또 수정해도
// 이 스크립트가 알아서 그 범위에 맞춰 플레이어를 가둬주기 때문에, 맵 바깥으로 나가버리는
// 문제가 다시 생기지 않는다. (Ground를 못 찾을 때만 Inspector에 적어둔 minX/maxX를 그대로 씀)
public class DodgeController : MonoBehaviour
{
    [Header("좌우 이동")]
    public float moveSpeed = 5f;

    [Tooltip("Ground를 찾지 못했을 때만 쓰이는 값. Ground를 찾으면 자동으로 덮어써짐")]
    public float minX = -4f;
    [Tooltip("Ground를 찾지 못했을 때만 쓰이는 값. Ground를 찾으면 자동으로 덮어써짐")]
    public float maxX = 4f;

    private float fixedY;
    private float fixedZ;
    private Quaternion startRotation;
    private bool inputLocked = false;

    void Awake()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        startRotation = transform.rotation;

        // 같은 오브젝트에 옛날 3D용 PlayerController가 같이 붙어있으면 그 스크립트도
        // 매 프레임 이 Transform을 따로 움직여서 서로 싸우게 된다. 있으면 바로 알려준다.
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in allScripts)
        {
            if (mb == null) continue;
            if (mb.GetType().Name == "PlayerController")
            {
                Debug.LogError("[AI] 이 오브젝트(" + gameObject.name + ")에 옛날 PlayerController 스크립트가 " +
                    "DodgeController와 같이 붙어 있습니다! 둘 다 매 프레임 위치를 따로 움직여서 " +
                    "좌표가 이상하게 밀리는 원인이 됩니다. Inspector에서 PlayerController 컴포넌트를 " +
                    "찾아 ⋮ 메뉴 > Remove Component로 지워주세요.");
            }
        }

        MeasurePlayerVisualOffsets();
        UpdateBoundsFromGround();

        // 씬에 저장된 위치가 이미 범위 밖(= 맵 바깥)이었다면 시작하자마자 범위 안으로 당겨온다.
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        transform.position = pos;

        Debug.Log("[AI] DodgeController 시작: minX=" + minX + ", maxX=" + maxX +
                   ", 시작 position.x=" + pos.x);
    }

    // 캐릭터 모델의 중심축(pivot)이 모델 정가운데가 아니라 한쪽으로 치우쳐 있을 수 있어서,
    // pivot 기준으로 실제 렌더링 범위가 왼쪽/오른쪽으로 각각 얼마나 삐져나오는지 측정해둔다.
    // (똑같이 0.4~0.5 정도로 대칭이라고 가정했던 이전 방식들이, 모델이 비대칭이라서
    //  한쪽으로 쏠려 보이는 문제의 원인이었음)
    private float leftVisualOffset = 0.4f;
    private float rightVisualOffset = 0.4f;

    private void MeasurePlayerVisualOffsets()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combined.Encapsulate(renderers[i].bounds);
        }

        float pivotX = transform.position.x;
        leftVisualOffset = pivotX - combined.min.x;
        rightVisualOffset = combined.max.x - pivotX;

        Debug.Log("[AI] 플레이어 시각적 범위 측정: pivot=" + pivotX +
                   ", 렌더링 bounds X=[" + combined.min.x + ", " + combined.max.x + "]" +
                   " => 왼쪽 여유 " + leftVisualOffset + ", 오른쪽 여유 " + rightVisualOffset);
    }

    private void UpdateBoundsFromGround()
    {
        // "Ground"라는 이름의 오브젝트가 씬에 여러 개 있으면 그 중 엉뚱한 걸 기준으로
        // 범위를 계산해서 화면에 보이는 바닥과 안 맞을 수 있다. 전부 찾아서 로그로 알려준다.
        Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
        Renderer groundRend = null;
        int groundCount = 0;

        foreach (Transform t in allTransforms)
        {
            if (t.name != "Ground" || !t.gameObject.activeInHierarchy) continue;
            Renderer r = t.GetComponent<Renderer>();
            if (r == null || !r.enabled) continue;

            groundCount++;
            Debug.Log("[AI] \"Ground\" 오브젝트 #" + groundCount + " position=" + t.position +
                       " worldBoundsX=[" + r.bounds.min.x + ", " + r.bounds.max.x + "]");

            if (groundRend == null) groundRend = r;
        }

        if (groundCount > 1)
        {
            Debug.LogWarning("[AI] \"Ground\"라는 이름의 오브젝트가 씬에 " + groundCount +
                "개 있습니다! 복사(Ctrl+D) 하다가 남은 여분일 가능성이 높으니, " +
                "Hierarchy에서 실제로 쓰는 바닥 하나만 남기고 나머지는 지워주세요. " +
                "지금은 그 중 첫 번째로 찾은 걸 기준으로 이동 범위를 계산합니다.");
        }

        if (groundRend == null) return;

        // 플레이어 pivot 기준 왼쪽/오른쪽으로 실제 모델이 삐져나온 만큼씩 비대칭으로 여백을 준다.
        float left = groundRend.bounds.min.x + leftVisualOffset;
        float right = groundRend.bounds.max.x - rightVisualOffset;

        if (left > right)
        {
            // Ground가 여백보다 좁은 극단적인 경우를 대비한 안전장치
            float mid = (groundRend.bounds.min.x + groundRend.bounds.max.x) * 0.5f;
            left = mid;
            right = mid;
        }

        minX = left;
        maxX = right;
    }

    void Update()
    {
        if (inputLocked) return;

        float h = 0f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;

        Vector3 pos = transform.position;

        if (Mathf.Abs(h) > 0.01f)
        {
            pos.x += h * moveSpeed * Time.deltaTime;
        }

        // 입력이 없을 때도 항상 범위 안으로 고정 (Ground 밖으로 못 나감)
        pos.x = Mathf.Clamp(pos.x, minX, maxX);

        pos.y = fixedY;
        pos.z = fixedZ;
        transform.position = pos;

        // 좌우 이동 중에도 옆으로 돌지 않고 항상 처음 바라보던 정면 방향을 유지
        transform.rotation = startRotation;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }
}
