using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 플레이 버튼의 클릭 동작을 담당하는 스크립트
/// - 씬 시작 시 셔터가 "WELCOME" 텍스트와 함께 위로 올라가며 열립니다.
/// - 클릭 시 GameData를 초기화하고 셔터가 "ROUND 1" 텍스트와 함께 내려오며 Stage1Scene으로 전환됩니다.
///
/// [설정 안내]
/// 1. Play Button 오브젝트에 이 스크립트를 부착합니다.
/// 2. stageShutter 필드에 씬의 StageShutter 컴포넌트를 할당합니다.
/// 3. stage1SceneName에 첫 번째 스테이지 씬 이름을 입력합니다. (기본값: "Stage1Scene")
/// 4. Button 컴포넌트의 OnClick()에 OnPlayButtonClicked()를 연결하거나
///    Awake에서 자동으로 연결됩니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlayButton : MonoBehaviour
{
    [Tooltip("씬의 StageShutter 컴포넌트를 할당하세요")]
    public StageShutter stageShutter;

    [Tooltip("첫 번째 스테이지 씬 이름")]
    public string stage1SceneName = "Stage1Scene";

    [Tooltip("플레이 버튼 클릭 시 셔터에 표시할 텍스트")]
    public string shutterText = "ROUND 1";

    [Tooltip("씬 시작 시 셔터에 표시할 텍스트")]
    public string welcomeText = "WELCOME";

    private bool isTransitioning = false;

    void Awake()
    {
        // Button 컴포넌트에 자동으로 클릭 이벤트 연결
        GetComponent<Button>().onClick.AddListener(OnPlayButtonClicked);
    }

    void Start()
    {
        // StartScene 시작 시 셔터를 닫힌 상태(WELCOME 텍스트)에서 자동으로 위로 엽니다.
        if (stageShutter != null)
            stageShutter.PlayOpenSequence(welcomeText, null);
    }

    /// <summary>
    /// 플레이 버튼 클릭 시 호출됩니다.
    /// 게임 데이터를 초기화하고 셔터 클로즈 연출 후 Stage1Scene으로 이동합니다.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // HP·점수 초기화 (새 게임 시작)
        GameData.ResetAll(3);

        if (stageShutter != null)
        {
            stageShutter.PlayCloseSequence(shutterText, LoadStage1);
        }
        else
        {
            LoadStage1();
        }
    }

    private void LoadStage1()
    {
        SceneManager.LoadScene(stage1SceneName);
    }
}
