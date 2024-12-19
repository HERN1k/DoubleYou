using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

using DoubleYou.AppWindows;
using DoubleYou.Domain.Interfaces;
using DoubleYou.Infrastructure.Data.Contexts;
using DoubleYou.Infrastructure.Repositories;
using DoubleYou.Services;
using DoubleYou.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using Windows.Storage;

namespace DoubleYou
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application, IDisposable
    {
        public Window? MainWindow { get => m_window; }
        public Window? IntroductionWindow { get => m_introduction; }
        public IServiceProvider ServiceProvider
        {
            get => m_services ?? throw new InvalidOperationException(Constants.AN_UNEXPECTED_ERROR_OCCURRED);
        }

        private readonly IServiceProvider? m_services;
        private readonly IUsersRepository? m_usersRepository;
        private readonly ILogger<App>? m_logger;
        private Window? m_introduction;
        private Window? m_window;
        private bool m_disposedValue;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            SubscribeToEvents();
            m_services = ConfigureServices();
            m_usersRepository = GetUsersRepository();
            m_logger = GetLogger();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(m_services, nameof(m_services));

                MigrationsExtension.ApplyMigrations(m_services);

                MigrationsExtension.EnsureDatabaseRestoredFromDump();

                m_logger?.LogInformation(Constants.APP_INITIALIZED);

                if (m_usersRepository == null)
                {
                    Current.Exit();
                    return;
                }

                if (m_usersRepository!.AnyUser())
                {
                    ActivateMainWindow();
                }
                else
                {
                    ActivateIntroductionWindow();
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private static ServiceProvider? ConfigureServices()
        {
            try
            {
                var services = new ServiceCollection();

                services.AddLogging(configure =>
                {
                    configure.AddDebug();
#if DEBUG
                    configure.SetMinimumLevel(LogLevel.Information);
#else
                    configure.SetMinimumLevel(LogLevel.Warning);
#endif
                    configure.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error)
                        .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error);
                });

#if DEBUG
                Debug.WriteLine(string.Concat("DB path: ", System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.SQL_DB_FILE_NAME)));
#endif
                services.AddPooledDbContextFactory<AppDBContext>(options =>
                    options.UseSqlite(string.Concat("Data Source=", System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.SQL_DB_FILE_NAME))));

                services.AddHttpClient(nameof(GTranslator), client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        AllowAutoRedirect = false,
                        MaxConnectionsPerServer = 10
                    };
                });

                services.AddMemoryCache();

                services.AddSingleton<IWindowsHelper, WindowsHelper>();
                services.AddSingleton<ILocalizationService, Localization>();
                services.AddSingleton<IUsersRepository, UsersRepository>();
                services.AddSingleton<ITranslator, GTranslator>();
                services.AddSingleton<IWordsRepository, WordsRepository>();
                services.AddSingleton<IVocabularyService, VocabularyService>();

                services.AddSingleton<IntroductionWindow>();
                services.AddSingleton<MainWindow>();

                return services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return null;
            }
        }

        public void ActivateMainWindow()
        {
            if (m_services == null)
            {
                Current.Exit();
                return;
            }

            try
            {
                m_window = m_services!.GetService<MainWindow>();

                if (m_window == null)
                {
#if DEBUG
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
#endif
                    throw new InvalidOperationException(Constants.MAIN_WINDOW_RESOLUTION_ERROR_MESSAGE);
                }

                m_window.Activate();

                if (m_introduction != null)
                {
                    m_introduction.Close();
                    m_introduction = null;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ActivateIntroductionWindow()
        {
            if (m_services == null)
            {
                Current.Exit();
                return;
            }

            try
            {
                m_introduction = m_services!.GetService<IntroductionWindow>();

                if (m_introduction == null)
                {
#if DEBUG
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
#endif
                    throw new InvalidOperationException(Constants.INTRODUCTION_WINDOW_RESOLUTION_ERROR_MESSAGE);
                }

                m_introduction.Activate();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private ILogger<App>? GetLogger()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(m_services, nameof(m_services));

                return m_services.GetRequiredService<ILogger<App>>();
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return null;
            }
        }

        private IUsersRepository? GetUsersRepository()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(m_services, nameof(m_services));

                return m_services.GetRequiredService<IUsersRepository>();
            }
            catch (Exception ex)
            {
                ShowException(ex);
                return null;
            }
        }

        private void SubscribeToEvents()
        {
            this.UnhandledException += OnAppUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnAppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            m_logger?.LogCritical(
                e.Exception,
                "{AN_UNEXPECTED_ERROR_OCCURRED}{Message}",
                Constants.AN_UNEXPECTED_ERROR_OCCURRED, e.Exception.Message);

            var notification = new AppNotificationBuilder()
                .AddText("An exception was thrown.")
                .AddText($"Type: {e.Exception.GetType()}")
                .AddText($"Message: {e.Exception.Message}\r\n" +
                         $"HResult: {e.Exception.HResult}")
                .BuildNotification();

            e.Handled = true;

            AppNotificationManager.Default.Show(notification);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            m_logger?.LogCritical(
                e.Exception,
                "{AN_UNEXPECTED_ERROR_OCCURRED}{Message}",
                Constants.AN_UNEXPECTED_ERROR_OCCURRED, e.Exception.Message);

            var notification = new AppNotificationBuilder()
                .AddText("An exception was thrown.")
                .AddText($"Type: {e.Exception.GetType()}")
                .AddText($"Message: {e.Exception.Message}\r\n" +
                         $"HResult: {e.Exception.HResult}")
                .BuildNotification();

            e.SetObserved();

            AppNotificationManager.Default.Show(notification);
        }

        private static void ShowException(string message)
        {
#if DEBUG
            Debug.WriteLine(string.Concat(Constants.AN_UNEXPECTED_ERROR_OCCURRED, " ", message));
#endif
            Alerts.ShowCriticalErrorWindow(message);
        }

        private static void ShowException(Exception ex)
        {
#if DEBUG
            Debug.WriteLine(ex);
#endif
            Alerts.ShowCriticalErrorWindow(ex);
        }

        #region Disposing
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    this.UnhandledException -= OnAppUnhandledException;
                    TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                }

                m_disposedValue = true;
            }
        }

        ~App()
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