using System.Windows;
using System.Windows.Controls;
using MoyuWindows.Models;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class CongratulatePage : Page
{
    private readonly AppState _appState;
    
    private static readonly string[] Motivations = new[]
    {
        "坚持就是胜利，明天继续加油！",
        "积少成多，词汇量正在稳步提升！",
        "今天的努力是明天的收获！",
        "学习是最好的投资，继续保持！",
        "每天进步一点点，终将成就非凡！",
        "知识改变命运，加油！",
        "坚持背单词，英语水平蹭蹭涨！"
    };
    
    public CongratulatePage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        var stats = _appState.Statistics;
        var wordCount = _appState.WordList.Count;
        
        // 学习数量
        LearnedCountText.Text = wordCount.ToString();
        
        // 正确率
        var total = stats.TodayCorrect + stats.TodayWrong;
        var accuracy = total > 0 ? (double)stats.TodayCorrect / total * 100 : 0;
        AccuracyText.Text = $"{accuracy:F0}%";
        
        // 正确/错误数
        CorrectCountText.Text = stats.TodayCorrect.ToString();
        WrongCountText.Text = stats.TodayWrong.ToString();
        
        // 随机鼓励语
        var random = new Random();
        MotivationText.Text = Motivations[random.Next(Motivations.Length)];
    }
    
    private void ViewWrongBook_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.WrongBook;
    }
    
    private void BackToHome_Click(object sender, RoutedEventArgs e)
    {
        _appState.Reset();
    }
}
