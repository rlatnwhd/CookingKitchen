using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 손님 오브젝트를 제어하는 스크립트
///
/// [동작 방식]
/// - CustomerManager가 슬롯 위치에 이 프리팹을 생성
/// - 지정된 슬롯 위치 아래에서 위로 팝업 애니메이션으로 등장 (월드 좌표 Y 이동)
/// - 레시피 완성 시 아래로 내려가며 사라지는 퇴장 애니메이션
///
/// [프리팹 구성]
/// - SpriteRenderer 컴포넌트를 가진 오브젝트에 이 스크립트 부착
/// - spriteRenderer 필드에 SpriteRenderer 할당
///
/// [Z 위치 설정]
/// - CustomerManager의 슬롯 Transform Z값으로 배경 레이어 사이에 배치
/// - (예: 배경 뒷면 Z=1.0, 배경 앞면 Z=0.1 → 슬롯 Z=0.5 로 설정)
/// </summary>
public class Customer : MonoBehaviour
{
    [Header("스프라이트 렌더러")]
    [Tooltip("손님 이미지를 표시하는 SpriteRenderer 컴포넌트")]
    public SpriteRenderer spriteRenderer;

    [Header("애니메이션 설정")]
    [Tooltip("등장 애니메이션 지속 시간 (초)")]
    public float appearDuration = 0.5f;

    [Tooltip("퇴장 애니메이션 지속 시간 (초)")]
    public float disappearDuration = 0.4f;

    [Tooltip("숨겨진 상태의 Y 오프셋 — 슬롯 위치 기준으로 이 값만큼 아래에서 등장합니다 (양수 입력)")]
    public float hiddenYOffset = 2f;

    /// <summary>이 손님과 연결된 레시피 카드 (RecipeQueueManager에서 사용)</summary>
    public RecipeUI LinkedCard { get; private set; }

    /// <summary>배정된 슬롯 인덱스 (0 = 첫 번째 슬롯)</summary>
    public int SlotIndex { get; private set; }

    // 표시될 목표 위치 (슬롯의 월드 좌표)
    private Vector3 targetPosition;

    // 숨겨진 위치 (슬롯 Y에서 hiddenYOffset만큼 아래)
    private Vector3 hiddenPosition;

    /// <summary>
    /// 손님을 초기화하고 등장 애니메이션을 시작합니다.
    /// CustomerManager에서 호출합니다.
    /// </summary>
    /// <param name="slotIndex">배정된 슬롯 번호</param>
    /// <param name="worldPos">슬롯 월드 위치 (Z 포함)</param>
    /// <param name="card">연결된 레시피 카드</param>
    /// <param name="sprite">표시할 스프라이트 (null이면 프리팹 기본 유지)</param>
    public void Initialize(int slotIndex, Vector3 worldPos, RecipeUI card, Sprite sprite)
    {
        SlotIndex = slotIndex;
        LinkedCard = card;
        targetPosition = worldPos;
        hiddenPosition = new Vector3(worldPos.x, worldPos.y - hiddenYOffset, worldPos.z);

        if (spriteRenderer != null && sprite != null)
            spriteRenderer.sprite = sprite;

        transform.position = hiddenPosition;
        StartCoroutine(AppearCoroutine());
    }

    /// <summary>
    /// 퇴장 애니메이션을 시작합니다. 완료 후 오브젝트를 자동으로 삭제합니다.
    /// </summary>
    public void Disappear()
    {
        StartCoroutine(DisappearCoroutine());
    }

    // ───────────────────── 내부 코루틴 ─────────────────────

    private IEnumerator AppearCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / appearDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f); // EaseOut Cubic
            transform.position = Vector3.Lerp(hiddenPosition, targetPosition, smooth);
            yield return null;
        }
        transform.position = targetPosition;
    }

    private IEnumerator DisappearCoroutine()
    {
        Vector3 startPos = transform.position;
        float   elapsed  = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / disappearDuration);
            float smooth = t * t; // EaseIn Quad
            transform.position = Vector3.Lerp(startPos, hiddenPosition, smooth);
            yield return null;
        }
        transform.position = hiddenPosition;
        Destroy(gameObject);
    }
}
