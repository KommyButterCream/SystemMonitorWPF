using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using SystemMonitor.Models;
using SystemMonitor.Services;
using SystemMonitor.ViewModels;
using SystemMonitor.Views;

namespace SystemMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            try
            {
                ISettingsService settingsService = new JsonSettingsService();

                MonitorSettings settings;
                string? warningMessage = null;

                try
                {
                    settings = await settingsService.LoadAsync();
                }
                catch (Exception exception)
                when (exception is JsonException ||
                exception is ArgumentException ||
                exception is IOException ||
                exception is UnauthorizedAccessException)
                {
                    Debug.WriteLine(exception.ToString());

                    settings = MonitorSettings.CreateDefault();

                    warningMessage =
                     "설정을 읽지 못해 기본값으로 시작합니다.\n" +
                     "기존 파일은 변경하지 않았습니다.\n\n" +
                     exception.Message;
                }

                var monitoringService = new HardwareMonitoringService();

                var viewModel = new MainViewModel(monitoringService);

                viewModel.ApplySettings(settings);

                var window = new MainWindow(viewModel, settingsService);

                MainWindow = window;

                ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;

                window.Show();

                if (warningMessage is not null)
                {
                    MessageBox.Show(
                        window,
                        warningMessage,
                        "설정 불러오기",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());

                MessageBox.Show(
                    "앱을 시작하지 못했습니다.\n\n" + exception.Message,
                    "System Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }
    }

}
