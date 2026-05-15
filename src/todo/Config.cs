using System.Text.Json;

namespace todo;

public class ConfigHandler {
    public static string ConfigPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "stefan.todo"
    );

    public static string ConfigFilePath() => Path.Combine(ConfigPath(), "config.json");

    public static bool ConfigExists() => File.Exists(ConfigFilePath());

    public static void CreateConfig() {
        if (!Directory.Exists(ConfigPath())) {
            Directory.CreateDirectory(ConfigPath());
        }

        SaveConfig(new Config(Path.Combine(ConfigPath(), "todo")));
    }

    public static void SaveConfig(Config config) => File.WriteAllText(ConfigFilePath(), JsonSerializer.Serialize(config));

    public static Config? LoadConfig() => ConfigExists() ? JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFilePath())) : null;

}

public class Config(string todoPath) {
    public string TodoPath { get; init; } = todoPath;
}
