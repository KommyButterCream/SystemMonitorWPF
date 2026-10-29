using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml;
using LibreHardwareMonitor.Hardware;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal class HardwareMonitoringService : IMonitoringService
    {
        private readonly Computer _computer = new Computer
        {
            IsCpuEnabled = true,
            IsMemoryEnabled = true,
            IsGpuEnabled = true
        };

        private bool _isOpen;
        private bool _isDisposed;

        public MonitoringSnapshot ReadAll()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            DeviceReading cpu = ReadCpu();
            DeviceReading ram = ReadRam();
            DeviceReading gpu = ReadGpu();

            return new MonitoringSnapshot(cpu, ram, gpu);
        }
        private DeviceReading ReadCpu()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            try
            {
                if (!_isOpen)
                {
                    _computer.Open();
                    _isOpen = true;
                }

                IHardware? cpu = _computer.Hardware.FirstOrDefault(hardware => hardware.HardwareType == HardwareType.Cpu);

                if (cpu is null)
                {
                    return new DeviceReading(null, DateTimeOffset.Now, ReadingStatus.Unsupported);
                }

                cpu.Update();

                ISensor? usageSensor = cpu.Sensors.FirstOrDefault(sensor => sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total");
                ISensor? temperatureSensor = cpu.Sensors.FirstOrDefault(sensor => sensor.SensorType == SensorType.Temperature && sensor.Name == "Core (Tctl/Tdie)");

                double? usage = null;
                ReadingStatus usageStatus = ReadingStatus.Unsupported;

                if (usageSensor is not null)
                {
                    usageStatus = ReadingStatus.Error;

                    if (usageSensor.Value is float usageValue &&
                    float.IsFinite(usageValue) &&
                    usageValue >= 0.0f &&
                    usageValue <= 100.0f)
                    {
                        usage = usageValue;
                        usageStatus = ReadingStatus.Available;
                    }
                }

                double? temperature = null;
                ReadingStatus temperatureStatus = ReadingStatus.Unsupported;

                if (temperatureSensor is not null)
                {
                    temperatureStatus = ReadingStatus.Error;

                    if (temperatureSensor.Value is float temperatureValue &&
                        float.IsFinite(temperatureValue) &&
                        temperatureValue > 0.0f)
                    {
                        temperature = temperatureValue;
                        temperatureStatus = ReadingStatus.Available;
                    }
                }


                return new DeviceReading(usage, DateTimeOffset.Now, usageStatus, temperature, temperatureStatus);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());

                // 열기나 갱신이 실패하면 다음 수집에서 다시 초기화합니다.
                try
                {
                    _computer.Close();
                }
                catch (Exception cleanupException)
                {
                    Debug.WriteLine(cleanupException.ToString());
                }
                finally
                {
                    _isOpen = false;
                }

                return new DeviceReading(null, DateTimeOffset.Now, ReadingStatus.Error, null, ReadingStatus.Error);
            }
        }

        private DeviceReading ReadRam()
        {
            try
            {
                if (!_isOpen)
                {
                    _computer.Open();
                    _isOpen = true;
                }

                var memoryDevices = _computer.Hardware
                    .Where(hardware =>
                        hardware.HardwareType == HardwareType.Memory)
                    .ToArray();

                double? usage = null;
                double? usedGiB = null;
                double? totalGiB = null;

                ReadingStatus usageStatus = ReadingStatus.Unsupported;
                ReadingStatus capacityStatus = ReadingStatus.Unsupported;

                IHardware? physicalMemory = memoryDevices.FirstOrDefault(
                    hardware => hardware.Identifier.ToString() == "/ram");

                if (physicalMemory is not null)
                {
                    try
                    {
                        physicalMemory.Update();

                        ISensor? usageSensor =
                            physicalMemory.Sensors.FirstOrDefault(
                                sensor =>
                                    sensor.SensorType == SensorType.Load &&
                                    sensor.Name == "Memory");

                        usage = GetSensorValue(usageSensor, 0.0, 100.0);
                        usageStatus = GetSensorStatus(usageSensor, usage);

                        ISensor? usedSensor =
                            physicalMemory.Sensors.FirstOrDefault(
                                sensor =>
                                    sensor.SensorType == SensorType.Data &&
                                    sensor.Name == "Memory Used");

                        ISensor? availableSensor =
                            physicalMemory.Sensors.FirstOrDefault(
                                sensor =>
                                    sensor.SensorType == SensorType.Data &&
                                    sensor.Name == "Memory Available");

                        double? used = GetSensorValue(usedSensor);
                        double? available = GetSensorValue(availableSensor);

                        if (used is double usedValue &&
                            available is double availableValue &&
                            usedValue + availableValue > 0.0)
                        {
                            usedGiB = usedValue;
                            totalGiB = usedValue + availableValue;
                            capacityStatus = ReadingStatus.Available;
                        }
                        else
                        {
                            capacityStatus =
                                usedSensor is null || availableSensor is null
                                    ? ReadingStatus.Unsupported
                                    : ReadingStatus.Error;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(
                            $"물리 메모리 읽기 실패: {exception}");

                        usage = null;
                        usedGiB = null;
                        totalGiB = null;

                        usageStatus = ReadingStatus.Error;
                        capacityStatus = ReadingStatus.Error;
                    }
                }

                var dimms = memoryDevices
                    .Where(hardware =>
                        hardware.Identifier.ToString().StartsWith(
                            "/memory/dimm/",
                            StringComparison.Ordinal))
                    .ToArray();

                double? maximumTemperature = null;
                int validTemperatureCount = 0;
                bool temperatureReadFailed = false;

                foreach (IHardware dimm in dimms)
                {
                    try
                    {
                        dimm.Update();

                        ISensor? temperatureSensor =
                            dimm.Sensors.FirstOrDefault(
                                sensor =>
                                    sensor.SensorType == SensorType.Temperature &&
                                    sensor.Index == 0);

                        if (temperatureSensor is null)
                        {
                            continue;
                        }

                        double? temperature = GetSensorValue(
                            temperatureSensor,
                            double.Epsilon);

                        if (temperature is double value)
                        {
                            maximumTemperature =
                                maximumTemperature is double current
                                    ? Math.Max(current, value)
                                    : value;

                            validTemperatureCount++;
                        }
                        else
                        {
                            temperatureReadFailed = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        temperatureReadFailed = true;

                        Debug.WriteLine(
                            $"RAM 모듈 온도 읽기 실패: {exception}");
                    }
                }

                ReadingStatus temperatureStatus;

                if (validTemperatureCount > 0)
                {
                    temperatureStatus = ReadingStatus.Available;
                }
                else
                {
                    temperatureStatus = temperatureReadFailed
                        ? ReadingStatus.Error
                        : ReadingStatus.Unsupported;
                }

                string temperatureLabel =
                    $"온도 · 감지 모듈 최대 " +
                    $"({validTemperatureCount}/{dimms.Length})";

                return new DeviceReading(
                    usage,
                    DateTimeOffset.Now,
                    usageStatus,
                    maximumTemperature,
                    temperatureStatus,
                    usedGiB,
                    totalGiB,
                    capacityStatus,
                    temperatureLabel);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"RAM 읽기 실패: {exception}");

                return new DeviceReading(
                    null,
                    DateTimeOffset.Now,
                    ReadingStatus.Error,
                    temperatureStatus: ReadingStatus.Error,
                    capacityStatus: ReadingStatus.Error,
                    temperatureLabel: "온도 · 감지 모듈 최대");
            }
        }

        private DeviceReading ReadGpu()
        {
            try
            {
                if (!_isOpen)
                {
                    _computer.Open();
                    _isOpen = true;
                }

                IHardware? gpu = _computer.Hardware.FirstOrDefault(
                    hardware =>
                        hardware.HardwareType == HardwareType.GpuNvidia);

                if (gpu is null)
                {
                    return new DeviceReading(
                        null,
                        DateTimeOffset.Now,
                        ReadingStatus.Unsupported,
                        temperatureLabel: "온도 · GPU Core");
                }

                gpu.Update();

                ISensor? usageSensor = gpu.Sensors.FirstOrDefault(
                    sensor =>
                        sensor.SensorType == SensorType.Load &&
                        sensor.Name == "GPU Core");

                double? usage = GetSensorValue(
                    usageSensor,
                    0.0,
                    100.0);

                ReadingStatus usageStatus =
                    GetSensorStatus(usageSensor, usage);

                ISensor? temperatureSensor = gpu.Sensors.FirstOrDefault(
                    sensor =>
                        sensor.SensorType == SensorType.Temperature &&
                        sensor.Name == "GPU Core");

                double? temperature = GetSensorValue(
                    temperatureSensor,
                    double.Epsilon);

                ReadingStatus temperatureStatus =
                    GetSensorStatus(temperatureSensor, temperature);

                ISensor? usedSensor = gpu.Sensors.FirstOrDefault(
                    sensor =>
                        sensor.SensorType == SensorType.SmallData &&
                        sensor.Name == "GPU Memory Used");

                ISensor? totalSensor = gpu.Sensors.FirstOrDefault(
                    sensor =>
                        sensor.SensorType == SensorType.SmallData &&
                        sensor.Name == "GPU Memory Total");

                double? usedMiB = GetSensorValue(usedSensor);

                double? totalMiB = GetSensorValue(
                    totalSensor,
                    double.Epsilon);

                double? usedGiB = null;
                double? totalGiB = null;

                ReadingStatus capacityStatus;

                if (usedMiB is double used &&
                    totalMiB is double total &&
                    used <= total)
                {
                    usedGiB = used / 1024.0;
                    totalGiB = total / 1024.0;

                    capacityStatus = ReadingStatus.Available;
                }
                else
                {
                    capacityStatus =
                        usedSensor is null || totalSensor is null
                            ? ReadingStatus.Unsupported
                            : ReadingStatus.Error;
                }

                return new DeviceReading(
                    usage,
                    DateTimeOffset.Now,
                    usageStatus,
                    temperature,
                    temperatureStatus,
                    usedGiB,
                    totalGiB,
                    capacityStatus,
                    temperatureLabel: "온도 · GPU Core");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(
                    $"NVIDIA GPU 읽기 실패: {exception}");

                return new DeviceReading(
                    null,
                    DateTimeOffset.Now,
                    ReadingStatus.Error,
                    temperatureStatus: ReadingStatus.Error,
                    capacityStatus: ReadingStatus.Error,
                    temperatureLabel: "온도 · GPU Core");
            }
        }
        private static double? GetSensorValue(
            ISensor? sensor,
            double minimum = 0.0,
            double maximum = double.MaxValue)
        {
            if(sensor?.Value is float value &&
                float.IsFinite(value) &&
                value >= minimum &&
                value <= maximum)
            {
                return value;
            }

            return null;
        }

        private static ReadingStatus GetSensorStatus(
            ISensor? sensor,
            double? value)
        {
            if(sensor is null)
            {
                return ReadingStatus.Unsupported;
            }

            return value.HasValue ? ReadingStatus.Available : ReadingStatus.Error;
        }
          
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _computer.Close();
            _isOpen = false;
            _isDisposed = true;
        }
    }
}
