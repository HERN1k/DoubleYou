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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;

using DoubleYou.Services;

using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Networking.Connectivity;

using static DoubleYou.Domain.Entities.Entities;

namespace DoubleYou.Pages
{
    public sealed class LearnedWordsCollectionItem(string index, HorizontalAlignment horizontalAlignment, string? word, string? translatedWord, string date)
    {
        public string Index { get; set; } = index ?? string.Empty;
        public HorizontalAlignment HorizontalAlignment { get; set; } = horizontalAlignment;
        public string Word { get; set; } = word ?? string.Empty;
        public string TranslatedWord { get; set; } = translatedWord ?? string.Empty;
        public string Date { get; set; } = date ?? string.Empty;
    }

    /// <summary> 
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LearnedWordsPage : Page
    {
        public ObservableCollection<LearnedWordsCollectionItem> Words { get; set; } = new();
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
        private readonly string m_dateTimeFormat = "d MMMM yyyy";
        private string m_translateLanguageCode = Constants.UKRAINIAN_LANGUAGE_KEY;
        private int m_wordsPage = 0;
        private bool m_isLoaderActive;

        public LearnedWordsPage()
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
                CollectionScrollViewer.ViewChanged += OnScrollViewChanged;

                UpdateTexts();

                await m_vocabulary.InitializeWordsAsync();
                m_translateLanguageCode = await m_usersRepository.GetUserNativeLanguageAsync();

                if (m_translateLanguageCode == Constants.ENGLISH_LANGUAGE_KEY)
                {
                    m_translateLanguageCode = Constants.UKRAINIAN_LANGUAGE_KEY;
                }

                await UpdateWordsCollection();

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
            CollectionScrollViewer.ViewChanged -= OnScrollViewChanged;
            WordsCollection.ContainerContentChanging -= OnWordsCollectionContentChanging;

            RemoveEventHandlersToListView();
        }
        #endregion

        #region UI
        private async Task UpdateWordsCollection()
        {
            try
            {
                IsLoaderActive = true;

                if (m_windowsHelper.IsInternetAvailable())
                {
                    var words = await m_vocabulary.WordsLearnedAsync(++m_wordsPage);

                    if (words != null && words.Count > 0)
                    {
                        RemoveEventHandlersToListView();

                        var translatedWords = await m_translator.Translate(
                            language: m_translateLanguageCode.ParseLanguageCode(),
                            words: words);

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                        {
                            int index = Words.Count;
                            foreach (var word in translatedWords)
                            {
                                DateTime localTime = word.Key.LearnedDate.HasValue
                                    ? word.Key.LearnedDate.Value.ToLocalTime()
                                    : DateTime.Now;

                                Words.Add(new(
                                    index: $"¹{++index}",
                                    horizontalAlignment: m_translateLanguageCode.ResolveAlignmentByLanguage(),
                                    word: word.Key.Data.ToTitleCase().TrimToLength(24),
                                    translatedWord: word.Value.ToTitleCase().TrimToLength(24),
                                    date: localTime.ToString(m_dateTimeFormat, m_localization.CurrentCulture)));
                            }

                            DisplayDesiredIcon();
                        });

                        AddEventHandlersToListView();

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

        private void UpdateTexts(CultureInfo? culture = null)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                MainTitle.Text = m_localization.GetString(Constants.WORDS_LEARNED_TITLE_KEY, culture);
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }
        #endregion

        #region Event handlers
        private void OnNetworkStatusChanged(object? sender)
        {
            DisplayDesiredIcon();
        }

        private void OnWordsCollectionContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            AddEventHandlersToListView();
        }

        private async void OnSpeekWord(object sender, RoutedEventArgs args)
        {
            if (sender is Button button)
            {
                var word = button.Tag as string;

                if (!string.IsNullOrEmpty(word))
                {
                    try
                    {
                        await m_windowsHelper.SpeakAsync(word);
                    }
                    catch (Exception ex)
                    {
                        ShowException(ex);
                    }
                }
            }
        }

        private async void OnScrollViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight)
            {
                if (IsLoaderActive)
                {
                    return;
                }

                await UpdateWordsCollection();
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
                EmptyIcon.Visibility = Visibility.Collapsed;
            });

            EnsureAddedTaskToUIThread(isEnqueued);
        }

        private void SetEmptyIconVisible()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                EmptyIcon.Visibility = Visibility.Visible;
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