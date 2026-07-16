using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicVolumeController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text valueText;

    private const string VolumeKey = "MusicVolume";

    void Awake()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.55f);

        if (musicSource != null)
            musicSource.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        UpdateValueText(volume);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = volume;

        UpdateValueText(volume);
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    void UpdateValueText(float volume)
    {
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
    }
}