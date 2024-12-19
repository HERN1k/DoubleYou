using System;
using System.Diagnostics;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Pages;
using DoubleYou.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

using WinRT;
using WinRT.Interop;

namespace DoubleYou.AppWindows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IntroductionWindow : Window, IDisposable
    {
        private readonly AppWindow m_AppWindow;
        private readonly IWindowsHelper m_windowsHelper;
        private WindowsSystemDispatcherQueueHelper? m_wsdqHelper;
        private DesktopAcrylicController? m_acrylicController;
        private SystemBackdropConfiguration? m_configurationSource;
        private bool m_disposedValue;

        public IntroductionWindow()
        {
            this.InitializeComponent();
            m_windowsHelper = (Application.Current as App)?.ServiceProvider.GetService<IWindowsHelper>()
                ?? throw new ArgumentNullException(nameof(IWindowsHelper));
            m_AppWindow = GetAppWindowForCurrentWindow();
            SubscribeToEvents();
            ConfigureWindowTitleBar();
            TrySetAcrylicBackdrop(useAcrylicThin: false);
            NavigateToIntroductionPage();
        }

        private void SubscribeToEvents()
        {
            ((FrameworkElement)this.Content).ActualThemeChanged += OnThemeChanged;
            if (DesktopAcrylicController.IsSupported())
            {
                this.Activated += OnWindowActivated;
                this.Closed += OnWindowClosed;
            }
        }

        private void NavigateToIntroductionPage()
        {
            bool isEnqueued = this.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    RootFrame.Navigate(typeof(IntroductionPage));
                }
                catch (Exception ex)
                {
                    ShowException(ex);
                }
            });

            EnsureAddedTaskToUIThread(isEnqueued, nameof(NavigateToIntroductionPage));
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
                }

                m_disposedValue = true;
            }
        }

        ~IntroductionWindow()
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