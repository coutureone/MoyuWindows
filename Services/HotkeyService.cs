using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MoyuWindows.Services;

/// <summary>
/// 全局热键服务
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    private readonly Window _window;
    private readonly int _hotkeyId = 9000;
    private IntPtr _windowHandle;
    private HwndSource? _source;
    
    public event EventHandler? HotkeyPressed;
    
    public HotkeyService(Window window)
    {
        _window = window;
    }
    
    /// <summary>
    /// 注册全局热键 Ctrl+Shift+M
    /// </summary>
    public bool RegisterHotkey()
    {
        try
        {
            _windowHandle = new WindowInteropHelper(_window).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);
            
            // VK_M = 0x4D
            var result = RegisterHotKey(_windowHandle, _hotkeyId, MOD_CONTROL | MOD_SHIFT, 0x4D);
            
            if (result)
            {
                Console.WriteLine("✅ 全局热键 Ctrl+Shift+M 注册成功");
            }
            else
            {
                Console.WriteLine("⚠️ 全局热键注册失败，可能已被其他程序占用");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 注册热键失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 注销热键
    /// </summary>
    public void UnregisterHotkey()
    {
        try
        {
            UnregisterHotKey(_windowHandle, _hotkeyId);
            _source?.RemoveHook(HwndHook);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"注销热键失败: {ex.Message}");
        }
    }
    
    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }
    
    public void Dispose()
    {
        UnregisterHotkey();
    }
}

/// <summary>
/// 开机启动服务
/// </summary>
public static class StartupService
{
    private const string AppName = "Moyu";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    
    /// <summary>
    /// 检测是否已设置开机启动
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 设置开机启动
    /// </summary>
    public static bool SetStartup(bool enable)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null) return false;
            
            if (enable)
            {
                var exePath = Environment.ProcessPath ?? "";
                key.SetValue(AppName, $"\"{exePath}\"");
                Console.WriteLine("✅ 已设置开机启动");
            }
            else
            {
                key.DeleteValue(AppName, false);
                Console.WriteLine("✅ 已取消开机启动");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 设置开机启动失败: {ex.Message}");
            return false;
        }
    }
}
