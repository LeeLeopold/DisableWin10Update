using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using TaskScheduler;

namespace WindowsService
{
    public partial class Service : ServiceBase
    {
        #region 字符串常量 (路径相关)
        private static readonly string BasePath = @"C:\Windows10Upgrade";
        private static readonly string FilePath = @"C:\Windows10Upgrade\Windows10UpgraderApp.exe";
        private static readonly string FileName = "Windows10UpgraderApp.exe";
        #endregion

        public Service()
        {
            InitializeComponent();

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OptionWindows10Upgrade);
            timer.Interval = 1000;//每1秒执行一次  
            timer.Enabled = true;

            System.Timers.Timer timer1 = new System.Timers.Timer();
            timer1.Elapsed += new System.Timers.ElapsedEventHandler(StopWindowsUpdate);
            timer1.Interval = 1000 * 60 * 60 * 1;//每1h执行一次  
            timer1.Enabled = true;
        }

        #region service 状态
        protected override void OnStart(string[] args)
        {
            Logger.Logger.Default.Info("在" + DateTime.Now.ToString() + "【DisableWin10UpdateService】 服务启动..");
            StopTask();
        }

        protected override void OnStop()
        {
            Logger.Logger.Default.Info("在" + DateTime.Now.ToString() + "【DisableWin10UpdateService】 服务停止..");
        }

        protected override void OnShutdown()
        {
            Logger.Logger.Default.Info("在" + DateTime.Now.ToString() + "【计算机关闭】");
        }
        #endregion

        #region Windows10Upgrade(易升)文件相关

        #region Windows10Upgrade 替换操作 @"C:\Windows10Upgrade\Windows10UpgraderApp.exe"
        /// <summary>
        ///  Windows10UpgraderApp.exe 替换和关闭操作
        /// </summary>
        private static void OptionWindows10Upgrade(object sender, System.Timers.ElapsedEventArgs e)
        {
            switch (CheckWindows10UpgradePath(FilePath))
            {
                // 是文件
                case 1:
                    File.Delete(FilePath);
                    Logger.Logger.Default.Info(FilePath + "是文件,且已被删除");
                    Directory.SetCurrentDirectory(BasePath);    // 将当前目录设为C:\Windows10Upgrade
                    Directory.CreateDirectory(FileName);        // 创建目录C:\Windows10Upgrade\Windows10UpgraderApp.exe
                    Logger.Logger.Default.Info(FilePath + "文件夹已创建");
                    break;
                // 是文件夹
                case 2:
                    break;
                // 路径尚不存在
                case 3:
                    //Logger.Default.Info(FilePath + "路径不存在");
                    break;
            }
        }
        #endregion

        #region 检查路径是文件、文件夹还是不存在
        /// <summary>
        /// 检查路径是文件、文件夹还是不存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static int CheckWindows10UpgradePath(string path)
        {
            if (File.Exists(path))
            {
                // 是文件
                return 1;
            }
            else if (Directory.Exists(path))
            {
                // 是文件夹
                return 2;
            }
            else
            {
                // 都不是【即,不存在】
                return 3;
            }
        }
        #endregion

        #endregion

        #region WindowsUpdate相关计划任务的禁用

        private static void StopWindowsUpdate(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopTask();
        }

        #region StopTask
        /// <summary>
        /// 1.引用：C:\Windows\System32\taskschd.dll
        /// 2.属性：嵌入互操作类型=False
        /// 3.命名空间：using TaskScheduler;
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/aa380751(v=vs.85).aspx
        /// 禁止WindowsUpdate相关的计划任务
        /// </summary>
        private static void StopTask()
        {
            #region 1.连接TaskSchedulerClass
            TaskSchedulerClass scheduler = new TaskSchedulerClass();
            scheduler.Connect();
            #endregion

            #region 2.获取计划任务文件夹
            // 和update相关的文件夹有两个 UpdateOrchestrator、WindowsUpdate
            List<ITaskFolder> taskFolders = new List<ITaskFolder>();
            ITaskFolder folder1 = scheduler.GetFolder("\\Microsoft\\Windows\\UpdateOrchestrator");
            ITaskFolder folder2 = scheduler.GetFolder("\\Microsoft\\Windows\\WindowsUpdate");
            taskFolders.Add(folder1);
            taskFolders.Add(folder2);
            #endregion

            #region 3.在文件夹内根据名称获取的计划任务并禁用
            //在UpdateOrchestrator中有:Schedule Scan、UpdateAssistant、UpdateAssistantCalendarRun、UpdateAssistantWakeupRun等
            //在WindowsUpdate中有:Automatic App Update、Scheduled Start、sih、sihboot

            foreach (ITaskFolder taskFolder in taskFolders)
            {
                foreach (IRegisteredTask task in taskFolder.GetTasks(1))
                {
                    var result = DisableResult(taskFolder, task.Name);
                    if (!result.Item1)
                    {
                        //Console.WriteLine(result.Item2);
                        Logger.Logger.Default.Error("在" + DateTime.Now.ToString() + result.Item2);
                    }
                    else
                    {
                        //Console.WriteLine(result.Item2);
                        Logger.Logger.Default.Info("在" + DateTime.Now.ToString() + result.Item2);
                    }
                }
            }
            #endregion
        }
        #endregion

        #region DisableResult
        /// <summary>
        /// 禁用task结果
        /// </summary>
        /// <param name="taskFolder">计划任务文件夹</param>
        /// <param name="taskName">计划任务名称</param>
        /// <returns>元祖bool, string</returns>
        private static Tuple<bool, string> DisableResult(ITaskFolder taskFolder, string taskName)
        {
            bool isSuccess = false;
            string resultMsg;
            try
            {
                IRegisteredTask task = taskFolder.GetTask(taskName);
                ITaskDefinition definition = task.Definition;
                for (int i = 1; i <= definition.Triggers.Count; i++)
                {
                    definition.Triggers[i].Enabled = false;
                }
                taskFolder.RegisterTaskDefinition(taskName, definition, (int)_TASK_CREATION.TASK_UPDATE,
                    "",//user
                    "",//password
                    _TASK_LOGON_TYPE.TASK_LOGON_NONE, "");
                task.Enabled = false;
                isSuccess = true;
                resultMsg = "计划任务【" + taskName + "】已成功禁用";
            }
            catch (UnauthorizedAccessException)
            {
                isSuccess = false;
                resultMsg = "计划任务【" + taskName + "】当前用户没有禁用权限";
            }
            catch (System.IO.FileNotFoundException)
            {
                isSuccess = true;
                resultMsg = "在这台PC上没有计划任务【" + taskName + "】";
            }
            catch (Exception ex)
            {
                isSuccess = false;
                resultMsg = "处理计划任务【" + taskName + "】时，发生错误:" + ex.ToString();
            }
            return Tuple.Create(isSuccess, resultMsg);
        }
        #endregion

        #endregion
    }
}
