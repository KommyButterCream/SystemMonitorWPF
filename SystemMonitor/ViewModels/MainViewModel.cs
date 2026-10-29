using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SystemMonitor.Commands;
using SystemMonitor.Models;
using SystemMonitor.Services;
using System.Collections.ObjectModel;

namespace SystemMonitor.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private CancellationTokenSource? _monitoringCancellation;
        private Task _monitoringTask = Task.CompletedTask;
        private Task? _shutdownTask;
        private bool _isShuttingDown;

        private readonly IMonitoringService _monitoringService;

        private MonitorSettings _settings = MonitorSettings.CreateDefault();

        private bool _isMonitoring;

        private PeriodicTimer? _monitoringTimer;

        public MonitorSettings CurrentSettings
        {
            get
            {
                return _settings;
            }
        }

        public void ApplySettings(MonitorSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if(_isShuttingDown)
            {
                return;
            }

            _settings = settings;

            if(_monitoringTimer is not null)
            {
                _monitoringTimer.Period = TimeSpan.FromMilliseconds(settings.UpdateIntervalMilliseconds);
            }

            CpuCard.ApplyRules(settings.CpuUsageRule, settings.CpuTemperatureRule);
            RamCard.ApplyRules(settings.RamUsageRule, settings.RamTemperatureRule);
            GpuCard.ApplyRules(settings.GpuUsageRule, settings.GpuTemperatureRule);

            OnPropertyChanged(nameof(CurrentSettings));
        }
        public bool IsMonitoring
        {
            get
            {
                return _isMonitoring;
            }
            private set
            {
                if(_isMonitoring == value)
                {
                    return;
                }

                _isMonitoring = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MonitoringStatus));

                StartMonitoringCommand.RaiseCanExecuteChanged();
                StopMonitoringCommand.RaiseCanExecuteChanged();
            }
        }

        public string MonitoringStatus
        {
            get
            {
                if(!IsMonitoring)
                {
                    return "중지됨";
                }

                if(_monitoringCancellation?.IsCancellationRequested == true)
                {
                    return "중지 중";
                }

                return "갱신 중";
            }
        }

        public RelayCommand StartMonitoringCommand { get; }
        public RelayCommand StopMonitoringCommand { get; }

        public DeviceCardViewModel CpuCard { get; }
        public DeviceCardViewModel RamCard { get; }
        public DeviceCardViewModel GpuCard { get; } 

        public ObservableCollection<DeviceCardViewModel> DeviceCards { get; } = new ObservableCollection<DeviceCardViewModel>();

        public MainViewModel(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService
                ?? throw new ArgumentNullException(nameof(monitoringService));

            CpuCard = new DeviceCardViewModel("CPU", _settings.CpuUsageRule, _settings.CpuTemperatureRule);
            RamCard = new DeviceCardViewModel("RAM", _settings.RamUsageRule, _settings.RamTemperatureRule);
            GpuCard = new DeviceCardViewModel("GPU", _settings.GpuUsageRule, _settings.GpuTemperatureRule);
            
            DeviceCards.Add(CpuCard);
            DeviceCards.Add(RamCard);
            DeviceCards.Add(GpuCard);

            StartMonitoringCommand = new RelayCommand(StartMonitoring, CanStartMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, CanStopMonitoring);
        }

        private bool CanStartMonitoring()
        {
            return !IsMonitoring && !_isShuttingDown;
        }

        private bool CanStopMonitoring()
        {
            return IsMonitoring && 
                _monitoringCancellation is not null &&
                !_monitoringCancellation.IsCancellationRequested;
        }

        private void StartMonitoring()
        {
            if(!CanStartMonitoring())
            {
                return;
            }

            var cancellation = new CancellationTokenSource();

            _monitoringCancellation = cancellation;

            IsMonitoring = true;

            _monitoringTask = MonitorAsync(cancellation);
        }

        public void StopMonitoring()
        {
            _monitoringCancellation?.Cancel();

            OnPropertyChanged(nameof(MonitoringStatus));

            StopMonitoringCommand.RaiseCanExecuteChanged();
        }

        private void ApplyReading(MonitoringSnapshot snapshot)
        {
            ApplyDeviceReading(CpuCard, snapshot.Cpu);
            ApplyDeviceReading(RamCard, snapshot.Ram);
            ApplyDeviceReading(GpuCard, snapshot.Gpu);
        }

        private static void ApplyDeviceReading(DeviceCardViewModel card, DeviceReading reading)
        {
            card.Update(
                 reading.UsagePercent,
                 reading.Status,
                 reading.TemperatureCelsius,
                 reading.TemperatureStatus,
                 reading.UsedGiB,
                 reading.TotalGiB,
                 reading.CapacityStatus,
                 reading.TemperatureLabel);
        }
        private async Task MonitorAsync(CancellationTokenSource cancellation)
        {
            CancellationToken token = cancellation.Token;

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_settings.UpdateIntervalMilliseconds));

                _monitoringTimer = timer;

                while (await timer.WaitForNextTickAsync(token))
                {
                    token.ThrowIfCancellationRequested();

                    MonitoringSnapshot reading = await Task.Run(() => _monitoringService.ReadAll(), token);

                    token.ThrowIfCancellationRequested();

                    ApplyReading(reading);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 중지 과정에서 발생하는 정상적인 취소
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());

                var failedReading = new DeviceReading(
                    null,
                    DateTimeOffset.Now,
                    ReadingStatus.Error,
                    temperatureStatus: ReadingStatus.Error,
                    capacityStatus: ReadingStatus.Error);

                ApplyReading(new MonitoringSnapshot(
                    failedReading,
                    failedReading,
                    failedReading));
            }
            finally
            {
                _monitoringCancellation = null;
                _monitoringTimer = null;

                cancellation.Dispose();

                IsMonitoring = false;
            }
        }

        public Task ShutdownAsync()
        {
            // 여러 종료 요청이 와도 같은 정리 작업을 기다립니다.
            // 정리가 실패한 경우에는 다음 요청에서 재시도합니다.
            if (_shutdownTask is null || _shutdownTask.IsFaulted)
            {
                _shutdownTask = ShutdownCoreAsync();
            }

            return _shutdownTask;
        }

        private async Task ShutdownCoreAsync()
        {
            _isShuttingDown = true;
            StartMonitoringCommand.RaiseCanExecuteChanged();

            StopMonitoring();

            await _monitoringTask;

            await Task.Run(() => _monitoringService.Dispose());
        }
    }
}
