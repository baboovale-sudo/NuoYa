using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using OLAPlug; // 确保引用了 SDK 命名空间

namespace OLA
{
    public partial class Form1 : Form
    {
        // ==========================================
        // 🌍 全局配置中心 (Global Settings)
        // ==========================================
        public static class OLAConfig
        {
            // 1. 注册码配置 (请在此处修改你的注册码)
            public const string UserCode = "d841c28403974a56b31a74856542b6b7";
            public const string SoftCode = "c8285fc70089468f82cb927fee5fdf25";
            public const string Key = "OLA";

            // 2. 窗口绑定参数 (修改此处可全局生效)
            public const string Bind_Display = "gdi";
            public const string Bind_Mouse = "windows3";
            public const string Bind_Keypad = "windows";
            public const int Bind_Mode = 0;
        }

        // ==========================================
        // 🔥 API 定义 
        // ==========================================
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", ExactSpelling = true)]
        static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint GA_ROOT = 2;

        // 任务管理器
        Dictionary<int, TaskWorker> workers = new Dictionary<int, TaskWorker>();
        private CancellationTokenSource? _monitorToken;
        private bool _isMonitorActive = false;
        private bool _isRowAlreadySelected = false;
        private bool _isScriptRunning = false;
        private const string INI_SECTION = "Settings";

        public Form1()
        {
            InitializeComponent();
            InitializeSettings();
            this.moniqi_liebiao.ClearSelection();

            // 注册插件
            Register_OLA();

            this.moniqi_liebiao.ClearSelection();
            this.moniqi_liebiao.CurrentCell = null;

            this.renwu_liebiao.DoubleClick += new EventHandler(this.renwu_liebiao_DoubleClick);
            this.yixuan_renwu.DoubleClick += new EventHandler(this.yixuan_renwu_DoubleClick);
        }

        private void InitializeSettings()
        {
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
            if (this.moniqi_xuanze.Items.Count == 0)
            {
                this.moniqi_xuanze.Items.Add("雷电模拟器");
                this.moniqi_xuanze.Items.Add("MuMu模拟器");
                this.moniqi_xuanze.Items.Add("夜神模拟器");
            }
            this.moniqi_xuanze.SelectedIndexChanged += new EventHandler(this.moniqi_xuanze_SelectedIndexChanged);
            this.moniqi_xuanze.DropDown += new EventHandler(this.Control_Intercept_DropDown);
            this.moniqi_xuanze.SelectedIndex = 0;
            this.duokai_shuliang.Text = "2";
            this.duokai_shuliang.Enter += new EventHandler(this.Control_Intercept_Enter);
            this.duokai_shuliang.KeyPress += new KeyPressEventHandler(this.Control_Intercept_KeyPress);

            if (this.pailie_fangshi.Items.Count == 0)
            {
                this.pailie_fangshi.Items.Add("平铺排序");
                this.pailie_fangshi.Items.Add("隐藏窗口");
            }
            this.pailie_fangshi.SelectedIndex = 0;

            this.lujing_shuru.Enter += new EventHandler(this.Control_Intercept_Enter);
            this.lujing_shuru.KeyPress += new KeyPressEventHandler(this.Control_Intercept_KeyPress);
            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                cb.DropDown += new EventHandler(this.Control_Intercept_DropDown);
            }
            this.moniqi_liebiao.CellMouseDown += new DataGridViewCellMouseEventHandler(this.moniqi_liebiao_CellMouseDown);
            this.moniqi_liebiao.CellMouseUp += new DataGridViewCellMouseEventHandler(this.moniqi_liebiao_CellMouseUp);
            this.tingzhi_xuanzhong.Click += new EventHandler(this.tingzhi_xuanzhong_Click);
            this.zanting_suoyou.Click += new EventHandler(this.zanting_suoyou_Click);
            this.huifu_suoyou.Click += new EventHandler(this.huifu_suoyou_Click);

            this.queding_shezhi.Click += new EventHandler(this.queding_shezhi_Click);

            this.Load += new EventHandler(this.Form1_Load);

            if (this.renwu_liebiao.Items.Count == 0)
            {
                this.renwu_liebiao.Items.Add("主线任务");
                this.renwu_liebiao.Items.Add("每日活跃");
                this.renwu_liebiao.Items.Add("自动签到");
                this.renwu_liebiao.Items.Add("支线任务");
                this.renwu_liebiao.Items.Add("挂机任务");
            }
        }

        private void queding_shezhi_Click(object? sender, EventArgs e)
        {
            SaveSettings();

            string selectedMode = this.pailie_fangshi.Text;
            string emuType = this.moniqi_xuanze.Text;
            string basePath = this.lujing_shuru.Text.Trim();

            if (selectedMode.Contains("平铺"))
            {
                if (emuType.Contains("雷电"))
                {
                    if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                    {
                        MessageBox.Show("模拟器路径无效，无法执行排序！");
                        return;
                    }
                    string cmdExe = Path.Combine(basePath, "ldconsole.exe");
                    if (File.Exists(cmdExe))
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.FileName = cmdExe;
                            psi.Arguments = "sortWnd";
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("执行排序出错: " + ex.Message);
                        }
                    }
                }
            }
            else if (selectedMode.Contains("隐藏"))
            {
                int count = 0;
                for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
                {
                    var cellVal = this.moniqi_liebiao.Rows[i].Cells["jubing"].Value;
                    string hwndStr = cellVal != null ? cellVal.ToString() : "0";

                    if (long.TryParse(hwndStr, out long childHwndVal) && childHwndVal > 0)
                    {
                        IntPtr childHwnd = (IntPtr)childHwndVal;
                        IntPtr parentHwnd = GetAncestor(childHwnd, GA_ROOT);

                        if (parentHwnd != IntPtr.Zero)
                        {
                            SetWindowPos(parentHwnd, IntPtr.Zero, -3000, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                            count++;
                        }
                    }
                }
                if (count == 0) MessageBox.Show("未检测到运行中的窗口，无法隐藏。");
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            if (Directory.Exists(this.lujing_shuru.Text))
            {
                shuaxin_liebiao_Click(null, EventArgs.Empty);
            }
            this.moniqi_liebiao.ClearSelection();
            this.moniqi_liebiao.CurrentCell = null;
        }

        private void LoadSettings()
        {
            string defaultEmuName = this.moniqi_xuanze.Items.Count > 0 ? (this.moniqi_xuanze.Items[0]?.ToString() ?? string.Empty) : string.Empty;
            string emuName = IniHelper.Read(INI_SECTION, "EmulatorType", defaultEmuName);
            int idx = this.moniqi_xuanze.Items.IndexOf(emuName);
            if (idx != -1) this.moniqi_xuanze.SelectedIndex = idx;
            else if (this.moniqi_xuanze.Items.Count > 0) this.moniqi_xuanze.SelectedIndex = 0;

            string maxCount = IniHelper.Read(INI_SECTION, "MaxCount", "2");
            this.duokai_shuliang.Text = maxCount;

            string savedPath = IniHelper.Read(INI_SECTION, "BasePath", "");
            if (string.IsNullOrEmpty(savedPath))
            {
                savedPath = Auto_Find_Path(this.moniqi_xuanze.Text);
            }
            this.lujing_shuru.Text = savedPath;

            string defaultSortType = this.pailie_fangshi.Items.Count > 0 ? (this.pailie_fangshi.Items[0]?.ToString() ?? string.Empty) : string.Empty;
            string sortType = IniHelper.Read(INI_SECTION, "SortType", defaultSortType);
            idx = this.pailie_fangshi.Items.IndexOf(sortType);
            if (idx != -1) this.pailie_fangshi.SelectedIndex = idx;
            else if (this.pailie_fangshi.Items.Count > 0) this.pailie_fangshi.SelectedIndex = 0;

            this.zidong_denglu.Checked = IniHelper.Read(INI_SECTION, "AutoLogin", "True") == "True";
            this.shifou_zhiding.Checked = IniHelper.Read(INI_SECTION, "PinToTop", "False") == "True";
            this.shifou_huanhao.Checked = IniHelper.Read(INI_SECTION, "SwitchAccount", "False") == "True";
            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                string defaultServer = cb.Items.Count > 0 ? (cb.Items[0]?.ToString() ?? string.Empty) : string.Empty;
                string serverName = IniHelper.Read(INI_SECTION, "ServerRegion", defaultServer);
                cb.SelectedIndex = cb.Items.IndexOf(serverName);
                if (cb.SelectedIndex == -1 && cb.Items.Count > 0) cb.SelectedIndex = 0;
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isScriptRunning)
            {
                if (MessageBox.Show("有脚本正在运行，确定要关闭程序吗？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                quanbu_tingzhi_Click(null, EventArgs.Empty);
            }
            SaveSettings();
        }

        private void SaveSettings()
        {
            IniHelper.Write(INI_SECTION, "EmulatorType", this.moniqi_xuanze.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "MaxCount", this.duokai_shuliang.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "BasePath", this.lujing_shuru.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "SortType", this.pailie_fangshi.Text ?? string.Empty);
            IniHelper.Write(INI_SECTION, "AutoLogin", this.zidong_denglu.Checked.ToString());
            IniHelper.Write(INI_SECTION, "PinToTop", this.shifou_zhiding.Checked.ToString());
            IniHelper.Write(INI_SECTION, "SwitchAccount", this.shifou_huanhao.Checked.ToString());
            Control[] qufus = this.Controls.Find("qufu_xuanze", true);
            if (qufus.Length > 0 && qufus[0] is ComboBox cb)
            {
                IniHelper.Write(INI_SECTION, "ServerRegion", cb.Text ?? string.Empty);
            }
        }

        private void StartGlobalMonitor()
        {
            if (_isMonitorActive) return;
            _isMonitorActive = true;
            _monitorToken = new CancellationTokenSource();
            var token = _monitorToken.Token;
            Task.Run(() =>
            {
                Thread.Sleep(500);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        TaskWorker[] currentWorkers;
                        lock (workers) { currentWorkers = new List<TaskWorker>(workers.Values).ToArray(); }

                        int activeCount = 0;

                        if (currentWorkers.Length > 0)
                        {
                            foreach (var worker in currentWorkers)
                            {
                                if (worker.RunState == 1 || worker.RunState == 2)
                                {
                                    activeCount++;
                                    double seconds = (DateTime.Now - worker.LastStartTime).TotalSeconds;

                                    if (seconds < 60)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        worker.MarkAsMonitored();
                                    }

                                    if (!worker.IsAlive())
                                    {
                                        worker.PerformRestart();
                                    }
                                }
                            }
                        }

                        if (activeCount == 0)
                        {
                            _isScriptRunning = false;
                            break;
                        }
                    }
                    catch { }

                    try { Task.Delay(10000, token).Wait(); } catch { break; }
                }
                _isMonitorActive = false;
            }, token);
        }

        private void StopGlobalMonitor()
        {
            _monitorToken?.Cancel();
            _isMonitorActive = false;
        }

        private void quanbu_qidong_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(this.duokai_shuliang.Text, out int maxCount)) { MessageBox.Show("多开数量必须是数字！"); return; }
            string basePath = this.lujing_shuru.Text.Trim();
            if (!Directory.Exists(basePath)) { MessageBox.Show("模拟器路径不存在！"); return; }
            if (this.moniqi_liebiao.Rows.Count == 0) { MessageBox.Show("列表为空！"); return; }

            _isScriptRunning = true;

            List<string> selectedTasks = new List<string>();
            foreach (var item in this.yixuan_renwu.Items)
            {
                selectedTasks.Add(item.ToString() ?? "");
            }

            int runningCount = 0;
            lock (workers)
            {
                foreach (var kvp in workers)
                {
                    if (kvp.Value.RunState != 4)
                    {
                        runningCount++;
                    }
                }
            }

            if (runningCount >= maxCount)
            {
                MessageBox.Show($"当前运行数量({runningCount})已达到设定上限({maxCount})，无法继续启动！");
                return;
            }

            int currentCount = runningCount;
            var rowsToStart = new List<(int index, string name, string className, string basePath)>();

            for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
            {
                if (currentCount >= maxCount) break;
                if (this.moniqi_liebiao.Rows[i].Selected)
                {
                    if (IsRowValidForStart(i))
                    {
                        AddToStartList(rowsToStart, i, basePath);
                        currentCount++;
                    }
                }
            }

            if (currentCount < maxCount)
            {
                for (int i = 0; i < this.moniqi_liebiao.Rows.Count; i++)
                {
                    if (currentCount >= maxCount) break;
                    if (!this.moniqi_liebiao.Rows[i].Selected)
                    {
                        if (IsRowValidForStart(i))
                        {
                            AddToStartList(rowsToStart, i, basePath);
                            currentCount++;
                        }
                    }
                }
            }

            if (rowsToStart.Count == 0) return;

            Task.Run(() =>
            {
                foreach (var item in rowsToStart)
                {
                    TaskWorker worker;
                    lock (workers)
                    {
                        if (workers.ContainsKey(item.index))
                        {
                            worker = workers[item.index];
                        }
                        else
                        {
                            worker = new TaskWorker(item.index, item.name, item.className, item.basePath)
                            {
                                StatusCallback = (r, status, hwnd) => UpdateRowStatus(r, status, hwnd),
                                ExceptionCallback = (r, msg) => UpdateRowException(r, msg),

                                // 🔥🔥🔥 这里设置为空，界面就不会显示日志刷屏了 🔥🔥🔥
                                LogCallback = (msg) => { }
                            };
                            workers[item.index] = worker;
                        }

                        worker.TaskList = new List<string>(selectedTasks);
                    }

                    long checkHwnd = 0;
                    try
                    {
                        // 🔥 使用新的 SDK 对象来检测窗口
                        OLAPlugServer tempOla = new OLAPlugServer();
                        if (tempOla.OLAObject != 0)
                        {
                            checkHwnd = tempOla.FindWindow(item.className, item.name);
                            // 释放临时对象
                            tempOla.ReleaseObj();
                        }
                    }
                    catch { checkHwnd = 0; }

                    worker.Start();
                    this.Invoke(new Action(() => { StartGlobalMonitor(); }));

                    if (checkHwnd > 0) System.Threading.Thread.Sleep(500);
                    else System.Threading.Thread.Sleep(3000);
                }
            });
        }

        private void renwu_liebiao_DoubleClick(object? sender, EventArgs e)
        {
            if (_isScriptRunning) return;
            if (this.renwu_liebiao.SelectedItem != null)
            {
                object item = this.renwu_liebiao.SelectedItem;
                this.yixuan_renwu.Items.Add(item);
                this.renwu_liebiao.Items.Remove(item);
            }
        }

        private void yixuan_renwu_DoubleClick(object? sender, EventArgs e)
        {
            if (_isScriptRunning) return;
            if (this.yixuan_renwu.SelectedItem != null)
            {
                object item = this.yixuan_renwu.SelectedItem;
                this.renwu_liebiao.Items.Add(item);
                this.yixuan_renwu.Items.Remove(item);
            }
        }

        private bool IsRowValidForStart(int i)
        {
            if (workers.ContainsKey(i) && workers[i].RunState != 4) return false;
            string? name = this.moniqi_liebiao.Rows[i].Cells["moniqi"].Value?.ToString();
            if (string.IsNullOrEmpty(name)) return false;
            return true;
        }

        private void AddToStartList(List<(int, string, string, string)> list, int i, string basePath)
        {
            string name = this.moniqi_liebiao.Rows[i].Cells["moniqi"].Value?.ToString()!;
            string className = "";
            if (name.Contains("雷电")) className = "LDPlayerMainFrame";
            else if (name.Contains("MuMu")) className = "Qt5QWindowIcon";
            else if (name.Contains("夜神")) className = "NoxWndMainClass";

            list.Add((i, name, className, basePath));
        }

        private void quanbu_tingzhi_Click(object? sender, EventArgs e)
        {
            StopGlobalMonitor();
            lock (workers)
            {
                foreach (var kvp in workers)
                {
                    kvp.Value.Stop();
                }
                workers.Clear();
            }
            _isScriptRunning = false;
        }

        private void tingzhi_xuanzhong_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选中要停止的模拟器！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index))
                {
                    workers[index].Stop();
                }
            }
        }

        private void zanting_suoyou_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0) { MessageBox.Show("请先选中！"); return; }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index)) workers[index].Pause();
            }
        }

        private void huifu_suoyou_Click(object? sender, EventArgs e)
        {
            if (this.moniqi_liebiao.SelectedRows.Count == 0) { MessageBox.Show("请先选中！"); return; }
            foreach (System.Windows.Forms.DataGridViewRow row in this.moniqi_liebiao.SelectedRows)
            {
                int index = row.Index;
                if (workers.ContainsKey(index)) workers[index].Resume();
            }
        }

        private void guanbi_suoyou_Click(object? sender, EventArgs e)
        {
            quanbu_tingzhi_Click(null, EventArgs.Empty);
            string basePath = this.lujing_shuru.Text.Trim()!;
            string emuType = this.moniqi_xuanze.Text!;
            string cmdExe = ""; string args = "";
            if (emuType.Contains("雷电")) { cmdExe = Path.Combine(basePath, "ldconsole.exe"); args = "quitall"; }
            else if (emuType.Contains("MuMu"))
            {
                string parentDir = Directory.GetParent(basePath)?.FullName ?? "";
                string shellPath = Path.Combine(parentDir, "shell");
                cmdExe = Path.Combine(shellPath, "MuMuManager.exe");
                if (!File.Exists(cmdExe)) cmdExe = Path.Combine(basePath, "MuMuManager.exe");
                args = "control -v all shutdown";
            }
            else if (emuType.Contains("夜神"))
            {
                cmdExe = Path.Combine(basePath, "NoxConsole.exe");
                if (!File.Exists(cmdExe)) cmdExe = Path.Combine(Directory.GetParent(basePath)?.FullName ?? "", "bin", "NoxConsole.exe");
                args = "quitall";
            }
            if (!string.IsNullOrEmpty(cmdExe) && File.Exists(cmdExe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = cmdExe, Arguments = args, UseShellExecute = false, CreateNoWindow = true });
                    MessageBox.Show("已发送关闭指令");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("错误: " + ex.Message);
                }
            }
        }

        public void UpdateRowStatus(int rowIndex, string status, string hwnd)
        {
            if (this.moniqi_liebiao.InvokeRequired)
            {
                this.moniqi_liebiao.Invoke(new Action<int, string, string>(UpdateRowStatus), rowIndex, status, hwnd);
                return;
            }
            if (rowIndex >= 0 && rowIndex < this.moniqi_liebiao.Rows.Count)
            {
                this.moniqi_liebiao.Rows[rowIndex].Cells["zhuangtai"].Value = status;
                this.moniqi_liebiao.Rows[rowIndex].Cells["jubing"].Value = hwnd;
            }
        }

        public void UpdateRowException(int rowIndex, string msg)
        {
            if (this.moniqi_liebiao.InvokeRequired)
            {
                this.moniqi_liebiao.Invoke(new Action<int, string>(UpdateRowException), rowIndex, msg);
                return;
            }
            if (rowIndex >= 0 && rowIndex < this.moniqi_liebiao.Rows.Count)
            {
                this.moniqi_liebiao.Rows[rowIndex].Cells["yichang"].Value = msg;
            }
        }

        private void Control_Intercept_DropDown(object? sender, EventArgs e) { if (_isScriptRunning) { (sender as ComboBox).DroppedDown = false; this.moniqi_liebiao.Focus(); MessageBox.Show("运行中禁止修改配置！"); } }
        private void Control_Intercept_Enter(object? sender, EventArgs e) { if (_isScriptRunning) { this.moniqi_liebiao.Focus(); MessageBox.Show("运行中禁止修改配置！"); } }
        private void Control_Intercept_KeyPress(object? sender, KeyPressEventArgs e) { if (_isScriptRunning) { e.Handled = true; this.moniqi_liebiao.Focus(); } }

        private void moniqi_liebiao_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.RowIndex >= 0) _isRowAlreadySelected = this.moniqi_liebiao.Rows[e.RowIndex].Selected;
        }

        private void moniqi_liebiao_CellMouseUp(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.RowIndex >= 0)
            {
                if (_isRowAlreadySelected) { this.moniqi_liebiao.Rows[e.RowIndex].Selected = false; this.moniqi_liebiao.CurrentCell = null; }
                _isRowAlreadySelected = false;
            }
        }

        private void Register_OLA()
        {
            try
            {
                // 🔥 从配置中心读取注册码
                int ret = OLAPlugDLLHelper.Reg(OLAConfig.UserCode, OLAConfig.SoftCode, OLAConfig.Key);

                if (ret != 1) MessageBox.Show($"注册失败:{ret}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DLL调用失败或丢失: {ex.Message}");
            }
        }

        private void moniqi_xuanze_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_isScriptRunning)
            {
                string name = this.moniqi_xuanze.Text;
                string foundPath = Auto_Find_Path(name);
                this.lujing_shuru.Text = string.IsNullOrEmpty(foundPath) ? @"D:\未找到，请手动输入" : foundPath;
            }
        }

        private string Auto_Find_Path(string emulatorName)
        {
            string nameLower = emulatorName.ToLower();
            if (nameLower.Contains("雷电"))
            {
                string res = Deep_Search_D_Drive("LDPlayer64");
                if (!string.IsNullOrEmpty(res)) return res;
                return Deep_Search_D_Drive("LDPlayer");
            }
            else if (nameLower.Contains("mumu"))
            {
                string res = Deep_Search_D_Drive("MuMuPlayer");
                if (!string.IsNullOrEmpty(res)) return res;
                return Deep_Search_D_Drive("MuMuPlayer-12.0");
            }
            return "";
        }

        private string Deep_Search_D_Drive(string targetName)
        {
            string root = @"D:\";
            string level0 = Path.Combine(root, targetName);
            if (Directory.Exists(level0)) return level0;
            try
            {
                string[] dirs = Directory.GetDirectories(root);
                foreach (string dir in dirs)
                {
                    string tryPath = Path.Combine(dir, targetName);
                    if (Directory.Exists(tryPath)) return tryPath;

                    if (Path.GetFileName(dir).Equals(targetName, StringComparison.OrdinalIgnoreCase)) return dir;
                }
            }
            catch { }
            return "";
        }

        private void shuaxin_liebiao_Click(object? sender, EventArgs e)
        {
            if (_isScriptRunning) { MessageBox.Show("运行中无法刷新"); return; }
            moniqi_liebiao.Rows.Clear();
            string path = lujing_shuru.Text.Trim();
            if (!Directory.Exists(path))
            {
                MessageBox.Show("路径不存在！");
                return;
            }
            string vms = Path.Combine(path, "vms");
            if (!Directory.Exists(vms)) vms = Path.Combine(Directory.GetParent(path)?.FullName ?? "", "vms");
            if (Directory.Exists(vms))
            {
                if (moniqi_xuanze.Text.Contains("雷电")) Parse_Leidian_Vms(vms);
                else if (moniqi_xuanze.Text.Contains("MuMu")) Parse_Mumu_Vms(vms);
            }
            moniqi_liebiao.ClearSelection(); moniqi_liebiao.CurrentCell = null;
        }

        private void Parse_Leidian_Vms(string p)
        {
            foreach (var d in Directory.GetDirectories(p))
            {
                string n = new DirectoryInfo(d).Name;
                string id = n.StartsWith("leidian") ? n.Substring(7) : n;
                if (int.TryParse(id, out _) && id != "0") Tianjia_Hang((moniqi_liebiao.Rows.Count + 1).ToString(), $"雷电模拟器-{id}", "未运行");
            }
        }

        private void Parse_Mumu_Vms(string p)
        {
            foreach (var d in Directory.GetDirectories(p))
            {
                string n = new DirectoryInfo(d).Name;
                string[] s = n.Split('-');
                if (s.Length > 0)
                {
                    string id = s[s.Length - 1];
                    if (int.TryParse(id, out _) && id != "0") Tianjia_Hang((moniqi_liebiao.Rows.Count + 1).ToString(), $"MUMU模拟器-{id}", "未运行");
                }
            }
        }

        private void Tianjia_Hang(string a, string b, string c)
        {
            int i = moniqi_liebiao.Rows.Add();
            var r = moniqi_liebiao.Rows[i];
            r.Cells["xuhao"].Value = a; r.Cells["moniqi"].Value = b; r.Cells["zhuangtai"].Value = c;
            r.Cells["zhanghao"].Value = ""; r.Cells["mima"].Value = "";
            r.Cells["jubing"].Value = "0"; r.Cells["yichang"].Value = "无异常";
        }

        private void quanbu_tingzhi_Click_1(object? sender, EventArgs e)
        {
            quanbu_tingzhi_Click(sender, EventArgs.Empty);
        }
        private void label6_Click(object? sender, EventArgs e) { }
    }
}