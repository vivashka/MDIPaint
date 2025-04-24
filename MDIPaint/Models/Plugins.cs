using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PluginInterface;

namespace MDIPaint.Models;

public class Plugins
{
    public Dictionary<string, bool> Filters { get; set; } = new Dictionary<string, bool>();
    
    public void SavePluginsToAppSettings(Plugins plugins, string filePath = "appsettings.json")
    {
        var json = File.ReadAllText(filePath);

        var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        jsonObj["Plugins"] = new { Filters = plugins.Filters };

        var updatedJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, updatedJson);
    }

}