using System;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace DoubleYou.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IntroductionPage : Page
    {
        private readonly ILocalizationService m_localization;
        private readonly IUsersRepository m_usersRepository;
        private string m_cultureCode = Constants.ENGLISH_LANGUAGE_KEY;
        private string m_nativeLanguage = Constants.UKRAINIAN_LANGUAGE_KEY;
        private string m_favoriteTopic = Constants.PROGRAMMING_TOPIC_KEY;
        private bool m_isSettingCultureCode;
        private bool m_isSettingNativeLanguage;
        private bool m_isSettingFavoriteTopic;

        public IntroductionPage()
        {
            this.InitializeComponent();

            m_localization = (Application.Current as App)?.ServiceProvider.GetService<ILocalizationService>()
                ?? throw new ArgumentNullException(nameof(ILocalizationService));
            m_usersRepository = (Application.Current as App)?.ServiceProvider.GetService<IUsersRepository>()
                ?? throw new ArgumentNullException(nameof(IUsersRepository));
        }

        #region OnNavigated
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                CultureCodeSelect.SelectionChanged += CultureCodeSelected;
                NativeLanguageSelect.SelectionChanged += NativeLanguageSelected;
                FavoriteTopicSelect.SelectionChanged += FavoriteTopicSelected;
                RecoverDataButton.Click += OnRecoverDataButtonClick;

                StartAnimation();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            CultureCodeSelect.SelectionChanged -= CultureCodeSelected;
            NativeLanguageSelect.SelectionChanged -= NativeLanguageSelected;
            FavoriteTopicSelect.SelectionChanged -= FavoriteTopicSelected;
            RecoverDataButton.Click -= OnRecoverDataButtonClick;
        }
        #endregion

        #region Event handlers
        private void OnRecoverDataButtonClick(object sender, RoutedEventArgs args)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var picker = new FileOpenPicker()
                    {
                        SuggestedStartLocation = PickerLocationId.Desktop
                    };

                    var window = (Application.Current as App)?.IntroductionWindow;

                    if (window == null)
                    {
                        return;
                    }

                    InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

                    picker.FileTypeFilter.Add("*");
                    var file = await picker.PickSingleFileAsync();

                    if (file != null && !string.IsNullOrEmpty(file.Path))
                    {
                        MigrationsExtension.InstalDatabaseDump(file.Path);
                    }
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void CultureCodeSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_isSettingCultureCode)
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
                        m_cultureCode = code;

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
                        {
                            try
                            {
                                CultureCodeSelect.Visibility = Visibility.Collapsed;

                                Storyboard? animation = null;

                                animation = Resources
                                    .Where(r => r.Key is string element && element == "EndCultureCodeFadeAnimation")
                                    .FirstOrDefault()
                                    .Value as Storyboard;

                                animation?.Begin();
                                await Task.Delay(1500);

                                CultureCodePanel.Visibility = Visibility.Collapsed;
                                NativeLanguagePanel.Visibility = Visibility.Visible;

                                animation = Resources
                                    .Where(r => r.Key is string element && element == "StartNativeLanguageFadeAnimation")
                                    .FirstOrDefault()
                                    .Value as Storyboard;

                                SetNativeLanguageValue();

                                animation?.Begin();
                                await Task.Delay(1500);
                            }
                            catch (Exception ex)
                            {
                                ShowException(ex);
                            }
                        });

                        EnsureAddedTaskToUIThread(isEnqueued);

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

        private void NativeLanguageSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_isSettingNativeLanguage)
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
                        m_nativeLanguage = code;

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
                        {
                            try
                            {
                                NativeLanguageSelect.Visibility = Visibility.Collapsed;

                                Storyboard? animation = null;

                                animation = Resources
                                    .Where(r => r.Key is string element && element == "EndNativeLanguageFadeAnimation")
                                    .FirstOrDefault()
                                    .Value as Storyboard;

                                animation?.Begin();
                                await Task.Delay(1500);

                                NativeLanguagePanel.Visibility = Visibility.Collapsed;
                                FavoriteTopicPanel.Visibility = Visibility.Visible;

                                animation = Resources
                                    .Where(r => r.Key is string element && element == "StartFavoriteTopicFadeAnimation")
                                    .FirstOrDefault()
                                    .Value as Storyboard;

                                SetFavoriteTopicValue();

                                animation?.Begin();
                                await Task.Delay(1500);
                            }
                            catch (Exception ex)
                            {
                                ShowException(ex);
                            }
                        });

                        EnsureAddedTaskToUIThread(isEnqueued);

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

        private void FavoriteTopicSelected(object sender, SelectionChangedEventArgs args)
        {
            if (m_isSettingFavoriteTopic)
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
                        m_favoriteTopic = code;

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
                        {
                            try
                            {
                                FavoriteTopicSelect.Visibility = Visibility.Collapsed;

                                Storyboard? animation = null;

                                animation = Resources
                                    .Where(r => r.Key is string element && element == "EndFavoriteTopicFadeAnimation")
                                    .FirstOrDefault()
                                    .Value as Storyboard;

                                animation?.Begin();
                                await Task.Delay(1500);

                                FavoriteTopicPanel.Visibility = Visibility.Collapsed;

                                await SaveUserAsync();

                                await ActivateMainWindow();
                            }
                            catch (Exception ex)
                            {
                                ShowException(ex);
                            }
                        });

                        EnsureAddedTaskToUIThread(isEnqueued);

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
        #endregion

        #region UI
        private void StartAnimation()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    RecoverDataButton.Content = m_localization.GetString(Constants.RECOVER_DATA_KEY);

                    await Task.Delay(500);

                    MainText.Text = Constants.APP_DISPLAY_NAME;

                    Storyboard? animation = Resources
                        .Where(r => r.Key is string element && element == "StartFadeAnimation")
                        .FirstOrDefault()
                        .Value as Storyboard;

                    animation?.Begin();
                    await Task.Delay(1500);

                    animation = Resources
                        .Where(r => r.Key is string element && element == "EndFadeAnimation")
                        .FirstOrDefault()
                        .Value as Storyboard;

                    animation?.Begin();
                    await Task.Delay(2000);

                    MainText.Text = string.Empty;
                    MainText.Opacity = 1;

                    foreach (var c in Constants.SLOGAN)
                    {
                        MainText.Text += c;

                        if (c != 'W')
                        {
                            await Task.Delay(100);
                        }
                        else
                        {
                            await Task.Delay(1000);
                        }
                    }

                    await Task.Delay(900);

                    animation?.Begin();
                    await Task.Delay(1500);

                    MainText.Visibility = Visibility.Collapsed;
                    CultureCodePanel.Visibility = Visibility.Visible;

                    SetCultureCodeValue();

                    animation = Resources
                        .Where(r => r.Key is string element && element == "StartCultureCodeFadeAnimation")
                        .FirstOrDefault()
                        .Value as Storyboard;

                    animation?.Begin();
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Other
        private async Task SaveUserAsync()
        {
            try
            {
                await m_usersRepository.SaveUserAsync(new(m_nativeLanguage, m_favoriteTopic));

                await m_localization.ChangeCulture(m_cultureCode);
            }
            catch (Exception ex)
            {
                await m_usersRepository.RemoveUserAsync();
                ShowException(ex);
                Windows.ApplicationModel.Core.CoreApplication.Exit();
                throw;
            }
        }

        private async Task ActivateMainWindow()
        {
            try
            {
                (Application.Current as App)?.ActivateMainWindow();
            }
            catch (Exception ex)
            {
                await m_usersRepository.RemoveUserAsync();
                ShowException(ex);
                Windows.ApplicationModel.Core.CoreApplication.Exit();
                throw;
            }
        }
        #endregion

        #region Setting properties
        private void SetCultureCodeValue()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    m_isSettingCultureCode = true;

                    CultureCodeTitle.Text = m_localization.GetString("InterfaceLanguage");

                    CultureCodeSelect.Items.Clear();
                    CultureCodeSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.SELECT_ITEM),
                        Tag = string.Empty,
                        IsEnabled = false,
                    });
                    CultureCodeSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.ENGLISH_LANGUAGE_KEY),
                        Tag = Constants.ENGLISH_LANGUAGE_KEY
                    });
                    CultureCodeSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.UKRAINIAN_LANGUAGE_KEY),
                        Tag = Constants.UKRAINIAN_LANGUAGE_KEY
                    });

                    CultureCodeSelect.SelectedIndex = 0;

                    m_isSettingCultureCode = false;
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void SetNativeLanguageValue()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    m_isSettingNativeLanguage = true;

                    NativeLanguageTitle.Text = m_localization.GetString("NativeLanguage");

                    NativeLanguageSelect.Items.Clear();
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.SELECT_ITEM),
                        Tag = string.Empty,
                        IsEnabled = false,
                    });
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.UKRAINIAN_LANGUAGE_KEY),
                        Tag = Constants.UKRAINIAN_LANGUAGE_KEY
                    });
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.RUSSIAN_LANGUAGE_KEY),
                        Tag = Constants.RUSSIAN_LANGUAGE_KEY
                    });
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.POLISH_LANGUAGE_KEY),
                        Tag = Constants.POLISH_LANGUAGE_KEY
                    });
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.GERMAN_LANGUAGE_KEY),
                        Tag = Constants.GERMAN_LANGUAGE_KEY
                    });
                    NativeLanguageSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.HEBREW_LANGUAGE_KEY),
                        Tag = Constants.HEBREW_LANGUAGE_KEY
                    });

                    NativeLanguageSelect.SelectedIndex = 0;

                    m_isSettingNativeLanguage = false;
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void SetFavoriteTopicValue()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    m_isSettingFavoriteTopic = true;

                    FavoriteTopicTitle.Text = m_localization.GetString("FavoriteTopic");

                    FavoriteTopicSelect.Items.Clear();
                    FavoriteTopicSelect.Items.Add(new ComboBoxItem
                    {
                        Content = m_localization.GetString(Constants.SELECT_ITEM),
                        Tag = string.Empty,
                        IsEnabled = false,
                    });
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

                    FavoriteTopicSelect.SelectedIndex = 0;

                    m_isSettingFavoriteTopic = false;
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Exceptions
        private void ShowPropertyNotChanged()
        {
            this.ShowAlert(m_localization.GetString("Error"), m_localization.GetString("PropertyNotChanged"), InfoBarSeverity.Error);
        }

        private void EnsureAddedTaskToUIThread(bool isEnqueued)
        {
            if (!isEnqueued)
            {
                this.ShowAlert(m_localization.GetString("Error"), m_localization.GetString("FailedAddTaskToUIThread"), InfoBarSeverity.Error);
            }
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