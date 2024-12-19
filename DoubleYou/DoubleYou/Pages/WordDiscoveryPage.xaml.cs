using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;

using DoubleYou.Services;

using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Networking.Connectivity;

namespace DoubleYou.Pages
{
    public sealed class DiscoveryWordCollectionItem(Guid id, HorizontalAlignment horizontalAlignment, string? word, string? translatedWord)
    {
        public string Index { get; set; } = string.Empty;
        public Guid Id { get; set; } = id;
        public HorizontalAlignment HorizontalAlignment { get; set; } = horizontalAlignment;
        public string Word { get; set; } = word ?? string.Empty;
        public string TranslatedWord { get; set; } = translatedWord ?? string.Empty;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WordDiscoveryPage : Page
    {
        public ObservableCollection<DiscoveryWordCollectionItem> Words { get; set; } = new();
        public bool IsLoaderActive
        {
            get => m_isLoaderActive;
            private set
            {
                m_isLoaderActive = value;
                Loader.SetLoaderActive(value);
            }
        }

        private readonly IWindowsHelper m_windowsHelper;
        private readonly IVocabularyService m_vocabulary;
        private readonly ITranslator m_translator;
        private readonly ILocalizationService m_localization;
        private readonly IUsersRepository m_usersRepository;
        private string m_translateLanguageCode = Constants.UKRAINIAN_LANGUAGE_KEY;
        private bool m_isLoaderActive;

        public WordDiscoveryPage()
        {
            this.InitializeComponent();

            m_windowsHelper = (Application.Current as App)?.ServiceProvider.GetService<IWindowsHelper>()
                ?? throw new ArgumentNullException(nameof(IWindowsHelper));
            m_vocabulary = (Application.Current as App)?.ServiceProvider.GetService<IVocabularyService>()
                ?? throw new ArgumentNullException(nameof(IVocabularyService));
            m_translator = (Application.Current as App)?.ServiceProvider.GetService<ITranslator>()
                ?? throw new ArgumentNullException(nameof(ITranslator));
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
                m_localization.CultureChanged += OnCultureChanged;
                m_vocabulary.WordsInitializing += OnWordsInitializing;
                NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
                WordsCollection.ContainerContentChanging += OnWordsCollectionContentChanging;
                UpdateWordsButton.Click += UpdateWordsInCollection;
                AddWordsButton.Click += AddWordsInCollection;
                SaveWordsButton.Click += SaveWordsCollection;

                UpdateTexts();

                await m_vocabulary.InitializeWordsAsync();
                m_translateLanguageCode = await m_usersRepository.GetUserNativeLanguageAsync();

                if (m_translateLanguageCode == Constants.ENGLISH_LANGUAGE_KEY)
                {
                    m_translateLanguageCode = Constants.UKRAINIAN_LANGUAGE_KEY;
                }

                var animation = Resources
                    .Where(r => r.Key is string element && element == "StartAnimation")
                    .FirstOrDefault().Value as Storyboard;

                ArgumentNullException.ThrowIfNull(animation, nameof(animation));

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    animation.Begin();
                });

                EnsureAddedTaskToUIThread(isEnqueued);

                await UpdateWordsCollection();

                await ShowDialogInstallVoice();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            m_localization.CultureChanged -= OnCultureChanged;
            m_vocabulary.WordsInitializing -= OnWordsInitializing;
            NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
            WordsCollection.ContainerContentChanging -= OnWordsCollectionContentChanging;
            UpdateWordsButton.Click -= UpdateWordsInCollection;
            AddWordsButton.Click -= AddWordsInCollection;
            SaveWordsButton.Click -= SaveWordsCollection;

            RemoveEventHandlersToListView();
        }
        #endregion

        #region UI
        private async Task UpdateWordsCollection()
        {
            try
            {
                IsLoaderActive = true;

                RemoveEventHandlersToListView();
                Words.Clear();

                if (m_windowsHelper.IsInternetAvailable())
                {
                    if (m_vocabulary.Words.Count > 0)
                    {
                        var words = await m_translator.Translate(
                            language: m_translateLanguageCode.ParseLanguageCode(),
                            words: m_vocabulary.Words);

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                        {
                            List<DiscoveryWordCollectionItem> wordsCollection = new();

                            foreach (var word in words)
                            {
                                wordsCollection.Add(new(
                                    id: word.Key.Id,
                                    horizontalAlignment: m_translateLanguageCode.ResolveAlignmentByLanguage(),
                                    word: word.Key.Data.ToTitleCase().TrimToLength(24),
                                    translatedWord: word.Value.ToTitleCase().TrimToLength(24)));
                            }

                            wordsCollection.Sort((e1, e2) =>
                                string.Compare(e1.Word, e2.Word, StringComparison.OrdinalIgnoreCase));

                            int index = 0;
                            foreach (var word in wordsCollection)
                            {
                                word.Index = $"¹{++index}";

                                Words.Add(word);
                            }

                            wordsCollection.Clear();

                            DisplayDesiredIcon();
                        });

                        EnsureAddedTaskToUIThread(isEnqueued);
                    }
                    else
                    {
                        if (Words.Count == 0)
                        {
                            SetEmptyIconVisible();
                        }
                    }
                }
                else
                {
                    SetOfflineIconVisible(false);
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                IsLoaderActive = false;
            }
        }

        private async Task ShowDialogInstallVoice()
        {
            if (m_windowsHelper.IsCorrectVoiceInstalled())
            {
                return;
            }

            var isDialogShow = await m_usersRepository.GetUserIsDialogShowInstallVoiceAsync();

            if (isDialogShow)
            {
                return;
            }

            var button = new Button()
            {
                Content = m_localization.GetString(Constants.ADD_KEY)
            };

            button.Click += OpenSpeechSettings;

            this.ShowAlertWithButton(
                title: m_localization.GetString(Constants.INSTALLING_VOICES_KEY),
                message: m_localization.GetString(Constants.DEVICE_NOT_HAVE_VOICES_KEY),
                button: button,
                severity: InfoBarSeverity.Warning);

            await m_usersRepository.SetUserIsDialogShowInstallVoiceAsync(!isDialogShow);
        }

        private void UpdateTexts(CultureInfo? culture = null)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                MainTitle.Text = m_localization.GetString(Constants.NEW_WORDS_KEY, culture);
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Event handlers 
        private void OnNetworkStatusChanged(object? sender)
        {
            DisplayDesiredIcon();
        }

        private async void OnSpeekWord(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                var word = button.Tag as string;

                if (!string.IsNullOrEmpty(word))
                {
                    await m_windowsHelper.SpeakAsync(word);
                }
            }
        }

        private async void UpdateWordsInCollection(object sender, RoutedEventArgs args)
        {
            if (!m_windowsHelper.IsInternetAvailable())
            {
                return;
            }

            try
            {
                IsLoaderActive = true;

                await m_vocabulary.UpdateWordsAsync();

                await UpdateWordsCollection();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                IsLoaderActive = false;
            }
        }

        private async void AddWordsInCollection(object sender, RoutedEventArgs args)
        {
            if (!m_windowsHelper.IsInternetAvailable())
            {
                return;
            }

            try
            {
                IsLoaderActive = true;

                await m_vocabulary.AddRundomWordsAsync();

                await UpdateWordsCollection();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                IsLoaderActive = false;
            }
        }

        private async void SaveWordsCollection(object sender, RoutedEventArgs args)
        {
            if (m_vocabulary.Words.Count == 0)
            {
                return;
            }

            if (!m_windowsHelper.IsInternetAvailable())
            {
                return;
            }

            try
            {
                IsLoaderActive = true;

                var ids = m_vocabulary.Words
                    .Select(x => x.Id)
                    .ToList();

                await m_vocabulary.SetWordsLearnedAsync(ids);

                RemoveEventHandlersToListView();

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    Words.Clear();
                });

                EnsureAddedTaskToUIThread(isEnqueued);


                SetSuccessIconVisible();

                this.ShowAlert(
                    title: m_localization.GetString(Constants.WELL_DONE_KEY),
                    message: m_localization.GetString(Constants.WORDS_SAVED_AS_LEARNED_KEY),
                    severity: InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                IsLoaderActive = false;
            }
        }

        private async void RemoveWordInCollection(object sender, PointerRoutedEventArgs args)
        {
            if (sender is not SymbolIcon icon)
            {
                return;
            }

            if (icon.Tag is not Guid id)
            {
                return;
            }

            try
            {
                await m_vocabulary.SetWordRefuseLearnAsync(id);

                await UpdateWordsCollection();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void OnSymbolIconPointerEntered(object sender, PointerRoutedEventArgs args)
        {
            if (sender is SymbolIcon icon)
            {
                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    icon.Opacity = 0.5;
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
        }

        private void OnSymbolIconPointerExited(object sender, PointerRoutedEventArgs args)
        {
            if (sender is SymbolIcon icon)
            {
                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    icon.Opacity = 0.25;
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
        }

        private void OnWordsCollectionContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                CollectionScrollViewer.ChangeView(
                    horizontalOffset: null,
                    verticalOffset: CollectionScrollViewer.ScrollableHeight,
                    zoomFactor: null);
            });

            EnsureAddedTaskToUIThread(isEnqueued);

            AddEventHandlersToListView();
        }

        private async void OpenSpeechSettings(object sender, RoutedEventArgs args)
        {
            try
            {
                var uri = new Uri(Constants.SETTINGS_URI_SPEECH);

                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                if (sender is Button button)
                {
                    button.Click -= OpenSpeechSettings;
                }
            }
        }

        private void OnWordsInitializing(object? sender, VocabularyService.WordsInitializingEventArgs args)
        {
            IsLoaderActive = args.IsInitializing;
        }

        private void OnCultureChanged(object? sender, Localization.CultureChangedEventArgs args)
        {
            UpdateTexts(args.CurrentCulture);
        }
        #endregion

        #region Info icons
        private void SetOfflineIconVisible(bool isInternetAvailable)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                OfflineIcon.Visibility = isInternetAvailable
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                SuccessIcon.Visibility = Visibility.Collapsed;
                EmptyIcon.Visibility = Visibility.Collapsed;
                ButtonsStack.Visibility = isInternetAvailable
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void SetEmptyIconVisible()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                EmptyIcon.Visibility = Visibility.Visible;
                SuccessIcon.Visibility = Visibility.Collapsed;
                OfflineIcon.Visibility = Visibility.Collapsed;
                ButtonsStack.Visibility = Visibility.Collapsed;
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void SetSuccessIconVisible()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                SuccessIcon.Visibility = Visibility.Visible;
                ButtonsStack.Visibility = Visibility.Visible;
                EmptyIcon.Visibility = Visibility.Collapsed;
                OfflineIcon.Visibility = Visibility.Collapsed;
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void DisplayDesiredIcon()
        {
            if (!m_windowsHelper.IsInternetAvailable())
            {
                SetOfflineIconVisible(false);

                return;
            }
            else if (Words.Count == 0)
            {
                SetEmptyIconVisible();

                return;
            }

            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                ButtonsStack.Visibility = Visibility.Visible;
                SuccessIcon.Visibility = Visibility.Collapsed;
                EmptyIcon.Visibility = Visibility.Collapsed;
                OfflineIcon.Visibility = Visibility.Collapsed;
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Other
        private void AddEventHandlersToListView()
        {
            RemoveEventHandlersToListView();

            foreach (var word in Words)
            {
                var container = WordsCollection.ContainerFromItem(word) as ListViewItem;

                if (container != null)
                {
                    var button = UIUtilities.FindChild<Button>(container);
                    if (button != null)
                    {
                        button.Click += OnSpeekWord;
                    }

                    var symbolIcon = UIUtilities.FindChild<SymbolIcon>(container);
                    if (symbolIcon != null)
                    {
                        symbolIcon.PointerReleased += RemoveWordInCollection;
                        symbolIcon.PointerEntered += OnSymbolIconPointerEntered;
                        symbolIcon.PointerExited += OnSymbolIconPointerExited;
                    }
                }
            }
        }

        private void RemoveEventHandlersToListView()
        {
            foreach (var word in Words)
            {
                var container = WordsCollection.ContainerFromItem(word) as ListViewItem;

                if (container != null)
                {
                    var button = UIUtilities.FindChild<Button>(container);
                    if (button != null)
                    {
                        button.Click -= OnSpeekWord;
                    }

                    var symbolIcon = UIUtilities.FindChild<SymbolIcon>(container);
                    if (symbolIcon != null)
                    {
                        symbolIcon.PointerReleased -= RemoveWordInCollection;
                        symbolIcon.PointerEntered -= OnSymbolIconPointerEntered;
                        symbolIcon.PointerExited -= OnSymbolIconPointerExited;
                    }
                }
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