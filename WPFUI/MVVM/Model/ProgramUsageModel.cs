using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WPFUI.Core;
using Newtonsoft.Json;

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
        _timer = new Timer(1000);
        _timer.Elapsed += async (sender, args) => await UpdateWindowTitle();
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private async Task UpdateWindowTitle()
    {
        int currentPid = GetActiveWindowProcessId();
        string processName = GetProcessNameByPID(currentPid);
        this.CurrentProgram = GetExecutableDescriptionByPID(currentPid);
        
        var knownPrograms = JsonConvert.DeserializeObject<Dictionary<string,ProgramDetails>>(File.ReadAllText("programlist.json"));

        if (knownPrograms.ContainsKey(processName) == false)
        {
            knownPrograms[processName] = new ProgramDetails {executableDescription = GetExecutableDescriptionByPID(currentPid), path = GetExecutablePathByPID(currentPid)};
            using (StreamWriter writer = new StreamWriter("programlist.json"))
            {
                writer.WriteLine(JsonConvert.SerializeObject(knownPrograms));
            }
        }
        else
        {
            /*CHECK IF THE CURRENT PROGRAM MATCHES DATABASE AND IF ANY FIELDS ARE BLANK HERE*/
        }
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

    static string GetActiveWindowProgramName()
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
    
    static string GetExecutableDescriptionByPID(int pid)
    {
        Process process = Process.GetProcessById(pid);
        string filePath = process.MainModule.FileName;
        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(filePath);
        return fileInfo.FileDescription;
    }
    
    static string GetExecutablePathByPID(int pid)
    {
        Process process = Process.GetProcessById(pid);
        return process.MainModule.FileName;
    }
}

public class ProgramDetails
{
    public string executableDescription;
    public string path;
}