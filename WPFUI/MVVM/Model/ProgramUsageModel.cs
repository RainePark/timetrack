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
    
    // This observable collection is used to display the active blocks on the dashboard
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

    // Import the required methods to be able to get the current program that is being used
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public ProgramUsageModel()
    {
        // Create the database if it doesn't exist
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
        
        // Get the current usage to date
        this.TotalUsageSeconds = GetTotalUsageSinceMidnight();
        TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
        this.UsageHours = (int)totalDayUsage.TotalHours;
        this.UsageMinutes = (int)totalDayUsage.Minutes;
        // Update the elements displayed on the dashboard
        this.ActiveBlocksCollection = new ObservableCollection<Block>(BlocksModel.GetAllActiveBlocks());
        UpdateDashboardText();
        
        // Start a timer that updates the active program and databases every second
        _timer = new Timer(1000);
        _timer.Elapsed += async (sender, args) => await UpdateActiveApplication();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    // Main function of the program that runs every second to log the active program
    public async Task UpdateActiveApplication()
    {
        // Get the actve window details and set the pid, executable path, process name and window title from the function
        List<string> currentWindowDetails = GetActiveWindowDetails();
        int currentPid = Convert.ToInt32(currentWindowDetails[0]);
        string executablePath = currentWindowDetails[1];
        string processName = currentWindowDetails[2];
        string windowTitle = currentWindowDetails[3];
        // Checks that the program is in the programlist and updates any values that need to be
        AppendProgramDetails(executablePath, processName);
        // Checks if it is a new day to see if the blockstatus database needs to be reset + clears out data from the detailed usage database that is over 10 days old
        CheckIfNewDay();
        // Checks if the current program is a system app
        bool system;
        try { system = GetKnownPrograms()[processName].system; }
        catch { system = true; }
        // Catches a special case of the "Idle" process which is not able to be accessed even with admin privileges
        if (processName == "Idle"){ system = true; }
        // Logs the program usage to the database
        LogToDatabase(processName, windowTitle, DateTime.Now, system);
        // Update the elements displayed on the dashboard
        this.TotalUsageSeconds = GetTotalUsageSinceMidnight();
        TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
        this.UsageHours = (int)totalDayUsage.TotalHours;
        this.UsageMinutes = (int)totalDayUsage.Minutes;
        this.ActiveBlocksCollection = new ObservableCollection<Block>(BlocksModel.GetAllActiveBlocks());
        UpdateDashboardText();
        // Checks to see if the current program needs to be blocked
        BlocksModel.CheckAllBlocks(processName, currentPid);
    }

    // Returns the ProgramDetails of all known programs
    public static Dictionary<string, ProgramDetails> GetKnownPrograms()
    {
        return (JsonConvert.DeserializeObject<Dictionary<string, ProgramDetails>>(File.ReadAllText("user\\programlist.json")));
    }

    // Return a string of the user's screen time for the dashboard view
    public void UpdateDashboardText()
    {
        // Checks if the word hour should be plural or not
        if (this.UsageHours == 1)
        {
            this.DashboardText = this.UsageHours + " hour, ";
        }
        else
        {
            this.DashboardText = this.UsageHours + " hours, ";
        }
        // Checks if the word minute should be plural or not
        if (this.UsageMinutes == 1)
        {
            this.DashboardText += this.UsageMinutes + " minute.";
        }
        else
        {
            this.DashboardText += this.UsageMinutes + " minutes.";
        }
    }
    
    // Gets screen time of the user for that day in seconds
    public static int GetTotalUsageSinceMidnight()
    {
        int secondCount;
        // Initiates connection with the database
        using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            // Gets the total number of program usage entries since midnight, filtering out system apps if the user has chosen to
            string query = $"SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time AND (system = false OR system = {SettingsModel.GetUserSettings().SystemApps});";
            using (var command = new SQLiteCommand(query, connection))
            {
                connection.Open();
                // The start time to compare to is set to midnight local time and converted to UTC as that is how it is stored in the database
                // This allows for changes in time zone to still show accurate data for that "day"
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                secondCount = Convert.ToInt32(command.ExecuteScalar());
            }
        }
        return secondCount;
    }

    // Gets the usage of a specific program since midnight in seconds
    public static int GetProgramUsageSinceMidnight(string name)
    {
        int secondCount;
        // Initiates connection with the database
        using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            // Gets the total number of entries for a specific program since midnight
            string query = "SELECT COUNT(*) FROM detailed_usage WHERE time > @specified_time AND program = @program_name;";
            using (var command = new SQLiteCommand(query, connection))
            {
                // Similar to the GetTotalUsageSinceMidnight function, the start time is set to midnight local time and converted to UTC
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@program_name", name);
                secondCount = Convert.ToInt32(command.ExecuteScalar());
            }
        }
        return secondCount;
    }

    // Returns a list of all the details needed about the active window
    // This is done is one function as it is more efficient to only call GetForegroundWindow() once and reduces the chance that the window changes between calls
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
        // Returns null if there is an error to prevent crashes of the program.
        // This is not a major problem as if the path is null in other functions it is usually caught and handled. 
        // Additionally, if the path is accessable the next time the program is run, it will update to the new, correct value. 
        catch
        {
            processPath = null;
        }

        // Get process name
        string processName = process.ProcessName;
        
        // Return all the values retreived from the process
        return new List<string>{((int)processId).ToString(), processPath, processName, windowTitle};
    }

    // Log program usage to the database
    static void LogToDatabase(string program, string windowtitle, DateTime datetime, Boolean system)
    {
        // Get the current tme in UTC to write to database
        DateTime currentTime = datetime.ToUniversalTime();
        // Initiates connection with the database
        using (SQLiteConnection connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
        {
            connection.Open();
            
            // Update detailed usage database with timestamp, program at current second, and window title.
            string insertDetailedQuery = "INSERT INTO detailed_usage (time, program, windowtitle, system) VALUES (@time, @program, @windowtitle, @system)";
            using (SQLiteCommand detailedcommand = new SQLiteCommand(insertDetailedQuery, connection))
            {
                detailedcommand.Parameters.AddWithValue("@time", currentTime);
                detailedcommand.Parameters.AddWithValue("@program", program);
                detailedcommand.Parameters.AddWithValue("@windowtitle", windowtitle);
                detailedcommand.Parameters.AddWithValue("@system", system);
                detailedcommand.ExecuteNonQuery();
            }
            
            // Update long term usage database with program usage
            string searchSql = "SELECT * FROM lt_usage WHERE date = @date AND program = @program";
            using (SQLiteCommand ltcommand = new SQLiteCommand(searchSql, connection))
            {
                // Check if the program is already logged for that day
                ltcommand.Parameters.AddWithValue("@date", datetime.ToString("yyyy-MM-dd"));
                ltcommand.Parameters.AddWithValue("@program", program);
                SQLiteDataReader ltreader = ltcommand.ExecuteReader();
                if (ltreader.HasRows && ltreader.Read())
                {
                    // Make sure there isn't more than 1 row
                    int usage = ltreader.GetInt32(ltreader.GetOrdinal("usage"));
                    if (ltreader.Read())
                    {
                        throw new Exception("Multiple rows returned for date and program");
                    }
                    // Add 1 to the value if the program is already logged
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
                    // Add the program to the database if it is not already there
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

    // Append program details to the program database
    static public void AppendProgramDetails(string executablePath, string processName)
    {
        // Get known programs from json file
        Dictionary<string, ProgramDetails> knownPrograms = GetKnownPrograms();
        // Check if the program is already in the database
        if (!knownPrograms.ContainsKey(processName))
        {
            // Skips adding the program and logs an error if the path is null
            if (executablePath != null)
            {
                // Gets the executable description from the file details
                FileVersionInfo executableDetails = FileVersionInfo.GetVersionInfo(executablePath); 
                string executableDescription;
                
                // If the description is null, use the file name instead
                if (String.IsNullOrEmpty(executableDetails.FileDescription))
                {
                    executableDescription = executableDetails.FileName.Split("\\").Last();
                }
                else
                {
                    executableDescription = executableDetails.FileDescription;
                }

                // Checks if the program is a system program
                bool system = executablePath.Contains("C:\\Windows\\");

                // Adds the program to the database
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
            // Logs an error if the path is null
            else
            {
                string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Exectable \"{processName}\" path is null";
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
            // Check if the executable path exists
            if (File.Exists(programDetails.path))
            {
                // Create a clone of the program details to compare to the original
                ProgramDetails newProgramDetails = programDetails.Clone();
                // Updates the path if it has been changed
                if (programDetails.path != executablePath)
                {
                    newProgramDetails.path = executablePath;
                }
                // Get updated executable details based on the new path
                FileVersionInfo executableDetails = FileVersionInfo.GetVersionInfo(executablePath); 
                // Updates the executable description and system status
                if (String.IsNullOrEmpty(executableDetails.FileDescription))
                {
                    newProgramDetails.executableDescription = executableDetails.FileName.Split("\\").Last();
                }
                else
                {
                    newProgramDetails.executableDescription = executableDetails.FileDescription;
                }
                newProgramDetails.system = executablePath.Contains("C:\\Windows\\");
                
                // Update the database with the new program details if there are any changes
                if (!programDetails.HasSameValuesAs(newProgramDetails))
                {
                    knownPrograms[processName] = newProgramDetails;
                    using (StreamWriter writer = new StreamWriter("user\\programlist.json"))
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(knownPrograms));
                    }
                    string updateWithTimestamp = $"[I] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} \"{processName}\" has been updated in programlist.json";
                    using (StreamWriter writer = File.AppendText("user\\log.txt"))
                    {
                        writer.WriteLine(updateWithTimestamp);
                    }
                }
            }
            else
            {
                // Log an error if the executable path no longer exists
                string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Could not find path {programDetails.path} for executable \"{processName}\"";
                using (StreamWriter writer = File.AppendText("user\\log.txt"))
                {
                    writer.WriteLine(errorWithTimestamp);
                }
            }            
        }
    }

    // Function to check if it is a new day
    public void CheckIfNewDay()
    {
        // Check for the latest entry in the database
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
                    // If the current date is after the latest entry, set a flag to be true
                    DateTime latestDateTimeEntry = Convert.ToDateTime(result).ToLocalTime().Date;
                    int compareResult = DateTime.Compare(currentDate, latestDateTimeEntry);
                    currentDateIsAfterLatestEntry = compareResult > 0;
                }
            }
            connection.Close();
        }
        // Remove all entries from the database older than 10 days if it is a new day
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
            // Clear the block status database if it is a new day
            BlocksModel.WriteBlockStatus(new Dictionary<string, object>());
        }
    }
}

// Class to store program details
public class ProgramDetails
{
    public string executableDescription;
    public string path;
    public bool system;
     
    // Function to clone the program details
    public ProgramDetails Clone()
    {
        return new ProgramDetails { executableDescription = executableDescription, path = path, system = system };
    }

    // Function to check if two program details have the same values
    public bool HasSameValuesAs(ProgramDetails other)
    {
        return executableDescription == other.executableDescription && path == other.path && system == other.system;
    }
}