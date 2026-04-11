using TMPro;
using UnityEngine;

/// <summary>
/// 점수를 TextMeshPro UI에 표시하는 스크립트
///
/// [설정 방법]
/// 1. Canvas 아래 TextMeshPro - Text (UI) 오브젝트를 만들고
///    이 스크립트를 붙인 뒤 scoreText 필드에 연결합니다.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Tooltip("점수를 표시할 TextMeshPro UI")]
    public TextMeshProUGUI scoreText;

    [Tooltip("점수 앞에 붙을 접두사")]
    public string prefix = "SCORE : ";

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(ScoreManager.Instance.Score);
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = prefix + score.ToString();
    }
}
