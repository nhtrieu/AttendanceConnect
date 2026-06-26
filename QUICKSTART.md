# Quick Start Guide - AttendanceConnect

## 1. Cấu hình nhanh (5 phút)

### Bước 1: Mở `AttendanceConnect/appsettings.json`

Chỉnh sửa các giá trị sau:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=sa;Password=YOUR_PASSWORD;"
},
"ZKTeco": {
  "IP": "192.168.1.100",
  "Port": 8000,
  "Key": "YOUR_DEVICE_KEY"
},
"Sync": {
  "DeviceCode": "DEVICE_001"
}
```

**Ví dụ cụ thể:**
```
Server=DESKTOP-ABC\SQLEXPRESS;Database=HRSystem;User Id=sa;Password=Admin123;
```

### Bước 2: Build project

```bash
cd AttendanceConnect
dotnet build
```

### Bước 3: Chạy ứng dụng

```bash
dotnet run
```

✅ Ứng dụng sẽ chạy ở background, icon hiển thị ở system tray

## 2. Sử dụng

### Nhấp phải chuột trên icon ở system tray:
- **Execute Now**: Chạy sync ngay
- **Exit**: Thoát

### Nhấp đôi chuột trên icon:
- Xem trạng thái, lần sync cuối

## 3. Kiểm tra

✅ Xem file log: `logs/attendance-{ngày hôm nay}.log`

✅ Kiểm tra database: Bảng `tblHR_AttendanceLogs` có dữ liệu mới không

## 4. Chế độ tự động

- Chạy mỗi **2 giờ** từ **08:30 - 20:00**
- Có thể thay đổi trong `appsettings.json`

## 5. Icon Trạng thái

- 🟢 Green = OK
- 🔴 Red = Lỗi (kiểm tra log)

---

**Cần trợ giúp?** Xem file `README.md` để biết chi tiết
