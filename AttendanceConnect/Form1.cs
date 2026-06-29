using System.Data;
using AttendanceConnect.Models;
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

        private TabControl _tabControl = null!;
        private DataGridView _logsGrid = null!;
        private Button _btnReadData = null!;
        private Button _btnUpdate = null!;
        private List<AttendanceLog> _pendingLogs = new();

        private Label _pcTimeLabel = null!;
        private Label _deviceTimeLabel = null!;
        private Button _btnSyncTime = null!;

        private TextBox _logsViewTextBox = null!;
        private Button _btnRefreshLogs = null!;
        private Button _btnPrevLogWeek = null!;
        private Button _btnNextLogWeek = null!;
        private Label _logWeekLabel = null!;
        private DateTime _viewedLogWeekStart;

        private const string AdminPassword = "Admin@123";
        private bool _settingsUnlocked;

        private TextBox _settingsServerTextBox = null!;
        private TextBox _settingsDbNameTextBox = null!;
        private TextBox _settingsUserIdTextBox = null!;
        private TextBox _settingsPasswordTextBox = null!;
        private TextBox _settingsZkIpTextBox = null!;
        private TextBox _settingsZkPortTextBox = null!;
        private TextBox _settingsZkKeyTextBox = null!;
        private TextBox _settingsDeviceCodeTextBox = null!;
        private TextBox _settingsIntervalHoursTextBox = null!;
        private TextBox _settingsStartTimeTextBox = null!;
        private TextBox _settingsEndTimeTextBox = null!;
        private TextBox _settingsLogPathTextBox = null!;
        private Button _btnSaveSettings = null!;

        private ZKTecoHelper _zkTecoHelper = null!;
        private DatabaseService _databaseService = null!;
        private SimpleLogger _logger = null!;
        private AppSettingsService _appSettings = null!;

        private string _zkTecoIp = "";
        private int _zkTecoPort;
        private int _zkTecoCommKey;
        private string _deviceCode = "";

        private TimeSpan _syncStartTime;
        private TimeSpan _syncEndTime;
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
                _appSettings = new AppSettingsService();

                _logger = new SimpleLogger(_appSettings.Settings.Logging.LogPath);

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
                ApplyRuntimeSettings();
                _zkTecoHelper = new ZKTecoHelper();
                _databaseService = new DatabaseService(_appSettings.GetConnectionString(), _logger);

                _logger.Information("Services initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing services: {ex.Message}", ex);
                MessageBox.Show("Error initializing services. Please check AttendanceConnect.exe.config", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Refreshes the runtime fields read from <see cref="_appSettings"/> - called on startup and after saving Settings tab changes.</summary>
        private void ApplyRuntimeSettings()
        {
            _zkTecoIp = _appSettings.Settings.ZKTeco.IP;
            _zkTecoPort = _appSettings.Settings.ZKTeco.Port;
            _zkTecoCommKey = int.TryParse(_appSettings.Settings.ZKTeco.Key, out var parsed) ? parsed : 0;
            _deviceCode = _appSettings.Settings.Sync.DeviceCode;

            _syncIntervalHours = _appSettings.Settings.Sync.IntervalHours;
            _syncStartTime = TimeSpan.Parse(_appSettings.Settings.Sync.StartTime);
            _syncEndTime = TimeSpan.Parse(_appSettings.Settings.Sync.EndTime);
        }

        private void InitializeUI()
        {
            // Create icons
            _normalIcon = IconGenerator.CreateNormalIcon();
            _errorIcon = IconGenerator.CreateErrorIcon();

            // Setup form
            this.Text = "AttendanceConnect Sync";
            this.Width = 840;
            this.Height = 576;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Opacity = 0;

            InitializeTabs();

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

            _notifyIcon.DoubleClick += (s, e) => ShowMainForm();
        }

        private void InitializeTabs()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var attendanceTab = new TabPage("Chấm công");
            attendanceTab.Controls.Add(BuildAttendanceTabContent());

            var logsTab = new TabPage("Xem logs");
            logsTab.Controls.Add(BuildLogsTabContent());

            var settingsTab = new TabPage("Cài đặt");
            settingsTab.Controls.Add(BuildSettingsTabContent());

            _tabControl.TabPages.Add(attendanceTab);
            _tabControl.TabPages.Add(logsTab);
            _tabControl.TabPages.Add(settingsTab);

            _tabControl.Selecting += (s, e) =>
            {
                if (e.TabPage == settingsTab && !_settingsUnlocked)
                {
                    if (!PromptForAdminPassword())
                    {
                        e.Cancel = true;
                        return;
                    }

                    _settingsUnlocked = true;
                    LoadSettingsIntoFields();
                }
            };

            _tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (_tabControl.SelectedTab == logsTab)
                    RefreshLogsView();
            };

            this.Controls.Add(_tabControl);
        }

        private bool PromptForAdminPassword()
        {
            using var dialog = new Form
            {
                Text = "Xác thực",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Width = 320,
                Height = 160,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            var label = new Label
            {
                Text = "Nhập mật khẩu quản trị:",
                Location = new Point(15, 15),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(15, 40),
                Width = 270,
                PasswordChar = '*'
            };

            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(120, 75),
                Width = 75,
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(205, 75),
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.Add(label);
            dialog.Controls.Add(textBox);
            dialog.Controls.Add(btnOk);
            dialog.Controls.Add(btnCancel);
            dialog.AcceptButton = btnOk;
            dialog.CancelButton = btnCancel;

            var result = dialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return false;

            if (textBox.Text == AdminPassword)
                return true;

            MessageBox.Show("Mật khẩu không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        private Panel BuildAttendanceTabContent()
        {
            var container = new Panel { Dock = DockStyle.Fill };

            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46
            };

            _btnReadData = new Button
            {
                Text = "Đọc dữ liệu",
                Location = new Point(10, 8),
                Size = new Size(120, 30)
            };
            _btnReadData.Click += BtnReadData_Click;

            _btnUpdate = new Button
            {
                Text = "Cập nhật dữ liệu",
                Location = new Point(140, 8),
                Size = new Size(140, 30),
                Enabled = false
            };
            _btnUpdate.Click += BtnUpdate_Click;

            _btnSyncTime = new Button
            {
                Text = "Đồng bộ giờ",
                Location = new Point(290, 8),
                Size = new Size(120, 30)
            };
            _btnSyncTime.Click += BtnSyncTime_Click;

            var pcTimeCaption = new Label
            {
                Text = "Giờ máy tính:",
                Location = new Point(430, 14),
                AutoSize = true
            };

            _pcTimeLabel = new Label
            {
                Text = "Chưa đọc",
                Location = new Point(500, 14),
                Size = new Size(110, 20),
                ForeColor = Color.Blue
            };

            var deviceTimeCaption = new Label
            {
                Text = "Giờ máy cc:",
                Location = new Point(630, 14),
                AutoSize = true
            };

            _deviceTimeLabel = new Label
            {
                Text = "Chưa đọc",
                Location = new Point(690, 14),
                Size = new Size(110, 20),
                ForeColor = Color.Red
            };

            toolbarPanel.Controls.Add(_btnReadData);
            toolbarPanel.Controls.Add(_btnUpdate);
            toolbarPanel.Controls.Add(_btnSyncTime);
            toolbarPanel.Controls.Add(pcTimeCaption);
            toolbarPanel.Controls.Add(_pcTimeLabel);
            toolbarPanel.Controls.Add(deviceTimeCaption);
            toolbarPanel.Controls.Add(_deviceTimeLabel);

            _logsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _logsGrid.Columns.Add("UserID", "UserID");
            _logsGrid.Columns.Add("VerifyDate", "Giờ chấm công");
            _logsGrid.Columns.Add("FullName", "Nhân viên");
            _logsGrid.Columns.Add("VerifyType", "Loại chấm công");
            _logsGrid.Columns.Add("VerifyState", "Tình trạng");

            _logsGrid.Columns["UserID"].FillWeight = 70;
            _logsGrid.Columns["VerifyType"].FillWeight = 70;
            _logsGrid.Columns["VerifyState"].FillWeight = 70;
            _logsGrid.Columns["FullName"].FillWeight = 190;

            container.Controls.Add(toolbarPanel);
            container.Controls.Add(_logsGrid);
            _logsGrid.BringToFront();

            return container;
        }

        private Panel BuildLogsTabContent()
        {
            var container = new Panel { Dock = DockStyle.Fill };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50
            };

            _btnPrevLogWeek = new Button
            {
                Text = "< Tuần trước",
                Location = new Point(10, 10),
                Size = new Size(110, 30)
            };
            _btnPrevLogWeek.Click += (s, e) => ChangeLogWeek(-7);

            _btnNextLogWeek = new Button
            {
                Text = "Tuần sau >",
                Location = new Point(130, 10),
                Size = new Size(110, 30)
            };
            _btnNextLogWeek.Click += (s, e) => ChangeLogWeek(7);

            _btnRefreshLogs = new Button
            {
                Text = "Làm mới",
                Location = new Point(250, 10),
                Size = new Size(100, 30)
            };
            _btnRefreshLogs.Click += (s, e) => RefreshLogsView();

            _logWeekLabel = new Label
            {
                Location = new Point(360, 18),
                Size = new Size(250, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            buttonPanel.Controls.Add(_btnPrevLogWeek);
            buttonPanel.Controls.Add(_btnNextLogWeek);
            buttonPanel.Controls.Add(_btnRefreshLogs);
            buttonPanel.Controls.Add(_logWeekLabel);

            _logsViewTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font(FontFamily.GenericMonospace, 9)
            };

            container.Controls.Add(buttonPanel);
            container.Controls.Add(_logsViewTextBox);
            _logsViewTextBox.BringToFront();

            return container;
        }

        private void ChangeLogWeek(int dayOffset)
        {
            _viewedLogWeekStart = _viewedLogWeekStart.AddDays(dayOffset);
            RefreshLogsView();
        }

        private void RefreshLogsView()
        {
            if (_viewedLogWeekStart == default)
                _viewedLogWeekStart = SimpleLogger.GetWeekStart(DateTime.Now);

            var currentWeekStart = SimpleLogger.GetWeekStart(DateTime.Now);
            if (_viewedLogWeekStart > currentWeekStart)
                _viewedLogWeekStart = currentWeekStart;

            _btnNextLogWeek.Enabled = _viewedLogWeekStart < currentWeekStart;
            _logWeekLabel.Text = $"Tuần: {_viewedLogWeekStart:dd/MM/yyyy} - {_viewedLogWeekStart.AddDays(6):dd/MM/yyyy}";

            try
            {
                var logFilePath = _logger.GetLogFilePathForDate(_viewedLogWeekStart);
                _logsViewTextBox.Text = File.Exists(logFilePath)
                    ? File.ReadAllText(logFilePath)
                    : "(Không có log cho tuần này)";

                _logsViewTextBox.SelectionStart = _logsViewTextBox.Text.Length;
                _logsViewTextBox.ScrollToCaret();
            }
            catch (Exception ex)
            {
                _logsViewTextBox.Text = $"Lỗi khi đọc log: {ex.Message}";
            }
        }

        private Panel BuildSettingsTabContent()
        {
            var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Height = 0
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _settingsServerTextBox = new TextBox();
            _settingsDbNameTextBox = new TextBox();
            _settingsUserIdTextBox = new TextBox();
            _settingsPasswordTextBox = new TextBox { PasswordChar = '*' };
            _settingsZkIpTextBox = new TextBox();
            _settingsZkPortTextBox = new TextBox();
            _settingsZkKeyTextBox = new TextBox();
            _settingsDeviceCodeTextBox = new TextBox();
            _settingsIntervalHoursTextBox = new TextBox();
            _settingsStartTimeTextBox = new TextBox();
            _settingsEndTimeTextBox = new TextBox();
            _settingsLogPathTextBox = new TextBox();

            AddSettingsRow(layout, "Server", _settingsServerTextBox);
            AddSettingsRow(layout, "Database", _settingsDbNameTextBox);
            AddSettingsRow(layout, "User", _settingsUserIdTextBox);
            AddSettingsRow(layout, "Password", _settingsPasswordTextBox);
            AddSettingsRow(layout, "IP thiết bị", _settingsZkIpTextBox);
            AddSettingsRow(layout, "Port", _settingsZkPortTextBox);
            AddSettingsRow(layout, "Key", _settingsZkKeyTextBox);
            AddSettingsRow(layout, "Mã thiết bị", _settingsDeviceCodeTextBox);
            AddSettingsRow(layout, "Giờ sync (mỗi N giờ)", _settingsIntervalHoursTextBox);
            AddSettingsRow(layout, "Giờ bắt đầu sync", _settingsStartTimeTextBox);
            AddSettingsRow(layout, "Giờ kết thúc sync", _settingsEndTimeTextBox);
            AddSettingsRow(layout, "Đường dẫn log", _settingsLogPathTextBox);

            _btnSaveSettings = new Button
            {
                Text = "Lưu cài đặt",
                Location = new Point(0, 0),
                Size = new Size(120, 32),
                Top = 10
            };
            _btnSaveSettings.Click += BtnSaveSettings_Click;

            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
            buttonPanel.Controls.Add(_btnSaveSettings);

            container.Controls.Add(layout);
            container.Controls.Add(buttonPanel);
            buttonPanel.BringToFront();

            return container;
        }

        private static void AddSettingsRow(TableLayoutPanel layout, string labelText, Control input)
        {
            var row = layout.RowCount;
            layout.RowCount = row + 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            var label = new Label
            {
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(3, 4, 3, 4);

            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private void LoadSettingsIntoFields()
        {
            var settings = _appSettings.Settings;

            _settingsServerTextBox.Text = settings.Database.Server;
            _settingsDbNameTextBox.Text = settings.Database.DbName;
            _settingsUserIdTextBox.Text = settings.Database.UserId;
            _settingsPasswordTextBox.Text = _appSettings.GetDecodedPassword();
            _settingsZkIpTextBox.Text = settings.ZKTeco.IP;
            _settingsZkPortTextBox.Text = settings.ZKTeco.Port.ToString();
            _settingsZkKeyTextBox.Text = settings.ZKTeco.Key;
            _settingsDeviceCodeTextBox.Text = settings.Sync.DeviceCode;
            _settingsIntervalHoursTextBox.Text = settings.Sync.IntervalHours.ToString();
            _settingsStartTimeTextBox.Text = settings.Sync.StartTime;
            _settingsEndTimeTextBox.Text = settings.Sync.EndTime;
            _settingsLogPathTextBox.Text = settings.Logging.LogPath;
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(_settingsZkPortTextBox.Text, out var port))
            {
                MessageBox.Show("Port phải là số.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(_settingsIntervalHoursTextBox.Text, out var intervalHours))
            {
                MessageBox.Show("Giờ sync phải là số.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!TimeSpan.TryParse(_settingsStartTimeTextBox.Text, out _) || !TimeSpan.TryParse(_settingsEndTimeTextBox.Text, out _))
            {
                MessageBox.Show("Giờ bắt đầu/kết thúc sync không đúng định dạng (HH:mm).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var settings = _appSettings.Settings;
            settings.Database.Server = _settingsServerTextBox.Text.Trim();
            settings.Database.DbName = _settingsDbNameTextBox.Text.Trim();
            settings.Database.UserId = _settingsUserIdTextBox.Text.Trim();
            _appSettings.SetPassword(_settingsPasswordTextBox.Text);
            settings.ZKTeco.IP = _settingsZkIpTextBox.Text.Trim();
            settings.ZKTeco.Port = port;
            settings.ZKTeco.Key = _settingsZkKeyTextBox.Text.Trim();
            settings.Sync.DeviceCode = _settingsDeviceCodeTextBox.Text.Trim();
            settings.Sync.IntervalHours = intervalHours;
            settings.Sync.StartTime = _settingsStartTimeTextBox.Text.Trim();
            settings.Sync.EndTime = _settingsEndTimeTextBox.Text.Trim();
            settings.Logging.LogPath = _settingsLogPathTextBox.Text.Trim();

            try
            {
                _appSettings.Save();

                ApplyRuntimeSettings();
                _databaseService = new DatabaseService(_appSettings.GetConnectionString(), _logger);

                _logger.Information("Đã cập nhật cấu hình hệ thống");
                MessageBox.Show("Đã lưu cài đặt.", "Cài đặt", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save settings", ex);
                MessageBox.Show($"Lỗi khi lưu cài đặt: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowMainForm()
        {
            this.Opacity = 1;
            this.ShowInTaskbar = true;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
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
            var now = DateTime.Now.TimeOfDay;

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
                SetNormalIcon();

                // Only fetch logs newer than what's already in the database
                var (fromDate, toDate) = await GetSyncDateRangeAsync();

                // Fetch logs from ZKTeco
                var (logs, _, _) = await Task.Run(() => FetchAttendanceLogsFromDevice(fromDate, toDate));

                if (logs.Count == 0)
                {
                    _logger.Information($"Đọc dữ liệu từ {fromDate:yyyy-MM-dd HH:mm:ss}: 0 dòng");
                    _notifyIcon.Text = "AttendanceConnect - No new logs";
                    return;
                }

                // Insert to database
                var insertedCount = await _databaseService.InsertAttendanceLogsAsync(logs);

                _logger.Information($"Đọc dữ liệu từ {fromDate:yyyy-MM-dd HH:mm:ss}: {logs.Count} dòng + insert DB thành công: {insertedCount} dòng");

                // Update last sync time
                File.WriteAllText(
                    Path.Combine(Application.StartupPath, "last_sync.txt"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

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

        private async Task<(DateTime FromDate, DateTime ToDate)> GetSyncDateRangeAsync()
        {
            var lastVerifyDate = await _databaseService.GetLastVerifyDateAsync() ?? DateTime.Now.AddYears(-1);
            var fromDate = lastVerifyDate.AddSeconds(1);
            var toDate = DateTime.Now.AddDays(1);
            return (fromDate, toDate);
        }

        private async void BtnSyncTime_Click(object? sender, EventArgs e)
        {
            _btnSyncTime.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var deviceTime = await Task.Run(SyncDeviceTimeWithComputer);

                if (deviceTime.HasValue)
                {
                    _deviceTimeLabel.Text = deviceTime.Value.ToString("HH:mm:ss dd/MM/yyyy");
                    _logger.Information($"Đồng bộ giờ máy chấm công thành công: {deviceTime.Value:yyyy-MM-dd HH:mm:ss}");
                    MessageBox.Show("Đồng bộ giờ máy chấm công thành công.", "Đồng bộ giờ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Đồng bộ giờ thất bại: {_zkTecoHelper.LastErrorMessage}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to sync device time", ex);
                MessageBox.Show($"Lỗi khi đồng bộ giờ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _btnSyncTime.Enabled = true;
            }
        }

        private DateTime? SyncDeviceTimeWithComputer()
        {
            var connectResult = _zkTecoHelper.ConnectDevice(_zkTecoIp, _zkTecoPort, _zkTecoCommKey);
            if (connectResult != 1)
            {
                _logger.Error($"Failed to connect to ZKTeco device {_zkTecoIp}:{_zkTecoPort} - {_zkTecoHelper.LastErrorMessage}");
                return null;
            }

            try
            {
                if (_zkTecoHelper.SetDeviceTime(DateTime.Now) != 1)
                {
                    _logger.Error($"Failed to set device time: {_zkTecoHelper.LastErrorMessage}");
                    return null;
                }

                var newDeviceTime = _zkTecoHelper.GetDeviceTime();
                return newDeviceTime == DateTime.MinValue ? null : newDeviceTime;
            }
            finally
            {
                _zkTecoHelper.DisConnect();
            }
        }

        private async void BtnReadData_Click(object? sender, EventArgs e)
        {
            _btnReadData.Enabled = false;
            _btnUpdate.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var (fromDate, toDate) = await GetSyncDateRangeAsync();
                var (logs, deviceTime, pcTime) = await Task.Run(() => FetchAttendanceLogsFromDevice(fromDate, toDate));
                var staffNames = await _databaseService.GetStaffNamesAsync();

                _pcTimeLabel.Text = pcTime.ToString("HH:mm:ss dd/MM/yyyy");
                _deviceTimeLabel.Text = deviceTime.HasValue ? deviceTime.Value.ToString("HH:mm:ss dd/MM/yyyy") : "Không đọc được";

                _logger.Information($"Đọc dữ liệu từ {fromDate:yyyy-MM-dd HH:mm:ss}: {logs.Count} dòng");

                _pendingLogs = logs;
                PopulateLogsGrid(logs, staffNames);

                MessageBox.Show($"Đã đọc {logs.Count} log mới từ máy chấm công.", "Đọc dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to read attendance data", ex);
                MessageBox.Show($"Lỗi khi đọc dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _btnReadData.Enabled = true;
                _btnUpdate.Enabled = _pendingLogs.Count > 0;
            }
        }

        private async void BtnUpdate_Click(object? sender, EventArgs e)
        {
            if (_pendingLogs.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để cập nhật. Vui lòng đọc dữ liệu trước.", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnReadData.Enabled = false;
            _btnUpdate.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                var insertedCount = await _databaseService.InsertAttendanceLogsAsync(_pendingLogs);

                _logger.Information($"Insert DB thành công: {insertedCount} dòng");

                File.WriteAllText(
                    Path.Combine(Application.StartupPath, "last_sync.txt"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                MessageBox.Show($"Đã cập nhật {insertedCount}/{_pendingLogs.Count} log lên phần mềm chấm công.", "Cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _pendingLogs = new List<AttendanceLog>();
                _logsGrid.Rows.Clear();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to update attendance data to database", ex);
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                _btnReadData.Enabled = true;
                _btnUpdate.Enabled = _pendingLogs.Count > 0;
            }
        }

        private void PopulateLogsGrid(List<AttendanceLog> logs, Dictionary<string, string> staffNames)
        {
            _logsGrid.Rows.Clear();

            foreach (var log in logs)
            {
                var fullName = staffNames.TryGetValue(log.UserId.ToString(), out var name) ? name : "";
                _logsGrid.Rows.Add(log.UserId, log.VerifyDate, fullName, log.VerifyType, log.VerifyState);
            }
        }

        private (List<AttendanceLog> Logs, DateTime? DeviceTime, DateTime PcTime) FetchAttendanceLogsFromDevice(DateTime fromDate, DateTime toDate)
        {
            var logs = new List<AttendanceLog>();
            DateTime? deviceTime = null;
            var pcTime = DateTime.Now;

            var connectResult = _zkTecoHelper.ConnectDevice(_zkTecoIp, _zkTecoPort, _zkTecoCommKey);
            if (connectResult != 1)
            {
                _logger.Error($"Failed to connect to ZKTeco device {_zkTecoIp}:{_zkTecoPort} - {_zkTecoHelper.LastErrorMessage}");
                return (logs, deviceTime, pcTime);
            }

            _logger.Information($"Kết nối đến máy chấm công thành công lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}");

            try
            {
                // Read device time and PC time together (same moment) so they can be compared.
                var rawDeviceTime = _zkTecoHelper.GetDeviceTime();
                pcTime = DateTime.Now;
                deviceTime = rawDeviceTime == DateTime.MinValue ? null : rawDeviceTime;

                var table = _zkTecoHelper.ReadLogByPeriod(fromDate, toDate);
                if (table == null)
                {
                    if (!string.IsNullOrEmpty(_zkTecoHelper.LastErrorMessage))
                        _logger.Error($"Failed to read attendance logs from ZKTeco device: {_zkTecoHelper.LastErrorMessage}");

                    return (logs, deviceTime, pcTime);
                }

                foreach (DataRow row in table.Rows)
                {
                    if (!int.TryParse(row["UserID"].ToString(), out var userId))
                        continue;

                    logs.Add(new AttendanceLog
                    {
                        UserId = userId,
                        VerifyDate = (DateTime)row["VerifyDate"],
                        VerifyType = (int)row["VerifyType"],
                        VerifyState = (int)row["VerifyState"],
                        WorkCode = (int)row["WorkCode"],
                        DeviceCode = _deviceCode,
                        ImportedAt = DateTime.Now
                    });
                }
            }
            finally
            {
                _zkTecoHelper.DisConnect();
            }

            return (logs, deviceTime, pcTime);
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
                this.Opacity = 0;
                this.ShowInTaskbar = false;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}
