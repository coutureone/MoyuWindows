using System.IO;
using System.Speech.Synthesis;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MoyuWindows.Models;
using MoyuWindows.Services;

namespace MoyuWindows.ViewModels;

/// <summary>
/// 应用全局状态 - 单例模式
/// </summary>
public partial class AppState : ObservableObject
{
    private static readonly Lazy<AppState> _instance = new(() => new AppState());
    public static AppState Instance => _instance.Value;
    
    private DateTime? _learningStartTime;
    private readonly SpeechSynthesizer _speechSynthesizer;
    private readonly string _settingsPath;
    
    #region 页面状态
    
    [ObservableProperty]
    private PageType _currentPage = PageType.Home;
    
    [ObservableProperty]
    private string _currentBook = "CET4_1";
    
    [ObservableProperty]
    private int _defaultWordCount = 20;
    
    [ObservableProperty]
    private List<Word> _wordList = new();
    
    [ObservableProperty]
    private int _currentIndex;
    
    #endregion
    
    #region 主题设置
    
    [ObservableProperty]
    private AppTheme _appTheme = AppTheme.System;
    
    #endregion
    
    #region 测试模式
    
    [ObservableProperty]
    private QuizMode _quizMode = QuizMode.CnToEn;
    
    #endregion
    
    #region 学习统计
    
    [ObservableProperty]
    private LearningStatistics _statistics = new();
    
    [ObservableProperty]
    private int _todayLearningDuration;
    
    #endregion
    
    #region 计算属性
    
    public Word? CurrentWord => CurrentIndex < WordList.Count ? WordList[CurrentIndex] : null;
    
    public double Progress => WordList.Count > 0 ? (double)CurrentIndex / WordList.Count : 0;
    
    public string ProgressText => $"{CurrentIndex}/{WordList.Count}";
    
    public (int current, int total) CurrentBookProgress => DatabaseService.Instance.GetProgress(CurrentBook);
    
    #endregion
    
    private AppState()
    {
        _speechSynthesizer = new SpeechSynthesizer();
        
        // 设置文件路径
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Moyu"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        
        LoadSettings();
        LoadStatistics();
    }
    
    #region 设置管理
    
    public void LoadSettings()
    {
        var (book, count) = DatabaseService.Instance.GetGlobalSettings();
        CurrentBook = book;
        DefaultWordCount = count > 0 ? count : 20;
        
        // 从本地文件加载设置
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings != null)
                {
                    if (Enum.TryParse<AppTheme>(settings.AppTheme, out var theme))
                    {
                        AppTheme = theme;
                    }
                    if (Enum.TryParse<QuizMode>(settings.QuizMode, out var mode))
                    {
                        QuizMode = mode;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载设置失败: {ex.Message}");
        }
    }
    
    public void SaveSettings()
    {
        DatabaseService.Instance.UpdateCurrentBook(CurrentBook);
        DatabaseService.Instance.UpdateWordCount(DefaultWordCount);
        
        try
        {
            var settings = new UserSettings
            {
                AppTheme = AppTheme.ToString(),
                QuizMode = QuizMode.ToString()
            };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存设置失败: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 主题
    
    public void ApplyTheme()
    {
        ThemeService.ApplyTheme(AppTheme);
        OnPropertyChanged(nameof(AppTheme));
    }
    
    #endregion
    
    #region 单词列表操作
    
    public void CreateWordList(int count)
    {
        WordList = DatabaseService.Instance.GetRandomWords(count, CurrentBook);
        CurrentIndex = 0;
        StartLearning();
        OnPropertyChanged(nameof(CurrentWord));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressText));
    }
    
    public void NextWord()
    {
        if (CurrentIndex < WordList.Count - 1)
        {
            CurrentIndex++;
            OnPropertyChanged(nameof(CurrentWord));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }
        else
        {
            // 完成学习
            CurrentPage = PageType.Congratulate;
        }
    }
    
    public void UpdateWordStatus(int wordRank, int status)
    {
        DatabaseService.Instance.UpdateWordStatus(wordRank, status, CurrentBook);
        
        var index = WordList.FindIndex(w => w.WordRank == wordRank);
        if (index >= 0)
        {
            WordList[index].Status = status;
        }
    }
    
    public void IncrementProgress()
    {
        DatabaseService.Instance.IncrementProgress(CurrentBook);
    }
    
    #endregion
    
    #region 学习计时
    
    public void StartLearning()
    {
        _learningStartTime = DateTime.Now;
    }
    
    public void EndLearning()
    {
        if (_learningStartTime.HasValue)
        {
            var duration = (int)(DateTime.Now - _learningStartTime.Value).TotalSeconds;
            DatabaseService.Instance.AddLearningDuration(duration);
            TodayLearningDuration += duration;
            _learningStartTime = null;
        }
    }
    
    #endregion
    
    #region 统计
    
    public void LoadStatistics()
    {
        Statistics = DatabaseService.Instance.GetStatistics();
        TodayLearningDuration = DatabaseService.Instance.GetTodayLearningDuration();
    }
    
    public void RecordCorrectAnswer()
    {
        DatabaseService.Instance.RecordAnswer(true);
        Statistics.TodayCorrect++;
        Statistics.TodayLearned++;
        OnPropertyChanged(nameof(Statistics));
    }
    
    public void RecordWrongAnswer(Word word)
    {
        DatabaseService.Instance.RecordAnswer(false);
        DatabaseService.Instance.AddToWrongBook(word, CurrentBook);
        Statistics.TodayWrong++;
        OnPropertyChanged(nameof(Statistics));
    }
    
    #endregion
    
    #region 收藏
    
    public void ToggleFavorite(Word word)
    {
        if (word.IsFavorite)
        {
            DatabaseService.Instance.RemoveFromFavorites(word.WordRank, CurrentBook);
        }
        else
        {
            DatabaseService.Instance.AddToFavorites(word, CurrentBook);
        }
        
        var index = WordList.FindIndex(w => w.WordRank == word.WordRank);
        if (index >= 0)
        {
            WordList[index].IsFavorite = !WordList[index].IsFavorite;
        }
        OnPropertyChanged(nameof(CurrentWord));
    }
    
    public bool IsFavorite(Word word)
    {
        return DatabaseService.Instance.IsFavorite(word.WordRank, CurrentBook);
    }
    
    #endregion
    
    #region 发音
    
    public void Speak(string text)
    {
        try
        {
            _speechSynthesizer.SpeakAsyncCancelAll();
            _speechSynthesizer.SpeakAsync(text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 发音失败: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 重置
    
    public void Reset()
    {
        EndLearning();
        CurrentPage = PageType.Home;
        CurrentIndex = 0;
        WordList = new List<Word>();
        LoadStatistics();
    }
    
    public void ResetBookProgress(string book)
    {
        DatabaseService.Instance.ResetProgress(book);
    }
    
    #endregion
}
