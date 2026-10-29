using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SystemMonitor.Models;

namespace SystemMonitor.Services
{
    internal class JsonSettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            RespectRequiredConstructorParameters = true
        };

        public JsonSettingsService()
        {
            string localDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            _settingsFilePath = Path.Combine(
                localDataPath, "SystemMonitor", "settings.json");
        }

        public async Task<MonitorSettings> LoadAsync()
        {
            string json;

            try
            {
                json = await File.ReadAllTextAsync(_settingsFilePath, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                return MonitorSettings.CreateDefault();
            }
            catch (DirectoryNotFoundException)
            {
                return MonitorSettings.CreateDefault();
            }

            MonitorSettings? settings = JsonSerializer.Deserialize<MonitorSettings>(json, _jsonOptions);

            if (settings is null)
            {
                throw new JsonException("설정 파일에 설정 객체가 없습니다.");
            }

            return settings;
        }

        public async Task SaveAsync(MonitorSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            string directory = Path.GetDirectoryName(_settingsFilePath)!;

            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(directory, $"settings.{Guid.NewGuid():N}.tmp");

            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonOptions);

                await File.WriteAllTextAsync(temporaryPath, json, Encoding.UTF8);

                if (File.Exists(_settingsFilePath))
                {
                    File.Replace(temporaryPath, _settingsFilePath, _settingsFilePath + ".bak");
                }
                else
                {
                    File.Move(temporaryPath, _settingsFilePath);
                }
            }
            finally
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception)
                    when (exception is IOException ||
                    exception is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"임시 설정 파일 정리 실패: {exception}");
                }
            }
        }
    }
}
