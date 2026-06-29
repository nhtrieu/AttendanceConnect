using System.Data;
using AttendanceConnect.Models;
using ZKEMKEEPERLib;

namespace AttendanceConnect.Services
{
    /// <summary>
    /// Ported from the ika_* methods in the legacy SDKHelper.cs (D:\Develop\Chamcong SDK\ZKTSDK\Helper\SDKHelper.cs),
    /// stripped of WinForms control dependencies, for reuse across future .NET projects.
    /// Connection is persistent (call ConnectDevice once, then reuse for multiple calls, then DisConnect).
    /// </summary>
    public class ZKTecoHelper
    {
        private readonly CZKEMClass _zkem = new();
        private bool _isConnected;
        private int _machineNumber = 1;

        public string LastMessage { get; private set; } = "";
        public string LastErrorMessage { get; private set; } = "";

        #region Connect

        public bool GetConnectState() => _isConnected;

        public int GetMachineNumber() => _machineNumber;

        public void SetMachineNumber(int number) => _machineNumber = number;

        private int GetLastError()
        {
            var errorCode = 0;
            _zkem.GetLastError(ref errorCode);
            return errorCode;
        }

        /// <summary>
        /// Connects if not currently connected; disconnects (and returns -2) if already connected.
        /// Matches the original ika_ConnectDevice toggle behavior.
        /// </summary>
        public int ConnectDevice(string ip, int port, int commKey)
        {
            LastErrorMessage = "";

            if (port <= 0 || port > 65535)
            {
                LastErrorMessage = "*Port illegal!";
                return -1;
            }

            if (commKey < 0 || commKey > 999999)
            {
                LastErrorMessage = "*CommKey illegal!";
                return -1;
            }

            _zkem.SetCommPassword(commKey);

            if (_isConnected)
            {
                _zkem.Disconnect();
                _isConnected = false;
                LastErrorMessage = "Device is disconnected!";
                return -2;
            }

            if (_zkem.Connect_Net(ip, port))
            {
                _isConnected = true;
                LastMessage = "Device is connected!";
                LastErrorMessage = "";
                return 1;
            }

            var errorCode = GetLastError();
            LastErrorMessage = $"*Unable to connect the device,ErrorCode={errorCode}";
            return errorCode;
        }

        public void DisConnect()
        {
            _zkem.Disconnect();
            _isConnected = false;
        }

        #endregion

        #region DeviceInfo

        public int GetDeviceInfo(out string deviceName, out string serialNumber, out string deviceMac, out string productTime)
        {
            deviceName = "";
            serialNumber = "";
            deviceMac = "";
            productTime = "";

            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            _zkem.EnableDevice(_machineNumber, false);

            _zkem.GetProductCode(_machineNumber, out deviceName);
            _zkem.GetDeviceMAC(_machineNumber, ref deviceMac);
            _zkem.GetSerialNumber(_machineNumber, out serialNumber);
            _zkem.GetDeviceStrInfo(_machineNumber, 1, out productTime);

            _zkem.EnableDevice(_machineNumber, true);

            LastMessage = "Get the device info successfully";
            LastErrorMessage = "";
            return 1;
        }

        public int GetCapacityInfo(out int adminCount, out int userCount, out int fpCount, out int recordCount, out int passwordCount, out int oplogCount, out int faceCount)
        {
            adminCount = 0;
            userCount = 0;
            fpCount = 0;
            recordCount = 0;
            passwordCount = 0;
            oplogCount = 0;
            faceCount = 0;

            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            _zkem.EnableDevice(_machineNumber, false);

            _zkem.GetDeviceStatus(_machineNumber, 2, ref userCount);
            _zkem.GetDeviceStatus(_machineNumber, 1, ref adminCount);
            _zkem.GetDeviceStatus(_machineNumber, 3, ref fpCount);
            _zkem.GetDeviceStatus(_machineNumber, 4, ref passwordCount);
            _zkem.GetDeviceStatus(_machineNumber, 5, ref oplogCount);
            _zkem.GetDeviceStatus(_machineNumber, 6, ref recordCount);
            _zkem.GetDeviceStatus(_machineNumber, 21, ref faceCount);

            _zkem.EnableDevice(_machineNumber, true);

            LastMessage = "Get the device capacity successfully";
            LastErrorMessage = "";
            return 1;
        }

        #endregion

        #region AttLogMng

        private static DataTable CreateLogTable()
        {
            var table = new DataTable();
            table.Columns.Add("UserID", typeof(string));
            table.Columns.Add("VerifyDate", typeof(DateTime));
            table.Columns.Add("VerifyType", typeof(int));
            table.Columns.Add("VerifyState", typeof(int));
            table.Columns.Add("WorkCode", typeof(int));
            return table;
        }

        private static void AddLogRow(DataTable table, string enrollNumber, int year, int month, int day,
            int hour, int minute, int second, int verifyMode, int inOutMode, int workCode)
        {
            var row = table.NewRow();
            row["UserID"] = enrollNumber;
            row["VerifyDate"] = new DateTime(year, month, day, hour, minute, second);
            row["VerifyType"] = verifyMode;
            row["VerifyState"] = inOutMode;
            row["WorkCode"] = workCode;
            table.Rows.Add(row);
        }

        public DataTable? ReadAllAttendanceLog()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return null;
            }

            _zkem.EnableDevice(_machineNumber, false);

            DataTable? logTable = null;

            if (_zkem.ReadGeneralLogData(_machineNumber))
            {
                logTable = CreateLogTable();
                var workCode = 0;
                while (_zkem.SSR_GetGeneralLogData(_machineNumber, out var enrollNumber, out var verifyMode,
                           out var inOutMode, out var year, out var month, out var day, out var hour,
                           out var minute, out var second, ref workCode))
                {
                    AddLogRow(logTable, enrollNumber, year, month, day, hour, minute, second, verifyMode, inOutMode, workCode);
                }
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                LastErrorMessage = errorCode != 0 ? $"*Read attlog failed,ErrorCode: {errorCode}" : "";
                if (errorCode == 0)
                    LastMessage = "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);

            return logTable;
        }

        public DataTable? ReadLogByPeriod(DateTime fromTime, DateTime toTime)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return null;
            }

            _zkem.EnableDevice(_machineNumber, false);

            DataTable? logTable = null;

            if (_zkem.ReadTimeGLogData(_machineNumber, fromTime.ToString("yyyy-MM-dd HH:mm:ss"), toTime.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                logTable = CreateLogTable();
                var workCode = 0;
                while (_zkem.SSR_GetGeneralLogData(_machineNumber, out var enrollNumber, out var verifyMode,
                           out var inOutMode, out var year, out var month, out var day, out var hour,
                           out var minute, out var second, ref workCode))
                {
                    AddLogRow(logTable, enrollNumber, year, month, day, hour, minute, second, verifyMode, inOutMode, workCode);
                }
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                LastErrorMessage = errorCode != 0 ? $"*Read attlog by period failed,ErrorCode: {errorCode}" : "";
                if (errorCode == 0)
                    LastMessage = "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);

            return logTable;
        }

        /// <summary>Reads only logs not yet returned by a previous ReadNewAttLog call (device-side tracked).</summary>
        public DataTable? ReadNewAttLog()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return null;
            }

            _zkem.EnableDevice(_machineNumber, false);

            DataTable? logTable = null;

            if (_zkem.ReadNewGLogData(_machineNumber))
            {
                logTable = CreateLogTable();
                var workCode = 0;
                while (_zkem.SSR_GetGeneralLogData(_machineNumber, out var enrollNumber, out var verifyMode,
                           out var inOutMode, out var year, out var month, out var day, out var hour,
                           out var minute, out var second, ref workCode))
                {
                    AddLogRow(logTable, enrollNumber, year, month, day, hour, minute, second, verifyMode, inOutMode, workCode);
                }
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                LastErrorMessage = errorCode != 0 ? $"*Read attlog by period failed,ErrorCode: {errorCode}" : "";
                if (errorCode == 0)
                    LastMessage = "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);

            return logTable;
        }

        public int DeleteAttLog()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            int ret;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.ClearGLog(_machineNumber))
            {
                _zkem.RefreshData(_machineNumber);
                ret = 1;
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                ret = errorCode;
                LastErrorMessage = errorCode != 0 ? $"*Delete attlog, ErrorCode: {errorCode}" : "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);
            return ret;
        }

        public int DeleteAttLogByPeriod(DateTime fromTime, DateTime toTime)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            int ret;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.DeleteAttlogBetweenTheDate(_machineNumber, fromTime.ToString("yyyy-MM-dd HH:mm:ss"), toTime.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                _zkem.RefreshData(_machineNumber);
                ret = 1;
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                ret = errorCode;
                LastErrorMessage = errorCode != 0 ? $"*Delete attlog by period failed,ErrorCode: {errorCode}" : "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);
            return ret;
        }

        public int DelOldAttLogFromTime(DateTime fromTime)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            int ret;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.DeleteAttlogByTime(_machineNumber, fromTime.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                _zkem.RefreshData(_machineNumber);
                ret = 1;
                LastErrorMessage = "";
            }
            else
            {
                var errorCode = GetLastError();
                ret = errorCode;
                LastErrorMessage = errorCode != 0 ? $"*Delete old attlog from time failed,ErrorCode: {errorCode}" : "No data from terminal returns!";
            }

            _zkem.EnableDevice(_machineNumber, true);
            return ret;
        }

        #endregion

        #region ClearData

        private int ClearDataFlag(int dataFlag, string successMessage, string failureLabel)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            int ret;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.ClearData(_machineNumber, dataFlag))
            {
                _zkem.RefreshData(_machineNumber);
                LastMessage = successMessage;
                LastErrorMessage = "";
                ret = 1;
            }
            else
            {
                var errorCode = GetLastError();
                LastErrorMessage = errorCode != 0 ? $"*{failureLabel} failed,ErrorCode={errorCode}" : "No data from terminal returns!";
                ret = errorCode;
            }

            _zkem.EnableDevice(_machineNumber, true);
            return ret;
        }

        public int ClearAllLogs() => ClearDataFlag(1, "All AttLogs have been cleared from terminal!", "ClearAllLogs");

        public int ClearAllFps() => ClearDataFlag(2, "All fp templates have been cleared from terminal!", "ClearAllFps");

        public int ClearAllUsers() => ClearDataFlag(5, "All users have been cleared from terminal!", "ClearAllUsers");

        public int ClearAllData()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            int ret;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.ClearKeeperData(_machineNumber))
            {
                _zkem.RefreshData(_machineNumber);
                LastMessage = "All Data have been cleared from terminal!";
                LastErrorMessage = "";
                ret = 1;
            }
            else
            {
                var errorCode = GetLastError();
                LastErrorMessage = errorCode != 0 ? $"*ClearAllData failed,ErrorCode={errorCode}" : "No data from terminal returns!";
                ret = errorCode;
            }

            _zkem.EnableDevice(_machineNumber, true);
            return ret;
        }

        #endregion

        #region UserMng

        private static string StripNullTerminator(string name)
        {
            var index = name.IndexOf('\0');
            return index > 0 ? name.Substring(0, index) : name;
        }

        public DataTable? GetAllUsers()
        {
            if (!_isConnected)
                return null;

            DataTable? userTable = null;
            _zkem.EnableDevice(_machineNumber, false);

            try
            {
                _zkem.ReadAllUserID(_machineNumber);

                userTable = new DataTable();
                userTable.Columns.Add("UserID", typeof(string));
                userTable.Columns.Add("Name", typeof(string));
                userTable.Columns.Add("Password", typeof(string));
                userTable.Columns.Add("Privilege", typeof(int));
                userTable.Columns.Add("CardNumber", typeof(string));

                while (_zkem.SSR_GetAllUserInfo(_machineNumber, out var enrollNumber, out var name, out var password, out var privilege, out _))
                {
                    _zkem.GetStrCardNumber(out var cardNumber);

                    var row = userTable.NewRow();
                    row["UserID"] = enrollNumber;
                    row["Name"] = string.IsNullOrEmpty(name) ? name : StripNullTerminator(name);
                    row["Password"] = password;
                    row["Privilege"] = privilege;
                    row["CardNumber"] = cardNumber ?? "";
                    userTable.Rows.Add(row);
                }

                LastErrorMessage = "";
            }
            catch
            {
                userTable = null;
            }
            finally
            {
                _zkem.EnableDevice(_machineNumber, true);
            }

            return userTable;
        }

        public List<Employee> GetEmployees()
        {
            var employees = new List<Employee>();

            if (!_isConnected)
                return employees;

            _zkem.EnableDevice(_machineNumber, false);

            try
            {
                _zkem.ReadAllUserID(_machineNumber);

                while (_zkem.SSR_GetAllUserInfo(_machineNumber, out var enrollNumber, out var name, out var password, out var privilege, out _))
                {
                    _zkem.GetStrCardNumber(out var cardNumber);

                    employees.Add(new Employee
                    {
                        Pin = enrollNumber,
                        Name = string.IsNullOrEmpty(name) ? name : StripNullTerminator(name),
                        Privilege = privilege,
                        Password = password,
                        CardNumber = cardNumber ?? ""
                    });
                }
            }
            catch
            {
                // ignored, matches legacy behavior of returning whatever was collected so far
            }
            finally
            {
                _zkem.EnableDevice(_machineNumber, true);
            }

            return employees;
        }

        public void SetEmployees(List<Employee> employees)
        {
            _zkem.EnableDevice(_machineNumber, false);

            try
            {
                var batchUpdate = _zkem.BeginBatchUpdate(_machineNumber, 1);

                foreach (var employee in employees)
                {
                    _zkem.SetStrCardNumber(employee.CardNumber);
                    _zkem.SSR_SetUserInfo(_machineNumber, employee.Pin, employee.Name, employee.Password, employee.Privilege, true);
                }

                if (batchUpdate)
                    _zkem.BatchUpdate(_machineNumber);
            }
            catch
            {
                // ignored, matches legacy behavior
            }
            finally
            {
                _zkem.EnableDevice(_machineNumber, true);
            }
        }

        public int DeleteEnrollData(string userId, int backupNumber)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (_zkem.SSR_DeleteEnrollData(_machineNumber, userId, backupNumber))
            {
                _zkem.RefreshData(_machineNumber);
                LastMessage = $"SSR_DeleteEnrollData,UserID={userId} BackupNumber={backupNumber}";
                LastErrorMessage = "";
                return 1;
            }

            var errorCode = GetLastError();
            LastErrorMessage = errorCode == 0 && backupNumber == 11
                ? $"SSR_DeleteEnrollData,UserID={userId} BackupNumber={backupNumber}"
                : $"*Operation failed,ErrorCode={errorCode}";
            return 0;
        }

        public int DelUserTmp(string userId, int fingerIndex)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (string.IsNullOrWhiteSpace(userId) || fingerIndex < 0)
            {
                LastErrorMessage = "*Please input data first!";
                return -1023;
            }

            if (_zkem.SSR_DelUserTmpExt(_machineNumber, userId, fingerIndex))
            {
                _zkem.RefreshData(_machineNumber);
                LastMessage = $"SSR_DelUserTmpExt,UserID:{userId} FingerIndex:{fingerIndex}";
                LastErrorMessage = "";
                return 1;
            }

            LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            return 0;
        }

        public int SetUserInfo(string userId, string name, int privilege, string cardNumber, string password)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                LastErrorMessage = "*Please input data first!";
                return -1023;
            }

            if (privilege == 5)
            {
                LastErrorMessage = "*User Defined Role is Error! Please Register again!";
                return -1023;
            }

            _zkem.GetSysOption(_machineNumber, "~PIN2Width", out var pin2WidthStr);
            var pin2Width = Convert.ToInt32(pin2WidthStr);
            _zkem.GetSysOption(_machineNumber, "~IsABCPinEnable", out var abcPinEnableStr);
            var isAbcPinEnable = Convert.ToInt32(abcPinEnableStr);
            _zkem.GetSysOption(_machineNumber, "~T9FunOn", out var t9FunOnStr);
            var t9FunOn = Convert.ToInt32(t9FunOnStr);

            if (userId.Length > pin2Width)
            {
                LastErrorMessage = $"*User ID error! The max length is {pin2Width}";
                return -1022;
            }

            if (isAbcPinEnable == 0 || t9FunOn == 0)
            {
                if (userId.Substring(0, 1) == "0")
                {
                    LastErrorMessage = "*User ID error! The first letter can not be as 0";
                    return -1022;
                }

                foreach (var c in userId)
                {
                    if (!char.IsDigit(c))
                    {
                        LastErrorMessage = "*User ID error! User ID only support digital";
                        return -1022;
                    }
                }
            }

            var result = 0;
            _zkem.EnableDevice(_machineNumber, false);

            _zkem.SetStrCardNumber(cardNumber.Trim());
            if (_zkem.SSR_SetUserInfo(_machineNumber, userId.Trim(), name.Trim(), password.Trim(), privilege, true))
            {
                LastMessage = "Set user information successfully";
                LastErrorMessage = "";
                result = 1;
            }
            else
            {
                LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            }

            _zkem.RefreshData(_machineNumber);
            _zkem.EnableDevice(_machineNumber, true);

            return result;
        }

        public int GetUserInfo(string userId, out string name, out int privilege, out string cardNumber, out string password)
        {
            name = "";
            privilege = 0;
            cardNumber = "";
            password = "";

            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                LastErrorMessage = "*Please input user id first!";
                return -1023;
            }

            _zkem.GetSysOption(_machineNumber, "~PIN2Width", out var pin2WidthStr);
            var pin2Width = Convert.ToInt32(pin2WidthStr);

            if (userId.Length > pin2Width)
            {
                LastErrorMessage = $"*User ID error! The max length is {pin2Width}";
                return -1022;
            }

            var result = 0;
            _zkem.EnableDevice(_machineNumber, false);

            if (_zkem.SSR_GetUserInfo(_machineNumber, userId.Trim(), out name, out password, out privilege, out _))
            {
                _zkem.GetStrCardNumber(out cardNumber);
                if (cardNumber == "0")
                    cardNumber = "";

                LastMessage = "Get user information successfully";
                LastErrorMessage = "";
                result = 1;
            }
            else
            {
                var errorCode = GetLastError();
                name = " ";
                password = " ";
                cardNumber = " ";
                privilege = 5;
                LastErrorMessage = $"*Operation failed, the User is not exist. ErrorCode={errorCode}";
            }

            _zkem.EnableDevice(_machineNumber, true);
            return result;
        }

        #endregion

        #region DeviceTime

        public int SetDeviceTime(DateTime newDeviceTime)
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (_zkem.SetDeviceTime2(_machineNumber, newDeviceTime.Year, newDeviceTime.Month, newDeviceTime.Day,
                    newDeviceTime.Hour, newDeviceTime.Minute, newDeviceTime.Second))
            {
                _zkem.RefreshData(_machineNumber);
                LastMessage = "Successfully set the time";
                LastErrorMessage = "";
                return 1;
            }

            LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            return 0;
        }

        public DateTime GetDeviceTime()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return DateTime.MinValue;
            }

            var year = 0;
            var month = 0;
            var day = 0;
            var hour = 0;
            var minute = 0;
            var second = 0;

            if (_zkem.GetDeviceTime(_machineNumber, ref year, ref month, ref day, ref hour, ref minute, ref second))
            {
                LastMessage = "Get device time successfully";
                LastErrorMessage = "";
                return new DateTime(year, month, day, hour, minute, second);
            }

            LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            return DateTime.MinValue;
        }

        #endregion

        #region Power

        public int RestartDevice()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (_zkem.RestartDevice(_machineNumber))
            {
                LastErrorMessage = "";
                DisConnect();
            }
            else
            {
                LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            }

            return 1;
        }

        public int PowerOffDevice()
        {
            if (!_isConnected)
            {
                LastErrorMessage = "*Please connect first!";
                return -1024;
            }

            if (_zkem.PowerOffDevice(_machineNumber))
            {
                LastErrorMessage = "";
                DisConnect();
            }
            else
            {
                LastErrorMessage = $"*Operation failed,ErrorCode={GetLastError()}";
            }

            return 1;
        }

        #endregion
    }
}
