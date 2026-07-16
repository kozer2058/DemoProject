using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button timedModeButton;
    [SerializeField] private Button endlessModeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text lastRecordText;

    void Awake()
    {
        Time.timeScale = 1f;
        BindSceneUI();

        if (timedModeButton != null)
            timedModeButton.onClick.AddListener(PlayTimedMode);
        if (endlessModeButton != null)
            endlessModeButton.onClick.AddListener(PlayEndlessMode);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        RefreshLastRecord();
    }

    void BindSceneUI()
    {
        timedModeButton ??= SceneObjectLookup.FindComponent<Button>("TimedModeButton");
        endlessModeButton ??= SceneObjectLookup.FindComponent<Button>("EndlessModeButton");
        quitButton ??= SceneObjectLookup.FindComponent<Button>("QuitButton");
        lastRecordText ??= SceneObjectLookup.FindComponent<TMP_Text>("LastRecordText");
    }

    public void PlayTimedMode()
    {
        StartMode(GameMode.Timed);
    }

    public void PlayEndlessMode()
    {
        StartMode(GameMode.Endless);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void StartMode(GameMode mode)
    {
        GameSession.PrepareRun(mode);
        SceneManager.LoadScene("StoryScene");
    }

    void RefreshLastRecord()
    {
        if (lastRecordText == null)
            return;

        int score = PlayerPrefs.GetInt("LastEndlessScore", 0);
        if (score <= 0)
        {
            lastRecordText.text = "LAST ENDLESS RUN\nNo record yet";
            return;
        }

        string playerName = PlayerPrefs.GetString("LastEndlessName", "Player");
        float seconds = PlayerPrefs.GetFloat("LastEndlessTime", 0f);
        int kills = PlayerPrefs.GetInt("LastEndlessKills", 0);
        lastRecordText.text =
            $"LAST ENDLESS RUN\n{playerName}  SCORE {score}\nSURVIVED {GameSession.FormatTime(seconds)}  KILLS {kills}";
    }
}
