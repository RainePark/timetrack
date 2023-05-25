using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using WPFUI.Core;

namespace WPFUI.MVVM.Model;

public class SettingsModel : ObservableObject
{
    public SettingsModel()
    {

    }

    public static Settings GetUserSettings()
    {
        return (JsonConvert.DeserializeObject<Settings>(File.ReadAllText("user\\settings.json")));
    }

    public static void WriteSettings(Settings settings)
    {
        using (StreamWriter writer = new StreamWriter("user\\settings.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(settings));
        }
    }
}

public class Settings
{
    public string UserName;
    public bool SystemApps;
    public string BlockType;
}