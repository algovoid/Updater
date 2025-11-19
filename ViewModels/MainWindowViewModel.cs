using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Updater.Models;


namespace Updater.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly UpdateConfig _config;
        
        [ObservableProperty]
        private string _statusText = "Ready to check for updates";
        
        [ObservableProperty]
        private int _progress;
        
        [ObservableProperty]
        private bool _isUpdating;
        
        [ObservableProperty]
        private bool _canCancel;
        
        [ObservableProperty]
        private string _updateButtonText = "Check for Updates";
        
        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = new();

        private CancellationTokenSource _cancellationTokenSource;

        public MainWindowViewModel()
        {
            // Load configuration
            _config = LoadConfig();
            
            // Initialize logs
            LogEntries.Add(new LogEntry { Timestamp = DateTime.Now, Message = "Updater initialized successfully", Type = "System" });
            LogEntries.Add(new LogEntry { Timestamp = DateTime.Now, Message = "Ready to begin update process", Type = "System" });
            
            // Initialize commands
            UpdateCommand = new AsyncRelayCommand(ExecuteUpdateAsync, () => !IsUpdating);
            CancelCommand = new RelayCommand(ExecuteCancel, () => CanCancel);
        }
        
        public ICommand UpdateCommand { get; }
        public ICommand CancelCommand { get; }
        
        public string VersionInfo => $"Current Version: {_config.CurrentVersion} | Software: {_config.SoftwareName}";
        
        private UpdateConfig LoadConfig()
        {
            try
            {
                const string configPath = "update-config.json";
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<UpdateConfig>(json) ?? new UpdateConfig();
                }
                
                // Create default config if it doesn't exist
                var defaultConfig = new UpdateConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
            catch (Exception ex)
            {
                AddLog($"Error loading config: {ex.Message}", "Error");
                return new UpdateConfig();
            }
        }
        
        private void SaveConfig(UpdateConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText("update-config.json", json);
            }
            catch (Exception ex)
            {
                AddLog($"Error saving config: {ex.Message}", "Error");
            }
        }
        
        
        private async Task ExecuteUpdateAsync()
        {
            IsUpdating = true;
            CanCancel = true;
            UpdateButtonText = "Updating...";
            
            ((AsyncRelayCommand)UpdateCommand).NotifyCanExecuteChanged();
            ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            AddLog("Update process initiated by user", "User");
            AddLog("Preparing to check for updates...", "System");
            
            try
            {
                await SimulateUpdateAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                AddLog("Update was cancelled", "Warning");
                StatusText = "Update cancelled";
            }
            catch (Exception ex)
            {
                AddLog($"Update failed: {ex.Message}", "Error");
                StatusText = "Update failed";
            }
            finally
            {
                IsUpdating = false;
                CanCancel = false;
                ((AsyncRelayCommand)UpdateCommand).NotifyCanExecuteChanged();
                ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
                UpdateButtonText = "Check for Updates";
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        private void ExecuteCancel()
        {
            _cancellationTokenSource?.Cancel();
            AddLog("Update process cancelled by user", "Warning");
        }
        
        private async Task SimulateUpdateAsync(CancellationToken cancellationToken)
        {
            var steps = new[]
            {
                new { Progress = 10, Message = "Connecting to update server...", Status = "Checking for updates..." },
                new { Progress = 25, Message = "Checking for available updates...", Status = "Checking for updates..." },
                new { Progress = 35, Message = $"New version found: 2.6.0 for {_config.SoftwareName}", Status = "Downloading update..." },
                new { Progress = 50, Message = "Downloading update package...", Status = "Downloading update..." },
                new { Progress = 65, Message = "Download complete. Verifying integrity...", Status = "Verifying files..." },
                new { Progress = 80, Message = "Verification successful", Status = "Installing update..." },
                new { Progress = 90, Message = "Installing update files...", Status = "Installing update..." },
                new { Progress = 100,Message = "Update completed successfully!", Status = "Update complete!" }
            };
            
            foreach (var step in steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Progress = step.Progress;
                    StatusText = step.Status;
                    AddLog(step.Message, "Update");
                });
                
                await Task.Delay(800, cancellationToken);
            }
            
            if (!cancellationToken.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateButtonText = "Update Complete";
                    AddLog($"Update for {_config.SoftwareName} completed successfully!", "Success");
                });
                
                await Task.Delay(3000, cancellationToken);
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateButtonText = "Check for Updates";
                });
            }
        }
        
        private void AddLog(string message, string type = "Info")
        {
            Dispatcher.UIThread.Post(() =>
            {
                LogEntries.Add(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    Type = type
                });
            });
        }
        
        // Update command can execute logic
        private void OnIsUpdatingChanged() => ((AsyncRelayCommand)UpdateCommand).NotifyCanExecuteChanged();
        
        // Cancel command can execute logic  
        private void OnCanCancelChanged() => ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
        
        
        
        partial void OnIsUpdatingChanged(bool value)
        {
            OnCanCancelChanged();
            OnIsUpdatingChanged();
            // Notify commands that their execution state may have changed
            //UpdateCommand.NotifyCanExecuteChanged();
            //CancelCommand.NotifyCanExecuteChanged();
        }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
        
    public string Formatted => $"[{Timestamp:HH:mm:ss}] {Message}";
}