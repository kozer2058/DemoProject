using UnityEngine;
// 1. BẮT BUỘC phải thêm thư viện này
using UnityEngine.InputSystem; 

public class TutorialController : MonoBehaviour
{
    public GameObject tutorialPanel;

    void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void Update()
    {
        // 2. Sử dụng cú pháp của New Input System để kiểm tra phím K vừa được bấm
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            ToggleTutorial();
        }
    }

    public void ToggleTutorial()
    {
        if (tutorialPanel != null)
        {
            bool isActive = !tutorialPanel.activeSelf;
            tutorialPanel.SetActive(!tutorialPanel.activeSelf);
            Time.timeScale = isActive ? 0f : 1f;
        }
    }
}