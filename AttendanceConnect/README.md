# AttendanceConnect - ZKTeco Attendance Sync Service

Desktop Windows application để đồng bộ log chấm công từ máy chấm công ZKTeco vào SQL Server database.

## Tính năng

✅ **System Tray UI**: Chạy ở background với icon ở system tray  
✅ **Tự động Sync**: Chạy tự động mỗi 2 giờ từ 8:30 - 20:00  
✅ **Execute Now**: Menu để chạy sync ngay lập tức  
✅ **Logging**: Log tất cả hoạt động (thành công/lỗi) vào file  
✅ **Error Detection**: Icon thay đổi thành đỏ khi có lỗi kết nối  
✅ **Lightweight**: Xây dựng với .NET Core 8, không cần dependencies phức tạp

## Yêu cầu

- Windows 10 / Windows Server 2019 trở lên
- .NET 8.0 Runtime hoặc SDK
- SQL Server (cơ sở dữ liệu tblHR_AttendanceLogs đã tồn tại)
- Máy chấm công ZKTeco với API accessible qua HTTP

## Cấu hình

### 1. Chỉnh sửa `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "ZKTeco": {
    "IP": "192.168.1.100",
    "Port": 8000,
    "Key": "Your_Device_Key_Here"
  },
  "Sync": {
    "DeviceCode": "DEVICE_001",
    "IntervalHours": 2,
    "StartTime": "08:30",
    "EndTime": "20:00"
  },
  "Logging": {
    "LogPath": "./logs/attendance-{Date}.log",
    "LogLevel": "Information"
  }
}
```

**Cấu hình chi tiết:**
- `DefaultConnection`: Connection string tới SQL Server
- `IP`: Địa chỉ IP của máy chấm công ZKTeco
- `Port`: Port API của máy chấm công (mặc định 8000)
- `Key`: Device key / password để kết nối ZKTeco
- `DeviceCode`: Mã máy chấm công (ghi vào database)
- `IntervalHours`: Khoảng thời gian sync (giờ)
- `StartTime`: Thời gian bắt đầu sync hàng ngày
- `EndTime`: Thời gian kết thúc sync hàng ngày
- `LogPath`: Đường dẫn file log (hỗ trợ rolling daily)

### 2. Database Schema

Ứng dụng sẽ insert vào bảng `tblHR_AttendanceLogs`:

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

## Build & Run

### Build Release

```bash
dotnet publish -c Release -o ./publish
```

### Chạy ứng dụng

```bash
dotnet run
```

hoặc sau khi publish:

```bash
./publish/AttendanceConnect.exe
```

## Cách sử dụng

1. **Khởi động ứng dụng**: Ứng dụng sẽ chạy ở background, icon hiển thị ở system tray
2. **Nhấp phải chuột trên icon** để xem menu:
   - **Execute Now**: Chạy sync ngay lập tức
   - **Exit**: Thoát ứng dụng
3. **Nhấp đôi trên icon** để xem trạng thái:
   - Interval sync hiện tại
   - Thời gian hoạt động
   - Trạng thái (Running/Error)
   - Lần sync cuối cùng

### Icon Trạng thái

- 🟢 **Green Circle**: Ứng dụng chạy bình thường
- 🔴 **Red Circle + !**: Lỗi kết nối (ZKTeco hoặc Database)

## Log Files

Log được lưu tại: `./logs/attendance-{Date}.log`

Mỗi ngày tạo file log mới. Format:
```
2026-06-26 10:30:45.123 +07:00 [INF] Starting attendance sync...
2026-06-26 10:30:46.456 +07:00 [INF] Successfully fetched 15 attendance logs from ZKTeco
2026-06-26 10:30:47.789 +07:00 [INF] Successfully inserted 15/15 attendance logs
```

## Troubleshooting

### Lỗi: "DefaultConnection not found in appsettings.json"
- Kiểm tra file `appsettings.json` có tồn tại và có section `ConnectionStrings` không

### Lỗi: "Failed to connect to ZKTeco device"
- Kiểm tra IP, Port, Key của máy chấm công trong `appsettings.json`
- Kiểm tra firewall cho phép kết nối tới port ZKTeco
- Kiểm tra API của ZKTeco có accessible không (endpoint `/api/logs` hoặc `/api/health`)

### Lỗi: "Database connection failed"
- Kiểm tra connection string SQL Server
- Kiểm tra server có accessible không
- Kiểm tra user/password có đúng không
- Kiểm tra database và bảng `tblHR_AttendanceLogs` có tồn tại không

### Không có log được insert
- Kiểm tra file log để xem chi tiết lỗi
- Nếu sync chạy nhưng không có log, có thể ZKTeco không có log mới

## Architecture

```
AttendanceConnect/
├── Models/
│   └── AttendanceLog.cs          # Data model
├── Services/
│   ├── ZKTecoService.cs          # Kết nối & fetch log từ ZKTeco
│   └── DatabaseService.cs        # Insert vào SQL Server
├── Utilities/
│   ├── LoggerSetup.cs            # Cấu hình Serilog
│   └── IconGenerator.cs          # Tạo icon cho system tray
├── Form1.cs                       # Main form với system tray
├── Program.cs                     # Entry point
└── appsettings.json               # Configuration file
```

## Dependencies

- **Microsoft.Data.SqlClient**: SQL Server data provider
- **Serilog**: Logging framework
- **Serilog.Sinks.File**: File sink cho Serilog
- **Microsoft.Extensions.Configuration.Json**: Configuration từ JSON

## Notes

- Ứng dụng chỉ sync trong khoảng thời gian được cấu hình (mặc định 8:30 - 20:00)
- Nếu kết nối đứt, icon sẽ chuyển sang đỏ nhưng ứng dụng vẫn chạy
- Log không bị xóa sau khi import, chỉ được đọc
- Nếu UserID không tồn tại trong hệ thống, vẫn được import (đây là log thô)
- File `last_sync.txt` được tạo để track lần sync cuối cùng

## License

Internal Use Only
