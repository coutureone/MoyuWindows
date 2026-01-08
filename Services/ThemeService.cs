using System.Windows;
using MoyuWindows.Models;

namespace MoyuWindows.Services;

/// <summary>
/// 主题服务 - 处理主题切换
/// </summary>
public static class ThemeService
{
    /// <summary>
    /// 应用主题（在应用启动时调用）
    /// </summary>
    public static void ApplyTheme(AppTheme theme)
    {
        var actualTheme = theme;
        
        // 如果是跟随系统，检测系统主题
        if (theme == AppTheme.System)
        {
            actualTheme = IsSystemDarkTheme() ? AppTheme.Dark : AppTheme.Light;
        }
        
        // 切换颜色资源字典
        var app = Application.Current;
        if (app == null) return;
        
        try
        {
            // 确定要使用的主题文件
            var themeFile = actualTheme == AppTheme.Dark ? "Colors.xaml" : "ColorsLight.xaml";
            var themeUri = new Uri($"Styles/{themeFile}", UriKind.Relative);
            
            // 查找并替换颜色资源字典
            for (int i = 0; i < app.Resources.MergedDictionaries.Count; i++)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null && 
                    (dict.Source.OriginalString.Contains("Colors.xaml") || 
                     dict.Source.OriginalString.Contains("ColorsLight.xaml")))
                {
                    // 创建新的资源字典
                    var newDict = new ResourceDictionary { Source = themeUri };
                    app.Resources.MergedDictionaries[i] = newDict;
                    Console.WriteLine($"✅ 主题已切换到: {themeFile}");
                    return;
                }
            }
            
            // 如果没找到，直接添加
            var colorDict = new ResourceDictionary { Source = themeUri };
            app.Resources.MergedDictionaries.Insert(0, colorDict);
            Console.WriteLine($"✅ 主题已添加: {themeFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 切换主题失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检测系统是否为深色主题
    /// </summary>
    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = Dark, 1 = Light
                }
            }
        }
        catch
        {
            // 忽略错误，默认深色
        }
        
        return true; // 默认深色主题
    }
}
