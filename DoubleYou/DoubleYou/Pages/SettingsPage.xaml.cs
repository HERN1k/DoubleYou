/*  
    MIT License

    Copyright (c) 2024 Vlad Hirnyk (HERN1k)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.ApplicationModel;
using Windows.Storage.Pickers;

using WinRT.Interop;

namespace DoubleYou.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private readonly IServiceProvider m_serviceProvider;
        private readonly ILocalizationService m_localization;
        private readonly IUsersRepository m_usersRepository;
        private bool m_isSettingLanguage;
        private bool m_translationLanguage;
        private bool m_favoriteTopic;
        private bool m_sureWantResetDB;

        public SettingsPage()
        {
            this.InitializeComponent();

            m_serviceProvider = (Application.Current as App)?.ServiceProvider
                ?? throw new ArgumentNullException(nameof(IVocabularyService));
            m_localization = (Application.Current as App)?.ServiceProvider.GetService<ILocalizationService>()
                ?? throw new ArgumentNullException(nameof(ILocalizationService));
            m_usersRepository = (Application.Current as App)?.ServiceProvider.GetService<IUsersRepository>()
                ?? throw new ArgumentNullException(nameof(IUsersRepository));
        }

        #region OnNavigated
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                LanguageSelect.SelectionChanged += LanguageSelected;
                m_localization.CultureChanged += OnCultureChanged;
                TranslationLanguageSelect.SelectionChanged += TranslationLanguageSelected;
                FavoriteTopicSelect.SelectionChanged += FavoriteTopicSelected;
                DataBackupButton.Click += OnCreateDataBackupButtonClick;
                FactoryResetButton.Click += OnFactoryResetButtonClick;

                await UpdateTexts();

                try
                {
                    await UpdateTexts();
                    SetVersion();
                    StartAnimationOnLoad();
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            LanguageSelect.SelectionChanged -= LanguageSelected;
            m_localization.CultureChanged -= OnCultureChanged;
            TranslationLanguageSelect.SelectionChanged -= TranslationLanguageSelected;
            FavoriteTopicSelect.SelectionChanged -= FavoriteTopicSelected;
            DataBackupButton.Click -= OnCreateDataBackupButtonClick;
            FactoryResetButton.Click -= OnFactoryResetButtonClick;
        }
        #endregion

        #region Event handlers
        private async void LanguageSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_isSettingLanguage)
            {
                return;
            }

            try
            {
                var item = (sender as ComboBox)?.SelectedItem as ComboBoxItem;

                if (item != null)
                {
                    var code = item.Tag as string;

                    if (!string.IsNullOrEmpty(code))
                    {
                        bool isSuccess = await m_localization.ChangeCulture(code);

                        if (isSuccess)
                        {
                            return;
                        }
                    }
                }

                ShowPropertyNotChanged();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async void TranslationLanguageSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_translationLanguage)
            {
                return;
            }

            try
            {
                var item = (sender as ComboBox)?.SelectedItem as ComboBoxItem;

                if (item != null)
                {
                    var code = item.Tag as string;

                    if (!string.IsNullOrEmpty(code))
                    {
                        await m_usersRepository.SaveUserNativeLanguageAsync(code);

                        return;
                    }
                }

                ShowPropertyNotChanged();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async void FavoriteTopicSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_favoriteTopic)
            {
                return;
            }

            try
            {
                var item = (sender as ComboBox)?.SelectedItem as ComboBoxItem;

                if (item != null)
                {
                    var code = item.Tag as string;

                    if (!string.IsNullOrEmpty(code))
                    {
                        await m_usersRepository.SaveUserFavoriteTopicAsync(code);

                        return;
                    }
                }

                ShowPropertyNotChanged();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void OnCreateDataBackupButtonClick(object sender, RoutedEventArgs args)
        {
            try
            {
                var picker = new FolderPicker()
                {
                    SuggestedStartLocation = PickerLocationId.Desktop
                };

                var window = (Application.Current as App)?.MainWindow;

                if (window == null)
                {
                    return;
                }

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
                {
                    InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

                    picker.FileTypeFilter.Add("*");
                    var folder = await picker.PickSingleFolderAsync();

                    if (folder != null && !string.IsNullOrEmpty(folder.Path))
                    {
                        MigrationsExtension.CreateDatabaseDump(folder.Path);

                        var alertMessage = string.Concat(
                            m_localization.GetString(Constants.DATA_BACKUP_COMPLETED_SUCCESSFULLY_KEY),
                            ": ",
                            folder.Path);

                        this.ShowAlert(
                            m_localization.GetString(Constants.INFORMATION_KEY),
                            alertMessage,
                            InfoBarSeverity.Success);
                    }
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async void OnFactoryResetButtonClick(object sender, RoutedEventArgs args)
        {
            if (!m_sureWantResetDB)
            {
                this.ShowAlert(
                    m_localization.GetString(Constants.WARNING_KEY),
                    m_localization.GetString(Constants.ARE_YOU_SURE_YOU_WANT_RESET_ALL_SETTINGS_KEY),
                    InfoBarSeverity.Warning);

                m_sureWantResetDB = !m_sureWantResetDB;

                return;
            }

            await MigrationsExtension.ClearDB(m_serviceProvider);
        }

        private async void OnCultureChanged(object? sender, Services.Localization.CultureChangedEventArgs args)
        {
            await UpdateTexts(args.CurrentCulture);
        }
        #endregion

        #region UI
        private void SetVersion()
        {
            try
            {
                var sb = new StringBuilder();
                var version = Package.Current.Id.Version;

                sb.Append("Version ");
                sb.Append(version.Major);
                sb.Append('.');
                sb.Append(version.Minor);
                sb.Append('.');
                sb.Append(version.Build);
                sb.Append('.');
                sb.Append(version.Revision);

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    VersionText.Text = sb.ToString();
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task UpdateTexts(CultureInfo? culture = null)
        {
            await SetPropertiesValues();

            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                SettingsTitle.Text = m_localization.GetString(Constants.SETTINGS_KEY, culture);
                ApplicationLanguage.Text = m_localization.GetString(Constants.APPLICATION_LANGUAGE_KEY, culture);
                TranslationLanguage.Text = m_localization.GetString(Constants.TRANSLATION_LANGUAGE_KEY, culture);
                FavoriteTopic.Text = m_localization.GetString(Constants.FAVORITE_TOPIC_KEY, culture);
                DataBackup.Text = m_localization.GetString(Constants.CREATING_DATA_BACKUP_KEY, culture);
                DataBackupButton.Content = m_localization.GetString(Constants.SELECT_FOLDER_KEY, culture);
                FactoryReset.Text = m_localization.GetString(Constants.FACTORY_RESET_KEY, culture);
                FactoryResetButton.Content = m_localization.GetString(Constants.RESET_KEY, culture);
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void StartAnimationOnLoad()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var animation = Resources
                        .Where(r => r.Key is string element && element == "StartAnimation")
                        .FirstOrDefault().Value as Storyboard;

                    ArgumentNullException.ThrowIfNull(animation, nameof(animation));

                    animation.Begin();
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Setting properties
        private async Task SetPropertiesValues()
        {
            await SetLanguagePropertyValue();
            await SetTranslationLanguagePropertyValue();
            await SetFavoriteTopicPropertyValue();
        }

        private async Task SetLanguagePropertyValue()
        {
            try
            {
                var language = await m_usersRepository.GetUserCultureAsync();

                ArgumentException.ThrowIfNullOrEmpty(language, nameof(language));

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        m_isSettingLanguage = true;

                        LanguageSelect.Items.Clear();
                        LanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.ENGLISH_LANGUAGE_KEY),
                            Tag = Constants.ENGLISH_LANGUAGE_KEY
                        });
                        LanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.UKRAINIAN_LANGUAGE_KEY),
                            Tag = Constants.UKRAINIAN_LANGUAGE_KEY
                        });

                        var obj = LanguageSelect.Items
                            .Where(e => e is ComboBoxItem item && (string)item.Tag == language)
                            .FirstOrDefault();

                        LanguageSelect.SelectedIndex = LanguageSelect.Items.IndexOf(obj);

                        m_isSettingLanguage = false;
                    }
                    catch (Exception ex)
                    {
                        ShowException(ex);
                    }
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task SetTranslationLanguagePropertyValue()
        {
            try
            {
                var language = await m_usersRepository.GetUserNativeLanguageAsync();

                ArgumentException.ThrowIfNullOrEmpty(language, nameof(language));

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        m_translationLanguage = true;

                        TranslationLanguageSelect.Items.Clear();
                        TranslationLanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.UKRAINIAN_LANGUAGE_KEY),
                            Tag = Constants.UKRAINIAN_LANGUAGE_KEY
                        });
                        TranslationLanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.RUSSIAN_LANGUAGE_KEY),
                            Tag = Constants.RUSSIAN_LANGUAGE_KEY
                        });
                        TranslationLanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.POLISH_LANGUAGE_KEY),
                            Tag = Constants.POLISH_LANGUAGE_KEY
                        });
                        TranslationLanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.GERMAN_LANGUAGE_KEY),
                            Tag = Constants.GERMAN_LANGUAGE_KEY
                        });
                        TranslationLanguageSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.HEBREW_LANGUAGE_KEY),
                            Tag = Constants.HEBREW_LANGUAGE_KEY
                        });

                        var obj = TranslationLanguageSelect.Items
                            .Where(e => e is ComboBoxItem item && (string)item.Tag == language)
                            .FirstOrDefault();

                        TranslationLanguageSelect.SelectedIndex = TranslationLanguageSelect.Items.IndexOf(obj);

                        m_translationLanguage = false;
                    }
                    catch (Exception ex)
                    {
                        ShowException(ex);
                    }
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task SetFavoriteTopicPropertyValue()
        {
            try
            {
                var topic = await m_usersRepository.GetUserFavoriteTopicAsync();

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        m_favoriteTopic = true;

                        FavoriteTopicSelect.Items.Clear();
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.PROGRAMMING_TOPIC_KEY),
                            Tag = Constants.PROGRAMMING_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.TECHNOLOGY_TOPIC_KEY),
                            Tag = Constants.TECHNOLOGY_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.EDUCATION_TOPIC_KEY),
                            Tag = Constants.EDUCATION_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.FINANCE_TOPIC_KEY),
                            Tag = Constants.FINANCE_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.MEDICINE_TOPIC_KEY),
                            Tag = Constants.MEDICINE_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.LAW_TOPIC_KEY),
                            Tag = Constants.LAW_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.TRAVEL_TOPIC_KEY),
                            Tag = Constants.TRAVEL_TOPIC_KEY
                        });
                        FavoriteTopicSelect.Items.Add(new ComboBoxItem
                        {
                            Content = m_localization.GetString(Constants.HEALTH_TOPIC_KEY),
                            Tag = Constants.HEALTH_TOPIC_KEY
                        });

                        var obj = FavoriteTopicSelect.Items
                            .Where(e => e is ComboBoxItem item && (string)item.Tag == topic.ToString())
                            .FirstOrDefault();

                        FavoriteTopicSelect.SelectedIndex = FavoriteTopicSelect.Items.IndexOf(obj);

                        m_favoriteTopic = false;
                    }
                    catch (Exception ex)
                    {
                        ShowException(ex);
                    }
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }
        #endregion

        #region Exceptions
        private void EnsureAddedTaskToUIThread(bool isEnqueued)
        {
            if (!isEnqueued)
            {
                this.ShowAlert(m_localization.GetString("Error"), m_localization.GetString("FailedAddTaskToUIThread"), InfoBarSeverity.Error);
            }
        }

        private void ShowPropertyNotChanged()
        {
            this.ShowAlert(m_localization.GetString("Error"), m_localization.GetString("PropertyNotChanged"), InfoBarSeverity.Error);
        }

        private void ShowException(Exception ex)
        {
#if DEBUG
            this.ShowAlertExceptionWithTrace(ex, m_localization);
#else
            this.ShowAlertException(ex, m_localization);
#endif
        }
        #endregion
    }
}