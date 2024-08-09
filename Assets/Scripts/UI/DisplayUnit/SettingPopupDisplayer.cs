using UnityEngine;

public class SettingPopupDisplayer : TopBannerPopup
{
    [SerializeField] private SettingSlider musicSetting;
    [SerializeField] private SettingSlider soundSetting;
    [SerializeField] private SettingSlider vibrationSetting;
    [SerializeField] private SettingSlider hintSetting;

    private void UpdateDisplay()
    {
        musicSetting.SetValue(UserDataManager.MusicOn);
        soundSetting.SetValue(UserDataManager.SoundOn);
        vibrationSetting.SetValue(UserDataManager.VibrationOn);
        hintSetting.SetValue(UserDataManager.HintOn);
    }

    public override void ShowMe()
    {
        UpdateDisplay();
        base.ShowMe();
    }

    public void SetMusic(bool value)
    {
        UserDataManager.SetMusic(value);
    }

    public void SetSound(bool value)
    {
        UserDataManager.SetSound(value);
    }

    public void SetVibration(bool value)
    {
        UserDataManager.SetVibration(value);
    }

    public void SetHint(bool value)
    {
        UserDataManager.SetHint(value);
    }
}
