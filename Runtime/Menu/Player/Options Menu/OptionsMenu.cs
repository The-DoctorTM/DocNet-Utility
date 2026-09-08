using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace DocNet.Menus.Player
{
    public class OptionsMenu : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _musicSlider;

        private const string masterParam = "master";
        private const string sfxParam = "sfx";
        private const string musicParam = "music";

        private void Start()
        {
            if (!_audioMixer) { Debug.LogWarning("AudioMixer not assigned!"); return; }

            InitializeSlider(_masterSlider, masterParam);
            InitializeSlider(_sfxSlider, sfxParam);
            InitializeSlider(_musicSlider, musicParam);
        }

        private void InitializeSlider(Slider slider, string parameter)
        {
            if (!slider) { Debug.LogWarning($"Slider for {parameter} not assigned!"); return; }

            float savedValue = PlayerPrefs.GetFloat(parameter, GetMixerValue(parameter));
            slider.value = savedValue;
            SetMixerValue(parameter, savedValue); // Apply to mixer on start
            slider.onValueChanged.AddListener(value => SetMixerValue(parameter, value));
        }

        private float GetMixerValue(string parameter)
        {
            if (!_audioMixer) { Debug.LogWarning("AudioMixer not assigned!"); return 0f; }
            _audioMixer.GetFloat(parameter, out float value);
            return value;
        }

        private void SetMixerValue(string parameter, float value)
        {
            if (!_audioMixer) { Debug.LogWarning("AudioMixer not assigned!"); return; }
            _audioMixer.SetFloat(parameter, value);
            PlayerPrefs.SetFloat(parameter, value);

        }

    }
}