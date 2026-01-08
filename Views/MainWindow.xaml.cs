using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoyuWindows.Models;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class MainWindow : Window
{
    private readonly AppState _appState;
    private HotkeyService? _hotkeyService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _appState = AppState.Instance;
        DataContext = _appState;
        
        // 初始化系统托盘图标
        InitializeTrayIcon();
        
        // 初始化系统托盘菜单
        InitializeTrayMenu();
        
        // 导航到首页
        NavigateTo(PageType.Home);
        
        // 监听页面变化
        _appState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AppState.CurrentPage))
            {
                Dispatcher.Invoke(() => NavigateTo(_appState.CurrentPage));
            }
        };
        
        // 窗口加载后注册全局热键
        Loaded += MainWindow_Loaded;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 注册全局热键 Ctrl+Shift+M
        _hotkeyService = new HotkeyService(this);
        _hotkeyService.HotkeyPressed += (s, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                {
                    Hide();
                }
                else
                {
                    ShowAndActivate();
                }
            });
        };
        _hotkeyService.RegisterHotkey();
    }
    
    private void InitializeTrayIcon()
    {
        try
        {
            // 方法1: 尝试从嵌入资源加载图标
            try
            {
                var resourceUri = new Uri("pack://application:,,,/Resources/icon.ico");
                var streamInfo = Application.GetResourceStream(resourceUri);
                if (streamInfo != null)
                {
                    NotifyIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                    Console.WriteLine("成功从嵌入资源加载图标");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从嵌入资源加载图标失败: {ex.Message}");
            }
            
            // 方法2: 尝试从文件系统加载图标
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                NotifyIcon.Icon = new System.Drawing.Icon(iconPath);
                Console.WriteLine($"成功从文件系统加载图标: {iconPath}");
                return;
            }
            else
            {
                Console.WriteLine($"未找到图标文件: {iconPath}");
            }
            
            // 方法3: 使用系统默认图标
            NotifyIcon.Icon = SystemIcons.Application;
            Console.WriteLine("使用系统默认图标");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置托盘图标失败: {ex.Message}");
            // 出错时使用系统默认图标
            try
            {
                NotifyIcon.Icon = SystemIcons.Application;
            }
            catch { }
        }
    }
    
    private void InitializeTrayMenu()
    {
        try
        {
            // 初始化词库菜单
            var books = new[]
            {
                ("CET4_1", "四级核心词汇"),
                ("CET4_3", "四级完整词汇"),
                ("CET6_1", "六级核心词汇"),
                ("CET6_3", "六级完整词汇"),
                ("IELTS_3", "雅思词汇"),
                ("TOEFL_2", "托福词汇"),
                ("SAT_2", "SAT词汇"),
                ("GRE_3", "GRE词汇"),
                ("Goin", "五十音"),
                ("StdJp_Mid", "标日中级词汇")
            };
            
            foreach (var (id, name) in books)
            {
                var progress = DatabaseService.Instance.GetProgress(id);
                var item = new MenuItem
                {
                    Header = $"{name} ({progress.current}/{progress.total})",
                    Tag = id,
                    IsChecked = id == _appState.CurrentBook
                };
                item.Click += BookItem_Click;
                BookMenu.Items.Add(item);
            }
            
            // 初始化数量菜单
            var counts = new[] { 5, 10, 15, 20, 25, 30, 40, 50 };
            foreach (var count in counts)
            {
                var item = new MenuItem
                {
                    Header = $"{count} 个",
                    Tag = count,
                    IsChecked = count == _appState.DefaultWordCount
                };
                item.Click += CountItem_Click;
                CountMenu.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化托盘菜单失败: {ex.Message}");
        }
    }
    
    private void NavigateTo(PageType page)
    {
        try
        {
            Page? targetPage = page switch
            {
                PageType.Home => new HomePage(),
                PageType.Remember => new RememberPage(),
                PageType.Choice => new ChoicePage(),
                PageType.Spelling => new SpellingPage(),
                PageType.Congratulate => new CongratulatePage(),
                PageType.WrongBook => new WrongBookPage(),
                PageType.Favorites => new FavoritesPage(),
                PageType.Statistics => new StatisticsPage(),
                PageType.Settings => new SettingsPage(),
                _ => new HomePage()
            };
            
            MainFrame.Navigate(targetPage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"导航失败: {ex.Message}");
            MessageBox.Show($"页面加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #region 标题栏事件
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 双击标题栏不做任何操作（保持窗口大小）
        }
        else
        {
            DragMove();
        }
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
    
    #endregion
    
    #region 系统托盘事件
    
    private void NotifyIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }
    
    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }
    
    private void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
    
    private void BookItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is string bookId)
        {
            _appState.CurrentBook = bookId;
            _appState.SaveSettings();
            
            // 更新菜单选中状态
            foreach (var menuItem in BookMenu.Items.OfType<MenuItem>())
            {
                menuItem.IsChecked = menuItem.Tag as string == bookId;
            }
        }
    }
    
    private void CountItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is int count)
        {
            _appState.DefaultWordCount = count;
            _appState.SaveSettings();
            
            // 更新菜单选中状态
            foreach (var menuItem in CountMenu.Items.OfType<MenuItem>())
            {
                menuItem.IsChecked = menuItem.Tag is int c && c == count;
            }
        }
    }
    
    private void Favorites_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.Favorites;
        ShowAndActivate();
    }
    
    private void WrongBook_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.WrongBook;
        ShowAndActivate();
    }
    
    private void Statistics_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.Statistics;
        ShowAndActivate();
    }
    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.Settings;
        ShowAndActivate();
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _appState.EndLearning();
        _hotkeyService?.Dispose();
        NotifyIcon?.Dispose();
        Application.Current.Shutdown();
    }
    
    #endregion
    
    #region 快捷键
    
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // ESC 关闭窗口
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }
    
    #endregion
    
    #region 窗口状态
    
    private void Window_Deactivated(object sender, EventArgs e)
    {
        // 窗口失去焦点时可以选择隐藏（更"摸鱼"）
        // Hide();
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 阻止真正关闭，改为隐藏
        e.Cancel = true;
        Hide();
    }
    
    #endregion
}
