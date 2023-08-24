using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GenshinImpact_Start_CSharpRemake;

/// <summary>
/// BlankWindow.xaml 的交互逻辑
/// </summary>
public partial class BlankWindow
{
    private int _transitionDuration;
    public BlankWindow(int transitionDuration = 7, int windowColor = 1)
    {
        InitializeComponent();
        _transitionDuration = transitionDuration;
        BlankGrid.Background = windowColor switch
        {
            1 => Brushes.White,
            0 => Brushes.Black,
            _ => BlankGrid.Background
        };
        Loaded += BlankWindow_Loaded;
    }

    private async void BlankWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(TimeSpan.FromSeconds(_transitionDuration));
        Close(); 
    }
}