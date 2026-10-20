using UnityEngine;

// [AI] 위에서 떨어지는 장애물. 플레이어 키 높이 아래로 떨어지면 사라진다.
// (예전엔 "Ground 윗면 계산값"에 닿으면 사라지게 했었는데, 캐릭터 모델 피벗 문제로
//  플레이어의 실제 Position Y가 그 계산값보다 훨씬 낮게(-8.1 등) 잡혀 있다 보니,
//  똥이 플레이어 높이까지 내려오기도 전에 먼저 사라져서 "맞아도 아무 반응 없음" 버그가
//  생겼었다. 그래서 기준을 Ground가 아니라 "실제 플레이어 Position Y"로 바꿔서,
//  적어도 플레이어 높이까지는 반드시 내려오고 그 아래로 좀 더 지나야 사라지게 했다.)
public class Obstacle : MonoBehaviour
{
    public float fallSpeed = 3f;

    [Header("크기")]
    [Tooltip("똥 이미지 크기 배율. 1 = 원본 크기, 2 = 2배로 크게, 0.5 = 절반 크기")]
    public float size = 1f;

    [Tooltip("플레이어/Ground를 못 찾았을 때만 쓰이는 안전장치용 최저 높이")]
    public float destroyBelowY = -20f;

    void Start()
    {
        transform.localScale = Vector3.one * size;

        GameObject player = GameObject.Find("Player_Luffy");
        if (player != null)
        {
            // 플레이어 높이보다 조금 더 아래까지 내려온 다음에 사라지도록 여유를 둬서,
            // 떨어지는 도중 플레이어와 겹치는 순간이 반드시 있게 한다.
            destroyBelowY = player.transform.position.y - 1f;
        }
        else
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                destroyBelowY = ground.transform.position.y + ground.transform.localScale.y * 0.5f - 1f;
            }
        }
    }

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        if (transform.position.y < destroyBelowY)
        {
            Destroy(gameObject);
        }
    }
}
