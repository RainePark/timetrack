using System;
using System.Collections.Generic;
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

public class ProgramUsageModel : ObservableObject
{
    private Timer _timer;
    private string _currentProgram;

    public string CurrentProgram
    {
        get { return _currentProgram; }
        set
        {
            if (_currentProgram != value)
            {
                _currentProgram = value;
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

            string createLTTableQuery =
                "CREATE TABLE IF NOT EXISTS lt_usage (date INTEGER, program TEXT, usage INTEGER)";
            using (SQLiteCommand command = new SQLiteCommand(createLTTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            connection.Close();
        }

        _timer = new Timer(1000);
        _timer.Elapsed += async (sender, args) => await UpdateActiveApplication();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private async Task UpdateActiveApplication()
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

        this.CurrentProgram = windowTitle;

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
    }

     public static Dictionary<string, ProgramDetails> GetKnownPrograms()
    {
        return (JsonConvert.DeserializeObject<Dictionary<string, ProgramDetails>>(File.ReadAllText("user\\programlist.json")));
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