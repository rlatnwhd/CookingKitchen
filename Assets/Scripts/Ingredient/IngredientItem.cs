using UnityEngine;

/// <summary>
/// 하늘에서 떨어지는 재료 오브젝트에 부착하는 스크립트
/// - 자신의 재료 타입을 저장
/// - 일정 Y좌표 아래로 내려가면 오브젝트 삭제
///
/// [프리팹 설정 안내]
/// - Rigidbody2D : Gravity Scale > 0 (중력으로 낙하)
/// - Collider2D  : isTrigger = ON (플레이어와 물리 충돌 없이 통과)
///   → isTrigger이므로 재료가 플레이어 콜라이더에 막히지 않음
///   → 플레이어가 재료를 수집하는 로직은 플레이어 스크립트의
///     OnTriggerEnter2D에서 처리 예정
/// </summary>
public class IngredientItem : MonoBehaviour
{
    [Header("재료 설정")]
    [Tooltip("이 오브젝트의 재료 종류 (스포너가 자동 설정)")]
    public IngredientType ingredientType;

    [Header("삭제 설정")]
    [Tooltip("이 Y좌표 아래로 떨어지면 오브젝트가 삭제됩니다")]
    public float destroyYPosition = -6f;

    void Update()
    {
        CheckOutOfBounds();
    }

    /// <summary>
    /// 매 프레임마다 Y좌표를 확인하여 설정값 아래로 내려가면 오브젝트를 삭제합니다.
    /// </summary>
    private void CheckOutOfBounds()
    {
        if (transform.position.y < destroyYPosition)
        {
            Destroy(gameObject);
        }
    }
}
