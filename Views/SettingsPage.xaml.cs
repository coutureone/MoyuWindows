using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MoyuWindows.Models;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class SettingsPage : Page
{
    private readonly AppState _appState;
    private bool _isInitializing = true;
    
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
    
    public SettingsPage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        LoadSettings();
        _isInitializing = false;
    }
    
    private void LoadSettings()
    {
        // 主题
        var themeIndex = _appState.AppTheme switch
        {
            AppTheme.System => 0,
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };
        ThemeComboBox.SelectedIndex = themeIndex;
        
        // 测试模式
        var modeIndex = _appState.QuizMode switch
        {
            QuizMode.CnToEn => 0,
            QuizMode.EnToCn => 1,
            QuizMode.Spelling => 2,
            _ => 0
        };
        QuizModeComboBox.SelectedIndex = modeIndex;
        
        // 默认数量
        var countIndex = _appState.DefaultWordCount switch
        {
            5 => 0,
            10 => 1,
            15 => 2,
            20 => 3,
            25 => 4,
            30 => 5,
            40 => 6,
            50 => 7,
            _ => 3
        };
        WordCountComboBox.SelectedIndex = countIndex;
        
        // 当前词书
        CurrentBookText.Text = BookNames.GetValueOrDefault(_appState.CurrentBook, _appState.CurrentBook);
        
        // 进度
        var progress = DatabaseService.Instance.GetProgress(_appState.CurrentBook);
        ProgressText.Text = $"{progress.current}/{progress.total}";
        
        // 开机启动状态
        StartupCheckBox.IsChecked = StartupService.IsStartupEnabled();
    }
    
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _appState.SaveSettings();
        _appState.CurrentPage = PageType.Home;
    }
    
    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        _appState.AppTheme = ThemeComboBox.SelectedIndex switch
        {
            0 => AppTheme.System,
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System
        };
        _appState.SaveSettings();
        
        // 提示用户重启应用以应用主题
        var result = MessageBox.Show(
            "主题设置已保存。\n\n需要重启应用才能完全应用新主题，是否立即重启？",
            "主题切换",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );
        
        if (result == MessageBoxResult.Yes)
        {
            // 重启应用
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
                Application.Current.Shutdown();
            }
        }
    }
    
    private void QuizModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        _appState.QuizMode = QuizModeComboBox.SelectedIndex switch
        {
            0 => QuizMode.CnToEn,
            1 => QuizMode.EnToCn,
            2 => QuizMode.Spelling,
            _ => QuizMode.CnToEn
        };
    }
    
    private void WordCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        
        var counts = new[] { 5, 10, 15, 20, 25, 30, 40, 50 };
        if (WordCountComboBox.SelectedIndex >= 0 && WordCountComboBox.SelectedIndex < counts.Length)
        {
            _appState.DefaultWordCount = counts[WordCountComboBox.SelectedIndex];
        }
    }
    
    private void ResetProgress_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"确定要重置「{BookNames.GetValueOrDefault(_appState.CurrentBook, _appState.CurrentBook)}」的学习进度吗？\n\n此操作不可恢复！",
            "确认重置",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        
        if (result == MessageBoxResult.Yes)
        {
            _appState.ResetBookProgress(_appState.CurrentBook);
            
            // 刷新显示
            var progress = DatabaseService.Instance.GetProgress(_appState.CurrentBook);
            ProgressText.Text = $"{progress.current}/{progress.total}";
            
            MessageBox.Show("进度已重置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void ImportBook_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择自定义词库文件",
            Filter = "词库文件 (*.csv;*.json)|*.csv;*.json|CSV 文件 (*.csv)|*.csv|JSON 文件 (*.json)|*.json",
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var filePath = dialog.FileName;
                var ext = Path.GetExtension(filePath).ToLower();
                var bookName = Path.GetFileNameWithoutExtension(filePath);
                
                List<WordImport> words;
                
                if (ext == ".json")
                {
                    words = ImportJsonWordList(filePath);
                }
                else if (ext == ".csv")
                {
                    words = ImportCsvWordList(filePath);
                }
                else
                {
                    MessageBox.Show("不支持的文件格式", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (words.Count == 0)
                {
                    MessageBox.Show("文件中没有找到有效的单词数据", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // 保存到数据库
                var bookId = $"Custom_{DateTime.Now:yyyyMMddHHmmss}";
                DatabaseService.Instance.ImportCustomBook(bookId, bookName, words);
                
                MessageBox.Show($"导入成功！\n\n词库名称: {bookName}\n单词数量: {words.Count}", 
                    "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private List<WordImport> ImportJsonWordList(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var words = new List<WordImport>();
        
        try
        {
            // 尝试解析为数组
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var word = new WordImport
                    {
                        HeadWord = item.TryGetProperty("headWord", out var hw) ? hw.GetString() ?? "" :
                                   item.TryGetProperty("word", out var w) ? w.GetString() ?? "" : "",
                        TranCN = item.TryGetProperty("tranCN", out var tc) ? tc.GetString() ?? "" :
                                 item.TryGetProperty("translation", out var t) ? t.GetString() ?? "" :
                                 item.TryGetProperty("meaning", out var m) ? m.GetString() ?? "" : "",
                        Usphone = item.TryGetProperty("usphone", out var up) ? up.GetString() ?? "" : "",
                        Phrase = item.TryGetProperty("phrase", out var p) ? p.GetString() ?? "" :
                                 item.TryGetProperty("example", out var ex) ? ex.GetString() ?? "" : "",
                        PhraseCN = item.TryGetProperty("phraseCN", out var pc) ? pc.GetString() ?? "" :
                                   item.TryGetProperty("exampleCN", out var ec) ? ec.GetString() ?? "" : ""
                    };
                    
                    if (!string.IsNullOrEmpty(word.HeadWord))
                    {
                        words.Add(word);
                    }
                }
            }
        }
        catch
        {
            // 忽略解析错误
        }
        
        return words;
    }
    
    private List<WordImport> ImportCsvWordList(string filePath)
    {
        var words = new List<WordImport>();
        var lines = File.ReadAllLines(filePath);
        
        foreach (var line in lines.Skip(1)) // 跳过标题行
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var parts = line.Split(',');
            if (parts.Length >= 2)
            {
                var word = new WordImport
                {
                    HeadWord = parts[0].Trim().Trim('"'),
                    TranCN = parts[1].Trim().Trim('"'),
                    Usphone = parts.Length > 2 ? parts[2].Trim().Trim('"') : "",
                    Phrase = parts.Length > 3 ? parts[3].Trim().Trim('"') : "",
                    PhraseCN = parts.Length > 4 ? parts[4].Trim().Trim('"') : ""
                };
                
                if (!string.IsNullOrEmpty(word.HeadWord))
                {
                    words.Add(word);
                }
            }
        }
        
        return words;
    }
    
    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "导出学习数据",
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = $"moyu_export_{DateTime.Now:yyyyMMdd}.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var exportData = new
                {
                    ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Statistics = _appState.Statistics,
                    CurrentBook = _appState.CurrentBook,
                    Settings = new
                    {
                        Theme = _appState.AppTheme.ToString(),
                        QuizMode = _appState.QuizMode.ToString(),
                        DefaultWordCount = _appState.DefaultWordCount
                    },
                    Favorites = DatabaseService.Instance.GetFavoriteWords(),
                    WrongBook = DatabaseService.Instance.GetWrongBookWords(),
                    DailyRecords = DatabaseService.Instance.GetLast7DaysRecords()
                };
                
                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                File.WriteAllText(dialog.FileName, json);
                
                MessageBox.Show($"导出成功！\n\n保存路径: {dialog.FileName}", "导出成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        
        var isEnabled = StartupCheckBox.IsChecked == true;
        var success = StartupService.SetStartup(isEnabled);
        
        if (!success)
        {
            // 恢复原状态
            _isInitializing = true;
            StartupCheckBox.IsChecked = !isEnabled;
            _isInitializing = false;
            
            MessageBox.Show("设置开机启动失败，请以管理员身份运行程序后重试", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
