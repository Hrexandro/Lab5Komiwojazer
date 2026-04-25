using Shared.Communication;
using Shared.Models;
using Shared.Parsing;
using System.Diagnostics;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Lab5KomiwojazerGui;

public partial class MainWindow : Window
{
    private Process? _workerProcess;
    private List<City>? _cities;
    private int[]? _bestTour;
    private long _expectedProcessedCount;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LogListBox.Items.Clear();
            TourCanvas.Children.Clear();
            TourDataGrid.ItemsSource = null;
            StatusTextBlock.Text = "Uruchamianie...";
            BestLengthTextBlock.Text = "-";
            ProcessedTextBlock.Text = "-";
            _bestTour = null;
            MainProgressBar.Value = 0;
            ProgressPercentTextBlock.Text = "0%";
            EpochTextBlock.Text = "-";
            BestWorkerTextBlock.Text = "-";
            PhaseWorkerTextBlock.Text = "-";
            _expectedProcessedCount = 0;

            string mode = GetSelectedMode();
            string workerPath = ResolveWorkerPath(mode);

            if (!IOFile.Exists(workerPath))
            {
                MessageBox.Show($"Nie znaleziono pliku:\n{workerPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string workerDirectory = IOPath.GetDirectoryName(workerPath)!;
            string tspPath = ResolveTspPath(workerDirectory, TspPathTextBox.Text.Trim());

            if (!IOFile.Exists(tspPath))
            {
                MessageBox.Show($"Nie znaleziono pliku TSP:\n{tspPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _cities = Parser.LoadCities(tspPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = workerPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = workerDirectory
            };

            startInfo.ArgumentList.Add(tspPath);
            startInfo.ArgumentList.Add(WorkerCountTextBox.Text.Trim());
            startInfo.ArgumentList.Add(EpochCountTextBox.Text.Trim());
            startInfo.ArgumentList.Add(PmxAttemptsTextBox.Text.Trim());
            startInfo.ArgumentList.Add(ThreeOptTimeTextBox.Text.Trim());

            _workerProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            _workerProcess.Start();

            SetRunningState(true);
            StatusTextBlock.Text = "Uruchomiono";

            await Task.WhenAll(
                ReadWorkerOutputAsync(_workerProcess),
                _workerProcess.WaitForExitAsync());

            if (StatusTextBlock.Text != "Przerwano" && StatusTextBlock.Text != "Zakończono")
                StatusTextBlock.Text = "Zakończono";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Błąd";
        }
        finally
        {
            SetRunningState(false);
            _workerProcess?.Dispose();
            _workerProcess = null;
        }
    }

    private async Task ReadWorkerOutputAsync(Process process)
    {
        while (true)
        {
            string? line = await process.StandardOutput.ReadLineAsync();

            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            await Dispatcher.InvokeAsync(() => HandleWorkerMessage(line));
        }
    }

    private void HandleWorkerMessage(string line)
    {

        try
        {
            using var document = JsonDocument.Parse(line);

            if (!document.RootElement.TryGetProperty("type", out var typeElement))
                return;

            string? type = typeElement.GetString();
            if (type != "progress")
            {
                LogListBox.Items.Add(line);

                if (LogListBox.Items.Count > 300)
                    LogListBox.Items.RemoveAt(0);

                LogListBox.ScrollIntoView(line);
            }
            if (type == "started")
            {
                var message = JsonSerializer.Deserialize<StartedMessage>(line, JsonOptions);

                if (message is not null)
                {
                    StatusTextBlock.Text = $"Działa: {message.SynchronizationMode}";
                    _expectedProcessedCount = (long)message.WorkerCount * message.EpochCount * 2;
                    MainProgressBar.Maximum = _expectedProcessedCount;
                    MainProgressBar.Value = 0;
                    ProgressPercentTextBlock.Text = "0%";
                }
            }
            else if (type == "progress")
            {
                var message = JsonSerializer.Deserialize<ProgressMessage>(line, JsonOptions);

                if (message is not null)
                {
                    ProcessedTextBlock.Text = message.ProcessedCount.ToString();
                    EpochTextBlock.Text = message.Epoch.ToString();
                    PhaseWorkerTextBlock.Text = $"{message.Phase}, zadanie {message.WorkerId}";
                    StatusTextBlock.Text = $"Epoka {message.Epoch}, faza {message.Phase}, zadanie {message.WorkerId}";

                    if (_expectedProcessedCount > 0)
                    {
                        MainProgressBar.Value = Math.Min(message.ProcessedCount, _expectedProcessedCount);
                        double percent = message.ProcessedCount * 100.0 / _expectedProcessedCount;
                        ProgressPercentTextBlock.Text = $"{percent:F1}%";
                    }
                }
            }
            else if (type == "best")
            {
                var message = JsonSerializer.Deserialize<BestMessage>(line, JsonOptions);

                if (message is not null)
                {
                    BestLengthTextBlock.Text = message.Length.ToString("F2");
                    ProcessedTextBlock.Text = message.ProcessedCount.ToString();
                    StatusTextBlock.Text = $"Epoka {message.Epoch}, faza {message.Phase}, zadanie {message.WorkerId}";
                    EpochTextBlock.Text = message.Epoch.ToString();
                    PhaseWorkerTextBlock.Text = $"{message.Phase}, zadanie {message.WorkerId}";
                    BestWorkerTextBlock.Text = $"zadanie {message.WorkerId}";
                    _bestTour = message.Tour;
                    DrawTour();
                    UpdateTourTable();
                }
            }
            else if (type == "control")
            {
                var message = JsonSerializer.Deserialize<ControlMessage>(line, JsonOptions);

                if (message is not null)
                    StatusTextBlock.Text = $"Komenda: {message.Command}";
            }
            else if (type == "finished")
            {
                var message = JsonSerializer.Deserialize<FinishedMessage>(line, JsonOptions);

                if (message is not null)
                {
                    BestLengthTextBlock.Text = message.BestLength.ToString("F2");
                    ProcessedTextBlock.Text = message.ProcessedCount.ToString();
                    StatusTextBlock.Text = message.WasCancelled ? "Przerwano" : "Zakończono";

                    if (!message.WasCancelled && _expectedProcessedCount > 0)
                    {
                        MainProgressBar.Value = _expectedProcessedCount;
                        ProgressPercentTextBlock.Text = "100%";
                    }
                    else if (_expectedProcessedCount > 0)
                    {
                        MainProgressBar.Value = Math.Min(message.ProcessedCount, _expectedProcessedCount);

                        double percent = message.ProcessedCount * 100.0 / _expectedProcessedCount;
                        ProgressPercentTextBlock.Text = $"{percent:F1}%";
                    }

                    _bestTour = message.BestTour;
                    DrawTour();
                    UpdateTourTable();
                }
            }
            else if (type == "error")
            {
                var message = JsonSerializer.Deserialize<ErrorMessage>(line, JsonOptions);

                if (message is not null)
                    StatusTextBlock.Text = $"Błąd: {message.Message}";
            }
        }
        catch
        {
            StatusTextBlock.Text = "Nie udało się odczytać JSON";
        }
    }

    private void DrawTour()
    {
        TourCanvas.Children.Clear();

        if (_cities is null || _bestTour is null || _bestTour.Length == 0)
            return;

        double width = TourCanvas.ActualWidth;
        double height = TourCanvas.ActualHeight;

        if (width <= 10 || height <= 10)
            return;

        double minX = _cities.Min(city => city.X);
        double maxX = _cities.Max(city => city.X);
        double minY = _cities.Min(city => city.Y);
        double maxY = _cities.Max(city => city.Y);

        double dataWidth = maxX - minX;
        double dataHeight = maxY - minY;

        if (dataWidth <= 0 || dataHeight <= 0)
            return;

        double margin = 20;
        double scaleX = (width - 2 * margin) / dataWidth;
        double scaleY = (height - 2 * margin) / dataHeight;
        double scale = Math.Min(scaleX, scaleY);

        Point MapPoint(City city)
        {
            double x = margin + (city.X - minX) * scale;
            double y = height - margin - (city.Y - minY) * scale;

            return new Point(x, y);
        }

        for (int i = 0; i < _bestTour.Length; i++)
        {
            int currentIndex = _bestTour[i];
            int nextIndex = _bestTour[(i + 1) % _bestTour.Length];

            if (currentIndex < 0 || currentIndex >= _cities.Count)
                continue;

            if (nextIndex < 0 || nextIndex >= _cities.Count)
                continue;

            Point current = MapPoint(_cities[currentIndex]);
            Point next = MapPoint(_cities[nextIndex]);

            var line = new Line
            {
                X1 = current.X,
                Y1 = current.Y,
                X2 = next.X,
                Y2 = next.Y,
                Stroke = Brushes.DarkSlateGray,
                StrokeThickness = 1.5
            };

            TourCanvas.Children.Add(line);
        }

        foreach (int cityIndex in _bestTour)
        {
            if (cityIndex < 0 || cityIndex >= _cities.Count)
                continue;

            Point point = MapPoint(_cities[cityIndex]);

            var ellipse = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = Brushes.Crimson,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            TourCanvas.Children.Add(ellipse);
        }
    }
    private void UpdateTourTable()
    {
        if (_cities is null || _bestTour is null)
        {
            TourDataGrid.ItemsSource = null;
            return;
        }

        var rows = new List<TourRow>();

        for (int i = 0; i < _bestTour.Length; i++)
        {
            int cityIndex = _bestTour[i];

            if (cityIndex < 0 || cityIndex >= _cities.Count)
                continue;

            City city = _cities[cityIndex];

            rows.Add(new TourRow(
                i + 1,
                city.Id,
                city.X,
                city.Y));
        }

        TourDataGrid.ItemsSource = rows;
    }
    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        SendCommand("pause");
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        SendCommand("resume");
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        SendCommand("stop");
    }

    private void SendCommand(string command)
    {
        if (_workerProcess is null || _workerProcess.HasExited)
            return;

        _workerProcess.StandardInput.WriteLine(command);
        _workerProcess.StandardInput.Flush();
    }

    private void SetRunningState(bool isRunning)
    {
        StartButton.IsEnabled = !isRunning;
        PauseButton.IsEnabled = isRunning;
        ResumeButton.IsEnabled = isRunning;
        StopButton.IsEnabled = isRunning;
        ModeComboBox.IsEnabled = !isRunning;
    }

    private string GetSelectedMode()
    {
        var selected = (ModeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

        return selected == "ThreadPool"
            ? "threadpool"
            : "tpl";
    }

    private static string ResolveWorkerPath(string mode)
    {
        string projectName = mode == "threadpool"
            ? "Lab5KomiwojazerThreadPool"
            : "Lab5Komiwojazer";

        string exeName = $"{projectName}.exe";

        return IOPath.GetFullPath(IOPath.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            projectName,
            "bin",
            "Debug",
            "net10.0",
            exeName));
    }

    private static string ResolveTspPath(string workerDirectory, string path)
    {
        if (IOPath.IsPathRooted(path))
            return path;

        return IOPath.GetFullPath(IOPath.Combine(workerDirectory, path));
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawTour();
    }
    private sealed record TourRow(
    int Position,
    int CityId,
    double X,
    double Y);

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            if (_workerProcess is not null && !_workerProcess.HasExited)
            {
                _workerProcess.StandardInput.WriteLine("stop");
                _workerProcess.StandardInput.Flush();

                if (!_workerProcess.WaitForExit(2000))
                    _workerProcess.Kill(true);
            }
        }
        catch
        {
        }
    }
}