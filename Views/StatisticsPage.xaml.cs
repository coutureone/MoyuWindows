using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class StatisticsPage : Page
{
    private readonly AppState _appState;
    
    public StatisticsPage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadStatistics();
    }
    
    private void LoadStatistics()
    {
        try
        {
            _appState.LoadStatistics();
            var stats = _appState.Statistics;
            
            // 今日统计
            TodayLearnedText.Text = stats.TodayLearned.ToString();
            TodayCorrectText.Text = stats.TodayCorrect.ToString();
            TodayWrongText.Text = stats.TodayWrong.ToString();
            
            AccuracyText.Text = $"{stats.TodayAccuracy:F0}%";
            AccuracyBar.Value = stats.TodayAccuracy;
            
            // 累计统计
            TotalLearnedText.Text = stats.TotalLearned.ToString();
            TotalDaysText.Text = stats.TotalDays.ToString();
            StreakDaysText.Text = stats.StreakDays.ToString();
            
            // 最近7天
            var records = DatabaseService.Instance.GetLast7DaysRecords();
            DailyRecordsList.ItemsSource = records;
            
            // 成就
            var achievements = DatabaseService.Instance.GetAchievements();
            AchievementsList.ItemsSource = achievements;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载统计失败: {ex.Message}");
        }
    }
    
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = Models.PageType.Home;
    }
}

/// <summary>
/// Bool 到透明度转换器
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked)
        {
            return isUnlocked ? 1.0 : 0.4;
        }
        return 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
