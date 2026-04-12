using UnityEngine;

/// <summary>
/// 상한 식재료 피격 시 플레이어에서 펑 터져나오는 식재료 비주얼 전용 스크립트
/// - IngredientItem 컴포넌트가 없으므로 PlayerCollector가 수집하지 않습니다.
/// - Rigidbody2D로 초기 힘을 받아 퍼지며, 살짝 회전합니다.
/// - 일정 시간 후 또는 화면 아래로 나가면 자동 삭제됩니다.
///
/// [프리팹 설정 안내]
/// 1. 빈 오브젝트를 만들고 SpriteRenderer를 추가합니다. (스프라이트는 런타임에 설정됨)
/// 2. Rigidbody2D 추가 (Gravity Scale = 1, Dynamic)
/// 3. Collider2D는 불필요합니다 (수집 불가, 충돌 불필요)
/// 4. 이 스크립트(DroppedIngredient) 부착
/// 5. 프리팹으로 저장 후 PlayerCollector의 droppedIngredientPrefab에 할당합니다.
/// </summary>
public class DroppedIngredient : MonoBehaviour
{
    [Header("삭제 설정")]
    [Tooltip("이 Y좌표 아래로 떨어지면 오브젝트가 삭제됩니다")]
    public float destroyYPosition = -6f;

    [Tooltip("생성 후 이 시간(초)이 지나면 자동 삭제됩니다")]
    public float autoDestroyTime = 3f;

    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (transform.position.y < destroyYPosition || Time.time - spawnTime > autoDestroyTime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 주어진 방향과 힘으로 발사합니다. 회전력(torque)도 같이 적용됩니다.
    /// PlayerCollector의 BurstOwnedIngredients()에서 호출됩니다.
    /// </summary>
    public void Launch(Vector2 direction, float force, float torque)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
            rb.AddTorque(torque, ForceMode2D.Impulse);
        }
    }
}
