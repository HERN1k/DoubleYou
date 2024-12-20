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
    public sealed class RepetitionWordsCollectionItem(string tag, HorizontalAlignment horizontalAlignment, string? word, string? translatedWord)
    {
        public string Tag { get; set; } = tag ?? string.Empty;
        public HorizontalAlignment HorizontalAlignment { get; set; } = horizontalAlignment;
        public string Word { get; set; } = word ?? string.Empty;
        public string TranslatedWord { get; set; } = translatedWord ?? string.Empty;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RepetitionOfWordsPage : Page
    {
        public ObservableCollection<RepetitionWordsCollectionItem> Words { get; set; } = new();
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
        private readonly Duration m_storyboardDuration = new(TimeSpan.FromSeconds(0.25));
        private string m_translateLanguageCode = Constants.UKRAINIAN_LANGUAGE_KEY;
        private bool m_isTranslateHidden = true;
        private bool m_isLoaderActive;

        public RepetitionOfWordsPage()
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
                ButtonVisibilityChanger.Click += OnButtonClicked;
                WordsCollection.ContainerContentChanging += OnWordsCollectionContentChanging;

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
            ButtonVisibilityChanger.Click -= OnButtonClicked;
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

                RemoveEventHandlersToListView();
                Words.Clear();

                if (m_windowsHelper.IsInternetAvailable())
                {
                    var words = await m_vocabulary.WordsLearnedForRepetitionAsync();

                    if (words != null && words.Count > 0)
                    {
                        var translatedWords = await m_translator.Translate(
                            language: m_translateLanguageCode.ParseLanguageCode(),
                            words: words);

                        bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                        {
                            int index = 0;
                            foreach (var word in translatedWords)
                            {
                                Words.Add(new(
                                    tag: UIUtilities.CreateElementTag(index++),
                                    horizontalAlignment: m_translateLanguageCode.ResolveAlignmentByLanguage(),
                                    word: word.Key.Data.ToTitleCase().TrimToLength(24),
                                    translatedWord: word.Value.ToTitleCase().TrimToLength(24)));
                            }

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

        private void HideText(Grid grid)
        {
            if (grid == null)
            {
                return;
            }

            var text = grid.Children
                .Where(e => e is TextBlock)
                .FirstOrDefault() as TextBlock;

            var border = grid.Children
                .Where(e => e is Border)
                .FirstOrDefault() as Border;

            if (text != null && border != null)
            {
                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    var storyboard = new Storyboard();

                    var fadeOutTextAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = m_storyboardDuration,
                    };
                    Storyboard.SetTarget(fadeOutTextAnimation, text);
                    Storyboard.SetTargetProperty(fadeOutTextAnimation, "Opacity");

                    var fadeInBorderAnimation = new DoubleAnimation
                    {
                        To = 0.75,
                        Duration = m_storyboardDuration,
                    };
                    Storyboard.SetTarget(fadeInBorderAnimation, border);
                    Storyboard.SetTargetProperty(fadeInBorderAnimation, "Opacity");

                    storyboard.Children.Add(fadeOutTextAnimation);
                    storyboard.Children.Add(fadeInBorderAnimation);

                    storyboard.Begin();
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
        }

        private void ShowText(Grid grid)
        {
            if (grid == null)
            {
                return;
            }

            var text = grid.Children
                .Where(e => e is TextBlock)
                .FirstOrDefault() as TextBlock;

            var border = grid.Children
                .Where(e => e is Border)
                .FirstOrDefault() as Border;

            if (text != null && border != null)
            {
                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    var storyboard = new Storyboard();

                    var fadeInTextAnimation = new DoubleAnimation
                    {
                        To = 1,
                        Duration = m_storyboardDuration,
                    };
                    Storyboard.SetTarget(fadeInTextAnimation, text);
                    Storyboard.SetTargetProperty(fadeInTextAnimation, "Opacity");

                    var fadeOutBorderAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = m_storyboardDuration,
                    };
                    Storyboard.SetTarget(fadeOutBorderAnimation, border);
                    Storyboard.SetTargetProperty(fadeOutBorderAnimation, "Opacity");

                    storyboard.Children.Add(fadeInTextAnimation);
                    storyboard.Children.Add(fadeOutBorderAnimation);

                    storyboard.Begin();
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
        }

        private void UpdateTexts(CultureInfo? culture = null)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                MainTitle.Text = m_localization.GetString(Constants.REPETITION_OF_WORDS_TITLE_KEY, culture);

                if (m_isTranslateHidden)
                {
                    ButtonVisibilityChangerText.Text = m_localization.GetString(Constants.OPEN_ALL_KEY, culture);
                }
                else
                {
                    ButtonVisibilityChangerText.Text = m_localization.GetString(Constants.HIDE_ALL_KEY, culture);
                }
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

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
        {
            if (m_isTranslateHidden)
            {
                if (sender is Grid grid)
                {
                    grid.CapturePointer(args.Pointer);

                    ShowText(grid);
                }
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            if (m_isTranslateHidden)
            {
                if (sender is Grid grid)
                {
                    grid.ReleasePointerCapture(args.Pointer);

                    HideText(grid);
                }
            }
        }

        private void OnButtonClicked(object sender, RoutedEventArgs args)
        {
            try
            {
                bool isEnqueued = false;
                int index = 0;
                if (m_isTranslateHidden)
                {
                    m_isTranslateHidden = false;

                    foreach (var word in Words)
                    {
                        var container = WordsCollection.ContainerFromItem(word) as ListViewItem;

                        if (container != null)
                        {
                            var grid = container.FindElementByTag<Grid>(UIUtilities.CreateElementTag(index++));
                            if (grid != null)
                            {
                                ShowText(grid);

                                isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                                {
                                    ButtonVisibilityChangerIcon.Glyph = "\uED1A";
                                    ButtonVisibilityChangerText.Text = m_localization.GetString(Constants.HIDE_ALL_KEY);
                                });

                                EnsureAddedTaskToUIThread(isEnqueued);
                            }
                        }
                    }
                }
                else
                {
                    m_isTranslateHidden = true;

                    foreach (var word in Words)
                    {
                        var container = WordsCollection.ContainerFromItem(word) as ListViewItem;

                        if (container != null)
                        {
                            var grid = container.FindElementByTag<Grid>(UIUtilities.CreateElementTag(index++));
                            if (grid != null)
                            {
                                HideText(grid);

                                isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                                {
                                    ButtonVisibilityChangerIcon.Glyph = "\uE890";
                                    ButtonVisibilityChangerText.Text = m_localization.GetString(Constants.OPEN_ALL_KEY);
                                });

                                EnsureAddedTaskToUIThread(isEnqueued);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
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
                ButtonVisibilityChanger.Visibility = isInternetAvailable
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
                OfflineIcon.Visibility = Visibility.Collapsed;
                ButtonVisibilityChanger.Visibility = Visibility.Collapsed;
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
                ButtonVisibilityChanger.Visibility = Visibility.Visible;
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

                    var gridTranslateHiddenContainer = UIUtilities.FindChild<Grid>(container);
                    if (gridTranslateHiddenContainer != null)
                    {
                        var grid = gridTranslateHiddenContainer.Children
                            .FirstOrDefault(e => e is Grid) as Grid;

                        if (grid != null)
                        {
                            grid.PointerPressed += OnPointerPressed;
                            grid.PointerReleased += OnPointerReleased;
                        }
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

                    var grid = UIUtilities.FindChild<Grid>(container);
                    if (grid != null)
                    {
                        grid.PointerPressed -= OnPointerPressed;
                        grid.PointerReleased -= OnPointerReleased;
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