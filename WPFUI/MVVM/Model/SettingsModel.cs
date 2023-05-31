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

    // Function to get the user settings from the settings.json file
    public static Settings GetUserSettings()
    {
        return (JsonConvert.DeserializeObject<Settings>(File.ReadAllText("user\\settings.json")));
    }

    // Function to write the user settings to the settings.json file
    public static void WriteSettings(Settings settings)
    {
        using (StreamWriter writer = new StreamWriter("user\\settings.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(settings));
        }
    }
}

// Class to store the user settings
// Implements INotifyPropertyChanged to notify the Settings UI of any changes
public class Settings : INotifyPropertyChanged
    {
        private string _userName;
        public string UserName
        {
            get => this._userName;
            set
            {
                this._userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        private bool _systemApps;
        public bool SystemApps
        {
            get => this._systemApps;
            set
            {
                this._systemApps = value;
                OnPropertyChanged(nameof(SystemApps));
            }
        }

        private string _blockType;
        public string BlockType
        {
            get => this._blockType;
            set
            {
                this._blockType = value;
                OnPropertyChanged(nameof(BlockType));
            }
        }

        // Create a PropertyChangedEventHandler to allow the UIs to be notified of any changes
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }