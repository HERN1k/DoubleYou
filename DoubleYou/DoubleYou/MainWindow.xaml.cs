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
using System.Diagnostics;
using System.Linq;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Pages;
using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.ApplicationModel;
using Windows.UI.ViewManagement;

using WinRT;
using WinRT.Interop;

namespace DoubleYou
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, IDisposable
    {
        private readonly AppWindow m_AppWindow;
        private readonly UISettings m_UISettings;
        private readonly IWindowsHelper m_windowsHelper;
        private WindowsSystemDispatcherQueueHelper? m_wsdqHelper;
        private DesktopAcrylicController? m_acrylicController;
        private SystemBackdropConfiguration? m_configurationSource;
        private bool m_disposedValue;

        public MainWindow()
        {
            this.InitializeComponent();

            m_AppWindow = GetAppWindowForCurrentWindow();
            m_UISettings = new UISettings();
            m_windowsHelper = (Application.Current as App)?.ServiceProvider.GetService<IWindowsHelper>()
                ?? throw new ArgumentNullException(nameof(IWindowsHelper));
            SubscribeToEvents();
            ConfigureWindowTitleBar();
            TrySetAcrylicBackdrop(useAcrylicThin: false);
            OnThemeChanged((FrameworkElement)this.Content, EventArgs.Empty);
            NavigateToDefaultPage();
        }

        private void SubscribeToEvents()
        {
            ((FrameworkElement)this.Content).ActualThemeChanged += OnThemeChanged;
            if (DesktopAcrylicController.IsSupported())
            {
                this.Activated += OnWindowActivated;
                this.Closed += OnWindowClosed;
            }
            NavigationView.SelectionChanged += NavigationSelectionChanged;
        }

        private void ConfigureWindowTitleBar()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(m_AppWindow, nameof(m_AppWindow));

                    if (m_windowsHelper.IsWindowsVersionAtLeast(10, 0, 19041))
                    {
#pragma warning disable CA1416
                        this.Title = AppInfo.Current.DisplayInfo.DisplayName;
                        TitleBarTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
#pragma warning restore
                    }
                    else
                    {
                        this.Title = Constants.APP_DISPLAY_NAME;
                        TitleBarTextBlock.Text = Constants.APP_DISPLAY_NAME;
                    }

                    if (!AppWindowTitleBar.IsCustomizationSupported())
                    {
                        return;
                    }

                    if (ExtendsContentIntoTitleBar == true)
                    {
                        m_AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                    }

                    m_AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                    UIUtilities.ChangeThemeTitleBar(AppTitleBar, m_AppWindow.TitleBar);
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued, nameof(ConfigureWindowTitleBar));
        }

        private void NavigateToDefaultPage()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    RootFrame.Navigate(typeof(HomePage));
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued, nameof(NavigateToDefaultPage));
        }

        private void NavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (args.IsSettingsSelected)
                    {
                        RootFrame.Navigate(typeof(SettingsPage));

                        return;
                    }

                    var selectedItem = args.SelectedItem as NavigationViewItem;

                    if (selectedItem != null)
                    {
                        var tag = selectedItem.Tag as string;

                        switch (tag)
                        {
                            case "Home":
                                RootFrame.Navigate(typeof(HomePage));
                                break;

                            case "WordDiscovery":
                                RootFrame.Navigate(typeof(WordDiscoveryPage));
                                break;

                            case "LearnedWords":
                                RootFrame.Navigate(typeof(LearnedWordsPage));
                                break;

                            case "RepetitionOfWords":
                                RootFrame.Navigate(typeof(RepetitionOfWordsPage));
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued, nameof(NavigationSelectionChanged));
        }

        private void OnThemeChanged(FrameworkElement sender, object args)
        {
            try
            {
                if (m_configurationSource != null)
                {
                    SetConfigurationSourceTheme();
                }

                if (!AppWindowTitleBar.IsCustomizationSupported())
                {
                    return;
                }

                bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (Application.Current.Resources
                                .Where(r => (string)r.Key == "NavigationViewSelectionIndicatorForeground")
                                .FirstOrDefault()
                                .Value is SolidColorBrush selectionIndicator)
                        {
                            selectionIndicator.Color = UIUtilities.GetValueFormThemeDictionary(
                                theme: sender.ActualTheme,
                                key: "SelectionIndicatorBrush",
                                defaultValue: new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0))
                            ).Color;
                        }

                        this.AppWindow.SetIcon(Constants.LOGO_PATH);

                        UIUtilities.ChangeThemeTitleBar(AppTitleBar, m_AppWindow.TitleBar);
                    }
                    catch (Exception ex)
                    {
                        ShowException(ex);
                    }
                });

                EnsureAddedTaskToUIThread(isEnqueued, nameof(OnThemeChanged));
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            try
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                return AppWindow.GetFromWindowId(wndId);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private static void EnsureAddedTaskToUIThread(bool isEnqueued, string method)
        {
            if (!isEnqueued)
            {
#if DEBUG
                Debug.WriteLine(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD);
#endif
                Alerts.ShowCriticalErrorWindow(string.Concat(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD, "\n\nMethod: ", method));
            }
        }

        private static void ShowException(Exception ex)
        {
#if DEBUG
            Debug.WriteLine(ex);
#endif
            Alerts.ShowCriticalErrorWindow(ex);
        }

        #region Acrylic Backdrop
        private void TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (!DesktopAcrylicController.IsSupported())
            {
                return;
            }

            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            m_configurationSource = new()
            {
                IsInputActive = true
            };

            SetConfigurationSourceTheme();

            m_acrylicController = new()
            {
                Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
            };

            m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (m_configurationSource != null)
            {
                m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            this.Activated -= OnWindowActivated;
            m_configurationSource = null;
        }

        private void SetConfigurationSourceTheme()
        {
            if (m_configurationSource == null)
            {
                return;
            }

            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark:
                    m_configurationSource.Theme = SystemBackdropTheme.Dark;
                    break;
                case ElementTheme.Light:
                    m_configurationSource.Theme = SystemBackdropTheme.Light;
                    break;
                case ElementTheme.Default:
                    m_configurationSource.Theme = SystemBackdropTheme.Default;
                    break;
            }
        }
        #endregion

        #region Disposing
        private void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    ((FrameworkElement)this.Content).ActualThemeChanged -= OnThemeChanged;
                    if (m_acrylicController != null)
                    {
                        m_acrylicController.Dispose();
                        m_acrylicController = null;
                    }
                    this.Activated -= OnWindowActivated;
                    m_configurationSource = null;
                    NavigationView.SelectionChanged -= NavigationSelectionChanged;
                }

                m_disposedValue = true;
            }
        }

        ~MainWindow()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}