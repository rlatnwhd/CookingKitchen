using UnityEngine;

/// <summary>
/// 스테이지 간 유지해야 할 데이터를 저장하는 퍼시스턴트 싱글톤
/// DontDestroyOnLoad로 씬이 전환되어도 파괴되지 않습니다.
///
/// [저장 항목]
/// - 현재 HP
/// - 현재 누적 점수
///
/// [사용 방법]
/// - Stage1Scene에 빈 오브젝트를 만들고 이 스크립트를 부착합니다.
/// - 이후 씬에서도 같은 오브젝트를 배치해두면 자동으로 중복 제거됩니다.
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    /// <summary>현재 HP (씬 이동 후 HpManager가 이 값으로 초기화)</summary>
    public int CurrentHp { get; set; } = 3;

    /// <summary>현재 누적 점수 (씬 이동 후 ScoreManager가 이 값으로 초기화)</summary>
    public int CurrentScore { get; set; } = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 퍼시스턴트 GameData가 있으면 이번 씬의 것은 제거
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 새 게임 시작 시 데이터를 초기값으로 리셋합니다.
    /// </summary>
    public void ResetAll(int maxHp)
    {
        CurrentHp = maxHp;
        CurrentScore = 0;
    }
}
