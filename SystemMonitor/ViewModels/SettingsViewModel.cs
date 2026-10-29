using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SystemMonitor.Commands;
using SystemMonitor.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using SystemMonitor.Services;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace SystemMonitor.ViewModels
{
    internal class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingService;

        private string _errorMessage = string.Empty;

        private bool _isSaving;

        public DeviceThresholdEditorViewModel Cpu { get; }
        public DeviceThresholdEditorViewModel Ram { get; }
        public DeviceThresholdEditorViewModel Gpu { get; }

        public ObservableCollection<DeviceThresholdEditorViewModel> DeviceEditors { get; } = new();

        public string UpdateIntervalText { get; set; }
        public MonitorSettings? Result { get; private set; }
        public AsyncRelayCommand ApplyCommand { get; }
        public RelayCommand CancelCommand { get; }

        public event Action<bool>? CloseRequested;

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }

            private set
            {
                if (_errorMessage == value)
                {
                    return;
                }

                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaving
        {
            get
            {
                return _isSaving;
            }

            private set
            {
                if (_isSaving == value)
                {
                    return;
                }

                _isSaving = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEdit));

                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanEdit
        {
            get
            {
                return !IsSaving;
            }
        }
        public SettingsViewModel(MonitorSettings settings, ISettingsService settingService)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));

            Cpu = new DeviceThresholdEditorViewModel("CPU", settings.CpuUsageRule, settings.CpuTemperatureRule);
            Ram = new DeviceThresholdEditorViewModel("RAM", settings.RamUsageRule, settings.RamTemperatureRule);
            Gpu = new DeviceThresholdEditorViewModel("NVIDIA GPU", settings.GpuUsageRule, settings.GpuTemperatureRule);

            DeviceEditors.Add(Cpu);
            DeviceEditors.Add(Ram);
            DeviceEditors.Add(Gpu);

            UpdateIntervalText = settings.UpdateIntervalMilliseconds.ToString(CultureInfo.CurrentCulture);

            ApplyCommand = new AsyncRelayCommand(ApplyAsync, exception =>
            {
                Debug.WriteLine(exception.ToString());

                ErrorMessage = "설정을 저장하지 못했습니다. 경로와 쓰기 권한을 확인해주세요.";
            });

            CancelCommand = new RelayCommand(
                () => CloseRequested?.Invoke(false),
                () => !IsSaving);
        }

        private async Task ApplyAsync()
        {
            ErrorMessage = string.Empty;

            MonitorSettings settings;

            try
            {
                ThresholdRule cpuUsage = Cpu.CreateUsageRule();
                ThresholdRule cpuTemperature = Cpu.CreateTemperatureRule();

                ThresholdRule ramUsage = Ram.CreateUsageRule();
                ThresholdRule ramTemperature = Ram.CreateTemperatureRule();

                ThresholdRule gpuUsage = Gpu.CreateUsageRule();
                ThresholdRule gpuTemperature = Gpu.CreateTemperatureRule();

                if (!int.TryParse(UpdateIntervalText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int updateIntervalMilliseconds))
                {
                    ErrorMessage = "갱신 간격은 정수로 입력해주세요.";
                    return;
                }

                if (updateIntervalMilliseconds < MonitorSettings.MinimumUpdateIntervalMilliseconds ||
                  updateIntervalMilliseconds > MonitorSettings.MaximumUpdateIntervalMilliseconds)
                {
                    ErrorMessage = "갱신 간격은 500~5000ms 범위로 입력해주세요.";
                    return;
                }

                settings = new MonitorSettings(cpuUsage, cpuTemperature,
                    updateIntervalMilliseconds,
                    ramUsage, ramTemperature,
                    gpuUsage, gpuTemperature);
            }
            catch (ArgumentException exception)
            {
                ErrorMessage = exception.Message;
                return;
            }

            IsSaving = true;

            try
            {
                await _settingService.SaveAsync(settings);
            }
            finally
            {
                IsSaving = false;
            }

            Result = settings;

            CloseRequested?.Invoke(true);
        }
    }
}
