using UnityEngine;

/// <summary>
/// 스테이지 간 HP·점수를 유지하는 데이터 클래스
/// ─ DontDestroyOnLoad를 사용하지 않습니다.
/// ─ 데이터는 static 필드에 저장되므로 씬이 바뀌어도 값이 유지됩니다.
/// ─ 각 씬의 GameManager 오브젝트에 컴포넌트로 부착해도 되고 부착하지 않아도 됩니다.
///   (MonoBehaviour는 레거시 호환용으로 남겨둠)
/// </summary>
public class GameData : MonoBehaviour
{
    // ── static 필드: 씬 전환 후에도 값이 유지됨 ──────────────
    private static int _hp    = 3;
    private static int _score = 0;

    /// <summary>현재 HP</summary>
    public static int CurrentHp
    {
        get => _hp;
        set => _hp = value;
    }

    /// <summary>현재 누적 점수</summary>
    public static int CurrentScore
    {
        get => _score;
        set => _score = value;
    }

    /// <summary>새 게임 시작 시 HP·점수를 초기값으로 리셋합니다.</summary>
    public static void ResetAll(int maxHp = 3)
    {
        _hp    = maxHp;
        _score = 0;
    }
}

