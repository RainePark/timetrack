using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    
    private int _usageMinutes;
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
    
    private int _totalUsageSeconds;
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
            string createDetailedTableQuery = "CREATE TABLE IF NOT EXISTS detailed_usage (time DATETIME, program TEXT, windowtitle TEXT, system BOOLEAN)";
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
        
        this.TotalUsageSeconds = GetTotalUsageSinceMidnight();
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
        List<string> currentWindowDetails = GetActiveWindowDetails();
        int currentPid = Convert.ToInt32(currentWindowDetails[0]);
        string executablePath = currentWindowDetails[1];
        string processName = currentWindowDetails[2];
        string windowTitle = currentWindowDetails[3];
        AppendProgramDetails(executablePath, processName);
        CheckIfNewDay();
        bool system;
        try { system = GetKnownPrograms()[processName].system; }
        catch { system = false; }
        LogToDatabase(processName, windowTitle, DateTime.Now, system);
        this.TotalUsageSeconds = GetTotalUsageSinceMidnight();
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
        if (this.UsageHours == 1)
    {
        this.DashboardText = this.UsageHours + " hour, ";
    }
    else
    {
        this.DashboardText = this.UsageHours + " hours, ";
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
    
    public static int GetTotalUsageSinceMidnight()
    {
        int secondCount;
        using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            string query;
            if (SettingsModel.GetUserSettings().SystemApps)
            {
                query = "SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time;";
            }
            else
            {
                query = "SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time AND system = 0;";
            }
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                secondCount = Convert.ToInt32(command.ExecuteScalar());
            }
        }
        return secondCount;
    }

    public static int GetProgramUsageSinceMidnight(string name)
    {
        int secondCount;
        using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            string query = "SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time AND program = @program_name;";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@program_name", name);
                secondCount = Convert.ToInt32(command.ExecuteScalar());
            }
        }
        return secondCount;
    }

    static List<string> GetActiveWindowDetails()
    {
        // Get PID and Process object of active window
        IntPtr hwnd = GetForegroundWindow();
        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);
        Process process = Process.GetProcessById((int)processId);
        
        // Get window title
        StringBuilder builder = new StringBuilder(1024);
        GetWindowText(hwnd, builder, 1024);
        string windowTitle = builder.ToString();
        windowTitle.Replace(process.ProcessName + " - ", "");
        
        // Get executable path
        string processPath;
        try
        {
            ProcessModule mainModule = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.FileName == process.MainModule.FileName);
            processPath = mainModule?.FileName;
        }
        catch
        {
            processPath = null;
        }

        // Get process name
        string processName = process.ProcessName;
        
        return new List<string>{((int)processId).ToString(), processPath, processName, windowTitle};
    }

    static void LogToDatabase(string program, string windowtitle, DateTime datetime, Boolean system)
    {
        DateTime currentTime = datetime.ToUniversalTime();
        using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            
            /* Update detailed usage database with timestamp, program at current second, and window title. */
            string insertDetailedQuery = "INSERT INTO detailed_usage (time, program, windowtitle, system) VALUES (@time, @program, @windowtitle, @system)";
            using (SQLiteCommand detailedcommand = new SQLiteCommand(insertDetailedQuery, connection))
            {
                detailedcommand.Parameters.AddWithValue("@time", currentTime);
                detailedcommand.Parameters.AddWithValue("@program", program);
                detailedcommand.Parameters.AddWithValue("@windowtitle", windowtitle);
                detailedcommand.Parameters.AddWithValue("@system", system);
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

    static public void AppendProgramDetails(string executablePath, string processName)
    {
        Dictionary<string, ProgramDetails> knownPrograms = GetKnownPrograms();
        if (!knownPrograms.ContainsKey(processName))
        {
            if (executablePath != null)
            {
                FileVersionInfo executableDetails = FileVersionInfo.GetVersionInfo(executablePath); 
                string executableDescription;
                
                if (String.IsNullOrEmpty(executableDetails.FileDescription))
                {
                    executableDescription = executableDetails.FileName.Split("\\").Last();
                }
                else
                {
                    executableDescription = executableDetails.FileDescription;
                }
                bool system = executablePath.Contains("C:\\Windows\\");
                knownPrograms[processName] = new ProgramDetails
                {
                    executableDescription = executableDescription, 
                    path = executablePath,
                    system = system
                };
                using (StreamWriter writer = new StreamWriter("user\\programlist.json"))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(knownPrograms));
                }
            }
            else
            {
                string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Could not access new process \"{processName}\"";
                using (StreamWriter writer = File.AppendText("user\\log.txt"))
                {
                    writer.WriteLine(errorWithTimestamp);
                }
            }
        }
        // Check for any changes to the executable
        else
        {
            ProgramDetails programDetails = knownPrograms[processName];
            ProgramDetails newProgramDetails = programDetails.Clone();
            // NEED TO CHECK IF PATH ACTUALLY EXISTS FIRST - ALSO NEED TO CHECK ALL REFERENCES TO PATH TO IGNORE PATH NOT FOUND
            if (programDetails.path != executablePath)
            {
                newProgramDetails.path = executablePath;
            }
            FileVersionInfo executableDetails = FileVersionInfo.GetVersionInfo(executablePath); 
            if (String.IsNullOrEmpty(executableDetails.FileDescription))
            {
                newProgramDetails.executableDescription = executableDetails.FileName.Split("\\").Last();
            }
            else
            {
                newProgramDetails.executableDescription = executableDetails.FileDescription;
            }
            newProgramDetails.system = executablePath.Contains("C:\\Windows\\");
            
            if (!programDetails.HasSameValuesAs(newProgramDetails))
            {
                knownPrograms[processName] = newProgramDetails;
                using (StreamWriter writer = new StreamWriter("user\\programlist.json"))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(knownPrograms));
                }
            }
        }
    }

    public void CheckIfNewDay()
    {
        bool currentDateIsAfterLatestEntry = false;
        DateTime currentDate = DateTime.Now.ToLocalTime().Date;
        using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            string query1 = "SELECT MAX(time) FROM detailed_usage";
            using (SQLiteCommand command = new SQLiteCommand(query1, connection))
            {
                object result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    DateTime latestDateTimeEntry = Convert.ToDateTime(result).ToLocalTime().Date;
                    int compareResult = DateTime.Compare(currentDate, latestDateTimeEntry);
                    currentDateIsAfterLatestEntry = compareResult > 0;
                }
            }
            connection.Close();
        }
        if (currentDateIsAfterLatestEntry){
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
            {
                connection.Open();
                string query = "DELETE FROM detailed_usage WHERE time < DATE('now', '-10 days')";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            BlocksModel.WriteBlockStatus(new Dictionary<string, object>());
        }
    }
}

public class ProgramDetails
{
    public string executableDescription;
    public string path;
    public bool system;
        
    public ProgramDetails Clone()
    {
        return new ProgramDetails { executableDescription = executableDescription, path = path, system = system };
    }

    public bool HasSameValuesAs(ProgramDetails other)
    {
        return executableDescription == other.executableDescription && path == other.path && system == other.system;
    }
}