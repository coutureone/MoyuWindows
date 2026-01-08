using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoyuWindows.Models;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class ChoicePage : Page
{
    private readonly AppState _appState;
    private readonly Button[] _optionButtons;
    private List<Word> _options = new();
    private int _correctIndex;
    private bool _answered;
    
    public ChoicePage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
        _optionButtons = new[] { Option1, Option2, Option3, Option4 };
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        SetupQuestion();
        Focusable = true;
        Focus();
    }
    
    private void SetupQuestion()
    {
        _answered = false;
        
        var word = _appState.CurrentWord;
        if (word == null)
        {
            _appState.CurrentPage = PageType.Congratulate;
            return;
        }
        
        // 如果是拼写模式，导航到拼写页面
        if (_appState.QuizMode == QuizMode.Spelling)
        {
            _appState.CurrentPage = PageType.Spelling;
            return;
        }
        
        // 更新进度
        var progress = _appState.Progress * 100;
        ProgressBar.Value = progress;
        ProgressText.Text = _appState.ProgressText;
        
        // 根据测试模式设置题目
        switch (_appState.QuizMode)
        {
            case QuizMode.CnToEn:
                // 显示中文，选择英文
                QuestionText.Text = word.TranCN;
                SetupEnglishOptions(word);
                break;
            
            case QuizMode.EnToCn:
                // 显示英文，选择中文
                QuestionText.Text = word.HeadWord;
                SetupChineseOptions(word);
                break;
            
            default:
                // 默认：中文选英文
                QuestionText.Text = word.TranCN;
                SetupEnglishOptions(word);
                break;
        }
    }
    
    private void SetupEnglishOptions(Word word)
    {
        // 获取干扰项（英文单词）
        var distractors = DatabaseService.Instance.GetRandomWordsForOptions(3, _appState.CurrentBook, word.WordRank);
        
        // 创建选项列表
        _options = new List<Word> { word };
        _options.AddRange(distractors);
        
        // 打乱顺序
        var random = new Random();
        _options = _options.OrderBy(_ => random.Next()).ToList();
        
        // 记录正确答案位置
        _correctIndex = _options.FindIndex(w => w.WordRank == word.WordRank);
        
        // 设置选项按钮（显示英文单词）
        var labels = new[] { "A", "B", "C", "D" };
        for (int i = 0; i < 4 && i < _options.Count; i++)
        {
            _optionButtons[i].Content = $"{labels[i]}. {_options[i].HeadWord}";
            _optionButtons[i].Tag = i;
            _optionButtons[i].IsEnabled = true;
            ResetButtonStyle(_optionButtons[i]);
        }
    }
    
    private void SetupChineseOptions(Word word)
    {
        // 获取干扰项（其他单词）
        var distractors = DatabaseService.Instance.GetRandomWordsForOptions(3, _appState.CurrentBook, word.WordRank);
        
        // 创建选项列表
        _options = new List<Word> { word };
        _options.AddRange(distractors);
        
        // 打乱顺序
        var random = new Random();
        _options = _options.OrderBy(_ => random.Next()).ToList();
        
        // 记录正确答案位置
        _correctIndex = _options.FindIndex(w => w.WordRank == word.WordRank);
        
        // 设置选项按钮（显示中文翻译）
        var labels = new[] { "A", "B", "C", "D" };
        for (int i = 0; i < 4 && i < _options.Count; i++)
        {
            _optionButtons[i].Content = $"{labels[i]}. {_options[i].TranCN}";
            _optionButtons[i].Tag = i;
            _optionButtons[i].IsEnabled = true;
            ResetButtonStyle(_optionButtons[i]);
        }
    }
    
    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (_answered) return;
        
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int index))
        {
            CheckAnswer(index);
        }
    }
    
    private void CheckAnswer(int selectedIndex)
    {
        if (_answered) return;
        _answered = true;
        
        var word = _appState.CurrentWord;
        bool isCorrect = selectedIndex == _correctIndex;
        
        // 显示结果
        if (isCorrect)
        {
            // 正确
            SetButtonCorrect(_optionButtons[selectedIndex]);
            _appState.RecordCorrectAnswer();
            
            // 更新进度
            _appState.IncrementProgress();
        }
        else
        {
            // 错误
            SetButtonWrong(_optionButtons[selectedIndex]);
            SetButtonCorrect(_optionButtons[_correctIndex]);
            
            if (word != null)
            {
                _appState.RecordWrongAnswer(word);
            }
        }
        
        // 禁用所有按钮
        foreach (var btn in _optionButtons)
        {
            btn.IsEnabled = false;
        }
        
        // 延迟后进入下一题
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            NextQuestion();
        };
        timer.Start();
    }
    
    private void NextQuestion()
    {
        if (_appState.CurrentIndex < _appState.WordList.Count - 1)
        {
            _appState.CurrentIndex++;
            SetupQuestion();
        }
        else
        {
            // 完成测试
            _appState.EndLearning();
            _appState.CurrentPage = PageType.Congratulate;
        }
    }
    
    private void ResetButtonStyle(Button button)
    {
        button.BorderBrush = (Brush)FindResource("BorderBrush");
        button.Background = (Brush)FindResource("SurfaceBrush");
    }
    
    private void SetButtonCorrect(Button button)
    {
        button.BorderBrush = (Brush)FindResource("SuccessBrush");
        button.Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)); // 半透明绿色
    }
    
    private void SetButtonWrong(Button button)
    {
        button.BorderBrush = (Brush)FindResource("DangerBrush");
        button.Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)); // 半透明红色
    }
    
    private void Page_KeyDown(object sender, KeyEventArgs e)
    {
        if (_answered) return;
        
        int index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.A => 0,
            Key.B => 1,
            Key.C => 2,
            Key.D => 3,
            _ => -1
        };
        
        if (index >= 0 && index < _options.Count)
        {
            CheckAnswer(index);
            e.Handled = true;
        }
    }
}
