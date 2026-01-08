using System.IO;
using Microsoft.Data.Sqlite;
using MoyuWindows.Models;

namespace MoyuWindows.Services;

/// <summary>
/// 数据库服务 - 单例模式
/// </summary>
public class DatabaseService
{
    private static readonly Lazy<DatabaseService> _instance = new(() => new DatabaseService());
    public static DatabaseService Instance => _instance.Value;
    
    private readonly string _dbPath;
    private SqliteConnection? _connection;
    
    private DatabaseService()
    {
        // 数据库路径：应用数据目录
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Moyu"
        );
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "moyu.db");
    }
    
    public void Initialize()
    {
        // 如果数据库不存在，从嵌入资源或文件系统复制
        if (!File.Exists(_dbPath))
        {
            bool extracted = false;
            
            // 方法1: 尝试从嵌入资源提取
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "MoyuWindows.moyu.db";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var fileStream = File.Create(_dbPath);
                    stream.CopyTo(fileStream);
                    Console.WriteLine($"✅ 数据库已从嵌入资源提取到: {_dbPath}");
                    extracted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 从嵌入资源提取数据库失败: {ex.Message}");
            }
            
            // 方法2: 如果嵌入资源提取失败，尝试从文件系统复制
            if (!extracted)
            {
                var sourceDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "moyu.db");
                if (File.Exists(sourceDb))
                {
                    File.Copy(sourceDb, _dbPath);
                    Console.WriteLine($"✅ 数据库已从文件系统复制到: {_dbPath}");
                }
                else
                {
                    Console.WriteLine("⚠️ 未找到数据库文件，将创建空数据库");
                }
            }
        }
        else
        {
            Console.WriteLine($"✅ 使用现有数据库: {_dbPath}");
        }
        
        OpenDatabase();
        CreateNewTables();
    }
    
    private void OpenDatabase()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        Console.WriteLine($"✅ 数据库已打开: {_dbPath}");
    }
    
    private void CreateNewTables()
    {
        // 错词本表
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS WrongBook (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                wordRank INTEGER,
                bookName TEXT,
                headWord TEXT,
                tranCN TEXT,
                usphone TEXT,
                phrase TEXT,
                phraseCN TEXT,
                wrongCount INTEGER DEFAULT 1,
                lastWrongDate TEXT,
                UNIQUE(wordRank, bookName)
            )
        ");
        
        // 收藏表
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS Favorites (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                wordRank INTEGER,
                bookName TEXT,
                headWord TEXT,
                tranCN TEXT,
                usphone TEXT,
                phrase TEXT,
                phraseCN TEXT,
                addDate TEXT,
                UNIQUE(wordRank, bookName)
            )
        ");
        
        // 学习统计表
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS Statistics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                dateString TEXT UNIQUE,
                learnedCount INTEGER DEFAULT 0,
                correctCount INTEGER DEFAULT 0,
                wrongCount INTEGER DEFAULT 0,
                duration INTEGER DEFAULT 0
            )
        ");
        
        // 成就表
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS Achievements (
                id TEXT PRIMARY KEY,
                name TEXT,
                description TEXT,
                icon TEXT,
                isUnlocked INTEGER DEFAULT 0,
                unlockedDate TEXT
            )
        ");
        
        // 自定义词库表
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS CustomBooks (
                bookName TEXT PRIMARY KEY,
                displayName TEXT,
                total INTEGER DEFAULT 0,
                createdAt TEXT
            )
        ");
        
        InitializeAchievements();
        Console.WriteLine("✅ 新表已创建/更新");
    }
    
    private void InitializeAchievements()
    {
        var achievements = new[]
        {
            ("first_word", "初学乍练", "背完第一个单词", "🌱"),
            ("ten_words", "小有成就", "累计背完10个单词", "📖"),
            ("hundred_words", "百词斩", "累计背完100个单词", "🎯"),
            ("thousand_words", "千词王", "累计背完1000个单词", "👑"),
            ("streak_3", "三日连学", "连续学习3天", "🔥"),
            ("streak_7", "周学达人", "连续学习7天", "⭐"),
            ("streak_30", "月学大师", "连续学习30天", "🏆"),
            ("accuracy_90", "高准确率", "单日正确率超过90%", "🎖️")
        };
        
        foreach (var (id, name, desc, icon) in achievements)
        {
            ExecuteNonQuery($@"
                INSERT OR IGNORE INTO Achievements (id, name, description, icon, isUnlocked)
                VALUES ('{id}', '{name}', '{desc}', '{icon}', 0)
            ");
        }
    }
    
    #region 全局设置
    
    public (string book, int count) GetGlobalSettings()
    {
        var book = "CET4_1";
        var count = 20;
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT currentBookName, currentWordNumber FROM Global LIMIT 1";
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            book = reader.GetString(0);
            count = reader.GetInt32(1);
        }
        
        return (book, count);
    }
    
    public void UpdateCurrentBook(string book)
    {
        ExecuteNonQuery($"UPDATE Global SET currentBookName = '{book}'");
    }
    
    public void UpdateWordCount(int count)
    {
        ExecuteNonQuery($"UPDATE Global SET currentWordNumber = {count}");
    }
    
    #endregion
    
    #region 词书进度
    
    public (int current, int total) GetProgress(string book)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT current, number FROM Count WHERE bookName = '{book}'";
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return (reader.GetInt32(0), reader.GetInt32(1));
        }
        
        return (0, 0);
    }
    
    public List<BookProgress> GetAllBookProgress()
    {
        var result = new List<BookProgress>();
        var bookNames = new Dictionary<string, string>
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
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT bookName, current, number FROM Count";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var bookName = reader.GetString(0);
            result.Add(new BookProgress
            {
                BookName = bookName,
                DisplayName = bookNames.GetValueOrDefault(bookName, bookName),
                Current = reader.GetInt32(1),
                Total = reader.GetInt32(2)
            });
        }
        
        return result;
    }
    
    public void IncrementProgress(string book)
    {
        ExecuteNonQuery($"UPDATE Count SET current = current + 1 WHERE bookName = '{book}'");
    }
    
    public void ResetProgress(string book)
    {
        ExecuteNonQuery($"UPDATE Count SET current = 0 WHERE bookName = '{book}'");
        ExecuteNonQuery($"UPDATE {book} SET status = 0");
    }
    
    #endregion
    
    #region 单词操作
    
    public List<Word> GetRandomWords(int count, string book)
    {
        var words = new List<Word>();
        
        // 检查是否是日语五十音
        if (book == "Goin")
        {
            return GetRandomGoinWords(count);
        }
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $@"
            SELECT wordRank, headWord, tranCN, usphone, phrase, phraseCN, status 
            FROM {book} 
            WHERE status = 0 
            ORDER BY RANDOM() 
            LIMIT {count}
        ";
        
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                words.Add(new Word
                {
                    WordRank = reader.GetInt32(0),
                    HeadWord = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    TranCN = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Usphone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Phrase = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    PhraseCN = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Status = reader.GetInt32(6)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 获取单词失败: {ex.Message}");
        }
        
        return words;
    }
    
    private List<Word> GetRandomGoinWords(int count)
    {
        var words = new List<Word>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $@"
            SELECT wordRank, hiragana, katakana, romaji, status 
            FROM Goin 
            WHERE status = 0 
            ORDER BY RANDOM() 
            LIMIT {count}
        ";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new Word
            {
                WordRank = reader.GetInt32(0),
                HeadWord = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Hiragana = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Katakana = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Romaji = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Status = reader.GetInt32(4)
            });
        }
        
        return words;
    }
    
    public List<Word> GetRandomWordsForOptions(int count, string book, int excludeRank)
    {
        var words = new List<Word>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $@"
            SELECT wordRank, headWord, tranCN, usphone 
            FROM {book} 
            WHERE wordRank != {excludeRank}
            ORDER BY RANDOM() 
            LIMIT {count}
        ";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new Word
            {
                WordRank = reader.GetInt32(0),
                HeadWord = reader.IsDBNull(1) ? "" : reader.GetString(1),
                TranCN = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Usphone = reader.IsDBNull(3) ? "" : reader.GetString(3)
            });
        }
        
        return words;
    }
    
    public void UpdateWordStatus(int wordRank, int status, string book)
    {
        ExecuteNonQuery($"UPDATE {book} SET status = {status} WHERE wordRank = {wordRank}");
    }
    
    #endregion
    
    #region 错词本
    
    public void AddToWrongBook(Word word, string book)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecuteNonQuery($@"
            INSERT INTO WrongBook (wordRank, bookName, headWord, tranCN, usphone, phrase, phraseCN, wrongCount, lastWrongDate)
            VALUES ({word.WordRank}, '{book}', '{Escape(word.HeadWord)}', '{Escape(word.TranCN)}', 
                    '{Escape(word.Usphone)}', '{Escape(word.Phrase)}', '{Escape(word.PhraseCN)}', 1, '{now}')
            ON CONFLICT(wordRank, bookName) DO UPDATE SET 
                wrongCount = wrongCount + 1,
                lastWrongDate = '{now}'
        ");
    }
    
    public List<Word> GetWrongBookWords()
    {
        var words = new List<Word>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT wordRank, bookName, headWord, tranCN, usphone, phrase, phraseCN, wrongCount, lastWrongDate FROM WrongBook ORDER BY lastWrongDate DESC";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new Word
            {
                WordRank = reader.GetInt32(0),
                BookName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                HeadWord = reader.IsDBNull(2) ? "" : reader.GetString(2),
                TranCN = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Usphone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Phrase = reader.IsDBNull(5) ? "" : reader.GetString(5),
                PhraseCN = reader.IsDBNull(6) ? "" : reader.GetString(6),
                WrongCount = reader.GetInt32(7),
                LastWrongDate = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
            });
        }
        
        return words;
    }
    
    public void RemoveFromWrongBook(int wordRank, string bookName)
    {
        ExecuteNonQuery($"DELETE FROM WrongBook WHERE wordRank = {wordRank} AND bookName = '{bookName}'");
    }
    
    #endregion
    
    #region 收藏夹
    
    public void AddToFavorites(Word word, string book)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecuteNonQuery($@"
            INSERT OR IGNORE INTO Favorites (wordRank, bookName, headWord, tranCN, usphone, phrase, phraseCN, addDate)
            VALUES ({word.WordRank}, '{book}', '{Escape(word.HeadWord)}', '{Escape(word.TranCN)}', 
                    '{Escape(word.Usphone)}', '{Escape(word.Phrase)}', '{Escape(word.PhraseCN)}', '{now}')
        ");
    }
    
    public void RemoveFromFavorites(int wordRank, string bookName)
    {
        ExecuteNonQuery($"DELETE FROM Favorites WHERE wordRank = {wordRank} AND bookName = '{bookName}'");
    }
    
    public List<Word> GetFavoriteWords()
    {
        var words = new List<Word>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT wordRank, bookName, headWord, tranCN, usphone, phrase, phraseCN FROM Favorites ORDER BY addDate DESC";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new Word
            {
                WordRank = reader.GetInt32(0),
                BookName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                HeadWord = reader.IsDBNull(2) ? "" : reader.GetString(2),
                TranCN = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Usphone = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Phrase = reader.IsDBNull(5) ? "" : reader.GetString(5),
                PhraseCN = reader.IsDBNull(6) ? "" : reader.GetString(6),
                IsFavorite = true
            });
        }
        
        return words;
    }
    
    public bool IsFavorite(int wordRank, string bookName)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM Favorites WHERE wordRank = {wordRank} AND bookName = '{bookName}'";
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
    
    #endregion
    
    #region 统计
    
    public void RecordAnswer(bool isCorrect)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var column = isCorrect ? "correctCount" : "wrongCount";
        
        ExecuteNonQuery($@"
            INSERT INTO Statistics (dateString, learnedCount, {column})
            VALUES ('{today}', 1, 1)
            ON CONFLICT(dateString) DO UPDATE SET 
                learnedCount = learnedCount + 1,
                {column} = {column} + 1
        ");
    }
    
    public void AddLearningDuration(int seconds)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        ExecuteNonQuery($@"
            INSERT INTO Statistics (dateString, duration)
            VALUES ('{today}', {seconds})
            ON CONFLICT(dateString) DO UPDATE SET 
                duration = duration + {seconds}
        ");
    }
    
    public LearningStatistics GetStatistics()
    {
        var stats = new LearningStatistics();
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        
        // 今日统计
        using (var cmd = _connection!.CreateCommand())
        {
            cmd.CommandText = $"SELECT learnedCount, correctCount, wrongCount FROM Statistics WHERE dateString = '{today}'";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                stats.TodayLearned = reader.GetInt32(0);
                stats.TodayCorrect = reader.GetInt32(1);
                stats.TodayWrong = reader.GetInt32(2);
            }
        }
        
        // 累计统计
        using (var cmd = _connection!.CreateCommand())
        {
            cmd.CommandText = "SELECT SUM(learnedCount), COUNT(DISTINCT dateString) FROM Statistics";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                stats.TotalLearned = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                stats.TotalDays = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }
        }
        
        // 连续学习天数
        stats.StreakDays = CalculateStreakDays();
        
        return stats;
    }
    
    private int CalculateStreakDays()
    {
        var streak = 0;
        var checkDate = DateTime.Now.Date;
        
        while (true)
        {
            var dateStr = checkDate.ToString("yyyy-MM-dd");
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM Statistics WHERE dateString = '{dateStr}' AND learnedCount > 0";
            var result = cmd.ExecuteScalar();
            
            if (Convert.ToInt32(result) > 0)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }
        
        return streak;
    }
    
    public int GetTodayLearningDuration()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = $"SELECT duration FROM Statistics WHERE dateString = '{today}'";
        var result = cmd.ExecuteScalar();
        return result == null ? 0 : Convert.ToInt32(result);
    }
    
    public List<DailyRecord> GetLast7DaysRecords()
    {
        var records = new List<DailyRecord>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT dateString, learnedCount, correctCount, wrongCount, duration FROM Statistics ORDER BY dateString DESC LIMIT 7";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new DailyRecord
            {
                DateString = reader.GetString(0),
                LearnedCount = reader.GetInt32(1),
                CorrectCount = reader.GetInt32(2),
                WrongCount = reader.GetInt32(3),
                Duration = reader.GetInt32(4)
            });
        }
        
        return records;
    }
    
    #endregion
    
    #region 成就
    
    public List<Achievement> GetAchievements()
    {
        var achievements = new List<Achievement>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, name, description, icon, isUnlocked, unlockedDate FROM Achievements";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            achievements.Add(new Achievement
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                Icon = reader.GetString(3),
                IsUnlocked = reader.GetInt32(4) == 1,
                UnlockedDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5))
            });
        }
        
        return achievements;
    }
    
    public void UnlockAchievement(string id)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecuteNonQuery($"UPDATE Achievements SET isUnlocked = 1, unlockedDate = '{now}' WHERE id = '{id}'");
    }
    
    #endregion
    
    #region 自定义词库
    
    public void ImportCustomBook(string bookId, string displayName, List<WordImport> words)
    {
        // 创建自定义词库表
        ExecuteNonQuery($@"
            CREATE TABLE IF NOT EXISTS {bookId} (
                wordRank INTEGER PRIMARY KEY,
                headWord TEXT,
                tranCN TEXT,
                usphone TEXT,
                phrase TEXT,
                phraseCN TEXT,
                status INTEGER DEFAULT 0
            )
        ");
        
        // 插入单词
        int rank = 1;
        foreach (var word in words)
        {
            ExecuteNonQuery($@"
                INSERT OR REPLACE INTO {bookId} (wordRank, headWord, tranCN, usphone, phrase, phraseCN, status)
                VALUES ({rank}, '{Escape(word.HeadWord)}', '{Escape(word.TranCN)}', 
                        '{Escape(word.Usphone ?? "")}', '{Escape(word.Phrase ?? "")}', '{Escape(word.PhraseCN ?? "")}', 0)
            ");
            rank++;
        }
        
        // 添加到 Count 表
        ExecuteNonQuery($@"
            INSERT OR REPLACE INTO Count (bookName, current, number)
            VALUES ('{bookId}', 0, {words.Count})
        ");
        
        // 添加到 CustomBooks 表
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecuteNonQuery($@"
            INSERT OR REPLACE INTO CustomBooks (bookName, displayName, total, createdAt)
            VALUES ('{bookId}', '{Escape(displayName)}', {words.Count}, '{now}')
        ");
    }
    
    public List<(string id, string name, int current, int total)> GetCustomBooks()
    {
        var books = new List<(string, string, int, int)>();
        
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            SELECT cb.bookName, cb.displayName, COALESCE(c.current, 0), cb.total 
            FROM CustomBooks cb 
            LEFT JOIN Count c ON cb.bookName = c.bookName
        ";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            books.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3)
            ));
        }
        
        return books;
    }
    
    #endregion
    
    #region 辅助方法
    
    private void ExecuteNonQuery(string sql)
    {
        try
        {
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SQL执行失败: {ex.Message}\nSQL: {sql}");
        }
    }
    
    private static string Escape(string value)
    {
        return value?.Replace("'", "''") ?? "";
    }
    
    #endregion
}
