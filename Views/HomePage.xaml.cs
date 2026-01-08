using System.Windows;
using System.Windows.Controls;
using MoyuWindows.Models;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class HomePage : Page
{
    private readonly AppState _appState;
    private int _wordCount;
    
    private static readonly Dictionary<string, string> BookNames = new()
    {
        ["CET4_1"] = "四级核心词汇",
        ["CET4_3"] = "四级完整词汇",
        ["CET6_1"] = "六级核心词汇",
        ["CET6_3"] = "六级完整词汇",
        ["IELTS_3"] = "雅思词汇",
        ["TOEFL_2"] = "托福词汇",
        ["SAT_2"] = "SAT词汇",
        ["GRE_3"] = "GRE词汇",
        ["Goin"] = "五十音",
        ["StdJp_Mid"] = "标日中级词汇"
    };
    
    public HomePage()
    {
        InitializeComponent();
        
        _appState = AppState.Instance;
        _wordCount = _appState.DefaultWordCount;
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // 更新词书名称
        BookNameText.Text = BookNames.GetValueOrDefault(_appState.CurrentBook, _appState.CurrentBook);
        
        // 更新进度
        var progress = DatabaseService.Instance.GetProgress(_appState.CurrentBook);
        ProgressText.Text = $"进度: {progress.current}/{progress.total}";
        BookProgressBar.Value = progress.total > 0 ? (double)progress.current / progress.total * 100 : 0;
        
        // 更新数量
        WordCountText.Text = _wordCount.ToString();
        
        // 更新统计
        var stats = _appState.Statistics;
        TodayLearnedText.Text = stats.TodayLearned.ToString();
        StreakDaysText.Text = stats.StreakDays.ToString();
        AccuracyText.Text = $"{stats.TodayAccuracy:F0}%";
    }
    
    private void DecreaseCount_Click(object sender, RoutedEventArgs e)
    {
        if (_wordCount > 5)
        {
            _wordCount -= 5;
            WordCountText.Text = _wordCount.ToString();
        }
    }
    
    private void IncreaseCount_Click(object sender, RoutedEventArgs e)
    {
        if (_wordCount < 100)
        {
            _wordCount += 5;
            WordCountText.Text = _wordCount.ToString();
        }
    }
    
    private void QuickSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int count))
        {
            _wordCount = count;
            WordCountText.Text = _wordCount.ToString();
        }
    }
    
    private void StartLearning_Click(object sender, RoutedEventArgs e)
    {
        // 保存设置
        _appState.DefaultWordCount = _wordCount;
        _appState.SaveSettings();
        
        // 创建单词列表
        _appState.CreateWordList(_wordCount);
        
        if (_appState.WordList.Count > 0)
        {
            // 导航到记忆页面
            _appState.CurrentPage = PageType.Remember;
        }
        else
        {
            MessageBox.Show("该词书已学习完成！请选择其他词书或重置进度。", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
