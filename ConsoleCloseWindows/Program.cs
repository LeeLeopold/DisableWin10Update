using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;

namespace ConsoleCloseWindows
{
    class Program
    {
        
        static void Main(string[] args)
        {
            string AppName = "CloseWindowsForm";
            string regCurrent = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
            if (RegisterTool.DeleteValue(regCurrent, AppName))
            {
                Console.WriteLine("删除 注册表当前用户启动 成功");
            }
            else
            {
                Console.WriteLine("删除 注册表当前用户启动 失败");
                Logger.Logger.Default.Error("删除 注册表当前用户启动 失败");
            }
        }

    }
}
