using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 진행 흐름을 제어하는 스크립트
/// - 씬 시작 시 셔터가 닫혀 있다가 위로 올라가며 게임이 시작됩니다.
/// - 타이머 종료 시 다음 라운드 텍스트와 함께 셔터가 내려오고 씬이 전환됩니다.
/// - HP=0 또는 마지막 스테이지 종료 시 GAME OVER / CLEAR 텍스트와 함께 전환됩니다.
///
/// [설정 안내]
/// 1. 각 스테이지 씬에 빈 오브젝트를 만들고 이 스크립트를 부착합니다.
/// 2. stageIndex를 씬에 맞게 설정합니다. (Stage1Scene=1, Stage2Scene=2, Stage3Scene=3)
/// 3. stageShutter에 씬 내 StageShutter 컴포넌트를 할당합니다.
/// 4. File > Build Settings에 모든 씬(Stage1~3, GameClear, GameOver)을 추가합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// true이면 타이머·입력·스폰 등 모든 게임 로직이 멈춥니다.
    /// 셔터 오픈 전과 씬 전환 중에 true입니다.
    /// </summary>
    public static bool IsGameStopped { get; private set; }

    [Header("스테이지 설정")]
    [Tooltip("이 씬의 스테이지 번호 (1, 2, 3)")]
    public int stageIndex = 1;

    [Header("씬 이름")]
    [Tooltip("스테이지 씬 이름 목록. 순서: 0=Stage1Scene, 1=Stage2Scene, 2=Stage3Scene")]
    public string[] stageSceneNames = { "Stage1Scene", "Stage2Scene", "Stage3Scene" };

    [Tooltip("전체 클리어 씬 이름")]
    public string gameClearSceneName = "GameClearScene";

    [Tooltip("게임 오버 씬 이름")]
    public string gameOverSceneName = "GameOverScene";

    [Header("셔터 연결")]
    [Tooltip("씬의 StageShutter 컴포넌트를 할당하세요")]
    public StageShutter stageShutter;

    [Header("셔터 텍스트 형식")]
    [Tooltip("라운드 텍스트 형식. {0}=스테이지 번호 (예: ROUND {0})")]
    public string roundTextFormat = "ROUND {0}";

    [Tooltip("마지막 스테이지 클리어 시 셔터 텍스트")]
    public string gameClearCloseText = "STAGE CLEAR!";

    [Tooltip("게임 오버 시 셔터 텍스트")]
    public string gameOverCloseText = "GAME OVER";

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 씬 로드 직후: 셔터가 열릴 때까지 게임 일시 정지
        IsGameStopped = true;
        Time.timeScale = 1f;
    }

    void Start()
    {
        // HP=0 이벤트 구독
        if (HpManager.Instance != null)
            HpManager.Instance.OnPlayerDead += HandlePlayerDead;

        // 셔터 오픈 연출 → 완료되면 게임 시작
        string openText = string.Format(roundTextFormat, stageIndex);
        if (stageShutter != null)
            stageShutter.PlayOpenSequence(openText, OnGameStart);
        else
            OnGameStart(); // 셔터 없으면 바로 시작
    }

    void OnDestroy()
    {
        if (HpManager.Instance != null)
            HpManager.Instance.OnPlayerDead -= HandlePlayerDead;
    }

    // ──────────────────────────────────────────────
    // 외부 호출 (GameTimer → 스테이지 클리어)
    // ──────────────────────────────────────────────

    /// <summary>GameTimer가 0에 도달했을 때 호출합니다.</summary>
    public void OnStageClear()
    {
        if (IsGameStopped) return;

        SaveToGameData();
        IsGameStopped = true;
        Time.timeScale = 0f;

        bool isLastStage = stageIndex >= stageSceneNames.Length;
        string closeText = isLastStage
            ? gameClearCloseText
            : string.Format(roundTextFormat, stageIndex + 1);
        string targetScene = isLastStage
            ? gameClearSceneName
            : stageSceneNames[stageIndex]; // 1-based index → 0-based 다음 스테이지

        if (stageShutter != null)
            stageShutter.PlayCloseSequence(closeText, () => LoadScene(targetScene));
        else
            LoadScene(targetScene);
    }

    // ──────────────────────────────────────────────
    // 내부 처리
    // ──────────────────────────────────────────────

    /// <summary>셔터 오픈 완료 후 호출됩니다. 게임 로직을 활성화합니다.</summary>
    private void OnGameStart()
    {
        IsGameStopped = false;
    }

    private void HandlePlayerDead()
    {
        if (IsGameStopped) return;

        SaveToGameData();
        IsGameStopped = true;
        Time.timeScale = 0f;

        if (stageShutter != null)
            stageShutter.PlayCloseSequence(gameOverCloseText, () => LoadScene(gameOverSceneName));
        else
            LoadScene(gameOverSceneName);
    }

    private void SaveToGameData()
    {
        if (GameData.Instance == null) return;
        if (HpManager.Instance != null)
            GameData.Instance.CurrentHp = HpManager.Instance.CurrentHp;
        if (ScoreManager.Instance != null)
            GameData.Instance.CurrentScore = ScoreManager.Instance.Score;
    }

    private void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        IsGameStopped = false;
        SceneManager.LoadScene(sceneName);
    }
}
