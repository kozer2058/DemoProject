using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StorySceneController : MonoBehaviour
{
    [Header("Story Pages")]
    [SerializeField] private GameObject[] commonPages;
    [SerializeField] private GameObject timedMissionPage;
    [SerializeField] private GameObject endlessMissionPage;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text nextButtonText;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private readonly List<GameObject> activePages = new();
    private int currentPage;

    void Awake()
    {
        Time.timeScale = 1f;

        BuildPageList();

        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipStory);

        if (nextButtonText == null && nextButton != null)
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);

        if (activePages.Count > 0)
            ShowPage(0);
        else
            Debug.LogError("StorySceneController has no story pages.", this);
    }

    void BuildPageList()
    {
        activePages.Clear();

        if (commonPages != null)
        {
            foreach (GameObject page in commonPages)
            {
                if (page == null)
                    continue;

                page.SetActive(false);
                activePages.Add(page);
            }
        }

        if (timedMissionPage != null)
            timedMissionPage.SetActive(false);

        if (endlessMissionPage != null)
            endlessMissionPage.SetActive(false);

        GameObject finalPage = GameSession.SelectedMode == GameMode.Endless
            ? endlessMissionPage
            : timedMissionPage;

        if (finalPage != null)
            activePages.Add(finalPage);
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
    }

    public void NextPage()
    {
        if (currentPage >= activePages.Count - 1)
        {
            LoadGame();
            return;
        }

        ShowPage(currentPage + 1);
    }

    public void SkipStory()
    {
        LoadGame();
    }

    void ShowPage(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, activePages.Count - 1);

        for (int i = 0; i < activePages.Count; i++)
        {
            if (activePages[i] != null)
                activePages[i].SetActive(i == currentPage);
        }

        if (previousButton != null)
            previousButton.interactable = currentPage > 0;

        bool isLastPage = currentPage == activePages.Count - 1;

        if (nextButtonText != null)
            nextButtonText.text = isLastPage ? "DEPLOY" : "NEXT >";

        if (progressText != null)
            progressText.text = $"{currentPage + 1:00} / {activePages.Count:00}";
    }

    void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipStory);
    }
}