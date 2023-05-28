using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using WPFUI.Core;

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
        public ObservableCollection<Dictionary<string, int>> MostUsedAppsCollection { get; set; }
        public Dictionary<string, int> WindowTitleDictionary { get; set; }
        public ObservableCollection<Dictionary<string, int>> MostCommonWindowTitleCollection { get; set; }
        // then just need to create the graphs and we're good

        public IEnumerable<ISeries> Series { get; set; }
        public LiveChartsCore.Measure.Margin Margin { get; set; }
        public SolidColorPaint LegendTextPaint { get; set; }
        
        public UsageViewModel()
        {
            this.PageTitle = "Usage";
            this.AnalysisTime = DateTime.Now;
            this.TotalUsageSeconds = ProgramUsageModel.GetTotalUsageSinceMidnight();
            TimeSpan totalDayUsage = TimeSpan.FromSeconds(this.TotalUsageSeconds);
            this.UsageHours = (int)totalDayUsage.TotalHours;
            this.UsageMinutes = (int)totalDayUsage.Minutes;
            
            // dont forget to take into account the system flag in the sql calls
            
            // Dictionary<string, int> sortedDictionary = myDictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            
            // testing
            Margin = new LiveChartsCore.Measure.Margin(10, 0); 
            Series = new ISeries[] 
            {
                new PieSeries<double> { Values = new double[] { 2 }, Name = "Slice 1" },
                new PieSeries<double> { Values = new double[] { 4 }, Name = "Slice 2" },
                new PieSeries<double> { Values = new double[] { 1 }, Name = "Slice 3" },
                new PieSeries<double> { Values = new double[] { 4 }, Name = "Slice 4" },
                new PieSeries<double> { Values = new double[] { 3 }, Name = "Slice 5" }
            };
            LegendTextPaint = new SolidColorPaint 
            { 
                Color = new SKColor(255, 255, 255), 
                SKTypeface = SKTypeface.FromFamilyName("Montserrat"), 
            }; 
        }

        /*public Dictionary<string, int> GetProgramUsage()
        {
            
        }*/

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}