using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 버튼의 클릭 동작을 담당하는 스크립트
///
/// [설정 안내]
/// 1. Tutorial Button 오브젝트에 이 스크립트를 부착합니다.
/// 2. tutorialPanel 필드에 씬의 TutorialPanel 컴포넌트를 할당합니다.
/// 3. Button 컴포넌트의 OnClick() 이벤트에 OnTutorialButtonClicked()를 연결합니다.
///    또는 Awake에서 자동으로 연결됩니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class TutorialButton : MonoBehaviour
{
    [Tooltip("씬의 TutorialPanel 컴포넌트를 할당하세요")]
    public TutorialPanel tutorialPanel;

    void Awake()
    {
        // Button 컴포넌트에 자동으로 클릭 이벤트 연결
        GetComponent<Button>().onClick.AddListener(OnTutorialButtonClicked);
    }

    /// <summary>
    /// 튜토리얼 버튼 클릭 시 호출됩니다. 튜토리얼 패널을 위에서 내립니다.
    /// </summary>
    public void OnTutorialButtonClicked()
    {
        if (tutorialPanel != null)
            tutorialPanel.ShowPanel();
    }
}
