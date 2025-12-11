using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Text;
using static OLAPlug.OLAPlugDLLHelper;

namespace OLAPlug
{
    public class ColorModel
    {
        /// <summary>
        /// 颜色起始范围 颜色格式 RRGGBB 或者#RRGGBB
        /// </summary>
        public string StartColor { get; set; }

        /// <summary>
        /// 颜色结束范围 颜色格式 RRGGBB 或者#RRGGBB
        /// </summary>
        public string EndColor { get; set; }

        /// <summary>
        ///  0普通模式取合集,1反色模式取合集,2普通模式取交集,3反色模式取交集
        /// </summary>
        public int Type { get; set; }
    }

    public class PointColorModel
    {
        public Point Point { get; set; }
        public List<ColorModel> Colors { get; set; }
    }

    public class OcrResult
    {
        /// <summary>
        /// 识别结果
        /// </summary>
        public List<OcrModel> Regions { get; set; }

        /// <summary>
        /// 识别文字
        /// </summary>
        public string Text { get; set; }
    }

    public class OcrModel
    {
        /// <summary>
        /// 识别评分
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 识别文字
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 中心点
        /// </summary>
        public Point Center { get; set; }

        /// <summary>
        /// 矩形4个顶点
        /// </summary>
        public List<Point> Vertices { get; set; }

        /// <summary>
        /// 矩形角度. When the angle is 0, 90, 180, 270 etc., the rectangle becomes
        /// </summary>
        public double Angle { get; set; }
    }

    public class MatchResult
    {
        public bool MatchState { get; set; } = false;
        public double MatchVal { get; set; } = 0;
        public double Angle { get; set; } = 0;
        /// <summary>
        /// 多图识别时返回图片索引从0开始
        /// </summary>
        public int Index { get; set; } = 0;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Point
    {
        public Point() { }
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Size
    {
        public Size() { }
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class OLAPlugServer
    {
        public long OLAObject;

        public string UserCode = "";
        public string SoftCode = "";
        public string FeatureList = "";

        public OLAPlugServer()
        {
            OLAObject = CreateCOLAPlugInterFace();
        }

        public string PtrToStringUTF8(long ptr)
        {
            var str = Marshal.PtrToStringUTF8((IntPtr)ptr);
            FreeStringPtr(ptr);
            return str;
        }

        public string PtrToStringAuto(long ptr)
        {
            var str = Marshal.PtrToStringAuto((IntPtr)ptr);
            FreeStringPtr(ptr);
            return str;
        }

        public string PtrToStringAnsi(long ptr)
        {
            var str = Marshal.PtrToStringAnsi((IntPtr)ptr);
            FreeStringPtr(ptr);
            return str;
        }

        public string GetStringFromPtr(long ptr)
        {
            int size = GetStringSize(ptr);
            StringBuilder lpString = new StringBuilder(size + 1);//+1 用于存储终止符 '\0'
            int outSize = GetStringFromPtr(ptr, lpString, size + 1);
            return lpString.ToString();
        }

        /// <summary>
        /// 释放OLA对象
        /// </summary>
        public void ReleaseObj()
        {
            DestroyCOLAPlugInterFace();
        }

        public List<Dictionary<string, object>> Query(long db, string sql)
        {
            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            long stmt = ExecuteReader(db, sql);
            // 获取列名
            List<string> columnNames = new List<string>();
            for (int i = 0; i < GetColumnCount(stmt); i++)
            {
                columnNames.Add(GetColumnName(stmt, i));
            }
            //读取数据
            while (Read(stmt)==1)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                foreach (string columnName in columnNames)
                {
                    int iCol = GetColumnIndex(stmt, columnName);
                    switch (GetColumnType(stmt, iCol))
                    {
                        //SQLITE_INTEGER
                        case 1:
                            row[columnName] = GetInt64(stmt, iCol);
                            break;
                        //SQLITE_FLOAT
                        case 2:
                            row[columnName] = GetDouble(stmt, iCol);
                            break;
                        //SQLITE_TEXT
                        case 3:
                            row[columnName] = GetString(stmt, iCol);
                            break;
                        //SQLITE_BLOB
                        case 4:
                            row[columnName] = GetString(stmt, iCol);
                            break;
                        //SQLITE_NULL
                        case 5:
                            row[columnName] = null;
                            break;
                    }
                }
                data.Add(row);
            }
            ;
            //释放资源
            Finalize(stmt);
            return data;
        }

        public string GetConfigByKey(string configKey)
        {
            string configStr = GetConfig(configKey);
            if (string.IsNullOrEmpty(configKey))
            {
                return configStr;
            }
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configStr);
            if (configDict.TryGetValue(configKey, out var configValue))
            {
                return configValue.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// 创建OLA对象
        /// </summary>
        /// <returns>OLAPlug对象指针，用于后续接口的传参</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL与COM的调用模式不一样。创建的对象需要使用 DestroyCOLAPlugInterFace 接口释放内存。
        /// </remarks>
        public long CreateCOLAPlugInterFace(){
            return OLAPlugDLLHelper.CreateCOLAPlugInterFace();
        }

        /// <summary>
        /// 释放OLA对象内存
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该接口为DLL版本专用。
        /// </remarks>
        public int DestroyCOLAPlugInterFace(){
            return OLAPlugDLLHelper.DestroyCOLAPlugInterFace(OLAObject);
        }

        /// <summary>
        /// 返回当前插件版本号。
        /// </summary>
        /// <returns>当前插件的版本描述字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string Ver(){
            return PtrToStringUTF8(OLAPlugDLLHelper.Ver());
        }

        /// <summary>
        /// 设置全局路径。建议使用 SetConfig 接口。
        /// </summary>
        /// <param name="path">要设置的路径值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetPath(string path){
            return OLAPlugDLLHelper.SetPath(OLAObject, path);
        }

        /// <summary>
        /// 获取全局路径。(可用于调试) 建议使用 GetConfig 接口。
        /// </summary>
        /// <returns>以字符串的形式返回当前设置的全局路径</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetPath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetPath(OLAObject));
        }

        /// <summary>
        /// 获取本机的机器码。此机器码用于网站后台。要求调用进程必须有管理员权限，否则返回空串。
        /// </summary>
        /// <returns>字符串表达的机器码。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// <br/>2. 此机器码包含的硬件设备有硬盘、显卡、网卡等。重装系统不会改变此值。
        /// <br/>3. 插拔任何USB设备，以及安装任何网卡驱动程序，都会导致机器码改变。
        /// </remarks>
        public string GetMachineCode(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMachineCode(OLAObject));
        }

        /// <summary>
        /// 获取注册在系统中的OLAPlug.dll的路径。
        /// </summary>
        /// <returns>返回OLAPlug.dll所在路径。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetBasePath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetBasePath(OLAObject));
        }

        /// <summary>
        /// 调用此函数来注册，从而使用插件的高级功能。推荐使用此函数。多个OLA对象仅需要注册一次。
        /// </summary>
        /// <param name="userCode">用户码</param>
        /// <param name="softCode">软件码</param>
        /// <param name="featureList">功能列表</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int Reg(string userCode, string softCode, string featureList){
            return OLAPlugDLLHelper.Reg(userCode, softCode, featureList);
        }

        /// <summary>
        /// 绑定指定的窗口，并指定这个窗口的屏幕颜色获取方式、鼠标仿真模式、键盘仿真模式以及模式设定
        /// </summary>
        /// <param name="hwnd">指定的窗口句柄</param>
        /// <param name="display">屏幕颜色获取方式
        ///<br/> normal: 正常模式，平常我们用的前台截屏模式
        ///<br/> gdi: gdi模式
        ///<br/> gdi2: gdi2模式，此模式兼容性较强，但是速度比gdi模式要慢许多
        ///<br/> gdi3: gdi3模式，此模式兼容性较强，但是速度比gdi模式要慢许多
        ///<br/> gdi4: gdi4模式，支持小程序、浏览器截图
        ///<br/> gdi5: gdi5模式，支持小程序、浏览器截图
        ///<br/> dxgi: DXGI模式, 支持小程序和浏览器截图,在windows10 1903及以上版本中支持
        ///<br/> vnc: vnc模式
        ///<br/> dx: dx模式（需要管理员权限）
        /// </param>
        /// <param name="mouse">鼠标仿真模式
        ///<br/> normal: 正常模式，平常我们用的前台鼠标模式
        ///<br/> windows: Windows模式，采取模拟windows消息方式
        ///<br/> windows3: Windows3模式,采取模拟windows消息方式,适用于多窗口的进程
        ///<br/> vnc: vnc模式
        ///<br/> dx.mouse.position.lock.api: 通过封锁系统API来锁定鼠标位置
        ///<br/> dx.mouse.position.lock.message: 通过封锁系统消息来锁定鼠标位置
        ///<br/> dx.mouse.focus.input.api: 通过封锁系统API来锁定鼠标输入焦点
        ///<br/> dx.mouse.focus.input.message: 通过封锁系统消息来锁定鼠标输入焦点
        ///<br/> dx.mouse.clip.lock.api: 通过封锁系统API来锁定刷新区域
        ///<br/> dx.mouse.input.lock.api: 通过封锁系统API来锁定鼠标输入接口
        ///<br/> dx.mouse.state.api: 通过封锁系统API来锁定鼠标输入状态
        ///<br/> dx.mouse.state.message: 通过封锁系统消息来锁定鼠标输入状态
        ///<br/> dx.mouse.api: 通过封锁系统API来模拟dx鼠标输入
        ///<br/> dx.mouse.cursor: 开启后台获取鼠标特征码
        ///<br/> dx.mouse.raw.input: 特定窗口鼠标操作支持
        ///<br/> dx.mouse.input.lock.api2: 防止前台鼠标移动
        ///<br/> dx.mouse.input.lock.api3: 防止前台鼠标移动
        ///<br/> dx.mouse.raw.input.active: 配合dx.mouse.raw.input使用
        /// </param>
        /// <param name="keypad">键盘仿真模式
        ///<br/> normal: 正常模式，平常我们用的前台键盘模式
        ///<br/> windows: Windows模式，采取模拟windows消息方式
        ///<br/> vnc: vnc模式
        ///<br/> dx.keypad.input.lock.api: 通过封锁系统API来锁定键盘输入接口
        ///<br/> dx.keypad.state.api: 通过封锁系统API来锁定键盘输入状态
        ///<br/> dx.keypad.api: 通过封锁系统API来模拟dx键盘输入
        ///<br/> dx.keypad.raw.input: 特定窗口键盘操作支持
        ///<br/> dx.keypad.raw.input.active: 配合dx.keypad.raw.input使用
        /// </param>
        /// <param name="mode">模式设定
        ///<br/> 0: 推荐模式，此模式比较通用，而且后台效果是最好的
        ///<br/> 1: 远程线程注入
        ///<br/> 2: 驱动注入模式1,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        ///<br/> 3: 驱动注入模式2,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        ///<br/> 4: 驱动注入模式3,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 绑定失败
        ///<br/>1: 绑定成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. dx模式组合可以使用"|"连接多个模式，例如："dx.mouse.position.lock.api|dx.mouse.focus.input.api"
        /// </remarks>
        public int BindWindow(long hwnd, string display, string mouse, string keypad, int mode){
            return OLAPlugDLLHelper.BindWindow(OLAObject, hwnd, display, mouse, keypad, mode);
        }

        /// <summary>
        /// 绑定指定的窗口，并指定这个窗口的屏幕颜色获取方式、鼠标仿真模式、键盘仿真模式以及模式设定
        /// </summary>
        /// <param name="hwnd">指定的窗口句柄</param>
        /// <param name="display">屏幕颜色获取方式
        ///<br/> normal: 正常模式，平常我们用的前台截屏模式
        ///<br/> gdi: gdi模式
        ///<br/> gdi2: gdi2模式，此模式兼容性较强，但是速度比gdi模式要慢许多
        ///<br/> gdi3: gdi3模式，此模式兼容性较强，但是速度比gdi模式要慢许多
        ///<br/> gdi4: gdi4模式，支持小程序、浏览器截图
        ///<br/> gdi5: gdi5模式，支持小程序、浏览器截图
        ///<br/> dxgi: DXGI模式, 支持小程序和浏览器截图,在windows10 1903及以上版本中支持
        ///<br/> vnc: vnc模式
        ///<br/> dx: dx模式（需要管理员权限）
        /// </param>
        /// <param name="mouse">鼠标仿真模式
        ///<br/> normal: 正常模式，平常我们用的前台鼠标模式
        ///<br/> windows: Windows模式，采取模拟windows消息方式
        ///<br/> windows3: Windows3模式,采取模拟windows消息方式,适用于多窗口的进程
        ///<br/> vnc: vnc模式
        ///<br/> dx.mouse.position.lock.api: 通过封锁系统API来锁定鼠标位置
        ///<br/> dx.mouse.position.lock.message: 通过封锁系统消息来锁定鼠标位置
        ///<br/> dx.mouse.focus.input.api: 通过封锁系统API来锁定鼠标输入焦点
        ///<br/> dx.mouse.focus.input.message: 通过封锁系统消息来锁定鼠标输入焦点
        ///<br/> dx.mouse.clip.lock.api: 通过封锁系统API来锁定刷新区域
        ///<br/> dx.mouse.input.lock.api: 通过封锁系统API来锁定鼠标输入接口
        ///<br/> dx.mouse.state.api: 通过封锁系统API来锁定鼠标输入状态
        ///<br/> dx.mouse.state.message: 通过封锁系统消息来锁定鼠标输入状态
        ///<br/> dx.mouse.api: 通过封锁系统API来模拟dx鼠标输入
        ///<br/> dx.mouse.cursor: 开启后台获取鼠标特征码
        ///<br/> dx.mouse.raw.input: 特定窗口鼠标操作支持
        ///<br/> dx.mouse.input.lock.api2: 防止前台鼠标移动
        ///<br/> dx.mouse.input.lock.api3: 防止前台鼠标移动
        ///<br/> dx.mouse.raw.input.active: 配合dx.mouse.raw.input使用
        /// </param>
        /// <param name="keypad">键盘仿真模式
        ///<br/> normal: 正常模式，平常我们用的前台键盘模式
        ///<br/> windows: Windows模式，采取模拟windows消息方式
        ///<br/> vnc: vnc模式
        ///<br/> dx.keypad.input.lock.api: 通过封锁系统API来锁定键盘输入接口
        ///<br/> dx.keypad.state.api: 通过封锁系统API来锁定键盘输入状态
        ///<br/> dx.keypad.api: 通过封锁系统API来模拟dx键盘输入
        ///<br/> dx.keypad.raw.input: 特定窗口键盘操作支持
        ///<br/> dx.keypad.raw.input.active: 配合dx.keypad.raw.input使用
        /// </param>
        /// <param name="pubstr"></param>
        /// <param name="mode">模式设定
        ///<br/> 0: 推荐模式，此模式比较通用，而且后台效果是最好的
        ///<br/> 1: 远程线程注入
        ///<br/> 2: 驱动注入模式1,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        ///<br/> 3: 驱动注入模式2,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        ///<br/> 4: 驱动注入模式3,当0,1无法使用时使用,需要加载驱动,第一次使用驱动会下载PDB文件绑定时间会变长
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 绑定失败
        ///<br/>1: 绑定成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. dx模式组合可以使用"|"连接多个模式，例如："dx.mouse.position.lock.api|dx.mouse.focus.input.api"
        /// </remarks>
        public int BindWindowEx(long hwnd, string display, string mouse, string keypad, string pubstr, int mode){
            return OLAPlugDLLHelper.BindWindowEx(OLAObject, hwnd, display, mouse, keypad, pubstr, mode);
        }

        /// <summary>
        /// 解绑窗口，取消之前通过 BindWindow 或 BindWindowEx 绑定的窗口。
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 解绑失败
        ///<br/>1: 解绑成功
        /// </returns>
        public int UnBindWindow(){
            return OLAPlugDLLHelper.UnBindWindow(OLAObject);
        }

        /// <summary>
        /// 获取当前对象已经绑定的窗口句柄，如果没有绑定窗口则返回0
        /// </summary>
        /// <returns>返回当前绑定的窗口句柄。如果没有绑定窗口，则返回0。</returns>
        public long GetBindWindow(){
            return OLAPlugDLLHelper.GetBindWindow(OLAObject);
        }

        /// <summary>
        /// 强制卸载已经注入到指定窗口的HookDLL。此函数用于清理和释放窗口相关的DLL资源，但需要谨慎使用，因为它会影响其他使用相同DLL的OLA对象。
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>0 卸载失败（可能原因：无效的窗口句柄、DLL已卸载、权限不足等），1 卸载成功。
        ///<br/>0: 卸载失败
        ///<br/>1: 卸载成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此操作为强制卸载，会影响使用相同DLL的其他OLA对象。
        /// <br/>2. 建议在程序退出前的清理工作、确认没有其他OLA对象需要使用该DLL、或处理DLL加载异常时使用。
        /// <br/>3. 卸载DLL后，相关的功能将无法使用
        /// <br/>4. 建议在卸载前保存必要的数据。
        /// <br/>5. 某些系统窗口可能会拒绝DLL卸载操作。
        /// <br/>6. 如果有多个OLA对象共享DLL，应协调好卸载时机。
        /// <br/>7. 建议实现错误处理和日志记录机制
        /// <br/>8. 在批量操作时要注意性能和稳定性。
        /// </remarks>
        public int ReleaseWindowsDll(long hwnd){
            return OLAPlugDLLHelper.ReleaseWindowsDll(OLAObject, hwnd);
        }

        /// <summary>
        /// 释放字符串内存。
        /// </summary>
        /// <param name="ptr">要释放的字符串内存地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeStringPtr(long ptr){
            return OLAPlugDLLHelper.FreeStringPtr(ptr);
        }

        /// <summary>
        /// 释放字节流内存。
        /// </summary>
        /// <param name="ptr">要释放的字节流地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeMemoryPtr(long ptr){
            return OLAPlugDLLHelper.FreeMemoryPtr(ptr);
        }

        /// <summary>
        /// 读取字符串大小。
        /// </summary>
        /// <param name="ptr">字符串内存地址</param>
        /// <returns>字符串缓冲区大小</returns>
        public int GetStringSize(long ptr){
            return OLAPlugDLLHelper.GetStringSize(ptr);
        }

        /// <summary>
        /// 从指定内存地址读取字符串，参考windows函数 GetWindowText实现。
        /// </summary>
        /// <param name="ptr">字符串内存地址。</param>
        /// <param name="lpString">接收字符串的缓冲区。</param>
        /// <param name="size">缓冲区大小，可以通过 GetStringSize 接口读取字符串大小，size要+1用于存储终止符'\0'。</param>
        /// <returns>成功返回字符串实际长度，失败返回0。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 使用此函数时需要确保传入的内存地址有效且可访问。
        /// <br/>2. 建议在使用前先通过 GetStringSize 接口获取实际需要的缓冲区大小。
        /// <br/>3. 缓冲区大小不足可能导致字符串截断。
        /// </remarks>
        public int GetStringFromPtr(long ptr, StringBuilder lpString, int size){
            return OLAPlugDLLHelper.GetStringFromPtr(ptr, lpString, size);
        }

        /// <summary>
        /// 延时指定的毫秒，过程中不阻塞UI操作。一般高级语言使用。按键用不到。
        /// </summary>
        /// <param name="millisecond">延时时间（毫秒）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int Delay(int millisecond){
            return OLAPlugDLLHelper.Delay(millisecond);
        }

        /// <summary>
        /// 延时指定范围内随机毫秒，过程中不阻塞UI操作。一般高级语言使用。按键用不到。
        /// </summary>
        /// <param name="minMillisecond">最小延时时间（毫秒）</param>
        /// <param name="maxMillisecond">最大延时时间（毫秒）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int Delays(int minMillisecond, int maxMillisecond){
            return OLAPlugDLLHelper.Delays(minMillisecond, maxMillisecond);
        }

        /// <summary>
        /// 开启/关闭UAC。
        /// </summary>
        /// <param name="enable">是否启用UAC。</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetUAC(int enable){
            return OLAPlugDLLHelper.SetUAC(OLAObject, enable);
        }

        /// <summary>
        /// 检测当前系统是否有开启UAC(用户账户控制)。
        /// </summary>
        /// <returns>当前状态
        ///<br/>0: 关闭
        ///<br/>1: 开启
        /// </returns>
        public int CheckUAC(){
            return OLAPlugDLLHelper.CheckUAC(OLAObject);
        }

        /// <summary>
        /// 运行指定的应用程序。
        /// </summary>
        /// <param name="appPath">要运行的程序路径。</param>
        /// <param name="mode">运行模式
        ///<br/> 0: 普通模式
        ///<br/> 1: 加强模式
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int RunApp(string appPath, int mode){
            return OLAPlugDLLHelper.RunApp(OLAObject, appPath, mode);
        }

        /// <summary>
        /// 执行指定的CMD指令，并返回cmd的输出结果。
        /// </summary>
        /// <param name="cmd">要执行的cmd命令。</param>
        /// <param name="current_dir">执行此cmd命令时所在目录。如果为空，表示使用当前目录。</param>
        /// <param name="time_out">超时设置，单位是毫秒。0表示一直等待。大于0表示等待指定的时间后强制结束。</param>
        /// <returns>cmd指令的执行结果。返回空字符串表示执行失败。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string ExecuteCmd(string cmd, string current_dir, int time_out){
            return PtrToStringUTF8(OLAPlugDLLHelper.ExecuteCmd(OLAObject, cmd, current_dir, time_out));
        }

        /// <summary>
        /// 读取用户自定义设置。
        /// </summary>
        /// <param name="configKey">配置项名称。</param>
        /// <returns>返回匹配结果，例如 {"EnableRealKeypad":false, "EnableRealMouse":true, ...}。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetConfig(string configKey){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetConfig(OLAObject, configKey));
        }

        /// <summary>
        /// 修改用户自定义设置。
        /// </summary>
        /// <param name="configStr">配置项字符串，格式为 {"key1":value1,"key2":"value2"}。</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetConfig(string configStr){
            return OLAPlugDLLHelper.SetConfig(OLAObject, configStr);
        }

        /// <summary>
        /// 修改用户自定义设置。
        /// </summary>
        /// <param name="key">配置项字符串，如: RealMouseMode。</param>
        /// <param name="value">配置项值字符串，如: true。</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetConfigByKey(string key, string value){
            return OLAPlugDLLHelper.SetConfigByKey(OLAObject, key, value);
        }

        /// <summary>
        /// 拖动文件到指定窗口。
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="file_path">文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SendDropFiles(long hwnd, string file_path){
            return OLAPlugDLLHelper.SendDropFiles(OLAObject, hwnd, file_path);
        }

        /// <summary>
        /// 设置默认编码。
        /// </summary>
        /// <param name="inputEncoding">输入编码。默认值0
        ///<br/> 0: gbk
        ///<br/> 1: utf-8
        ///<br/> 2: Unicode
        /// </param>
        /// <param name="outputEncoding">输出编码。默认值1
        ///<br/> 0: gbk
        ///<br/> 1: utf-8
        ///<br/> 2: Unicode
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetDefaultEncode(int inputEncoding, int outputEncoding){
            return OLAPlugDLLHelper.SetDefaultEncode(inputEncoding, outputEncoding);
        }

        /// <summary>
        /// 获取最后一次错误ID。
        /// </summary>
        /// <returns>错误ID</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 错误ID为0表示没有错误。
        /// </remarks>
        public int GetLastError(){
            return OLAPlugDLLHelper.GetLastError();
        }

        /// <summary>
        /// 获取最后一次错误字符串。
        /// </summary>
        /// <returns>错误字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetLastErrorString(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetLastErrorString());
        }

        /// <summary>
        /// 获取随机整数
        /// </summary>
        /// <param name="min">随机数的最小值（包含）</param>
        /// <param name="max">随机数的最大值（包含）</param>
        /// <returns>返回指定范围内的随机整数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的随机数包含最小值和最大值
        /// <br/>2. 每个线程使用独立的随机种子，确保多线程环境下的随机性
        /// <br/>3. 适用于需要生成随机整数用于测试、游戏、模拟等场景
        /// <br/>4. 与 GetRandomDouble 函数配合使用可以实现更复杂的随机数需求
        /// <br/>5. 建议在程序初始化时调用一次，确保随机种子正确初始化
        /// </remarks>
        public int GetRandomNumber(int min, int max){
            return OLAPlugDLLHelper.GetRandomNumber(OLAObject, min, max);
        }

        /// <summary>
        /// 获取随机浮点数
        /// </summary>
        /// <param name="min">随机数的最小值（包含）</param>
        /// <param name="max">随机数的最大值（包含）</param>
        /// <returns>返回指定范围内的随机浮点数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的随机数包含最小值和最大值
        /// <br/>2. 每个线程使用独立的随机种子，确保多线程环境下的随机性
        /// <br/>3. 适用于需要高精度随机数的场景，如概率计算、模拟仿真等
        /// <br/>4. 与 GetRandomNumber 函数配合使用可以实现更复杂的随机数需求
        /// <br/>5. 浮点数精度取决于系统实现，通常为双精度（64位）
        /// <br/>6. 建议在程序初始化时调用一次，确保随机种子正确初始化
        /// </remarks>
        public double GetRandomDouble(double min, double max){
            return OLAPlugDLLHelper.GetRandomDouble(OLAObject, min, max);
        }

        /// <summary>
        /// 排除掉指定区域结果，用于颜色识别结果及图像识别
        /// </summary>
        /// <param name="json">识别返回的结果</param>
        /// <param name="type">识别类型
        ///<br/> 1: 颜色识别
        ///<br/> 2: 图像识别
        /// </param>
        /// <param name="x1">排除区域左上角的X坐标</param>
        /// <param name="y1">排除区域左上角的Y坐标</param>
        /// <param name="x2">排除区域右下角的X坐标</param>
        /// <param name="y2">排除区域右下角的Y坐标</param>
        /// <returns>返回排除掉指定区域结果的JSON数据</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public string ExcludePos(string json, int type, int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.ExcludePos(OLAObject, json, type, x1, y1, x2, y2));
        }

        /// <summary>
        /// 返回离坐标点最近的结果，用于颜色识别结果及图像识别
        /// </summary>
        /// <param name="json">识别结果返回值</param>
        /// <param name="type">识别类型
        ///<br/> 1: 颜色识别
        ///<br/> 2: 图像识别
        /// </param>
        /// <param name="x">返回结果的X坐标</param>
        /// <param name="y">返回结果的Y坐标</param>
        /// <returns>返回最近结果的JSON字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// <br/>2. 返回格式根据 type 不同而不同：
        /// <br/>3. 颜色识别：{"x":10,"y":20}
        /// <br/>4. 图像识别：{"MatchVal":0.85,"MatchState":true,"Index":0,"Angle":45.0,"MatchPoint":{"x":100,"y":200}}
        /// </remarks>
        public string FindNearestPos(string json, int type, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindNearestPos(OLAObject, json, type, x, y));
        }

        /// <summary>
        /// 根据坐标点距离排序，用于颜色识别结果及图像识别
        /// </summary>
        /// <param name="json">识别结果返回值</param>
        /// <param name="type">识别类型
        ///<br/> 1: 颜色识别
        ///<br/> 2: 图像识别
        /// </param>
        /// <param name="x">锚点的X坐标</param>
        /// <param name="y">锚点的Y坐标</param>
        /// <returns>按顺序排列后的坐标点列表（字符串形式）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public string SortPosDistance(string json, int type, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.SortPosDistance(OLAObject, json, type, x, y));
        }

        /// <summary>
        /// 查找二值化图片中像素最密集区域，可以配合找色块等功能做二次分析。
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="x1">返回左上角x坐标</param>
        /// <param name="y1">返回左上角y坐标</param>
        /// <param name="x2">返回右下角x坐标</param>
        /// <param name="y2">返回右下角y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int GetDenseRect(long image, int width, int height, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetDenseRect(OLAObject, image, width, height, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 寻路算法
        /// </summary>
        /// <param name="image">二值化图像句柄</param>
        /// <param name="startX">起点x坐标</param>
        /// <param name="startY">起点y坐标</param>
        /// <param name="endX">终点x坐标</param>
        /// <param name="endY">终点y坐标</param>
        /// <param name="potentialRadius">潜在半径</param>
        /// <param name="searchRadius">搜索半径</param>
        /// <returns>返回路径规划结果字符串指针，格式为坐标点数组的JSON字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 确保输入的图像为二值化图像，白色区域为可通行，黑色区域为障碍物
        /// <br/>3. 起点和终点坐标必须在图像范围内
        /// <br/>4. potentialRadius 和 searchRadius 参数影响路径质量和搜索效率
        /// <br/>5. 当 potentialRadius 或 searchRadius 为负数时，只返回JPS寻路数据，不做路径优化
        /// </remarks>
        public List<Point> PathPlanning(long image, int startX, int startY, int endX, int endY, double potentialRadius, double searchRadius){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.PathPlanning(OLAObject, image, startX, startY, endX, endY, potentialRadius, searchRadius));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 创建图
        /// </summary>
        /// <param name="json">图的JSON表示，包含节点和边的信息,传空创建一个空的图对象</param>
        /// <returns>图的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图指针需要调用 DeleteGraph 释放内存
        /// <br/>2. 确保 JSON 格式正确，否则可能导致创建失败
        /// </remarks>
        public long CreateGraph(string json){
            return OLAPlugDLLHelper.CreateGraph(OLAObject, json);
        }

        /// <summary>
        /// 获取图
        /// </summary>
        /// <param name="graphPtr">图的指针，由CreateGraph接口返回</param>
        /// <returns>返回图的指针，如果图不存在或无效返回0。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 确保传入的 graphPtr 是有效的图指针
        /// <br/>2. 返回的指针用于验证图的有效性，不需要额外释放内存
        /// <br/>3. 在调用其他图操作函数前，建议先调用此函数验证图的有效性
        /// </remarks>
        public long GetGraph(long graphPtr){
            return OLAPlugDLLHelper.GetGraph(OLAObject, graphPtr);
        }

        /// <summary>
        /// 添加边
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="weight">权重</param>
        /// <param name="isDirected">是否是有向边</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 确保 from 和 to 节点在图中存在
        /// <br/>2. 权重值应为正数，用于最短路径计算
        /// <br/>3. 有向边只允许从 from 到 to 的方向，无向边允许双向通行
        /// <br/>4. 重复添加相同的边可能会覆盖之前的权重设置
        /// </remarks>
        public int AddEdge(long graphPtr, string from, string to, double weight, bool isDirected){
            return OLAPlugDLLHelper.AddEdge(OLAObject, graphPtr, from, to, weight, isDirected);
        }

        /// <summary>
        /// 获取最短路径
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <returns>最短路径</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 确保 startNode 节点在图中存在
        /// <br/>3. 如果起点无法到达某些节点，这些节点将不会出现在结果中
        /// <br/>4. 返回的JSON格式包含每个可达节点的距离和路径信息
        /// <br/>5. 算法会考虑边的权重，寻找总权重最小的路径
        /// <br/>6. 适用于需要分析图中所有节点可达性的场景
        /// <br/>7. 对于大型图，计算时间可能较长
        /// </remarks>
        public string GetShortestPath(long graphPtr, string from, string to){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetShortestPath(OLAObject, graphPtr, from, to));
        }

        /// <summary>
        /// 获取最短距离
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <returns>最短距离</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 距离是路径上所有边权重的总和
        /// <br/>2. 如果两点间不存在路径，函数返回-1
        /// <br/>3. 确保 from 和 to 节点在图中存在
        /// <br/>4. 算法会考虑边的权重，寻找总权重最小的路径
        /// <br/>5. 对于无向图，from到to的距离等于to到from的距离
        /// </remarks>
        public double GetShortestDistance(long graphPtr, string from, string to){
            return OLAPlugDLLHelper.GetShortestDistance(OLAObject, graphPtr, from, to);
        }

        /// <summary>
        /// 清空图
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 清空操作会删除所有节点和边，但保留图的基本结构
        /// <br/>2. 清空后可以重新添加节点和边
        /// <br/>3. 清空操作不可逆，请谨慎使用
        /// <br/>4. 建议在清空前备份重要的图数据
        /// </remarks>
        public int ClearGraph(long graphPtr){
            return OLAPlugDLLHelper.ClearGraph(OLAObject, graphPtr);
        }

        /// <summary>
        /// 删除图
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 删除操作会释放图对象占用的所有内存资源
        /// <br/>2. 删除后不能再使用该图指针进行任何操作
        /// <br/>3. 建议在程序结束前删除所有创建的图对象
        /// <br/>4. 删除操作不可逆，请确保不再需要该图对象
        /// <br/>5. 删除图对象后，相关的路径计算结果也会失效
        /// </remarks>
        public int DeleteGraph(long graphPtr){
            return OLAPlugDLLHelper.DeleteGraph(OLAObject, graphPtr);
        }

        /// <summary>
        /// 获取节点数量.
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <returns>节点数量</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 节点数量在创建图时确定，添加边不会改变节点数量
        /// <br/>2. 如果图指针无效，可能返回0或错误值
        /// <br/>3. 节点数量反映了图的基本规模
        /// <br/>4. 建议在创建图后立即检查节点数量以验证图的正确性
        /// </remarks>
        public int GetNodeCount(long graphPtr){
            return OLAPlugDLLHelper.GetNodeCount(OLAObject, graphPtr);
        }

        /// <summary>
        /// 获取边数量
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <returns>边数量</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 边数量会随着 AddEdge 操作而增加
        /// <br/>2. 对于无向图，一条边只计算一次
        /// <br/>3. 如果图指针无效，可能返回0或错误值
        /// <br/>4. 边数量反映了图的连接复杂度
        /// <br/>5. 建议在添加边后检查边数量以验证操作是否成功
        /// </remarks>
        public int GetEdgeCount(long graphPtr){
            return OLAPlugDLLHelper.GetEdgeCount(OLAObject, graphPtr);
        }

        /// <summary>
        /// 获取最短路径到所有节点
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="startNode">起点</param>
        /// <returns>最短路径到所有节点</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 确保 startNode 节点在图中存在
        /// <br/>3. 如果起点无法到达某些节点，这些节点将不会出现在结果中
        /// <br/>4. 返回的JSON格式包含每个可达节点的距离和路径信息
        /// <br/>5. 算法会考虑边的权重，寻找总权重最小的路径
        /// <br/>6. 适用于需要分析图中所有节点可达性的场景
        /// <br/>7. 对于大型图，计算时间可能较长
        /// </remarks>
        public string GetShortestPathToAllNodes(long graphPtr, string startNode){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetShortestPathToAllNodes(OLAObject, graphPtr, startNode));
        }

        /// <summary>
        /// 获取最小生成树
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <returns>最小生成树</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 最小生成树要求图是连通的
        /// <br/>3. 如果图不连通，函数返回0
        /// <br/>4. 最小生成树包含n-1条边（n为节点数）
        /// <br/>5. 算法会考虑边的权重，选择总权重最小的树
        /// <br/>6. 适用于网络设计、电路设计等需要最小成本连接的场景
        /// <br/>7. 对于无向图，最小生成树是唯一的（当所有边权重不同时）
        /// <br/>8. 返回的JSON包含总权重和所有边的详细信息
        /// </remarks>
        public string GetMinimumSpanningTree(long graphPtr){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMinimumSpanningTree(OLAObject, graphPtr));
        }

        /// <summary>
        /// 获取有向路径到所有节点.
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="startNode">起点</param>
        /// <returns>有向路径到所有节点</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 确保 startNode 节点在图中存在
        /// <br/>3. 如果起点无法到达某些节点，这些节点将不会出现在结果中
        /// <br/>4. 返回的字符串包含每个可达节点的有向路径和距离信息
        /// <br/>5. 算法会考虑边的权重，寻找总权重最小的有向路径
        /// <br/>6. 适用于需要分析有向图中所有节点可达性的场景
        /// <br/>7. 对于大型有向图，计算时间可能较长
        /// <br/>8. 有向路径考虑了边的方向性，与无向图的最短路径不同
        /// </remarks>
        public string GetDirectedPathToAllNodes(long graphPtr, string startNode){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDirectedPathToAllNodes(OLAObject, graphPtr, startNode));
        }

        /// <summary>
        /// 获取有向图最小生成树.
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="root">根节点</param>
        /// <returns>返回最小生成树信息的字符串指针，格式为JSON；如果无法生成最小树形图返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 最小树形图要求从根节点能够到达所有其他节点
        /// <br/>3. 如果根节点无法到达某些节点，函数返回0
        /// <br/>4. 最小树形图包含n-1条边（n为节点数）
        /// <br/>5. 算法会考虑边的权重，选择总权重最小的有向树
        /// <br/>6. 适用于网络设计、依赖关系分析等需要最小成本有向连接的场景
        /// <br/>7. 对于有向图，最小树形图可能不唯一
        /// </remarks>
        public string GetMinimumArborescence(long graphPtr, string root){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetMinimumArborescence(OLAObject, graphPtr, root));
        }

        /// <summary>
        /// 通过坐标创建图
        /// </summary>
        /// <param name="json">坐标节点JSON数据</param>
        /// <param name="connectAll">是否连接所有节点（默认为true）</param>
        /// <param name="maxDistance">最大连接距离（默认为无穷大）</param>
        /// <param name="useEuclideanDistance">是否使用欧几里得距离作为权重（默认为true）</param>
        /// <returns>图的指针，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图指针需要调用 DeleteGraph 释放内存
        /// <br/>2. JSON格式支持两种：
        /// <br/>3. 数组格式: [{"name":"A","x":0,"y":0},{"name":"B","x":1,"y":1}]
        /// <br/>4. 对象格式: {"A":{"x":0,"y":0},"B":{"x":1,"y":1}}
        /// <br/>5. connectAll为true时，所有节点间距离小于maxDistance的会被连接
        /// <br/>6. useEuclideanDistance为true时，边权重为节点间的欧几里得距离
        /// </remarks>
        public long CreateGraphFromCoordinates(string json, bool connectAll, double maxDistance, bool useEuclideanDistance){
            return OLAPlugDLLHelper.CreateGraphFromCoordinates(OLAObject, json, connectAll, maxDistance, useEuclideanDistance);
        }

        /// <summary>
        /// 添加坐标节点到现有图
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="name">节点名称</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="connectToExisting">是否连接到现有节点（默认为true）</param>
        /// <param name="maxDistance">最大连接距离（默认为无穷大）</param>
        /// <param name="useEuclideanDistance">是否使用欧几里得距离作为权重（默认为true）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 确保 graphPtr 是有效的图指针
        /// <br/>2. 如果节点名称已存在，会更新坐标信息
        /// <br/>3. connectToExisting为true时，新节点会连接到距离小于maxDistance的现有节点
        /// </remarks>
        public int AddCoordinateNode(long graphPtr, string name, double x, double y, bool connectToExisting, double maxDistance, bool useEuclideanDistance){
            return OLAPlugDLLHelper.AddCoordinateNode(OLAObject, graphPtr, name, x, y, connectToExisting, maxDistance, useEuclideanDistance);
        }

        /// <summary>
        /// 获取节点的坐标信息
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="name">节点名称</param>
        /// <returns>节点坐标信息的JSON字符串指针，节点不存在返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 返回格式: {"name":"节点名","x":坐标X,"y":坐标Y}
        /// <br/>3. 确保 graphPtr 是有效的图指针
        /// <br/>4. 如果节点不存在，返回0
        /// </remarks>
        public string GetNodeCoordinates(long graphPtr, string name){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetNodeCoordinates(OLAObject, graphPtr, name));
        }

        /// <summary>
        /// 设置节点间的连接关系
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="from">起始节点名称</param>
        /// <param name="to">目标节点名称</param>
        /// <param name="canConnect">是否可以连接（true为可以连接，false为不能连接）</param>
        /// <param name="weight">连接权重（如果canConnect为true时使用，-1表示使用欧几里得距离）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 确保 graphPtr 是有效的图指针
        /// <br/>2. 节点必须已存在于图中
        /// <br/>3. 设置连接关系会影响路径计算
        /// <br/>4. 如果canConnect为false，会删除对应的边
        /// </remarks>
        public int SetNodeConnection(long graphPtr, string from, string to, bool canConnect, double weight){
            return OLAPlugDLLHelper.SetNodeConnection(OLAObject, graphPtr, from, to, canConnect, weight);
        }

        /// <summary>
        /// 获取节点间的连接状态
        /// </summary>
        /// <param name="graphPtr">图的指针</param>
        /// <param name="from">起始节点名称</param>
        /// <param name="to">目标节点名称</param>
        /// <returns>当前状态
        ///<br/>0: 表示不能连接
        ///<br/>1: 表示可以连接
        ///<br/>-1: 表示节点不存在或图指针无效
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 确保 graphPtr 是有效的图指针
        /// </remarks>
        public int GetNodeConnectionStatus(long graphPtr, string from, string to){
            return OLAPlugDLLHelper.GetNodeConnectionStatus(OLAObject, graphPtr, from, to);
        }

        /// <summary>
        /// 执行汇编指令
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="asmStr"></param>
        /// <param name="type">执行类型,取值如下:
        ///<br/> 0: 在本进程中执行(创建线程),hwnd无效
        ///<br/> 1: 在hwnd指定进程内执行(创建远程线程)
        ///<br/> 2: 在已注入绑定的目标进程创建线程执行(需排队)
        ///<br/> 3: 同模式2,但在hwnd所在线程直接执行
        ///<br/> 4: 同模式0,但在当前线程直接执行
        ///<br/> 5: 在hwnd指定进程内执行(APC注入)
        ///<br/> 6: 直接在hwnd所在线程执行
        /// </param>
        /// <param name="baseAddr"></param>
        /// <returns>32位进程返回EAX，64位进程返回RAX，执行失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 使用此函数需要谨慎，错误的汇编指令可能导致程序崩溃
        /// <br/>2. 建议在测试环境中先验证汇编代码的正确性
        /// <br/>3. 不同执行模式适用于不同的应用场景，请根据需求选择合适的type参数
        /// <br/>4. 在目标进程中执行需要相应的权限
        /// <br/>5. 使用APC注入模式(type=5)
        /// <br/>6. 返回值的解释取决于汇编指令的具体内容
        /// <br/>7. 建议在使用前备份重要数据
        /// </remarks>
        public long AsmCall(long hwnd, string asmStr, int type, long baseAddr){
            return OLAPlugDLLHelper.AsmCall(OLAObject, hwnd, asmStr, type, baseAddr);
        }

        /// <summary>
        /// 把汇编语言字符串转换为机器码并用16进制字符串的形式输出
        /// </summary>
        /// <param name="asmStr">汇编语言字符串，大小写均可，如"mov eax,1"</param>
        /// <param name="baseAddr">汇编指令所在的地址，用于计算相对地址；对于绝对地址指令可以设为0</param>
        /// <param name="arch">架构类型
        ///<br/> 0: x86
        ///<br/> 1: arm
        ///<br/> 2: arm64
        /// </param>
        /// <param name="mode">模式
        ///<br/> 16: 16位
        ///<br/> 32: 32位
        ///<br/> 64: 64位
        /// </param>
        /// <returns>成功返回机器码字符串的指针（16进制格式，如"aa bb cc"）；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 支持的汇编语法取决于底层汇编器
        /// <br/>3. baseAddr 参数用于计算相对地址
        /// <br/>4. 不同架构和模式支持的指令集不同
        /// <br/>5. 建议在使用前验证汇编语法的正确性
        /// <br/>6. 机器码输出格式为16进制字符串，如"aa bb cc"
        /// <br/>7. 此函数适用于代码分析和逆向工程工具开发
        /// </remarks>
        public string Assemble(string asmStr, long baseAddr, int arch, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.Assemble(OLAObject, asmStr, baseAddr, arch, mode));
        }

        /// <summary>
        /// 把指定的机器码转换为汇编语言输出
        /// </summary>
        /// <param name="asmCode">机器码，形式如"aa bb cc"这样的16进制表示的字符串（空格可忽略）</param>
        /// <param name="baseAddr">指令所在的地址，用于计算相对地址和符号解析</param>
        /// <param name="arch">架构类型
        ///<br/> 0: x86
        ///<br/> 1: arm
        ///<br/> 2: arm64
        /// </param>
        /// <param name="mode">模式
        ///<br/> 16: 16位
        ///<br/> 32: 32位
        ///<br/> 64: 64位
        /// </param>
        /// <param name="showType">显示类型
        ///<br/> 0: 显示详细汇编信息（包括地址、机器码、汇编指令）
        ///<br/> 1: 只显示机器码
        /// </param>
        /// <returns>成功返回汇编语言字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 如果有多条指令，则每条指令以字符"|"连接
        /// <br/>3. showType=0时显示详细汇编信息，包括地址、机器码、汇编指令
        /// <br/>4. showType=1时只显示机器码
        /// <br/>5. 机器码输入格式为16进制字符串，空格可以忽略
        /// <br/>6. 不同架构和模式支持的指令集不同
        /// <br/>7. baseAddr 参数用于计算相对地址和符号解析
        /// <br/>8. 此函数适用于逆向工程、代码分析和调试工具开发
        /// </remarks>
        public string Disassemble(string asmCode, long baseAddr, int arch, int mode, int showType){
            return PtrToStringUTF8(OLAPlugDLLHelper.Disassemble(OLAObject, asmCode, baseAddr, arch, mode, showType));
        }

        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="userCode">(字符串): 用户码。</param>
        /// <param name="softCode">(字符串): 软件码。</param>
        /// <param name="featureList">(字符串): 功能列表。为空只使用授权系统，不注册插件</param>
        /// <param name="softVersion">(字符串): 软件版本。</param>
        /// <param name="dealerCode">(字符串): 经销商码。</param>
        /// <returns>JSON字符串: 登录结果。</returns>
        public string Login(string userCode, string softCode, string featureList, string softVersion, string dealerCode){
            return PtrToStringUTF8(OLAPlugDLLHelper.Login(userCode, softCode, featureList, softVersion, dealerCode));
        }

        /// <summary>
        /// 激活。
        /// </summary>
        /// <param name="userCode">(字符串): 用户码。</param>
        /// <param name="softCode">(字符串): 软件码。</param>
        /// <param name="softVersion">(字符串): 软件版本。</param>
        /// <param name="dealerCode">(字符串): 经销商码。</param>
        /// <param name="licenseKey">(字符串): 激活码。</param>
        /// <returns>JSON字符串: 激活结果。</returns>
        public string Activate(string userCode, string softCode, string softVersion, string dealerCode, string licenseKey){
            return PtrToStringUTF8(OLAPlugDLLHelper.Activate(userCode, softCode, softVersion, dealerCode, licenseKey));
        }

        /// <summary>
        /// 释放绘制系统资源并清理所有对象
        /// </summary>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiCleanup(){
            return OLAPlugDLLHelper.DrawGuiCleanup(OLAObject);
        }

        /// <summary>
        /// 启用或禁用绘制系统
        /// </summary>
        /// <param name="active">1 启用，0 禁用</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetGuiActive(int active){
            return OLAPlugDLLHelper.DrawGuiSetGuiActive(OLAObject, active);
        }

        /// <summary>
        /// 查询绘制系统是否启用
        /// </summary>
        /// <returns>状态，0 未启用，1 已启用</returns>
        public int DrawGuiIsGuiActive(){
            return OLAPlugDLLHelper.DrawGuiIsGuiActive(OLAObject);
        }

        /// <summary>
        /// 设置绘制窗口是否可穿透点击
        /// </summary>
        /// <param name="enabled">1 可穿透，0 不可穿透</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetGuiClickThrough(int enabled){
            return OLAPlugDLLHelper.DrawGuiSetGuiClickThrough(OLAObject, enabled);
        }

        /// <summary>
        /// 查询绘制窗口是否设置为可穿透
        /// </summary>
        /// <returns>状态，0 否，1 是</returns>
        public int DrawGuiIsGuiClickThrough(){
            return OLAPlugDLLHelper.DrawGuiIsGuiClickThrough(OLAObject);
        }

        /// <summary>
        /// 创建矩形对象
        /// </summary>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="mode">绘制模式，见DrawMode</param>
        /// <param name="lineThickness">线宽（像素），对描边模式有效</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiRectangle(int x, int y, int width, int height, int mode, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiRectangle(OLAObject, x, y, width, height, mode, lineThickness);
        }

        /// <summary>
        /// 创建圆形对象
        /// </summary>
        /// <param name="x">圆心X</param>
        /// <param name="y">圆心Y</param>
        /// <param name="radius">半径</param>
        /// <param name="mode">绘制模式，见DrawMode</param>
        /// <param name="lineThickness">线宽（像素），对描边模式有效</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiCircle(int x, int y, int radius, int mode, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiCircle(OLAObject, x, y, radius, mode, lineThickness);
        }

        /// <summary>
        /// 创建直线对象
        /// </summary>
        /// <param name="x1">起点X</param>
        /// <param name="y1">起点Y</param>
        /// <param name="x2">终点X</param>
        /// <param name="y2">终点Y</param>
        /// <param name="lineThickness">线宽（像素）</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiLine(int x1, int y1, int x2, int y2, double lineThickness){
            return OLAPlugDLLHelper.DrawGuiLine(OLAObject, x1, y1, x2, y2, lineThickness);
        }

        /// <summary>
        /// 创建文本对象
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <param name="fontPath">字体文件路径（ttf/otf）</param>
        /// <param name="fontSize">字号（像素）</param>
        /// <param name="align">对齐方式，见TextAlign</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiText(string text, int x, int y, string fontPath, int fontSize, int align){
            return OLAPlugDLLHelper.DrawGuiText(OLAObject, text, x, y, fontPath, fontSize, align);
        }

        /// <summary>
        /// 创建图片对象
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiImage(string imagePath, int x, int y){
            return OLAPlugDLLHelper.DrawGuiImage(OLAObject, imagePath, x, y);
        }

        /// <summary>
        /// 创建图片对象
        /// </summary>
        /// <param name="imagePtr">图片指针</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiImagePtr(long imagePtr, int x, int y){
            return OLAPlugDLLHelper.DrawGuiImagePtr(OLAObject, imagePtr, x, y);
        }

        /// <summary>
        /// 创建窗口对象
        /// </summary>
        /// <param name="title">标题文本</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="style">窗口样式，见WindowStyle</param>
        /// <returns>窗口句柄，失败返回0</returns>
        public long DrawGuiWindow(string title, int x, int y, int width, int height, int style){
            return OLAPlugDLLHelper.DrawGuiWindow(OLAObject, title, x, y, width, height, style);
        }

        /// <summary>
        /// 创建面板对象
        /// </summary>
        /// <param name="parentHandle">父对象句柄（窗口/面板）</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>面板句柄，失败返回0</returns>
        public long DrawGuiPanel(long parentHandle, int x, int y, int width, int height){
            return OLAPlugDLLHelper.DrawGuiPanel(OLAObject, parentHandle, x, y, width, height);
        }

        /// <summary>
        /// 创建按钮对象
        /// </summary>
        /// <param name="parentHandle">父对象句柄（窗口/面板）</param>
        /// <param name="text">按钮文本</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>按钮句柄，失败返回0</returns>
        public long DrawGuiButton(long parentHandle, string text, int x, int y, int width, int height){
            return OLAPlugDLLHelper.DrawGuiButton(OLAObject, parentHandle, text, x, y, width, height);
        }

        /// <summary>
        /// 设置对象位置
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetPosition(long handle, int x, int y){
            return OLAPlugDLLHelper.DrawGuiSetPosition(OLAObject, handle, x, y);
        }

        /// <summary>
        /// 设置对象尺寸
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetSize(long handle, int width, int height){
            return OLAPlugDLLHelper.DrawGuiSetSize(OLAObject, handle, width, height);
        }

        /// <summary>
        /// 设置对象颜色（RGBA）
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="r">红色分量（0-255）</param>
        /// <param name="g">绿色分量（0-255）</param>
        /// <param name="b">蓝色分量（0-255）</param>
        /// <param name="a">透明度（0-255）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetColor(long handle, int r, int g, int b, int a){
            return OLAPlugDLLHelper.DrawGuiSetColor(OLAObject, handle, r, g, b, a);
        }

        /// <summary>
        /// 设置对象整体透明度
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="alpha">透明度（0-255）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetAlpha(long handle, int alpha){
            return OLAPlugDLLHelper.DrawGuiSetAlpha(OLAObject, handle, alpha);
        }

        /// <summary>
        /// 设置绘制模式
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="mode">绘制模式，见DrawMode</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetDrawMode(long handle, int mode){
            return OLAPlugDLLHelper.DrawGuiSetDrawMode(OLAObject, handle, mode);
        }

        /// <summary>
        /// 设置线宽
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="thickness">线宽（像素）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetLineThickness(long handle, double thickness){
            return OLAPlugDLLHelper.DrawGuiSetLineThickness(OLAObject, handle, thickness);
        }

        /// <summary>
        /// 设置文本字体
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="fontPath">字体文件路径（ttf/otf）</param>
        /// <param name="fontSize">字号（像素）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetFont(long handle, string fontPath, int fontSize){
            return OLAPlugDLLHelper.DrawGuiSetFont(OLAObject, handle, fontPath, fontSize);
        }

        /// <summary>
        /// 设置文本对齐
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="align">对齐方式，见TextAlign</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetTextAlign(long handle, int align){
            return OLAPlugDLLHelper.DrawGuiSetTextAlign(OLAObject, handle, align);
        }

        /// <summary>
        /// 设置文本内容
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="text">文本内容</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetText(long handle, string text){
            return OLAPlugDLLHelper.DrawGuiSetText(OLAObject, handle, text);
        }

        /// <summary>
        /// 设置窗口标题
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="title">标题文本</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetWindowTitle(long handle, string title){
            return OLAPlugDLLHelper.DrawGuiSetWindowTitle(OLAObject, handle, title);
        }

        /// <summary>
        /// 设置窗口样式
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="style">窗口样式，见WindowStyle</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetWindowStyle(long handle, int style){
            return OLAPlugDLLHelper.DrawGuiSetWindowStyle(OLAObject, handle, style);
        }

        /// <summary>
        /// 设置窗口是否置顶
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="topMost">1 置顶，0 取消置顶</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetWindowTopMost(long handle, int topMost){
            return OLAPlugDLLHelper.DrawGuiSetWindowTopMost(OLAObject, handle, topMost);
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="alpha">透明度（0-255）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetWindowTransparency(long handle, int alpha){
            return OLAPlugDLLHelper.DrawGuiSetWindowTransparency(OLAObject, handle, alpha);
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiDeleteObject(long handle){
            return OLAPlugDLLHelper.DrawGuiDeleteObject(OLAObject, handle);
        }

        /// <summary>
        /// 清空所有对象
        /// </summary>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiClearAll(){
            return OLAPlugDLLHelper.DrawGuiClearAll(OLAObject);
        }

        /// <summary>
        /// 设置对象可见性
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="visible">1 可见，0 隐藏</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetVisible(long handle, int visible){
            return OLAPlugDLLHelper.DrawGuiSetVisible(OLAObject, handle, visible);
        }

        /// <summary>
        /// 设置对象Z序（绘制顺序）
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="zOrder">Z序值，数值越大越靠前</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetZOrder(long handle, int zOrder){
            return OLAPlugDLLHelper.DrawGuiSetZOrder(OLAObject, handle, zOrder);
        }

        /// <summary>
        /// 设置对象父子关系
        /// </summary>
        /// <param name="handle">子对象句柄</param>
        /// <param name="parentHandle">父对象句柄</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetParent(long handle, long parentHandle){
            return OLAPlugDLLHelper.DrawGuiSetParent(OLAObject, handle, parentHandle);
        }

        /// <summary>
        /// 设置按钮点击回调
        /// </summary>
        /// <param name="handle">按钮对象句柄</param>
        /// <param name="callback">按钮回调函数指针</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetButtonCallback(long handle, DrawGuiButtonCallback callback){
            return OLAPlugDLLHelper.DrawGuiSetButtonCallback(OLAObject, handle, callback);
        }

        /// <summary>
        /// 设置鼠标事件回调
        /// </summary>
        /// <param name="handle">目标对象句柄</param>
        /// <param name="callback">鼠标回调函数指针</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiSetMouseCallback(long handle, DrawGuiMouseCallback callback){
            return OLAPlugDLLHelper.DrawGuiSetMouseCallback(OLAObject, handle, callback);
        }

        /// <summary>
        /// 获取对象类型
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <returns>对象类型，见DrawType</returns>
        public int DrawGuiGetDrawObjectType(long handle){
            return OLAPlugDLLHelper.DrawGuiGetDrawObjectType(OLAObject, handle);
        }

        /// <summary>
        /// 获取对象位置
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">返回左上角X（输出）</param>
        /// <param name="y">返回左上角Y（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiGetPosition(long handle, out int x, out int y){
            return OLAPlugDLLHelper.DrawGuiGetPosition(OLAObject, handle, out x, out y);
        }

        /// <summary>
        /// 获取对象尺寸
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="width">返回宽度（输出）</param>
        /// <param name="height">返回高度（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DrawGuiGetSize(long handle, out int width, out int height){
            return OLAPlugDLLHelper.DrawGuiGetSize(OLAObject, handle, out width, out height);
        }

        /// <summary>
        /// 判断坐标点是否在对象内
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>结果，0 否，1 是</returns>
        public int DrawGuiIsPointInObject(long handle, int x, int y){
            return OLAPlugDLLHelper.DrawGuiIsPointInObject(OLAObject, handle, x, y);
        }

        /// <summary>
        /// 设置内存读写模式
        /// </summary>
        /// <param name="mode">内存模式 0.远程模式 1.本地模式(需要DLL注入) 2.驱动API方式读写内存 3.驱动MDL 方式读写内存</param>
        /// <returns>1成功 其他失败</returns>
        public int SetMemoryMode(int mode){
            return OLAPlugDLLHelper.SetMemoryMode(OLAObject, mode);
        }

        /// <summary>
        /// 导出驱动
        /// </summary>
        /// <param name="driver_path">驱动路径</param>
        /// <param name="type">驱动类型</param>
        /// <returns>1成功 其他失败</returns>
        public int ExportDriver(string driver_path, int type){
            return OLAPlugDLLHelper.ExportDriver(OLAObject, driver_path, type);
        }

        /// <summary>
        /// 加载驱动
        /// </summary>
        /// <param name="driver_name">驱动名称,为空则初始化欧拉驱动</param>
        /// <param name="driver_path">驱动路径</param>
        /// <returns>1成功 其他失败</returns>
        public int LoadDriver(string driver_name, string driver_path){
            return OLAPlugDLLHelper.LoadDriver(OLAObject, driver_name, driver_path);
        }

        /// <summary>
        /// 卸载驱动
        /// </summary>
        /// <param name="driver_name">驱动名称</param>
        /// <returns>1成功 其他失败</returns>
        public int UnloadDriver(string driver_name){
            return OLAPlugDLLHelper.UnloadDriver(OLAObject, driver_name);
        }

        /// <summary>
        /// 测试驱动是否正常加载
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int DriverTest(){
            return OLAPlugDLLHelper.DriverTest(OLAObject);
        }

        /// <summary>
        /// 加载PDB文件,驱动加载失败时可以尝试加载PDB文件
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int LoadPdb(){
            return OLAPlugDLLHelper.LoadPdb(OLAObject);
        }

        /// <summary>
        /// 隐藏进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="enable">是否隐藏</param>
        /// <returns>1成功 其他失败</returns>
        public int HideProcess(long pid, int enable){
            return OLAPlugDLLHelper.HideProcess(OLAObject, pid, enable);
        }

        /// <summary>
        /// 保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="enable">是否保护</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectProcess(long pid, int enable){
            return OLAPlugDLLHelper.ProtectProcess(OLAObject, pid, enable);
        }

        /// <summary>
        /// 添加保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="mode">保护模式</param>
        /// <param name="allow_pid">允许的进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int AddProtectPID(long pid, long mode, long allow_pid){
            return OLAPlugDLLHelper.AddProtectPID(OLAObject, pid, mode, allow_pid);
        }

        /// <summary>
        /// 删除保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int RemoveProtectPID(long pid){
            return OLAPlugDLLHelper.RemoveProtectPID(OLAObject, pid);
        }

        /// <summary>
        /// 添加允许进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int AddAllowPID(long pid){
            return OLAPlugDLLHelper.AddAllowPID(OLAObject, pid);
        }

        /// <summary>
        /// 删除允许进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int RemoveAllowPID(long pid){
            return OLAPlugDLLHelper.RemoveAllowPID(OLAObject, pid);
        }

        /// <summary>
        /// 伪装进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="fake_pid">伪装的目标进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int FakeProcess(long pid, long fake_pid){
            return OLAPlugDLLHelper.FakeProcess(OLAObject, pid, fake_pid);
        }

        /// <summary>
        /// 保护窗口,防止截屏
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="flag">保护标志 0还原 1黑屏 2透明</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectWindow(long hwnd, int flag){
            return OLAPlugDLLHelper.ProtectWindow(OLAObject, hwnd, flag);
        }

        /// <summary>
        /// 打开进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="process_handle">进程句柄</param>
        /// <returns>1成功 其他失败</returns>
        public int KeOpenProcess(long pid, out long process_handle){
            return OLAPlugDLLHelper.KeOpenProcess(OLAObject, pid, out process_handle);
        }

        /// <summary>
        /// 打开线程
        /// </summary>
        /// <param name="thread_id">线程ID</param>
        /// <param name="thread_handle">线程句柄</param>
        /// <returns>1成功 其他失败</returns>
        public int KeOpenThread(long thread_id, out long thread_handle){
            return OLAPlugDLLHelper.KeOpenThread(OLAObject, thread_id, out thread_handle);
        }

        /// <summary>
        /// 启动安全守护
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int StartSecurityGuard(){
            return OLAPlugDLLHelper.StartSecurityGuard(OLAObject);
        }

        /// <summary>
        /// 生成RSA密钥
        /// </summary>
        /// <param name="publicKeyPath">公钥路径</param>
        /// <param name="privateKeyPath">私钥路径</param>
        /// <param name="type">类型,取值如下:
        ///<br/> 0: 生成pem格式秘钥
        ///<br/> 1: 生成xml格式秘钥
        ///<br/> 2: 生成PKCS1格式秘钥
        /// </param>
        /// <param name="keySize">密钥大小,取值如下:
        ///<br/> 512: 512位
        ///<br/> 1024: 1024位
        ///<br/> 2048: 2048位
        ///<br/> 4096: 4096位
        /// </param>
        /// <returns>0 成功,其他 失败</returns>
        public int GenerateRSAKey(string publicKeyPath, string privateKeyPath, int type, int keySize){
            return OLAPlugDLLHelper.GenerateRSAKey(OLAObject, publicKeyPath, privateKeyPath, type, keySize);
        }

        /// <summary>
        /// 转换RSA公钥
        /// </summary>
        /// <param name="publicKey">公钥</param>
        /// <param name="inputType">输入类型,取值如下:
        ///<br/> 0: pem格式
        ///<br/> 1: xml格式
        ///<br/> 2: PKCS1格式
        /// </param>
        /// <param name="outputType">输出类型,取值如下:
        ///<br/> 0: pem格式
        ///<br/> 1: xml格式
        ///<br/> 2: PKCS1格式
        /// </param>
        /// <returns>成功返回转换后的公钥字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string ConvertRSAPublicKey(string publicKey, int inputType, int outputType){
            return PtrToStringUTF8(OLAPlugDLLHelper.ConvertRSAPublicKey(OLAObject, publicKey, inputType, outputType));
        }

        /// <summary>
        /// 转换RSA私钥
        /// </summary>
        /// <param name="privateKey">私钥</param>
        /// <param name="inputType">输入类型,取值如下:
        ///<br/> 0: pem格式
        ///<br/> 1: xml格式
        ///<br/> 2: PKCS1格式
        /// </param>
        /// <param name="outputType">输出类型,取值如下:
        ///<br/> 0: pem格式
        ///<br/> 1: xml格式
        ///<br/> 2: PKCS1格式
        /// </param>
        /// <returns>成功返回转换后的私钥字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string ConvertRSAPrivateKey(string privateKey, int inputType, int outputType){
            return PtrToStringUTF8(OLAPlugDLLHelper.ConvertRSAPrivateKey(OLAObject, privateKey, inputType, outputType));
        }

        /// <summary>
        /// 使用RSA公钥加密
        /// </summary>
        /// <param name="message">明文</param>
        /// <param name="publicKey">公钥</param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: PKCS1
        ///<br/> 1: OAEP
        /// </param>
        /// <returns>成功返回加密后的密文字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string EncryptWithRsa(string message, string publicKey, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.EncryptWithRsa(OLAObject, message, publicKey, paddingType));
        }

        /// <summary>
        /// 使用RSA私钥解密
        /// </summary>
        /// <param name="cipher">密文</param>
        /// <param name="privateKey">私钥</param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: PKCS1
        ///<br/> 1: OAEP
        /// </param>
        /// <returns>成功返回解密后的明文字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string DecryptWithRsa(string cipher, string privateKey, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.DecryptWithRsa(OLAObject, cipher, privateKey, paddingType));
        }

        /// <summary>
        /// 使用RSA私钥签名
        /// </summary>
        /// <param name="message">明文</param>
        /// <param name="privateCer">私钥</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 0: MD5
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        ///<br/> 5: SHA3-256
        ///<br/> 6: SHA3-384
        ///<br/> 7: SHA3-512
        /// </param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: Pkcs1
        ///<br/> 1: Pss
        /// </param>
        /// <returns>成功返回签名后的base64字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string SignWithRsa(string message, string privateCer, int shaType, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SignWithRsa(OLAObject, message, privateCer, shaType, paddingType));
        }

        /// <summary>
        /// 使用RSA公钥验证签名
        /// </summary>
        /// <param name="message">明文</param>
        /// <param name="signature">签名</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 0: MD5
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        ///<br/> 5: SHA3-256
        ///<br/> 6: SHA3-384
        ///<br/> 7: SHA3-512
        /// </param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: Pkcs1
        ///<br/> 1: Pss
        /// </param>
        /// <param name="publicCer">公钥</param>
        /// <returns>成功返回验证结果；1表示验证成功，0表示验证失败</returns>
        public int VerifySignWithRsa(string message, string signature, int shaType, int paddingType, string publicCer){
            return OLAPlugDLLHelper.VerifySignWithRsa(OLAObject, message, signature, shaType, paddingType, publicCer);
        }

        /// <summary>
        /// AES加密简化版本，使用默认参数
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="key">密钥字符串长度应为16/24/32个字符，对应AES-128/192/256</param>
        /// <returns>成功返回加密后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 此接口使用CBC模式和PKCS7填充，默认IV为0。如需自定义参数请使用 AESEncryptEx
        /// </remarks>
        public string AESEncrypt(string source, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESEncrypt(OLAObject, source, key));
        }

        /// <summary>
        /// AES解密简化版本，使用默认参数
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="key">密钥字符串长度应为16/24/32个字符，对应AES-128/192/256)</param>
        /// <returns>成功返回解密后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>2. 此接口使用CBC模式和PKCS7填充，默认IV为0。如需自定义参数请使用 AESDecryptEx
        /// </remarks>
        public string AESDecrypt(string source, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESDecrypt(OLAObject, source, key));
        }

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始向量</param>
        /// <param name="mode">加密模式,取值如下:
        ///<br/> 0: CBC
        ///<br/> 1: ECB
        ///<br/> 2: CFB
        ///<br/> 3: OFB
        ///<br/> 4: CTS
        /// </param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: PKCS7
        ///<br/> 1: Zeros
        ///<br/> 2: AnsiX923
        ///<br/> 3: ISO10126
        ///<br/> 4: NoPadding
        /// </param>
        /// <returns>成功返回加密后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string AESEncryptEx(string source, string key, string iv, int mode, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESEncryptEx(OLAObject, source, key, iv, mode, paddingType));
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始向量</param>
        /// <param name="mode">加密模式,取值如下:
        ///<br/> 0: CBC
        ///<br/> 1: ECB
        ///<br/> 2: CFB
        ///<br/> 3: OFB
        ///<br/> 4: CTS
        /// </param>
        /// <param name="paddingType">填充类型,取值如下:
        ///<br/> 0: PKCS7
        ///<br/> 1: Zeros
        ///<br/> 2: AnsiX923
        ///<br/> 3: ISO10126
        ///<br/> 4: NoPadding
        /// </param>
        /// <returns>成功返回解密后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string AESDecryptEx(string source, string key, string iv, int mode, int paddingType){
            return PtrToStringUTF8(OLAPlugDLLHelper.AESDecryptEx(OLAObject, source, key, iv, mode, paddingType));
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="source">源数据</param>
        /// <returns>成功返回加密后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string MD5Encrypt(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.MD5Encrypt(OLAObject, source));
        }

        /// <summary>
        /// SHA系列哈希算法
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 0: MD5
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        ///<br/> 5: SHA3-256
        ///<br/> 6: SHA3-384
        ///<br/> 7: SHA3-512
        /// </param>
        /// <returns>成功返回哈希后的数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string SHAHash(string source, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SHAHash(OLAObject, source, shaType));
        }

        /// <summary>
        /// HMAC消息认证码
        /// </summary>
        /// <param name="source">源数据</param>
        /// <param name="key">密钥</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 0: MD5
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        ///<br/> 5: SHA3-256
        ///<br/> 6: SHA3-384
        ///<br/> 7: SHA3-512
        /// </param>
        /// <returns>成功返回HMAC值；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string HMAC(string source, string key, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.HMAC(OLAObject, source, key, shaType));
        }

        /// <summary>
        /// 生成随机字节
        /// </summary>
        /// <param name="length">要生成的随机字节长度</param>
        /// <param name="type">字符类型,取值如下:
        ///<br/> 0: 十六进制字符(0-9A-F)
        ///<br/> 1: 数字+大写字母(0-9A-Z)
        ///<br/> 2: 数字+大小写字母(0-9A-Za-z)
        ///<br/> 3: 可打印ASCII字符(包含特殊字符)
        ///<br/> 4: Base64字符集(A-Za-z0-9+/)
        /// </param>
        /// <returns>成功返回随机字节字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 可直接用作AES密钥，推荐长度：16/24/32
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string GenerateRandomBytes(int length, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.GenerateRandomBytes(OLAObject, length, type));
        }

        /// <summary>
        /// 生成GUID
        /// </summary>
        /// <param name="type">类型,取值如下:
        ///<br/> 0: 带-的GUID如{123e4567-e89b-12d3-a456-426614174000}
        ///<br/> 1: 不带-的GUID如123e4567e89b12d3a456426614174000
        /// </param>
        /// <returns>成功返回GUID字符串的指针；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string GenerateGuid(int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.GenerateGuid(OLAObject, type));
        }

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="source">源数据</param>
        /// <returns>成功返回Base64编码后的字符串；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string Base64Encode(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.Base64Encode(OLAObject, source));
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="source">Base64编码的字符串</param>
        /// <returns>成功返回解码后的原始数据；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string Base64Decode(string source){
            return PtrToStringUTF8(OLAPlugDLLHelper.Base64Decode(OLAObject, source));
        }

        /// <summary>
        /// PBKDF2密钥派生函数
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="salt">盐值</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="keyLength">派生密钥长度</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        /// </param>
        /// <returns>成功返回派生密钥；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string PBKDF2(string password, string salt, int iterations, int keyLength, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.PBKDF2(OLAObject, password, salt, iterations, keyLength, shaType));
        }

        /// <summary>
        /// 计算文件MD5哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>成功返回MD5哈希值；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string MD5File(string filePath){
            return PtrToStringUTF8(OLAPlugDLLHelper.MD5File(OLAObject, filePath));
        }

        /// <summary>
        /// 计算文件SHA哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="shaType">哈希类型,取值如下:
        ///<br/> 0: MD5
        ///<br/> 1: SHA1
        ///<br/> 2: SHA256
        ///<br/> 3: SHA384
        ///<br/> 4: SHA512
        ///<br/> 5: SHA3-256
        ///<br/> 6: SHA3-384
        ///<br/> 7: SHA3-512
        /// </param>
        /// <returns>成功返回哈希值；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// </remarks>
        public string SHAFile(string filePath, int shaType){
            return PtrToStringUTF8(OLAPlugDLLHelper.SHAFile(OLAObject, filePath, shaType));
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int CreateFolder(string path){
            return OLAPlugDLLHelper.CreateFolder(OLAObject, path);
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DeleteFolder(string path){
            return OLAPlugDLLHelper.DeleteFolder(OLAObject, path);
        }

        /// <summary>
        /// 获取文件夹列表
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="baseDir">基础目录,不为空时返回这个相对路径</param>
        /// <returns>返回字符串，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string GetFolderList(string path, string baseDir){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFolderList(OLAObject, path, baseDir));
        }

        /// <summary>
        /// 判断文件夹是否存在
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int IsDirectory(string path){
            return OLAPlugDLLHelper.IsDirectory(OLAObject, path);
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int IsFile(string path){
            return OLAPlugDLLHelper.IsFile(OLAObject, path);
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int CreateFile(string path){
            return OLAPlugDLLHelper.CreateFile(OLAObject, path);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int DeleteFile(string path){
            return OLAPlugDLLHelper.DeleteFile(OLAObject, path);
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int CopyFile(string src, string dst){
            return OLAPlugDLLHelper.CopyFile(OLAObject, src, dst);
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int MoveFile(string src, string dst){
            return OLAPlugDLLHelper.MoveFile(OLAObject, src, dst);
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int RenameFile(string src, string dst){
            return OLAPlugDLLHelper.RenameFile(OLAObject, src, dst);
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件大小，失败返回0</returns>
        public long GetFileSize(string path){
            return OLAPlugDLLHelper.GetFileSize(OLAObject, path);
        }

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="baseDir">基础目录,不为空时返回这个相对路径</param>
        /// <returns>返回字符串，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string GetFileList(string path, string baseDir){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFileList(OLAObject, path, baseDir));
        }

        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="withExtension">是否包含扩展名</param>
        /// <returns>文件名，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string GetFileName(string path, int withExtension){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetFileName(OLAObject, path, withExtension));
        }

        /// <summary>
        /// 转为绝对路径
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>绝对路径，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string ToAbsolutePath(string path){
            return PtrToStringUTF8(OLAPlugDLLHelper.ToAbsolutePath(OLAObject, path));
        }

        /// <summary>
        /// 转为相对路径
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>相对路径，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string ToRelativePath(string path){
            return PtrToStringUTF8(OLAPlugDLLHelper.ToRelativePath(OLAObject, path));
        }

        /// <summary>
        /// 判断文件/目录是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int FileOrDirectoryExists(string path){
            return OLAPlugDLLHelper.FileOrDirectoryExists(OLAObject, path);
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码
        ///<br/> -1: : 自动检测编码
        ///<br/> 0: : GBK字符串
        ///<br/> 1: : Unicode字符串
        ///<br/> 2: : UTF8字符串
        ///<br/> 3: : UTF-8 with BOM auto-remove
        /// </param>
        /// <returns>返回字符串，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string ReadFileString(string filePath, int encoding){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadFileString(OLAObject, filePath, encoding));
        }

        /// <summary>
        /// 从文件中读取指定偏移量的指定大小的字节
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="offset">偏移量</param>
        /// <param name="size">大小,0表示读取整个文件</param>
        /// <returns>返回缓冲区地址,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的缓冲区地址需调用FreeMemoryPtr接口释放内存
        /// </remarks>
        public long ReadBytesFromFile(string filePath, int offset, long size){
            return OLAPlugDLLHelper.ReadBytesFromFile(OLAObject, filePath, offset, size);
        }

        /// <summary>
        /// 将字节流写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="dataAddr">数据地址</param>
        /// <param name="dataSize">数据大小</param>
        /// <returns>是否成功</returns>
        public int WriteBytesToFile(string filePath, long dataAddr, int dataSize){
            return OLAPlugDLLHelper.WriteBytesToFile(OLAObject, filePath, dataAddr, dataSize);
        }

        /// <summary>
        /// 将字符串写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="data">数据</param>
        /// <param name="encoding">编码</param>
        /// <returns>是否成功</returns>
        public int WriteStringToFile(string filePath, string data, int encoding){
            return OLAPlugDLLHelper.WriteStringToFile(OLAObject, filePath, data, encoding);
        }

        /// <summary>
        /// 启动全局钩子
        /// </summary>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数必须在程序启动时调用，否则无法注册热键。
        /// </remarks>
        public int StartHotkeyHook(){
            return OLAPlugDLLHelper.StartHotkeyHook(OLAObject);
        }

        /// <summary>
        /// 停止全局钩子
        /// </summary>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数用于停止之前启动的全局钩子。在程序退出前应该调用此函数以清理资源。
        /// </remarks>
        public int StopHotkeyHook(){
            return OLAPlugDLLHelper.StopHotkeyHook(OLAObject);
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="keycode">按键代码，例如VK_F1, VK_A等</param>
        /// <param name="modifiers">修饰键组合，使用Modifier枚举值的位或组合 例如：Modifier::CTRL | Modifier::ALT</param>
        /// <param name="callback">回调函数，当热键被触发时调用</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册一个全局热键，当用户按下指定的按键组合时会触发回调函数。
        /// </remarks>
        public int RegisterHotkey(int keycode, int modifiers, HotkeyCallback callback){
            return OLAPlugDLLHelper.RegisterHotkey(OLAObject, keycode, modifiers, callback);
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="keycode">按键代码，必须与RegisterHotkey时使用的相同</param>
        /// <param name="modifiers">修饰键组合，必须与RegisterHotkey时使用的相同</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注销之前注册的热键。在程序退出前应该注销所有注册的快捷键。
        /// </remarks>
        public int UnregisterHotkey(int keycode, int modifiers){
            return OLAPlugDLLHelper.UnregisterHotkey(OLAObject, keycode, modifiers);
        }

        /// <summary>
        /// 注册鼠标按钮事件
        /// </summary>
        /// <param name="button">鼠标按钮，使用MouseButtons枚举值</param>
        /// <param name="type">鼠标按钮类型，使用MouseButtonType枚举值</param>
        /// <param name="callback">回调函数，当鼠标按钮事件被触发时调用</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册一个全局鼠标按钮事件，当用户按下鼠标按钮时会触发回调函数。
        /// </remarks>
        public int RegisterMouseButton(int button, int type, MouseCallback callback){
            return OLAPlugDLLHelper.RegisterMouseButton(OLAObject, button, type, callback);
        }

        /// <summary>
        /// 注销鼠标按钮事件
        /// </summary>
        /// <param name="button">鼠标按钮，必须与RegisterMouseButton时使用的相同</param>
        /// <param name="type"></param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注销之前注册的鼠标按钮事件。在程序退出前应该注销所有注册的鼠标按钮事件。
        /// </remarks>
        public int UnregisterMouseButton(int button, int type){
            return OLAPlugDLLHelper.UnregisterMouseButton(OLAObject, button, type);
        }

        /// <summary>
        /// 注册鼠标滚轮事件
        /// </summary>
        /// <param name="callback">回调函数，当鼠标滚轮事件被触发时调用</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册一个全局鼠标滚轮事件，当用户滚动鼠标滚轮时会触发回调函数。
        /// </remarks>
        public int RegisterMouseWheel(MouseWheelCallback callback){
            return OLAPlugDLLHelper.RegisterMouseWheel(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标滚轮事件
        /// </summary>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在程序退出前应该注销所有注册的鼠标滚轮事件。
        /// </remarks>
        public int UnregisterMouseWheel(){
            return OLAPlugDLLHelper.UnregisterMouseWheel(OLAObject);
        }

        /// <summary>
        /// 注册鼠标移动事件
        /// </summary>
        /// <param name="callback">回调函数，当鼠标移动事件被触发时调用</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册一个全局鼠标移动事件，当用户移动鼠标时会触发回调函数。
        /// </remarks>
        public int RegisterMouseMove(MouseMoveCallback callback){
            return OLAPlugDLLHelper.RegisterMouseMove(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标移动事件
        /// </summary>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注销之前注册的鼠标移动事件。在程序退出前应该注销所有注册的鼠标拖动事件。
        /// </remarks>
        public int UnregisterMouseMove(){
            return OLAPlugDLLHelper.UnregisterMouseMove(OLAObject);
        }

        /// <summary>
        /// 注册鼠标拖动事件
        /// </summary>
        /// <param name="callback">回调函数，当鼠标拖动事件被触发时调用</param>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册一个全局鼠标拖动事件，当用户拖动鼠标时会触发回调函数。
        /// </remarks>
        public int RegisterMouseDrag(MouseDragCallback callback){
            return OLAPlugDLLHelper.RegisterMouseDrag(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标拖动事件
        /// </summary>
        /// <returns>int32_t 返回1表示成功，非0表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在程序退出前应该注销所有注册的鼠标拖动事件。
        /// </remarks>
        public int UnregisterMouseDrag(){
            return OLAPlugDLLHelper.UnregisterMouseDrag(OLAObject);
        }

        /// <summary>
        /// 注入DLL
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="dll_path">dll路径</param>
        /// <param name="type">注入类型</param>
        /// <param name="bypassGuard">是否绕过保护</param>
        /// <returns>是否成功</returns>
        public int Inject(long hwnd, string dll_path, int type, int bypassGuard){
            return OLAPlugDLLHelper.Inject(OLAObject, hwnd, dll_path, type, bypassGuard);
        }

        /// <summary>
        /// 从URL注入DLL
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="url">URL</param>
        /// <param name="type">注入类型</param>
        /// <param name="bypassGuard">是否绕过保护</param>
        /// <returns>是否成功</returns>
        public int InjectFromUrl(long hwnd, string url, int type, int bypassGuard){
            return OLAPlugDLLHelper.InjectFromUrl(OLAObject, hwnd, url, type, bypassGuard);
        }

        /// <summary>
        /// 从内存注入DLL
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="bufferAddr">内存数据地址</param>
        /// <param name="bufferSize">内存数据大小</param>
        /// <param name="type">注入类型</param>
        /// <param name="bypassGuard">是否绕过保护</param>
        /// <returns>是否成功</returns>
        public int InjectFromBuffer(long hwnd, long bufferAddr, int bufferSize, int type, int bypassGuard){
            return OLAPlugDLLHelper.InjectFromBuffer(OLAObject, hwnd, bufferAddr, bufferSize, type, bypassGuard);
        }

        /// <summary>
        /// 创建空的JSON对象
        /// </summary>
        /// <returns>返回新创建的JSON对象句柄，失败时返回0</returns>
        public long JsonCreateObject(){
            return OLAPlugDLLHelper.JsonCreateObject();
        }

        /// <summary>
        /// 创建空的JSON数组
        /// </summary>
        /// <returns>返回新创建的JSON数组句柄，失败时返回0</returns>
        public long JsonCreateArray(){
            return OLAPlugDLLHelper.JsonCreateArray();
        }

        /// <summary>
        /// 解析JSON字符串
        /// </summary>
        /// <param name="str">要解析的JSON字符串</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回解析后的JSON对象句柄，失败时返回0</returns>
        public long JsonParse(string str, out int err){
            return OLAPlugDLLHelper.JsonParse(str, out err);
        }

        /// <summary>
        /// 将JSON对象序列化为字符串
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="indent">缩进空格数，0表示不格式化</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回JSON字符串，需调用FreeStringPtr释放，失败时返回0</returns>
        public string JsonStringify(long obj, int indent, out int err){
            return PtrToStringUTF8(OLAPlugDLLHelper.JsonStringify(obj, indent, out err));
        }

        /// <summary>
        /// 释放JSON对象
        /// </summary>
        /// <param name="obj">要释放的JSON对象句柄</param>
        /// <returns>成功返回1，失败返回0</returns>
        public int JsonFree(long obj){
            return OLAPlugDLLHelper.JsonFree(obj);
        }

        /// <summary>
        /// 获取JSON对象中的值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回对应的JSON值句柄，失败时返回0</returns>
        public long JsonGetValue(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetValue(obj, key, out err);
        }

        /// <summary>
        /// 获取JSON数组中的元素
        /// </summary>
        /// <param name="arr">JSON数组句柄</param>
        /// <param name="index">元素索引</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回数组元素句柄，失败时返回0</returns>
        public long JsonGetArrayItem(long arr, int index, out int err){
            return OLAPlugDLLHelper.JsonGetArrayItem(arr, index, out err);
        }

        /// <summary>
        /// 获取JSON对象中的字符串值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回字符串值，需调用FreeStringPtr释放，失败时返回0</returns>
        public string JsonGetString(long obj, string key, out int err){
            return PtrToStringUTF8(OLAPlugDLLHelper.JsonGetString(obj, key, out err));
        }

        /// <summary>
        /// 获取JSON对象中的数值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回数值，失败时返回0.0</returns>
        public double JsonGetNumber(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetNumber(obj, key, out err);
        }

        /// <summary>
        /// 获取JSON对象中的布尔值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回布尔值，失败时返回0</returns>
        public int JsonGetBool(long obj, string key, out int err){
            return OLAPlugDLLHelper.JsonGetBool(obj, key, out err);
        }

        /// <summary>
        /// 获取JSON对象或数组的大小
        /// </summary>
        /// <param name="obj">JSON对象或数组句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: JSON解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 键不存在
        ///<br/> 5: 索引超出范围
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回对象属性数量或数组长度，失败时返回0</returns>
        public int JsonGetSize(long obj, out int err){
            return OLAPlugDLLHelper.JsonGetSize(obj, out err);
        }

        /// <summary>
        /// 设置JSON对象中的值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="value">要设置的值句柄</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonSetValue(long obj, string key, long value){
            return OLAPlugDLLHelper.JsonSetValue(obj, key, value);
        }

        /// <summary>
        /// 向JSON数组添加元素
        /// </summary>
        /// <param name="arr">JSON数组句柄</param>
        /// <param name="value">要添加的元素句柄</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonArrayAppend(long arr, long value){
            return OLAPlugDLLHelper.JsonArrayAppend(arr, value);
        }

        /// <summary>
        /// 设置JSON对象中的字符串值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="value">字符串值</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonSetString(long obj, string key, string value){
            return OLAPlugDLLHelper.JsonSetString(obj, key, value);
        }

        /// <summary>
        /// 设置JSON对象中的数值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="value">数值</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonSetNumber(long obj, string key, double value){
            return OLAPlugDLLHelper.JsonSetNumber(obj, key, value);
        }

        /// <summary>
        /// 设置JSON对象中的布尔值
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">键名</param>
        /// <param name="value">布尔值</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonSetBool(long obj, string key, int value){
            return OLAPlugDLLHelper.JsonSetBool(obj, key, value);
        }

        /// <summary>
        /// 删除JSON对象中的键
        /// </summary>
        /// <param name="obj">JSON对象句柄</param>
        /// <param name="key">要删除的键名</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonDeleteKey(long obj, string key){
            return OLAPlugDLLHelper.JsonDeleteKey(obj, key);
        }

        /// <summary>
        /// 清空JSON对象或数组
        /// </summary>
        /// <param name="obj">JSON对象或数组句柄</param>
        /// <returns>返回操作结果错误码
        ///<br/>0: 操作成功
        ///<br/>1: 无效的句柄
        ///<br/>2: JSON解析失败
        ///<br/>3: 类型不匹配
        ///<br/>4: 键不存在
        ///<br/>5: 索引超出范围
        ///<br/>6: 未知错误
        /// </returns>
        public int JsonClear(long obj){
            return OLAPlugDLLHelper.JsonClear(obj);
        }

        /// <summary>
        /// 解析匹配图像JSON
        /// </summary>
        /// <param name="str">匹配图像JSON字符串</param>
        /// <param name="matchState">匹配状态</param>
        /// <param name="x">匹配点X坐标</param>
        /// <param name="y">匹配点Y坐标</param>
        /// <param name="width">匹配宽度</param>
        /// <param name="height">匹配高度</param>
        /// <param name="matchVal">匹配值</param>
        /// <param name="angle">匹配角度</param>
        /// <param name="index">匹配索引</param>
        /// <returns>返回操作结果错误码
        ///<br/>1: 解析成功
        ///<br/>0: 解析失败
        /// </returns>
        public int ParseMatchImageJson(string str, out int matchState, out int x, out int y, out int width, out int height, out double matchVal, out double angle, out int index){
            return OLAPlugDLLHelper.ParseMatchImageJson(str, out matchState, out x, out y, out width, out height, out matchVal, out angle, out index);
        }

        /// <summary>
        /// 获取匹配图像JSON数量
        /// </summary>
        /// <param name="str">匹配图像JSON字符串</param>
        /// <returns>返回匹配图像JSON数量</returns>
        public int GetMatchImageAllCount(string str){
            return OLAPlugDLLHelper.GetMatchImageAllCount(str);
        }

        /// <summary>
        /// 解析匹配图像JSON所有
        /// </summary>
        /// <param name="str">匹配图像JSON字符串</param>
        /// <param name="parseIndex">解析索引</param>
        /// <param name="matchState">匹配状态</param>
        /// <param name="x">匹配点X坐标</param>
        /// <param name="y">匹配点Y坐标</param>
        /// <param name="width">匹配宽度</param>
        /// <param name="height">匹配高度</param>
        /// <param name="matchVal">匹配值</param>
        /// <param name="angle">匹配角度</param>
        /// <param name="index">匹配索引</param>
        /// <returns>返回操作结果错误码
        ///<br/>1: 解析成功
        ///<br/>0: 解析失败
        /// </returns>
        public int ParseMatchImageAllJson(string str, int parseIndex, out int matchState, out int x, out int y, out int width, out int height, out double matchVal, out double angle, out int index){
            return OLAPlugDLLHelper.ParseMatchImageAllJson(str, parseIndex, out matchState, out x, out y, out width, out height, out matchVal, out angle, out index);
        }

        /// <summary>
        /// 对插件部分接口的返回值进行解析,并返回result中的元素个数,针对JSON格式和,|分割的字符串
        /// </summary>
        /// <param name="resultStr"></param>
        /// <returns>整型数: result中的元素个数。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数用于对插件部分接口的返回值进行解析,并返回result中的元素个数。
        /// </remarks>
        public int GetResultCount(string resultStr){
            return OLAPlugDLLHelper.GetResultCount(resultStr);
        }

        /// <summary>
        /// 生成鼠标移动轨迹数据,用于二次开发
        /// </summary>
        /// <param name="startX">起点X坐标</param>
        /// <param name="startY">起点Y坐标</param>
        /// <param name="endX">终点X坐标</param>
        /// <param name="endY">终点Y坐标</param>
        /// <returns>返回轨迹数据,如 [{"deltaX": 8,"deltaY": 5,"time": 7,"x": 108,"y": 105}, ...]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public List<Point> GenerateMouseTrajectory(int startX, int startY, int endX, int endY){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.GenerateMouseTrajectory(OLAObject, startX, startY, endX, endY));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 按住指定的虚拟键码
        /// </summary>
        /// <param name="vk_code">按键码</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyDown(int vk_code){
            return OLAPlugDLLHelper.KeyDown(OLAObject, vk_code);
        }

        /// <summary>
        /// 弹起来虚拟键vk_code
        /// </summary>
        /// <param name="vk_code">按键码</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyUp(int vk_code){
            return OLAPlugDLLHelper.KeyUp(OLAObject, vk_code);
        }

        /// <summary>
        /// 按下指定的虚拟键码
        /// </summary>
        /// <param name="vk_code">按键码</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyPress(int vk_code){
            return OLAPlugDLLHelper.KeyPress(OLAObject, vk_code);
        }

        /// <summary>
        /// 按住鼠标左键
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int LeftDown(){
            return OLAPlugDLLHelper.LeftDown(OLAObject);
        }

        /// <summary>
        /// 弹起鼠标左键
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int LeftUp(){
            return OLAPlugDLLHelper.LeftUp(OLAObject);
        }

        /// <summary>
        /// 把鼠标移动到目的点(x, y)
        /// </summary>
        /// <param name="x">目标X坐标</param>
        /// <param name="y">目标Y坐标</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int MoveTo(int x, int y){
            return OLAPlugDLLHelper.MoveTo(OLAObject, x, y);
        }

        /// <summary>
        /// 把鼠标移动到目的点(x, y),不使用鼠标轨迹,即使开启鼠标轨迹这个接口也不会生效
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int MoveToWithoutSimulator(int x, int y){
            return OLAPlugDLLHelper.MoveToWithoutSimulator(OLAObject, x, y);
        }

        /// <summary>
        /// 执行鼠标右键点击操作
        /// </summary>
        /// <returns>0: 失败, 1: 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数执行完整的右键点击操作（按下并释放）
        /// <br/>2. 如果需要单独控制按下和释放，请使用 RightDown 和 RightUp 函数
        /// <br/>3. 点击操作会使用当前鼠标位置
        /// <br/>4. 如果需要移动到特定位置后点击，请先使用 MoveTo 函数
        /// <br/>5. 在调用此函数前，确保鼠标右键未被其他程序占用
        /// </remarks>
        public int RightClick(){
            return OLAPlugDLLHelper.RightClick(OLAObject);
        }

        /// <summary>
        /// 鼠标右键双击
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int RightDoubleClick(){
            return OLAPlugDLLHelper.RightDoubleClick(OLAObject);
        }

        /// <summary>
        /// 按住鼠标右键
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int RightDown(){
            return OLAPlugDLLHelper.RightDown(OLAObject);
        }

        /// <summary>
        /// 弹起鼠标右键
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int RightUp(){
            return OLAPlugDLLHelper.RightUp(OLAObject);
        }

        /// <summary>
        /// 获取鼠标特征码
        /// </summary>
        /// <returns>返回鼠标特征码</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 并非所有的游戏都支持后台鼠标特征码,在获取特征码之前,需先操作鼠标
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string GetCursorShape(){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetCursorShape(OLAObject));
        }

        /// <summary>
        /// 获取鼠标图标
        /// </summary>
        /// <returns>OLAImage对象的地址</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 图片使用完后需要调用 FreeImagePtr 接口进行释放
        /// </remarks>
        public long GetCursorImage(){
            return OLAPlugDLLHelper.GetCursorImage(OLAObject);
        }

        /// <summary>
        /// 根据指定的字符串序列，依次按顺序按下其中的字符
        /// </summary>
        /// <param name="keyStr"></param>
        /// <param name="delay">每按下一个按键，需要延时多久。单位毫秒（ms），这个值越大，按的速度越慢</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在某些情况下，SendString和SendString2都无法输入文字时，可以考虑用这个来输入
        /// <br/>2. 但这个接口只支持"a-z 0-9 ~-=[];',./"和空格,其它字符一律不支持.(包括中国)
        /// </remarks>
        public int KeyPressStr(string keyStr, int delay){
            return OLAPlugDLLHelper.KeyPressStr(OLAObject, keyStr, delay);
        }

        /// <summary>
        /// 发送字符串到指定窗口
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="str">字符串</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int SendString(long hwnd, string str){
            return OLAPlugDLLHelper.SendString(OLAObject, hwnd, str);
        }

        /// <summary>
        /// 发送字符串到指定地址
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <param name="type">类型  字符串类型,取值如下
        ///<br/> 0: GBK字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <returns></returns>
        public int SendStringEx(long hwnd, long addr, int len, int type){
            return OLAPlugDLLHelper.SendStringEx(OLAObject, hwnd, addr, len, type);
        }

        /// <summary>
        /// 按下指定的虚拟键码key_str
        /// </summary>
        /// <param name="keyStr"></param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyPressChar(string keyStr){
            return OLAPlugDLLHelper.KeyPressChar(OLAObject, keyStr);
        }

        /// <summary>
        /// 按住指定的虚拟键码key_str
        /// </summary>
        /// <param name="keyStr"></param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyDownChar(string keyStr){
            return OLAPlugDLLHelper.KeyDownChar(OLAObject, keyStr);
        }

        /// <summary>
        /// 弹起来虚拟键key_str
        /// </summary>
        /// <param name="keyStr"></param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int KeyUpChar(string keyStr){
            return OLAPlugDLLHelper.KeyUpChar(OLAObject, keyStr);
        }

        /// <summary>
        /// 鼠标相对于上次的位置移动rx, ry, 前台模式鼠标相对移动时相对当前鼠标位置
        /// </summary>
        /// <param name="rx">相对于上次的X偏移</param>
        /// <param name="ry">相对于上次的Y偏移</param>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int MoveR(int rx, int ry){
            return OLAPlugDLLHelper.MoveR(OLAObject, rx, ry);
        }

        /// <summary>
        /// 滚轮点击
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int MiddleClick(){
            return OLAPlugDLLHelper.MiddleClick(OLAObject);
        }

        /// <summary>
        /// 将鼠标移动到指定范围内的随机位置
        /// </summary>
        /// <param name="x">目标区域左上角的X坐标</param>
        /// <param name="y">目标区域左上角的Y坐标</param>
        /// <param name="w">目标区域的宽度（从x计算起）</param>
        /// <param name="h">目标区域的高度（从y计算起）</param>
        /// <returns>DLL调用: 返回字符串指针，包含移动后的坐标，格式为"x,y"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 需要调用 FreeStringPtr 释放内存
        /// <br/>2. 此函数会在指定范围内随机选择一个点作为目标位置
        /// <br/>3. 坐标系统原点(0,0)在屏幕左上角
        /// <br/>4. 确保指定的范围在屏幕可见区域内
        /// <br/>5. 如果范围参数无效（如负数），函数将返回失败
        /// <br/>6. 移动操作是即时的，没有动画效果
        /// <br/>7. 建议在移动后添加适当的延时，使操作更自然
        /// </remarks>
        public string MoveToEx(int x, int y, int w, int h){
            return PtrToStringUTF8(OLAPlugDLLHelper.MoveToEx(OLAObject, x, y, w, h));
        }

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <param name="x">返回的鼠标X坐标</param>
        /// <param name="y">返回的鼠标Y坐标</param>
        /// <returns>0 ：失败, 1 ：成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此接口绑定后使用，获取的是相当游戏窗口的鼠标坐标
        /// </remarks>
        public int GetCursorPos(out int x, out int y){
            return OLAPlugDLLHelper.GetCursorPos(OLAObject, out x, out y);
        }

        /// <summary>
        /// 弹起鼠标中键
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int MiddleUp(){
            return OLAPlugDLLHelper.MiddleUp(OLAObject);
        }

        /// <summary>
        /// 按住鼠标中键
        /// </summary>
        /// <returns>0: 失败, 1: 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数仅模拟按下中键，不会自动释放
        /// <br/>2. 如果需要释放中键，需要调用 MiddleUp 函数
        /// <br/>3. 建议在操作完成后及时释放中键，避免影响后续操作
        /// <br/>4. 如果系统不支持中键操作，函数将返回失败
        /// <br/>5. 在调用此函数前，确保鼠标中键未被其他程序占用
        /// </remarks>
        public int MiddleDown(){
            return OLAPlugDLLHelper.MiddleDown(OLAObject);
        }

        /// <summary>
        /// 滚轮双击
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数执行完整的鼠标中键双击操作（按下并释放）
        /// <br/>2. 如果需要单独控制按下和释放，请使用 MiddleDown 和 MiddleUp 函数
        /// <br/>3. 点击操作会使用当前鼠标位置
        /// <br/>4. 如果需要移动到特定位置后点击，请先使用 MoveTo 函数
        /// <br/>5. 在调用此函数前，确保鼠标中键未被其他程序占用
        /// </remarks>
        public int MiddleDoubleClick(){
            return OLAPlugDLLHelper.MiddleDoubleClick(OLAObject);
        }

        /// <summary>
        /// 鼠标左键点击
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int LeftClick(){
            return OLAPlugDLLHelper.LeftClick(OLAObject);
        }

        /// <summary>
        /// 鼠标左键双击
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int LeftDoubleClick(){
            return OLAPlugDLLHelper.LeftDoubleClick(OLAObject);
        }

        /// <summary>
        /// 滚轮向上滚
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int WheelUp(){
            return OLAPlugDLLHelper.WheelUp(OLAObject);
        }

        /// <summary>
        /// 滚轮向下滚
        /// </summary>
        /// <returns>0 : 失败, 1 : 成功</returns>
        public int WheelDown(){
            return OLAPlugDLLHelper.WheelDown(OLAObject);
        }

        /// <summary>
        /// 等待指定的按键按下 (前台,不是后台)
        /// </summary>
        /// <param name="vk_code">等待的按键码</param>
        /// <param name="time_out">等待超时时间，单位毫秒</param>
        /// <returns>0 : 超时, 1 : 指定的按键按下</returns>
        public int WaitKey(int vk_code, int time_out){
            return OLAPlugDLLHelper.WaitKey(OLAObject, vk_code, time_out);
        }

        /// <summary>
        /// 设置当前系统鼠标的精确度开关
        /// </summary>
        /// <param name="enable">0 关闭指针精确度开关. 1打开指针精确度开关. 一般推荐关闭</param>
        /// <returns>设置之前的精确度开关</returns>
        public int EnableMouseAccuracy(int enable){
            return OLAPlugDLLHelper.EnableMouseAccuracy(OLAObject, enable);
        }

        /// <summary>
        /// @brief 把双精度浮点数转换成二进制形式（IEEE 754标准）
        /// </summary>
        /// <param name="double_value">需要转换的double值</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DoubleToData(double double_value){
            return PtrToStringUTF8(OLAPlugDLLHelper.DoubleToData(OLAObject, double_value));
        }

        /// <summary>
        /// 把单精度浮点数转换成二进制形式. IEEE 754标准
        /// </summary>
        /// <param name="float_value">float值</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FloatToData(float float_value){
            return PtrToStringUTF8(OLAPlugDLLHelper.FloatToData(OLAObject, float_value));
        }

        /// <summary>
        /// 把字符串转换成二进制形式.
        /// </summary>
        /// <param name="string_value">字符串值</param>
        /// <param name="type">类型0: 返回Ascii表达的字符串1: 返回Unicode表达的字符串2: 返回UTF8表达的字符串</param>
        /// <returns>返回二进制字符串的指针@title 字符串转二进制 - StringToData</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string StringToData(string string_value, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.StringToData(OLAObject, string_value, type));
        }

        /// <summary>
        /// 把64位整数转换成32位整数.
        /// </summary>
        /// <param name="v">64位整数</param>
        /// <returns>32位整数@title 64位整数转32位整数 - Int64ToInt32</returns>
        public int Int64ToInt32(long v){
            return OLAPlugDLLHelper.Int64ToInt32(OLAObject, v);
        }

        /// <summary>
        /// 把32位整数转换成64位整数.
        /// </summary>
        /// <param name="v">32位整数</param>
        /// <returns>64位整数@title 32位整数转64位整数 - Int32ToInt64</returns>
        public long Int32ToInt64(int v){
            return OLAPlugDLLHelper.Int32ToInt64(OLAObject, v);
        }

        /// <summary>
        /// 搜索指定的二进制数据,默认步长是1.默认开启多线程,默认搜索全部内存类型.如果要定制搜索,请用FindDataEx
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="data">要搜索的二进制数据,支持CE数据格式 比如"00 01 23 45 * ?? ?b c? * f1"等.</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindData(long hwnd, string addr_range, string data){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindData(OLAObject, hwnd, addr_range, data));
        }

        /// <summary>
        /// 搜索指定的二进制数据,默认步长是1.默认开启多线程,默认搜索全部内存类型.如果要定制搜索,请用FindDataEx
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="data">要搜索的二进制数据,支持CE数据格式 比如"00 01 23 45 * ?? ?b c? * f1"等.</param>
        /// <param name="step">步长</param>
        /// <param name="multi_thread">是否开启多线程</param>
        /// <param name="mode">搜索模式
        ///<br/> 0: 搜索全部内存类型
        ///<br/> 1: 搜索可写内存
        ///<br/> 2: 不搜索可写内存
        ///<br/> 4: 搜索可执行内存
        ///<br/> 8: 不搜索可执行内存
        ///<br/> 16: 搜索写时复制内存
        ///<br/> 32: 不搜索写时复制内存
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDataEx(long hwnd, string addr_range, string data, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDataEx(OLAObject, hwnd, addr_range, data, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的双精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="double_value_min">最小值</param>
        /// <param name="double_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDouble(long hwnd, string addr_range, double double_value_min, double double_value_max){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDouble(OLAObject, hwnd, addr_range, double_value_min, double_value_max));
        }

        /// <summary>
        /// 搜索指定范围内的双精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="double_value_min">最小值</param>
        /// <param name="double_value_max">最大值</param>
        /// <param name="step">步长</param>
        /// <param name="multi_thread">是否开启多线程</param>
        /// <param name="mode">搜索模式
        ///<br/> 0: 搜索全部内存类型
        ///<br/> 1: 搜索可写内存
        ///<br/> 2: 不搜索可写内存
        ///<br/> 4: 搜索可执行内存
        ///<br/> 8: 不搜索可执行内存
        ///<br/> 16: 搜索写时复制内存
        ///<br/> 32: 不搜索写时复制内存
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDoubleEx(long hwnd, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindDoubleEx(OLAObject, hwnd, addr_range, double_value_min, double_value_max, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的单精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="float_value_min">最小值</param>
        /// <param name="float_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindFloat(long hwnd, string addr_range, float float_value_min, float float_value_max){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindFloat(OLAObject, hwnd, addr_range, float_value_min, float_value_max));
        }

        /// <summary>
        /// 搜索指定范围内的单精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="float_value_min">最小值</param>
        /// <param name="float_value_max">最大值</param>
        /// <param name="step">步长</param>
        /// <param name="multi_thread">是否开启多线程</param>
        /// <param name="mode">搜索模式
        ///<br/> 0: 搜索全部内存类型
        ///<br/> 1: 搜索可写内存
        ///<br/> 2: 不搜索可写内存
        ///<br/> 4: 搜索可执行内存
        ///<br/> 8: 不搜索可执行内存
        ///<br/> 16: 搜索写时复制内存
        ///<br/> 32: 不搜索写时复制内存
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindFloatEx(long hwnd, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindFloatEx(OLAObject, hwnd, addr_range, float_value_min, float_value_max, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的长整型数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="int_value_min">最小值</param>
        /// <param name="int_value_max">最大值</param>
        /// <param name="type"></param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindInt(long hwnd, string addr_range, long int_value_min, long int_value_max, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindInt(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type));
        }

        /// <summary>
        /// 搜索指定范围内的长整型数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="int_value_min">最小值</param>
        /// <param name="int_value_max">最大值</param>
        /// <param name="type">搜索的整数类型,取值如下
        ///<br/> 0: 32位
        ///<br/> 1: 16 位
        ///<br/> 2: 8位
        ///<br/> 3: 64位
        /// </param>
        /// <param name="step">步长</param>
        /// <param name="multi_thread">是否开启多线程</param>
        /// <param name="mode">搜索模式
        ///<br/> 0: 搜索全部内存类型
        ///<br/> 1: 搜索可写内存
        ///<br/> 2: 不搜索可写内存
        ///<br/> 4: 搜索可执行内存
        ///<br/> 8: 不搜索可执行内存
        ///<br/> 16: 搜索写时复制内存
        ///<br/> 32: 不搜索写时复制内存
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindIntEx(long hwnd, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindIntEx(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="string_value">要搜索的字符串</param>
        /// <param name="type">类型
        ///<br/> 0: 返回Ascii表达的字符串
        ///<br/> 1: 返回Unicode表达的字符串
        ///<br/> 2: 返回UTF8表达的字符串
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindString(long hwnd, string addr_range, string string_value, int type){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindString(OLAObject, hwnd, addr_range, string_value, type));
        }

        /// <summary>
        /// 搜索指定范围内的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="string_value">要搜索的字符串</param>
        /// <param name="type">类型
        ///<br/> 0: 返回Ascii表达的字符串
        ///<br/> 1: 返回Unicode表达的字符串
        ///<br/> 2: 返回UTF8表达的字符串
        /// </param>
        /// <param name="step">步长</param>
        /// <param name="multi_thread">是否开启多线程</param>
        /// <param name="mode">搜索模式
        ///<br/> 0: 搜索全部内存类型
        ///<br/> 1: 搜索可写内存
        ///<br/> 2: 不搜索可写内存
        ///<br/> 4: 搜索可执行内存
        ///<br/> 8: 不搜索可执行内存
        ///<br/> 16: 搜索写时复制内存
        ///<br/> 32: 不搜索写时复制内存
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn" 比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindStringEx(long hwnd, string addr_range, string string_value, int type, int step, int multi_thread, int mode){
            return PtrToStringUTF8(OLAPlugDLLHelper.FindStringEx(OLAObject, hwnd, addr_range, string_value, type, step, multi_thread, mode));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔 比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadData(long hwnd, string addr, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadData(OLAObject, hwnd, addr, len));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔 比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadDataAddr(long hwnd, long addr, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadDataAddr(OLAObject, hwnd, addr, len));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的数据字符串指针. 返回0表示读取失败.</returns>
        public long ReadDataAddrToBin(long hwnd, long addr, int len){
            return OLAPlugDLLHelper.ReadDataAddrToBin(OLAObject, hwnd, addr, len);
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的内存地址</returns>
        public long ReadDataToBin(long hwnd, string addr, int len){
            return OLAPlugDLLHelper.ReadDataToBin(OLAObject, hwnd, addr, len);
        }

        /// <summary>
        /// 读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double ReadDouble(long hwnd, string addr){
            return OLAPlugDLLHelper.ReadDouble(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double ReadDoubleAddr(long hwnd, long addr){
            return OLAPlugDLLHelper.ReadDoubleAddr(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float ReadFloat(long hwnd, string addr){
            return OLAPlugDLLHelper.ReadFloat(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float ReadFloatAddr(long hwnd, long addr){
            return OLAPlugDLLHelper.ReadFloatAddr(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的长整型数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">类型
        ///<br/> 0: 32位有符号
        ///<br/> 1: 16位有符号
        ///<br/> 2: 8位有符号
        ///<br/> 3: 64位
        ///<br/> 4: 32位无符号
        ///<br/> 5: 16位无符号
        ///<br/> 6: 8位无符号
        /// </param>
        /// <returns>读取到的整数值64位</returns>
        public long ReadInt(long hwnd, string addr, int type){
            return OLAPlugDLLHelper.ReadInt(OLAObject, hwnd, addr, type);
        }

        /// <summary>
        /// 读取指定地址的长整型数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="type">类型
        ///<br/> 0: 32位有符号
        ///<br/> 1: 16位有符号
        ///<br/> 2: 8位有符号
        ///<br/> 3: 64位
        ///<br/> 4: 32位无符号
        ///<br/> 5: 16位无符号
        ///<br/> 6: 8位无符号
        /// </param>
        /// <returns>读取到的整数值64位</returns>
        public long ReadIntAddr(long hwnd, long addr, int type){
            return OLAPlugDLLHelper.ReadIntAddr(OLAObject, hwnd, addr, type);
        }

        /// <summary>
        /// 读取指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">类型  字符串类型,取值如下
        ///<br/> 0: : GBK字符串
        ///<br/> 1: : Unicode字符串
        ///<br/> 2: : UTF8字符串
        /// </param>
        /// <param name="len">需要读取的字节数目.如果为0，则自动判定字符串长度.</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的字符串,以UTF-8编码</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadString(long hwnd, string addr, int type, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadString(OLAObject, hwnd, addr, type, len));
        }

        /// <summary>
        /// 读取指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="type">类型  字符串类型,取值如下
        ///<br/> 0: GBK字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="len">需要读取的字节数目.如果为0，则自动判定字符串长度.</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的字符串,以UTF-8编码</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadStringAddr(long hwnd, long addr, int type, int len){
            return PtrToStringUTF8(OLAPlugDLLHelper.ReadStringAddr(OLAObject, hwnd, addr, type, len));
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteData(long hwnd, string addr, string data){
            return OLAPlugDLLHelper.WriteData(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">字符串数据地址</param>
        /// <param name="len"></param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteDataFromBin(long hwnd, string addr, long data, int len){
            return OLAPlugDLLHelper.WriteDataFromBin(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteDataAddr(long hwnd, long addr, string data){
            return OLAPlugDLLHelper.WriteDataAddr(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <param name="len"></param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteDataAddrFromBin(long hwnd, long addr, long data, int len){
            return OLAPlugDLLHelper.WriteDataAddrFromBin(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteDouble(long hwnd, string addr, double double_value){
            return OLAPlugDLLHelper.WriteDouble(OLAObject, hwnd, addr, double_value);
        }

        /// <summary>
        /// 写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteDoubleAddr(long hwnd, long addr, double double_value){
            return OLAPlugDLLHelper.WriteDoubleAddr(OLAObject, hwnd, addr, double_value);
        }

        /// <summary>
        /// 写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteFloat(long hwnd, string addr, float float_value){
            return OLAPlugDLLHelper.WriteFloat(OLAObject, hwnd, addr, float_value);
        }

        /// <summary>
        /// 写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteFloatAddr(long hwnd, long addr, float float_value){
            return OLAPlugDLLHelper.WriteFloatAddr(OLAObject, hwnd, addr, float_value);
        }

        /// <summary>
        /// 写入指定地址的整数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">类型
        ///<br/> 0: 32位有符号
        ///<br/> 1: 16位有符号
        ///<br/> 2: 8位有符号
        ///<br/> 3: 64位
        ///<br/> 4: 32位无符号
        ///<br/> 5: 16位无符号
        ///<br/> 6: 8位无符号
        /// </param>
        /// <param name="value">要写入的整数值</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteInt(long hwnd, string addr, int type, long value){
            return OLAPlugDLLHelper.WriteInt(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 写入指定地址的整数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="type">类型
        ///<br/> 0: 32位有符号
        ///<br/> 1: 16位有符号
        ///<br/> 2: 8位有符号
        ///<br/> 3: 64位
        ///<br/> 4: 32位无符号
        ///<br/> 5: 16位无符号
        ///<br/> 6: 8位无符号
        /// </param>
        /// <param name="value">要写入的整数值</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteIntAddr(long hwnd, long addr, int type, long value){
            return OLAPlugDLLHelper.WriteIntAddr(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 写入指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式 比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[ [<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: Ascii字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="value">要写入的字符串</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteString(long hwnd, string addr, int type, string value){
            return OLAPlugDLLHelper.WriteString(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 写入指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: Ascii字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="value">要写入的字符串</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int WriteStringAddr(long hwnd, long addr, int type, string value){
            return OLAPlugDLLHelper.WriteStringAddr(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 设置是否把所有内存接口函数中的窗口句柄当作进程ID
        /// </summary>
        /// <param name="enable">是否启用
        ///<br/> 0: 不启用
        ///<br/> 1: 启用
        /// </param>
        /// <returns>成功返回1,失败返回0</returns>
        public int SetMemoryHwndAsProcessId(int enable){
            return OLAPlugDLLHelper.SetMemoryHwndAsProcessId(OLAObject, enable);
        }

        /// <summary>
        /// 释放进程内存
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int FreeProcessMemory(long hwnd){
            return OLAPlugDLLHelper.FreeProcessMemory(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取模块基地址
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="module_name">模块名</param>
        /// <returns>成功返回模块基地址,失败返回0</returns>
        public long GetModuleBaseAddr(long hwnd, string module_name){
            return OLAPlugDLLHelper.GetModuleBaseAddr(OLAObject, hwnd, module_name);
        }

        /// <summary>
        /// 获取模块大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="module_name">模块名</param>
        /// <returns>成功返回模块大小,失败返回0</returns>
        public int GetModuleSize(long hwnd, string module_name){
            return OLAPlugDLLHelper.GetModuleSize(OLAObject, hwnd, module_name);
        }

        /// <summary>
        /// 获取远程API地址
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="base_addr">基地址</param>
        /// <param name="fun_name">函数名</param>
        /// <returns>成功返回远程API地址,失败返回0</returns>
        public long GetRemoteApiAddress(long hwnd, long base_addr, string fun_name){
            return OLAPlugDLLHelper.GetRemoteApiAddress(OLAObject, hwnd, base_addr, fun_name);
        }

        /// <summary>
        /// 在指定的窗口所在进程分配一段内存
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID. 默认是窗口句柄. 如果要指定为进程ID,需要调用SetMemoryHwndAsProcessId</param>
        /// <param name="addr">预期的分配地址。如果是0表示自动分配，否则就尝试在此地址上分配内存</param>
        /// <param name="size">需要分配的内存大小</param>
        /// <param name="type">需要分配的内存类型，取值如下:
        ///<br/> 0: 可读可写可执行
        ///<br/> 1: 可读可执行，不可写
        ///<br/> 2: 可读可写,不可执行
        /// </param>
        /// <returns>分配的内存地址，如果是0表示分配失败</returns>
        public long VirtualAllocEx(long hwnd, long addr, int size, int type){
            return OLAPlugDLLHelper.VirtualAllocEx(OLAObject, hwnd, addr, size, type);
        }

        /// <summary>
        /// 释放指定的内存
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要释放的内存地址</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int VirtualFreeEx(long hwnd, long addr){
            return OLAPlugDLLHelper.VirtualFreeEx(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 修改指定的内存保护属性
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要修改的内存地址</param>
        /// <param name="size">需要修改的内存大小</param>
        /// <param name="type">需要修改的内存类型，取值如下:
        ///<br/> 0: 可读可写可执行
        ///<br/> 1: 可读可执行，不可写
        ///<br/> 2: 可读可写,不可执行
        /// </param>
        /// <param name="protect"></param>
        /// <returns>成功返回修改之前的读写属性,失败返回-1</returns>
        public int VirtualProtectEx(long hwnd, long addr, int size, int type, int protect){
            return OLAPlugDLLHelper.VirtualProtectEx(OLAObject, hwnd, addr, size, type, protect);
        }

        /// <summary>
        /// 查询指定的内存信息
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要查询的内存地址</param>
        /// <param name="pmbi">内存信息结构体指针</param>
        /// <returns>成功返回1,失败返回0</returns>
        public string VirtualQueryEx(long hwnd, long addr, long pmbi){
            return PtrToStringUTF8(OLAPlugDLLHelper.VirtualQueryEx(OLAObject, hwnd, addr, pmbi));
        }

        /// <summary>
        /// 在指定的窗口所在进程创建一个线程
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="lpStartAddress">线程入口地址</param>
        /// <param name="lpParameter">线程参数</param>
        /// <param name="dwCreationFlags">创建标志</param>
        /// <param name="lpThreadId">返回线程ID</param>
        /// <returns>成功返回线程句柄,失败返回0</returns>
        public long CreateRemoteThread(long hwnd, long lpStartAddress, long lpParameter, int dwCreationFlags, out long lpThreadId){
            return OLAPlugDLLHelper.CreateRemoteThread(OLAObject, hwnd, lpStartAddress, lpParameter, dwCreationFlags, out lpThreadId);
        }

        /// <summary>
        /// 关闭一个内核对象
        /// </summary>
        /// <param name="handle">要关闭的对象句柄</param>
        /// <returns>成功返回1,失败返回0</returns>
        public int CloseHandle(long handle){
            return OLAPlugDLLHelper.CloseHandle(OLAObject, handle);
        }

        /// <summary>
        /// 识别指定窗口区域内的文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string Ocr(int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.Ocr(OLAObject, x1, y1, x2, y2));
        }

        /// <summary>
        /// 识别指定图像中的文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string OcrFromPtr(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromPtr(OLAObject, ptr));
        }

        /// <summary>
        /// 识别BMP数据中的文字
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="size">BMP图像数据大小</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string OcrFromBmpData(long ptr, int size){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromBmpData(OLAObject, ptr, size));
        }

        /// <summary>
        /// 识别指定窗口区域内的文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点 Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrDetails(int x1, int y1, int x2, int y2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrDetails(OLAObject, x1, y1, x2, y2));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 识别指定图像中的文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点 Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrFromPtrDetails(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromPtrDetails(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 识别BMP数据中的文字
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="size">BMP图像数据大小</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点 Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrFromBmpDataDetails(long ptr, int size){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromBmpDataDetails(OLAObject, ptr, size));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 使用V5模型识别指定窗口区域内的文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string OcrV5(int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrV5(OLAObject, x1, y1, x2, y2));
        }

        /// <summary>
        /// 使用V5模型识别指定窗口区域内的文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点 Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrV5Details(int x1, int y1, int x2, int y2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrV5Details(OLAObject, x1, y1, x2, y2));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 使用V5模型识别指定图像中的文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string OcrV5FromPtr(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrV5FromPtr(OLAObject, ptr));
        }

        /// <summary>
        /// 使用V5模型识别指定图像中的文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点 Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrV5FromPtrDetails(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrV5FromPtrDetails(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 获取OCR配置
        /// </summary>
        /// <param name="configKey">配置键</param>
        /// <returns>配置值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 支持所有OCR配置参数，包括：
        /// <br/>2. GPU相关参数
        /// <br/>3. OcrUseGpu (bool): 是否使用GPU推理，false使用CPU，true使用GPU，默认false
        /// <br/>4. OcrUseTensorrt (bool): 是否使用TensorRT加速，默认false
        /// <br/>5. OcrGpuId (int): GPU设备ID，0表示第一个GPU，默认0
        /// <br/>6. OcrGpuMem (int): GPU内存大小(MB)，默认4000
        /// <br/>7. CPU相关参数
        /// <br/>8. OcrCpuThreads (int): CPU线程数，默认8
        /// <br/>9. OcrEnableMkldnn (bool): 是否启用MKL-DNN加速，默认true
        /// <br/>10. 推理相关参数
        /// <br/>11. OcrPrecision (string): 推理精度，可选fp32/fp16/int8，默认"int8"
        /// <br/>12. OcrBenchmark (bool): 是否启用性能基准测试，默认false
        /// <br/>13. OcrOutput (string): 基准测试日志保存路径，默认"./output/"
        /// <br/>14. OcrImageDir (string): 输入图像目录，默认""
        /// <br/>15. OcrType (string): 执行类型，ocr或structure，默认"ocr"
        /// <br/>16. 检测相关参数
        /// <br/>17. OcrDetModelDir (string): 检测模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_det_infer/"
        /// <br/>18. OcrLimitType (string): 输入图像限制类型，max或min，默认"max"
        /// <br/>19. OcrLimitSideLen (int): 输入图像限制边长，默认960
        /// <br/>20. OcrDetDbThresh (double): 检测DB阈值，范围0.0-1.0，默认0.3
        /// <br/>21. OcrDetDbBoxThresh (double): 检测DB框阈值，范围0.0-1.0，默认0.6
        /// <br/>22. OcrDetDbUnclipRatio (double): 检测DB未裁剪比例，默认1.5
        /// <br/>23. OcrUseDilation (bool): 是否对输出图使用膨胀操作，默认false
        /// <br/>24. OcrDetDbScoreMode (string): 检测DB评分模式，fast或slow，默认"slow"
        /// <br/>25. OcrVisualize (bool): 是否显示检测结果，默认true
        /// <br/>26. 识别相关参数
        /// <br/>27. OcrRecModelDir (string): 识别模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_rec_infer/"
        /// <br/>28. OcrRecBatchNum (int): 识别批处理数量，默认6
        /// <br/>29. OcrRecCharDictPath (string): 识别字符字典路径，默认"./ppocr/utils/ppocr_keys_v1.txt"
        /// <br/>30. OcrRecImgH (int): 识别图像高度，默认48
        /// <br/>31. OcrRecImgW (int): 识别图像宽度，默认320
        /// <br/>32. 分类相关参数
        /// <br/>33. OcrUseAngleCls (bool): 是否使用角度分类，默认false
        /// <br/>34. OcrClsModelDir (string): 分类模型路径，默认""
        /// <br/>35. OcrClsThresh (double): 分类阈值，范围0.0-1.0，默认0.9
        /// <br/>36. OcrClsBatchNum (int): 分类批处理数量，默认1
        /// <br/>37. 布局相关参数
        /// <br/>38. OcrLayoutModelDir (string): 布局模型路径，默认""
        /// <br/>39. OcrLayoutDictPath (string): 布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string): 表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
        /// <br/>48. 前向相关参数
        /// <br/>49. OcrDet (bool): 是否使用检测，默认true
        /// <br/>50. OcrRec (bool): 是否使用识别，默认true
        /// <br/>51. OcrCls (bool): 是否使用分类，默认false
        /// <br/>52. OcrTable (bool): 是否使用表格结构，默认false
        /// <br/>53. OcrLayout (bool): 是否使用布局分析，默认false
        /// <br/>54. 配置值以JSON字符串形式返回，需要根据参数类型进行转换
        /// <br/>55. 与 SetOcrConfig 和 SetOcrConfigByKey 函数配合使用
        /// <br/>56. 适用于OCR配置管理和调试场景
        /// </remarks>
        public string GetOcrConfig(string configKey){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetOcrConfig(OLAObject, configKey));
        }

        /// <summary>
        /// 设置OCR配置
        /// </summary>
        /// <param name="configStr">配置字符串</param>
        /// <returns>是否成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 支持所有OCR配置参数，包括：
        /// <br/>2. GPU相关参数
        /// <br/>3. OcrUseGpu (bool): 是否使用GPU推理，false使用CPU，true使用GPU，默认false
        /// <br/>4. OcrUseTensorrt (bool): 是否使用TensorRT加速，默认false
        /// <br/>5. OcrGpuId (int): GPU设备ID，0表示第一个GPU，默认0
        /// <br/>6. OcrGpuMem (int): GPU内存大小(MB)，默认4000
        /// <br/>7. CPU相关参数
        /// <br/>8. OcrCpuThreads (int): CPU线程数，默认8
        /// <br/>9. OcrEnableMkldnn (bool): 是否启用MKL-DNN加速，默认true
        /// <br/>10. 推理相关参数
        /// <br/>11. OcrPrecision (string): 推理精度，可选fp32/fp16/int8，默认"int8"
        /// <br/>12. OcrBenchmark (bool): 是否启用性能基准测试，默认false
        /// <br/>13. OcrOutput (string): 基准测试日志保存路径，默认"./output/"
        /// <br/>14. OcrImageDir (string): 输入图像目录，默认""
        /// <br/>15. OcrType (string): 执行类型，ocr或structure，默认"ocr"
        /// <br/>16. 检测相关参数
        /// <br/>17. OcrDetModelDir (string): 检测模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_det_infer/"
        /// <br/>18. OcrLimitType (string): 输入图像限制类型，max或min，默认"max"
        /// <br/>19. OcrLimitSideLen (int): 输入图像限制边长，默认960
        /// <br/>20. OcrDetDbThresh (double): 检测DB阈值，范围0.0-1.0，默认0.3
        /// <br/>21. OcrDetDbBoxThresh (double): 检测DB框阈值，范围0.0-1.0，默认0.6
        /// <br/>22. OcrDetDbUnclipRatio (double): 检测DB未裁剪比例，默认1.5
        /// <br/>23. OcrUseDilation (bool): 是否对输出图使用膨胀操作，默认false
        /// <br/>24. OcrDetDbScoreMode (string): 检测DB评分模式，fast或slow，默认"slow"
        /// <br/>25. OcrVisualize (bool): 是否显示检测结果，默认true
        /// <br/>26. 识别相关参数
        /// <br/>27. OcrRecModelDir (string): 识别模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_rec_infer/"
        /// <br/>28. OcrRecBatchNum (int): 识别批处理数量，默认6
        /// <br/>29. OcrRecCharDictPath (string): 识别字符字典路径，默认"./ppocr/utils/ppocr_keys_v1.txt"
        /// <br/>30. OcrRecImgH (int): 识别图像高度，默认48
        /// <br/>31. OcrRecImgW (int): 识别图像宽度，默认320
        /// <br/>32. 分类相关参数
        /// <br/>33. OcrUseAngleCls (bool): 是否使用角度分类，默认false
        /// <br/>34. OcrClsModelDir (string): 分类模型路径，默认""
        /// <br/>35. OcrClsThresh (double): 分类阈值，范围0.0-1.0，默认0.9
        /// <br/>36. OcrClsBatchNum (int): 分类批处理数量，默认1
        /// <br/>37. 布局相关参数
        /// <br/>38. OcrLayoutModelDir (string): 布局模型路径，默认""
        /// <br/>39. OcrLayoutDictPath (string): 布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string): 表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
        /// <br/>48. 前向相关参数
        /// <br/>49. OcrDet (bool): 是否使用检测，默认true
        /// <br/>50. OcrRec (bool): 是否使用识别，默认true
        /// <br/>51. OcrCls (bool): 是否使用分类，默认false
        /// <br/>52. OcrTable (bool): 是否使用表格结构，默认false
        /// <br/>53. OcrLayout (bool): 是否使用布局分析，默认false
        /// <br/>54. 配置值以JSON字符串形式返回，需要根据参数类型进行转换
        /// <br/>55. 与 SetOcrConfig 和 SetOcrConfigByKey 函数配合使用
        /// <br/>56. 适用于OCR配置管理和调试场景
        /// </remarks>
        public int SetOcrConfig(string configStr){
            return OLAPlugDLLHelper.SetOcrConfig(OLAObject, configStr);
        }

        /// <summary>
        /// 设置OCR配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        /// <returns>是否成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 支持所有OCR配置参数，包括：
        /// <br/>2. GPU相关参数
        /// <br/>3. OcrUseGpu (bool): 是否使用GPU推理，false使用CPU，true使用GPU，默认false
        /// <br/>4. OcrUseTensorrt (bool): 是否使用TensorRT加速，默认false
        /// <br/>5. OcrGpuId (int): GPU设备ID，0表示第一个GPU，默认0
        /// <br/>6. OcrGpuMem (int): GPU内存大小(MB)，默认4000
        /// <br/>7. CPU相关参数
        /// <br/>8. OcrCpuThreads (int): CPU线程数，默认8
        /// <br/>9. OcrEnableMkldnn (bool): 是否启用MKL-DNN加速，默认true
        /// <br/>10. 推理相关参数
        /// <br/>11. OcrPrecision (string): 推理精度，可选fp32/fp16/int8，默认"int8"
        /// <br/>12. OcrBenchmark (bool): 是否启用性能基准测试，默认false
        /// <br/>13. OcrOutput (string): 基准测试日志保存路径，默认"./output/"
        /// <br/>14. OcrImageDir (string): 输入图像目录，默认""
        /// <br/>15. OcrType (string): 执行类型，ocr或structure，默认"ocr"
        /// <br/>16. 检测相关参数
        /// <br/>17. OcrDetModelDir (string): 检测模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_det_infer/"
        /// <br/>18. OcrLimitType (string): 输入图像限制类型，max或min，默认"max"
        /// <br/>19. OcrLimitSideLen (int): 输入图像限制边长，默认960
        /// <br/>20. OcrDetDbThresh (double): 检测DB阈值，范围0.0-1.0，默认0.3
        /// <br/>21. OcrDetDbBoxThresh (double): 检测DB框阈值，范围0.0-1.0，默认0.6
        /// <br/>22. OcrDetDbUnclipRatio (double): 检测DB未裁剪比例，默认1.5
        /// <br/>23. OcrUseDilation (bool): 是否对输出图使用膨胀操作，默认false
        /// <br/>24. OcrDetDbScoreMode (string): 检测DB评分模式，fast或slow，默认"slow"
        /// <br/>25. OcrVisualize (bool): 是否显示检测结果，默认true
        /// <br/>26. 识别相关参数
        /// <br/>27. OcrRecModelDir (string): 识别模型路径，默认"./OCRv5_model/PP-OCRv5_mobile_rec_infer/"
        /// <br/>28. OcrRecBatchNum (int): 识别批处理数量，默认6
        /// <br/>29. OcrRecCharDictPath (string): 识别字符字典路径，默认"./ppocr/utils/ppocr_keys_v1.txt"
        /// <br/>30. OcrRecImgH (int): 识别图像高度，默认48
        /// <br/>31. OcrRecImgW (int): 识别图像宽度，默认320
        /// <br/>32. 分类相关参数
        /// <br/>33. OcrUseAngleCls (bool): 是否使用角度分类，默认false
        /// <br/>34. OcrClsModelDir (string): 分类模型路径，默认""
        /// <br/>35. OcrClsThresh (double): 分类阈值，范围0.0-1.0，默认0.9
        /// <br/>36. OcrClsBatchNum (int): 分类批处理数量，默认1
        /// <br/>37. 布局相关参数
        /// <br/>38. OcrLayoutModelDir (string): 布局模型路径，默认""
        /// <br/>39. OcrLayoutDictPath (string): 布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string): 表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
        /// <br/>48. 前向相关参数
        /// <br/>49. OcrDet (bool): 是否使用检测，默认true
        /// <br/>50. OcrRec (bool): 是否使用识别，默认true
        /// <br/>51. OcrCls (bool): 是否使用分类，默认false
        /// <br/>52. OcrTable (bool): 是否使用表格结构，默认false
        /// <br/>53. OcrLayout (bool): 是否使用布局分析，默认false
        /// <br/>54. 配置值以JSON字符串形式返回，需要根据参数类型进行转换
        /// <br/>55. 与 SetOcrConfig 和 SetOcrConfigByKey 函数配合使用
        /// <br/>56. 适用于OCR配置管理和调试场景
        /// </remarks>
        public int SetOcrConfigByKey(string key, string value){
            return OLAPlugDLLHelper.SetOcrConfigByKey(OLAObject, key, value);
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string OcrFromDict(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDict(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string OcrFromDict(int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDict(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public OcrResult OcrFromDictDetails(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictDetails(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public OcrResult OcrFromDictDetails(int x1, int y1, int x2, int y2, string colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictDetails(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string OcrFromDictPtr(long ptr, List<ColorModel> colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string OcrFromDictPtr(long ptr, string colorJson, string dict_name, double matchVal){
            return PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtr(OLAObject, ptr, colorJson, dict_name, matchVal));
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public OcrResult OcrFromDictPtrDetails(long ptr, List<ColorModel> colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtrDetails(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 从字库中识别文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public OcrResult OcrFromDictPtrDetails(long ptr, string colorJson, string dict_name, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.OcrFromDictPtrDetails(OLAObject, ptr, colorJson, dict_name, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 查找文字
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson"></param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="outX">输出参数，返回的X坐标</param>
        /// <param name="outY">输出参数，返回的Y坐标</param>
        /// <returns>成功返回 1，失败返回 0</returns>
        public int FindStr(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal, out int outX, out int outY){
            return OLAPlugDLLHelper.FindStr(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal, out outX, out outY);
        }

        /// <summary>
        /// 查找文字
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson"></param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="outX">输出参数，返回的X坐标</param>
        /// <param name="outY">输出参数，返回的Y坐标</param>
        /// <returns>成功返回 1，失败返回 0</returns>
        public int FindStr(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal, out int outX, out int outY){
            return OLAPlugDLLHelper.FindStr(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal, out outX, out outY);
        }

        /// <summary>
        /// 查找指定文字的坐标
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson"></param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>y (整型数): Y坐标</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrDetail(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 查找指定文字的坐标
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson"></param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>y (整型数): Y坐标</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrDetail(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 查找文字返回全部结果
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrAll(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 查找文字返回全部结果
        /// </summary>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrAll(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 查找图片中的文字
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>查找到的结果（格式为二进制字符串指针）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrFromPtr(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtr(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 查找图片中的文字
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>查找到的结果（格式为二进制字符串指针）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrFromPtr(long source, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtr(OLAObject, source, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 查找文字返回全部结果
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrFromPtrAll(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtrAll(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 查找文字返回全部结果
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="str">查找字符串</param>
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF", "Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrFromPtrAll(long source, string str, string colorJson, string dict, double matchVal){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindStrFromPtrAll(OLAObject, source, str, colorJson, dict, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcrFromPtr(long source, string numbers, List<ColorModel> colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcrFromPtr(OLAObject, source, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="source">图片</param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcrFromPtr(long source, string numbers, string colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcrFromPtr(OLAObject, source, numbers, colorJson, matchVal);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, List<ColorModel> colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcr(OLAObject, x1, y1, x2, y2, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, string colorJson, double matchVal){
            return OLAPlugDLLHelper.FastNumberOcr(OLAObject, x1, y1, x2, y2, numbers, colorJson, matchVal);
        }

        /// <summary>
        /// 对绑定窗口在指定区域进行截图并保存为图片
        /// </summary>
        /// <param name="x1">截图区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">截图区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">截图区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">截图区域右下角Y坐标（相对于窗口客户区）</param>
        /// <param name="file">输出文件路径，支持bmp/gif/jpg/jpeg/png</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 若目录不存在请确保先行创建；覆盖同名文件
        /// </remarks>
        public int Capture(int x1, int y1, int x2, int y2, string file){
            return OLAPlugDLLHelper.Capture(OLAObject, x1, y1, x2, y2, file);
        }

        /// <summary>
        /// 获取绑定窗口指定区域的BMP原始数据
        /// </summary>
        /// <param name="x1">区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">区域右下角Y坐标（相对于窗口客户区）</param>
        /// <param name="data">返回BMP数据指针（输出）</param>
        /// <param name="dataLen">返回数据字节长度（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放；数据包含完整BMP文件头，可直接落盘
        /// </remarks>
        public int GetScreenDataBmp(int x1, int y1, int x2, int y2, out long data, out int dataLen){
            return OLAPlugDLLHelper.GetScreenDataBmp(OLAObject, x1, y1, x2, y2, out data, out dataLen);
        }

        /// <summary>
        /// 获取绑定窗口指定区域的RGB原始数据
        /// </summary>
        /// <param name="x1">区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">区域右下角Y坐标（相对于窗口客户区）</param>
        /// <param name="data">返回像素数据指针（输出，BGR顺序）</param>
        /// <param name="dataLen">返回数据字节长度（输出）</param>
        /// <param name="stride">返回每行对齐后的字节跨度（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放；无文件头，按4字节边界对齐
        /// </remarks>
        public int GetScreenData(int x1, int y1, int x2, int y2, out long data, out int dataLen, out int stride){
            return OLAPlugDLLHelper.GetScreenData(OLAObject, x1, y1, x2, y2, out data, out dataLen, out stride);
        }

        /// <summary>
        /// 获取绑定窗口指定区域的图像数据句柄（内部缓存）
        /// </summary>
        /// <param name="x1">区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">区域右下角Y坐标（相对于窗口客户区）</param>
        /// <returns>返回内部缓存的图像句柄；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回句柄对应的内存由内部维护，不需要也不应该手动释放
        /// </remarks>
        public long GetScreenDataPtr(int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.GetScreenDataPtr(OLAObject, x1, y1, x2, y2);
        }

        /// <summary>
        /// 录制绑定窗口指定区域为GIF动画
        /// </summary>
        /// <param name="x1">区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">区域右下角Y坐标（相对于窗口客户区）</param>
        /// <param name="file">输出GIF文件路径</param>
        /// <param name="delay">帧间隔（毫秒）</param>
        /// <param name="time">录制总时长（毫秒）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 持续截图编码，性能开销较大
        /// </remarks>
        public int CaptureGif(int x1, int y1, int x2, int y2, string file, int delay, int time){
            return OLAPlugDLLHelper.CaptureGif(OLAObject, x1, y1, x2, y2, file, delay, time);
        }

        /// <summary>
        /// 从图像句柄读取像素数据
        /// </summary>
        /// <param name="imgPtr">图像句柄（由加载/生成接口返回）</param>
        /// <param name="data">返回像素数据指针（输出，BGR顺序）</param>
        /// <param name="size">返回数据字节长度（输出）</param>
        /// <param name="stride">返回每行字节跨度（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放
        /// </remarks>
        public int GetImageData(long imgPtr, out long data, out int size, out int stride){
            return OLAPlugDLLHelper.GetImageData(OLAObject, imgPtr, out data, out size, out stride);
        }

        /// <summary>
        /// 使用文件路径在源图中匹配模板图
        /// </summary>
        /// <param name="source">源图路径</param>
        /// <param name="templ">模板图路径</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配结果（结构体/指针，失败返回0）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 实现取决于type/angle/scale的组合策略
        /// </remarks>
        public MatchResult MatchImageFromPath(string source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPath(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 使用文件路径在源图中查找模板图的所有匹配
        /// </summary>
        /// <param name="source">源图路径</param>
        /// <param name="templ">模板图路径</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配点列表字符串指针；未找到返回空字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回字符串需调用FreeStringPtr释放
        /// </remarks>
        public List<MatchResult> MatchImageFromPathAll(string source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPathAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 使用内存源图与文件模板进行匹配
        /// </summary>
        /// <param name="source">源图句柄</param>
        /// <param name="templ">模板图路径</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配结果（结构体/指针，失败返回0）</returns>
        public MatchResult MatchImagePtrFromPath(long source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImagePtrFromPath(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 使用内存源图与文件模板查找所有匹配
        /// </summary>
        /// <param name="source">源图句柄</param>
        /// <param name="templ">模板图路径</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配点列表字符串指针；未找到返回空字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回字符串需调用FreeStringPtr释放
        /// </remarks>
        public List<MatchResult> MatchImagePtrFromPathAll(long source, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImagePtrFromPathAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 获取绑定窗口指定坐标点的颜色值
        /// </summary>
        /// <param name="x">指定点的X坐标（相对于窗口客户区）</param>
        /// <param name="y">指定点的Y坐标（相对于窗口客户区）</param>
        /// <returns>返回颜色值（BGR格式的整数），失败返回0</returns>
        public string GetColor(int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColor(OLAObject, x, y));
        }

        /// <summary>
        /// 获取绑定窗口指定坐标点的颜色值（返回指针）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="x">指定点的X坐标（相对于窗口客户区）</param>
        /// <param name="y">指定点的Y坐标（相对于窗口客户区）</param>
        /// <returns>返回指向颜色值的指针，数据在内部缓存中；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的指针指向内部缓存，不应手动释放；数据为BGR三个字节
        /// </remarks>
        public string GetColorPtr(long source, int x, int y){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColorPtr(OLAObject, source, x, y));
        }

        /// <summary>
        /// 复制一份图像数据
        /// </summary>
        /// <param name="sourcePtr">原始图像句柄</param>
        /// <returns>返回新图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CopyImage(long sourcePtr){
            return OLAPlugDLLHelper.CopyImage(OLAObject, sourcePtr);
        }

        /// <summary>
        /// 释放由MatchImageFromPath等接口产生的图片路径相关资源
        /// </summary>
        /// <param name="path">图片路径字符串指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于释放由MatchImageFromPathAll等返回的字符串资源
        /// </remarks>
        public int FreeImagePath(string path){
            return OLAPlugDLLHelper.FreeImagePath(OLAObject, path);
        }

        /// <summary>
        /// 释放所有已加载的图像资源
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 调用后所有已加载的图像数据指针将失效
        /// </remarks>
        public int FreeImageAll(){
            return OLAPlugDLLHelper.FreeImageAll(OLAObject);
        }

        /// <summary>
        /// 加载图片文件到内存
        /// </summary>
        /// <param name="path">图片文件路径</param>
        /// <returns>返回图像句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 加载后的图像可用于后续的图像匹配等操作
        /// </remarks>
        public long LoadImage(string path){
            return OLAPlugDLLHelper.LoadImage(OLAObject, path);
        }

        /// <summary>
        /// 从BMP数据加载图像
        /// </summary>
        /// <param name="data">BMP格式的数据指针</param>
        /// <param name="dataSize">数据字节长度</param>
        /// <returns>返回图像句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 数据必须包含完整的BMP文件头
        /// </remarks>
        public long LoadImageFromBmpData(long data, int dataSize){
            return OLAPlugDLLHelper.LoadImageFromBmpData(OLAObject, data, dataSize);
        }

        /// <summary>
        /// 从RGB数据加载图像
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="scan0">像素数据首地址（BGR顺序）</param>
        /// <param name="stride">每行字节跨度</param>
        /// <returns>返回图像句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 数据为连续的BGR三通道数据，每行字节对齐到4字节边界
        /// </remarks>
        public long LoadImageFromRGBData(int width, int height, long scan0, int stride){
            return OLAPlugDLLHelper.LoadImageFromRGBData(OLAObject, width, height, scan0, stride);
        }

        /// <summary>
        /// 释放由GetImageData等接口返回的图像数据指针
        /// </summary>
        /// <param name="screenPtr">图像句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeImagePtr(long screenPtr){
            return OLAPlugDLLHelper.FreeImagePtr(OLAObject, screenPtr);
        }

        /// <summary>
        /// 在绑定窗口中查找指定窗口图像（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPtr(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像（使用内存数据）
        /// </summary>
        /// <param name="source">源图句柄</param>
        /// <param name="templ"></param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchImageFromPtr(long source, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPtr(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="source">源图句柄</param>
        /// <param name="templ"></param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchImageFromPtrAll(long source, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchImageFromPtrAll(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定窗口图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsFromPtrAll(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPtrAll(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定窗口图像（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPath(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定窗口图像的所有匹配位置（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsFromPathAll(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsFromPathAll(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtr(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtr(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtrAll(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPtrAll(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPath(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPath(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像的所有匹配位置（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPathAll(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中使用阈值匹配查找指定窗口图像的所有匹配位置（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchWindowsThresholdFromPathAll(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 显示/隐藏匹配结果可视化或调试窗口
        /// </summary>
        /// <param name="flag">显示标志（0 关闭，1 打开）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int ShowMatchWindow(int flag){
            return OLAPlugDLLHelper.ShowMatchWindow(OLAObject, flag);
        }

        /// <summary>
        /// 计算两幅图像的结构相似性指数（SSIM）
        /// </summary>
        /// <param name="image1">第一幅图像句柄</param>
        /// <param name="image2">第二幅图像句柄</param>
        /// <returns>SSIM值（0~1），越接近1越相似</returns>
        public double CalculateSSIM(long image1, long image2){
            return OLAPlugDLLHelper.CalculateSSIM(OLAObject, image1, image2);
        }

        /// <summary>
        /// 计算两幅图像直方图的相似度
        /// </summary>
        /// <param name="image1">图像1句柄</param>
        /// <param name="image2">图像2句柄</param>
        /// <returns>直方图相似度（0~1）</returns>
        public double CalculateHistograms(long image1, long image2){
            return OLAPlugDLLHelper.CalculateHistograms(OLAObject, image1, image2);
        }

        /// <summary>
        /// 计算两幅图像的均方误差（MSE）
        /// </summary>
        /// <param name="image1">第一幅图像句柄</param>
        /// <param name="image2">第二幅图像句柄</param>
        /// <returns>MSE值，越小越相似</returns>
        public double CalculateMSE(long image1, long image2){
            return OLAPlugDLLHelper.CalculateMSE(OLAObject, image1, image2);
        }

        /// <summary>
        /// 将内存图像保存为文件
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="path">输出文件路径，支持bmp/gif/jpg/jpeg/png</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int SaveImageFromPtr(long ptr, string path){
            return OLAPlugDLLHelper.SaveImageFromPtr(OLAObject, ptr, path);
        }

        /// <summary>
        /// 调整图像大小
        /// </summary>
        /// <param name="ptr">原始图像句柄</param>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        /// <returns>新图像句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 使用双线性插值进行缩放
        /// </remarks>
        public long ReSize(long ptr, int width, int height){
            return OLAPlugDLLHelper.ReSize(OLAObject, ptr, width, height);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="color1">要查找的颜色值（BGR格式）</param>
        /// <param name="color2">要查找的颜色值（BGR格式）</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColor(int x1, int y1, int x2, int y2, string color1, string color2, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColor(OLAObject, x1, y1, x2, y2, color1, color2, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns>查找结果返回所有匹配点坐标的字符串，格式为"["x":10,"y":20],"[x":30,"y":40]"；未找到返回空字符串指针，需调用FreeStringPtr释放内存</returns>
        public List<Point> FindColorList(int x1, int y1, int x2, int y2, string color1, string color2){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorList(OLAObject, x1, y1, x2, y2, color1, color2));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorEx(int x1, int y1, int x2, int y2, string colorJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorEx(OLAObject, x1, y1, x2, y2, colorJson, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <returns>查找结果返回所有匹配点坐标的字符串，格式为"["x":10,"y":20],"[x":30,"y":40]"；未找到返回空字符串指针，需调用FreeStringPtr释放内存</returns>
        public List<Point> FindColorListEx(int x1, int y1, int x2, int y2, string colorJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorListEx(OLAObject, x1, y1, x2, y2, colorJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色列表（格式："color1,color2"）</param>
        /// <param name="pointJson"></param>
        /// <param name="dir">查找方向（取值同FindColor）
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的基准点X坐标</param>
        /// <param name="y">返回找到的基准点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColor(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色列表（格式："color1,color2"）</param>
        /// <param name="pointJson"></param>
        /// <param name="dir">查找方向（取值同FindColor）
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的基准点X坐标</param>
        /// <param name="y">返回找到的基准点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColor(int x1, int y1, int x2, int y2, string colorJson, string pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColor(OLAObject, x1, y1, x2, y2, colorJson, pointJson, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <returns></returns>
        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorList(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson)));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <returns></returns>
        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, string colorJson, string pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorList(OLAObject, x1, y1, x2, y2, colorJson, pointJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找多色点
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的基准点X坐标</param>
        /// <param name="y">返回找到的基准点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColorFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColorFromPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找多色点
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右，从上到下
        ///<br/> 1: 从左到右，从下到上
        ///<br/> 2: 从右到左，从上到下
        ///<br/> 3: 从右到左，从下到上
        /// </param>
        /// <param name="x">返回找到的基准点X坐标</param>
        /// <param name="y">返回找到的基准点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColorFromPtr(long ptr, string colorJson, string pointJson, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindMultiColorFromPtr(OLAObject, ptr, colorJson, pointJson, dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找多色点列表
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <returns></returns>
        public List<Point> FindMultiColorListFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorListFromPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson)));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找多色点列表
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson"></param>
        /// <param name="pointJson"></param>
        /// <returns></returns>
        public List<Point> FindMultiColorListFromPtr(long ptr, string colorJson, string pointJson){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindMultiColorListFromPtr(OLAObject, ptr, colorJson, pointJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 获取图像的宽度和高度
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="width">返回图像宽度</param>
        /// <param name="height">返回图像高度</param>
        /// <returns>获取结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int GetImageSize(long ptr, out int width, out int height){
            return OLAPlugDLLHelper.GetImageSize(OLAObject, ptr, out width, out height);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlock(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlock(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlock(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlock(OLAObject, x1, y1, x2, y2, colorList, count, width, height, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockPtr(long ptr, List<ColorModel> colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockPtr(long ptr, string colorList, int count, int width, int height, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtr(OLAObject, ptr, colorList, count, width, height, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockList(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockList(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockList(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockList(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListPtr(long ptr, List<ColorModel> colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListPtr(long ptr, string colorList, int count, int width, int height, int type){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtr(OLAObject, ptr, colorList, count, width, height, type));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockEx(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockEx(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockEx(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockEx(OLAObject, x1, y1, x2, y2, colorList, count, width, height, dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockPtrEx(long ptr, List<ColorModel> colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtrEx(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的连续区域（色块）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回色块中心点X坐标</param>
        /// <param name="y">返回色块中心点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorBlockPtrEx(long ptr, string colorList, int count, int width, int height, int dir, out int x, out int y){
            return OLAPlugDLLHelper.FindColorBlockPtrEx(OLAObject, ptr, colorList, count, width, height, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListEx(int x1, int y1, int x2, int y2, List<ColorModel> colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListEx(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListEx(int x1, int y1, int x2, int y2, string colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListEx(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListPtrEx(long ptr, List<ColorModel> colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtrEx(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在内存图像中查找指定颜色的所有连续区域（色块列表）
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要查找的颜色值（JSON格式）</param>
        /// <param name="count">要查找的色块数量</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="type">查找类型
        ///<br/> 0: 不重复
        ///<br/> 1: 重复
        /// </param>
        /// <param name="dir">查找方向
        ///<br/> 0:: 从左到右,从上到下
        ///<br/> 1:: 从左到右,从下到上
        ///<br/> 2:: 从右到左,从上到下
        ///<br/> 3:: 从右到左,从下到上
        ///<br/> 4:: 从中心往外查找
        ///<br/> 5:: 从上到下,从左到右
        ///<br/> 6:: 从上到下,从右到左
        ///<br/> 7:: 从下到上,从左到右
        ///<br/> 8:: 从下到上,从右到左
        /// </param>
        /// <returns></returns>
        public List<Point> FindColorBlockListPtrEx(long ptr, string colorList, int count, int width, int height, int type, int dir){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FindColorBlockListPtrEx(OLAObject, ptr, colorList, count, width, height, type, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 统计绑定窗口指定区域内指定颜色的像素数量
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNum(int x1, int y1, int x2, int y2, List<ColorModel> colorList){
            return OLAPlugDLLHelper.GetColorNum(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList));
        }

        /// <summary>
        /// 统计绑定窗口指定区域内指定颜色的像素数量
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNum(int x1, int y1, int x2, int y2, string colorList){
            return OLAPlugDLLHelper.GetColorNum(OLAObject, x1, y1, x2, y2, colorList);
        }

        /// <summary>
        /// 统计内存图像中指定颜色的像素数量
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNumPtr(long ptr, List<ColorModel> colorList){
            return OLAPlugDLLHelper.GetColorNumPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorList));
        }

        /// <summary>
        /// 统计内存图像中指定颜色的像素数量
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNumPtr(long ptr, string colorList){
            return OLAPlugDLLHelper.GetColorNumPtr(OLAObject, ptr, colorList);
        }

        /// <summary>
        /// 对图像进行裁剪
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="x1">裁剪区域左上角X坐标</param>
        /// <param name="y1">裁剪区域左上角Y坐标</param>
        /// <param name="x2">裁剪区域右下角X坐标</param>
        /// <param name="y2">裁剪区域右下角Y坐标</param>
        /// <returns>裁剪后图像句柄，失败返回0</returns>
        public long Cropped(long image, int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.Cropped(OLAObject, image, x1, y1, x2, y2);
        }

        /// <summary>
        /// 根据多色点生成阈值图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColorPtr(long ptr, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColorPtr(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 根据多色点生成阈值图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColorPtr(long ptr, string colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColorPtr(OLAObject, ptr, colorJson);
        }

        /// <summary>
        /// 根据多色点生成阈值图像（从屏幕区域）
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="colorJson"></param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColor(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 根据多色点生成阈值图像（从屏幕区域）
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="colorJson"></param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, string colorJson){
            return OLAPlugDLLHelper.GetThresholdImageFromMultiColor(OLAObject, x1, y1, x2, y2, colorJson);
        }

        /// <summary>
        /// 判断两幅图像是否完全相同
        /// </summary>
        /// <param name="ptr">第一幅图像句柄</param>
        /// <param name="ptr2">第二幅图像句柄</param>
        /// <returns>比较结果
        ///<br/>0: 不相同
        ///<br/>1: 相同
        /// </returns>
        public int IsSameImage(long ptr, long ptr2){
            return OLAPlugDLLHelper.IsSameImage(OLAObject, ptr, ptr2);
        }

        /// <summary>
        /// 显示图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在独立窗口中显示图像，用于调试和查看
        /// </remarks>
        public int ShowImage(long ptr){
            return OLAPlugDLLHelper.ShowImage(OLAObject, ptr);
        }

        /// <summary>
        /// 显示图片
        /// </summary>
        /// <param name="file">图片文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ShowImageFromFile(string file){
            return OLAPlugDLLHelper.ShowImageFromFile(OLAObject, file);
        }

        /// <summary>
        /// 将图像中指定颜色范围内的像素替换为新颜色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <param name="color">目标颜色（BGR十六进制字符串）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long SetColorsToNewColor(long ptr, List<ColorModel> colorJson, string color){
            return OLAPlugDLLHelper.SetColorsToNewColor(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), color);
        }

        /// <summary>
        /// 将图像中指定颜色范围内的像素替换为新颜色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <param name="color">目标颜色（BGR十六进制字符串）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long SetColorsToNewColor(long ptr, string colorJson, string color){
            return OLAPlugDLLHelper.SetColorsToNewColor(OLAObject, ptr, colorJson, color);
        }

        /// <summary>
        /// 保留图像中指定颜色，其余颜色变为黑色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">要保留的颜色范围（JSON）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long RemoveOtherColors(long ptr, List<ColorModel> colorJson){
            return OLAPlugDLLHelper.RemoveOtherColors(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 保留图像中指定颜色，其余颜色变为黑色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">要保留的颜色范围（JSON）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long RemoveOtherColors(long ptr, string colorJson){
            return OLAPlugDLLHelper.RemoveOtherColors(OLAObject, ptr, colorJson);
        }

        /// <summary>
        /// 在图像上绘制矩形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x1">矩形左上角X坐标</param>
        /// <param name="y1">矩形左上角Y坐标</param>
        /// <param name="x2">矩形右下角X坐标</param>
        /// <param name="y2">矩形右下角Y坐标</param>
        /// <param name="thickness">线条粗细，负值表示填充</param>
        /// <param name="color">绘制颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawRectangle(long ptr, int x1, int y1, int x2, int y2, int thickness, string color){
            return OLAPlugDLLHelper.DrawRectangle(OLAObject, ptr, x1, y1, x2, y2, thickness, color);
        }

        /// <summary>
        /// 在图像上绘制圆形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">圆心X坐标</param>
        /// <param name="y">圆心Y坐标</param>
        /// <param name="radius"></param>
        /// <param name="thickness">线条粗细，负值表示填充</param>
        /// <param name="color">绘制颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawCircle(long ptr, int x, int y, int radius, int thickness, string color){
            return OLAPlugDLLHelper.DrawCircle(OLAObject, ptr, x, y, radius, thickness, color);
        }

        /// <summary>
        /// 在图像上绘制填充多边形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pointJson">多边形顶点坐标（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">填充颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawFillPoly(long ptr, List<Point> pointJson, string color){
            return OLAPlugDLLHelper.DrawFillPoly(OLAObject, ptr, JsonConvert.SerializeObject(pointJson), color);
        }

        /// <summary>
        /// 在图像上绘制填充多边形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pointJson">多边形顶点坐标（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">填充颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawFillPoly(long ptr, string pointJson, string color){
            return OLAPlugDLLHelper.DrawFillPoly(OLAObject, ptr, pointJson, color);
        }

        /// <summary>
        /// 从图像中解码二维码
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回解码的二维码内容字符串，需调用FreeStringPtr释放内存；失败返回0</returns>
        public string DecodeQRCode(long ptr){
            return PtrToStringUTF8(OLAPlugDLLHelper.DecodeQRCode(OLAObject, ptr));
        }

        /// <summary>
        /// 生成二维码图像
        /// </summary>
        /// <param name="str">要编码的文本内容</param>
        /// <param name="pixelsPerModule">模块像素大小</param>
        /// <returns>返回二维码图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CreateQRCode(string str, int pixelsPerModule){
            return OLAPlugDLLHelper.CreateQRCode(OLAObject, str, pixelsPerModule);
        }

        /// <summary>
        /// 高级生成二维码图像
        /// </summary>
        /// <param name="str">要编码的文本内容</param>
        /// <param name="pixelsPerModule">模块像素大小</param>
        /// <param name="version">版本（1-40，0表示自动）</param>
        /// <param name="correction_level">纠错等级（0 L，1 M，2 Q，3 H）</param>
        /// <param name="mode">编码模式</param>
        /// <param name="structure_number">结构编号</param>
        /// <returns>返回二维码图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CreateQRCodeEx(string str, int pixelsPerModule, int version, int correction_level, int mode, int structure_number){
            return OLAPlugDLLHelper.CreateQRCodeEx(OLAObject, str, pixelsPerModule, version, correction_level, mode, structure_number);
        }

        /// <summary>
        /// 在动画图像序列中查找匹配帧（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">动画模板/序列句柄</param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <param name="delay"></param>
        /// <param name="time"></param>
        /// <param name="threadCount"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchAnimationFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchAnimationFromPtr(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在动画图像序列中查找匹配帧（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ"></param>
        /// <param name="matchVal"></param>
        /// <param name="type"></param>
        /// <param name="angle"></param>
        /// <param name="scale"></param>
        /// <param name="delay"></param>
        /// <param name="time"></param>
        /// <param name="threadCount"></param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchAnimationFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.MatchAnimationFromPath(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 移除两幅图像之间的差异部分
        /// </summary>
        /// <param name="image1">第一幅图像句柄</param>
        /// <param name="image2">第二幅图像句柄</param>
        /// <returns>返回差异移除后的图像句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 将两幅图像的相同部分保留，不同部分变为黑色
        /// </remarks>
        public long RemoveImageDiff(long image1, long image2){
            return OLAPlugDLLHelper.RemoveImageDiff(OLAObject, image1, image2);
        }

        /// <summary>
        /// 获取图像的BMP格式数据
        /// </summary>
        /// <param name="imgPtr">图像句柄</param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns>返回包含BMP文件头的完整BMP数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public int GetImageBmpData(long imgPtr, out long data, out int size){
            return OLAPlugDLLHelper.GetImageBmpData(OLAObject, imgPtr, out data, out size);
        }

        /// <summary>
        /// 获取图像的PNG格式数据
        /// </summary>
        /// <param name="imgPtr">图像句柄</param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns>返回包含PNG文件头的完整PNG数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public int GetImagePngData(long imgPtr, out long data, out int size){
            return OLAPlugDLLHelper.GetImagePngData(OLAObject, imgPtr, out data, out size);
        }

        /// <summary>
        /// 释放由GetImageData等接口返回的图像数据指针
        /// </summary>
        /// <param name="screenPtr"></param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeImageData(long screenPtr){
            return OLAPlugDLLHelper.FreeImageData(OLAObject, screenPtr);
        }

        /// <summary>
        /// 对图像像素进行缩放处理
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pixelsPerModule">像素缩放系数</param>
        /// <returns>处理后图像句柄，失败返回0</returns>
        public long ScalePixels(long ptr, int pixelsPerModule){
            return OLAPlugDLLHelper.ScalePixels(OLAObject, ptr, pixelsPerModule);
        }

        /// <summary>
        /// 创建图片
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="color">初始填充颜色（BGR格式）</param>
        /// <returns>返回新图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CreateImage(int width, int height, string color){
            return OLAPlugDLLHelper.CreateImage(OLAObject, width, height, color);
        }

        /// <summary>
        /// 设置指定像素颜色
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="x">指定点X坐标</param>
        /// <param name="y">指定点Y坐标</param>
        /// <param name="color">要设置的颜色值（BGR格式）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetPixel(long image, int x, int y, string color){
            return OLAPlugDLLHelper.SetPixel(OLAObject, image, x, y, color);
        }

        /// <summary>
        /// 批量设置图像中多个像素点的颜色
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="points">坐标点数组（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">颜色（BGR十六进制字符串）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int SetPixelList(long image, List<Point> points, string color){
            return OLAPlugDLLHelper.SetPixelList(OLAObject, image, JsonConvert.SerializeObject(points), color);
        }

        /// <summary>
        /// 批量设置图像中多个像素点的颜色
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="points">坐标点数组（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">颜色（BGR十六进制字符串）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int SetPixelList(long image, string points, string color){
            return OLAPlugDLLHelper.SetPixelList(OLAObject, image, points, color);
        }

        /// <summary>
        /// 拼接两张图像
        /// </summary>
        /// <param name="image1">图像1句柄</param>
        /// <param name="image2">图像2句柄</param>
        /// <param name="gap">图像间距（像素）</param>
        /// <param name="color">间距填充颜色（BGR十六进制字符串）</param>
        /// <param name="dir">拼接方向（0 水平，1 垂直）</param>
        /// <returns>新图像句柄，失败返回0</returns>
        public long ConcatImage(long image1, long image2, int gap, string color, int dir){
            return OLAPlugDLLHelper.ConcatImage(OLAObject, image1, image2, gap, color, dir);
        }

        /// <summary>
        /// 单张图像覆盖的增强版（支持 Alpha 羽化、并行计算）
        /// </summary>
        /// <param name="image1">前景图句柄（支持四通道Alpha）</param>
        /// <param name="image2">背景图句柄</param>
        /// <param name="x">覆盖位置x坐标</param>
        /// <param name="y">覆盖位置y坐标</param>
        /// <param name="alpha">全局透明度系数 (0~1)</param>
        /// <returns>混合后的图像</returns>
        public long CoverImage(long image1, long image2, int x, int y, double alpha){
            return OLAPlugDLLHelper.CoverImage(OLAObject, image1, image2, x, y, alpha);
        }

        /// <summary>
        /// 按角度旋转图像
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <returns>新图像句柄，失败返回0</returns>
        public long RotateImage(long image, double angle){
            return OLAPlugDLLHelper.RotateImage(OLAObject, image, angle);
        }

        /// <summary>
        /// 将图像编码为Base64字符串
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <returns>Base64字符串指针，需调用FreeStringPtr释放</returns>
        public string ImageToBase64(long image){
            return PtrToStringUTF8(OLAPlugDLLHelper.ImageToBase64(OLAObject, image));
        }

        /// <summary>
        /// 将Base64字符串解码为图像
        /// </summary>
        /// <param name="base64">Base64字符串</param>
        /// <returns>图像句柄，失败返回0</returns>
        public long Base64ToImage(string base64){
            return OLAPlugDLLHelper.Base64ToImage(OLAObject, base64);
        }

        /// <summary>
        /// 十六进制颜色解析为ARGB
        /// </summary>
        /// <param name="hex">十六进制颜色（如#AARRGGBB或#RRGGBB）</param>
        /// <param name="a">返回Alpha（输出）</param>
        /// <param name="r">返回Red（输出）</param>
        /// <param name="g">返回Green（输出）</param>
        /// <param name="b">返回Blue（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int Hex2ARGB(string hex, out int a, out int r, out int g, out int b){
            return OLAPlugDLLHelper.Hex2ARGB(OLAObject, hex, out a, out r, out g, out b);
        }

        /// <summary>
        /// 十六进制颜色解析为RGB
        /// </summary>
        /// <param name="hex">十六进制颜色（如#RRGGBB）</param>
        /// <param name="r">返回Red（输出）</param>
        /// <param name="g">返回Green（输出）</param>
        /// <param name="b">返回Blue（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int Hex2RGB(string hex, out int r, out int g, out int b){
            return OLAPlugDLLHelper.Hex2RGB(OLAObject, hex, out r, out g, out b);
        }

        /// <summary>
        /// 将ARGB转换为十六进制颜色字符串
        /// </summary>
        /// <param name="a">Alpha分量</param>
        /// <param name="r">Red分量</param>
        /// <param name="g">Green分量</param>
        /// <param name="b">Blue分量</param>
        /// <returns>十六进制颜色字符串指针，需调用FreeStringPtr释放</returns>
        public string ARGB2Hex(int a, int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.ARGB2Hex(OLAObject, a, r, g, b));
        }

        /// <summary>
        /// 将RGB颜色转换为十六进制字符串
        /// </summary>
        /// <param name="r">红色值</param>
        /// <param name="g">绿色值</param>
        /// <param name="b">蓝色值</param>
        /// <returns>十六进制字符串</returns>
        public string RGB2Hex(int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.RGB2Hex(OLAObject, r, g, b));
        }

        /// <summary>
        /// 将十六进制颜色转换为HSV颜色
        /// </summary>
        /// <param name="hex">十六进制颜色</param>
        /// <returns>HSV颜色</returns>
        public string Hex2HSV(string hex){
            return PtrToStringUTF8(OLAPlugDLLHelper.Hex2HSV(OLAObject, hex));
        }

        /// <summary>
        /// 将RGB颜色转换为HSV颜色
        /// </summary>
        /// <param name="r">红色值</param>
        /// <param name="g">绿色值</param>
        /// <param name="b">蓝色值</param>
        /// <returns>HSV颜色</returns>
        public string RGB2HSV(int r, int g, int b){
            return PtrToStringUTF8(OLAPlugDLLHelper.RGB2HSV(OLAObject, r, g, b));
        }

        /// <summary>
        /// 判断屏幕坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="x1">X坐标</param>
        /// <param name="y1">Y坐标</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColor(int x1, int y1, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColor(OLAObject, x1, y1, colorStart, colorEnd);
        }

        /// <summary>
        /// 判断图像坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColorPtr(long ptr, int x, int y, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColorPtr(OLAObject, ptr, x, y, colorStart, colorEnd);
        }

        /// <summary>
        /// 判断屏幕坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="x1">X坐标</param>
        /// <param name="y1">Y坐标</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColorEx(int x1, int y1, string colorJson){
            return OLAPlugDLLHelper.CmpColorEx(OLAObject, x1, y1, colorJson);
        }

        /// <summary>
        /// 判断图像坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColorPtrEx(long ptr, int x, int y, string colorJson){
            return OLAPlugDLLHelper.CmpColorPtrEx(OLAObject, ptr, x, y, colorJson);
        }

        /// <summary>
        /// 判断十六进制颜色是否在指定范围
        /// </summary>
        /// <param name="hex">颜色（十六进制）</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColorHexEx(string hex, string colorJson){
            return OLAPlugDLLHelper.CmpColorHexEx(OLAObject, hex, colorJson);
        }

        /// <summary>
        /// 判断十六进制颜色是否在指定范围
        /// </summary>
        /// <param name="hex">颜色（十六进制）</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>结果，0 否，1 是</returns>
        public int CmpColorHex(string hex, string colorStart, string colorEnd){
            return OLAPlugDLLHelper.CmpColorHex(OLAObject, hex, colorStart, colorEnd);
        }

        /// <summary>
        /// 基于种子点获取连通域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="points">种子点数组（JSON）</param>
        /// <param name="tolerance">容差阈值</param>
        /// <returns>连通域点数组字符串指针（JSON），需调用FreeStringPtr释放</returns>
        public long GetConnectedComponents(long ptr, List<Point> points, int tolerance){
            return OLAPlugDLLHelper.GetConnectedComponents(OLAObject, ptr, JsonConvert.SerializeObject(points), tolerance);
        }

        /// <summary>
        /// 基于种子点获取连通域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="points">种子点数组（JSON）</param>
        /// <param name="tolerance">容差阈值</param>
        /// <returns>连通域点数组字符串指针（JSON），需调用FreeStringPtr释放</returns>
        public long GetConnectedComponents(long ptr, string points, int tolerance){
            return OLAPlugDLLHelper.GetConnectedComponents(OLAObject, ptr, points, tolerance);
        }

        /// <summary>
        /// 基于几何与边缘特征检测指针（针状/箭头）方向
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">参考点X坐标</param>
        /// <param name="y">参考点Y坐标</param>
        /// <returns>方向角（度）</returns>
        public double DetectPointerDirection(long ptr, int x, int y){
            return OLAPlugDLLHelper.DetectPointerDirection(OLAObject, ptr, x, y);
        }

        /// <summary>
        /// 基于特征与模板的指针方向检测
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="templatePtr">模板图句柄（可选）</param>
        /// <param name="x">参考点X坐标</param>
        /// <param name="y">参考点Y坐标</param>
        /// <param name="useTemplate">是否启用模板匹配</param>
        /// <returns>方向角（度）</returns>
        public double DetectPointerDirectionByFeatures(long ptr, long templatePtr, int x, int y, bool useTemplate){
            return OLAPlugDLLHelper.DetectPointerDirectionByFeatures(OLAObject, ptr, templatePtr, x, y, useTemplate);
        }

        /// <summary>
        /// 快速模板匹配
        /// </summary>
        /// <param name="ptr">源图句柄</param>
        /// <param name="templatePtr">模板图句柄</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配结果（结构体/指针，失败返回0）</returns>
        public MatchResult FastMatch(long ptr, long templatePtr, double matchVal, int type, double angle, double scale){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.FastMatch(OLAObject, ptr, templatePtr, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 快速ROI,返回不为0的最大区域图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回ROI区域子图像句柄，失败返回0</returns>
        public long FastROI(long ptr){
            return OLAPlugDLLHelper.FastROI(OLAObject, ptr);
        }

        /// <summary>
        /// 获取ROI区域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x1">返回区域左上角X坐标（输出）</param>
        /// <param name="y1">返回区域左上角Y坐标（输出）</param>
        /// <param name="x2">返回区域右下角X坐标（输出）</param>
        /// <param name="y2">返回区域右下角Y坐标（输出）</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int GetROIRegion(long ptr, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetROIRegion(OLAObject, ptr, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 获取前景点
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>前景点数组字符串指针（JSON，如[{"x":10,"y":10}]]），需调用FreeStringPtr释放</returns>
        public List<Point> GetForegroundPoints(long ptr){
            var result = PtrToStringUTF8(OLAPlugDLLHelper.GetForegroundPoints(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 转换颜色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="type">0转为灰度 ,1.BGRA-RGBA,2.BGRA-BGR,3.BGRA-HSVA,4.BGRA-HSV</param>
        /// <returns>返回转换后的图像句柄，失败返回0</returns>
        public long ConvertColor(long ptr, int type){
            return OLAPlugDLLHelper.ConvertColor(OLAObject, ptr, type);
        }

        /// <summary>
        /// 阈值化
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="thresh">阈值</param>
        /// <param name="maxVal">最大值</param>
        /// <param name="type">0.二值化,1.反二值化,2.截断,3.阈值化,4.反阈值化,5.阈值化OTSU,6.反阈值化OTSU</param>
        /// <returns>返回阈值化后的图像句柄，失败返回0</returns>
        public long Threshold(long ptr, double thresh, double maxVal, int type){
            return OLAPlugDLLHelper.Threshold(OLAObject, ptr, thresh, maxVal, type);
        }

        /// <summary>
        /// 去除孤岛
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="minArea">最小面积</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long RemoveIslands(long ptr, int minArea){
            return OLAPlugDLLHelper.RemoveIslands(OLAObject, ptr, minArea);
        }

        /// <summary>
        /// 形态学梯度
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphGradient(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphGradient(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学顶帽
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphTophat(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphTophat(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学黑帽
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphBlackhat(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphBlackhat(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 膨胀
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Dilation(long ptr, int kernelSize){
            return OLAPlugDLLHelper.Dilation(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 腐蚀
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Erosion(long ptr, int kernelSize){
            return OLAPlugDLLHelper.Erosion(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 高斯模糊
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long GaussianBlur(long ptr, int kernelSize){
            return OLAPlugDLLHelper.GaussianBlur(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 图像锐化
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Sharpen(long ptr){
            return OLAPlugDLLHelper.Sharpen(OLAObject, ptr);
        }

        /// <summary>
        /// Canny边缘检测
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回边缘图像句柄，失败返回0</returns>
        public long CannyEdge(long ptr, int kernelSize){
            return OLAPlugDLLHelper.CannyEdge(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 翻转图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="flipCode"></param>
        /// <returns>返回翻转后的图像句柄，失败返回0</returns>
        public long Flip(long ptr, int flipCode){
            return OLAPlugDLLHelper.Flip(OLAObject, ptr, flipCode);
        }

        /// <summary>
        /// 形态学开运算
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphOpen(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphOpen(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学闭运算
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="kernelSize"></param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphClose(long ptr, int kernelSize){
            return OLAPlugDLLHelper.MorphClose(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 骨架化
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回骨架化后的图像句柄，失败返回0</returns>
        public long Skeletonize(long ptr){
            return OLAPlugDLLHelper.Skeletonize(OLAObject, ptr);
        }

        /// <summary>
        /// 从路径拼接图片
        /// </summary>
        /// <param name="path">图片目录或通配路径</param>
        /// <param name="trajectory">返回轨迹数据指针（输出，可为0）</param>
        /// <returns>返回拼接后的图像句柄，失败返回0</returns>
        public long ImageStitchFromPath(string path, out long trajectory){
            return OLAPlugDLLHelper.ImageStitchFromPath(OLAObject, path, out trajectory);
        }

        /// <summary>
        /// 创建拼接图片实例
        /// </summary>
        /// <returns>返回拼接实例句柄，失败返回0</returns>
        public long ImageStitchCreate(){
            return OLAPlugDLLHelper.ImageStitchCreate(OLAObject);
        }

        /// <summary>
        /// 拼接图片
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <param name="image">图像句柄</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int ImageStitchAppend(long imageStitch, long image){
            return OLAPlugDLLHelper.ImageStitchAppend(OLAObject, imageStitch, image);
        }

        /// <summary>
        /// 获取拼接图片结果
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <param name="trajectory"></param>
        /// <returns>返回拼接后的图像句柄，失败返回0</returns>
        public long ImageStitchGetResult(long imageStitch, out long trajectory){
            return OLAPlugDLLHelper.ImageStitchGetResult(OLAObject, imageStitch, out trajectory);
        }

        /// <summary>
        /// 释放拼接图片实例
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <returns>操作结果，0 失败，1 成功</returns>
        public int ImageStitchFree(long imageStitch){
            return OLAPlugDLLHelper.ImageStitchFree(OLAObject, imageStitch);
        }

        /// <summary>
        /// 打开已有注册表键
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径，例如 "Software\\Microsoft\\Windows"</param>
        /// <returns>注册表键句柄，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 仅在键已存在时返回有效句柄
        /// <br/>2. 使用完成后必须调用 RegistryCloseKey 释放句柄
        /// </remarks>
        public long RegistryOpenKey(int rootKey, string subKey){
            return OLAPlugDLLHelper.RegistryOpenKey(OLAObject, rootKey, subKey);
        }

        /// <summary>
        /// 创建（如不存在则创建）并打开注册表键
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径，例如 "Software\\OLAPlug"</param>
        /// <returns>注册表键句柄，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 如果键已存在，则直接打开已有键
        /// <br/>2. 使用完成后必须调用 RegistryCloseKey 释放句柄
        /// </remarks>
        public long RegistryCreateKey(int rootKey, string subKey){
            return OLAPlugDLLHelper.RegistryCreateKey(OLAObject, rootKey, subKey);
        }

        /// <summary>
        /// 关闭注册表键句柄
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 关闭后句柄失效，不可再使用
        /// </remarks>
        public int RegistryCloseKey(long key){
            return OLAPlugDLLHelper.RegistryCloseKey(OLAObject, key);
        }

        /// <summary>
        /// 判断指定注册表键是否存在
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <returns>1 表示存在，0 表示不存在</returns>
        public int RegistryKeyExists(int rootKey, string subKey){
            return OLAPlugDLLHelper.RegistryKeyExists(OLAObject, rootKey, subKey);
        }

        /// <summary>
        /// 删除指定注册表键
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <param name="recursive">是否递归删除子键，1 表示递归删除，0 表示仅删除当前键</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 建议谨慎使用递归删除，避免误删系统关键配置
        /// </remarks>
        public int RegistryDeleteKey(int rootKey, string subKey, int recursive){
            return OLAPlugDLLHelper.RegistryDeleteKey(OLAObject, rootKey, subKey, recursive);
        }

        /// <summary>
        /// 设置字符串类型的注册表值（REG_SZ）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称，空字符串表示默认值</param>
        /// <param name="value">字符串值内容</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        public int RegistrySetString(long key, string valueName, string value){
            return OLAPlugDLLHelper.RegistrySetString(OLAObject, key, valueName, value);
        }

        /// <summary>
        /// 读取字符串类型的注册表值（REG_SZ/REG_EXPAND_SZ）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称，空字符串表示默认值</param>
        /// <returns>字符串内容的句柄，失败或不存在时返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetString(long key, string valueName){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetString(OLAObject, key, valueName));
        }

        /// <summary>
        /// 设置 32 位整型注册表值（REG_DWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <param name="value">要写入的 32 位整型值</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        public int RegistrySetDword(long key, string valueName, int value){
            return OLAPlugDLLHelper.RegistrySetDword(OLAObject, key, valueName, value);
        }

        /// <summary>
        /// 读取 32 位整型注册表值（REG_DWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>读取到的数值；如果值不存在或类型不匹配，则返回 0</returns>
        public int RegistryGetDword(long key, string valueName){
            return OLAPlugDLLHelper.RegistryGetDword(OLAObject, key, valueName);
        }

        /// <summary>
        /// 设置 64 位整型注册表值（REG_QWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <param name="value">要写入的 64 位整型值</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        public int RegistrySetQword(long key, string valueName, long value){
            return OLAPlugDLLHelper.RegistrySetQword(OLAObject, key, valueName, value);
        }

        /// <summary>
        /// 读取 64 位整型注册表值（REG_QWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>读取到的数值；如果值不存在或类型不匹配，则返回 0</returns>
        public long RegistryGetQword(long key, string valueName){
            return OLAPlugDLLHelper.RegistryGetQword(OLAObject, key, valueName);
        }

        /// <summary>
        /// 删除指定名称的注册表值
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>操作结果，1 表示成功或值不存在，0 表示失败</returns>
        public int RegistryDeleteValue(long key, string valueName){
            return OLAPlugDLLHelper.RegistryDeleteValue(OLAObject, key, valueName);
        }

        /// <summary>
        /// 枚举当前键下的所有子键名称
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <returns>包含所有子键名称的 JSON 数组字符串句柄，例如 ["SubKey1","SubKey2"]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryEnumSubKeys(long key){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryEnumSubKeys(OLAObject, key));
        }

        /// <summary>
        /// 枚举当前键下的所有值名称
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <returns>包含所有值名称的 JSON 数组字符串句柄，例如 ["Value1","Value2"]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryEnumValues(long key){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryEnumValues(OLAObject, key));
        }

        /// <summary>
        /// 设置环境变量，内部基于注册表与系统 API 实现
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <param name="value">环境变量值</param>
        /// <param name="systemWide">是否为系统级环境变量，1 表示系统级，0 表示当前用户</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        public int RegistrySetEnvironmentVariable(string name, string value, int systemWide){
            return OLAPlugDLLHelper.RegistrySetEnvironmentVariable(OLAObject, name, value, systemWide);
        }

        /// <summary>
        /// 获取环境变量的值
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <param name="systemWide">是否从系统级环境变量读取，1 表示系统级，0 表示当前用户</param>
        /// <returns>环境变量值的字符串句柄，如果不存在则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetEnvironmentVariable(string name, int systemWide){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetEnvironmentVariable(OLAObject, name, systemWide));
        }

        /// <summary>
        /// 获取用户配置相关的注册表路径
        /// </summary>
        /// <returns>注册表路径字符串句柄，例如 "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserShell Folders"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetUserRegistryPath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetUserRegistryPath(OLAObject));
        }

        /// <summary>
        /// 获取系统配置相关的注册表路径
        /// </summary>
        /// <returns>注册表路径字符串句柄，例如 "Software\\Microsoft\\Windows\\CurrentVersion"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetSystemRegistryPath(){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetSystemRegistryPath(OLAObject));
        }

        /// <summary>
        /// 备份注册表键到文件
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <param name="filePath">备份文件路径（.reg 格式）</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 文件将以标准 .reg 格式保存，可以使用 regedit 导入
        /// </remarks>
        public int RegistryBackupToFile(int rootKey, string subKey, string filePath){
            return OLAPlugDLLHelper.RegistryBackupToFile(OLAObject, rootKey, subKey, filePath);
        }

        /// <summary>
        /// 从文件恢复注册表键
        /// </summary>
        /// <param name="filePath">备份文件路径（.reg 格式）</param>
        /// <returns>操作结果，1 表示成功，0 表示失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 文件必须是标准 .reg 格式
        /// </remarks>
        public int RegistryRestoreFromFile(string filePath){
            return OLAPlugDLLHelper.RegistryRestoreFromFile(OLAObject, filePath);
        }

        /// <summary>
        /// 比较两个注册表键
        /// </summary>
        /// <param name="rootKey1">第一个根键类型</param>
        /// <param name="subKey1">第一个子键路径</param>
        /// <param name="rootKey2">第二个根键类型</param>
        /// <param name="subKey2">第二个子键路径</param>
        /// <returns>JSON 字符串句柄，包含比较结果：{"equal": true/false, "differences": [...]}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryCompareKeys(int rootKey1, string subKey1, int rootKey2, string subKey2){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryCompareKeys(OLAObject, rootKey1, subKey1, rootKey2, subKey2));
        }

        /// <summary>
        /// 搜索注册表键
        /// </summary>
        /// <param name="rootKey">根键类型</param>
        /// <param name="searchPath">搜索起始路径</param>
        /// <param name="searchPattern">搜索模式（支持通配符 * 和 ?）</param>
        /// <param name="recursive">是否递归搜索，1 表示递归，0 表示仅搜索当前层级</param>
        /// <returns>JSON 数组字符串句柄，包含匹配的键路径，例如 ["path1","path2"]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistrySearchKeys(int rootKey, string searchPath, string searchPattern, int recursive){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistrySearchKeys(OLAObject, rootKey, searchPath, searchPattern, recursive));
        }

        /// <summary>
        /// 获取已安装软件列表
        /// </summary>
        /// <returns>JSON 数组字符串句柄，包含软件信息，每项包含 name、version、publisher、installDate 等字段</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// <br/>2. 该函数会同时扫描 32 位和 64 位软件列表
        /// </remarks>
        public string RegistryGetInstalledSoftware(){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetInstalledSoftware(OLAObject));
        }

        /// <summary>
        /// 获取 Windows 版本信息
        /// </summary>
        /// <returns>JSON 对象字符串句柄，包含 Windows版本信息：productName、currentVersion、currentBuild、releaseId 等</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetWindowsVersion(){
            return PtrToStringUTF8(OLAPlugDLLHelper.RegistryGetWindowsVersion(OLAObject));
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="password"></param>
        /// <returns>数据库连接句柄，如果打开失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于创建一个新的数据库文件
        /// <br/>2. 成功创建数据库后，返回一个非零的句柄，该句柄用于后续的数据库操作
        /// <br/>3. 如果指定的数据库文件存在，则返回0
        /// <br/>4. 在使用完数据库连接后，必须调用 CloseDatabase 接口关闭连接，以释放资源
        /// </remarks>
        public long CreateDatabase(string dbName, string password){
            return OLAPlugDLLHelper.CreateDatabase(OLAObject, dbName, password);
        }

        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="password"></param>
        /// <returns>数据库连接句柄，如果打开失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于打开一个现有的数据库文件
        /// <br/>2. 成功打开数据库后，返回一个非零的句柄，该句柄用于后续的数据库操作
        /// <br/>3. 如果指定的数据库文件不存在，连接失败
        /// <br/>4. 在使用完数据库连接后，必须调用 CloseDatabase 接口关闭连接，以释放资源
        /// </remarks>
        public long OpenDatabase(string dbName, string password){
            return OLAPlugDLLHelper.OpenDatabase(OLAObject, dbName, password);
        }

        /// <summary>
        /// 打开内存数据库连接
        /// </summary>
        /// <param name="address">数据库内存地址</param>
        /// <param name="size">数据库内存大小</param>
        /// <param name="password">数据库密码</param>
        /// <returns>数据库连接句柄，如果打开失败则返回 0</returns>
        public long OpenMemoryDatabase(long address, int size, string password){
            return OLAPlugDLLHelper.OpenMemoryDatabase(OLAObject, address, size, password);
        }

        /// <summary>
        /// 获取数据库操作的错误信息
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>错误信息字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 当数据库操作（如 ExecuteSql, ExecuteScalar 等）失败时，调用此函数可获取详细的错误描述
        /// <br/>2. 返回的字符串指针指向的内存由系统管理，调用者无需手动释放
        /// <br/>3. 此函数通常在数据库操作返回错误码后立即调用，以获取当前的错误状态
        /// </remarks>
        public string GetDatabaseError(long db){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDatabaseError(OLAObject, db));
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>操作结果，成功关闭返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于关闭由 OpenDatabase 接口打开的数据库连接
        /// <br/>2. 关闭连接后，传入的数据库句柄将失效，不能再用于其他数据库操作
        /// <br/>3. 即使关闭操作失败，也应认为该连接已不可用，并丢弃句柄
        /// <br/>4. 为防止资源泄漏，每个成功打开的数据库连接都应调用此接口进行关闭
        /// </remarks>
        public int CloseDatabase(long db){
            return OLAPlugDLLHelper.CloseDatabase(OLAObject, db);
        }

        /// <summary>
        /// 获取数据库中所有表的名称
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>包含所有表名的JSON数组字符串指针，例如：["table1", "table2", "table3"]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数查询数据库的系统表，获取所有用户定义表的名称列表
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 如果数据库中没有表，将返回一个空的JSON数组 "[]"
        /// <br/>4. 此操作不会修改数据库内容，是只读操作
        /// </remarks>
        public string GetAllTableNames(long db){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetAllTableNames(OLAObject, db));
        }

        /// <summary>
        /// 获取指定表的列信息
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="tableName">表名称</param>
        /// <returns>包含列信息的JSON数组字符串指针，例如：[{"name": "id", "type": "INTEGER"}, {"name":"name", "type": "TEXT"}]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数查询指定表的结构，返回其所有列的名称和数据类型
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 如果指定的表不存在，函数将返回 NULL 或一个表示错误的指针
        /// <br/>4. 数据类型通常为数据库原生类型，如 INTEGER, TEXT, REAL, BLOB 等
        /// </remarks>
        public string GetTableInfo(long db, string tableName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetTableInfo(OLAObject, db, tableName));
        }

        /// <summary>
        /// 获取指定表的详细列信息
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 GetTableInfo 相比，此函数提供更详细的元数据信息
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 主键信息（pk）和非空约束（notnull）对于理解表结构非常重要
        /// <br/>4. 此信息可用于动态生成SQL语句或进行数据验证
        /// </remarks>
        public string GetTableInfoDetail(long db, string tableName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetTableInfoDetail(OLAObject, db, tableName));
        }

        /// <summary>
        /// 执行一条SQL语句（非查询类）
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <returns>操作结果，成功执行返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于执行 INSERT, UPDATE, DELETE, CREATE TABLE 等修改数据库内容的SQL语句
        /// <br/>2. 对于 INSERT 语句，如果表有自增主键，新插入行的主键值可以通过其他接口获取
        /// <br/>3. 执行成功表示SQL语句被正确解析并执行，但不保证有数据行被实际修改
        /// <br/>4. 如果SQL语句语法错误或违反约束，将返回 0，可通过 GetDatabaseError 获取错误信息
        /// </remarks>
        public int ExecuteSql(long db, string sql){
            return OLAPlugDLLHelper.ExecuteSql(OLAObject, db, sql);
        }

        /// <summary>
        /// 执行一条返回单个值的SQL查询
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="sql">要执行的SQL查询语句</param>
        /// <returns>查询结果的字符串指针，如果查询失败或无结果则返回0，结果以字符串形式返回，调用者需根据预期类型进行转换</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于执行如 SELECT COUNT(*) FROM table 或 SELECT MAX(id) FROM table 这类返回单一值的查询
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 如果查询返回多行或多列，此函数的行为是未定义的，应使用 ExecuteReader
        /// </remarks>
        public int ExecuteScalar(long db, string sql){
            return OLAPlugDLLHelper.ExecuteScalar(OLAObject, db, sql);
        }

        /// <summary>
        /// 执行一条SQL查询并返回结果集
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="sql">要执行的SQL查询语句</param>
        /// <returns>结果集句柄，如果查询失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于执行 SELECT 语句，返回一个可遍历的结果集
        /// <br/>2. 成功执行后，返回一个非零的结果集句柄，用于后续的 Read, GetDataCount 等操作
        /// <br/>3. 在使用完结果集后，必须调用 Finalize 接口释放资源
        /// <br/>4. 如果SQL语句不是查询语句，行为是未定义的，应使用 ExecuteSql
        /// </remarks>
        public long ExecuteReader(long db, string sql){
            return OLAPlugDLLHelper.ExecuteReader(OLAObject, db, sql);
        }

        /// <summary>
        /// 读取结果集的下一行数据
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>成功读取到下一行返回 1，没有更多数据返回 0，发生错误返回 -1</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于遍历由 ExecuteReader 生成的结果集
        /// <br/>2. 调用此函数后，结果集的当前位置会移动到下一行
        /// <br/>3. 在首次调用 Read 前，结果集不指向任何有效数据行
        /// <br/>4. 返回 1 表示成功读取了一行，此时可以使用 GetXXXByColumnName 或 GetXXX 系列函数获取该行数据
        /// </remarks>
        public int Read(long stmt){
            return OLAPlugDLLHelper.Read(OLAObject, stmt);
        }

        /// <summary>
        /// 获取结果集中数据行的总数
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>数据行的总数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回结果集中包含的总行数
        /// <br/>2. 对于大型结果集，此操作可能需要遍历整个结果集，性能开销较大
        /// <br/>3. 某些数据库驱动可能不支持直接获取总行数，此时可能返回 -1 或其他错误值
        /// <br/>4. 在调用 Read 遍历结果集前后调用此函数，返回值应相同
        /// </remarks>
        public int GetDataCount(long stmt){
            return OLAPlugDLLHelper.GetDataCount(OLAObject, stmt);
        }

        /// <summary>
        /// 获取结果集中列的总数
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>列的总数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回结果集包含的列（字段）的数量
        /// <br/>2. 此值在结果集的生命周期内是固定的，不会改变
        /// <br/>3. 在首次调用 Read 前或后调用此函数均可，结果相同
        /// <br/>4. 获取列数后，可以通过 GetColumnName, GetColumnType 等函数获取每列的元信息
        /// </remarks>
        public int GetColumnCount(long stmt){
            return OLAPlugDLLHelper.GetColumnCount(OLAObject, stmt);
        }

        /// <summary>
        /// 根据列索引获取列名
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>列名的字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于获取结果集中指定位置列的名称
        /// <br/>2. 索引从 0 开始，最大值为 GetColumnCount(reader) - 1
        /// <br/>3. 如果 columnIndex 超出范围，行为是未定义的，可能返回 NULL 或错误指针
        /// <br/>4. 返回的字符串指针由系统管理，调用者无需手动释放内存
        /// </remarks>
        public string GetColumnName(long stmt, int iCol){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetColumnName(OLAObject, stmt, iCol));
        }

        /// <summary>
        /// 根据列索引获取列的索引（冗余函数，通常直接使用 columnIndex）
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的索引，如果列不存在则返回 -1</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于根据列名查找其在结果集中的位置（索引）
        /// <br/>2. 索引从 0 开始
        /// <br/>3. 如果结果集中存在同名列，此函数的行为可能不确定，通常返回第一个匹配的索引
        /// <br/>4. 此函数对于通过列名访问数据非常有用，可以避免硬编码列索引
        /// </remarks>
        public int GetColumnIndex(long stmt, string columnName){
            return OLAPlugDLLHelper.GetColumnIndex(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列索引获取列的数据类型
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>数据类型的字符串表示</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于获取结果集中指定列的数据类型
        /// <br/>2. 类型通常为 INTEGER, TEXT, REAL, BLOB 等
        /// <br/>3. 返回的字符串指针由系统管理，调用者无需手动释放内存
        /// <br/>4. 此信息可用于在获取数据前进行类型检查或转换
        /// </remarks>
        public int GetColumnType(long stmt, int iCol){
            return OLAPlugDLLHelper.GetColumnType(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 释放结果集资源
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>成功释放返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于关闭和释放由 ExecuteReader 生成的结果集占用的资源
        /// <br/>2. 在完成对结果集的所有操作（如 Read, GetData 等）后，必须调用此函数
        /// <br/>3. 即使在遍历结果集前发生错误，也应调用 Finalize 来清理资源
        /// <br/>4. 调用此函数后，传入的 reader 句柄将失效，不能再使用
        /// </remarks>
        public int Finalize(long stmt){
            return OLAPlugDLLHelper.Finalize(OLAObject, stmt);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 double 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>列的 double 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 double 类型
        /// <br/>2. 如果列的数据类型不是数值类型，系统会尝试进行转换
        /// <br/>3. 如果转换失败或数据为 NULL，返回值可能是 0.0 或其他默认值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public double GetDouble(long stmt, int iCol){
            return OLAPlugDLLHelper.GetDouble(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 int32 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>列的 int32 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 32 位整数类型
        /// <br/>2. 如果列的数据类型不是整数类型，系统会尝试进行转换
        /// <br/>3. 如果转换失败、数据为 NULL 或数值超出 int32 范围，行为是未定义的
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public int GetInt32(long stmt, int iCol){
            return OLAPlugDLLHelper.GetInt32(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 int64 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>列的 int64 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 64 位整数类型
        /// <br/>2. 适用于处理可能超出 32 位范围的大整数
        /// <br/>3. 如果列的数据类型不是整数类型，系统会尝试进行转换
        /// <br/>4. 如果转换失败、数据为 NULL 或数值超出 int64 范围，行为是未定义的
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public long GetInt64(long stmt, int iCol){
            return OLAPlugDLLHelper.GetInt64(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的字符串值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="iCol"></param>
        /// <returns>字符串值的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的文本数据
        /// <br/>2. 返回的字符串指针指向的数据由结果集管理，其生命周期与结果集相同
        /// <br/>3. 在调用 Finalize 释放结果集后，该指针将失效
        /// <br/>4. 如果列的数据类型不是文本类型，系统会将其转换为字符串
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public string GetString(long stmt, int iCol){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetString(OLAObject, stmt, iCol));
        }

        /// <summary>
        /// 根据列名获取当前行指定列的 double 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的 double 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetDouble 相同，但通过列名而非索引来访问数据
        /// <br/>2. 内部通常先调用 GetColumnIndex 获取索引，再调用 GetDouble
        /// <br/>3. 使用列名访问数据可以提高代码的可读性和可维护性，避免因列顺序改变而引发错误
        /// <br/>4. 如果列名不存在，行为是未定义的，可能返回 0.0 或其他错误值
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public double GetDoubleByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetDoubleByColumnName(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的 int32 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的 int32 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetInt32 相同，但通过列名而非索引来访问数据
        /// <br/>2. 使用列名可以避免硬编码列索引，使代码更灵活
        /// <br/>3. 如果列名不存在，行为是未定义的，可能返回 0 或其他错误值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public int GetInt32ByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetInt32ByColumnName(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的 int64 值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的 int64 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetInt64 相同，但通过列名而非索引来访问数据
        /// <br/>2. 适用于通过列名访问大整数类型的数据
        /// <br/>3. 如果列名不存在，行为是未定义的，可能返回 0 或其他错误值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public long GetInt64ByColumnName(long stmt, string columnName){
            return OLAPlugDLLHelper.GetInt64ByColumnName(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的字符串值
        /// </summary>
        /// <param name="stmt"></param>
        /// <param name="columnName">列的名称</param>
        /// <returns>字符串值的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetString 相同，但通过列名而非索引来访问数据
        /// <br/>2. 返回的字符串指针生命周期与结果集相同
        /// <br/>3. 这是访问结果集数据最常用和最安全的方式之一，因为它不依赖于列的物理顺序
        /// <br/>4. 如果列名不存在，行为是未定义的，可能返回 NULL 或其他错误指针
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public string GetStringByColumnName(long stmt, string columnName){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetStringByColumnName(OLAObject, stmt, columnName));
        }

        /// <summary>
        /// 初始化ola相关数据库,包括olg_config,ola_image表
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>成功初始化返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于在打开的数据库上创建OLA系统所需的表和索引
        /// <br/>2. 此操作是幂等的，如果数据库已初始化，则不会重复创建表
        /// <br/>3. 必须在使用任何依赖OLA数据库结构的接口前调用此函数
        /// <br/>4. 初始化失败通常是因为数据库文件不可写或磁盘空间不足
        /// </remarks>
        public int InitOlaDatabase(long db){
            return OLAPlugDLLHelper.InitOlaDatabase(OLAObject, db);
        }

        /// <summary>
        /// 从指定目录初始化OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">图像文件所在的目录路径</param>
        /// <param name="cover">是否覆盖已存在的数据</param>
        /// <returns>成功返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于批量导入指定目录下的所有图像文件到OLA数据库
        /// <br/>2. cover 参数控制是否覆盖数据库中已存在的同名图像数据
        /// <br/>3. 支持的图像格式通常包括 BMP, PNG, JPG 等常见格式
        /// <br/>4. 此操作可能耗时较长，取决于目录中文件的数量和大小
        /// </remarks>
        public int InitOlaImageFromDir(long db, string dir, int cover){
            return OLAPlugDLLHelper.InitOlaImageFromDir(OLAObject, db, dir, cover);
        }

        /// <summary>
        /// 移除指定文件夹下所有图片数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">包含要移除图像的目录路径</param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于批量删除数据库中与指定目录关联的所有OLA图像数据
        /// <br/>2. 此操作会删除所有在该目录下导入或与该目录路径匹配的图像记录
        /// <br/>3. 删除操作是永久性的，无法恢复
        /// <br/>4. 在执行此操作前，请确保不再需要这些图像数据，且没有其他功能依赖于它们
        /// </remarks>
        public int RemoveOlaImageFromDir(long db, string dir){
            return OLAPlugDLLHelper.RemoveOlaImageFromDir(OLAObject, db, dir);
        }

        /// <summary>
        /// 将OLA图像数据从数据库导出到指定目录
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir"></param>
        /// <param name="exportDir">导出的目标目录路径</param>
        /// <returns>成功导出返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于将数据库中存储的所有OLA图像数据导出为文件
        /// <br/>2. 导出的文件将保存在 exportDir 指定的目录中
        /// <br/>3. 确保目标目录存在且有写入权限
        /// <br/>4. 此操作可用于备份图像数据或在不同系统间迁移数据
        /// </remarks>
        public int ExportOlaImageDir(long db, string dir, string exportDir){
            return OLAPlugDLLHelper.ExportOlaImageDir(OLAObject, db, dir, exportDir);
        }

        /// <summary>
        /// 从文件导入单个OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir"></param>
        /// <param name="fileName"></param>
        /// <param name="cover">是否覆盖已存在的同名图像</param>
        /// <returns>成功导入返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于将单个图像文件导入到OLA数据库中，并指定其在库中的名称
        /// <br/>2. imagePath 必须指向一个有效的图像文件
        /// <br/>3. name 是该图像在数据库中的唯一标识符，后续操作将使用此名称
        /// <br/>4. cover 参数决定是否替换数据库中已存在的同名图像
        /// </remarks>
        public int ImportOlaImage(long db, string dir, string fileName, int cover){
            return OLAPlugDLLHelper.ImportOlaImage(OLAObject, db, dir, fileName, cover);
        }

        /// <summary>
        /// 从数据库中获取指定名称的OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir"></param>
        /// <param name="fileName"></param>
        /// <returns>图像数据的指针，如果未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从OLA数据库中检索已存储的图像
        /// <br/>2. 返回的指针指向的内存包含原始图像数据，调用者需负责解析和使用
        /// <br/>3. 图像数据的格式信息可能需要通过其他元数据接口获取
        /// <br/>4. 如果指定名称的图像不存在，函数返回 0
        /// </remarks>
        public long GetOlaImage(long db, string dir, string fileName){
            return OLAPlugDLLHelper.GetOlaImage(OLAObject, db, dir, fileName);
        }

        /// <summary>
        /// 从数据库中移除指定名称的OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir"></param>
        /// <param name="fileName"></param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从OLA数据库中删除指定的图像数据
        /// <br/>2. 删除操作是永久性的，无法通过常规手段恢复
        /// <br/>3. 如果指定名称的图像不存在，函数可能返回成功或失败，具体取决于实现
        /// <br/>4. 删除图像后，任何引用该图像的操作都将失败
        /// </remarks>
        public int RemoveOlaImage(long db, string dir, string fileName){
            return OLAPlugDLLHelper.RemoveOlaImage(OLAObject, db, dir, fileName);
        }

        /// <summary>
        /// 设置数据库配置项
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="key">配置项的键名</param>
        /// <param name="value">配置项的值</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于在数据库中存储键值对形式的配置信息
        /// <br/>2. 配置信息通常用于保存应用程序的设置或状态
        /// <br/>3. 如果键已存在，此操作将更新其值
        /// <br/>4. 配置项的存储是持久化的，即使关闭数据库后依然存在
        /// </remarks>
        public int SetDbConfig(long db, string key, string value){
            return OLAPlugDLLHelper.SetDbConfig(OLAObject, db, key, value);
        }

        /// <summary>
        /// 获取数据库配置项的值
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="key">配置项的键名</param>
        /// <returns>配置项的值字符串指针，如果键不存在则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从数据库中读取指定键的配置信息
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 如果指定的键在数据库中不存在，函数返回 0
        /// <br/>4. 获取配置项是应用程序读取持久化设置的标准方式
        /// </remarks>
        public string GetDbConfig(long db, string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDbConfig(OLAObject, db, key));
        }

        /// <summary>
        /// 从数据库中移除指定的配置项
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="key">配置项的键名</param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除数据库中存储的特定配置项
        /// <br/>2. 删除后，再次调用 GetDbConfig 将无法获取该键的值
        /// <br/>3. 如果指定的键不存在，函数可能返回成功或失败，具体取决于实现
        /// <br/>4. 此操作不会影响其他配置项
        /// </remarks>
        public int RemoveDbConfig(long db, string key){
            return OLAPlugDLLHelper.RemoveDbConfig(OLAObject, db, key);
        }

        /// <summary>
        /// 设置带作用域的数据库配置项
        /// </summary>
        /// <param name="key">配置项的键名</param>
        /// <param name="value">配置项的值</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 SetDbConfig 类似，但增加了作用域（scope）参数
        /// <br/>2. 作用域可用于对配置项进行分类或隔离，例如按模块、用户或环境划分
        /// <br/>3. 相同的键名在不同作用域下可以存储不同的值
        /// <br/>4. 此函数提供了更灵活的配置管理能力
        /// </remarks>
        public int SetDbConfigEx(string key, string value){
            return OLAPlugDLLHelper.SetDbConfigEx(OLAObject, key, value);
        }

        /// <summary>
        /// 获取带作用域的数据库配置项的值
        /// </summary>
        /// <param name="key">配置项的键名</param>
        /// <returns>配置项的值字符串指针，如果键不存在则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于读取在特定作用域下存储的配置信息
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 必须同时提供正确的作用域和键名才能获取到值
        /// <br/>4. 如果指定的作用域和键的组合不存在，函数返回 0
        /// </remarks>
        public string GetDbConfigEx(string key){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetDbConfigEx(OLAObject, key));
        }

        /// <summary>
        /// 从数据库中移除带作用域的配置项
        /// </summary>
        /// <param name="key">配置项的键名</param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除在特定作用域下存储的配置项
        /// <br/>2. 必须同时指定正确的作用域和键名才能成功删除
        /// <br/>3. 此操作只影响指定作用域下的特定键，不会影响其他作用域或无作用域的同名键
        /// <br/>4. 删除后，该作用域下的该键将不再存在
        /// </remarks>
        public int RemoveDbConfigEx(string key){
            return OLAPlugDLLHelper.RemoveDbConfigEx(OLAObject, key);
        }

        /// <summary>
        /// 从指定目录中加载字库文件，并将其初始化到OLA数据库中
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="dict_path">字库图片文件夹路径</param>
        /// <param name="cover">是否覆盖已存在的图像数据</param>
        /// <returns>成功返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从指定目录中加载字库图片文件，并将其初始化到OLA数据库中。适用于批量导入字库的场景
        /// <br/>2. cover 参数用于控制是否覆盖已存在的图像数据。设置为 1 时，会覆盖现有数据；设置为 0时，会跳过已存在的图像
        /// <br/>3. 如果初始化失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>4. 确保目录路径正确，且图像文件格式受支持，否则可能导致初始化失败
        /// </remarks>
        public int InitDictFromDir(long db, string dict_name, string dict_path, int cover){
            return OLAPlugDLLHelper.InitDictFromDir(OLAObject, db, dict_name, dict_path, cover);
        }

        /// <summary>
        /// 向指定字库中导入单个文字的图像
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="pic_file_name"></param>
        /// <param name="cover">是否覆盖已存在的同名文字</param>
        /// <returns>成功导入返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于将单个字符的图像添加到指定的字库中
        /// <br/>2. imagePath 必须指向一个有效的图像文件，该图像应只包含单个字符
        /// <br/>3. word 参数是该字符在字库中的标识，通常就是该字符本身
        /// <br/>4. 此接口适用于动态添加或更新字库中的单个字符
        /// </remarks>
        public int ImportDictWord(long db, string dict_name, string pic_file_name, int cover){
            return OLAPlugDLLHelper.ImportDictWord(OLAObject, db, dict_name, pic_file_name, cover);
        }

        /// <summary>
        /// 将OLA数据库中的图像数据导出到指定目录
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="export_dir"></param>
        /// <returns>成功返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于将OLA数据库中的图像数据导出到指定目录，适用于批量导出字库图像数据的场景
        /// <br/>2. 如果导出失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>3. 确保目录路径正确，且图像数据存在于数据库中，否则可能导致导出失败
        /// <br/>4. 导出的图像文件将保存在 exportDir 指定的目录中，确保目标目录有足够的存储空间
        /// </remarks>
        public int ExportDict(long db, string dict_name, string export_dir){
            return OLAPlugDLLHelper.ExportDict(OLAObject, db, dict_name, export_dir);
        }

        /// <summary>
        /// 从数据库中移除整个字库
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">要移除的字库名称</param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除数据库中存储的整个字库及其所有图像数据
        /// <br/>2. 此操作是永久性的，会删除该字库下的所有字符图像
        /// <br/>3. 删除后，任何使用该字库的识别操作都将失败
        /// <br/>4. 在执行此操作前应确保没有其他进程或功能依赖于该字库
        /// </remarks>
        public int RemoveDict(long db, string dict_name){
            return OLAPlugDLLHelper.RemoveDict(OLAObject, db, dict_name);
        }

        /// <summary>
        /// 移除词典词条
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="word">要移除的文字</param>
        /// <returns>成功移除返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从字库中删除特定字符的图像数据
        /// <br/>2. 此操作只影响指定字库中的指定字符，不会影响字库中的其他字符
        /// <br/>3. 删除后，OCR识别将无法再识别该字符
        /// <br/>4. 此接口适用于维护和更新字库，移除不再需要或错误的字符
        /// </remarks>
        public int RemoveDictWord(long db, string dict_name, string word){
            return OLAPlugDLLHelper.RemoveDictWord(OLAObject, db, dict_name, word);
        }

        /// <summary>
        /// 读取字库图片
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="word">要读取的文字</param>
        /// <param name="gap">文字间隔，单位为像素</param>
        /// <param name="dir">拼接方向，0-水平拼接，1-垂直拼接</param>
        /// <returns>图像对象的指针。如果操作失败，返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从OLA数据库中获取指定字典名称和文字的图像数据，适用于从数据库中查找指定文字的场景
        /// <br/>2. 如果图像不存在或操作失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>3. 确保字典名称和文字正确，且图像数据存在于数据库中，否则可能导致获取失败
        /// <br/>4. 使用完返回的图像对象指针后，应妥善处理资源，避免内存泄漏
        /// </remarks>
        public long GetDictImage(long db, string dict_name, string word, int gap, int dir){
            return OLAPlugDLLHelper.GetDictImage(OLAObject, db, dict_name, word, gap, dir);
        }

        /// <summary>
        /// 打开视频文件
        /// </summary>
        /// <param name="videoPath">视频文件路径（支持本地文件和网络流）</param>
        /// <returns>视频句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的句柄用于后续的视频操作，使用完毕后需调用CloseVideo释放
        /// </remarks>
        public long OpenVideo(string videoPath){
            return OLAPlugDLLHelper.OpenVideo(OLAObject, videoPath);
        }

        /// <summary>
        /// 打开摄像头设备
        /// </summary>
        /// <param name="deviceIndex">摄像头设备索引（默认0）</param>
        /// <returns>视频句柄，失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的句柄用于后续的视频操作，使用完毕后需调用CloseVideo释放
        /// </remarks>
        public long OpenCamera(int deviceIndex){
            return OLAPlugDLLHelper.OpenCamera(OLAObject, deviceIndex);
        }

        /// <summary>
        /// 关闭视频并释放资源
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int CloseVideo(long videoHandle){
            return OLAPlugDLLHelper.CloseVideo(OLAObject, videoHandle);
        }

        /// <summary>
        /// 检查视频是否已打开
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>检查结果
        ///<br/>0: 未打开
        ///<br/>1: 已打开
        /// </returns>
        public int IsVideoOpened(long videoHandle){
            return OLAPlugDLLHelper.IsVideoOpened(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频基本信息（JSON格式）
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>返回包含视频信息的JSON字符串指针，需调用FreeStringPtr释放；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. JSON包含：width, height, fps, totalFrames, duration, codecName, fileSize
        /// </remarks>
        public string GetVideoInfo(long videoHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetVideoInfo(OLAObject, videoHandle));
        }

        /// <summary>
        /// 获取视频宽度
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频宽度（像素），失败返回0</returns>
        public int GetVideoWidth(long videoHandle){
            return OLAPlugDLLHelper.GetVideoWidth(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频高度
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频高度（像素），失败返回0</returns>
        public int GetVideoHeight(long videoHandle){
            return OLAPlugDLLHelper.GetVideoHeight(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频帧率
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频帧率（FPS），失败返回0.0</returns>
        public double GetVideoFPS(long videoHandle){
            return OLAPlugDLLHelper.GetVideoFPS(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频总帧数
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频总帧数，失败返回0</returns>
        public int GetVideoTotalFrames(long videoHandle){
            return OLAPlugDLLHelper.GetVideoTotalFrames(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频时长
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频时长（秒），失败返回0.0</returns>
        public double GetVideoDuration(long videoHandle){
            return OLAPlugDLLHelper.GetVideoDuration(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取当前帧位置
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>当前帧索引，失败返回-1</returns>
        public int GetCurrentFrameIndex(long videoHandle){
            return OLAPlugDLLHelper.GetCurrentFrameIndex(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>当前时间戳（秒），失败返回0.0</returns>
        public double GetCurrentTimestamp(long videoHandle){
            return OLAPlugDLLHelper.GetCurrentTimestamp(OLAObject, videoHandle);
        }

        /// <summary>
        /// 读取下一帧
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄由内部管理，不需要手动释放
        /// </remarks>
        public long ReadNextFrame(long videoHandle){
            return OLAPlugDLLHelper.ReadNextFrame(OLAObject, videoHandle);
        }

        /// <summary>
        /// 读取指定索引的帧
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="frameIndex">帧索引（从0开始）</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄由内部管理，不需要手动释放
        /// </remarks>
        public long ReadFrameAtIndex(long videoHandle, int frameIndex){
            return OLAPlugDLLHelper.ReadFrameAtIndex(OLAObject, videoHandle, frameIndex);
        }

        /// <summary>
        /// 读取指定时间戳的帧
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="timestamp">时间戳（秒）</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄由内部管理，不需要手动释放
        /// </remarks>
        public long ReadFrameAtTime(long videoHandle, double timestamp){
            return OLAPlugDLLHelper.ReadFrameAtTime(OLAObject, videoHandle, timestamp);
        }

        /// <summary>
        /// 读取当前帧（不移动位置）
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄由内部管理，不需要手动释放
        /// </remarks>
        public long ReadCurrentFrame(long videoHandle){
            return OLAPlugDLLHelper.ReadCurrentFrame(OLAObject, videoHandle);
        }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="frameIndex">目标帧索引</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SeekToFrame(long videoHandle, int frameIndex){
            return OLAPlugDLLHelper.SeekToFrame(OLAObject, videoHandle, frameIndex);
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="timestamp">目标时间戳（秒）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SeekToTime(long videoHandle, double timestamp){
            return OLAPlugDLLHelper.SeekToTime(OLAObject, videoHandle, timestamp);
        }

        /// <summary>
        /// 跳转到视频开头
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SeekToBeginning(long videoHandle){
            return OLAPlugDLLHelper.SeekToBeginning(OLAObject, videoHandle);
        }

        /// <summary>
        /// 跳转到视频结尾
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SeekToEnd(long videoHandle){
            return OLAPlugDLLHelper.SeekToEnd(OLAObject, videoHandle);
        }

        /// <summary>
        /// 批量提取视频帧并保存为文件
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="startFrame">起始帧索引</param>
        /// <param name="endFrame">结束帧索引（-1表示到视频末尾）</param>
        /// <param name="step">帧间隔（1表示每帧都提取）</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="imageFormat">图像格式（"png"、"jpg"等）</param>
        /// <param name="jpegQuality">JPEG质量（0-100）</param>
        /// <returns>返回提取的帧数，失败返回0</returns>
        public int ExtractFramesToFiles(long videoHandle, int startFrame, int endFrame, int step, string outputDir, string imageFormat, int jpegQuality){
            return OLAPlugDLLHelper.ExtractFramesToFiles(OLAObject, videoHandle, startFrame, endFrame, step, outputDir, imageFormat, jpegQuality);
        }

        /// <summary>
        /// 按时间间隔提取帧并保存为文件
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="intervalSeconds">时间间隔（秒）</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="imageFormat">图像格式（"png"、"jpg"等）</param>
        /// <returns>返回提取的帧数，失败返回0</returns>
        public int ExtractFramesByInterval(long videoHandle, double intervalSeconds, string outputDir, string imageFormat){
            return OLAPlugDLLHelper.ExtractFramesByInterval(OLAObject, videoHandle, intervalSeconds, outputDir, imageFormat);
        }

        /// <summary>
        /// 提取关键帧（基于场景变化检测）
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="threshold">场景变化阈值（0-1）</param>
        /// <param name="maxFrames">最大提取帧数（0表示不限制）</param>
        /// <param name="outputDir">输出目录</param>
        /// <param name="imageFormat">图像格式（"png"、"jpg"等）</param>
        /// <returns>返回提取的关键帧数，失败返回0</returns>
        public int ExtractKeyFrames(long videoHandle, double threshold, int maxFrames, string outputDir, string imageFormat){
            return OLAPlugDLLHelper.ExtractKeyFrames(OLAObject, videoHandle, threshold, maxFrames, outputDir, imageFormat);
        }

        /// <summary>
        /// 保存当前帧为图片文件
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="quality">图片质量（对于JPEG，范围0-100）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SaveCurrentFrame(long videoHandle, string outputPath, int quality){
            return OLAPlugDLLHelper.SaveCurrentFrame(OLAObject, videoHandle, outputPath, quality);
        }

        /// <summary>
        /// 保存指定帧为图片文件
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="frameIndex">帧索引</param>
        /// <param name="outputPath">输出文件路径</param>
        /// <param name="quality">图片质量（对于JPEG，范围0-100）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SaveFrameAtIndex(long videoHandle, int frameIndex, string outputPath, int quality){
            return OLAPlugDLLHelper.SaveFrameAtIndex(OLAObject, videoHandle, frameIndex, outputPath, quality);
        }

        /// <summary>
        /// 将当前帧转换为Base64字符串
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="format">图片格式（"png"、"jpg"等）</param>
        /// <returns>返回Base64编码的图片数据字符串指针，需调用FreeStringPtr释放；失败返回0</returns>
        public string FrameToBase64(long videoHandle, string format){
            return PtrToStringUTF8(OLAPlugDLLHelper.FrameToBase64(OLAObject, videoHandle, format));
        }

        /// <summary>
        /// 计算两帧之间的相似度
        /// </summary>
        /// <param name="frame1">第一帧图像句柄</param>
        /// <param name="frame2">第二帧图像句柄</param>
        /// <returns>相似度（0-1，1表示完全相同）</returns>
        public double CalculateFrameSimilarity(long frame1, long frame2){
            return OLAPlugDLLHelper.CalculateFrameSimilarity(OLAObject, frame1, frame2);
        }

        /// <summary>
        /// 快速获取视频文件信息（无需打开整个视频）
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>返回包含视频信息的JSON字符串指针，需调用FreeStringPtr释放；失败返回0</returns>
        public string GetVideoInfoFromPath(string videoPath){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetVideoInfoFromPath(OLAObject, videoPath));
        }

        /// <summary>
        /// 检查视频文件是否有效
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>检查结果
        ///<br/>0: 无效
        ///<br/>1: 有效
        /// </returns>
        public int IsValidVideoFile(string videoPath){
            return OLAPlugDLLHelper.IsValidVideoFile(OLAObject, videoPath);
        }

        /// <summary>
        /// 快速提取单帧（无需保持视频打开状态）
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="frameIndex">帧索引</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄需调用FreeImagePtr释放
        /// </remarks>
        public long ExtractSingleFrame(string videoPath, int frameIndex){
            return OLAPlugDLLHelper.ExtractSingleFrame(OLAObject, videoPath, frameIndex);
        }

        /// <summary>
        /// 快速提取视频第一帧（常用于缩略图）
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>图像句柄（BGRA格式），失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的图像句柄需调用FreeImagePtr释放
        /// </remarks>
        public long ExtractThumbnail(string videoPath){
            return OLAPlugDLLHelper.ExtractThumbnail(OLAObject, videoPath);
        }

        /// <summary>
        /// 转换视频格式
        /// </summary>
        /// <param name="inputPath">输入视频路径</param>
        /// <param name="outputPath">输出视频路径</param>
        /// <param name="codec">编解码器（"H264", "XVID", "MJPG"等）</param>
        /// <param name="fps">输出帧率（-1表示使用原始帧率）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ConvertVideo(string inputPath, string outputPath, string codec, double fps){
            return OLAPlugDLLHelper.ConvertVideo(OLAObject, inputPath, outputPath, codec, fps);
        }

        /// <summary>
        /// 调整视频尺寸
        /// </summary>
        /// <param name="inputPath">输入视频路径</param>
        /// <param name="outputPath">输出视频路径</param>
        /// <param name="width">目标宽度</param>
        /// <param name="height">目标高度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ResizeVideo(string inputPath, string outputPath, int width, int height){
            return OLAPlugDLLHelper.ResizeVideo(OLAObject, inputPath, outputPath, width, height);
        }

        /// <summary>
        /// 剪切视频片段
        /// </summary>
        /// <param name="inputPath">输入视频路径</param>
        /// <param name="outputPath">输出视频路径</param>
        /// <param name="startTime">起始时间（秒）</param>
        /// <param name="endTime">结束时间（秒）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int TrimVideo(string inputPath, string outputPath, double startTime, double endTime){
            return OLAPlugDLLHelper.TrimVideo(OLAObject, inputPath, outputPath, startTime, endTime);
        }

        /// <summary>
        /// 从图片序列创建视频
        /// </summary>
        /// <param name="imageDir">图片目录路径</param>
        /// <param name="outputPath">输出视频路径</param>
        /// <param name="fps">帧率</param>
        /// <param name="codec">编解码器（"H264"等）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 图片文件名应按字母顺序排列
        /// </remarks>
        public int CreateVideoFromImages(string imageDir, string outputPath, double fps, string codec){
            return OLAPlugDLLHelper.CreateVideoFromImages(OLAObject, imageDir, outputPath, fps, codec);
        }

        /// <summary>
        /// 检测视频中的场景变化点
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="threshold">场景变化阈值（0-1）</param>
        /// <returns>返回场景变化帧索引的JSON数组字符串，需调用FreeStringPtr释放；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. JSON格式：[0, 123, 456, ...]
        /// </remarks>
        public string DetectSceneChanges(string videoPath, double threshold){
            return PtrToStringUTF8(OLAPlugDLLHelper.DetectSceneChanges(OLAObject, videoPath, threshold));
        }

        /// <summary>
        /// 计算视频平均亮度
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>平均亮度（0-255），失败返回-1</returns>
        public double CalculateAverageBrightness(string videoPath){
            return OLAPlugDLLHelper.CalculateAverageBrightness(OLAObject, videoPath);
        }

        /// <summary>
        /// 检测视频中的运动
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="threshold">运动检测阈值（建议值：30.0）</param>
        /// <returns>返回包含运动的帧索引的JSON数组字符串，需调用FreeStringPtr释放；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. JSON格式：[10, 25, 67, ...]
        /// </remarks>
        public string DetectMotion(string videoPath, double threshold){
            return PtrToStringUTF8(OLAPlugDLLHelper.DetectMotion(OLAObject, videoPath, threshold));
        }

        /// <summary>
        /// 设置窗口的状态（如显示、隐藏、最小化、最大化等）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="state">窗口状态标志，窗口状态标志，可选值如下
        ///<br/> 0: 关闭指定窗口（发送WM_CLOSE消息）
        ///<br/> 1: 激活指定窗口（设为前台窗口）
        ///<br/> 2: 最小化指定窗口，但不激活
        ///<br/> 3: 最小化指定窗口，并释放内存（适用于长期最小化）
        ///<br/> 4: 最大化指定窗口，同时激活窗口
        ///<br/> 5: 恢复指定窗口到正常大小，但不激活
        ///<br/> 6: 隐藏指定窗口（窗口不可见但仍在运行）
        ///<br/> 7: 显示指定窗口（使隐藏的窗口重新可见）
        ///<br/> 8: 置顶指定窗口（窗口始终保持在最前）
        ///<br/> 9: 取消置顶指定窗口（恢复正常Z序）
        ///<br/> 10: 禁止指定窗口（使窗口无法接收输入）
        ///<br/> 11: 取消禁止指定窗口（恢复窗口输入功能）
        ///<br/> 12: 恢复并激活指定窗口（从最小化状态）
        ///<br/> 13: 强制结束窗口所在进程（谨慎使用）
        ///<br/> 14: 闪烁指定的窗口（吸引用户注意）
        ///<br/> 15: 使指定的窗口获取输入焦点
        /// </param>
        /// <returns>0: 设置失败（可能原因：无效的窗口句柄、无效的状态标志、窗口已被销毁等）1: 设置成功</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在使用强制结束进程（flag=13）时要特别谨慎，确保已保存相关数据
        /// <br/>2. 某些状态组合可能会相互影响，建议按照逻辑顺序设置
        /// <br/>3. 窗口状态的改变可能会触发窗口的相关事件和回调
        /// <br/>4. 部分状态设置可能会受到系统或应用程序的安全策略限制
        /// </remarks>
        public int SetWindowState(long hwnd, int state){
            return OLAPlugDLLHelper.SetWindowState(OLAObject, hwnd, state);
        }

        /// <summary>
        /// 根据窗口标题或类名查找窗口
        /// </summary>
        /// <param name="class_name"></param>
        /// <param name="title"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于在系统中查找符合条件的第一个顶层窗口
        /// <br/>2. 如果 lpClassName 为 NULL，则忽略类名进行匹配
        /// <br/>3. 如果 lpWindowName 为 NULL，则忽略窗口标题进行匹配
        /// <br/>4. 此函数只搜索顶层窗口，不包括子窗口
        /// </remarks>
        public long FindWindow(string class_name, string title){
            return OLAPlugDLLHelper.FindWindow(OLAObject, class_name, title);
        }

        /// <summary>
        /// 获取系统剪贴板的文本内容
        /// </summary>
        /// <returns>剪贴板文本的指针，如果失败或剪贴板无文本则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数打开剪贴板，获取 CF_TEXT 格式的文本内容并返回
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 在调用此函数前，应确保剪贴板中包含文本数据
        /// <br/>4. 如果剪贴板被其他程序占用，函数可能失败
        /// </remarks>
        public long GetClipboard(){
            return OLAPlugDLLHelper.GetClipboard(OLAObject);
        }

        /// <summary>
        /// 设置系统剪贴板的文本内容
        /// </summary>
        /// <param name="text">要设置的文本内容</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数将指定的文本字符串放入系统剪贴板
        /// <br/>2. 执行后，文本内容可被其他应用程序粘贴使用
        /// <br/>3. 函数会自动打开和关闭剪贴板
        /// <br/>4. 如果剪贴板被其他程序长时间占用，设置可能会失败
        /// </remarks>
        public int SetClipboard(string text){
            return OLAPlugDLLHelper.SetClipboard(OLAObject, text);
        }

        /// <summary>
        /// 向指定窗口发送粘贴命令（模拟 Ctrl+V）
        /// </summary>
        /// <param name="hwnd">目标窗口的句柄</param>
        /// <returns>成功发送返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数向指定窗口发送 WM_PASTE 消息，触发其粘贴操作
        /// <br/>2. 目标窗口必须是可接收文本输入的控件（如编辑框）
        /// <br/>3. 执行前通常需要先调用 SetClipboard 将文本放入剪贴板
        /// <br/>4. 此操作是发送消息，不保证目标窗口一定会执行粘贴
        /// </remarks>
        public int SendPaste(long hwnd){
            return OLAPlugDLLHelper.SendPaste(OLAObject, hwnd);
        }

        /// <summary>
        /// 根据进程ID和窗口类名或标题查找窗口
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="flag"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数结合进程ID和窗口属性进行精确查找
        /// <br/>2. 用于确保找到的是指定进程内的窗口，避免与其他进程的同名窗口混淆
        /// <br/>3. 如果 processId 为 0，则搜索所有进程
        /// <br/>4. className 和 titleName 支持部分匹配
        /// </remarks>
        public long GetWindow(long hwnd, int flag){
            return OLAPlugDLLHelper.GetWindow(OLAObject, hwnd, flag);
        }

        /// <summary>
        /// 获取指定窗口的标题文本
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口标题字符串的指针，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数获取窗口标题栏上显示的文本
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 对于没有标题栏的窗口，返回的可能是空字符串或窗口名称
        /// <br/>4. 如果窗口句柄无效或不可访问，函数将失败
        /// </remarks>
        public string GetWindowTitle(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowTitle(OLAObject, hwnd));
        }

        /// <summary>
        /// 获取指定窗口的类名
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口类名字符串的指针，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 窗口类名是在创建窗口时注册的，标识了窗口的基本类型和行为
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 例如，记事本主窗口的类名通常是 "Notepad"
        /// <br/>4. 类名对于窗口识别和自动化操作非常重要
        /// </remarks>
        public string GetWindowClass(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowClass(OLAObject, hwnd));
        }

        /// <summary>
        /// 获取指定窗口的矩形区域（相对于屏幕）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>成功获取返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数获取窗口的完整区域，包括标题栏和边框
        /// <br/>2. 坐标是相对于整个屏幕的绝对坐标
        /// <br/>3. 获取的矩形可用于窗口定位、截图或移动操作
        /// <br/>4. 如果 hwnd 无效，函数将失败
        /// </remarks>
        public int GetWindowRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetWindowRect(OLAObject, hwnd, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 获取指定窗口对应进程的可执行文件路径
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>进程路径字符串的指针，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数通过窗口句柄获取其所属进程的完整路径
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 路径格式如 "C:\\Windows\\notepad.exe"
        /// <br/>4. 在某些权限受限或系统保护的进程中，获取路径可能会失败
        /// </remarks>
        public string GetWindowProcessPath(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetWindowProcessPath(OLAObject, hwnd));
        }

        /// <summary>
        /// 获取指定窗口的当前状态
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="flag"></param>
        /// <returns>返回值含义如：0-关闭, 1-正常, 2-最小化, 3-最大化, 4-隐藏等</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数查询窗口的当前显示状态
        /// <br/>2. 返回的状态码可用于判断窗口是否被最小化或最大化
        /// <br/>3. 状态信息对于自动化脚本控制窗口行为非常有用
        /// <br/>4. 如果窗口句柄无效，返回值未定义
        /// </remarks>
        public int GetWindowState(long hwnd, int flag){
            return OLAPlugDLLHelper.GetWindowState(OLAObject, hwnd, flag);
        }

        /// <summary>
        /// 获取当前处于活动状态（最前端）的窗口句柄
        /// </summary>
        /// <returns>前台窗口的句柄，如果没有前台窗口则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回当前用户正在交互的窗口
        /// <br/>2. 此窗口通常位于所有其他窗口之上
        /// <br/>3. 获取的句柄可用于对前台窗口进行操作
        /// <br/>4. 在多显示器或特定系统设置下，前台窗口可能为空
        /// </remarks>
        public long GetForegroundWindow(){
            return OLAPlugDLLHelper.GetForegroundWindow(OLAObject);
        }

        /// <summary>
        /// 获取指定窗口所属进程的ID
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>进程ID，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 每个进程在系统中都有一个唯一的标识符（PID）
        /// <br/>2. 获取进程ID可用于进一步的进程管理操作，如终止进程
        /// <br/>3. 此函数是连接窗口管理和进程管理的桥梁
        /// <br/>4. 如果窗口属于系统进程或权限受限，获取PID可能会失败
        /// </remarks>
        public int GetWindowProcessId(long hwnd){
            return OLAPlugDLLHelper.GetWindowProcessId(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取指定窗口客户区的大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width">指向接收客户区宽度的变量</param>
        /// <param name="height">指向接收客户区高度的变量</param>
        /// <returns>成功获取返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 客户区是窗口中用于显示内容的区域，不包括标题栏、边框和滚动条
        /// <br/>2. 获取的尺寸常用于绘制操作或调整内部控件布局
        /// <br/>3. 坐标相对于窗口客户区的左上角(0,0)
        /// <br/>4. 此函数对于UI自动化和截图定位至关重要
        /// </remarks>
        public int GetClientSize(long hwnd, out int width, out int height){
            return OLAPlugDLLHelper.GetClientSize(OLAObject, hwnd, out width, out height);
        }

        /// <summary>
        /// 获取鼠标光标所在位置的窗口句柄
        /// </summary>
        /// <returns>鼠标光标下最顶层窗口的句柄，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回当前鼠标指针位置处的窗口句柄
        /// <br/>2. 返回的是包含鼠标光标的最顶层窗口，不一定是活动窗口
        /// <br/>3. 常用于实现“点击取色”或“窗口信息抓取”等功能
        /// <br/>4. 在鼠标位于桌面或无窗口区域时，行为可能未定义
        /// </remarks>
        public long GetMousePointWindow(){
            return OLAPlugDLLHelper.GetMousePointWindow(OLAObject);
        }

        /// <summary>
        /// 获取特殊系统窗口的句柄
        /// </summary>
        /// <param name="flag">特殊窗口的标识符</param>
        /// <returns>特定系统窗口的句柄，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于获取如桌面窗口、任务栏、开始按钮等系统级窗口的句柄
        /// <br/>2. flag 参数指定要获取的窗口类型，如 0-桌面, 1-任务栏等
        /// <br/>3. 这些窗口句柄可用于系统级的界面操作或信息获取
        /// <br/>4. 不同系统版本下，特殊窗口的句柄和行为可能有所不同
        /// </remarks>
        public long GetSpecialWindow(int flag){
            return OLAPlugDLLHelper.GetSpecialWindow(OLAObject, flag);
        }

        /// <summary>
        /// 获取指定窗口客户区的矩形区域
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>成功获取返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 GetWindowRect 不同，此函数获取的是客户区坐标
        /// <br/>2. 坐标相对于窗口客户区的左上角(0,0)
        /// <br/>3. 客户区矩形常用于在窗口内进行精确的元素定位
        /// <br/>4. 如果 hwnd 无效，函数将失败
        /// </remarks>
        public int GetClientRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            return OLAPlugDLLHelper.GetClientRect(OLAObject, hwnd, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 设置指定窗口的标题文本
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="title">要设置的新标题</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数会改变窗口标题栏上显示的文本
        /// <br/>2. 新标题会立即反映在UI上
        /// <br/>3. 并非所有窗口都允许修改标题，某些系统窗口或受保护的应用可能忽略此操作
        /// <br/>4. 修改标题可能会影响基于标题的窗口查找逻辑
        /// </remarks>
        public int SetWindowText(long hwnd, string title){
            return OLAPlugDLLHelper.SetWindowText(OLAObject, hwnd, title);
        }

        /// <summary>
        /// 设置指定窗口的大小和位置
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数可以同时改变窗口的位置和大小
        /// <br/>2. 坐标是相对于屏幕的绝对坐标
        /// <br/>3. flags 参数可控制是否重绘窗口、是否发送消息等
        /// <br/>4. 此操作相当于直接调用 Windows API 的 MoveWindow
        /// </remarks>
        public int SetWindowSize(long hwnd, int width, int height){
            return OLAPlugDLLHelper.SetWindowSize(OLAObject, hwnd, width, height);
        }

        /// <summary>
        /// 设置指定窗口客户区的大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width">客户区的新宽度</param>
        /// <param name="height">客户区的新高度</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 SetWindowSize 不同，此函数设置的是客户区尺寸
        /// <br/>2. 系统会根据客户区大小自动调整窗口的整体大小以包含边框和标题栏
        /// <br/>3. 常用于确保窗口内容区域达到指定尺寸
        /// <br/>4. 设置后，窗口的总体尺寸会大于或等于指定的客户区尺寸
        /// </remarks>
        public int SetClientSize(long hwnd, int width, int height){
            return OLAPlugDLLHelper.SetClientSize(OLAObject, hwnd, width, height);
        }

        /// <summary>
        /// 设置窗口的透明度
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="alpha">透明度值，范围 0-255，0为完全透明，255为完全不透明</param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数通过设置窗口的分层属性来实现透明效果
        /// <br/>2. 窗口必须支持分层属性（WS_EX_LAYERED）才能设置透明度
        /// <br/>3. 透明度影响整个窗口，包括标题栏和边框
        /// <br/>4. 此功能常用于制作半透明界面或浮动工具窗口
        /// </remarks>
        public int SetWindowTransparent(long hwnd, int alpha){
            return OLAPlugDLLHelper.SetWindowTransparent(OLAObject, hwnd, alpha);
        }

        /// <summary>
        /// 在父窗口内查找子窗口
        /// </summary>
        /// <param name="parent">父窗口句柄，为 0 时查找所有顶层窗口</param>
        /// <param name="class_name">要查找的子窗口类名</param>
        /// <param name="title">要查找的子窗口标题</param>
        /// <returns>找到的子窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于枚举和查找特定的子窗口
        /// <br/>2. hwndChildAfter 用于从指定位置开始查找，为 NULL 时查找第一个匹配窗口
        /// <br/>3. 支持类名和标题的模糊匹配
        /// <br/>4. 是实现复杂UI自动化（如操作对话框中的按钮）的关键函数
        /// </remarks>
        public long FindWindowEx(long parent, string class_name, string title){
            return OLAPlugDLLHelper.FindWindowEx(OLAObject, parent, class_name, title);
        }

        /// <summary>
        /// 根据进程名称查找其创建的窗口
        /// </summary>
        /// <param name="process_name"></param>
        /// <param name="class_name"></param>
        /// <param name="title"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数先通过进程名找到进程ID，再查找属于该进程的窗口
        /// <br/>2. 用于确保找到的是特定应用程序的窗口
        /// <br/>3. processName 是文件名，不是完整路径
        /// <br/>4. 常用于启动程序后自动获取其主窗口句柄
        /// </remarks>
        public long FindWindowByProcess(string process_name, string class_name, string title){
            return OLAPlugDLLHelper.FindWindowByProcess(OLAObject, process_name, class_name, title);
        }

        /// <summary>
        /// 移动指定窗口到新的位置
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">窗口左上角的新x坐标</param>
        /// <param name="y">窗口左上角的新y坐标</param>
        /// <returns>成功移动返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数只改变窗口的位置，不改变其大小
        /// <br/>2. 坐标是相对于屏幕的绝对坐标
        /// <br/>3. 移动操作会触发窗口的 WM_WINDOWPOSCHANGING/CHANGED 消息
        /// <br/>4. 此函数是 SetWindowSize 的一个特例（只改变位置）
        /// </remarks>
        public int MoveWindow(long hwnd, int x, int y){
            return OLAPlugDLLHelper.MoveWindow(OLAObject, hwnd, x, y);
        }

        /// <summary>
        /// 获取Windows系统的DPI缩放比例
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns>DPI缩放比例，例如 1.0, 1.25, 1.5, 2.0 等</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数查询系统当前的显示缩放设置
        /// <br/>2. 在高DPI显示器上，此值通常大于 1.0
        /// <br/>3. 获取的缩放比例对于正确计算屏幕坐标和尺寸至关重要
        /// <br/>4. 避免在高DPI屏幕上出现界面模糊或定位不准的问题
        /// </remarks>
        public double GetScaleFromWindows(long hwnd){
            return OLAPlugDLLHelper.GetScaleFromWindows(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取指定窗口的DPI感知缩放比例
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>窗口的DPI缩放比例</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 GetScaleFromWindows 不同，此函数获取的是特定窗口的感知缩放
        /// <br/>2. 不同窗口可能具有不同的DPI感知模式（如未感知、系统感知、每监视器感知）
        /// <br/>3. 返回的比例更精确地反映了该窗口在当前显示环境下的实际缩放
        /// <br/>4. 对于需要高精度坐标的自动化操作，应使用此函数获取的比例
        /// </remarks>
        public double GetWindowDpiAwarenessScale(long hwnd){
            return OLAPlugDLLHelper.GetWindowDpiAwarenessScale(OLAObject, hwnd);
        }

        /// <summary>
        /// 枚举系统中所有正在运行的进程
        /// </summary>
        /// <param name="name"></param>
        /// <returns>包含进程ID、名称、路径等信息</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数扫描系统并返回所有活动进程的列表
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 可用于进程监控、查找特定程序或终止恶意进程
        /// </remarks>
        public string EnumProcess(string name){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumProcess(OLAObject, name));
        }

        /// <summary>
        /// 枚举指定父窗口下的所有子窗口
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="title"></param>
        /// <param name="className"></param>
        /// <param name="filter"></param>
        /// <returns>例如：[123456, 234567, 345678]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数递归或非递归地遍历窗口层次结构
        /// <br/>2. 返回的数组包含所有匹配的子窗口句柄
        /// <br/>3. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>4. 是实现窗口树分析和批量操作的基础
        /// </remarks>
        public string EnumWindow(long parent, string title, string className, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindow(OLAObject, parent, title, className, filter));
        }

        /// <summary>
        /// 根据进程名称枚举其创建的所有窗口
        /// </summary>
        /// <param name="process_name"></param>
        /// <param name="title"></param>
        /// <param name="class_name"></param>
        /// <param name="filter"></param>
        /// <returns>例如：[123456, 234567]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数结合进程枚举和窗口枚举，找出特定应用程序的所有窗口
        /// <br/>2. 返回的数组可用于对应用程序的多个窗口进行统一操作
        /// <br/>3. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>4. 常用于关闭程序的所有窗口或查找特定功能的子窗口
        /// </remarks>
        public string EnumWindowByProcess(string process_name, string title, string class_name, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowByProcess(OLAObject, process_name, title, class_name, filter));
        }

        /// <summary>
        /// 根据进程ID枚举其创建的所有窗口
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="title"></param>
        /// <param name="class_name"></param>
        /// <param name="filter"></param>
        /// <returns>例如：[123456, 234567]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 EnumWindowByProcess 类似，但使用进程ID作为参数
        /// <br/>2. 进程ID是唯一的，因此查找更精确
        /// <br/>3. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>4. 在已知进程ID的场景下（如通过 CreateChildProcess 获得），此函数更高效
        /// </remarks>
        public string EnumWindowByProcessId(long pid, string title, string class_name, int filter){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowByProcessId(OLAObject, pid, title, class_name, filter));
        }

        /// <summary>
        /// 高级窗口查找，支持多种条件和模糊匹配
        /// </summary>
        /// <param name="spec1"></param>
        /// <param name="flag1"></param>
        /// <param name="type1"></param>
        /// <param name="spec2"></param>
        /// <param name="flag2"></param>
        /// <param name="type2"></param>
        /// <param name="sort"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数是 FindWindow 和 FindWindowEx 的增强版
        /// <br/>2. 支持正则表达式、类名/标题的包含关系、属性匹配等多种查找方式
        /// <br/>3. flag 参数决定了 s1 和 s2 的具体含义和匹配逻辑
        /// <br/>4. 是实现复杂窗口定位策略的强力工具
        /// </remarks>
        public string EnumWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2, int sort){
            return PtrToStringUTF8(OLAPlugDLLHelper.EnumWindowSuper(OLAObject, spec1, flag1, type1, spec2, flag2, type2, sort));
        }

        /// <summary>
        /// 获取指定屏幕坐标点下的窗口句柄
        /// </summary>
        /// <param name="x">屏幕坐标x</param>
        /// <param name="y">屏幕坐标y</param>
        /// <returns>该坐标点下最顶层窗口的句柄，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回覆盖指定屏幕坐标的窗口
        /// <br/>2. 与 GetMousePointWindow 类似，但可以指定任意坐标点
        /// <br/>3. 常用于基于坐标的UI自动化或信息查询
        /// <br/>4. 如果坐标点位于多个窗口重叠区域，返回最顶层的窗口
        /// </remarks>
        public long GetPointWindow(int x, int y){
            return OLAPlugDLLHelper.GetPointWindow(OLAObject, x, y);
        }

        /// <summary>
        /// 获取指定进程的详细信息
        /// </summary>
        /// <param name="pid"></param>
        /// <returns>例如：{"pid": 1234, "name": "notepad.exe", "path": "C:\\...", "cmdline": "...", "parent":1111}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数查询指定进程的完整信息，包括命令行、父进程ID等
        /// <br/>2. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>3. 是进程分析和安全审计的重要工具
        /// <br/>4. 某些系统进程或权限不足时，部分信息可能无法获取
        /// </remarks>
        public string GetProcessInfo(long pid){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetProcessInfo(OLAObject, pid));
        }

        /// <summary>
        /// 显示或隐藏系统任务栏上的程序图标
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="show">1-显示图标, 0-隐藏图标</param>
        /// <returns>成功执行返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于控制应用程序在任务栏上的可见性
        /// <br/>2. 常用于创建后台服务程序或系统托盘应用
        /// <br/>3. 隐藏图标后，用户可能难以通过常规方式关闭程序
        /// <br/>4. 操作可能需要特定的窗口样式（如 WS_SYSMENU）
        /// </remarks>
        public int ShowTaskBarIcon(long hwnd, int show){
            return OLAPlugDLLHelper.ShowTaskBarIcon(OLAObject, hwnd, show);
        }

        /// <summary>
        /// 根据进程ID查找其创建的窗口
        /// </summary>
        /// <param name="process_id"></param>
        /// <param name="className">窗口类名，可为 NULL</param>
        /// <param name="title"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数直接通过进程ID查找窗口，效率较高
        /// <br/>2. 用于在已知进程ID的情况下快速定位其主窗口或子窗口
        /// <br/>3. className 和 titleName 支持部分匹配
        /// <br/>4. 是进程与窗口关联操作的常用接口
        /// </remarks>
        public long FindWindowByProcessId(long process_id, string className, string title){
            return OLAPlugDLLHelper.FindWindowByProcessId(OLAObject, process_id, className, title);
        }

        /// <summary>
        /// 获取指定窗口所属线程的ID
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>线程ID，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 每个窗口由一个特定的线程创建和管理
        /// <br/>2. 线程ID可用于线程级别的操作或调试
        /// <br/>3. 了解窗口的创建线程有助于分析程序结构和消息循环
        /// <br/>4. 某些系统窗口的线程ID可能无法获取
        /// </remarks>
        public long GetWindowThreadId(long hwnd){
            return OLAPlugDLLHelper.GetWindowThreadId(OLAObject, hwnd);
        }

        /// <summary>
        /// 高级窗口查找，功能与 EnumWindowSuper 类似
        /// </summary>
        /// <param name="spec1"></param>
        /// <param name="flag1"></param>
        /// <param name="type1"></param>
        /// <param name="spec2"></param>
        /// <param name="flag2"></param>
        /// <param name="type2"></param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数提供比标准 FindWindow 更强大的查找能力
        /// <br/>2. 支持复杂的匹配条件和多种查找策略
        /// <br/>3. 具体行为由 flag 参数控制
        /// <br/>4. 可用于实现自定义的窗口定位逻辑
        /// </remarks>
        public long FindWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2){
            return OLAPlugDLLHelper.FindWindowSuper(OLAObject, spec1, flag1, type1, spec2, flag2, type2);
        }

        /// <summary>
        /// 将客户区坐标转换为屏幕绝对坐标
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">指向客户区x坐标的变量，转换后存储屏幕x坐标</param>
        /// <param name="y">指向客户区y坐标的变量，转换后存储屏幕y坐标</param>
        /// <returns>成功转换返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于坐标系转换，将相对于窗口客户区的坐标转为全局屏幕坐标
        /// <br/>2. 常用于将鼠标点击位置或控件位置映射到屏幕
        /// <br/>3. 转换考虑了窗口的位置、DPI缩放和多显示器布局
        /// <br/>4. 是实现精确UI自动化的基础
        /// </remarks>
        public int ClientToScreen(long hwnd, out int x, out int y){
            return OLAPlugDLLHelper.ClientToScreen(OLAObject, hwnd, out x, out y);
        }

        /// <summary>
        /// 将屏幕绝对坐标转换为客户区坐标
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">指向屏幕x坐标的变量，转换后存储客户区x坐标</param>
        /// <param name="y">指向屏幕y坐标的变量，转换后存储客户区y坐标</param>
        /// <returns>成功转换返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 ClientToScreen 相反，将全局屏幕坐标转为相对于指定窗口客户区的坐标
        /// <br/>2. 常用于判断屏幕上的某个点是否在窗口客户区内
        /// <br/>3. 转换同样考虑了窗口位置、DPI和显示器设置
        /// <br/>4. 对于坐标计算和事件处理非常有用
        /// </remarks>
        public int ScreenToClient(long hwnd, out int x, out int y){
            return OLAPlugDLLHelper.ScreenToClient(OLAObject, hwnd, out x, out y);
        }

        /// <summary>
        /// 获取当前前台窗口中具有输入焦点的控件句柄
        /// </summary>
        /// <returns>具有焦点的控件句柄，如果失败则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回当前活动窗口中正在接收键盘输入的子窗口（如编辑框）
        /// <br/>2. 对于实现自动化输入操作（如 SendPaste）非常关键
        /// <br/>3. 只有可接收输入的控件（如文本框）才会获得焦点
        /// <br/>4. 如果前台窗口没有焦点控件或为桌面，返回值可能为 0
        /// </remarks>
        public long GetForegroundFocus(){
            return OLAPlugDLLHelper.GetForegroundFocus(OLAObject);
        }

        /// <summary>
        /// 设置窗口的显示状态（可见性）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="affinity"></param>
        /// <returns>成功设置返回 1，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数直接控制窗口的可见性，类似于 SetWindowState(SW_SHOW/SW_HIDE)
        /// <br/>2. 隐藏窗口后，它将从屏幕上消失，但仍在进程中运行
        /// <br/>3. 显示隐藏的窗口可以使其重新出现
        /// <br/>4. 操作不会改变窗口的最小化或最大化状态
        /// </remarks>
        public int SetWindowDisplay(long hwnd, int affinity){
            return OLAPlugDLLHelper.SetWindowDisplay(OLAObject, hwnd, affinity);
        }

        /// <summary>
        /// 检查指定窗口是否处于“假死”状态
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="time"></param>
        /// <returns>1-正常, 0-假死, -1-错误</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 通过向窗口发送消息并等待响应来判断其是否无响应
        /// <br/>2. 假死窗口通常无法处理用户输入或更新界面
        /// <br/>3. 可用于监控应用程序健康状况或自动重启无响应程序
        /// <br/>4. 检测需要一定时间，且可能受系统负载影响
        /// </remarks>
        public int IsDisplayDead(int x1, int y1, int x2, int y2, int time){
            return OLAPlugDLLHelper.IsDisplayDead(OLAObject, x1, y1, x2, y2, time);
        }

        /// <summary>
        /// 获取指定窗口的刷新帧率（FPS）
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>窗口的近似帧率，如 60, 30, 0（静态）等</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 通过监控窗口区域的变化频率来估算FPS
        /// <br/>2. 常用于游戏或视频应用的性能监控
        /// <br/>3. 对于静态窗口，返回值通常为 0 或很低的数值
        /// <br/>4. 估算值可能存在一定误差
        /// </remarks>
        public int GetWindowsFps(int x1, int y1, int x2, int y2){
            return OLAPlugDLLHelper.GetWindowsFps(OLAObject, x1, y1, x2, y2);
        }

        /// <summary>
        /// 终止进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>成功返回1,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数强制结束指定ID的进程
        /// <br/>2. 终止后，进程及其所有资源将被系统回收
        /// <br/>3. 未保存的数据将会丢失
        /// <br/>4. 需要足够的权限才能终止某些系统或受保护的进程
        /// </remarks>
        public int TerminateProcess(long pid){
            return OLAPlugDLLHelper.TerminateProcess(OLAObject, pid);
        }

        /// <summary>
        /// 终止进程树
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>成功返回1,失败返回0@title 终止进程树 - TerminateProcessTree</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数不仅终止指定进程，还递归终止其创建的所有子进程
        /// <br/>2. 用于彻底清理一个程序及其后台服务
        /// <br/>3. 操作非常强力，可能导致相关联的多个程序关闭
        /// <br/>4. 需要谨慎使用，避免误杀重要系统进程
        /// </remarks>
        public int TerminateProcessTree(long pid){
            return OLAPlugDLLHelper.TerminateProcessTree(OLAObject, pid);
        }

        /// <summary>
        /// 获取窗口命令行
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>命令行(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回创建进程时使用的完整命令行参数
        /// <br/>2. 包含可执行文件路径和所有传递的参数
        /// <br/>3. 返回的字符串指针需要调用 FreeStringPtr 接口释放内存
        /// <br/>4. 对于分析程序启动配置或调试非常有用
        /// </remarks>
        public string GetCommandLine(long hwnd){
            return PtrToStringUTF8(OLAPlugDLLHelper.GetCommandLine(OLAObject, hwnd));
        }

        /// <summary>
        /// 检查字体平滑
        /// </summary>
        /// <returns>成功返回1,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 字体平滑可使屏幕上的文字边缘更平滑，提高可读性
        /// <br/>2. 此设置影响所有应用程序的文本渲染
        /// <br/>3. 检查结果可用于调整自动化脚本的截图或OCR策略
        /// <br/>4. 在某些低分辨率或远程桌面场景下，此功能可能被关闭
        /// </remarks>
        public int CheckFontSmooth(){
            return OLAPlugDLLHelper.CheckFontSmooth(OLAObject);
        }

        /// <summary>
        /// 设置字体平滑
        /// </summary>
        /// <param name="enable">是否启用</param>
        /// <returns>成功返回1,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数修改系统的全局字体渲染设置
        /// <br/>2. 更改后，新创建的窗口将使用新的设置
        /// <br/>3. 可能需要重启应用程序甚至系统才能完全生效
        /// <br/>4. 滥用此功能可能影响用户体验，应谨慎使用
        /// </remarks>
        public int SetFontSmooth(int enable){
            return OLAPlugDLLHelper.SetFontSmooth(OLAObject, enable);
        }

        /// <summary>
        /// 启用调试权限
        /// </summary>
        /// <returns>成功返回1,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 调试权限（SeDebugPrivilege）允许进程调试或操作其他进程
        /// <br/>2. 此权限对于调用 TerminateProcess, EnumProcess 等函数通常是必需的
        /// <br/>3. 通常需要管理员权限才能成功启用
        /// <br/>4. 在进程启动后尽早调用此函数以确保后续操作的权限
        /// </remarks>
        public int EnableDebugPrivilege(){
            return OLAPlugDLLHelper.EnableDebugPrivilege(OLAObject);
        }

        /// <summary>
        /// 系统启动
        /// </summary>
        /// <param name="applicationName">应用程序名称</param>
        /// <param name="commandLine">命令行</param>
        /// <returns>成功返回子进程ID,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. flag 参数指定具体操作，如 0-关机, 1-重启, 2-注销, 3-睡眠等
        /// <br/>2. 执行此操作需要相应的系统权限（通常为管理员）
        /// <br/>3. 操作是不可逆的，执行前应提示用户保存数据
        /// <br/>4. 常用于系统维护脚本或远程管理工具
        /// </remarks>
        public int SystemStart(string applicationName, string commandLine){
            return OLAPlugDLLHelper.SystemStart(OLAObject, applicationName, commandLine);
        }

        /// <summary>
        /// 创建子进程
        /// </summary>
        /// <param name="applicationName">进程路径，如C:\windows\system32\notepad.exe</param>
        /// <param name="commandLine">命令行参数一定要包含进程路径，如aa bb cc</param>
        /// <param name="currentDirectory">启动目录, 可空</param>
        /// <param name="showType">显示方式，如果省略本参数,默认为“普通激活”方式.
        ///<br/> 1: 隐藏窗口
        ///<br/> 2: 普通激活
        ///<br/> 3: 最小化激活
        ///<br/> 4: 最大化激活
        ///<br/> 5: 普通不激活
        ///<br/> 6: 最小化不激活
        /// </param>
        /// <param name="parentProcessId">父进程ID, 整数型,支持系统进程的ID，只要是调试权限能Open的进程，如service.exe、csrss.exe、explorer.exe</param>
        /// <returns>成功返回子进程ID,失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数启动一个新的可执行程序作为当前进程的子进程
        /// <br/>2. cmdLine 包含可执行文件路径和参数
        /// <br/>3. showCmd 控制新进程主窗口的初始显示状态
        /// <br/>4. 成功时，新进程的ID通过 processId 参数返回
        /// </remarks>
        public int CreateChildProcess(string applicationName, string commandLine, string currentDirectory, int showType, int parentProcessId){
            return OLAPlugDLLHelper.CreateChildProcess(OLAObject, applicationName, commandLine, currentDirectory, showType, parentProcessId);
        }

        /// <summary>
        /// 加载模型
        /// </summary>
        /// <param name="modelPath">模型路径</param>
        /// <param name="outputPath"></param>
        /// <param name="names_label"></param>
        /// <param name="password">密码（可选，传NULL表示无密码）</param>
        /// <param name="modelType">模型类型0.TensorRT 1.ONNX(保留未开放) 2.pt(保留未开放)</param>
        /// <param name="inferenceType">推理类型0.Detect物体检测 1.Classify图像分类 2.Segment实例分割 3.Pose姿态估计 4.Obb旋转框检测5.KeyPoint关键点检测 6.Text文字识别 7.OCR文字识别 8.车牌识别 9.人脸识别 10.手势识别11.动作识别 12.行为识别 13.运动识别 14.轨迹识别 15.轨迹预测 16.轨迹跟踪note: 5-16未开放服务</param>
        /// <param name="inferenceDevice">推理设备0.GPU0 1.GPU1 2.GPU2 3.GPU3 以此类推，默认使用GPU0若无GPU设备，则无法使用，CPU版本后续推出</param>
        /// <returns>模型句柄（失败返回0）</returns>
        public long YoloLoadModel(string modelPath, string outputPath, string names_label, string password, int modelType, int inferenceType, int inferenceDevice){
            return OLAPlugDLLHelper.YoloLoadModel(OLAObject, modelPath, outputPath, names_label, password, modelType, inferenceType, inferenceDevice);
        }

        /// <summary>
        /// 释放模型
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>释放结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 0 失败, 1 成功
        /// </remarks>
        public int YoloReleaseModel(long modelHandle){
            return OLAPlugDLLHelper.YoloReleaseModel(OLAObject, modelHandle);
        }

        /// <summary>
        /// 从内存加载模型
        /// </summary>
        /// <param name="memoryAddr">内存地址</param>
        /// <param name="size">内存大小</param>
        /// <param name="modelType">模型类型0.TensorRT 1.ONNX(保留未开放) 2.pt(保留未开放)</param>
        /// <param name="inferenceType">推理类型0.Detect物体检测 1.Classify图像分类 2.Segment实例分割 3.Pose姿态估计 4.Obb旋转框检测5.KeyPoint关键点检测 6.Text文字识别 7.OCR文字识别 8.车牌识别 9.人脸识别 10.手势识别11.动作识别 12.行为识别 13.运动识别 14.轨迹识别 15.轨迹预测 16.轨迹跟踪note: 5-16未开放服务</param>
        /// <param name="inferenceDevice">推理设备0.GPU0 1.GPU1 2.GPU2 3.GPU3 以此类推，默认使用GPU0若无GPU设备，则无法使用，CPU版本后续推出</param>
        /// <returns>模型句柄（失败返回0）</returns>
        public long YoloLoadModelMemory(long memoryAddr, int size, int modelType, int inferenceType, int inferenceDevice){
            return OLAPlugDLLHelper.YoloLoadModelMemory(OLAObject, memoryAddr, size, modelType, inferenceType, inferenceDevice);
        }

        /// <summary>
        /// 推理
        /// </summary>
        /// <param name="handle">模型句柄</param>
        /// <param name="imagePtr">图像指针</param>
        /// <returns>JSON格式推理结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloInfer(long handle, long imagePtr){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloInfer(OLAObject, handle, imagePtr));
        }

        /// <summary>
        /// 检查模型是否有效
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>1 有效, 0 无效</returns>
        public int YoloIsModelValid(long modelHandle){
            return OLAPlugDLLHelper.YoloIsModelValid(OLAObject, modelHandle);
        }

        /// <summary>
        /// 列出所有已加载的模型
        /// </summary>
        /// <returns>JSON格式的模型列表</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"handle": 123, "type": 5, "inferenceType": 0, "device": 1}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloListModels(){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloListModels(OLAObject));
        }

        /// <summary>
        /// 获取模型信息
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>JSON格式的模型信息</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: {"handle": 123, "type": 5, "inferenceType": 0, "device": 1, "inputShape": [640,640], "classes": [...]}
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloGetModelInfo(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetModelInfo(OLAObject, modelHandle));
        }

        /// <summary>
        /// 设置模型配置参数
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="configJson">配置JSON</param>
        /// <returns>1 成功, 0 失败</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 配置格式: {"confidence": 0.5, "iou": 0.45, "maxDetections": 100, "classes": ["person","car"], "inputSize": [640, 640]}
        /// </remarks>
        public int YoloSetModelConfig(long modelHandle, string configJson){
            return OLAPlugDLLHelper.YoloSetModelConfig(OLAObject, modelHandle, configJson);
        }

        /// <summary>
        /// 获取模型配置参数
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>JSON格式的配置信息</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloGetModelConfig(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetModelConfig(OLAObject, modelHandle));
        }

        /// <summary>
        /// 模型预热
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="iterations">预热迭代次数</param>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloWarmup(long modelHandle, int iterations){
            return OLAPlugDLLHelper.YoloWarmup(OLAObject, modelHandle, iterations);
        }

        /// <summary>
        /// 物体检测（完整参数）
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="classes">检测类别JSON数组，如：["person", "car", "bus"]，传NULL表示检测所有类别</param>
        /// <param name="confidence">置信度阈值 (0.0-1.0)</param>
        /// <param name="iou">NMS交并比阈值 (0.0-1.0)</param>
        /// <param name="maxDetections">最大检测数量</param>
        /// <returns>JSON格式检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"class": "person", "confidence": 0.95, "bbox": [x1, y1, x2, y2]}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetect(long modelHandle, int x1, int y1, int x2, int y2, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetect(OLAObject, modelHandle, x1, y1, x2, y2, classes, confidence, iou, maxDetections));
        }

        /// <summary>
        /// 物体检测（简化版，使用默认参数）
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <returns>JSON格式检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 使用默认参数：confidence=0.5, iou=0.45, maxDetections=100, classes=all
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetectSimple(long modelHandle, int x1, int y1, int x2, int y2){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectSimple(OLAObject, modelHandle, x1, y1, x2, y2));
        }

        /// <summary>
        /// 从图像指针检测物体
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="classes">检测类别JSON数组，传NULL表示检测所有类别</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <param name="maxDetections">最大检测数量</param>
        /// <returns>JSON格式检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetectFromPtr(long modelHandle, long imagePtr, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromPtr(OLAObject, modelHandle, imagePtr, classes, confidence, iou, maxDetections));
        }

        /// <summary>
        /// 从文件路径检测物体
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePath">图像文件路径</param>
        /// <param name="classes">检测类别JSON数组，传NULL表示检测所有类别</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <param name="maxDetections">最大检测数量</param>
        /// <returns>JSON格式检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetectFromFile(long modelHandle, string imagePath, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromFile(OLAObject, modelHandle, imagePath, classes, confidence, iou, maxDetections));
        }

        /// <summary>
        /// 从Base64编码检测物体
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="base64Data">Base64编码的图像数据</param>
        /// <param name="classes">检测类别JSON数组，传NULL表示检测所有类别</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <param name="maxDetections">最大检测数量</param>
        /// <returns>JSON格式检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetectFromBase64(long modelHandle, string base64Data, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectFromBase64(OLAObject, modelHandle, base64Data, classes, confidence, iou, maxDetections));
        }

        /// <summary>
        /// 批量检测物体
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagesJson">图像列表JSON</param>
        /// <param name="classes">检测类别JSON数组，传NULL表示检测所有类别</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <param name="maxDetections">最大检测数量</param>
        /// <returns>JSON格式批量检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 格式: [{"type": "file", "path": "a.jpg"}, {"type": "base64", "data": "..."}, {"type":"region", "x1": 0, "y1": 0, "x2": 100, "y2": 100}]
        /// <br/>2. 返回格式: [{"index": 0, "results": [...]}, {"index": 1, "results": [...]}, ...]
        /// <br/>3. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloDetectBatch(long modelHandle, string imagesJson, string classes, double confidence, double iou, int maxDetections){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloDetectBatch(OLAObject, modelHandle, imagesJson, classes, confidence, iou, maxDetections));
        }

        /// <summary>
        /// 图像分类
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="topK">返回前K个结果</param>
        /// <returns>JSON格式分类结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"class": "cat", "confidence": 0.95}, {"class": "dog", "confidence": 0.03}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloClassify(long modelHandle, int x1, int y1, int x2, int y2, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassify(OLAObject, modelHandle, x1, y1, x2, y2, topK));
        }

        /// <summary>
        /// 从图像指针分类
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="topK">返回前K个结果</param>
        /// <returns>JSON格式分类结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloClassifyFromPtr(long modelHandle, long imagePtr, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassifyFromPtr(OLAObject, modelHandle, imagePtr, topK));
        }

        /// <summary>
        /// 从文件路径分类
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePath">图像文件路径</param>
        /// <param name="topK">返回前K个结果</param>
        /// <returns>JSON格式分类结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloClassifyFromFile(long modelHandle, string imagePath, int topK){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloClassifyFromFile(OLAObject, modelHandle, imagePath, topK));
        }

        /// <summary>
        /// 实例分割
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式分割结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"class": "person", "confidence": 0.95, "bbox": [x1, y1, x2, y2], "mask": [[x,y], ...]}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloSegment(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloSegment(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        /// <summary>
        /// 从图像指针分割
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式分割结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloSegmentFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloSegmentFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        /// <summary>
        /// 姿态估计
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式姿态估计结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"bbox": [x1, y1, x2, y2], "keypoints": [[x, y, conf], ...], "confidence":0.95}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloPose(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloPose(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        /// <summary>
        /// 从图像指针估计姿态
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式姿态估计结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloPoseFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloPoseFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        /// <summary>
        /// 旋转框检测
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式旋转框检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"class": "ship", "confidence": 0.95, "obb": [cx, cy, w, h, angle]}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloObb(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloObb(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        /// <summary>
        /// 从图像指针检测旋转框
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式旋转框检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloObbFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloObbFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        /// <summary>
        /// 关键点检测
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="x1">左上角x坐标</param>
        /// <param name="y1">左上角y坐标</param>
        /// <param name="x2">右下角x坐标</param>
        /// <param name="y2">右下角y坐标</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式关键点检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: [{"bbox": [x1, y1, x2, y2], "keypoints": [[x, y, conf], ...], "confidence":0.95}, ...]
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloKeyPoint(long modelHandle, int x1, int y1, int x2, int y2, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloKeyPoint(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
        }

        /// <summary>
        /// 从图像指针检测关键点
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="imagePtr">图像指针（OpenCV Mat指针）</param>
        /// <param name="confidence">置信度阈值</param>
        /// <param name="iou">NMS交并比阈值</param>
        /// <returns>JSON格式关键点检测结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloKeyPointFromPtr(long modelHandle, long imagePtr, double confidence, double iou){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloKeyPointFromPtr(OLAObject, modelHandle, imagePtr, confidence, iou));
        }

        /// <summary>
        /// 获取推理统计信息
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>JSON格式统计信息</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回格式: {"totalInferences": 100, "avgTime": 25.5, "minTime": 20.1, "maxTime": 35.2,"fps": 39.2}
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloGetInferenceStats(long modelHandle){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetInferenceStats(OLAObject, modelHandle));
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloResetStats(long modelHandle){
            return OLAPlugDLLHelper.YoloResetStats(OLAObject, modelHandle);
        }

        /// <summary>
        /// 获取最后一次错误信息
        /// </summary>
        /// <returns>错误信息字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloGetLastError(){
            return PtrToStringUTF8(OLAPlugDLLHelper.YoloGetLastError(OLAObject));
        }

        /// <summary>
        /// 清除错误信息
        /// </summary>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloClearError(){
            return OLAPlugDLLHelper.YoloClearError(OLAObject);
        }


    }
}
