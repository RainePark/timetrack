using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore.SkiaSharpView.Painting;
using Newtonsoft.Json;
using SkiaSharp;
using WPFUI.Core;
using WPFUI.MVVM.Model;

namespace WPFUI.MVVM.ViewModel
{
    class UsageViewModel : IPage
    { 
        private string _pageTitle;   
        public string PageTitle
        {
            get => this._pageTitle;
            set 
            { 
                this._pageTitle = value; 
                OnPropertyChanged();
            }
        }
        
        public DateTime AnalysisTime { get; set; }
        public int UsageHours { get; set; }
        public int UsageMinutes { get; set; }
        public int TotalUsageSeconds { get; set; }
        public string ScreenTimeTodayString { get; set; }
        public Dictionary<int,int> AppUsageByHour { get; set; }
        public int ActiveHours { get; set; }
        public string AveragePerActiveHourString { get; set; }
        public Dictionary<string, int> AppUsageDictionary { get; set; }
        public ObservableCollection<string> MostUsedAppsCollection { get; set; }
        public Dictionary<string, int> WindowTitleDictionary { get; set; }
        public ObservableCollection<string> MostCommonWindowTitleCollection { get; set; }
        public string AnalysisTimeString { get; set; }
        
        // Pie chart values
        public IEnumerable<ISeries> PieSeries { get; set; }
        public LiveChartsCore.Measure.Margin PieMargin { get; set; }
        public SolidColorPaint PieLegendTextPaint { get; set; }
        
        // Bar graph values
        public ISeries[] ChartSeries { get; set; }
        public Axis[] ChartXAxes { get; set; }
        public Axis[] ChartYAxes { get; set; }
        
        public UsageViewModel()
        {
            this.PageTitle = "Usage";
            this.AnalysisTime = DateTime.Now;
            this.TotalUsageSeconds = ProgramUsageModel.GetTotalUsageSinceMidnight();
            TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
            this.UsageHours = (int)totalDayUsage.TotalHours;
            this.UsageMinutes = (int)totalDayUsage.Minutes;
            this.ScreenTimeTodayString = $"{UsageHours}hrs, {UsageMinutes}mins";
            this.AppUsageByHour = GetHourlyUsageCount();
            this.ActiveHours = AppUsageByHour.Keys.ToList().Count;
            this.AveragePerActiveHourString = $"(average {Math.Round((double)((UsageHours*60)+UsageMinutes)/ActiveHours).ToString()}mins per active hour)";
            this.AppUsageDictionary = GetProgramUsageDictionary();
            this.MostUsedAppsCollection = GenerateMostUsedCollection(AppUsageDictionary);
            this.WindowTitleDictionary = GetWindowTitleUsageDictionary();
            this.MostCommonWindowTitleCollection = GenerateMostUsedCollection(WindowTitleDictionary);
            this.AnalysisTimeString = $"Report generated at {AnalysisTime.ToString("dd-MM-yy hh:mm:ss tt")}";
                      
            // Generate pie chart
            this.PieSeries = GeneratePieSeries(AppUsageDictionary);
            this.PieMargin = new LiveChartsCore.Measure.Margin(10, 0);
            this.PieLegendTextPaint = new SolidColorPaint 
            { 
                Color = new SKColor(255, 255, 255), 
                SKTypeface = SKTypeface.FromFamilyName("Montserrat"), 
            };
            
            // Generate bar graph
            GenerateBarChart(AppUsageByHour);
        }

        public Dictionary<string, int> GetProgramUsageDictionary()
        {
            string query = "SELECT program, COUNT(*) as program_count " +
                           "FROM detailed_usage " +
                           "WHERE time >= @specified_time " +
                           $"AND (system = false OR system = {SettingsModel.GetUserSettings().SystemApps}) " +
                           "GROUP BY program;";
            Dictionary<string, int> programUsage = new Dictionary<string, int>();
            using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
            {
                SQLiteCommand command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                connection.Open();
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string programName = reader.GetString(0);
                    int programCount = reader.GetInt32(1);
                    programUsage[programName] = programCount;
                }
            }
            var sortedProgramUsage = programUsage.OrderByDescending(x => x.Value);
            Dictionary<string, int> sortedDictionary = new Dictionary<string, int>();
            foreach (var kvp in sortedProgramUsage)
            {
                sortedDictionary.Add(kvp.Key, kvp.Value);
            }
            return sortedDictionary;
        }

        public Dictionary<string, int> GetWindowTitleUsageDictionary()
        {
            string query = "SELECT windowTitle, COUNT(*) as windowTitle_count " +
                        "FROM detailed_usage " +
                        "WHERE time >= @specified_time " +
                        $"AND (system = false OR system = {SettingsModel.GetUserSettings().SystemApps}) " +
                        "GROUP BY windowTitle;";
            Dictionary<string, int> windowTitleUsage = new Dictionary<string, int>();
            using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
            {
                SQLiteCommand command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@specified_time", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                connection.Open();
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string windowTitle = reader.GetString(0);
                    int windowTitleCount = reader.GetInt32(1);
                    windowTitleUsage[windowTitle] = windowTitleCount;
                }
            }
            var sortedWindowTitleUsage = windowTitleUsage.OrderByDescending(x => x.Value);
            Dictionary<string, int> sortedDictionary = new Dictionary<string, int>();
            foreach (var kvp in sortedWindowTitleUsage)
            {
                sortedDictionary.Add(kvp.Key, kvp.Value);
            }
            return sortedDictionary;
        }
        
        public Dictionary<int, int> GetHourlyUsageCount()
        {
            Dictionary<int, int> hourlyItemCount = new Dictionary<int, int>();
            string query = "SELECT strftime('%H', datetime(time, 'localtime')) as hour, COUNT(*) as count " +
                           "FROM detailed_usage " +
                           "WHERE time >= @start_time " +
                           $"AND (system = false OR system = {SettingsModel.GetUserSettings().SystemApps}) " +
                           "GROUP BY hour;";
            using (var connection = new SQLiteConnection("Data Source=user\\usagedata.db"))
            {
                SQLiteCommand command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@start_time", DateTime.Today.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                connection.Open();
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int hour = Convert.ToInt32(reader.GetString(0));
                    int count = reader.GetInt32(1);
                    hourlyItemCount[hour] = count;
                }
            }

            return hourlyItemCount;
        }

        public ObservableCollection<string> GenerateMostUsedCollection(Dictionary<string, int> inputDictionary)
        {
            ObservableCollection<string> outputCollection = new ObservableCollection<string>();
            Dictionary<string, int> inputDictionaryTrimmed = inputDictionary.Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);
            int i = 1;
            foreach (KeyValuePair<string, int> pair in inputDictionaryTrimmed)
            {
                TimeSpan usageTimeSpan = TimeSpan.FromSeconds(pair.Value);
                string outputString = $"{i}. {pair.Key} - {(int)usageTimeSpan.TotalHours}hrs, {(int)usageTimeSpan.Minutes}mins";
                outputCollection.Add(outputString);
                i++;
            }
            return outputCollection;
        }
        
        public IEnumerable<ISeries> GeneratePieSeries(Dictionary<string, int> inputDictionary)
        {
            List<ISeries> outputSeries = new List<ISeries>();
            int x = (int)Math.Ceiling(Math.Log(inputDictionary.Count, 2) * 1.6);
            Dictionary<string, int> inputDictionaryTrimmed = inputDictionary.Take(x).ToDictionary(pair => pair.Key, pair => pair.Value);
            Dictionary<string, int> inputDictionaryRemaining = inputDictionary.Skip(x).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (KeyValuePair<string, int> pair in inputDictionaryTrimmed)
            {
                string key = pair.Key.Length > 30 ? pair.Key.Substring(0, 30) : pair.Key;
                outputSeries.Add(new PieSeries<double> { Values = new double[] { (double)Math.Round(pair.Value / 60.0, 1) }, Name = pair.Key });
            }
            if (inputDictionaryRemaining.Values.ToList().Count > 0)
            {
                outputSeries.Add(new PieSeries<double> { Values = new double[] { (double)Math.Round(inputDictionaryRemaining.Values.Sum() / 60.0, 1) }, Name = "Other" });
            }
            return outputSeries;
        }

        public void GenerateBarChart(Dictionary<int, int> inputDictionary)
        {
            // Ensure that the dictionary has exactly 24 keys from 0 to 23
            Dictionary<int, double> hourlyUsage = new Dictionary<int, double>();
            for (int i = 0; i < 24; i++)
            {
                if (inputDictionary.ContainsKey(i))
                {
                    hourlyUsage[i] = Math.Round((double)inputDictionary[i] / 60, 1);
                }
                else
                {
                    hourlyUsage[i] = 0;
                }
            }
            IEnumerable<double> values = Enumerable.Range(0, 24).Select(i => hourlyUsage.ContainsKey(i) ? (double)hourlyUsage[i] : 0);
            
            /*// Create series to highlight current hour
            int currentHour = DateTime.Now.Hour;
            // Create an array of values with 24 elements
            double[] valuesHourHighlighter = new double[24];
            // Set the value of the current hour to 60 and the values of all other hours to 0
            for (int i = 0; i < 24; i++)
            {
                valuesHourHighlighter[i] = (i == currentHour) ? 60 : 0;
            }*/

            this.ChartSeries = new ISeries[] {
                new ColumnSeries<double>
                {
                    Name = "Usage",
                    Values = values,
                    Stroke = null,
                    Fill = new SolidColorPaint(SKColors.White),
                    IgnoresBarPosition = true
                }
                // Code to add background shading to the columns
                /*new ColumnSeries<double>
                {
                    IsHoverable = false,
                    Values = new double[] { 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, 60 },
                    Stroke = null,
                    Fill = new SolidColorPaint(new SKColor(255, 255, 255, 100)),
                    IgnoresBarPosition = true
                },
                */
                // Code to add background shading to the column of the current hour
                /*new ColumnSeries<double>
                {
                    IsHoverable = false,
                    Values = valuesHourHighlighter,
                    Stroke = null,
                    Fill = new SolidColorPaint(new SKColor(255, 255, 255)),
                    IgnoresBarPosition = true
                }*/
            };
            this.ChartYAxes = new Axis[] { 
                new Axis
                {
                    MinLimit = 0, 
                    MaxLimit = 60, 
                    MinStep = 15, 
                    ForceStepToMin = true, 
                    LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255))
                }
            };
            this.ChartXAxes = new Axis[] {
                new Axis
                {
                    Labels = new string[] {
                        "0:00", "", "", "",
                        "4:00", "", "", "",
                        "8:00", "", "", "",
                        "12:00", "", "", "",
                        "16:00", "", "", "",
                        "20:00", "", "", "23:00"
                    },
                    ForceStepToMin = true,
                    LabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255))
                }
            };
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}