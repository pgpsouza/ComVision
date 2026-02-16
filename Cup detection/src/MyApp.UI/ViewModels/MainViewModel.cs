using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using MyApp.Common.Models;
using MyApp.Services.Services;

namespace MyApp.UI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IItemService _itemService;
    private Process? _runningProcess;
    private int? _runningProcessId;
    private readonly object _processLock = new();

    public ObservableCollection<ItemModel> Items { get; } = new();

    private ItemModel? _selectedItem;
    public ItemModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ICommand ShowCommand { get; }
    public RelayCommand StopCommand { get; }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    // ImageSource bound to the Image control in XAML
    private ImageSource? _currentFrame;
    public ImageSource? CurrentFrame
    {
        get => _currentFrame;
        set => SetProperty(ref _currentFrame, value);
    }

    private CancellationTokenSource? _frameCts;
    private Task? _frameTask;

    private void StartFrameReceiver(int port = 9000)
    {
        StopFrameReceiver();
        _frameCts = new CancellationTokenSource();
        var ct = _frameCts.Token;
        _frameTask = Task.Run(() => ReceiveFramesLoop(port, ct), ct);
    }

    private void StopFrameReceiver()
    {
        try { _frameCts?.Cancel(); } catch { }
        _frameCts = null;
        _frameTask = null;
        System.Windows.Application.Current?.Dispatcher.Invoke(() => CurrentFrame = null);
    }

    private async Task ReceiveFramesLoop(int port, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port, ct).ConfigureAwait(false);
                using var ns = client.GetStream();

                while (!ct.IsCancellationRequested)
                {
                    var lenBuf = new byte[4];
                    var read = 0;
                    while (read < 4)
                    {
                        var r = await ns.ReadAsync(lenBuf, read, 4 - read, ct).ConfigureAwait(false);
                        if (r == 0) throw new EndOfStreamException();
                        read += r;
                    }

                    int length = BitConverter.ToInt32(lenBuf, 0);
                    if (length <= 0 || length > 10_000_000)
                    {
                        Array.Reverse(lenBuf);
                        length = BitConverter.ToInt32(lenBuf, 0);
                    }
                    if (length <= 0 || length > 10_000_000) throw new InvalidDataException("Invalid image length");

                    var imgBuf = new byte[length];
                    var offset = 0;
                    while (offset < length)
                    {
                        var r = await ns.ReadAsync(imgBuf, offset, length - offset, ct).ConfigureAwait(false);
                        if (r == 0) throw new EndOfStreamException();
                        offset += r;
                    }

                    using var ms = new MemoryStream(imgBuf);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();

                    System.Windows.Application.Current?.Dispatcher.Invoke(() => CurrentFrame = bmp);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception)
            {
                try { await Task.Delay(500, ct).ConfigureAwait(false); } catch { break; }
            }
        }
    }

    public MainViewModel(IItemService itemService)
    {
        _itemService = itemService;
        foreach (var item in _itemService.GetItems())
            Items.Add(item);

        ShowCommand = new RelayCommand(Show, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
    }

    private void Show()
    {
        var name = SelectedItem?.Name ?? "(none)";

        // Run the Python script from the venv in background so UI is not blocked
        _ = Task.Run(async () =>
        {
            try
            {
                // Adjust these paths if your venv or script have different names
                var venvPython = @"C:\Users\55279\Desktop\CompVision\venv\Scripts\python.exe";
                var scriptPath = @"C:\Users\55279\Desktop\CompVision\cup_detection.py";

                if (!File.Exists(venvPython))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show($"Python do venv não encontrado: {venvPython}"));
                    return;
                }

                if (!File.Exists(scriptPath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.MessageBox.Show($"Script não encontrado: {scriptPath}"));
                    return;
                }

                // set IsRunning on UI thread to avoid cross-thread PropertyChanged
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsRunning = true);

                // start receiver before launching script so frames can be accepted
                StartFrameReceiver(9000);

                var output = await RunScriptFromVenvAsync(venvPython, scriptPath);

                // marshal UI updates to UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"Selected: {name}\n\nScript output:\n{output}"));
            }
            catch (Exception ex)
            {
                // marshal exception message to UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    System.Windows.MessageBox.Show($"Erro ao executar script: {ex.Message}"));
            }
            finally
            {
                // clear IsRunning on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsRunning = false);
                // ensure Stop button state updated
                StopCommand.RaiseCanExecuteChangedOnUI();

                // stop receiving frames
                StopFrameReceiver();
            }
        });
    }

    private async Task<string> RunScriptFromVenvAsync(string pythonExe, string scriptPath, string args = "")
    {
        var psi = new ProcessStartInfo(pythonExe, $"\"{scriptPath}\" {args}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory
        };

        var proc = new Process { StartInfo = psi };
        proc.Start();

        // expose process for stop (thread-safe)
        lock (_processLock)
        {
            _runningProcess = proc;
            try { _runningProcessId = proc.Id; } catch { _runningProcessId = null; }
        }
        // raise CanExecuteChanged on UI thread so Stop button becomes enabled
        StopCommand.RaiseCanExecuteChangedOnUI();

        var outputTask = proc.StandardOutput.ReadToEndAsync();
        var errorTask = proc.StandardError.ReadToEndAsync();

        await proc.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        // clear running process (thread-safe)
        lock (_processLock)
        {
            _runningProcess = null;
            _runningProcessId = null;
        }
        StopCommand.RaiseCanExecuteChangedOnUI();

        try { proc.Dispose(); } catch { }

        if (proc.ExitCode != 0)
            throw new Exception(string.IsNullOrWhiteSpace(error) ? $"Exit code {proc.ExitCode}" : error);

        return output;
    }
    private void Stop()
    {
        int? pid = null;
        lock (_processLock)
        {
            pid = _runningProcessId;
            // clear stored references immediately to avoid races
            _runningProcess = null;
            _runningProcessId = null;
        }

        if (pid == null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show("Nenhum processo em execução."));
            StopCommand.RaiseCanExecuteChangedOnUI();
            return;
        }

        try
        {
            // obtain a fresh Process instance by id to avoid using a potentially disposed object
            var target = Process.GetProcessById(pid.Value);
            try
            {
                if (!target.HasExited)
                {
                    target.Kill(true);
                    target.WaitForExit(5000);
                }
            }
            catch (ArgumentException) { /* process already exited */ }
            catch (InvalidOperationException) { /* process already exited */ }
            catch (System.ComponentModel.Win32Exception) { /* access denied or already terminating */ }
        }
        catch (ArgumentException)
        {
            // process with pid no longer exists
        }
        catch (Exception ex)
        {
            var friendly = ex switch
            {
                InvalidOperationException => "O processo já foi finalizado ou não está associado.",
                System.ComponentModel.Win32Exception => "Falha ao encerrar o processo (acesso negado ou recurso não disponível).",
                _ => ex.Message
            };
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show($"Erro ao terminar o script: {friendly}"));
        }
        finally
        {
            StopCommand.RaiseCanExecuteChangedOnUI();
            StopFrameReceiver();
        }
    }
}