using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoyuWindows.Models;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class RememberPage : Page
{
    private readonly AppState _appState;
    
    public RememberPage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUI();
        Focusable = true;
        Focus();
    }
    
    private void UpdateUI()
    {
        var word = _appState.CurrentWord;
        if (word == null) return;
        
        // 更新进度
        var progress = _appState.Progress * 100;
        ProgressBar.Value = progress;
        ProgressText.Text = _appState.ProgressText;
        
        // 判断是否是日语五十音
        if (word.IsGoin)
        {
            // 显示五十音
            HeadWordText.Text = word.Hiragana ?? "";
            PhoneticText.Visibility = Visibility.Collapsed;
            SpeakButton.Visibility = Visibility.Collapsed;
            TranslationText.Visibility = Visibility.Collapsed;
            PhrasePanel.Visibility = Visibility.Collapsed;
            
            GoinPanel.Visibility = Visibility.Visible;
            KatakanaText.Text = word.Katakana ?? "";
            RomajiText.Text = word.Romaji ?? "";
        }
        else
        {
            // 显示英语单词
            HeadWordText.Text = word.HeadWord;
            PhoneticText.Text = string.IsNullOrEmpty(word.Usphone) ? "" : $"/{word.Usphone}/";
            PhoneticText.Visibility = string.IsNullOrEmpty(word.Usphone) ? Visibility.Collapsed : Visibility.Visible;
            SpeakButton.Visibility = Visibility.Visible;
            TranslationText.Text = word.TranCN;
            TranslationText.Visibility = Visibility.Visible;
            
            GoinPanel.Visibility = Visibility.Collapsed;
            
            // 例句
            if (!string.IsNullOrEmpty(word.Phrase))
            {
                PhrasePanel.Visibility = Visibility.Visible;
                PhraseText.Text = word.Phrase;
                PhraseCNText.Text = word.PhraseCN;
            }
            else
            {
                PhrasePanel.Visibility = Visibility.Collapsed;
            }
        }
        
        // 更新收藏状态
        UpdateFavoriteButton(word.IsFavorite || _appState.IsFavorite(word));
        
        // 最后一个单词时更改按钮文字
        if (_appState.CurrentIndex == _appState.WordList.Count - 1)
        {
            NextButton.Content = "开始测试 →";
        }
        else
        {
            NextButton.Content = "下一个 →";
        }
    }
    
    private void UpdateFavoriteButton(bool isFavorite)
    {
        FavoriteButton.Content = isFavorite ? "★" : "☆";
        FavoriteButton.Foreground = isFavorite 
            ? (System.Windows.Media.Brush)FindResource("WarningBrush") 
            : (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
    }
    
    private void Speak_Click(object sender, RoutedEventArgs e)
    {
        var word = _appState.CurrentWord;
        if (word != null && !string.IsNullOrEmpty(word.HeadWord))
        {
            _appState.Speak(word.HeadWord);
        }
    }
    
    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        var word = _appState.CurrentWord;
        if (word != null)
        {
            _appState.ToggleFavorite(word);
            UpdateFavoriteButton(word.IsFavorite);
        }
    }
    
    private void AddToWrongBook_Click(object sender, RoutedEventArgs e)
    {
        var word = _appState.CurrentWord;
        if (word != null)
        {
            try
            {
                _appState.RecordWrongAnswer(word);
                
                // 显示提示
                WrongButton.Content = "已添加 ✓";
                WrongButton.IsEnabled = false;
                WrongButton.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
                
                // 2秒后恢复
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    WrongButton.Content = "✗";
                    WrongButton.IsEnabled = true;
                    WrongButton.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void Next_Click(object sender, RoutedEventArgs e)
    {
        GoToNext();
    }
    
    private void GoToNext()
    {
        // 标记当前单词已学习
        var word = _appState.CurrentWord;
        if (word != null)
        {
            _appState.UpdateWordStatus(word.WordRank, 1);
        }
        
        if (_appState.CurrentIndex < _appState.WordList.Count - 1)
        {
            _appState.CurrentIndex++;
            UpdateUI();
        }
        else
        {
            // 进入选择题测试
            _appState.CurrentIndex = 0;
            _appState.CurrentPage = PageType.Choice;
        }
    }
    
    private void Page_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
            case Key.Enter:
            case Key.Right:
                GoToNext();
                e.Handled = true;
                break;
            case Key.S:
                Speak_Click(sender, e);
                e.Handled = true;
                break;
            case Key.F:
                Favorite_Click(sender, e);
                e.Handled = true;
                break;
        }
    }
}
