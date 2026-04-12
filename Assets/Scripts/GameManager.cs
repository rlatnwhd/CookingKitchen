using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 진행 흐름을 제어하는 스크립트
/// - 타이머 종료 시 다음 씬으로 전환
/// - HP가 0이 되면 GameOverScene으로 전환
/// - HP와 점수는 GameData를 통해 다음 씬에도 유지
/// - Time.timeScale = 0으로 게임을 완전히 중지한 뒤 WaitForSecondsRealtime으로 씬 전환
///
/// [설정 안내]
/// 1. 각 스테이지 씬에 빈 오브젝트를 만들고 이 스크립트를 부착합니다.
/// 2. stageIndex를 씬에 맞게 설정합니다. (Stage1Scene=1, Stage2Scene=2, Stage3Scene=3)
/// 3. stageSceneNames 배열 순서: 인덱스 0="Stage1Scene", 1="Stage2Scene", 2="Stage3Scene"
/// 4. File > Build Settings에 모든 씬을 추가해야 합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// true이면 타이머·입력·스폰 등 모든 게임 로직이 멈춥니다.
    /// PlayerController, GameTimer 등이 이 값을 확인합니다.
    /// </summary>
    public static bool IsGameStopped { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("이 씬의 스테이지 번호 (1, 2, 3)")]
    public int stageIndex = 1;

    [Header("씬 이름")]
    [Tooltip("스테이지 씬 이름 목록. 인덱스 순서: 0=Stage1Scene, 1=Stage2Scene, 2=Stage3Scene")]
    public string[] stageSceneNames = { "Stage1Scene", "Stage2Scene", "Stage3Scene" };

    [Tooltip("전체 클리어 씬 이름")]
    public string gameClearSceneName = "GameClearScene";

    [Tooltip("게임 오버 씬 이름")]
    public string gameOverSceneName = "GameOverScene";

    [Header("전환 설정")]
    [Tooltip("게임 멈춤 후 씬 전환까지 대기 시간 (실시간 초)")]
    public float transitionDelay = 1.5f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 새 씬이 로드될 때마다 게임 상태를 정상으로 초기화
        IsGameStopped = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        // HP=0 이벤트 구독
        if (HpManager.Instance != null)
            HpManager.Instance.OnPlayerDead += HandlePlayerDead;
    }

    void OnDestroy()
    {
        if (HpManager.Instance != null)
            HpManager.Instance.OnPlayerDead -= HandlePlayerDead;
    }

    // ──────────────────────────────────────────────
    // 외부 호출 (GameTimer → 스테이지 클리어)
    // ──────────────────────────────────────────────

    /// <summary>
    /// GameTimer가 0에 도달했을 때 호출합니다.
    /// HP와 점수를 GameData에 저장하고 다음 씬으로 전환합니다.
    /// </summary>
    public void OnStageClear()
    {
        if (IsGameStopped) return;

        SaveToGameData();
        StopGame();
        StartCoroutine(StageClearCoroutine());
    }

    // ──────────────────────────────────────────────
    // 내부 처리
    // ──────────────────────────────────────────────

    private void HandlePlayerDead()
    {
        if (IsGameStopped) return;

        SaveToGameData();
        StopGame();
        StartCoroutine(LoadSceneRealtime(gameOverSceneName));
    }

    /// <summary>
    /// 현재 HP·점수를 GameData에 저장합니다.
    /// </summary>
    private void SaveToGameData()
    {
        if (GameData.Instance == null) return;

        if (HpManager.Instance != null)
            GameData.Instance.CurrentHp = HpManager.Instance.CurrentHp;

        if (ScoreManager.Instance != null)
            GameData.Instance.CurrentScore = ScoreManager.Instance.Score;
    }

    /// <summary>
    /// Time.timeScale=0으로 게임을 즉시 중지합니다.
    /// </summary>
    private void StopGame()
    {
        IsGameStopped = true;
        Time.timeScale = 0f;
    }

    private IEnumerator StageClearCoroutine()
    {
        yield return new WaitForSecondsRealtime(transitionDelay);

        Time.timeScale = 1f;
        IsGameStopped = false;

        // 마지막 스테이지(3) 클리어 → 게임 클리어 씬
        if (stageIndex >= stageSceneNames.Length)
        {
            SceneManager.LoadScene(gameClearSceneName);
        }
        else
        {
            // 다음 스테이지 씬 (stageIndex는 1-based, 배열은 0-based)
            SceneManager.LoadScene(stageSceneNames[stageIndex]);
        }
    }

    private IEnumerator LoadSceneRealtime(string sceneName)
    {
        yield return new WaitForSecondsRealtime(transitionDelay);

        Time.timeScale = 1f;
        IsGameStopped = false;
        SceneManager.LoadScene(sceneName);
    }
}
