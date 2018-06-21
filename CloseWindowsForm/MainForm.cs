using ConsoleCloseWindows;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;

namespace CloseWindowsForm
{
    public partial class MainForm : Form
    {
        string AppName = "CloseWindowsForm";
        string AppFile = Application.ExecutablePath;

        #region Attribute Api
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int IsWindow(IntPtr hWnd);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool IsWindowVisible(IntPtr hwnd);
        #endregion

        public MainForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            SetStartup();

            System.Timers.Timer timer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 1000 //执行间隔时间,单位为毫秒; 这里实际间隔为1s 
            };
            timer.Start();
            timer.Elapsed += new ElapsedEventHandler(CloseWindowTimerEvent);

        }

        #region 方法

        #region 显示
        /// <summary>
        /// 显示
        /// </summary>
        /// <param name="s"></param>
        private void UICmd(string s)
        {
            BeginInvoke(new Action(() =>
            {
                tb_Cmd.Text += s;
                tb_Cmd.Text += Environment.NewLine;
                tb_Cmd.Select(tb_Cmd.Text.Length, 0);
                tb_Cmd.ScrollToCaret();
            }));
        }
        #endregion

        #region 按钮设置为开机启动
        private void btn_Add_Click(object sender, EventArgs e)
        {
            #region 所有用户
            //string regAll = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            //if (RegisterTool.SetValue(regAll, AppName, AppFile))
            //{
            //    UICmd("添加 注册表全局用户启动 成功");
            //}
            //else
            //{
            //    UICmd("添加 注册表全局用户启动 失败");
            //}
            #endregion
            string regCurrent = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
            if (RegisterTool.SetValue(regCurrent, AppName, AppFile))
            {
                UICmd("添加 注册表当前用户开机启动 成功");
            }
            else
            {
                UICmd("添加 注册表当前用户开机启动 失败");
                Logger.Logger.Default.Error("添加 注册表当前用户开机启动 失败");
            }

        }

        private void SetStartup()
        {
            string regCurrent = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
            if (RegisterTool.SetValue(regCurrent, AppName, AppFile))
            {
                UICmd("该程序为开机自启动程序");
            }
            else
            {
                Logger.Logger.Default.Error("添加 注册表当前用户开机启动 失败");
            }
        }

        #endregion

        #region 按钮关闭开机启动
        private void btn_Del_Click(object sender, EventArgs e)
        {
            #region 所有用户
            //string regAll = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            //if (RegisterTool.DeleteValue(regAll, AppName))
            //    UICmd("删除 注册表全局用户启动 成功");
            //else
            //    UICmd("删除 注册表全局用户启动 失败");
            #endregion

            string regCurrent = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
            if (RegisterTool.DeleteValue(regCurrent, AppName))
            {
                UICmd("删除 注册表当前用户启动 成功");
            }
            else
            { 
                UICmd("删除 注册表当前用户启动 失败");
                Logger.Logger.Default.Error("删除 注册表当前用户启动 失败");
            }
        }
        #endregion

        #endregion

        #region 事件

        #region 关闭Windows10UpgraderApp.exe窗口事件
        /// <summary>
        /// 关闭Windows10UpgraderApp.exe窗口事件
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void CloseWindowTimerEvent(object source, ElapsedEventArgs e)
        {
            IntPtr maindHwnd = FindWindow(null, "Windows10UpgraderApp.exe");
            if (maindHwnd != IntPtr.Zero)
            {
                if (IsWindowVisible(maindHwnd))
                {
                    ShowWindow(maindHwnd, 0);
                    UICmd("在" + DateTime.Now.ToString() + "时刻关闭 Windows10UpgraderApp.exe 文件夹一次");
                    Logger.Logger.Default.Info("在" + DateTime.Now.ToString() + "时刻关闭 Windows10UpgraderApp.exe 文件夹一次");
                }
            }
        }
        #endregion

        #region 输入密码按下
        private void tb_Pwd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (tb_Pwd.Text == "password")
                {
                    btn_Add.Enabled = true;
                    btn_Del.Enabled = true;
                }
            }
        }
        #endregion

        #region 托盘双击事件
        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //还原窗体显示    
                WindowState = FormWindowState.Normal;
                //激活窗体并给予它焦点
                this.Activate();
                //任务栏区显示图标
                this.ShowInTaskbar = true;
            }
        }
        #endregion

        #region 窗口size状态改变事件
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标
                this.ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon.Visible = true;
            }
        }
        #endregion

        #region 主窗口退出事件
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;    //取消"关闭窗口"事件
                this.WindowState = FormWindowState.Minimized;    //使关闭时窗口向右下角缩小的效果
                notifyIcon.Visible = true;
                this.ShowInTaskbar = false;
                return;
            }
        }
        #endregion

        #region 托盘点击显示
        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }
        #endregion

        #region 托盘点击退出
        private void 退出ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                this.Dispose();
                this.Close();
            }
        }
        #endregion

        #endregion

    }
}
