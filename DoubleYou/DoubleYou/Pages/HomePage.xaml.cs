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
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace DoubleYou.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private readonly ILocalizationService m_localization;
        private readonly IUsersRepository m_usersRepository;
        private readonly IVocabularyService m_vocabulary;
        private Domain.Entities.Entities.User? m_user;

        public HomePage()
        {
            this.InitializeComponent();

            m_localization = (Application.Current as App)?.ServiceProvider.GetService<ILocalizationService>()
                ?? throw new ArgumentNullException(nameof(ILocalizationService));
            m_usersRepository = (Application.Current as App)?.ServiceProvider.GetService<IUsersRepository>()
                ?? throw new ArgumentNullException(nameof(IUsersRepository));
            m_vocabulary = (Application.Current as App)?.ServiceProvider.GetService<IVocabularyService>()
                ?? throw new ArgumentNullException(nameof(IVocabularyService));
        }

        #region OnNavigated
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                m_user = await GetUserAsync();
                await SetWindowCulture();
                await SetTextOnPage();
                await StartAnimationOnLoad();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }
        #endregion

        #region UI
        private async Task SetWindowCulture()
        {
            try
            {
                if (!await m_localization.SetCultureOnStartup())
                {
                    throw new InvalidOperationException(Constants.FAILED_SET_APPLICATION_CULTURE);
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task SetTextOnPage()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(m_user, nameof(m_user));

                var wordsLearned = await m_vocabulary.CountWordsLearnedAsync();
                var wordsLearnedForLastTime = await m_vocabulary.CountWordsLearnedForLastTimeAsync();

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    WordsLearnedTitle.Text = m_localization.GetString("WordsLearned");
                    ForLastTimeTitle.Text = m_localization.GetString("ForLastTime");
                    FavoriteTopicTitle.Text = m_localization.GetString("FavoriteTopic");
                    TranslationTitle.Text = m_localization.GetString("Translation");
                    DaysInLearnTitle.Text = m_localization.GetString("DaysInLearn");

                    WordsLearnedNumber.Text = wordsLearned.ToString();
                    ForLastTimeNumber.Text = wordsLearnedForLastTime.ToString();
                    FavoriteTopic.Text = m_localization.GetString(m_user.FavoriteTopic.ToString());
                    TranslationNumber.Text = m_localization.GetString(m_user.TranslationLanguage);
                    DaysInLearnNumber.Text = (DateTime.UtcNow - m_user.CreatedUtc).Days.ToString();
                });

                EnsureAddedTaskToUIThread(isEnqueued);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private async Task StartAnimationOnLoad()
        {
            try
            {
                Storyboard? firstElement = null;

                var storyboards = Resources
                    .Where(r => r.Key is string element && element.EndsWith("Animation"))
                    .Select(r =>
                    {
                        if (r.Key is string element && element == "FavoriteTopicAnimation")
                        {
                            firstElement = (Storyboard)r.Value;
                        }

                        return (Storyboard)r.Value;
                    })
                    .ToList();

                ArgumentNullException.ThrowIfNull(firstElement, nameof(firstElement));

                storyboards.Remove(firstElement);

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    firstElement.Begin();
                    firstElement.Resume();
                });

                foreach (var storyboard in storyboards)
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        storyboard.Begin();
                        storyboard.Pause();
                    });
                    await Task.Delay(500);
                    storyboard.Resume();
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }
        #endregion

        #region Other
        private async Task<Domain.Entities.Entities.User> GetUserAsync()
        {
            try
            {
                var user = await m_usersRepository.GetUserAsync()
                    ?? throw new InvalidOperationException(Constants.USER_NOT_FOUND);

                return user;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                Windows.ApplicationModel.Core.CoreApplication.Exit();
                throw;
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
