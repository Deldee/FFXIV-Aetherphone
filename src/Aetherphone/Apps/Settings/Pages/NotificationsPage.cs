using Aetherphone.Core;
using Aetherphone.Core.Apps;
using Aetherphone.Core.Home;
using Aetherphone.Core.Localization;
using Aetherphone.Core.Notifications;
using Aetherphone.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System.Globalization;

namespace Aetherphone.Apps.Settings.Pages;

internal sealed class NotificationsPage : ISettingsPage
{
    public string Title => Loc.T(L.Settings.Notifications);
    public string Summary => string.Empty;
    public FontAwesomeIcon Icon => FontAwesomeIcon.Bell;
    public Vector4 Tint => new(0.98f, 0.27f, 0.25f, 1f);
    private readonly Configuration configuration;
    private readonly ISettingsNavigator navigator;
    private readonly AppNotificationPage appPage;
    private readonly AppInstaller installer;
    private readonly IReadOnlyList<IPhoneApp> apps;
    private AppSettingsEntry[] entries = Array.Empty<AppSettingsEntry>();
    private LanguageInfo? entriesLanguage;

    public NotificationsPage(Configuration configuration, ISettingsNavigator navigator, AppNotificationPage appPage,
        AppInstaller installer, IReadOnlyList<IPhoneApp> apps)
    {
        this.configuration = configuration;
        this.navigator = navigator;
        this.appPage = appPage;
        this.installer = installer;
        this.apps = apps;
    }

    public void Draw(in PhoneContext context, Rect body)
    {
        var theme = context.Theme;
        var scale = UiScale.Current;
        using (AppSurface.Begin(body))
        {
            ImGui.Dummy(new Vector2(0f, Metrics.Space.Md * scale));
            var doNotDisturb = configuration.DoNotDisturb;
            var alerts = GroupCard.Begin(theme, 2);
            var quietWhileBusy = SettingsRow.Bool(alerts.NextRow(), Loc.T(L.Settings.QuietWhileBusy),
                configuration.QuietWhileBusy, theme, null, Loc.T(L.Settings.QuietWhileBusyHint),
                dimmed: doNotDisturb);
            var showNotificationBanner = SettingsRow.Bool(alerts.NextRow(), Loc.T(L.Settings.ShowNotificationBanner),
                configuration.ShowNotificationBanner, theme, null, Loc.T(L.Settings.ShowNotificationBannerHint),
                dimmed: doNotDisturb);
            alerts.End();
            if (quietWhileBusy != configuration.QuietWhileBusy)
            {
                configuration.QuietWhileBusy = quietWhileBusy;
                configuration.Save();
            }

            if (showNotificationBanner != configuration.ShowNotificationBanner)
            {
                configuration.ShowNotificationBanner = showNotificationBanner;
                configuration.Save();
            }

            EnsureEntries();
            SettingsSection.Header(Loc.T(L.Settings.NotificationApps), theme);
            var installedCount = CountInstalled(entries);
            var rows = GroupCard.Begin(theme, installedCount);
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                if (!installer.IsInstalled(entry.AppId))
                {
                    continue;
                }

                if (SettingsRow.AppLink(rows.NextRow(), entry.AppId, entry.Accent, entry.Name, Summarize(entry),
                        theme))
                {
                    appPage.Show(entry);
                    navigator.Open(appPage);
                }
            }

            rows.End();
        }
    }

    private void EnsureEntries()
    {
        if (ReferenceEquals(entriesLanguage, Loc.Current))
        {
            return;
        }

        var count = 0;
        for (var index = 0; index < apps.Count; index++)
        {
            if (Qualifies(apps[index]))
            {
                count++;
            }
        }

        var built = new AppSettingsEntry[count];
        var writeIndex = 0;
        for (var index = 0; index < apps.Count; index++)
        {
            var app = apps[index];
            if (!Qualifies(app))
            {
                continue;
            }

            built[writeIndex++] = new AppSettingsEntry(app.Id, app.DisplayName, app.Accent,
                NotificationChannels.Contains(app.Id), app.HasBadge);
        }

        Array.Sort(built, static (left, right) =>
        {
            var primary = Loc.Culture.CompareInfo.Compare(left.Name, right.Name, CompareOptions.IgnoreCase);
            return primary != 0 ? primary : string.CompareOrdinal(left.AppId, right.AppId);
        });

        entries = built;
        entriesLanguage = Loc.Current;
    }

    private static bool Qualifies(IPhoneApp app) => app.HasBadge || NotificationChannels.Contains(app.Id);

    private int CountInstalled(AppSettingsEntry[] source)
    {
        var count = 0;
        for (var index = 0; index < source.Length; index++)
        {
            if (installer.IsInstalled(source[index].AppId))
            {
                count++;
            }
        }

        return count;
    }

    private string Summarize(AppSettingsEntry entry)
    {
        if (entry.HasChannel && !configuration.IsAppNotificationEnabled(entry.AppId))
        {
            return Loc.T(L.Settings.NotificationsOff);
        }

        if (entry.HasBadge && !configuration.IsAppBadgeEnabled(entry.AppId))
        {
            return Loc.T(L.Settings.NotificationsOff);
        }

        return string.Empty;
    }
}
