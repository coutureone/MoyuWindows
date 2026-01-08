using System.Windows;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 初始化数据库服务
        DatabaseService.Instance.Initialize();
        
        // 应用主题
        AppState.Instance.ApplyTheme();
    }
}
