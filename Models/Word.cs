namespace MoyuWindows.Models;

/// <summary>
/// 单词模型
/// </summary>
public class Word
{
    public int WordRank { get; set; }
    public string HeadWord { get; set; } = string.Empty;
    public string TranCN { get; set; } = string.Empty;
    public string Usphone { get; set; } = string.Empty;
    public string Phrase { get; set; } = string.Empty;
    public string PhraseCN { get; set; } = string.Empty;
    public int Status { get; set; } // 0: 未背过, 1: 已背过
    
    // 日语五十音专用
    public string? Hiragana { get; set; }
    public string? Katakana { get; set; }
    public string? Romaji { get; set; }
    
    // 收藏和错词状态
    public bool IsFavorite { get; set; }
    public int WrongCount { get; set; }
    public DateTime? LastWrongDate { get; set; }
    public string? BookName { get; set; }
    
    /// <summary>
    /// 是否为日语五十音
    /// </summary>
    public bool IsGoin => !string.IsNullOrEmpty(Hiragana);
}

/// <summary>
/// 导入单词 DTO
/// </summary>
public class WordImport
{
    public string HeadWord { get; set; } = string.Empty;
    public string TranCN { get; set; } = string.Empty;
    public string Usphone { get; set; } = string.Empty;
    public string Phrase { get; set; } = string.Empty;
    public string PhraseCN { get; set; } = string.Empty;
}

/// <summary>
/// 词书进度模型
/// </summary>
public class BookProgress
{
    public string BookName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    
    public double ProgressPercent => Total > 0 ? (double)Current / Total * 100 : 0;
}

/// <summary>
/// 学习统计模型
/// </summary>
public class LearningStatistics
{
    public int TodayLearned { get; set; }
    public int TodayCorrect { get; set; }
    public int TodayWrong { get; set; }
    public int TotalLearned { get; set; }
    public int TotalDays { get; set; }
    public int StreakDays { get; set; }
    public DateTime? LastLearnDate { get; set; }
    
    public double TodayAccuracy
    {
        get
        {
            var total = TodayCorrect + TodayWrong;
            return total > 0 ? (double)TodayCorrect / total * 100 : 0;
        }
    }
}

/// <summary>
/// 每日记录模型
/// </summary>
public class DailyRecord
{
    public string DateString { get; set; } = string.Empty;
    public int LearnedCount { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public int Duration { get; set; } // 学习时长（秒）
}

/// <summary>
/// 成就模型
/// </summary>
public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
}

/// <summary>
/// 应用主题枚举
/// </summary>
public enum AppTheme
{
    System,
    Light,
    Dark
}

/// <summary>
/// 测试模式枚举
/// </summary>
public enum QuizMode
{
    CnToEn,     // 看中文选英文
    EnToCn,     // 看英文选中文
    Spelling    // 拼写模式
}

/// <summary>
/// 页面枚举
/// </summary>
public enum PageType
{
    Home,
    Remember,
    Choice,
    Spelling,
    Congratulate,
    WrongBook,
    Favorites,
    Statistics,
    Settings
}

/// <summary>
/// 用户设置（用于 JSON 序列化）
/// </summary>
public class UserSettings
{
    public string AppTheme { get; set; } = "System";
    public string QuizMode { get; set; } = "CnToEn";
    public bool ReminderEnabled { get; set; }
    public string ReminderTime { get; set; } = "09:00";
}
