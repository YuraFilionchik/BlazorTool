﻿using System.Text.Json;

namespace BlazorTool.Services
{
    public class UserSettings
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // For human-readable JSON
            PropertyNameCaseInsensitive = true // For flexibility in key casing
        };

        public UserSettings(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            // Ensure the directory for the settings file exists
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public T GetSetting<T>(string login, string key)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                if (!File.Exists(_filePath))
                {
                    return default(T);
                }

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return default(T);
                }

                var allSettings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json, _jsonOptions);

                if (allSettings != null && allSettings.TryGetValue(login, out var userSettings) && userSettings.TryGetValue(key, out var jsonElement))
                {
                    try
                    {
                        // Attempt to deserialize the JsonElement to the target type T
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Handle cases where the stored JSON structure doesn't match T
                        // For example, if 'value' is a string but T is int.
                        return default(T);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // File not found is a valid scenario for GetSetting, return default
                return default(T);
            }
            catch (JsonException)
            {
                // Problem parsing the main JSON structure
                // Consider logging this error
                return default(T);
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions (e.g., IO errors)
                // Consider logging this error: Console.WriteLine($"Error reading settings: {ex.Message}");
                return default(T);
            }
            return default(T);
        }

        public void SetSetting<T>(string login, string key, T value)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            Dictionary<string, Dictionary<string, object>> allSettings;

            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    // Handle empty or whitespace-only file content
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        allSettings = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        // Deserialize into object first, then we can add/update more freely
                        allSettings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json, _jsonOptions)
                                      ?? new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    allSettings = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (JsonException ex)
            {
                // If the file is corrupt, start with a new settings dictionary.
                // Consider logging this and backing up the corrupt file.
                allSettings = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                throw ex;
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions (e.g., IO errors not covered by FileNotFound for read)
                // Consider logging this error: Console.WriteLine($"Error reading settings file for SetSetting: {ex.Message}");
                // Depending on policy, you might re-throw or start fresh. For now, start fresh.
                allSettings = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
                throw ex;
            }


            if (!allSettings.TryGetValue(login, out var userSettings))
            {
                userSettings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                allSettings[login] = userSettings;
            }

            userSettings[key] = value;

            try
            {
                var updatedJson = JsonSerializer.Serialize(allSettings, _jsonOptions);
                File.WriteAllText(_filePath, updatedJson);
            }
            catch (Exception ex)
            {
                // Handle potential IO exceptions during write
                // Consider logging this error: Console.WriteLine($"Error writing settings: {ex.Message}");
                // Depending on desired behavior, you might re-throw or have a retry mechanism.
                throw ex; // Re-throw for now so the caller is aware of the failure.
            }
        }
    }
}
