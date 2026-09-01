using Aetherphone.Core;
using Aetherphone.Core.Apps;
using Aetherphone.Core.Localization;
using Aetherphone.Core.Notifications;
using Aetherphone.Core.Theme;
using Aetherphone.Windows.Components;
using Dalamud.Bindings.ImGui;

using Dalamud.Interface;

namespace Aetherphone.Apps.Settings.Pages;

internal sealed class AppNotificationPage : ISettingsPage
{
    public string Title => entry.Name;
    public string Summary => string.Empty;
    public FontAwesomeIcon Icon => FontAwesomeIcon.Bell;
    public Vector4 Tint => entry.Accent;
    private readonly Configuration configuration;
    private readonly SoundService sound;
    private AppSettingsEntry entry = new(string.Empty, string.Empty, default, false, false);

    public AppNotificationPage(Configuration configuration, SoundService sound)
    {
        this.configuration = configuration;
        this.sound = sound;
    }

    public void Show(AppSettingsEntry target) => entry = target;

    public void Draw(in PhoneContext context, Rect body)
    {
        var theme = context.Theme;
        var scale = UiScale.Current;
        using (AppSurface.Begin(body))
        {
            var drewAlerts = DrawAlertsSection(theme, scale);
            if (!entry.HasBadge)
            {
                return;
            }

            if (drewAlerts)
            {
                ImGui.Dummy(new Vector2(0f, Metrics.Space.Lg * scale));
            }

            DrawBadgeSection(theme);
        }
    }

    private bool DrawAlertsSection(PhoneTheme theme, float scale)
    {
        if (!entry.HasChannel)
        {
            return false;
        }

        SettingsSection.Header(Loc.T(L.Common.Alerts), theme);
        var appSetting = configuration.NotificationSettingFor(entry.AppId);
        var wasEnabled = configuration.IsAppNotificationEnabled(entry.AppId);
        var card = GroupCard.Begin(theme, wasEnabled ? 2 : 1);
        var enabled = SettingsRow.Bool(card.NextRow(), Loc.T(L.Settings.AllowNotifications), wasEnabled, theme);

        if (wasEnabled)
        {
            var showNotificationBanner = SettingsRow.Bool(card.NextRow(),
                Loc.T(L.Settings.ShowNotificationBanner), appSetting.ShowNotificationBanner, theme);
            if (showNotificationBanner != appSetting.ShowNotificationBanner)
            {
                appSetting.ShowNotificationBanner = showNotificationBanner;
                configuration.Save();
            }
        }

        card.End();
        if (enabled != wasEnabled)
        {
            appSetting.Enabled = enabled;
            configuration.Save();
        }

        if (!wasEnabled)
        {
            return true;
        }

        ImGui.Dummy(new Vector2(0f, Metrics.Space.Lg * scale));
        SettingsSection.Header(Loc.T(L.Settings.Sound), theme);
        SoundOptionList.Draw(theme, sound, SoundKind.Notification, configuration.AppSoundOverride(entry.AppId),
            true, Select);
        return true;
    }

    private void DrawBadgeSection(PhoneTheme theme)
    {
        SettingsSection.Header(Loc.T(L.Home.HomeScreen), theme);
        var badgeEnabled = configuration.IsAppBadgeEnabled(entry.AppId);
        var card = GroupCard.Begin(theme, 1);
        var updated = SettingsRow.Bool(card.NextRow(), Loc.T(L.Settings.ShowBadge), badgeEnabled, theme);
        card.End();
        if (updated == badgeEnabled)
        {
            return;
        }

        configuration.SetAppBadgeEnabled(entry.AppId, updated);
        configuration.Save();
    }

    private void Select(string? token)
    {
        var setting = configuration.NotificationSettingFor(entry.AppId);
        if (!string.Equals(setting.Sound, token, StringComparison.Ordinal))
        {
            setting.Sound = token;
            configuration.Save();
        }

        sound.Preview(SoundKind.Notification, token ?? configuration.NotificationSound,
            configuration.NotificationVolume);
    }
}
