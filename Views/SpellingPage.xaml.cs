using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoyuWindows.Models;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class SpellingPage : Page
{
    private readonly AppState _appState;
    private bool _answered;
    
    public SpellingPage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        SetupQuestion();
        AnswerInput.Focus();
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
        
        // 更新进度
        var progress = _appState.Progress * 100;
        ProgressBar.Value = progress;
        ProgressText.Text = _appState.ProgressText;
        
        // 设置题目
        QuestionText.Text = word.TranCN;
        
        // 音标
        if (!string.IsNullOrEmpty(word.Usphone))
        {
            PhoneticText.Text = $"/{word.Usphone}/";
            PhoneticText.Visibility = Visibility.Visible;
            SpeakButton.Visibility = Visibility.Visible;
        }
        else
        {
            PhoneticText.Visibility = Visibility.Collapsed;
            SpeakButton.Visibility = Visibility.Collapsed;
        }
        
        // 重置输入框
        AnswerInput.Text = "";
        AnswerInput.IsEnabled = true;
        AnswerInput.BorderBrush = (Brush)FindResource("BorderBrush");
        
        // 重置反馈
        FeedbackText.Visibility = Visibility.Collapsed;
        CorrectAnswerPanel.Visibility = Visibility.Collapsed;
        
        // 显示/隐藏按钮
        ActionButtonsPanel.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Collapsed;
        
        AnswerInput.Focus();
    }
    
    private void Speak_Click(object sender, RoutedEventArgs e)
    {
        var word = _appState.CurrentWord;
        if (word != null && !string.IsNullOrEmpty(word.HeadWord))
        {
            _appState.Speak(word.HeadWord);
        }
    }
    
    private void AnswerInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 移除反馈（用户继续输入时）
        if (!_answered)
        {
            FeedbackText.Visibility = Visibility.Collapsed;
        }
    }
    
    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        CheckAnswer();
    }
    
    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        var word = _appState.CurrentWord;
        if (word != null)
        {
            // 跳过也算错误
            _appState.RecordWrongAnswer(word);
        }
        
        GoToNext();
    }
    
    private void CheckAnswer()
    {
        if (_answered) return;
        
        var word = _appState.CurrentWord;
        if (word == null) return;
        
        var userAnswer = AnswerInput.Text.Trim();
        if (string.IsNullOrEmpty(userAnswer))
        {
            // 提示用户输入
            FeedbackText.Text = "请输入答案";
            FeedbackText.Foreground = (Brush)FindResource("WarningBrush");
            FeedbackText.Visibility = Visibility.Visible;
            return;
        }
        
        _answered = true;
        AnswerInput.IsEnabled = false;
        
        // 检查答案（忽略大小写）
        bool isCorrect = string.Equals(userAnswer, word.HeadWord, StringComparison.OrdinalIgnoreCase);
        
        if (isCorrect)
        {
            // 正确
            AnswerInput.BorderBrush = (Brush)FindResource("SuccessBrush");
            FeedbackText.Text = "✓ 正确！";
            FeedbackText.Foreground = (Brush)FindResource("SuccessBrush");
            FeedbackText.Visibility = Visibility.Visible;
            
            _appState.RecordCorrectAnswer();
            _appState.IncrementProgress();
        }
        else
        {
            // 错误
            AnswerInput.BorderBrush = (Brush)FindResource("DangerBrush");
            FeedbackText.Text = "✗ 错误";
            FeedbackText.Foreground = (Brush)FindResource("DangerBrush");
            FeedbackText.Visibility = Visibility.Visible;
            
            // 显示正确答案
            CorrectAnswerText.Text = word.HeadWord;
            CorrectAnswerPanel.Visibility = Visibility.Visible;
            
            _appState.RecordWrongAnswer(word);
        }
        
        // 隐藏提交/跳过按钮，显示下一题按钮
        ActionButtonsPanel.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Visible;
        NextButton.Focus();
    }
    
    private void Next_Click(object sender, RoutedEventArgs e)
    {
        GoToNext();
    }
    
    private void GoToNext()
    {
        // 标记当前单词已学习
        var word = _appState.CurrentWord;
        if (word != null && !_answered)
        {
            _appState.UpdateWordStatus(word.WordRank, 1);
        }
        
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
    
    private void Page_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_answered)
                {
                    GoToNext();
                }
                else
                {
                    CheckAnswer();
                }
                e.Handled = true;
                break;
            
            case Key.Escape:
                if (!_answered)
                {
                    Skip_Click(sender, e);
                    e.Handled = true;
                }
                break;
        }
    }
}
