using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace OLA
{
    public class IniHelper
    {
        // ==========================================
        // 1. INI 文件路径定义
        // ==========================================
        // INI 文件名，放在程序所在目录
        private static readonly string IniFileName = "config.ini";

        // 完整路径
        public static string IniPath
        {
            get
            {
                // 获取当前执行程序的目录
                string? exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (exePath == null)
                {
                    // Fallback, though should not happen in a standard WinForms app
                    exePath = Directory.GetCurrentDirectory();
                }
                return Path.Combine(exePath, IniFileName);
            }
        }

        // ==========================================
        // 2. Windows API 声明 (P/Invoke)
        // ==========================================

        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        public static extern int GetIniString(
            string lpAppName,       // Section (例如: "General")
            string lpKeyName,       // Key (例如: "BasePath")
            string lpDefault,       // 默认值 (如果找不到 Section 或 Key)
            StringBuilder lpReturnedString, // 用于接收读取结果的 StringBuilder
            int nSize,              // lpReturnedString 的最大容量
            string lpFileName       // INI 文件完整路径
        );

        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        public static extern bool WriteIniString(
            string lpAppName,       // Section (例如: "General")
            string lpKeyName,       // Key (例如: "BasePath")
            string lpString,        // 要写入的值
            string lpFileName       // INI 文件完整路径
        );

        // ==========================================
        // 3. 辅助方法
        // ==========================================

        public static string Read(string Section, string Key, string DefaultValue)
        {
            StringBuilder sb = new StringBuilder(500);
            GetIniString(Section, Key, DefaultValue, sb, sb.Capacity, IniPath);
            return sb.ToString();
        }

        public static bool Write(string Section, string Key, string Value)
        {
            return WriteIniString(Section, Key, Value, IniPath);
        }
    }
}