using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using SystemMonitor.Services;
using SystemMonitor.ViewModels;

namespace SystemMonitor.Views
{
    public partial class MainWindow : Window
    {
        private bool _isClosing;
        private bool _allowClose;

        private readonly ISettingsService _settingsService;

        internal MainWindow(MainViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();

            _settingsService = settingsService;

            DataContext = viewModel;
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (e.Cancel || _allowClose)
            {
                return;
            }

            e.Cancel = true;

            if (_isClosing)
            {
                return;
            }

            _isClosing = true;

            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    await viewModel.ShutdownAsync();
                }

                // 현재 Closing 이벤트가 반환된 뒤 닫기를 다시 요청합니다.
                // 정리가 즉시 끝나더라도 Closing 안에서 Close를 재호출하지 않습니다.
                await Dispatcher.InvokeAsync(() =>
                {
                    _allowClose = true;
                    Close();
                }, DispatcherPriority.Normal);

                // 다른 Closing 구독자가 닫기를 취소했다면 다음 요청을 허용합니다.
                _allowClose = false;
                _isClosing = false;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());

                _allowClose = false;
                _isClosing = false;

                MessageBox.Show(
                    this,
                    "종료 처리 중 오류가 발생했습니다. 출력 창을 확인해주세요.",
                    "System Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing || DataContext is not MainViewModel mainViewModel)
            {
                return;
            }

            var settingsViewModel = new SettingsViewModel(mainViewModel.CurrentSettings, _settingsService);

            var settingsWindow = new SettingsWindow(settingsViewModel)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true && settingsViewModel.Result is { } settings)
            {
                mainViewModel.ApplySettings(settings);
            }
        }
    }
}