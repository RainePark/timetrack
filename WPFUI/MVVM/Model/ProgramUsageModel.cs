using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WPFUI.Core;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Globalization;
using WPFUI.MVVM.Model;

public class ProgramUsageModel : ObservableObject
{
    private Timer _timer;
    private int _usageHours;
    private int _usageMinutes;
    private int _totalUsageSeconds;

    public int UsageHours
    {
        get { return _usageHours; }
        set
        {
            if (_usageHours != value)
            {
                _usageHours = value;
                OnPropertyChanged();
            }
        }
    }
    
    public int UsageMinutes
    {
        get { return _usageMinutes; }
        set
        {
            if (_usageMinutes != value)
            {
                _usageMinutes = value;
                OnPropertyChanged();
            }
        }
    }
    
    public int TotalUsageSeconds
    {
        get { return _totalUsageSeconds; }
        set
        {
            if (_totalUsageSeconds != value)
            {
                _totalUsageSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    private string _dashboardText;
    public string DashboardText
    {
        get { return _dashboardText; }
        set
        {
            if (_dashboardText != value)
            {
                _dashboardText = value;
                OnPropertyChanged();
            }
        }
    }
    
    private ObservableCollection<Block> _activeBlocksCollection;
    public ObservableCollection<Block> ActiveBlocksCollection
    {
        get { return _activeBlocksCollection; }
        set
        {
            if (_activeBlocksCollection != value)
            {
                _activeBlocksCollection = value;
                OnPropertyChanged();
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public ProgramUsageModel()
    {
        using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            string createDetailedTableQuery = "CREATE TABLE IF NOT EXISTS detailed_usage (time DATETIME, program TEXT, windowtitle TEXT)";
            using (SQLiteCommand command = new SQLiteCommand(createDetailedTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            string createLTTableQuery = "CREATE TABLE IF NOT EXISTS lt_usage (date INTEGER, program TEXT, usage INTEGER)";
            using (SQLiteCommand command = new SQLiteCommand(createLTTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
        
        this.TotalUsageSeconds = GetUsageSinceMidnight();
        TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
        this.UsageHours = (int)totalDayUsage.TotalHours;
        this.UsageMinutes = (int)totalDayUsage.Minutes;
        this.ActiveBlocksCollection = new ObservableCollection<Block>(BlocksModel.GetAllActiveBlocks());
        UpdateDashboardText();
        
        _timer = new Timer(1000);
        _timer.Elapsed += async (sender, args) => await UpdateActiveApplication();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    public async Task UpdateActiveApplication()
    {
        int currentPid = GetActiveWindowProcessId();
        string processName = GetProcessNameByPID(currentPid);
        FileVersionInfo executableDetails = GetExecutableDetailsByPID(currentPid);
        string windowTitle = GetActiveWindowTitle();
        string executabledescription;
        if (executableDetails.FileDescription == "")
        {
            executabledescription = executableDetails.FileName.Split("\\").Last();
        }
        else
        {
            executabledescription = executableDetails.FileDescription;
        }
        
        Dictionary<string, ProgramDetails> knownPrograms = GetKnownPrograms();
        
        if (knownPrograms.ContainsKey(processName) == false)
        {
            bool system;
            if (executableDetails.FileName.Contains("C:\\Windows\\")) { system = true; }
            else { system = false; }

            knownPrograms[processName] = new ProgramDetails
            {
                executableDescription = executabledescription, path = GetExecutablePathByPID(currentPid),
                system = system, monitored = false
            };
            using (StreamWriter writer = new StreamWriter("user\\programlist.json"))
            {
                writer.WriteLine(JsonConvert.SerializeObject(knownPrograms));
            }
        }
        else
        {
            /*CHECK IF THE CURRENT PROGRAM MATCHES DATABASE AND IF ANY FIELDS ARE BLANK HERE*/
        }

        LogToDatabase(processName, windowTitle, DateTime.Now);
        this.TotalUsageSeconds = GetUsageSinceMidnight();
        TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
        this.UsageHours = (int)totalDayUsage.TotalHours;
        this.UsageMinutes = (int)totalDayUsage.Minutes;
        this.ActiveBlocksCollection = new ObservableCollection<Block>(BlocksModel.GetAllActiveBlocks());
        UpdateDashboardText();
        BlocksModel.CheckAllBlocks(processName, currentPid);
    }

    public static Dictionary<string, ProgramDetails> GetKnownPrograms()
    {
        return (JsonConvert.DeserializeObject<Dictionary<string, ProgramDetails>>(File.ReadAllText("user\\programlist.json")));
    }

     public void UpdateDashboardText()
     {
        Settings userSettings = SettingsModel.GetUserSettings();
        DateTime currentTime = DateTime.Now;string input = userSettings.UserName;

        if (currentTime.Hour >= 5 && currentTime.Hour < 12)
        {
            this.DashboardText = "Good morning, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
        }
        else if (currentTime.Hour >= 12 && currentTime.Hour < 17)
        {
            this.DashboardText = "Good afternoon, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
        }
        else if (currentTime.Hour >= 17 && currentTime.Hour < 22)
        {
            this.DashboardText = "Good evening, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
        }
        else
        {
            this.DashboardText = "Good night, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
        }
        if (this.UsageHours == 1)
        {
            this.DashboardText += this.UsageHours + " hour, ";
        }
        else
        {
            this.DashboardText += this.UsageHours + " hours, ";
        }
        if (this.UsageMinutes == 1)
        {
            this.DashboardText += this.UsageMinutes + " minute.";
        }
        else
        {
            this.DashboardText += this.UsageMinutes + " minutes.";
        }
     }

     public int GetUsageSinceMidnight()
     {
         int secondCount;
         using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
         {
             connection.Open();
             string query = "SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time;";
             using (var command = new SQLiteCommand(query, connection))
             {
                 command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                 secondCount = Convert.ToInt32(command.ExecuteScalar());
             }
         }
         return secondCount;
     }

    static int GetActiveWindowProcessId()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);
        return (int)processId;
    }

    static string GetProcessNameByPID(int pid)
    {
        Process process = Process.GetProcessById(pid);
        return process.ProcessName;
    }

    static string GetActiveWindowTitle()
    {
        IntPtr hwnd = GetForegroundWindow();
        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);
        Process process = Process.GetProcessById((int)processId);
        StringBuilder builder = new StringBuilder(1024);
        GetWindowText(hwnd, builder, 1024);
        string windowTitle = builder.ToString();
        return windowTitle.Replace(process.ProcessName + " - ", "");
    }

    static FileVersionInfo GetExecutableDetailsByPID(int pid)
    {
        Process process = Process.GetProcessById(pid);
        string filePath = process.MainModule.FileName;
        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(filePath);
        return fileInfo;
    }

    static string GetExecutablePathByPID(int pid)
    {
        Process process = Process.GetProcessById(pid);
        return process.MainModule.FileName;
    }

    static void LogToDatabase(string program, string windowtitle, DateTime datetime)
    {
        DateTime currentTime = datetime.ToUniversalTime();
        using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            
            /* Update detailed usage database with timestamp, program at current second, and window title. */
            string insertDetailedQuery = "INSERT INTO detailed_usage (time, program, windowtitle) VALUES (@time, @program, @windowtitle)";
            using (SQLiteCommand detailedcommand = new SQLiteCommand(insertDetailedQuery, connection))
            {
                detailedcommand.Parameters.AddWithValue("@time", currentTime);
                detailedcommand.Parameters.AddWithValue("@program", program);
                detailedcommand.Parameters.AddWithValue("@windowtitle", windowtitle);
                detailedcommand.ExecuteNonQuery();
            }
            
            /* Update long term usage database with program usage */
            string searchSql = "SELECT * FROM lt_usage WHERE date = @date AND program = @program";
            using (SQLiteCommand ltcommand = new SQLiteCommand(searchSql, connection))
            {
                ltcommand.Parameters.AddWithValue("@date", datetime.ToString("yyyy-MM-dd"));
                ltcommand.Parameters.AddWithValue("@program", program);
                SQLiteDataReader ltreader = ltcommand.ExecuteReader();
                if (ltreader.HasRows && ltreader.Read())
                {
                    int usage = ltreader.GetInt32(ltreader.GetOrdinal("usage"));
                    if (ltreader.Read())
                    {
                        throw new Exception("Multiple rows returned for date and program");
                    }
                    string updateLtSql = "UPDATE lt_usage SET usage = @usage WHERE program = @program";
                    using (SQLiteCommand updateLtCommand = new SQLiteCommand(updateLtSql, connection))
                    {
                        updateLtCommand.Parameters.AddWithValue("@usage", usage + 1);
                        updateLtCommand.Parameters.AddWithValue("@program", program);
                        updateLtCommand.ExecuteNonQuery();
                    }
                }
                else
                {
                    string insertLtQuery = "INSERT INTO lt_usage (date, program, usage) VALUES (@date, @program, 1)";
                    using (SQLiteCommand command = new SQLiteCommand(insertLtQuery, connection))
                    {
                        command.Parameters.AddWithValue("@date", datetime.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@program", program);
                        command.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }

        }
    }
}

public class ProgramDetails
{
    public string executableDescription;
    public string path;
    public bool system;
    public bool monitored;
}