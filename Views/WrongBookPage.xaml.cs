using System.Windows;
using System.Windows.Controls;
using MoyuWindows.Models;
using MoyuWindows.Services;
using MoyuWindows.ViewModels;

namespace MoyuWindows.Views;

public partial class WrongBookPage : Page
{
    private readonly AppState _appState;
    private List<Word> _words = new();
    
    public WrongBookPage()
    {
        InitializeComponent();
        _appState = AppState.Instance;
    }
    
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        LoadWords();
    }
    
    private void LoadWords()
    {
        _words = DatabaseService.Instance.GetWrongBookWords();
        WordList.ItemsSource = _words;
        
        CountText.Text = $"{_words.Count} 个";
        
        if (_words.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            WordList.Visibility = Visibility.Collapsed;
            PracticeButton.IsEnabled = false;
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            WordList.Visibility = Visibility.Visible;
            PracticeButton.IsEnabled = true;
        }
    }
    
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        _appState.CurrentPage = PageType.Home;
    }
    
    private void RemoveWord_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Word word)
        {
            DatabaseService.Instance.RemoveFromWrongBook(word.WordRank, word.BookName ?? "");
            LoadWords();
        }
    }
    
    private void Practice_Click(object sender, RoutedEventArgs e)
    {
        if (_words.Count == 0) return;
        
        // 使用错词本的单词练习
        _appState.WordList = _words.Take(Math.Min(_words.Count, _appState.DefaultWordCount)).ToList();
        _appState.CurrentIndex = 0;
        _appState.StartLearning();
        _appState.CurrentPage = PageType.Remember;
    }
}
