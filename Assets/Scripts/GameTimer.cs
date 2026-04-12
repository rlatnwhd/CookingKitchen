using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지 제한 시간을 카운트다운하여 화면에 표시하는 스크립트
/// - 40.0초부터 0.0초까지 소수점 한 자리로 표시
/// - 0초 도달 시 GameManager.Instance.OnStageClear()를 호출하여 씬 전환 시작
///
/// [설정 안내]
/// 1. 빈 오브젝트에 이 스크립트를 부착합니다.
/// 2. timerText에 Canvas 안의 TimerText (TextMeshProUGUI)를 할당합니다.
/// 3. totalTime은 기본 40초입니다 (인스펙터에서 조절 가능).
/// </summary>
public class GameTimer : MonoBehaviour
{
    [Header("타이머 설정")]
    [Tooltip("스테이지 제한 시간 (초)")]
    public float totalTime = 40f;

    [Header("UI 연결")]
    [Tooltip("TimerText - TextMeshProUGUI 할당")]
    public TextMeshProUGUI timerText;

    private float timeRemaining;
    private bool isRunning;

    void Start()
    {
        timeRemaining = totalTime;
        isRunning = true;
        UpdateDisplay();
    }

    void Update()
    {
        // 게임이 멈췄거나 이미 종료된 경우 진행 안 함 (셔터 오픈 중 포함)
        if (!isRunning || GameManager.IsGameStopped) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            UpdateDisplay();
            GameManager.Instance?.OnStageClear();
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// timerText에 남은 시간을 소수점 한 자리("F1")로 출력합니다.
    /// </summary>
    private void UpdateDisplay()
    {
        if (timerText != null)
            timerText.text = timeRemaining.ToString("F1");
    }
}
