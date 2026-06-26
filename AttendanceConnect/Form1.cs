using AttendanceConnect.Services;
using AttendanceConnect.Utilities;

namespace AttendanceConnect
{
    public partial class Form1 : Form
    {
        private NotifyIcon _notifyIcon = null!;
        private System.Windows.Forms.Timer _syncTimer = null!;
        private Icon _normalIcon = null!;
        private Icon _errorIcon = null!;
        private bool _isErrorState = false;

        private ZKTecoService _zkTecoService = null!;
        private DatabaseService _databaseService = null!;
        private SimpleLogger _logger = null!;
        private ConfigReader _config = null!;

        private TimeOnly _syncStartTime;
        private TimeOnly _syncEndTime;
        private int _syncIntervalHours;

        public Form1()
        {
            InitializeComponent();
            InitializeConfiguration();
            InitializeServices();
            InitializeUI();
            InitializeTimer();
        }

        private void InitializeConfiguration()
        {
            try
            {
                var configPath = Path.Combine(Application.StartupPath, "appsettings.json");
                _config = new ConfigReader(configPath);

                var logPath = _config.GetLogPath();
                _logger = new SimpleLogger(logPath);

                _logger.Information("Application started");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeServices()
        {
            try
            {
                var ip = _config.GetZKTecoIP();
                var port = _config.GetZKTecoPort();
                var key = _config.GetZKTecoKey();
                var deviceCode = _config.GetDeviceCode();

                _syncIntervalHours = _config.GetSyncInterval();
                _syncStartTime = TimeOnly.Parse(_config.GetStartTime());
                _syncEndTime = TimeOnly.Parse(_config.GetEndTime());

                _zkTecoService = new ZKTecoService(ip, port, key, deviceCode, _logger);

                var connectionString = _config.GetConnectionString("DefaultConnection");
                _databaseService = new DatabaseService(connectionString, _logger);

                _logger.Information("Services initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing services: {ex.Message}", ex);
                MessageBox.Show("Error initializing services. Please check appsettings.json", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeUI()
        {
            // Create icons
            _normalIcon = IconGenerator.CreateNormalIcon();
            _errorIcon = IconGenerator.CreateErrorIcon();

            // Setup form
            this.Text = "AttendanceConnect Sync";
            this.Width = 400;
            this.Height = 300;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Opacity = 0;

            // Create notify icon
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = _normalIcon;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "AttendanceConnect - Syncing";

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Execute Now", null, ExecuteNow_Click);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, Exit_Click);

            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (s, e) => ShowStatus();
        }

        private void InitializeTimer()
        {
            _syncTimer = new System.Windows.Forms.Timer();
            _syncTimer.Interval = 60000; // Check every minute
            _syncTimer.Tick += SyncTimer_Tick;
            _syncTimer.Start();

            _logger.Information($"Sync timer started - will sync every {_syncIntervalHours} hours between {_syncStartTime} and {_syncEndTime}");
        }

        private void SyncTimer_Tick(object? sender, EventArgs e)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);

            // Check if current time is within sync window
            if (now >= _syncStartTime && now <= _syncEndTime)
            {
                // Check if it's time to sync (every N hours)
                var lastSyncFile = Path.Combine(Application.StartupPath, "last_sync.txt");
                var shouldSync = true;

                if (File.Exists(lastSyncFile))
                {
                    var lastSyncTime = DateTime.ParseExact(
                        File.ReadAllText(lastSyncFile).Trim(),
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);

                    var timeSinceLastSync = DateTime.Now - lastSyncTime;
                    shouldSync = timeSinceLastSync.TotalHours >= _syncIntervalHours;
                }

                if (shouldSync)
                {
                    _ = ExecuteSyncAsync();
                }
            }
        }

        private void ExecuteNow_Click(object? sender, EventArgs e)
        {
            _ = ExecuteSyncAsync();
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            _logger.Information("Application closing");
            _syncTimer?.Stop();
            _notifyIcon?.Dispose();
            _normalIcon?.Dispose();
            _errorIcon?.Dispose();
            Application.Exit();
        }

        private async Task ExecuteSyncAsync()
        {
            try
            {
                _logger.Information("Starting attendance sync...");
                SetNormalIcon();

                // Fetch logs from ZKTeco
                var logs = await _zkTecoService.FetchAttendanceLogsAsync();

                if (logs.Count == 0)
                {
                    _logger.Information("No new attendance logs to sync");
                    _notifyIcon.Text = "AttendanceConnect - No new logs";
                    return;
                }

                // Insert to database
                var insertedCount = await _databaseService.InsertAttendanceLogsAsync(logs);

                // Update last sync time
                File.WriteAllText(
                    Path.Combine(Application.StartupPath, "last_sync.txt"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                _logger.Information($"Sync completed - {insertedCount} logs inserted");
                _notifyIcon.Text = $"AttendanceConnect - Last sync: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                SetNormalIcon();
            }
            catch (Exception ex)
            {
                _logger.Error("Sync failed", ex);
                _notifyIcon.Text = "AttendanceConnect - Error occurred";
                SetErrorIcon();
            }
        }

        private void SetNormalIcon()
        {
            if (_isErrorState)
            {
                _notifyIcon.Icon = _normalIcon;
                _isErrorState = false;
            }
        }

        private void SetErrorIcon()
        {
            if (!_isErrorState)
            {
                _notifyIcon.Icon = _errorIcon;
                _isErrorState = true;
            }
        }

        private void ShowStatus()
        {
            var message = $"AttendanceConnect Status\n\n" +
                         $"Sync Interval: Every {_syncIntervalHours} hours\n" +
                         $"Active Hours: {_syncStartTime} - {_syncEndTime}\n" +
                         $"Status: {(_isErrorState ? "Error" : "Running")}\n" +
                         $"Last Sync: {GetLastSyncTime()}\n";

            MessageBox.Show(message, "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetLastSyncTime()
        {
            var lastSyncFile = Path.Combine(Application.StartupPath, "last_sync.txt");
            if (File.Exists(lastSyncFile))
            {
                return File.ReadAllText(lastSyncFile).Trim();
            }
            return "Never";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}
