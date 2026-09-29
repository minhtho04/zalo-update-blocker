using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;

namespace ZaloBlocker.CLI
{
    internal class Program
    {
        private const string TargetProcessName = "Zalo";
        private const string Version = "2.0.1 (Terminal Edition)";
        
        // UTF-8 without BOM (Byte Order Mark) is mandatory for Electron / Node.js JSON.parse compatibility!
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if (args.Length > 0)
            {
                return RunCommandLineMode(args);
            }

            return RunInteractiveMenu();
        }

        #region Safe File I/O (BOM-Free)

        /// <summary>
        /// Đọc file text UTF-8 và tự động loại bỏ ký tự BOM (\uFEFF) nếu có
        /// </summary>
        private static string ReadConfigFile(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }
            return text;
        }

        /// <summary>
        /// Ghi file text chuẩn UTF-8 KHÔNG BOM để Electron/Node.js không bị SyntaxError
        /// </summary>
        private static void WriteConfigFile(string path, string content)
        {
            if (!string.IsNullOrEmpty(content) && content[0] == '\uFEFF')
            {
                content = content.Substring(1);
            }
            File.WriteAllText(path, content, Utf8NoBom);
        }

        #endregion

        #region CLI Mode

        private static int RunCommandLineMode(string[] args)
        {
            string command = args[0].ToLowerInvariant().TrimStart('-', '/');
            bool quiet = HasFlag(args, "q", "quiet");

            switch (command)
            {
                case "b":
                case "block":
                    return ExecuteBlock(quiet) ? 0 : 1;

                case "u":
                case "unblock":
                    return ExecuteUnblock(quiet) ? 0 : 1;

                case "s":
                case "status":
                    DisplayStatus();
                    return 0;

                case "k":
                case "kill":
                    int killed = KillZaloProcesses();
                    if (!quiet)
                    {
                        PrintColor(ConsoleColor.Green, string.Format("[+] Đã đóng {0} tiến trình Zalo.", killed));
                    }
                    return 0;

                case "backup":
                    string backupFile = BackupConfig(quiet);
                    return !string.IsNullOrEmpty(backupFile) ? 0 : 1;

                case "restore":
                    string restorePath = args.Length > 1 ? args[1] : null;
                    return ExecuteRestore(restorePath, quiet) ? 0 : 1;

                case "v":
                case "view":
                    ViewConfig();
                    return 0;

                case "h":
                case "help":
                case "?":
                    PrintHelp();
                    return 0;

                default:
                    PrintColor(ConsoleColor.Red, string.Format("[-] Lệnh không hợp lệ: {0}", args[0]));
                    PrintHelp();
                    return 1;
            }
        }

        private static bool HasFlag(string[] args, params string[] flags)
        {
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant().TrimStart('-', '/');
                foreach (string flag in flags)
                {
                    if (arg == flag.ToLowerInvariant()) return true;
                }
            }
            return false;
        }

        #endregion

        #region Interactive Mode

        private static int RunInteractiveMenu()
        {
            try
            {
                Console.Title = "Zalo Update Blocker - Terminal Edition";
            }
            catch { }

            while (true)
            {
                Console.Clear();
                PrintBanner();
                PrintMiniStatus();

                Console.WriteLine();
                PrintColor(ConsoleColor.Yellow, "  [DANH MỤC THAO TÁC]");
                Console.WriteLine("  " + new string('-', 48));
                PrintOption("1", "Chặn tự động cập nhật Zalo (Block Auto-Update)", ConsoleColor.Green);
                PrintOption("2", "Mở lại cập nhật Zalo (Unblock Auto-Update)", ConsoleColor.Cyan);
                PrintOption("3", "Sao lưu cấu hình config.json (Backup)", ConsoleColor.Magenta);
                PrintOption("4", "Khôi phục cấu hình từ bản sao lưu (Restore)", ConsoleColor.Yellow);
                PrintOption("5", "Đóng toàn bộ tiến trình Zalo (Kill Zalo)", ConsoleColor.Red);
                PrintOption("6", "Xem nội dung file cấu hình config.json", ConsoleColor.White);
                PrintOption("7", "Kiểm tra quyền Administrator & Khởi động lại", ConsoleColor.DarkCyan);
                PrintOption("0", "Thoát chương trình (Exit)", ConsoleColor.DarkGray);
                Console.WriteLine("  " + new string('-', 48));
                Console.Write("\n  👉 Nhập lựa chọn của bạn [0-7]: ");

                string choice = Console.ReadLine();
                if (choice == null) break;
                choice = choice.Trim();

                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        ExecuteBlock(false);
                        break;
                    case "2":
                        ExecuteUnblock(false);
                        break;
                    case "3":
                        BackupConfig(false);
                        break;
                    case "4":
                        InteractiveRestore();
                        break;
                    case "5":
                        int count = KillZaloProcesses();
                        PrintColor(ConsoleColor.Green, string.Format("  [+] Đã đóng thành công {0} tiến trình {1}.exe.", count, TargetProcessName));
                        break;
                    case "6":
                        ViewConfig();
                        break;
                    case "7":
                        RestartAsAdmin();
                        break;
                    case "0":
                    case "q":
                    case "exit":
                        PrintColor(ConsoleColor.Cyan, "  Cảm ơn bạn đã sử dụng Zalo Update Blocker! Tạm biệt.");
                        return 0;
                    default:
                        PrintColor(ConsoleColor.Red, "  [-] Lựa chọn không hợp lệ, vui lòng chọn lại!");
                        break;
                }

                Console.WriteLine();
                PrintColor(ConsoleColor.DarkGray, "  Nhấn phím bất kỳ để quay lại menu chính...");
                try { Console.ReadKey(true); } catch { }
            }

            return 0;
        }

        #endregion

        #region Core Business Actions

        public static bool ExecuteBlock(bool quiet)
        {
            if (!quiet) PrintColor(ConsoleColor.Cyan, "=== BẮT ĐẦU CHẶN CẬP NHẬT ZALO ===");

            // 1. Kiểm tra & tắt Zalo nếu đang chạy
            int killed = KillZaloProcesses();
            if (killed > 0 && !quiet)
            {
                PrintColor(ConsoleColor.Yellow, string.Format("  [!] Đã đóng {0} tiến trình Zalo đang chạy để tránh xung đột file cấu hình.", killed));
            }

            // 2. Tìm file config
            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Không tìm thấy file config.json tại:\n      {0}", path));
                return false;
            }

            // 3. Tự động sao lưu trước khi chỉnh sửa
            string backupPath = BackupConfig(true);
            if (!quiet && !string.IsNullOrEmpty(backupPath))
            {
                PrintColor(ConsoleColor.DarkGray, string.Format("  [*] Đã tự động tạo bản sao lưu an toàn tại:\n      {0}", backupPath));
            }

            // 4. Đọc & Cập nhật JSON
            try
            {
                string rawJson = ReadConfigFile(path);
                JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                Dictionary<string, object> dict = ser.DeserializeObject(rawJson) as Dictionary<string, object>;
                if (dict == null)
                {
                    PrintColor(ConsoleColor.Red, "  [-] Không thể phân tích cú pháp config.json (JSON không hợp lệ).");
                    return false;
                }

                // Cập nhật các khóa chặn update theo chuẩn
                dict["disable_auto_update"] = true;
                dict["zalo_installed"] = 1691996534469L;
                dict["shortcut-screenshot"] = "CommandOrControl+Alt+S";
                dict["ver_sc_cap"] = "1.0";
                dict["shortcut-screenshot-withoutZ"] = "CommandOrControl+Alt+A";
                dict["os_architecture"] = "64-bit";

                string serialized = ser.Serialize(dict);
                string formatted = FormatJson(serialized);

                WriteConfigFile(path, formatted);

                if (!quiet)
                {
                    PrintColor(ConsoleColor.Green, "  [✔] CHẶN CẬP NHẬT THÀNH CÔNG!");
                    PrintColor(ConsoleColor.White, "  [*] Đã ghi đè cấu hình: \"disable_auto_update\": true (UTF-8 No BOM)");
                    PrintColor(ConsoleColor.White, "  [*] Bây giờ bạn có thể mở lại Zalo mà không lo bị ép cập nhật.");
                }
                return true;
            }
            catch (Exception ex)
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Lỗi trong quá trình cập nhật file config: {0}", ex.Message));
                return false;
            }
        }

        public static bool ExecuteUnblock(bool quiet)
        {
            if (!quiet) PrintColor(ConsoleColor.Cyan, "=== MỞ LẠI CẬP NHẬT ZALO ===");

            int killed = KillZaloProcesses();
            if (killed > 0 && !quiet)
            {
                PrintColor(ConsoleColor.Yellow, string.Format("  [!] Đã đóng {0} tiến trình Zalo đang chạy.", killed));
            }

            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Không tìm thấy file config.json tại:\n      {0}", path));
                return false;
            }

            string backupPath = BackupConfig(true);
            if (!quiet && !string.IsNullOrEmpty(backupPath))
            {
                PrintColor(ConsoleColor.DarkGray, string.Format("  [*] Đã tạo bản sao lưu tại:\n      {0}", backupPath));
            }

            try
            {
                string rawJson = ReadConfigFile(path);
                JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                Dictionary<string, object> dict = ser.DeserializeObject(rawJson) as Dictionary<string, object>;
                if (dict == null)
                {
                    PrintColor(ConsoleColor.Red, "  [-] File config.json không hợp lệ.");
                    return false;
                }

                dict["disable_auto_update"] = false;

                string serialized = ser.Serialize(dict);
                string formatted = FormatJson(serialized);

                WriteConfigFile(path, formatted);

                if (!quiet)
                {
                    PrintColor(ConsoleColor.Green, "  [✔] ĐÃ MỞ LẠI CẬP NHẬT THÀNH CÔNG!");
                    PrintColor(ConsoleColor.White, "  [*] Đã cập nhật: \"disable_auto_update\": false (UTF-8 No BOM)");
                    PrintColor(ConsoleColor.White, "  [*] Zalo có thể kiểm tra và cập nhật phiên bản mới bình thường.");
                }
                return true;
            }
            catch (Exception ex)
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Lỗi: {0}", ex.Message));
                return false;
            }
        }

        public static string BackupConfig(bool quiet)
        {
            string sourceFile = ConfigFilePath;
            if (!File.Exists(sourceFile))
            {
                if (!quiet) PrintColor(ConsoleColor.Red, string.Format("  [-] Không tìm thấy file gốc tại:\n      {0}", sourceFile));
                return null;
            }

            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string backupDir = Path.Combine(desktopPath, "ZaloConfig_Backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destFileName = string.Format("config_backup_{0}.json", timestamp);
                string destPath = Path.Combine(backupDir, destFileName);

                string content = ReadConfigFile(sourceFile);
                WriteConfigFile(destPath, content);

                if (!quiet)
                {
                    PrintColor(ConsoleColor.Green, "  [✔] SAO LƯU THÀNH CÔNG!");
                    PrintColor(ConsoleColor.White, string.Format("  [*] Vị trí file: {0}", destPath));
                    FileInfo fi = new FileInfo(destPath);
                    PrintColor(ConsoleColor.DarkGray, string.Format("  [*] Dung lượng : {0} bytes", fi.Length));
                }
                return destPath;
            }
            catch (Exception ex)
            {
                if (!quiet) PrintColor(ConsoleColor.Red, string.Format("  [-] Lỗi sao lưu: {0}", ex.Message));
                return null;
            }
        }

        private static bool ExecuteRestore(string specificPath, bool quiet)
        {
            if (string.IsNullOrEmpty(specificPath))
            {
                PrintColor(ConsoleColor.Red, "  [-] Vui lòng cung cấp đường dẫn file backup để khôi phục.");
                return false;
            }

            if (!File.Exists(specificPath))
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] File backup không tồn tại: {0}", specificPath));
                return false;
            }

            int killed = KillZaloProcesses();
            if (killed > 0 && !quiet)
            {
                PrintColor(ConsoleColor.Yellow, string.Format("  [!] Đã đóng {0} tiến trình Zalo.", killed));
            }

            try
            {
                string content = ReadConfigFile(specificPath);
                WriteConfigFile(ConfigFilePath, content);

                if (!quiet)
                {
                    PrintColor(ConsoleColor.Green, "  [✔] KHÔI PHỤC CẤU HÌNH THÀNH CÔNG!");
                    PrintColor(ConsoleColor.White, string.Format("  [*] Đã nạp lại file từ: {0} (Đã làm sạch BOM)", specificPath));
                }
                return true;
            }
            catch (Exception ex)
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Lỗi khi khôi phục: {0}", ex.Message));
                return false;
            }
        }

        private static void InteractiveRestore()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string backupDir = Path.Combine(desktopPath, "ZaloConfig_Backup");

            if (!Directory.Exists(backupDir))
            {
                PrintColor(ConsoleColor.Yellow, "  [*] Chưa tìm thấy thư mục backup nào tại Desktop\\ZaloConfig_Backup.");
                return;
            }

            string[] backupFiles = Directory.GetFiles(backupDir, "config_backup_*.json");
            if (backupFiles.Length == 0)
            {
                PrintColor(ConsoleColor.Yellow, "  [*] Không có file sao lưu nào trong thư mục backup.");
                return;
            }

            Array.Sort(backupFiles);
            Array.Reverse(backupFiles);

            PrintColor(ConsoleColor.Cyan, "  Danh sách các bản sao lưu đã tìm thấy:");
            for (int i = 0; i < backupFiles.Length; i++)
            {
                FileInfo fi = new FileInfo(backupFiles[i]);
                Console.WriteLine(string.Format("    [{0}] {1} ({2} bytes, {3:yyyy-MM-dd HH:mm:ss})",
                    i + 1, fi.Name, fi.Length, fi.LastWriteTime));
            }
            Console.WriteLine("    [0] Hủy bỏ (Quay lại)");
            Console.Write("\n  Chọn bản sao lưu muốn khôi phục [0-" + backupFiles.Length + "]: ");
            string input = Console.ReadLine();
            int selectedIndex;
            if (int.TryParse(input, out selectedIndex) && selectedIndex >= 1 && selectedIndex <= backupFiles.Length)
            {
                ExecuteRestore(backupFiles[selectedIndex - 1], false);
            }
            else
            {
                PrintColor(ConsoleColor.DarkGray, "  [*] Đã hủy thao tác khôi phục.");
            }
        }

        public static void ViewConfig()
        {
            string path = ConfigFilePath;
            if (!File.Exists(path))
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] File config.json không tồn tại: {0}", path));
                return;
            }

            try
            {
                string rawJson = ReadConfigFile(path);
                JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                object parsed = ser.DeserializeObject(rawJson);
                string formatted = FormatJson(ser.Serialize(parsed));

                PrintColor(ConsoleColor.Cyan, string.Format("=== NỘI DUNG FILE: {0} ===", path));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(formatted);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Lỗi đọc file config.json: {0}", ex.Message));
            }
        }

        public static void DisplayStatus()
        {
            PrintBanner();
            PrintMiniStatus();
        }

        #endregion

        #region Process & System Helpers

        public static string ConfigFilePath
        {
            get
            {
                string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string zaloDataPath = Path.Combine(roamingPath, "ZaloData");
                return Path.Combine(zaloDataPath, "config.json");
            }
        }

        public static int GetZaloProcessCount()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName(TargetProcessName);
                return procs.Length;
            }
            catch
            {
                return 0;
            }
        }

        public static int KillZaloProcesses()
        {
            int count = 0;

            // Cách 1: Sử dụng Process API trực tiếp
            try
            {
                Process[] processes = Process.GetProcessesByName(TargetProcessName);
                foreach (Process p in processes)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1500);
                        count++;
                    }
                    catch { }
                }
            }
            catch { }

            // Cách 2: Sử dụng WMI để quét sạch các tiến trình còn sót lại
            try
            {
                ManagementScope scope = new ManagementScope(@"\\.\root\cimv2");
                scope.Connect();
                ObjectQuery query = new ObjectQuery(string.Format("SELECT * FROM Win32_Process WHERE Name = '{0}.exe'", TargetProcessName));
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject process in searcher.Get())
                    {
                        try
                        {
                            uint res = (uint)process.InvokeMethod("Terminate", null);
                            if (res == 0) count++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return count;
        }

        public static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void RestartAsAdmin()
        {
            if (IsAdministrator())
            {
                PrintColor(ConsoleColor.Green, "  [✔] Ứng dụng đã và đang chạy với quyền Administrator tối cao!");
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.Verb = "runas";
                psi.UseShellExecute = true;
                Process.Start(psi);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                PrintColor(ConsoleColor.Red, string.Format("  [-] Không thể khởi động lại với quyền Admin: {0}", ex.Message));
            }
        }

        public static bool? CheckUpdateBlockStatus()
        {
            string path = ConfigFilePath;
            if (!File.Exists(path)) return null;

            try
            {
                string raw = ReadConfigFile(path);
                JavaScriptSerializer ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                Dictionary<string, object> dict = ser.DeserializeObject(raw) as Dictionary<string, object>;
                if (dict == null) return null;

                if (dict.ContainsKey("disable_auto_update"))
                {
                    object val = dict["disable_auto_update"];
                    if (val is bool) return (bool)val;
                    if (val != null)
                    {
                        bool parsed;
                        if (bool.TryParse(val.ToString(), out parsed)) return parsed;
                    }
                }
                return false;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region JSON Formatting Utility

        public static string FormatJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            StringBuilder sb = new StringBuilder(json.Length * 2);
            int indent = 0;
            bool inQuotes = false;
            bool isEscaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (inQuotes)
                {
                    sb.Append(ch);
                    if (ch == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                    }
                    else
                    {
                        if (ch == '\"' && !isEscaped)
                        {
                            inQuotes = false;
                        }
                        isEscaped = false;
                    }
                    continue;
                }

                if (ch == '\"')
                {
                    inQuotes = true;
                    isEscaped = false;
                    sb.Append(ch);
                    continue;
                }

                if (ch == '{' || ch == '[')
                {
                    sb.Append(ch);
                    sb.AppendLine();
                    indent += 2;
                    sb.Append(new string(' ', indent));
                    continue;
                }

                if (ch == '}' || ch == ']')
                {
                    sb.AppendLine();
                    indent = Math.Max(0, indent - 2);
                    sb.Append(new string(' ', indent));
                    sb.Append(ch);
                    continue;
                }

                if (ch == ',')
                {
                    sb.Append(ch);
                    sb.AppendLine();
                    sb.Append(new string(' ', indent));
                    continue;
                }

                if (ch == ':')
                {
                    sb.Append(": ");
                    continue;
                }

                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }

                sb.Append(ch);
            }
            return sb.ToString();
        }

        #endregion

        #region UI & Display Helpers

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"  ╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine(@"  ║             ⚡ ZALO UPDATE BLOCKER [TERMINAL v2.0] ⚡          ║");
            Console.WriteLine(@"  ║             Tool chặn Zalo PC tự động cập nhật bản mới         ║");
            Console.WriteLine(@"  ║  Forked by: Minji No Support | Original: ThanhNguyenVN93       ║");
            Console.WriteLine(@"  ╚════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        private static void PrintMiniStatus()
        {
            Console.WriteLine();
            PrintColor(ConsoleColor.Yellow, "  [TRẠNG THÁI HIỆN TẠI HỆ THỐNG]");

            // 1. Tiến trình Zalo
            int procCount = GetZaloProcessCount();
            Console.Write("  • Tiến trình Zalo       : ");
            if (procCount > 0)
            {
                PrintColor(ConsoleColor.Red, string.Format("ĐANG CHẠY ({0} tiến trình)", procCount));
            }
            else
            {
                PrintColor(ConsoleColor.Green, "ĐÃ TẮT (Không chạy)");
            }

            // 2. Trạng thái chặn
            bool? isBlocked = CheckUpdateBlockStatus();
            Console.Write("  • Tự động cập nhật Zalo : ");
            if (!isBlocked.HasValue)
            {
                PrintColor(ConsoleColor.DarkGray, "Không xác định (Chưa có config.json)");
            }
            else if (isBlocked.Value)
            {
                PrintColor(ConsoleColor.Green, "🛡️  ĐÃ CHẶN (BLOCKED - An toàn)");
            }
            else
            {
                PrintColor(ConsoleColor.Yellow, "⚠️  ĐANG BẬT (UNBLOCKED - Sẽ tự update)");
            }

            // 3. Đường dẫn config
            Console.Write("  • File cấu hình         : ");
            if (File.Exists(ConfigFilePath))
            {
                PrintColor(ConsoleColor.White, ConfigFilePath);
            }
            else
            {
                PrintColor(ConsoleColor.Red, "Chưa tìm thấy (Hãy khởi chạy Zalo ít nhất 1 lần)");
            }

            // 4. Quyền Admin
            Console.Write("  • Quyền Administrator   : ");
            if (IsAdministrator())
            {
                PrintColor(ConsoleColor.Green, "Có (Administrator)");
            }
            else
            {
                PrintColor(ConsoleColor.DarkGray, "Người dùng chuẩn (User)");
            }
        }

        private static void PrintOption(string key, string text, ConsoleColor color)
        {
            Console.Write("   [");
            Console.ForegroundColor = color;
            Console.Write(key);
            Console.ResetColor();
            Console.Write("] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void PrintColor(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void PrintHelp()
        {
            PrintBanner();
            Console.WriteLine();
            PrintColor(ConsoleColor.Yellow, "  CÚ PHÁP SỬ DỤNG DÒNG LỆNH (CLI):");
            Console.WriteLine("    ZaloBlocker.exe [LỆNH / TÙY CHỌN]");
            Console.WriteLine();
            PrintColor(ConsoleColor.Yellow, "  DANH SÁCH LỆNH:");
            Console.WriteLine("    -b, --block        Tắt Zalo, tự động sao lưu và kích hoạt chặn update");
            Console.WriteLine("    -u, --unblock      Tắt Zalo, gỡ bỏ chặn (cho phép cập nhật lại)");
            Console.WriteLine("    -s, --status       Kiểm tra trạng thái Zalo và cấu hình hiện tại");
            Console.WriteLine("    -k, --kill         Đóng tất cả các tiến trình Zalo.exe đang chạy");
            Console.WriteLine("    --backup           Tạo bản sao lưu config.json ra Desktop");
            Console.WriteLine("    --restore <path>   Khôi phục file config từ đường dẫn backup chỉ định");
            Console.WriteLine("    -v, --view         Hiển thị toàn bộ nội dung file config.json");
            Console.WriteLine("    -q, --quiet        Chế độ im lặng (không hỏi/không pause, dùng cho script)");
            Console.WriteLine("    -h, --help         Hiển thị bảng trợ giúp này");
            Console.WriteLine();
            PrintColor(ConsoleColor.Yellow, "  VÍ DỤ:");
            Console.WriteLine("    ZaloBlocker.exe --block");
            Console.WriteLine("    ZaloBlocker.exe --status");
            Console.WriteLine("    ZaloBlocker.exe --unblock");
            Console.WriteLine();
            PrintColor(ConsoleColor.Cyan, "  Forked & Maintained by: Minji No Support");
            PrintColor(ConsoleColor.DarkGray, "  Original project by: ThanhNguyenVN93");
            PrintColor(ConsoleColor.DarkGray, "  https://github.com/ThanhNguyenVN93/Zalo-Update-Blocker");
        }

        #endregion
    }
}
