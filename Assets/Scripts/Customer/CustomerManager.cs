using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손님 슬롯 전체를 관리하는 스크립트
///
/// [설정 방법]
/// 1. 씬에 빈 GameObject를 손님이 서 있을 위치마다 배치하고 slotTransforms에 좌→우 순서로 할당
///    → 각 GameObject의 Z값으로 배경 레이어 사이를 조정하세요
///      (예: 뒷 배경 Z=1.0, 앞 배경 Z=0.1 → 슬롯 오브젝트 Z=0.5)
/// 2. Customer 컴포넌트가 붙은 손님 프리팹을 customerPrefab에 할당
/// 3. 손님 스프라이트들을 customerSprites 배열에 할당
/// 4. RecipeQueueManager 인스펙터의 customerManager 필드에 이 컴포넌트를 할당
///
/// [동작 로직]
/// - 레시피 카드 등장 → 왼쪽(인덱스 0)부터 빈 슬롯을 찾아 손님 생성
/// - 레시피 완성 → 연결된 손님이 아래로 퇴장 후 슬롯 해제
/// - 슬롯 위치는 고정 (손님이 퇴장해도 자리 이동 없음)
/// - 빈 자리는 다음 손님이 왼쪽 우선으로 채움
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("슬롯 위치 설정")]
    [Tooltip("손님이 서 있을 위치의 Transform. 좌→우 순서로 할당하세요.\n" +
             "각 오브젝트의 Z값으로 배경 사이 레이어를 설정합니다 (뒷배경~앞배경 사이 Z)")]
    public Transform[] slotTransforms;

    [Header("손님 프리팹")]
    [Tooltip("Customer 컴포넌트가 붙어있는 손님 프리팹")]
    public GameObject customerPrefab;

    [Header("손님 스프라이트")]
    [Tooltip("손님 생성 시 이 목록에서 랜덤으로 스프라이트를 선택합니다")]
    public Sprite[] customerSprites;

    // 슬롯별 현재 손님 (null = 빈 슬롯)
    private Customer[] slots;

    // RecipeUI → Customer 빠른 조회 테이블
    private Dictionary<RecipeUI, Customer> cardToCustomer = new Dictionary<RecipeUI, Customer>();

    void Awake()
    {
        int count = slotTransforms != null ? slotTransforms.Length : 0;
        slots = new Customer[count];
    }

    // ─────────────────── RecipeQueueManager 연동 ───────────────────

    /// <summary>
    /// 레시피 카드가 스폰될 때 RecipeQueueManager에서 호출합니다.
    /// [순서] 손님 등장 → 등장 완료 콜백 → 말풍선 표시 → onReady 콜백
    /// onReady 콜백 안에서 레시피 카드를 스폰하면 됩니다.
    /// </summary>
    /// <param name="card">연결될 RecipeUI</param>
    /// <param name="foodSprite">말풍선에 표시할 음식 스프라이트</param>
    /// <param name="onReady">손님 등장 + 말풍선 표시 완료 후 호출되는 콜백</param>
    public void OnRecipeSpawned(RecipeUI card, Sprite foodSprite, System.Action onReady)
    {
        if (card == null || customerPrefab == null) { onReady?.Invoke(); return; }
        if (slotTransforms == null || slotTransforms.Length == 0) { onReady?.Invoke(); return; }

        int slotIndex = FindEmptySlot();
        if (slotIndex < 0)
        {
            // 빈 슬롯이 없으면 손님 없이 콜백만 즉시 호출
            onReady?.Invoke();
            return;
        }

        GameObject obj = Instantiate(customerPrefab);
        Customer customer = obj.GetComponent<Customer>();
        if (customer == null)
        {
            Destroy(obj);
            onReady?.Invoke();
            return;
        }

        // 랜덤 손님 스프라이트
        Sprite sprite = null;
        if (customerSprites != null && customerSprites.Length > 0)
            sprite = customerSprites[UnityEngine.Random.Range(0, customerSprites.Length)];

        Vector3 worldPos = slotTransforms[slotIndex].position;
        customer.Initialize(slotIndex, worldPos, card, sprite, foodSprite);

        slots[slotIndex] = customer;
        cardToCustomer[card] = customer;

        // 등장 완료 콜백: 말풍선 팝업 + 레시피 카드 슬라이드 인을 동시에 시작
        customer.OnAppearComplete += () =>
        {
            customer.ShowBubble();
            onReady?.Invoke(); // 딜레이 없이 동시 실행
        };
    }

    /// <summary>
    /// 레시피가 완성될 때 호출. 만족 리액션 후 퇴장.
    /// </summary>
    public void OnRecipeCompleted(RecipeUI card)
    {
        if (card == null) return;
        if (!cardToCustomer.TryGetValue(card, out Customer customer)) return;

        cardToCustomer.Remove(card);
        if (customer == null) return;

        int idx = customer.SlotIndex;
        if (idx >= 0 && idx < slots.Length && slots[idx] == customer)
            slots[idx] = null;

        customer.DisappearSatisfied();
    }

    /// <summary>
    /// 제한시간 초과 시 호출. 불만 리액션 후 퇴장.
    /// </summary>
    public void OnRecipeTimedOut(RecipeUI card)
    {
        if (card == null) return;
        if (!cardToCustomer.TryGetValue(card, out Customer customer)) return;

        cardToCustomer.Remove(card);
        if (customer == null) return;

        int idx = customer.SlotIndex;
        if (idx >= 0 && idx < slots.Length && slots[idx] == customer)
            slots[idx] = null;

        customer.DisappearUnsatisfied();
    }

    /// <summary>
    /// 모든 손님을 즉시 제거합니다. 라운드 전환 등 전체 리셋 시 호출합니다.
    /// </summary>
    public void ClearAll()
    {
        if (slots != null)
        {
            foreach (var customer in slots)
            {
                if (customer != null)
                    Destroy(customer.gameObject);
            }
            for (int i = 0; i < slots.Length; i++)
                slots[i] = null;
        }
        cardToCustomer.Clear();
    }

    // ─────────────────── 내부 유틸 ───────────────────

    private int FindEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }
        return -1;
    }
}
