using Shared.Communication;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Lab5KomiwojazerGui;

public partial class MainWindow : Window
{
    private Process? _workerProcess;

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
            StatusTextBlock.Text = "Uruchamianie...";
            BestLengthTextBlock.Text = "-";
            ProcessedTextBlock.Text = "-";

            string mode = GetSelectedMode();
            string workerPath = ResolveWorkerPath(mode);

            if (!File.Exists(workerPath))
            {
                MessageBox.Show($"Nie znaleziono pliku:\n{workerPath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = workerPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(workerPath)!
            };

            startInfo.ArgumentList.Add(TspPathTextBox.Text.Trim());
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
        LogListBox.Items.Add(line);
        LogListBox.ScrollIntoView(line);

        try
        {
            using var document = JsonDocument.Parse(line);

            if (!document.RootElement.TryGetProperty("type", out var typeElement))
                return;

            string? type = typeElement.GetString();

            if (type == "started")
            {
                var message = JsonSerializer.Deserialize<StartedMessage>(line, JsonOptions);

                if (message is not null)
                    StatusTextBlock.Text = $"Działa: {message.SynchronizationMode}";
            }
            else if (type == "best")
            {
                var message = JsonSerializer.Deserialize<BestMessage>(line, JsonOptions);

                if (message is not null)
                {
                    BestLengthTextBlock.Text = message.Length.ToString("F2");
                    ProcessedTextBlock.Text = message.ProcessedCount.ToString();
                    StatusTextBlock.Text = $"Epoka {message.Epoch}, faza {message.Phase}, zadanie {message.WorkerId}";
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

        return Path.GetFullPath(Path.Combine(
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
}