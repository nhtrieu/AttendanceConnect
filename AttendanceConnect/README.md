# AttendanceConnect - ZKTeco Attendance Sync Service

Desktop Windows application (WinForms, .NET Framework 4.8) để đồng bộ log chấm công từ máy chấm công ZKTeco vào SQL Server database.

## Tính năng

✅ **System Tray UI**: Chạy ở background với icon ở system tray, double-click để mở giao diện chính
✅ **Tab "Chấm công"**: Đọc dữ liệu mới từ máy chấm công, xem trước trên lưới, cập nhật lên database; đồng bộ giờ máy tính ↔ máy chấm công
✅ **Tab "Xem logs"**: Xem log theo tuần (Thứ 2 - Chủ nhật), điều hướng tuần trước/sau
✅ **Tab "Cài đặt"**: Sửa toàn bộ cấu hình (DB, máy chấm công, giờ sync) ngay trong app, yêu cầu mật khẩu quản trị
✅ **Tự động Sync**: Chạy tự động theo lịch (mặc định mỗi 2 giờ, 8:30 - 20:00), chỉ lấy log mới hơn lần sync gần nhất
✅ **Execute Now**: Menu chuột phải tray icon để chạy sync ngay lập tức
✅ **Logging**: Log theo tuần, chỉ ghi các bước thành công chính + lỗi (không log rác)
✅ **Lightweight**: Không phụ thuộc nhiều thư viện ngoài

## Yêu cầu

- Windows (7 SP1 / 8.1 / 10 / 11 / Server tương ứng) có .NET Framework 4.8
- SQL Server (bảng `tblHR_AttendanceLogs` và view `vwAL_StaffAttendance` đã tồn tại)
- Máy chấm công ZKTeco + SDK `zkemkeeper.dll` đã đăng ký trên máy chạy app (xem mục Troubleshooting)

## Cấu hình

Cấu hình lưu trong **`AttendanceConnect.exe.config`** (sinh ra từ `App.config` lúc build), phần `<appSettings>`:

```xml
<appSettings>
  <add key="Database.Server" value="YOUR_SERVER" />
  <add key="Database.DbName" value="YOUR_DATABASE" />
  <add key="Database.UserId" value="YOUR_USER" />
  <add key="Database.Password" value="BASE64_ENCODED_PASSWORD" />

  <add key="ZKTeco.IP" value="192.168.1.100" />
  <add key="ZKTeco.Port" value="8000" />
  <add key="ZKTeco.Key" value="Your_Device_Key_Here" />

  <add key="Sync.DeviceCode" value="DEVICE_001" />
  <add key="Sync.IntervalHours" value="2" />
  <add key="Sync.StartTime" value="08:30" />
  <add key="Sync.EndTime" value="20:00" />

  <add key="Logging.LogPath" value="./logs/attendance-{Date}.log" />
  <add key="Logging.LogLevel" value="Information" />
</appSettings>
```

**Cấu hình chi tiết:**
- `Database.Server/DbName/UserId/Password`: Thông tin kết nối SQL Server. `Password` lưu dạng **Base64** (chỉ để tránh hiển thị lộ liễu, không phải mã hóa bảo mật thật — IT có thể decode ngược bằng `base64 -d` hoặc `Convert.FromBase64String`).
- `ZKTeco.IP/Port/Key`: Địa chỉ, port, comm key của máy chấm công.
- `Sync.DeviceCode`: Mã máy chấm công (ghi vào database).
- `Sync.IntervalHours/StartTime/EndTime`: Khoảng và giờ hoạt động sync tự động.
- `Logging.LogPath`: Đường dẫn file log (1 file/tuần, Thứ 2 - Chủ nhật, `{Date}` là ngày Thứ 2 đầu tuần).

**Sửa qua UI**: mở app → double-click tray icon → tab **"Cài đặt"** → nhập mật khẩu quản trị (`Admin@123`, đổi trong `Form1.cs` nếu cần) → sửa → Lưu. Áp dụng ngay, không cần khởi động lại app.

### Database Schema

Ứng dụng insert vào bảng `tblHR_AttendanceLogs`:

```sql
CREATE TABLE [dbo].[tblHR_AttendanceLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[VerifyDate] [datetime2](0) NOT NULL,
	[VerifyType] [int] NULL,
	[VerifyState] [int] NULL,
	[WorkCode] [int] NULL,
	[DeviceCode] [varchar](20) NULL,
	[ImportedAt] [datetime2](0) NOT NULL
)
```

Và đọc tên nhân viên từ view `vwAL_StaffAttendance` (cột `AttendanceUserID`, `FullName`) để hiển thị trên lưới tab "Chấm công".

## Build & Run

### Build Release

```bash
dotnet build -c Release
```

Output nằm trong `bin/Release/net48/`.

### Chạy ứng dụng (dev)

```bash
dotnet run
```

hoặc sau khi build:

```bash
./bin/Release/net48/AttendanceConnect.exe
```

### Đóng gói bản setup

```powershell
.\Setup\Build-Package.ps1
```

Tạo `dist/AttendanceConnect-Setup.zip` gồm app + `Setup/Install.ps1` + `Setup/Uninstall.ps1`. Xem [Setup/Install.ps1](Setup/Install.ps1) để biết các bước cài (check .NET 4.8, check `zkemkeeper.dll`, copy file, tạo shortcut, auto-start).

## Cách sử dụng

1. **Khởi động ứng dụng**: chạy ở background, icon hiển thị ở system tray.
2. **Nhấp phải chuột trên icon**:
   - **Execute Now**: chạy sync ngay (đọc + insert DB).
   - **Exit**: thoát ứng dụng.
3. **Nhấp đôi vào icon**: mở form chính với 3 tab (Chấm công / Xem logs / Cài đặt).

### Icon trạng thái

- 🟢 Icon thường: ứng dụng chạy bình thường.
- 🔴 Icon đỏ: lỗi kết nối (ZKTeco hoặc Database) ở lần sync gần nhất.

## Log Files

Lưu tại đường dẫn `Logging.LogPath` (mặc định `./logs/attendance-{Date}.log`), **1 file/tuần** (Thứ 2 - Chủ nhật). Xem trực tiếp trong app (tab "Xem logs", có nút lùi/tới giữa các tuần) hoặc mở file text thường.

Log chỉ ghi các bước chính + lỗi, ví dụ một lần sync thành công:
```
2026-06-29 10:30:45.123 [INF] Kết nối đến máy chấm công thành công lúc 10:30:45 29/06/2026
2026-06-29 10:30:47.456 [INF] Đọc dữ liệu từ 2026-06-26 17:08:33: 161 dòng + insert DB thành công: 161 dòng
```

## Troubleshooting

### Lỗi: "Error loading configuration" / "Error initializing services"
- Kiểm tra `AttendanceConnect.exe.config` có tồn tại cạnh file exe và đúng cấu trúc `<appSettings>` không.

### Lỗi: "Failed to connect to ZKTeco device"
- Kiểm tra IP, Port, Key trong tab "Cài đặt" (hoặc `AttendanceConnect.exe.config`).
- Kiểm tra `zkemkeeper.dll` đã đăng ký trên máy chưa: `Test-Path "HKLM:\SOFTWARE\Classes\zkemkeeper.ZKEM.1"` phải trả `True`. Nếu chưa, copy `zkemkeeper.dll` (đúng bitness 32/64-bit theo Windows) vào `System32` và chạy `regsvr32 zkemkeeper.dll` (Admin).
- Kiểm tra firewall cho phép kết nối tới port máy chấm công (mặc định 4370).

### Lỗi: "Database connection failed"
- Kiểm tra Server/DbName/UserId/Password trong tab "Cài đặt".
- Kiểm tra SQL login có quyền truy cập đúng database (`db_datareader`/`db_datawriter`).
- Kiểm tra bảng `tblHR_AttendanceLogs` và view `vwAL_StaffAttendance` đã tồn tại.

### Không có log được insert
- Mở tab "Xem logs" kiểm tra chi tiết lỗi.
- Nếu sync chạy nhưng không có log, có thể máy chấm công không có log mới trong khoảng thời gian tính từ lần sync trước.

## Architecture

```
AttendanceConnect/
├── App.config                     # Config gốc (compile ra AttendanceConnect.exe.config)
├── Models/
│   ├── AttendanceLog.cs           # Data model log chấm công
│   ├── Employee.cs                # Data model nhân viên (ika_* port)
│   └── AppSettings.cs             # Model cấu hình mạnh kiểu
├── Services/
│   ├── ZKTecoHelper.cs            # Thư viện helper SDK ZKTeco (port từ ika_* legacy) - dùng cho phát triển sau
│   └── DatabaseService.cs         # Đọc/ghi SQL Server
├── Utilities/
│   ├── AppSettingsService.cs      # Đọc/ghi AttendanceConnect.exe.config
│   ├── SimpleLogger.cs            # Logger tự viết, rotate theo tuần
│   └── IconGenerator.cs           # Tạo icon cho system tray
├── Setup/
│   ├── Build-Package.ps1          # Build Release + đóng gói zip
│   ├── Install.ps1                # Cài đặt trên máy đích
│   └── Uninstall.ps1              # Gỡ cài đặt
├── libs/
│   └── Interop.ZKEMKEEPERLib.dll  # COM interop assembly (sinh từ tlbimp, cần track trong git)
├── Form1.cs                       # Main form: tray icon + 3 tab
└── Program.cs                     # Entry point
```

## Dependencies

- **System.Data.SqlClient**: SQL Server data provider
- **System.Configuration** (Framework assembly): đọc/ghi `AttendanceConnect.exe.config`
- **Interop.ZKEMKEEPERLib**: COM interop với SDK ZKTeco (`zkemkeeper.dll`, cần đăng ký trên máy chạy)

## Notes

- Sync tự động chỉ chạy trong khoảng giờ cấu hình (mặc định 8:30 - 20:00), và chỉ đọc log mới hơn `VerifyDate` lớn nhất đã có trong DB.
- Nếu kết nối đứt, icon tray chuyển sang đỏ nhưng ứng dụng vẫn chạy.
- Log không bị xóa trên máy chấm công sau khi đọc.
- Nếu UserID không khớp nhân viên trong `vwAL_StaffAttendance`, log vẫn được đọc/insert (cột Nhân viên hiển thị rỗng).
- File `last_sync.txt` track thời điểm sync/cập nhật DB gần nhất.

## License

Internal Use Only
