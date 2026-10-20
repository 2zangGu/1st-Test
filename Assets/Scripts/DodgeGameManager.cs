using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// [AI] 2D 횡스크롤 장애물 피하기 미니게임 진행 관리.
// 제한시간 없이, 버틸 수 있을 때까지 생존 시간이 계속 늘어난다. 목숨(startingLives)이 0이 되면 실패.
// 똥(Obstacle)에 맞으면 그 똥은 사라지고 목숨이 1개 줄어든다.
// 역대 최고 생존 시간(최고 기록)은 PlayerPrefs에 저장되어 다음에 실행해도 유지된다.
public class DodgeGameManager : MonoBehaviour
{
    [Header("참조")]
    public Transform player;
    public DodgeController playerController;
    public Text timerText;
    public Text livesText;
    public Text highScoreText;
    public GameObject endPanel;
    public Text endPanelText;
    public Button restartButton;
    public GameObject obstaclePrefab;

    [Header("게임 설정")]
    public int startingLives = 3;
    public float spawnInterval = 1.0f;

    [Tooltip("Ground를 찾지 못했을 때만 쓰이는 값. Ground를 찾으면 자동으로 덮어써짐")]
    public float spawnMinX = -4f;
    [Tooltip("Ground를 찾지 못했을 때만 쓰이는 값. Ground를 찾으면 자동으로 덮어써짐")]
    public float spawnMaxX = 4f;
    public float spawnY = 8f;

    [Tooltip("플레이어/똥의 렌더러를 못 찾았을 때만 쓰이는 예비용 피격 반경")]
    public float playerHitRadius = 0.6f;
    [Tooltip("똥의 SpriteRenderer를 못 찾았을 때만 쓰이는 피격 반경 값")]
    public float hitRadius = 0.8f;
    public float invincibleDuration = 1.0f;

    // 기기에 저장되는 최고 기록 키. 다른 게임과 값이 겹치지 않도록 이름을 구체적으로 지었다.
    private const string HighScoreKey = "DodgeHighScoreSeconds";

    private float elapsed = 0f;
    private float highScore = 0f;
    private int lives;
    private bool gameOver = false;
    private float spawnTimer = 0f;
    private float invincibleTimer = 0f;
    private Renderer[] playerRenderers;

    void Start()
    {
        lives = startingLives;
        if (endPanel != null) endPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);

        // 기기에 저장되어 있던 역대 최고 생존 시간을 불러온다. 처음 실행이면 0.
        highScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);

        if (player == null)
        {
            Debug.LogError("[AI] DodgeGameManager의 Player 참조가 비어있습니다! 피격 판정이 전혀 동작하지 않습니다. " +
                "Inspector에서 GameManager 오브젝트의 Player 필드에 Player_Luffy를 다시 연결해주세요.");
        }
        else
        {
            // 플레이어 pivot이 아니라, 실제로 화면에 그려지는 몸 전체(렌더러들)의 범위를
            // 기준으로 피격 판정을 할 것이므로 미리 모아둔다. (pivot이 모델 가운데가 아니라서
            // "거리 기반" 판정은 눈에 보이는 몸이랑 안 맞았었음)
            playerRenderers = player.GetComponentsInChildren<Renderer>();
        }

        UpdateSpawnRangeFromGround();
        UpdateUI();
    }

    private void UpdateSpawnRangeFromGround()
    {
        // "Ground"라는 이름의 오브젝트가 씬에 여러 개 있으면(복사하다 남은 여분 등)
        // 엉뚱한 걸 기준으로 스폰 범위가 계산될 수 있어, 전부 찾아보고 알려준다.
        Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
        Renderer groundRend = null;
        int groundCount = 0;

        foreach (Transform t in allTransforms)
        {
            if (t.name != "Ground" || !t.gameObject.activeInHierarchy) continue;
            Renderer r = t.GetComponent<Renderer>();
            if (r == null || !r.enabled) continue;

            groundCount++;
            if (groundRend == null) groundRend = r;
        }

        if (groundCount > 1)
        {
            Debug.LogWarning("[AI] \"Ground\" 오브젝트가 " + groundCount +
                "개 있어 똥이 떨어지는 범위 계산이 부정확할 수 있습니다. " +
                "Hierarchy에서 실제로 쓰는 바닥 하나만 남기고 나머지는 지워주세요.");
        }

        if (groundRend == null) return;

        // 똥이 "플레이어가 실제로 움직일 수 있는 범위" 전체에 떨어지도록,
        // Ground의 지금 가로 폭을 기준으로 스폰 범위를 맞춘다.
        float margin = 0.5f;
        spawnMinX = groundRend.bounds.min.x + margin;
        spawnMaxX = groundRend.bounds.max.x - margin;
    }

    void Update()
    {
        if (gameOver) return;

        elapsed += Time.deltaTime;
        spawnTimer += Time.deltaTime;
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;

        // 생존 시간이 역대 최고 기록을 넘기는 순간 바로 갱신하고 저장한다.
        if (elapsed > highScore)
        {
            highScore = elapsed;
            PlayerPrefs.SetFloat(HighScoreKey, highScore);
        }

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnObstacle();
        }

        CheckCollisions();
        UpdateUI();
    }

    void SpawnObstacle()
    {
        if (obstaclePrefab == null || player == null) return;
        float x = Random.Range(spawnMinX, spawnMaxX);
        Vector3 pos = new Vector3(x, spawnY, player.position.z);
        Instantiate(obstaclePrefab, pos, Quaternion.identity);
    }

    void CheckCollisions()
    {
        if (invincibleTimer > 0f || player == null) return;
        if (playerRenderers == null || playerRenderers.Length == 0) return;

        // 플레이어 pivot에서 거리로 재지 않고, 실제 눈에 보이는 몸 전체 범위(여러 렌더러를
        // 합친 박스)와 똥의 실제 렌더링 범위가 겹치는지로 판정한다.
        // -> pivot이 모델 가운데가 아니어도 상관없이 "몸 어디든 닿으면" 맞는다.
        // 2D 게임이라 Z축(깊이)은 아예 무시하고 화면상의 X/Y만 본다. (3D Bounds.Intersects는
        // Z축까지 겹쳐야 true가 되는데, 그것 때문에 눈에는 닿아 보여도 판정이 안 됐을 수 있음)
        Bounds playerBounds = playerRenderers[0].bounds;
        for (int i = 1; i < playerRenderers.Length; i++)
        {
            playerBounds.Encapsulate(playerRenderers[i].bounds);
        }
        // 살짝 여유를 더 줘서, 딱 맞닿기 직전에도 "닿은 것"으로 쳐준다.
        playerBounds.Expand(0.3f);

        Obstacle[] obstacles = FindObjectsOfType<Obstacle>();
        foreach (Obstacle o in obstacles)
        {
            SpriteRenderer sr = o.GetComponent<SpriteRenderer>();
            bool hit = false;

            if (sr != null)
            {
                Bounds ob = sr.bounds;
                // X와 Y축만 비교 (Z는 무시)
                bool overlapX = playerBounds.min.x <= ob.max.x && playerBounds.max.x >= ob.min.x;
                bool overlapY = playerBounds.min.y <= ob.max.y && playerBounds.max.y >= ob.min.y;
                hit = overlapX && overlapY;
            }
            else
            {
                // 똥 오브젝트에 SpriteRenderer가 없는 예외 상황에서만 거리 기반으로 대체 판정
                float dist = Vector2.Distance(
                    new Vector2(o.transform.position.x, o.transform.position.y),
                    new Vector2(player.position.x, player.position.y));
                hit = dist < (hitRadius + playerHitRadius);
            }

            if (hit)
            {
                Debug.Log("[AI] 피격! (남은 목숨: " + (lives - 1) + ")");
                Destroy(o.gameObject);
                TakeHit();
                break;
            }
        }
    }

    void TakeHit()
    {
        lives--;
        invincibleTimer = invincibleDuration;
        if (lives <= 0)
        {
            EndGame(false);
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            int totalSeconds = Mathf.FloorToInt(elapsed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = "생존 시간: " + minutes + ":" + seconds.ToString("00");
        }
        if (livesText != null)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            for (int i = lives; i < startingLives; i++) hearts += "♡ ";
            livesText.text = hearts;
        }
        if (highScoreText != null)
        {
            int hsTotalSeconds = Mathf.FloorToInt(highScore);
            int hsMinutes = hsTotalSeconds / 60;
            int hsSeconds = hsTotalSeconds % 60;
            highScoreText.text = "최고 기록: " + hsMinutes + ":" + hsSeconds.ToString("00");
        }
    }

    void EndGame(bool won)
    {
        gameOver = true;
        // 혹시 몰라 게임이 끝나는 순간 최고 기록을 디스크에 확실히 저장해둔다.
        PlayerPrefs.Save();
        if (playerController != null) playerController.SetInputLocked(true);
        if (endPanel != null) endPanel.SetActive(true);
        if (endPanelText != null)
        {
            int hsTotalSeconds = Mathf.FloorToInt(highScore);
            int hsMinutes = hsTotalSeconds / 60;
            int hsSecibds = hsTotalSeconds % 60;
            string hsText = hsMinutes + ":" + hsTotalSeconds.ToString("00");
            endPanelText.text = (won ? "승리!" : "실패...") + "\n\n최고 기록: " + hsText;
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
