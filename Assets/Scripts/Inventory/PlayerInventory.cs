using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 재료 수량을 관리하는 싱글톤 스크립트
/// - 재료 타입별 보유 수량 저장
/// - 수량 변경 시 이벤트로 UI에 알림
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PlayerInventory Instance { get; private set; }

    // 재료 타입별 보유 수량
    private Dictionary<IngredientType, int> inventory = new Dictionary<IngredientType, int>();

    /// <summary>
    /// 재료 수량이 변경될 때 발생하는 이벤트
    /// 인자: (변경된 재료 타입, 변경 후 수량)
    /// UI 스크립트가 이 이벤트를 구독해 실시간으로 화면을 갱신합니다.
    /// </summary>
    public event Action<IngredientType, int> OnIngredientChanged;

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

    /// <summary>
    /// 재료를 1개 추가합니다. (플레이어가 수집 시 호출)
    /// </summary>
    public void AddIngredient(IngredientType type)
    {
        if (!inventory.ContainsKey(type))
            inventory[type] = 0;

        inventory[type]++;
        OnIngredientChanged?.Invoke(type, inventory[type]);
    }

    /// <summary>
    /// 재료를 1개 소비합니다. (레시피 완성 시 호출 예정)
    /// </summary>
    public void RemoveIngredient(IngredientType type)
    {
        if (!inventory.ContainsKey(type) || inventory[type] <= 0) return;

        inventory[type]--;
        OnIngredientChanged?.Invoke(type, inventory[type]);
    }

    /// <summary>
    /// 지정한 재료의 보유 수량을 반환합니다. 없으면 0.
    /// </summary>
    public int GetCount(IngredientType type)
    {
        return inventory.ContainsKey(type) ? inventory[type] : 0;
    }

    /// <summary>
    /// 지정한 재료를 1개 이상 보유 중인지 확인합니다.
    /// </summary>
    public bool HasIngredient(IngredientType type)
    {
        return GetCount(type) > 0;
    }

    /// <summary>
    /// 현재 1개 이상 보유 중인 재료 목록을 반환합니다.
    /// </summary>
    public List<IngredientType> GetOwnedTypes()
    {
        List<IngredientType> result = new List<IngredientType>();
        foreach (var kv in inventory)
        {
            if (kv.Value > 0)
                result.Add(kv.Key);
        }
        return result;
    }

    /// <summary>
    /// 라운드 종료 또는 레시피 완성 시 인벤토리를 초기화합니다.
    /// </summary>
    public void ClearInventory()
    {
        List<IngredientType> keys = new List<IngredientType>(inventory.Keys);
        foreach (var key in keys)
        {
            inventory[key] = 0;
            OnIngredientChanged?.Invoke(key, 0);
        }
    }
}
