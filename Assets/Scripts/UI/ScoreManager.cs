using System;
using UnityEngine;

/// <summary>
/// 게임 점수를 관리하는 싱글톤 스크립트
///
/// [점수 규칙]
/// - 레시피 완성 시 사용된 재료 수 × 100점 추가
///
/// [사용 방법]
/// - ScoreManager.Instance.AddScore(amount) 로 점수 추가
/// - ScoreManager.Instance.Score 로 현재 점수 조회
/// - OnScoreChanged 이벤트로 UI 연동
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>점수가 변경될 때 발생합니다. (변경 후 총점)</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>현재 누적 점수</summary>
    public int Score { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>점수를 추가합니다.</summary>
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>점수를 0으로 초기화합니다. (새 게임 시작 시 호출)</summary>
    public void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
