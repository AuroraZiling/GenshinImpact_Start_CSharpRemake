using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Color = System.Drawing.Color;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Point = System.Drawing.Point;

namespace GenshinImpact_Start_CSharpRemake;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public bool GameRunning;
    public double WhitePercentage;

    public string? RunningPath;
    public int? TargetGame;
    public double? DetectPercent;


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public MainWindow()
    {
        InitializeComponent();
        if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "config.json")))
        {
            ExtractEmbeddedResource("GenshinImpact_Start_CSharpRemake.config.json", Environment.CurrentDirectory, "config.json");
        }

        try
        {
            using var file = File.OpenText(Path.Combine(Environment.CurrentDirectory, "config.json"));
            using var reader = new JsonTextReader(file);
            var o = (JObject)JToken.ReadFrom(reader);
            if (o["path"] != null && o["mode"] != null && o["percent"] != null)
            {
                RunningPath = o["path"]?.ToString();
                TargetGame = o["mode"]?.Value<int>();
                DetectPercent = o["percent"]?.Value<double>();
            }

            if (!File.Exists(RunningPath))
            {
                MessageBox.Show("非法的执行路径, 请在config.json手动设置");
                Environment.Exit(0);
            }

            Title = TargetGame switch
            {
                0 => "Genshin Impact Start - CSharp Remake",
                1 => "Star Rail Start - CSharp Remake",
                _ => "Genshin Impact Start - CSharp Remake"
            };

            if (DetectPercent is < 85 or > 100)
            {
                MessageBox.Show("占比不合理，有效范围为85-100，请检查config.json");
                Environment.Exit(0);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("检查时发现错误\n"+e.Message);
            Environment.Exit(0);
        }
        Task.Run(WaitForStart);
    }

    private static void ExtractEmbeddedResource(string resourceName, string directoryPath, string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        var filePath = Path.Combine(directoryPath, Path.GetFileName(fileName));
        using var fileStream = File.Create(filePath);
        resourceStream?.CopyTo(fileStream);
    }

    public void ScanGameRunning()
    {
        var targetProcess = TargetGame switch
        {
            0 => "YuanShen",
            1 => "StarRail",
            _ => "YuanShen"
        };
        GameRunning = Process.GetProcessesByName(targetProcess).Length == 1;
        Application.Current.Dispatcher.Invoke(() =>
        {
            GameProcessStatus.Text = GameRunning ? "运行中" : "未运行";
        });
    }

    public void ScanScreenColor()
    {
        if (Screen.PrimaryScreen == null) return;
        var screenBounds = Screen.PrimaryScreen.Bounds;
        var screenCapture = new Bitmap(screenBounds.Width, screenBounds.Height);
        using var g = Graphics.FromImage(screenCapture);
        g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
        WhitePercentage = CalculateColorPercentage(screenCapture);
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScreenWhitePercentage.Text = WhitePercentage.ToString("F2") + "%";
        });
    }

    private double CalculateColorPercentage(Bitmap image)
    {
        var whitePixelCount = 0;
        var totalPixelCount = image.Width * image.Height;

        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                var pixelColor = image.GetPixel(x, y);
                if (IsWhite(pixelColor))
                {
                    whitePixelCount++;
                }
            }
        }

        return (double)whitePixelCount / totalPixelCount * 100.0;
    }

    private bool IsWhite(Color color)
    {
        if (TargetGame != 1)
        {
            return color is { R: > 250, G: > 250, B: > 250 };
        }
        else
        {
            return color is { R: < 20, G: < 20, B: < 20 };
        }
    }

    public void WaitForStart()
    {
        while (true)
        {
            ScanGameRunning();
            ScanScreenColor();
            if (GameRunning || !(WhitePercentage >= DetectPercent)) continue;
            var info = new ProcessStartInfo
            {
                FileName = RunningPath
            };
            var process = new Process();
            process.StartInfo = info;
            if (process.Start())
            {
                Thread.Sleep(5000);
                SetForegroundWindow(process.MainWindowHandle);
                Environment.Exit(0);
            }
            Task.Delay(100);
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        Environment.Exit(0);
    }
}