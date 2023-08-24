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

    public string ExecutablePath;
    public string ProcessName;
    public int TransitionDuration;
    public int DetectColor;
    public double DetectPercent;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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
            if (o["executablePath"] != null && o["processName"] != null && o["transitionDuration"] != null && o["detectColor"] != null && o["colorPercent"] != null)
            {
                ExecutablePath = o["executablePath"]?.ToString();
                ProcessName = o["processName"]?.ToString();
                TransitionDuration = (int)o["transitionDuration"]?.Value<int>();
                DetectColor = (int)o["detectColor"]?.Value<int>();
                DetectPercent = (double)o["colorPercent"]?.Value<double>();
            }

            if (!File.Exists(ExecutablePath))
            {
                MessageBox.Show("非法的执行路径, 请在config.json手动设置");
                Environment.Exit(0);
            }

            ScreenColorPercentageLabel.Text = DetectColor switch
            {
                0 => "屏幕黑色占比: ",
                1 => "屏幕白色占比: ",
                _ => "屏幕白色占比: "
            };

            if (TransitionDuration is < 3 or > 10)
            {
                MessageBox.Show("TransitionDuration 参数不合理，有效范围为3-10，请检查config.json");
                Environment.Exit(0);
            }

            if (DetectPercent is < 85 or > 100)
            {
                MessageBox.Show("DetectPercent 参数不合理，有效范围为85-100，请检查config.json");
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
        GameRunning = Process.GetProcessesByName(ProcessName).Length == 1;
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
            ScreenColorPercentage.Text = WhitePercentage.ToString("F2") + "%";
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
                if (IsTargetColor(pixelColor))
                {
                    whitePixelCount++;
                }
            }
        }

        return (double)whitePixelCount / totalPixelCount * 100.0;
    }

    private bool IsTargetColor(Color color)
    {
        if (DetectColor != 0)
        {
            return color is { R: > 250, G: > 250, B: > 250 };
        }
        return color is { R: < 20, G: < 20, B: < 20 };
    }

    public void WaitForStart()
    {
        while (true)
        {
            ScanGameRunning();
            ScanScreenColor();
            if (GameRunning || !(WhitePercentage >= DetectPercent)) continue;
            var newThread = new Thread(() =>
            {
                var blankWindow = new BlankWindow(TransitionDuration, DetectColor);
                blankWindow.Dispatcher.Invoke(() =>
                {
                    blankWindow.Show();
                    blankWindow.Focus();
                    blankWindow.BlankGrid.Focus();
                });
                
                System.Windows.Threading.Dispatcher.Run();
            });
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();

            var info = new ProcessStartInfo
            {
                FileName = ExecutablePath
            };
            var process = new Process();
            process.StartInfo = info;
            if (process.Start())
            {
                Thread.Sleep(TransitionDuration * 1000);
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