using Newtonsoft.Json;
using System.Collections.Concurrent;
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

        public OLAPlugServer(string dllPath="olaplug/OLAPlug_x64.dll")
        {
            OLAPlugDLLHelper.Initialize(dllPath);
            OLAObject = CreateCOLAPlugInterFace();
        }

        public string PtrToStringUTF8(long ptr)
        {
            if (ptr==0)
                return "";
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
            while (Read(stmt) == 1)
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
            var func = OLAPlugDLLHelper.GetFunction<CreateCOLAPlugInterFaceDelegate>("CreateCOLAPlugInterFace");
            return func();
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
            var func = OLAPlugDLLHelper.GetFunction<DestroyCOLAPlugInterFaceDelegate>("DestroyCOLAPlugInterFace");
            return func(OLAObject);
        }

        /// <summary>
        /// 返回当前插件版本号。
        /// </summary>
        /// <returns>当前插件的版本描述字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string Ver(){
            var func = OLAPlugDLLHelper.GetFunction<VerDelegate>("Ver");
            return PtrToStringUTF8(func());
        }

        /// <summary>
        /// 获取插件信息
        /// </summary>
        /// <param name="type">信息类型
        ///<br/> 1: 精简版信息
        ///<br/> 2: 完整版信息
        /// </param>
        /// <returns>插件信息</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetPlugInfo(int type){
            var func = OLAPlugDLLHelper.GetFunction<GetPlugInfoDelegate>("GetPlugInfo");
            return PtrToStringUTF8(func(type));
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
            var func = OLAPlugDLLHelper.GetFunction<SetPathDelegate>("SetPath");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 获取全局路径。(可用于调试) 建议使用 GetConfig 接口。
        /// </summary>
        /// <returns>以字符串的形式返回当前设置的全局路径</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetPath(){
            var func = OLAPlugDLLHelper.GetFunction<GetPathDelegate>("GetPath");
            return PtrToStringUTF8(func(OLAObject));
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
            var func = OLAPlugDLLHelper.GetFunction<GetMachineCodeDelegate>("GetMachineCode");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 获取注册在系统中的OLAPlug.dll的路径。
        /// </summary>
        /// <returns>返回OLAPlug.dll所在路径。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetBasePath(){
            var func = OLAPlugDLLHelper.GetFunction<GetBasePathDelegate>("GetBasePath");
            return PtrToStringUTF8(func(OLAObject));
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
            var func = OLAPlugDLLHelper.GetFunction<RegDelegate>("Reg");
            return func(userCode, softCode, featureList);
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
        ///<br/> vmware: 虚拟机模式（需要管理员权限）
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
        ///<br/> dx.mouse.vmware: 虚拟机鼠标穿透模式,目前只支持vm16,仅限高级版使用
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
        ///<br/> dx.keypad.vmware: 虚拟机键盘穿透模式,目前只支持vm16,仅限高级版使用
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
            var func = OLAPlugDLLHelper.GetFunction<BindWindowDelegate>("BindWindow");
            return func(OLAObject, hwnd, display, mouse, keypad, mode);
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
        ///<br/> vmware: 虚拟机模式（需要管理员权限）
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
        ///<br/> dx.mouse.vmware: 虚拟机鼠标穿透模式,目前只支持vm16,仅限高级版使用
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
        ///<br/> dx.keypad.vmware: 虚拟机键盘穿透模式,目前只支持vm16,仅限高级版使用
        /// </param>
        /// <param name="pubstr">通用绑定模式（暂未启用）
        ///<br/> dx.public.graphic.revert: 翻转DX截图的图像结果
        ///<br/> dx.public.active.api: 自动定时发送激活命令
        ///<br/> dx.public.active.api2: 自动定时发送激活命令2
        ///<br/> ola.bypass.guard: 绑定失败的时候可以尝试打开
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
        public int BindWindowEx(long hwnd, string display, string mouse, string keypad, string pubstr, int mode){
            var func = OLAPlugDLLHelper.GetFunction<BindWindowExDelegate>("BindWindowEx");
            return func(OLAObject, hwnd, display, mouse, keypad, pubstr, mode);
        }

        /// <summary>
        /// 解绑窗口，取消之前通过 BindWindow 或 BindWindowEx 绑定的窗口。
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 解绑失败
        ///<br/>1: 解绑成功
        /// </returns>
        public int UnBindWindow(){
            var func = OLAPlugDLLHelper.GetFunction<UnBindWindowDelegate>("UnBindWindow");
            return func(OLAObject);
        }

        /// <summary>
        /// 获取当前对象已经绑定的窗口句柄，如果没有绑定窗口则返回0
        /// </summary>
        /// <returns>返回当前绑定的窗口句柄。如果没有绑定窗口，则返回0。</returns>
        public long GetBindWindow(){
            var func = OLAPlugDLLHelper.GetFunction<GetBindWindowDelegate>("GetBindWindow");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<ReleaseWindowsDllDelegate>("ReleaseWindowsDll");
            return func(OLAObject, hwnd);
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
            var func = OLAPlugDLLHelper.GetFunction<FreeStringPtrDelegate>("FreeStringPtr");
            return func(ptr);
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
            var func = OLAPlugDLLHelper.GetFunction<FreeMemoryPtrDelegate>("FreeMemoryPtr");
            return func(ptr);
        }

        /// <summary>
        /// 读取字符串大小。
        /// </summary>
        /// <param name="ptr">字符串内存地址</param>
        /// <returns>字符串缓冲区大小</returns>
        public int GetStringSize(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<GetStringSizeDelegate>("GetStringSize");
            return func(ptr);
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
            var func = OLAPlugDLLHelper.GetFunction<GetStringFromPtrDelegate>("GetStringFromPtr");
            return func(ptr, lpString, size);
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
            var func = OLAPlugDLLHelper.GetFunction<DelayDelegate>("Delay");
            return func(millisecond);
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
            var func = OLAPlugDLLHelper.GetFunction<DelaysDelegate>("Delays");
            return func(minMillisecond, maxMillisecond);
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
            var func = OLAPlugDLLHelper.GetFunction<SetUACDelegate>("SetUAC");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 检测当前系统是否有开启UAC(用户账户控制)。
        /// </summary>
        /// <returns>当前状态
        ///<br/>0: 关闭
        ///<br/>1: 开启
        /// </returns>
        public int CheckUAC(){
            var func = OLAPlugDLLHelper.GetFunction<CheckUACDelegate>("CheckUAC");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<RunAppDelegate>("RunApp");
            return func(OLAObject, appPath, mode);
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
            var func = OLAPlugDLLHelper.GetFunction<ExecuteCmdDelegate>("ExecuteCmd");
            return PtrToStringUTF8(func(OLAObject, cmd, current_dir, time_out));
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
            var func = OLAPlugDLLHelper.GetFunction<GetConfigDelegate>("GetConfig");
            return PtrToStringUTF8(func(OLAObject, configKey));
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
            var func = OLAPlugDLLHelper.GetFunction<SetConfigDelegate>("SetConfig");
            return func(OLAObject, configStr);
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
            var func = OLAPlugDLLHelper.GetFunction<SetConfigByKeyDelegate>("SetConfigByKey");
            return func(OLAObject, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<SendDropFilesDelegate>("SendDropFiles");
            return func(OLAObject, hwnd, file_path);
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
            var func = OLAPlugDLLHelper.GetFunction<SetDefaultEncodeDelegate>("SetDefaultEncode");
            return func(inputEncoding, outputEncoding);
        }

        /// <summary>
        /// 获取最后一次错误ID。
        /// </summary>
        /// <returns>错误ID</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 错误ID为0表示没有错误。
        /// </remarks>
        public int GetLastError(){
            var func = OLAPlugDLLHelper.GetFunction<GetLastErrorDelegate>("GetLastError");
            return func();
        }

        /// <summary>
        /// 获取最后一次错误字符串。
        /// </summary>
        /// <returns>错误字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存。
        /// </remarks>
        public string GetLastErrorString(){
            var func = OLAPlugDLLHelper.GetFunction<GetLastErrorStringDelegate>("GetLastErrorString");
            return PtrToStringUTF8(func());
        }

        /// <summary>
        /// 隐藏指定模块
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>隐藏上下文</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 隐藏模块可能会导致未知的问题,请谨慎使用
        /// <br/>2. 隐藏上下文需要调用 UnhideModule 接口释放
        /// </remarks>
        public long HideModule(string moduleName){
            var func = OLAPlugDLLHelper.GetFunction<HideModuleDelegate>("HideModule");
            return func(OLAObject, moduleName);
        }

        /// <summary>
        /// 恢复指定模块
        /// </summary>
        /// <param name="ctx">隐藏上下文</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 隐藏上下文需要调用 HideModule 接口生成，并且不能重复释放
        /// <br/>2. 释放后，模块将恢复显示
        /// </remarks>
        public int UnhideModule(long ctx){
            var func = OLAPlugDLLHelper.GetFunction<UnhideModuleDelegate>("UnhideModule");
            return func(OLAObject, ctx);
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
            var func = OLAPlugDLLHelper.GetFunction<GetRandomNumberDelegate>("GetRandomNumber");
            return func(OLAObject, min, max);
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
            var func = OLAPlugDLLHelper.GetFunction<GetRandomDoubleDelegate>("GetRandomDouble");
            return func(OLAObject, min, max);
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
            var func = OLAPlugDLLHelper.GetFunction<ExcludePosDelegate>("ExcludePos");
            return PtrToStringUTF8(func(OLAObject, json, type, x1, y1, x2, y2));
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
            var func = OLAPlugDLLHelper.GetFunction<FindNearestPosDelegate>("FindNearestPos");
            return PtrToStringUTF8(func(OLAObject, json, type, x, y));
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
            var func = OLAPlugDLLHelper.GetFunction<SortPosDistanceDelegate>("SortPosDistance");
            return PtrToStringUTF8(func(OLAObject, json, type, x, y));
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
            var func = OLAPlugDLLHelper.GetFunction<GetDenseRectDelegate>("GetDenseRect");
            return func(OLAObject, image, width, height, out x1, out y1, out x2, out y2);
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
            var func = OLAPlugDLLHelper.GetFunction<PathPlanningDelegate>("PathPlanning");
            var result = PtrToStringUTF8(func(OLAObject, image, startX, startY, endX, endY, potentialRadius, searchRadius));
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
            var func = OLAPlugDLLHelper.GetFunction<CreateGraphDelegate>("CreateGraph");
            return func(OLAObject, json);
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
            var func = OLAPlugDLLHelper.GetFunction<GetGraphDelegate>("GetGraph");
            return func(OLAObject, graphPtr);
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
            var func = OLAPlugDLLHelper.GetFunction<AddEdgeDelegate>("AddEdge");
            return func(OLAObject, graphPtr, from, to, weight, isDirected);
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
            var func = OLAPlugDLLHelper.GetFunction<GetShortestPathDelegate>("GetShortestPath");
            return PtrToStringUTF8(func(OLAObject, graphPtr, from, to));
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
            var func = OLAPlugDLLHelper.GetFunction<GetShortestDistanceDelegate>("GetShortestDistance");
            return func(OLAObject, graphPtr, from, to);
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
            var func = OLAPlugDLLHelper.GetFunction<ClearGraphDelegate>("ClearGraph");
            return func(OLAObject, graphPtr);
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
            var func = OLAPlugDLLHelper.GetFunction<DeleteGraphDelegate>("DeleteGraph");
            return func(OLAObject, graphPtr);
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
            var func = OLAPlugDLLHelper.GetFunction<GetNodeCountDelegate>("GetNodeCount");
            return func(OLAObject, graphPtr);
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
            var func = OLAPlugDLLHelper.GetFunction<GetEdgeCountDelegate>("GetEdgeCount");
            return func(OLAObject, graphPtr);
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
            var func = OLAPlugDLLHelper.GetFunction<GetShortestPathToAllNodesDelegate>("GetShortestPathToAllNodes");
            return PtrToStringUTF8(func(OLAObject, graphPtr, startNode));
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
            var func = OLAPlugDLLHelper.GetFunction<GetMinimumSpanningTreeDelegate>("GetMinimumSpanningTree");
            return PtrToStringUTF8(func(OLAObject, graphPtr));
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
            var func = OLAPlugDLLHelper.GetFunction<GetDirectedPathToAllNodesDelegate>("GetDirectedPathToAllNodes");
            return PtrToStringUTF8(func(OLAObject, graphPtr, startNode));
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
            var func = OLAPlugDLLHelper.GetFunction<GetMinimumArborescenceDelegate>("GetMinimumArborescence");
            return PtrToStringUTF8(func(OLAObject, graphPtr, root));
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
            var func = OLAPlugDLLHelper.GetFunction<CreateGraphFromCoordinatesDelegate>("CreateGraphFromCoordinates");
            return func(OLAObject, json, connectAll, maxDistance, useEuclideanDistance);
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
            var func = OLAPlugDLLHelper.GetFunction<AddCoordinateNodeDelegate>("AddCoordinateNode");
            return func(OLAObject, graphPtr, name, x, y, connectToExisting, maxDistance, useEuclideanDistance);
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
            var func = OLAPlugDLLHelper.GetFunction<GetNodeCoordinatesDelegate>("GetNodeCoordinates");
            return PtrToStringUTF8(func(OLAObject, graphPtr, name));
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
            var func = OLAPlugDLLHelper.GetFunction<SetNodeConnectionDelegate>("SetNodeConnection");
            return func(OLAObject, graphPtr, from, to, canConnect, weight);
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
            var func = OLAPlugDLLHelper.GetFunction<GetNodeConnectionStatusDelegate>("GetNodeConnectionStatus");
            return func(OLAObject, graphPtr, from, to);
        }

        /// <summary>
        /// 执行汇编指令
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="asmStr">汇编语言字符串,大小写均可以。比如 "mov eax,1" 也支持输入机器码</param>
        /// <param name="type">执行类型
        ///<br/> 0: 在本进程中执行(创建线程),hwnd无效
        ///<br/> 1: 在hwnd指定进程内执行(创建远程线程)
        ///<br/> 2: 在已注入绑定的目标进程创建线程执行(需排队)
        ///<br/> 3: 同模式2,但在hwnd所在线程直接执行
        ///<br/> 4: 同模式0,但在当前线程直接执行
        ///<br/> 5: 在hwnd指定进程内执行(APC注入)
        ///<br/> 6: 直接在hwnd所在线程执行
        /// </param>
        /// <param name="baseAddr">汇编指令所在的地址,如果为0则自动分配内存</param>
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
            var func = OLAPlugDLLHelper.GetFunction<AsmCallDelegate>("AsmCall");
            return func(OLAObject, hwnd, asmStr, type, baseAddr);
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
            var func = OLAPlugDLLHelper.GetFunction<AssembleDelegate>("Assemble");
            return PtrToStringUTF8(func(OLAObject, asmStr, baseAddr, arch, mode));
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
            var func = OLAPlugDLLHelper.GetFunction<DisassembleDelegate>("Disassemble");
            return PtrToStringUTF8(func(OLAObject, asmCode, baseAddr, arch, mode, showType));
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
            var func = OLAPlugDLLHelper.GetFunction<LoginDelegate>("Login");
            return PtrToStringUTF8(func(userCode, softCode, featureList, softVersion, dealerCode));
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
            var func = OLAPlugDLLHelper.GetFunction<ActivateDelegate>("Activate");
            return PtrToStringUTF8(func(userCode, softCode, softVersion, dealerCode, licenseKey));
        }

        /// <summary>
        /// 添加VMware DMA设备(默认连接字符串)
        /// </summary>
        /// <param name="vmId">VMware虚拟机ID</param>
        /// <returns>成功返回设备ID(>=0), 失败返回-1</returns>
        public long DmaAddDevice(int vmId){
            var func = OLAPlugDLLHelper.GetFunction<DmaAddDeviceDelegate>("DmaAddDevice");
            return func(OLAObject, vmId);
        }

        /// <summary>
        /// 添加自定义DMA设备
        /// </summary>
        /// <param name="connectionString">设备连接字符串(如"vmware://rw=1,id=1", "fpga://algo=4"等)</param>
        /// <returns>成功返回设备ID(>=0), 失败返回-1</returns>
        public long DmaAddDeviceEx(string connectionString){
            var func = OLAPlugDLLHelper.GetFunction<DmaAddDeviceExDelegate>("DmaAddDeviceEx");
            return func(OLAObject, connectionString);
        }

        /// <summary>
        /// 删除DMA设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaRemoveDevice(long deviceId){
            var func = OLAPlugDLLHelper.GetFunction<DmaRemoveDeviceDelegate>("DmaRemoveDevice");
            return func(OLAObject, deviceId);
        }

        /// <summary>
        /// 根据进程名获取PID
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="processName">进程名(支持部分匹配)</param>
        /// <returns>成功返回PID, 失败返回0</returns>
        public int DmaGetPidFromName(long deviceId, string processName){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetPidFromNameDelegate>("DmaGetPidFromName");
            return func(OLAObject, deviceId, processName);
        }

        /// <summary>
        /// 获取所有进程PID列表
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>返回二进制字符串的指针，数据格式:"pid1|pid2|pid3...",失败返回空字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaGetPidList(long deviceId){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetPidListDelegate>("DmaGetPidList");
            return PtrToStringUTF8(func(OLAObject, deviceId));
        }

        /// <summary>
        /// 获取进程基本信息
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <returns>返回二进制字符串的指针，数据格式:"进程名,镜像基址,镜像大小",失败返回空字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaGetProcessInfo(long deviceId, int pid){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetProcessInfoDelegate>("DmaGetProcessInfo");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid));
        }

        /// <summary>
        /// 获取模块基址
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="moduleName">模块名(空字符串表示主模块)</param>
        /// <returns>成功返回模块基址, 失败返回0</returns>
        public long DmaGetModuleBase(long deviceId, int pid, string moduleName){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetModuleBaseDelegate>("DmaGetModuleBase");
            return func(OLAObject, deviceId, pid, moduleName);
        }

        /// <summary>
        /// 获取模块大小
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="moduleName">模块名</param>
        /// <returns>成功返回模块大小, 失败返回0</returns>
        public int DmaGetModuleSize(long deviceId, int pid, string moduleName){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetModuleSizeDelegate>("DmaGetModuleSize");
            return func(OLAObject, deviceId, pid, moduleName);
        }

        /// <summary>
        /// 获取模块导出函数地址
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="moduleName">模块名</param>
        /// <param name="functionName">函数名</param>
        /// <returns>成功返回函数地址, 失败返回0</returns>
        public long DmaGetProcAddress(long deviceId, int pid, string moduleName, string functionName){
            var func = OLAPlugDLLHelper.GetFunction<DmaGetProcAddressDelegate>("DmaGetProcAddress");
            return func(OLAObject, deviceId, pid, moduleName, functionName);
        }

        /// <summary>
        /// 创建散列读句柄
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <returns>成功返回散列句柄, 失败返回0</returns>
        public long DmaScatterCreate(long deviceId, int pid){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterCreateDelegate>("DmaScatterCreate");
            return func(OLAObject, deviceId, pid);
        }

        /// <summary>
        /// 准备散列读地址
        /// </summary>
        /// <param name="scatterHandle">散列句柄</param>
        /// <param name="address">地址</param>
        /// <param name="size">大小</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaScatterPrepare(long scatterHandle, long address, int size){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterPrepareDelegate>("DmaScatterPrepare");
            return func(OLAObject, scatterHandle, address, size);
        }

        /// <summary>
        /// 执行散列读
        /// </summary>
        /// <param name="scatterHandle">散列句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaScatterExecute(long scatterHandle){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterExecuteDelegate>("DmaScatterExecute");
            return func(OLAObject, scatterHandle);
        }

        /// <summary>
        /// 从散列读结果中读取数据
        /// </summary>
        /// <param name="scatterHandle">散列句柄</param>
        /// <param name="address">地址</param>
        /// <param name="buffer">输出缓冲区地址</param>
        /// <param name="size">读取大小</param>
        /// <returns>实际读取的字节数</returns>
        public int DmaScatterRead(long scatterHandle, long address, long buffer, int size){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterReadDelegate>("DmaScatterRead");
            return func(OLAObject, scatterHandle, address, buffer, size);
        }

        /// <summary>
        /// 清除散列读准备的数据
        /// </summary>
        /// <param name="scatterHandle">散列句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaScatterClear(long scatterHandle){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterClearDelegate>("DmaScatterClear");
            return func(OLAObject, scatterHandle);
        }

        /// <summary>
        /// 关闭散列读句柄
        /// </summary>
        /// <param name="scatterHandle">散列句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaScatterClose(long scatterHandle){
            var func = OLAPlugDLLHelper.GetFunction<DmaScatterCloseDelegate>("DmaScatterClose");
            return func(OLAObject, scatterHandle);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="data">要搜索的二进制数据,支持CE数据格式 比如"00 01 23 45 * ?? ?b c? * f1"等.</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindData(long deviceId, int pid, string addr_range, string data){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindDataDelegate>("DmaFindData");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, data));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindDataEx(long deviceId, int pid, string addr_range, string data, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindDataExDelegate>("DmaFindDataEx");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, data, step, multi_thread, mode));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="double_value_min">最小值</param>
        /// <param name="double_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindDouble(long deviceId, int pid, string addr_range, double double_value_min, double double_value_max){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindDoubleDelegate>("DmaFindDouble");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, double_value_min, double_value_max));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindDoubleEx(long deviceId, int pid, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindDoubleExDelegate>("DmaFindDoubleEx");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, double_value_min, double_value_max, step, multi_thread, mode));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="float_value_min">最小值</param>
        /// <param name="float_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindFloat(long deviceId, int pid, string addr_range, float float_value_min, float float_value_max){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindFloatDelegate>("DmaFindFloat");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, float_value_min, float_value_max));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindFloatEx(long deviceId, int pid, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindFloatExDelegate>("DmaFindFloatEx");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, float_value_min, float_value_max, step, multi_thread, mode));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="int_value_min">最小值</param>
        /// <param name="int_value_max">最大值</param>
        /// <param name="type">搜索的整数类型,取值如下
        ///<br/> 0: 32位
        ///<br/> 1: 16位
        ///<br/> 2: 8位
        ///<br/> 3: 64位
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindInt(long deviceId, int pid, string addr_range, long int_value_min, long int_value_max, int type){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindIntDelegate>("DmaFindInt");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, int_value_min, int_value_max, type));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="int_value_min">最小值</param>
        /// <param name="int_value_max">最大值</param>
        /// <param name="type">搜索的整数类型,取值如下
        ///<br/> 0: 32位
        ///<br/> 1: 16位
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindIntEx(long deviceId, int pid, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindIntExDelegate>("DmaFindIntEx");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, int_value_min, int_value_max, type, step, multi_thread, mode));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="string_value">要搜索的字符串</param>
        /// <param name="type">类型
        ///<br/> 0: 返回Ascii表达的字符串
        ///<br/> 1: 返回Unicode表达的字符串
        ///<br/> 2: 返回UTF8表达的字符串
        /// </param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindString(long deviceId, int pid, string addr_range, string string_value, int type){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindStringDelegate>("DmaFindString");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, string_value, type));
        }

        /// <summary>
        /// 通过DMA搜索指定范围内的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3...|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaFindStringEx(long deviceId, int pid, string addr_range, string string_value, int type, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DmaFindStringExDelegate>("DmaFindStringEx");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr_range, string_value, type, step, multi_thread, mode));
        }

        /// <summary>
        /// 通过DMA读取指定地址的数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaReadData(long deviceId, int pid, string addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDataDelegate>("DmaReadData");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr, len));
        }

        /// <summary>
        /// 通过DMA读取指定地址的数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaReadDataAddr(long deviceId, int pid, long addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDataAddrDelegate>("DmaReadDataAddr");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr, len));
        }

        /// <summary>
        /// 通过DMA读取指定地址的数据到本地缓冲区
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的数据字符串指针. 返回0表示读取失败.</returns>
        public long DmaReadDataAddrToBin(long deviceId, int pid, long addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDataAddrToBinDelegate>("DmaReadDataAddrToBin");
            return func(OLAObject, deviceId, pid, addr, len);
        }

        /// <summary>
        /// 通过DMA读取指定地址的数据到本地缓冲区
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的内存地址</returns>
        public long DmaReadDataToBin(long deviceId, int pid, string addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDataToBinDelegate>("DmaReadDataToBin");
            return func(OLAObject, deviceId, pid, addr, len);
        }

        /// <summary>
        /// 通过DMA读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double DmaReadDouble(long deviceId, int pid, string addr){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDoubleDelegate>("DmaReadDouble");
            return func(OLAObject, deviceId, pid, addr);
        }

        /// <summary>
        /// 通过DMA读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double DmaReadDoubleAddr(long deviceId, int pid, long addr){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadDoubleAddrDelegate>("DmaReadDoubleAddr");
            return func(OLAObject, deviceId, pid, addr);
        }

        /// <summary>
        /// 通过DMA读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float DmaReadFloat(long deviceId, int pid, string addr){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadFloatDelegate>("DmaReadFloat");
            return func(OLAObject, deviceId, pid, addr);
        }

        /// <summary>
        /// 通过DMA读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float DmaReadFloatAddr(long deviceId, int pid, long addr){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadFloatAddrDelegate>("DmaReadFloatAddr");
            return func(OLAObject, deviceId, pid, addr);
        }

        /// <summary>
        /// 通过DMA读取指定地址的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
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
        public long DmaReadInt(long deviceId, int pid, string addr, int type){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadIntDelegate>("DmaReadInt");
            return func(OLAObject, deviceId, pid, addr, type);
        }

        /// <summary>
        /// 通过DMA读取指定地址的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        public long DmaReadIntAddr(long deviceId, int pid, long addr, int type){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadIntAddrDelegate>("DmaReadIntAddr");
            return func(OLAObject, deviceId, pid, addr, type);
        }

        /// <summary>
        /// 通过DMA读取指定地址的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: GBK字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="len">需要读取的字节数目.如果为0，则自动判定字符串长度.</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的字符串,以UTF-8编码</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaReadString(long deviceId, int pid, string addr, int type, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadStringDelegate>("DmaReadString");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr, type, len));
        }

        /// <summary>
        /// 通过DMA读取指定地址的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: GBK字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="len">需要读取的字节数目.如果为0，则自动判定字符串长度.</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的字符串,以UTF-8编码</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DmaReadStringAddr(long deviceId, int pid, long addr, int type, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaReadStringAddrDelegate>("DmaReadStringAddr");
            return PtrToStringUTF8(func(OLAObject, deviceId, pid, addr, type, len));
        }

        /// <summary>
        /// 通过DMA写入指定地址的数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteData(long deviceId, int pid, string addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDataDelegate>("DmaWriteData");
            return func(OLAObject, deviceId, pid, addr, data);
        }

        /// <summary>
        /// 通过DMA写入指定地址的数据(源为本地缓冲区)
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">字符串数据地址</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteDataFromBin(long deviceId, int pid, string addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDataFromBinDelegate>("DmaWriteDataFromBin");
            return func(OLAObject, deviceId, pid, addr, data, len);
        }

        /// <summary>
        /// 通过DMA写入指定地址的数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="data">二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteDataAddr(long deviceId, int pid, long addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDataAddrDelegate>("DmaWriteDataAddr");
            return func(OLAObject, deviceId, pid, addr, data);
        }

        /// <summary>
        /// 通过DMA写入指定地址的数据(源为本地缓冲区)
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="data">数据 二进制数据地址</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteDataAddrFromBin(long deviceId, int pid, long addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDataAddrFromBinDelegate>("DmaWriteDataAddrFromBin");
            return func(OLAObject, deviceId, pid, addr, data, len);
        }

        /// <summary>
        /// 通过DMA写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteDouble(long deviceId, int pid, string addr, double double_value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDoubleDelegate>("DmaWriteDouble");
            return func(OLAObject, deviceId, pid, addr, double_value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteDoubleAddr(long deviceId, int pid, long addr, double double_value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteDoubleAddrDelegate>("DmaWriteDoubleAddr");
            return func(OLAObject, deviceId, pid, addr, double_value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteFloat(long deviceId, int pid, string addr, float float_value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteFloatDelegate>("DmaWriteFloat");
            return func(OLAObject, deviceId, pid, addr, float_value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteFloatAddr(long deviceId, int pid, long addr, float float_value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteFloatAddrDelegate>("DmaWriteFloatAddr");
            return func(OLAObject, deviceId, pid, addr, float_value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteInt(long deviceId, int pid, string addr, int type, long value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteIntDelegate>("DmaWriteInt");
            return func(OLAObject, deviceId, pid, addr, type, value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的整数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteIntAddr(long deviceId, int pid, long addr, int type, long value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteIntAddrDelegate>("DmaWriteIntAddr");
            return func(OLAObject, deviceId, pid, addr, type, value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: Ascii字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="value">要写入的字符串</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteString(long deviceId, int pid, string addr, int type, string value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteStringDelegate>("DmaWriteString");
            return func(OLAObject, deviceId, pid, addr, type, value);
        }

        /// <summary>
        /// 通过DMA写入指定地址的字符串
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="pid">进程PID</param>
        /// <param name="addr">地址</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: Ascii字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="value">要写入的字符串</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DmaWriteStringAddr(long deviceId, int pid, long addr, int type, string value){
            var func = OLAPlugDLLHelper.GetFunction<DmaWriteStringAddrDelegate>("DmaWriteStringAddr");
            return func(OLAObject, deviceId, pid, addr, type, value);
        }

        /// <summary>
        /// 释放绘制系统资源并清理所有对象
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiCleanup(){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiCleanupDelegate>("DrawGuiCleanup");
            return func(OLAObject);
        }

        /// <summary>
        /// 启用或禁用绘制系统
        /// </summary>
        /// <param name="active">1 启用，0 禁用</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetGuiActive(int active){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetGuiActiveDelegate>("DrawGuiSetGuiActive");
            return func(OLAObject, active);
        }

        /// <summary>
        /// 查询绘制系统是否启用
        /// </summary>
        /// <returns>状态，0 未启用，1 已启用</returns>
        public int DrawGuiIsGuiActive(){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiIsGuiActiveDelegate>("DrawGuiIsGuiActive");
            return func(OLAObject);
        }

        /// <summary>
        /// 设置绘制窗口是否可穿透点击
        /// </summary>
        /// <param name="enabled">1 可穿透，0 不可穿透</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetGuiClickThrough(int enabled){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetGuiClickThroughDelegate>("DrawGuiSetGuiClickThrough");
            return func(OLAObject, enabled);
        }

        /// <summary>
        /// 查询绘制窗口是否设置为可穿透
        /// </summary>
        /// <returns>状态
        ///<br/>0: 否
        ///<br/>1: 是
        /// </returns>
        public int DrawGuiIsGuiClickThrough(){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiIsGuiClickThroughDelegate>("DrawGuiIsGuiClickThrough");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiRectangleDelegate>("DrawGuiRectangle");
            return func(OLAObject, x, y, width, height, mode, lineThickness);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiCircleDelegate>("DrawGuiCircle");
            return func(OLAObject, x, y, radius, mode, lineThickness);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiLineDelegate>("DrawGuiLine");
            return func(OLAObject, x1, y1, x2, y2, lineThickness);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiTextDelegate>("DrawGuiText");
            return func(OLAObject, text, x, y, fontPath, fontSize, align);
        }

        /// <summary>
        /// 创建图片对象
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiImage(string imagePath, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiImageDelegate>("DrawGuiImage");
            return func(OLAObject, imagePath, x, y);
        }

        /// <summary>
        /// 创建图片对象
        /// </summary>
        /// <param name="imagePtr">图片指针</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>对象句柄，失败返回0</returns>
        public long DrawGuiImagePtr(long imagePtr, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiImagePtrDelegate>("DrawGuiImagePtr");
            return func(OLAObject, imagePtr, x, y);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiWindowDelegate>("DrawGuiWindow");
            return func(OLAObject, title, x, y, width, height, style);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiPanelDelegate>("DrawGuiPanel");
            return func(OLAObject, parentHandle, x, y, width, height);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiButtonDelegate>("DrawGuiButton");
            return func(OLAObject, parentHandle, text, x, y, width, height);
        }

        /// <summary>
        /// 设置对象位置
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">左上角X</param>
        /// <param name="y">左上角Y</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetPosition(long handle, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetPositionDelegate>("DrawGuiSetPosition");
            return func(OLAObject, handle, x, y);
        }

        /// <summary>
        /// 设置对象尺寸
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetSize(long handle, int width, int height){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetSizeDelegate>("DrawGuiSetSize");
            return func(OLAObject, handle, width, height);
        }

        /// <summary>
        /// 设置对象颜色（RGBA）
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="r">红色分量（0-255）</param>
        /// <param name="g">绿色分量（0-255）</param>
        /// <param name="b">蓝色分量（0-255）</param>
        /// <param name="a">透明度（0-255）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetColor(long handle, int r, int g, int b, int a){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetColorDelegate>("DrawGuiSetColor");
            return func(OLAObject, handle, r, g, b, a);
        }

        /// <summary>
        /// 设置对象整体透明度
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="alpha">透明度（0-255）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetAlpha(long handle, int alpha){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetAlphaDelegate>("DrawGuiSetAlpha");
            return func(OLAObject, handle, alpha);
        }

        /// <summary>
        /// 设置绘制模式
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="mode">绘制模式，见DrawMode</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetDrawMode(long handle, int mode){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetDrawModeDelegate>("DrawGuiSetDrawMode");
            return func(OLAObject, handle, mode);
        }

        /// <summary>
        /// 设置线宽
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="thickness">线宽（像素）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetLineThickness(long handle, double thickness){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetLineThicknessDelegate>("DrawGuiSetLineThickness");
            return func(OLAObject, handle, thickness);
        }

        /// <summary>
        /// 设置文本字体
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="fontPath">字体文件路径（ttf/otf）</param>
        /// <param name="fontSize">字号（像素）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetFont(long handle, string fontPath, int fontSize){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetFontDelegate>("DrawGuiSetFont");
            return func(OLAObject, handle, fontPath, fontSize);
        }

        /// <summary>
        /// 设置文本对齐
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="align">对齐方式，见TextAlign</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetTextAlign(long handle, int align){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetTextAlignDelegate>("DrawGuiSetTextAlign");
            return func(OLAObject, handle, align);
        }

        /// <summary>
        /// 设置文本内容
        /// </summary>
        /// <param name="handle">文本对象句柄</param>
        /// <param name="text">文本内容</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetText(long handle, string text){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetTextDelegate>("DrawGuiSetText");
            return func(OLAObject, handle, text);
        }

        /// <summary>
        /// 设置窗口标题
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="title">标题文本</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetWindowTitle(long handle, string title){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetWindowTitleDelegate>("DrawGuiSetWindowTitle");
            return func(OLAObject, handle, title);
        }

        /// <summary>
        /// 设置窗口样式
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="style">窗口样式，见WindowStyle</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetWindowStyle(long handle, int style){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetWindowStyleDelegate>("DrawGuiSetWindowStyle");
            return func(OLAObject, handle, style);
        }

        /// <summary>
        /// 设置窗口是否置顶
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="topMost">1 置顶，0 取消置顶</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetWindowTopMost(long handle, int topMost){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetWindowTopMostDelegate>("DrawGuiSetWindowTopMost");
            return func(OLAObject, handle, topMost);
        }

        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <param name="alpha">透明度（0-255）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetWindowTransparency(long handle, int alpha){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetWindowTransparencyDelegate>("DrawGuiSetWindowTransparency");
            return func(OLAObject, handle, alpha);
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiDeleteObject(long handle){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiDeleteObjectDelegate>("DrawGuiDeleteObject");
            return func(OLAObject, handle);
        }

        /// <summary>
        /// 清空所有对象
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiClearAll(){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiClearAllDelegate>("DrawGuiClearAll");
            return func(OLAObject);
        }

        /// <summary>
        /// 设置对象可见性
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="visible">1 可见，0 隐藏</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetVisible(long handle, int visible){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetVisibleDelegate>("DrawGuiSetVisible");
            return func(OLAObject, handle, visible);
        }

        /// <summary>
        /// 设置对象Z序（绘制顺序）
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="zOrder">Z序值，数值越大越靠前</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetZOrder(long handle, int zOrder){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetZOrderDelegate>("DrawGuiSetZOrder");
            return func(OLAObject, handle, zOrder);
        }

        /// <summary>
        /// 设置对象父子关系
        /// </summary>
        /// <param name="handle">子对象句柄</param>
        /// <param name="parentHandle">父对象句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetParent(long handle, long parentHandle){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetParentDelegate>("DrawGuiSetParent");
            return func(OLAObject, handle, parentHandle);
        }

        /// <summary>
        /// 设置按钮点击回调
        /// </summary>
        /// <param name="handle">按钮对象句柄</param>
        /// <param name="callback">按钮回调函数指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetButtonCallback(long handle, DrawGuiButtonCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetButtonCallbackDelegate>("DrawGuiSetButtonCallback");
            return func(OLAObject, handle, callback);
        }

        /// <summary>
        /// 设置鼠标事件回调
        /// </summary>
        /// <param name="handle">目标对象句柄</param>
        /// <param name="callback">鼠标回调函数指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiSetMouseCallback(long handle, DrawGuiMouseCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiSetMouseCallbackDelegate>("DrawGuiSetMouseCallback");
            return func(OLAObject, handle, callback);
        }

        /// <summary>
        /// 获取对象类型
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <returns>对象类型，见DrawType</returns>
        public int DrawGuiGetDrawObjectType(long handle){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiGetDrawObjectTypeDelegate>("DrawGuiGetDrawObjectType");
            return func(OLAObject, handle);
        }

        /// <summary>
        /// 获取对象位置
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">返回左上角X（输出）</param>
        /// <param name="y">返回左上角Y（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiGetPosition(long handle, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiGetPositionDelegate>("DrawGuiGetPosition");
            return func(OLAObject, handle, out x, out y);
        }

        /// <summary>
        /// 获取对象尺寸
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="width">返回宽度（输出）</param>
        /// <param name="height">返回高度（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DrawGuiGetSize(long handle, out int width, out int height){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiGetSizeDelegate>("DrawGuiGetSize");
            return func(OLAObject, handle, out width, out height);
        }

        /// <summary>
        /// 判断坐标点是否在对象内
        /// </summary>
        /// <param name="handle">对象句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>结果enum 0 否enum 1 是</returns>
        public int DrawGuiIsPointInObject(long handle, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<DrawGuiIsPointInObjectDelegate>("DrawGuiIsPointInObject");
            return func(OLAObject, handle, x, y);
        }

        /// <summary>
        /// 设置内存读写模式
        /// </summary>
        /// <param name="mode">内存模式
        ///<br/> 0: 远程模式
        ///<br/> 1: 本地模式(需要DLL注入)
        ///<br/> 2: 驱动API方式读写内存
        ///<br/> 3: 驱动MDL方式读写内存
        /// </param>
        /// <returns>1成功 其他失败</returns>
        public int SetMemoryMode(int mode){
            var func = OLAPlugDLLHelper.GetFunction<SetMemoryModeDelegate>("SetMemoryMode");
            return func(OLAObject, mode);
        }

        /// <summary>
        /// 导出驱动
        /// </summary>
        /// <param name="driver_path">驱动路径</param>
        /// <param name="type">驱动类型</param>
        /// <returns>1成功 其他失败</returns>
        public int ExportDriver(string driver_path, int type){
            var func = OLAPlugDLLHelper.GetFunction<ExportDriverDelegate>("ExportDriver");
            return func(OLAObject, driver_path, type);
        }

        /// <summary>
        /// 加载驱动
        /// </summary>
        /// <param name="driver_name">驱动名称,为空则初始化欧拉驱动</param>
        /// <param name="driver_path">驱动路径</param>
        /// <returns>1成功 其他失败</returns>
        public int LoadDriver(string driver_name, string driver_path){
            var func = OLAPlugDLLHelper.GetFunction<LoadDriverDelegate>("LoadDriver");
            return func(OLAObject, driver_name, driver_path);
        }

        /// <summary>
        /// 卸载驱动
        /// </summary>
        /// <param name="driver_name">驱动名称</param>
        /// <returns>1成功 其他失败</returns>
        public int UnloadDriver(string driver_name){
            var func = OLAPlugDLLHelper.GetFunction<UnloadDriverDelegate>("UnloadDriver");
            return func(OLAObject, driver_name);
        }

        /// <summary>
        /// 测试驱动是否正常加载
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int DriverTest(){
            var func = OLAPlugDLLHelper.GetFunction<DriverTestDelegate>("DriverTest");
            return func(OLAObject);
        }

        /// <summary>
        /// 加载PDB文件,驱动加载失败时可以尝试加载PDB文件
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int LoadPdb(){
            var func = OLAPlugDLLHelper.GetFunction<LoadPdbDelegate>("LoadPdb");
            return func(OLAObject);
        }

        /// <summary>
        /// 获取PDB文件下载URL和保存路径列表
        /// </summary>
        /// <returns>PDB文件下载URL|保存路径列表</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的URL列表格式为： url1|path1\nurl2|path2\nurl3|path3\n...
        /// <br/>2. 需要调用 FreeStringPtr 释放返回的URL列表
        /// </remarks>
        public string GetPdbDownloadUrls(){
            var func = OLAPlugDLLHelper.GetFunction<GetPdbDownloadUrlsDelegate>("GetPdbDownloadUrls");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 隐藏进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="enable">是否隐藏</param>
        /// <returns>1成功 其他失败</returns>
        public int HideProcess(long pid, int enable){
            var func = OLAPlugDLLHelper.GetFunction<HideProcessDelegate>("HideProcess");
            return func(OLAObject, pid, enable);
        }

        /// <summary>
        /// 保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="enable">是否保护</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectProcess(long pid, int enable){
            var func = OLAPlugDLLHelper.GetFunction<ProtectProcessDelegate>("ProtectProcess");
            return func(OLAObject, pid, enable);
        }

        /// <summary>
        /// 保护进程模式2
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="enable">是否保护</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectProcess2(long pid, int enable){
            var func = OLAPlugDLLHelper.GetFunction<ProtectProcess2Delegate>("ProtectProcess2");
            return func(OLAObject, pid, enable);
        }

        /// <summary>
        /// 添加保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="mode">保护模式</param>
        /// <param name="allow_pid">允许的进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int AddProtectPID(long pid, long mode, long allow_pid){
            var func = OLAPlugDLLHelper.GetFunction<AddProtectPIDDelegate>("AddProtectPID");
            return func(OLAObject, pid, mode, allow_pid);
        }

        /// <summary>
        /// 删除保护进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int RemoveProtectPID(long pid){
            var func = OLAPlugDLLHelper.GetFunction<RemoveProtectPIDDelegate>("RemoveProtectPID");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 添加允许进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int AddAllowPID(long pid){
            var func = OLAPlugDLLHelper.GetFunction<AddAllowPIDDelegate>("AddAllowPID");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 删除允许进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int RemoveAllowPID(long pid){
            var func = OLAPlugDLLHelper.GetFunction<RemoveAllowPIDDelegate>("RemoveAllowPID");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 伪装进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="fake_pid">伪装的目标进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int FakeProcess(long pid, long fake_pid){
            var func = OLAPlugDLLHelper.GetFunction<FakeProcessDelegate>("FakeProcess");
            return func(OLAObject, pid, fake_pid);
        }

        /// <summary>
        /// 保护窗口,防止截屏
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="flag">保护标志 0还原 1黑屏 2透明</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectWindow(long hwnd, int flag){
            var func = OLAPlugDLLHelper.GetFunction<ProtectWindowDelegate>("ProtectWindow");
            return func(OLAObject, hwnd, flag);
        }

        /// <summary>
        /// 打开进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="process_handle">进程句柄</param>
        /// <returns>1成功 其他失败</returns>
        public int KeOpenProcess(long pid, out long process_handle){
            var func = OLAPlugDLLHelper.GetFunction<KeOpenProcessDelegate>("KeOpenProcess");
            return func(OLAObject, pid, out process_handle);
        }

        /// <summary>
        /// 打开线程
        /// </summary>
        /// <param name="thread_id">线程ID</param>
        /// <param name="thread_handle">线程句柄</param>
        /// <returns>1成功 其他失败</returns>
        public int KeOpenThread(long thread_id, out long thread_handle){
            var func = OLAPlugDLLHelper.GetFunction<KeOpenThreadDelegate>("KeOpenThread");
            return func(OLAObject, thread_id, out thread_handle);
        }

        /// <summary>
        /// 启动安全守护
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int StartSecurityGuard(){
            var func = OLAPlugDLLHelper.GetFunction<StartSecurityGuardDelegate>("StartSecurityGuard");
            return func(OLAObject);
        }

        /// <summary>
        /// 测试文件保护驱动通信是否正常
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileTestDriver(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileTestDriverDelegate>("ProtectFileTestDriver");
            return func(OLAObject);
        }

        /// <summary>
        /// 启用文件保护驱动
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileEnableDriver(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileEnableDriverDelegate>("ProtectFileEnableDriver");
            return func(OLAObject);
        }

        /// <summary>
        /// 禁用文件保护驱动
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileDisableDriver(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileDisableDriverDelegate>("ProtectFileDisableDriver");
            return func(OLAObject);
        }

        /// <summary>
        /// 启动文件系统过滤器
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileStartFilter(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileStartFilterDelegate>("ProtectFileStartFilter");
            return func(OLAObject);
        }

        /// <summary>
        /// 停止文件系统过滤器
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileStopFilter(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileStopFilterDelegate>("ProtectFileStopFilter");
            return func(OLAObject);
        }

        /// <summary>
        /// 添加受保护路径
        /// </summary>
        /// <param name="path">要保护的文件或文件夹路径</param>
        /// <param name="mode">保护模式：0-全部拦截, 1-允许白名单, 2-拦截黑名单</param>
        /// <param name="is_directory">是否为目录 (1-目录, 0-文件)</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileAddProtectedPath(string path, int mode, int is_directory){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileAddProtectedPathDelegate>("ProtectFileAddProtectedPath");
            return func(OLAObject, path, mode, is_directory);
        }

        /// <summary>
        /// 移除受保护路径
        /// </summary>
        /// <param name="path">要移除保护的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileRemoveProtectedPath(string path){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileRemoveProtectedPathDelegate>("ProtectFileRemoveProtectedPath");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 清空所有受保护路径
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileClearProtectedPaths(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileClearProtectedPathsDelegate>("ProtectFileClearProtectedPaths");
            return func(OLAObject);
        }

        /// <summary>
        /// 查询路径是否受保护
        /// </summary>
        /// <param name="path">要查询的文件或文件夹路径</param>
        /// <param name="mode">输出参数，用于接收该路径的保护模式（可为NULL）</param>
        /// <returns>1-路径受保护, 0-路径未受保护或查询失败</returns>
        public int ProtectFileQueryProtectedPath(string path, out int mode){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileQueryProtectedPathDelegate>("ProtectFileQueryProtectedPath");
            return func(OLAObject, path, out mode);
        }

        /// <summary>
        /// 添加进程到白名单
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileAddWhitelist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileAddWhitelistDelegate>("ProtectFileAddWhitelist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 从白名单移除进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileRemoveWhitelist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileRemoveWhitelistDelegate>("ProtectFileRemoveWhitelist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 清空白名单
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileClearWhitelist(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileClearWhitelistDelegate>("ProtectFileClearWhitelist");
            return func(OLAObject);
        }

        /// <summary>
        /// 查询进程是否在白名单中
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1-在白名单中, 0-不在白名单中或查询失败</returns>
        public int ProtectFileQueryWhitelist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileQueryWhitelistDelegate>("ProtectFileQueryWhitelist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 添加进程到黑名单
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileAddBlacklist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileAddBlacklistDelegate>("ProtectFileAddBlacklist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 从黑名单移除进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileRemoveBlacklist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileRemoveBlacklistDelegate>("ProtectFileRemoveBlacklist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 清空黑名单
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int ProtectFileClearBlacklist(){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileClearBlacklistDelegate>("ProtectFileClearBlacklist");
            return func(OLAObject);
        }

        /// <summary>
        /// 查询进程是否在黑名单中
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>1-在黑名单中, 0-不在黑名单中或查询失败</returns>
        public int ProtectFileQueryBlacklist(long pid){
            var func = OLAPlugDLLHelper.GetFunction<ProtectFileQueryBlacklistDelegate>("ProtectFileQueryBlacklist");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 启用VT驱动
        /// </summary>
        /// <param name="enable">是否启用VT驱动</param>
        /// <returns>1加载VT驱动成功 其他失败</returns>
        public int EnabletVtDriver(int enable){
            var func = OLAPlugDLLHelper.GetFunction<EnabletVtDriverDelegate>("EnabletVtDriver");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 写入指定地址的数据. 可以让执行和读写分离，可以有效的解决CRC检测
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtFakeWriteData(long hwnd, string addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<VtFakeWriteDataDelegate>("VtFakeWriteData");
            return func(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据. 可以让执行和读写分离，可以有效的解决CRC检测
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">字符串数据地址</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtFakeWriteDataFromBin(long hwnd, string addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<VtFakeWriteDataFromBinDelegate>("VtFakeWriteDataFromBin");
            return func(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 写入指定地址的数据. 可以让执行和读写分离，可以有效的解决CRC检测
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtFakeWriteDataAddr(long hwnd, long addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<VtFakeWriteDataAddrDelegate>("VtFakeWriteDataAddr");
            return func(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据. 可以让执行和读写分离，可以有效的解决CRC检测
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtFakeWriteDataAddrFromBin(long hwnd, long addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<VtFakeWriteDataAddrFromBinDelegate>("VtFakeWriteDataAddrFromBin");
            return func(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 卸载伪造内存
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtUnFakeMemoryAddr(long hwnd, long addr){
            var func = OLAPlugDLLHelper.GetFunction<VtUnFakeMemoryAddrDelegate>("VtUnFakeMemoryAddr");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 卸载伪造内存
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VtUnFakeMemory(long hwnd, string addr){
            var func = OLAPlugDLLHelper.GetFunction<VtUnFakeMemoryDelegate>("VtUnFakeMemory");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 开启高级保护
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectEnableDriver(){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectEnableDriverDelegate>("VipProtectEnableDriver");
            return func(OLAObject);
        }

        /// <summary>
        /// 关闭高级保护
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectDisableDriver(){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectDisableDriverDelegate>("VipProtectDisableDriver");
            return func(OLAObject);
        }

        /// <summary>
        /// 添加保护
        /// </summary>
        /// <param name="pid">需要保护的进程ID</param>
        /// <param name="path">需要保护的文件或文件夹路径</param>
        /// <param name="mode">保护模式：1-允许白名单进程访问, 2-禁止全部访问, 3-禁止黑名单进程访问,4-允许白名单文件路径访问, 5-禁止黑名单文件路径访问</param>
        /// <param name="permission">保护权限：位标志组合，VIP_PERMISSION_BLOCK_OPEN |VIP_PERMISSION_HIDE_INFORMATION | VIP_PERMISSION_BLOCK_MEMORY | VIP_PERMISSION_BLOCK_WINDOWS</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectAddProtect(long pid, string path, int mode, int permission){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectAddProtectDelegate>("VipProtectAddProtect");
            return func(OLAObject, pid, path, mode, permission);
        }

        /// <summary>
        /// 移除保护
        /// </summary>
        /// <param name="pid">需要移除保护的进程ID</param>
        /// <param name="path">需要移除保护的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectRemoveProtect(long pid, string path){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectRemoveProtectDelegate>("VipProtectRemoveProtect");
            return func(OLAObject, pid, path);
        }

        /// <summary>
        /// 清空所有保护
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectClearAll(){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectClearAllDelegate>("VipProtectClearAll");
            return func(OLAObject);
        }

        /// <summary>
        /// 添加白名单
        /// </summary>
        /// <param name="pid">需要添加白名单的进程ID</param>
        /// <param name="path">需要添加白名单的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectAddWhitelist(long pid, string path){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectAddWhitelistDelegate>("VipProtectAddWhitelist");
            return func(OLAObject, pid, path);
        }

        /// <summary>
        /// 移除白名单
        /// </summary>
        /// <param name="pid">需要移除白名单的进程ID</param>
        /// <param name="path">需要移除白名单的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectRemoveWhitelist(long pid, string path){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectRemoveWhitelistDelegate>("VipProtectRemoveWhitelist");
            return func(OLAObject, pid, path);
        }

        /// <summary>
        /// 清空白名单
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectClearWhitelist(){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectClearWhitelistDelegate>("VipProtectClearWhitelist");
            return func(OLAObject);
        }

        /// <summary>
        /// 添加黑名单
        /// </summary>
        /// <param name="pid">需要添加黑名单的进程ID</param>
        /// <param name="path">需要添加黑名单的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectAddBlacklist(long pid, string path){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectAddBlacklistDelegate>("VipProtectAddBlacklist");
            return func(OLAObject, pid, path);
        }

        /// <summary>
        /// 移除黑名单
        /// </summary>
        /// <param name="pid">需要移除黑名单的进程ID</param>
        /// <param name="path">需要移除黑名单的文件或文件夹路径</param>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectRemoveBlacklist(long pid, string path){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectRemoveBlacklistDelegate>("VipProtectRemoveBlacklist");
            return func(OLAObject, pid, path);
        }

        /// <summary>
        /// 清空黑名单
        /// </summary>
        /// <returns>1成功 其他失败</returns>
        public int VipProtectClearBlacklist(){
            var func = OLAPlugDLLHelper.GetFunction<VipProtectClearBlacklistDelegate>("VipProtectClearBlacklist");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<GenerateRSAKeyDelegate>("GenerateRSAKey");
            return func(OLAObject, publicKeyPath, privateKeyPath, type, keySize);
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
            var func = OLAPlugDLLHelper.GetFunction<ConvertRSAPublicKeyDelegate>("ConvertRSAPublicKey");
            return PtrToStringUTF8(func(OLAObject, publicKey, inputType, outputType));
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
            var func = OLAPlugDLLHelper.GetFunction<ConvertRSAPrivateKeyDelegate>("ConvertRSAPrivateKey");
            return PtrToStringUTF8(func(OLAObject, privateKey, inputType, outputType));
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
            var func = OLAPlugDLLHelper.GetFunction<EncryptWithRsaDelegate>("EncryptWithRsa");
            return PtrToStringUTF8(func(OLAObject, message, publicKey, paddingType));
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
            var func = OLAPlugDLLHelper.GetFunction<DecryptWithRsaDelegate>("DecryptWithRsa");
            return PtrToStringUTF8(func(OLAObject, cipher, privateKey, paddingType));
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
            var func = OLAPlugDLLHelper.GetFunction<SignWithRsaDelegate>("SignWithRsa");
            return PtrToStringUTF8(func(OLAObject, message, privateCer, shaType, paddingType));
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
        /// <returns>验证结果
        ///<br/>0: 验证失败
        ///<br/>1: 验证成功
        /// </returns>
        public int VerifySignWithRsa(string message, string signature, int shaType, int paddingType, string publicCer){
            var func = OLAPlugDLLHelper.GetFunction<VerifySignWithRsaDelegate>("VerifySignWithRsa");
            return func(OLAObject, message, signature, shaType, paddingType, publicCer);
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
            var func = OLAPlugDLLHelper.GetFunction<AESEncryptDelegate>("AESEncrypt");
            return PtrToStringUTF8(func(OLAObject, source, key));
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
            var func = OLAPlugDLLHelper.GetFunction<AESDecryptDelegate>("AESDecrypt");
            return PtrToStringUTF8(func(OLAObject, source, key));
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
            var func = OLAPlugDLLHelper.GetFunction<AESEncryptExDelegate>("AESEncryptEx");
            return PtrToStringUTF8(func(OLAObject, source, key, iv, mode, paddingType));
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
            var func = OLAPlugDLLHelper.GetFunction<AESDecryptExDelegate>("AESDecryptEx");
            return PtrToStringUTF8(func(OLAObject, source, key, iv, mode, paddingType));
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
            var func = OLAPlugDLLHelper.GetFunction<MD5EncryptDelegate>("MD5Encrypt");
            return PtrToStringUTF8(func(OLAObject, source));
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
            var func = OLAPlugDLLHelper.GetFunction<SHAHashDelegate>("SHAHash");
            return PtrToStringUTF8(func(OLAObject, source, shaType));
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
            var func = OLAPlugDLLHelper.GetFunction<HMACDelegate>("HMAC");
            return PtrToStringUTF8(func(OLAObject, source, key, shaType));
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
            var func = OLAPlugDLLHelper.GetFunction<GenerateRandomBytesDelegate>("GenerateRandomBytes");
            return PtrToStringUTF8(func(OLAObject, length, type));
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
            var func = OLAPlugDLLHelper.GetFunction<GenerateGuidDelegate>("GenerateGuid");
            return PtrToStringUTF8(func(OLAObject, type));
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
            var func = OLAPlugDLLHelper.GetFunction<Base64EncodeDelegate>("Base64Encode");
            return PtrToStringUTF8(func(OLAObject, source));
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
            var func = OLAPlugDLLHelper.GetFunction<Base64DecodeDelegate>("Base64Decode");
            return PtrToStringUTF8(func(OLAObject, source));
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
            var func = OLAPlugDLLHelper.GetFunction<PBKDF2Delegate>("PBKDF2");
            return PtrToStringUTF8(func(OLAObject, password, salt, iterations, keyLength, shaType));
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
            var func = OLAPlugDLLHelper.GetFunction<MD5FileDelegate>("MD5File");
            return PtrToStringUTF8(func(OLAObject, filePath));
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
            var func = OLAPlugDLLHelper.GetFunction<SHAFileDelegate>("SHAFile");
            return PtrToStringUTF8(func(OLAObject, filePath, shaType));
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int CreateFolder(string path){
            var func = OLAPlugDLLHelper.GetFunction<CreateFolderDelegate>("CreateFolder");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DeleteFolder(string path){
            var func = OLAPlugDLLHelper.GetFunction<DeleteFolderDelegate>("DeleteFolder");
            return func(OLAObject, path);
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
            var func = OLAPlugDLLHelper.GetFunction<GetFolderListDelegate>("GetFolderList");
            return PtrToStringUTF8(func(OLAObject, path, baseDir));
        }

        /// <summary>
        /// 判断文件夹是否存在
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int IsDirectory(string path){
            var func = OLAPlugDLLHelper.GetFunction<IsDirectoryDelegate>("IsDirectory");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int IsFile(string path){
            var func = OLAPlugDLLHelper.GetFunction<IsFileDelegate>("IsFile");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int CreateFile(string path){
            var func = OLAPlugDLLHelper.GetFunction<CreateFileDelegate>("CreateFile");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int DeleteFile(string path){
            var func = OLAPlugDLLHelper.GetFunction<DeleteFileDelegate>("DeleteFile");
            return func(OLAObject, path);
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int CopyFile(string src, string dst){
            var func = OLAPlugDLLHelper.GetFunction<CopyFileDelegate>("CopyFile");
            return func(OLAObject, src, dst);
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int MoveFile(string src, string dst){
            var func = OLAPlugDLLHelper.GetFunction<MoveFileDelegate>("MoveFile");
            return func(OLAObject, src, dst);
        }

        /// <summary>
        /// 重命名文件
        /// </summary>
        /// <param name="src">源文件路径</param>
        /// <param name="dst">目标文件路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        /// </returns>
        public int RenameFile(string src, string dst){
            var func = OLAPlugDLLHelper.GetFunction<RenameFileDelegate>("RenameFile");
            return func(OLAObject, src, dst);
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件大小，失败返回0</returns>
        public long GetFileSize(string path){
            var func = OLAPlugDLLHelper.GetFunction<GetFileSizeDelegate>("GetFileSize");
            return func(OLAObject, path);
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
            var func = OLAPlugDLLHelper.GetFunction<GetFileListDelegate>("GetFileList");
            return PtrToStringUTF8(func(OLAObject, path, baseDir));
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
            var func = OLAPlugDLLHelper.GetFunction<GetFileNameDelegate>("GetFileName");
            return PtrToStringUTF8(func(OLAObject, path, withExtension));
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
            var func = OLAPlugDLLHelper.GetFunction<ToAbsolutePathDelegate>("ToAbsolutePath");
            return PtrToStringUTF8(func(OLAObject, path));
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
            var func = OLAPlugDLLHelper.GetFunction<ToRelativePathDelegate>("ToRelativePath");
            return PtrToStringUTF8(func(OLAObject, path));
        }

        /// <summary>
        /// 判断文件/目录是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否存在，失败返回0</returns>
        public int FileOrDirectoryExists(string path){
            var func = OLAPlugDLLHelper.GetFunction<FileOrDirectoryExistsDelegate>("FileOrDirectoryExists");
            return func(OLAObject, path);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadFileStringDelegate>("ReadFileString");
            return PtrToStringUTF8(func(OLAObject, filePath, encoding));
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
            var func = OLAPlugDLLHelper.GetFunction<ReadBytesFromFileDelegate>("ReadBytesFromFile");
            return func(OLAObject, filePath, offset, size);
        }

        /// <summary>
        /// 将字节流写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="dataAddr">数据地址</param>
        /// <param name="dataSize">数据大小</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteBytesToFile(string filePath, long dataAddr, int dataSize){
            var func = OLAPlugDLLHelper.GetFunction<WriteBytesToFileDelegate>("WriteBytesToFile");
            return func(OLAObject, filePath, dataAddr, dataSize);
        }

        /// <summary>
        /// 将字符串写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="data">数据</param>
        /// <param name="encoding">编码</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteStringToFile(string filePath, string data, int encoding){
            var func = OLAPlugDLLHelper.GetFunction<WriteStringToFileDelegate>("WriteStringToFile");
            return func(OLAObject, filePath, data, encoding);
        }

        /// <summary>
        /// 启动全局钩子
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int StartHotkeyHook(){
            var func = OLAPlugDLLHelper.GetFunction<StartHotkeyHookDelegate>("StartHotkeyHook");
            return func(OLAObject);
        }

        /// <summary>
        /// 停止全局钩子
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int StopHotkeyHook(){
            var func = OLAPlugDLLHelper.GetFunction<StopHotkeyHookDelegate>("StopHotkeyHook");
            return func(OLAObject);
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="keycode">按键码</param>
        /// <param name="modifiers">修饰键组合，使用Modifier枚举值的位或组合，比如按下Ctrl+Alt modifiers:2+8=10enum 0 无掩码enum 1 左Shift键掩码enum 2 左Ctrl键掩码enum 4 左Meta键掩码enum 8 左Alt键掩码enum 16 右Shift键掩码enum 32 右Ctrl键掩码enum 64 右Meta键掩码enum 128 右Alt键掩码</param>
        /// <param name="callback">回调函数 int HotKeyCallback(int keycode, int modifiers) 参考接口参数定义</param>
        /// <returns>注册监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册键盘快捷键监听,可监听单个按键、组合键等，同一组按键只能创建一个监听
        /// <br/>2. 注册键盘快捷键监听前需要调用StartHotkeyHook安装键盘鼠标钩子
        /// <br/>3. 回调函数 int HotKeyCallback(int keycode, intmodifiers)，参考接口参数定义，回1阻断消息传递，keycode传0可以监听所有按键信息
        /// <br/>4. 参考windows函数 SetWindowsHookExW 实现
        /// </remarks>
        public int RegisterHotkey(int keycode, int modifiers, HotkeyCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<RegisterHotkeyDelegate>("RegisterHotkey");
            return func(OLAObject, keycode, modifiers, callback);
        }

        /// <summary>
        /// 注销热键
        /// </summary>
        /// <param name="keycode">按键码</param>
        /// <param name="modifiers">修饰键组合，使用Modifier枚举值的位或组合，比如按下Ctrl+Alt modifiers:2+8=10enum 1 左Shift键掩码enum 2 左Ctrl键掩码enum 4 左Meta键掩码enum 8 左Alt键掩码enum 16 右Shift键掩码enum 32 右Ctrl键掩码enum 64 右Meta键掩码enum 128 右Alt键掩码</param>
        /// <returns>卸载监听状态</returns>
        public int UnregisterHotkey(int keycode, int modifiers){
            var func = OLAPlugDLLHelper.GetFunction<UnregisterHotkeyDelegate>("UnregisterHotkey");
            return func(OLAObject, keycode, modifiers);
        }

        /// <summary>
        /// 注册鼠标按钮事件
        /// </summary>
        /// <param name="button">按键类型enum 1 鼠标左键enum 2 鼠标右键enum 3 鼠标中键enum 4 拓展键1enum 5 拓展键2</param>
        /// <param name="type">按键状态，使用Modifier枚举值的位或组合enum 0 鼠标点击enum 1 鼠标按下enum 2 鼠标释放</param>
        /// <param name="callback">回调函数 void MouseCallback(int button,int x, int y, int clicks)</param>
        /// <returns>注册监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册鼠标快捷键监听前需要调用StartHotkeyHook安装键盘鼠标钩子
        /// <br/>2. 回调函数 void MouseCallback(int button,int x, int y, int clicks)button 参考参数定义x X坐标y Y坐标clicks 点击次数
        /// <br/>3. 参考windows函数 SetWindowsHookExW 实现
        /// </remarks>
        public int RegisterMouseButton(int button, int type, MouseCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<RegisterMouseButtonDelegate>("RegisterMouseButton");
            return func(OLAObject, button, type, callback);
        }

        /// <summary>
        /// 注销鼠标按钮事件
        /// </summary>
        /// <param name="button">按键类型enum 1 鼠标左键enum 2 鼠标右键enum 3 鼠标中键enum 4 拓展键1enum 5 拓展键2</param>
        /// <param name="type">按键状态，使用Modifier枚举值的位或组合enum 0 鼠标点击enum 1 鼠标按下enum 2 鼠标释放</param>
        /// <returns>卸载监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int UnregisterMouseButton(int button, int type){
            var func = OLAPlugDLLHelper.GetFunction<UnregisterMouseButtonDelegate>("UnregisterMouseButton");
            return func(OLAObject, button, type);
        }

        /// <summary>
        /// 注册鼠标滚轮事件
        /// </summary>
        /// <param name="callback">回调函数 void MouseWheelCallback(int x, int y, int amount, int rotation)</param>
        /// <returns>注册监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册鼠标快捷键监听前需要调用StartHotkeyHook安装键盘鼠标钩子
        /// <br/>2. 回调函数 void MouseWheelCallback(int x, int y, int amount, int rotation) 参数定义x 鼠标X坐标y 鼠标Y坐标amount 滚动量rotation 滚动方向
        /// <br/>3. 参考windows函数 SetWindowsHookExW 实现
        /// </remarks>
        public int RegisterMouseWheel(MouseWheelCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<RegisterMouseWheelDelegate>("RegisterMouseWheel");
            return func(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标滚轮事件
        /// </summary>
        /// <returns>卸载监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int UnregisterMouseWheel(){
            var func = OLAPlugDLLHelper.GetFunction<UnregisterMouseWheelDelegate>("UnregisterMouseWheel");
            return func(OLAObject);
        }

        /// <summary>
        /// 注册鼠标移动事件
        /// </summary>
        /// <param name="callback">回调函数 void MouseMoveCallback(int x, int y)</param>
        /// <returns>注册监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册鼠标快捷键监听前需要调用StartHotkeyHook安装键盘鼠标钩子
        /// <br/>2. 回调函数 void MouseMoveCallback(int x, int y) 参数定义x 鼠标X坐标y 鼠标Y坐标
        /// <br/>3. 参考windows函数 SetWindowsHookExW 实现
        /// </remarks>
        public int RegisterMouseMove(MouseMoveCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<RegisterMouseMoveDelegate>("RegisterMouseMove");
            return func(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标移动事件
        /// </summary>
        /// <returns>卸载监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int UnregisterMouseMove(){
            var func = OLAPlugDLLHelper.GetFunction<UnregisterMouseMoveDelegate>("UnregisterMouseMove");
            return func(OLAObject);
        }

        /// <summary>
        /// 注册鼠标拖动事件
        /// </summary>
        /// <param name="callback">回调函数 void MouseDragCallback(int x, int y)</param>
        /// <returns>注册监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 注册鼠标快捷键监听前需要调用StartHotkeyHook安装键盘鼠标钩子
        /// <br/>2. 回调函数 void MouseDragCallback(int x, int y) 参数定义x 鼠标X坐标y 鼠标Y坐标
        /// <br/>3. 参考windows函数 SetWindowsHookExW 实现
        /// </remarks>
        public int RegisterMouseDrag(MouseDragCallback callback){
            var func = OLAPlugDLLHelper.GetFunction<RegisterMouseDragDelegate>("RegisterMouseDrag");
            return func(OLAObject, callback);
        }

        /// <summary>
        /// 注销鼠标拖动事件
        /// </summary>
        /// <returns>卸载监听状态
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int UnregisterMouseDrag(){
            var func = OLAPlugDLLHelper.GetFunction<UnregisterMouseDragDelegate>("UnregisterMouseDrag");
            return func(OLAObject);
        }

        /// <summary>
        /// 注入DLL
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="dll_path">DLL文件的完整路径</param>
        /// <param name="type">注入类型
        ///<br/> 1: 标准注入(CreateRemoteThread)
        ///<br/> 2: 驱动注入模式1
        ///<br/> 3: 驱动注入模式2
        ///<br/> 4: 驱动注入模式
        /// </param>
        /// <param name="bypassGuard">是否绕过保护
        ///<br/> 0: 不绕过
        ///<br/> 1: 尝试绕过常见反注入保护
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL文件必须存在且路径正确
        /// <br/>2. 目标进程必须有足够的权限允许注入
        /// <br/>3. 不同注入类型的成功率和兼容性可能不同
        /// <br/>4. 标准注入(type=0)最稳定,但容易被检测
        /// <br/>5. 手动映射注入(type=3)隐蔽性最好,但兼容性较差
        /// <br/>6. 绕过保护选项可能无法对抗所有反注入机制
        /// <br/>7. 注入系统进程或受保护进程需要管理员权限
        /// <br/>8. 32位进程只能注入32位DLL,64位进程只能注入64位DLL
        /// <br/>9. 建议在注入前确认DLL的架构与目标进程匹配
        /// <br/>10. 注入失败可能导致目标进程崩溃,请谨慎使用
        /// <br/>11. 某些杀毒软件可能会拦截DLL注入操作
        /// </remarks>
        public int Inject(long hwnd, string dll_path, int type, int bypassGuard){
            var func = OLAPlugDLLHelper.GetFunction<InjectDelegate>("Inject");
            return func(OLAObject, hwnd, dll_path, type, bypassGuard);
        }

        /// <summary>
        /// 从网络URL下载DLL文件并注入到指定窗口进程,支持远程注入场景。(部分模式文件会落盘)
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="url">DLL文件的下载URL地址</param>
        /// <param name="type">注入类型
        ///<br/> 1: 标准注入(CreateRemoteThread)
        ///<br/> 2: 驱动注入模式1
        ///<br/> 3: 驱动注入模式2
        ///<br/> 4: 驱动注入模式
        /// </param>
        /// <param name="bypassGuard">是否绕过保护
        ///<br/> 0: 不绕过
        ///<br/> 1: 尝试绕过常见反注入保护
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. URL必须可访问且指向有效的DLL文件
        /// <br/>2. 需要网络连接,下载可能需要一定时间
        /// <br/>3. 下载的DLL会临时保存到本地再进行注入
        /// <br/>4. 建议使用HTTPS协议确保传输安全
        /// <br/>5. 下载失败或DLL损坏会导致注入失败
        /// <br/>6. 防火墙或杀毒软件可能会拦截下载
        /// <br/>7. 下载的临时文件会在注入后清理
        /// <br/>8. 目标进程必须有足够的权限允许注入
        /// <br/>9. 不同注入类型的成功率和兼容性可能不同
        /// <br/>10. 32位进程只能注入32位DLL,64位进程只能注入64位DLL
        /// <br/>11. 注入系统进程或受保护进程需要管理员权限
        /// <br/>12. 某些网络环境可能不支持直接下载可执行文件
        /// <br/>13. 建议验证下载文件的完整性和来源安全性
        /// </remarks>
        public int InjectFromUrl(long hwnd, string url, int type, int bypassGuard){
            var func = OLAPlugDLLHelper.GetFunction<InjectFromUrlDelegate>("InjectFromUrl");
            return func(OLAObject, hwnd, url, type, bypassGuard);
        }

        /// <summary>
        /// 从内存缓冲区直接注入DLL到指定窗口进程,无需落地文件,隐蔽性最强。(部分模式文件会落盘)
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="bufferAddr">DLL数据在内存中的起始地址</param>
        /// <param name="bufferSize">DLL数据的大小(字节)</param>
        /// <param name="type">注入类型
        ///<br/> 1: 标准注入(CreateRemoteThread)
        ///<br/> 2: 驱动注入模式1
        ///<br/> 3: 驱动注入模式2
        ///<br/> 4: 驱动注入模式
        /// </param>
        /// <param name="bypassGuard">是否绕过保护
        ///<br/> 0: 不绕过
        ///<br/> 1: 尝试绕过常见反注入保护
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL数据必须完整且有效,缓冲区不能损坏
        /// <br/>2. 内存注入无需落地文件,隐蔽性最强
        /// <br/>3. 推荐使用手动映射注入(type=3)以获得最佳兼容性
        /// <br/>4. 标准注入(type=0)可能无法从内存加载
        /// <br/>5. 确保bufferAddr指向的内存在注入完成前保持有效
        /// <br/>6. 注入完成后可以立即释放bufferAddr指向的内存
        /// <br/>7. 目标进程必须有足够的权限允许注入
        /// <br/>8. 32位进程只能注入32位DLL,64位进程只能注入64位DLL
        /// <br/>9. 注入系统进程或受保护进程需要管理员权限
        /// <br/>10. 内存注入可以有效规避部分文件监控类反注入
        /// <br/>11. 某些杀毒软件的内存扫描仍可能检测到注入行为
        /// <br/>12. 建议对DLL数据进行加密,在注入前解密以提高隐蔽性
        /// <br/>13. bufferSize必须与实际DLL文件大小完全一致
        /// </remarks>
        public int InjectFromBuffer(long hwnd, long bufferAddr, int bufferSize, int type, int bypassGuard){
            var func = OLAPlugDLLHelper.GetFunction<InjectFromBufferDelegate>("InjectFromBuffer");
            return func(OLAObject, hwnd, bufferAddr, bufferSize, type, bypassGuard);
        }

        /// <summary>
        /// 创建空的JSON对象
        /// </summary>
        /// <returns>返回新创建的JSON对象句柄，失败时返回0</returns>
        public long JsonCreateObject(){
            var func = OLAPlugDLLHelper.GetFunction<JsonCreateObjectDelegate>("JsonCreateObject");
            return func();
        }

        /// <summary>
        /// 创建空的JSON数组
        /// </summary>
        /// <returns>返回新创建的JSON数组句柄，失败时返回0</returns>
        public long JsonCreateArray(){
            var func = OLAPlugDLLHelper.GetFunction<JsonCreateArrayDelegate>("JsonCreateArray");
            return func();
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
            var func = OLAPlugDLLHelper.GetFunction<JsonParseDelegate>("JsonParse");
            return func(str, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonStringifyDelegate>("JsonStringify");
            return PtrToStringUTF8(func(obj, indent, out err));
        }

        /// <summary>
        /// 释放JSON对象
        /// </summary>
        /// <param name="obj">要释放的JSON对象句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int JsonFree(long obj){
            var func = OLAPlugDLLHelper.GetFunction<JsonFreeDelegate>("JsonFree");
            return func(obj);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetValueDelegate>("JsonGetValue");
            return func(obj, key, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetArrayItemDelegate>("JsonGetArrayItem");
            return func(arr, index, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetStringDelegate>("JsonGetString");
            return PtrToStringUTF8(func(obj, key, out err));
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetNumberDelegate>("JsonGetNumber");
            return func(obj, key, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetBoolDelegate>("JsonGetBool");
            return func(obj, key, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonGetSizeDelegate>("JsonGetSize");
            return func(obj, out err);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonSetValueDelegate>("JsonSetValue");
            return func(obj, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonArrayAppendDelegate>("JsonArrayAppend");
            return func(arr, value);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonSetStringDelegate>("JsonSetString");
            return func(obj, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonSetNumberDelegate>("JsonSetNumber");
            return func(obj, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonSetBoolDelegate>("JsonSetBool");
            return func(obj, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonDeleteKeyDelegate>("JsonDeleteKey");
            return func(obj, key);
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
            var func = OLAPlugDLLHelper.GetFunction<JsonClearDelegate>("JsonClear");
            return func(obj);
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
            var func = OLAPlugDLLHelper.GetFunction<ParseMatchImageJsonDelegate>("ParseMatchImageJson");
            return func(str, out matchState, out x, out y, out width, out height, out matchVal, out angle, out index);
        }

        /// <summary>
        /// 获取匹配图像JSON数量
        /// </summary>
        /// <param name="str">匹配图像JSON字符串</param>
        /// <returns>返回匹配图像JSON数量</returns>
        public int GetMatchImageAllCount(string str){
            var func = OLAPlugDLLHelper.GetFunction<GetMatchImageAllCountDelegate>("GetMatchImageAllCount");
            return func(str);
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
            var func = OLAPlugDLLHelper.GetFunction<ParseMatchImageAllJsonDelegate>("ParseMatchImageAllJson");
            return func(str, parseIndex, out matchState, out x, out y, out width, out height, out matchVal, out angle, out index);
        }

        /// <summary>
        /// 对插件部分接口的返回值进行解析,并返回result中的元素个数,针对JSON格式和,|分割的字符串
        /// </summary>
        /// <param name="resultStr">(字符串): 插件接口的返回值。</param>
        /// <returns>整型数: result中的元素个数。</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数用于对插件部分接口的返回值进行解析,并返回result中的元素个数。
        /// </remarks>
        public int GetResultCount(string resultStr){
            var func = OLAPlugDLLHelper.GetFunction<GetResultCountDelegate>("GetResultCount");
            return func(resultStr);
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
            var func = OLAPlugDLLHelper.GetFunction<GenerateMouseTrajectoryDelegate>("GenerateMouseTrajectory");
            var result = PtrToStringUTF8(func(OLAObject, startX, startY, endX, endY));
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
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyDown(int vk_code){
            var func = OLAPlugDLLHelper.GetFunction<KeyDownDelegate>("KeyDown");
            return func(OLAObject, vk_code);
        }

        /// <summary>
        /// 弹起来虚拟键vk_code
        /// </summary>
        /// <param name="vk_code">按键码</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyUp(int vk_code){
            var func = OLAPlugDLLHelper.GetFunction<KeyUpDelegate>("KeyUp");
            return func(OLAObject, vk_code);
        }

        /// <summary>
        /// 按下指定的虚拟键码
        /// </summary>
        /// <param name="vk_code">按键码</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyPress(int vk_code){
            var func = OLAPlugDLLHelper.GetFunction<KeyPressDelegate>("KeyPress");
            return func(OLAObject, vk_code);
        }

        /// <summary>
        /// 按住鼠标左键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int LeftDown(){
            var func = OLAPlugDLLHelper.GetFunction<LeftDownDelegate>("LeftDown");
            return func(OLAObject);
        }

        /// <summary>
        /// 弹起鼠标左键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int LeftUp(){
            var func = OLAPlugDLLHelper.GetFunction<LeftUpDelegate>("LeftUp");
            return func(OLAObject);
        }

        /// <summary>
        /// 把鼠标移动到目的点(x, y)
        /// </summary>
        /// <param name="x">目标X坐标</param>
        /// <param name="y">目标Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int MoveTo(int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<MoveToDelegate>("MoveTo");
            return func(OLAObject, x, y);
        }

        /// <summary>
        /// 把鼠标移动到目的点(x, y),不使用鼠标轨迹,即使开启鼠标轨迹这个接口也不会生效
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int MoveToWithoutSimulator(int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<MoveToWithoutSimulatorDelegate>("MoveToWithoutSimulator");
            return func(OLAObject, x, y);
        }

        /// <summary>
        /// 执行鼠标右键点击操作
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数执行完整的右键点击操作（按下并释放）
        /// <br/>2. 如果需要单独控制按下和释放，请使用 RightDown 和 RightUp 函数
        /// <br/>3. 点击操作会使用当前鼠标位置
        /// <br/>4. 如果需要移动到特定位置后点击，请先使用 MoveTo 函数
        /// <br/>5. 在调用此函数前，确保鼠标右键未被其他程序占用
        /// </remarks>
        public int RightClick(){
            var func = OLAPlugDLLHelper.GetFunction<RightClickDelegate>("RightClick");
            return func(OLAObject);
        }

        /// <summary>
        /// 鼠标右键双击
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int RightDoubleClick(){
            var func = OLAPlugDLLHelper.GetFunction<RightDoubleClickDelegate>("RightDoubleClick");
            return func(OLAObject);
        }

        /// <summary>
        /// 按住鼠标右键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int RightDown(){
            var func = OLAPlugDLLHelper.GetFunction<RightDownDelegate>("RightDown");
            return func(OLAObject);
        }

        /// <summary>
        /// 弹起鼠标右键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int RightUp(){
            var func = OLAPlugDLLHelper.GetFunction<RightUpDelegate>("RightUp");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<GetCursorShapeDelegate>("GetCursorShape");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 获取鼠标图标
        /// </summary>
        /// <returns>OLAImage对象的地址</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 图片使用完后需要调用 FreeImagePtr 接口进行释放
        /// </remarks>
        public long GetCursorImage(){
            var func = OLAPlugDLLHelper.GetFunction<GetCursorImageDelegate>("GetCursorImage");
            return func(OLAObject);
        }

        /// <summary>
        /// 根据指定的字符串序列，依次按顺序按下其中的字符
        /// </summary>
        /// <param name="keyStr">需要按下的字符串序列. 比如"1234","abcd","7389,1462"等</param>
        /// <param name="delay">每按下一个按键，需要延时多久。单位毫秒（ms），这个值越大，按的速度越慢</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在某些情况下，SendString和SendString2都无法输入文字时，可以考虑用这个来输入
        /// <br/>2. 但这个接口只支持"a-z 0-9 ~-=[];',./"和空格,其它字符一律不支持.(包括中国)
        /// </remarks>
        public int KeyPressStr(string keyStr, int delay){
            var func = OLAPlugDLLHelper.GetFunction<KeyPressStrDelegate>("KeyPressStr");
            return func(OLAObject, keyStr, delay);
        }

        /// <summary>
        /// 发送字符串到指定窗口
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="str">字符串</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int SendString(long hwnd, string str){
            var func = OLAPlugDLLHelper.GetFunction<SendStringDelegate>("SendString");
            return func(OLAObject, hwnd, str);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SendStringEx(long hwnd, long addr, int len, int type){
            var func = OLAPlugDLLHelper.GetFunction<SendStringExDelegate>("SendStringEx");
            return func(OLAObject, hwnd, addr, len, type);
        }

        /// <summary>
        /// 按下指定的虚拟键码keyStr
        /// </summary>
        /// <param name="keyStr">按键字符</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyPressChar(string keyStr){
            var func = OLAPlugDLLHelper.GetFunction<KeyPressCharDelegate>("KeyPressChar");
            return func(OLAObject, keyStr);
        }

        /// <summary>
        /// 按住指定的虚拟键码keyStr
        /// </summary>
        /// <param name="keyStr">按键字符</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyDownChar(string keyStr){
            var func = OLAPlugDLLHelper.GetFunction<KeyDownCharDelegate>("KeyDownChar");
            return func(OLAObject, keyStr);
        }

        /// <summary>
        /// 弹起来虚拟键keyStr
        /// </summary>
        /// <param name="keyStr">按键字符</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int KeyUpChar(string keyStr){
            var func = OLAPlugDLLHelper.GetFunction<KeyUpCharDelegate>("KeyUpChar");
            return func(OLAObject, keyStr);
        }

        /// <summary>
        /// 鼠标相对于上次的位置移动rx, ry, 前台模式鼠标相对移动时相对当前鼠标位置
        /// </summary>
        /// <param name="rx">相对于上次的X偏移</param>
        /// <param name="ry">相对于上次的Y偏移</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int MoveR(int rx, int ry){
            var func = OLAPlugDLLHelper.GetFunction<MoveRDelegate>("MoveR");
            return func(OLAObject, rx, ry);
        }

        /// <summary>
        /// 滚轮点击
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int MiddleClick(){
            var func = OLAPlugDLLHelper.GetFunction<MiddleClickDelegate>("MiddleClick");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<MoveToExDelegate>("MoveToEx");
            return PtrToStringUTF8(func(OLAObject, x, y, w, h));
        }

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        /// <param name="x">返回的鼠标X坐标</param>
        /// <param name="y">返回的鼠标Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此接口绑定后使用，获取的是相当游戏窗口的鼠标坐标
        /// </remarks>
        public int GetCursorPos(out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<GetCursorPosDelegate>("GetCursorPos");
            return func(OLAObject, out x, out y);
        }

        /// <summary>
        /// 弹起鼠标中键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int MiddleUp(){
            var func = OLAPlugDLLHelper.GetFunction<MiddleUpDelegate>("MiddleUp");
            return func(OLAObject);
        }

        /// <summary>
        /// 按住鼠标中键
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数仅模拟按下中键，不会自动释放
        /// <br/>2. 如果需要释放中键，需要调用 MiddleUp 函数
        /// <br/>3. 建议在操作完成后及时释放中键，避免影响后续操作
        /// <br/>4. 如果系统不支持中键操作，函数将返回失败
        /// <br/>5. 在调用此函数前，确保鼠标中键未被其他程序占用
        /// </remarks>
        public int MiddleDown(){
            var func = OLAPlugDLLHelper.GetFunction<MiddleDownDelegate>("MiddleDown");
            return func(OLAObject);
        }

        /// <summary>
        /// 滚轮双击
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 此函数执行完整的鼠标中键双击操作（按下并释放）
        /// <br/>2. 如果需要单独控制按下和释放，请使用 MiddleDown 和 MiddleUp 函数
        /// <br/>3. 点击操作会使用当前鼠标位置
        /// <br/>4. 如果需要移动到特定位置后点击，请先使用 MoveTo 函数
        /// <br/>5. 在调用此函数前，确保鼠标中键未被其他程序占用
        /// </remarks>
        public int MiddleDoubleClick(){
            var func = OLAPlugDLLHelper.GetFunction<MiddleDoubleClickDelegate>("MiddleDoubleClick");
            return func(OLAObject);
        }

        /// <summary>
        /// 鼠标左键点击
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int LeftClick(){
            var func = OLAPlugDLLHelper.GetFunction<LeftClickDelegate>("LeftClick");
            return func(OLAObject);
        }

        /// <summary>
        /// 鼠标左键双击
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int LeftDoubleClick(){
            var func = OLAPlugDLLHelper.GetFunction<LeftDoubleClickDelegate>("LeftDoubleClick");
            return func(OLAObject);
        }

        /// <summary>
        /// 滚轮向上滚
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int WheelUp(){
            var func = OLAPlugDLLHelper.GetFunction<WheelUpDelegate>("WheelUp");
            return func(OLAObject);
        }

        /// <summary>
        /// 滚轮向下滚
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败@eunm 1 成功
        /// </returns>
        public int WheelDown(){
            var func = OLAPlugDLLHelper.GetFunction<WheelDownDelegate>("WheelDown");
            return func(OLAObject);
        }

        /// <summary>
        /// 等待指定的按键按下 (前台,不是后台)
        /// </summary>
        /// <param name="vk_code">等待的按键码</param>
        /// <param name="time_out">等待超时时间，单位毫秒</param>
        /// <returns>等待结果
        ///<br/>0: 超时
        ///<br/>1: 指定的按键按下
        /// </returns>
        public int WaitKey(int vk_code, int time_out){
            var func = OLAPlugDLLHelper.GetFunction<WaitKeyDelegate>("WaitKey");
            return func(OLAObject, vk_code, time_out);
        }

        /// <summary>
        /// 设置当前系统鼠标的精确度开关
        /// </summary>
        /// <param name="enable">是否提高指针精确度，一般推荐关闭
        ///<br/> 0: 关闭指针精确度开关
        ///<br/> 1: 打开指针精确度开关
        /// </param>
        /// <returns>设置之前的精确度开关</returns>
        public int EnableMouseAccuracy(int enable){
            var func = OLAPlugDLLHelper.GetFunction<EnableMouseAccuracyDelegate>("EnableMouseAccuracy");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 生成鼠标渐开线随机移动轨迹
        /// </summary>
        /// <param name="startX">起点X坐标（中心点）</param>
        /// <param name="startY">起点Y坐标（中心点）</param>
        /// <param name="radius">移动半径范围（像素）</param>
        /// <param name="stepDistance">轨迹点之间的距离（像素，建议3-10，0表示自动计算为5）</param>
        /// <param name="curvature">曲率系数（0.5-2.0，越大越弯曲，默认1.0）</param>
        /// <param name="noiseAmplitude">随机扰动幅度（0-5像素，默认2.0）</param>
        /// <returns>返回JSON格式的轨迹点数据，格式：{"points":[{"x":100,"y":200},...], "count":150}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr接口释放内存
        /// <br/>2. 此函数生成在指定半径范围内的渐开线式随机游走轨迹，模拟自然的鼠标移动
        /// <br/>3. stepDistance越小轨迹越平滑但点数越多，建议值：精细3-5，标准5-8，快速8-10
        /// <br/>4. curvature控制弯曲程度，值越大轨迹越弯曲，建议范围0.5-2.0
        /// <br/>5. noiseAmplitude控制随机抖动幅度，模拟人手抖动，建议范围1.0-3.0
        /// </remarks>
        public List<Point> GenerateInvoluteMouseTrajectory(int startX, int startY, int radius, int stepDistance, double curvature, double noiseAmplitude){
            var func = OLAPlugDLLHelper.GetFunction<GenerateInvoluteMouseTrajectoryDelegate>("GenerateInvoluteMouseTrajectory");
            var result = PtrToStringUTF8(func(OLAObject, startX, startY, radius, stepDistance, curvature, noiseAmplitude));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 关闭用户日志系统并释放资源
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 可以使用位或运算组合颜色，例如：FOREGROUND_RED | FOREGROUND_GREEN = 黄色
        /// <br/>2. 添加 FOREGROUND_INTENSITY (0x08) 可以使颜色变亮
        /// <br/>3. 关闭后，下次写入日志时会自动重新初始化
        /// <br/>4. 通常在程序退出前调用，或需要完全重置日志系统时使用
        /// </remarks>
        public int LogShutdown(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogShutdownDelegate>("LogShutdown");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 设置日志文件路径
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="logFilePath">日志文件路径（为空则使用默认路径）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认路径：./user_logs/app.log（程序运行目录下）
        /// <br/>2. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetFilePath(long loggerHandle, string logFilePath){
            var func = OLAPlugDLLHelper.GetFunction<LogSetFilePathDelegate>("LogSetFilePath");
            return func(OLAObject, loggerHandle, logFilePath);
        }

        /// <summary>
        /// 设置日志格式（支持占位符）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="logPattern">日志格式（为空则使用默认格式）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认格式：[%Y-%m-%d %H:%M:%S.%e] [%^%l%$] %v
        /// <br/>2. 支持的占位符（基于 spdlog 格式）：【时间日期】%Y - 年份（4位数字，如 2026）%y - 年份（2位数字，如 26）%m - 月份（01-12）%d - 日期（01-31）%H - 小时（00-23，24小时制）%I - 小时（01-12，12小时制）%M - 分钟（00-59）%S - 秒（00-59）%e - 毫秒（000-999）%f - 微秒（000000-999999）%F - 纳秒（000000000-999999999）%p - AM/PM 标识%a - 星期简写（Mon, Tue, ...）%A - 星期全称（Monday, Tuesday, ...）%b - 月份简写（Jan, Feb, ...）%B - 月份全称（January, February, ...）%c - 日期时间（Thu Aug 23 15:35:46 2014）%D - 短日期（MM/DD/YY）%x - 日期表示（08/23/14）%X - 时间表示（15:35:46）%T - ISO 8601 时间格式（HH:MM:SS）%R - 24小时制时间（HH:MM）%z - UTC 偏移量（+0800）%Z - 时区名称（CST）%E - Unix 纪元秒数（1440351346）【日志信息】%v - 日志消息内容%l - 日志级别（TRACE, DEBUG, INFO, WARN, ERROR, CRITICAL）%L - 日志级别简写（T, D, I, W, E, C）%n - 日志记录器名称%t - 线程ID%P - 进程ID【源代码信息】%s - 源文件名%g - 源文件短名称（不含路径）%# - 源代码行号%! - 函数名【颜色控制】%^ - 颜色范围开始标记（根据日志级别自动着色）%$ - 颜色范围结束标记【特殊字符】%% - 百分号字面量%+ - spdlog 默认格式
        /// <br/>3. 示例格式："[%Y-%m-%d %H:%M:%S.%e] [%^%l%$] %v" → [2026-02-28 14:30:45.123] [INFO] 日志消息"[%T.%e] [%L] [%t] %v" → [14:30:45.123] [I] [12345] 日志消息"%Y%m%d %H:%M:%S [%l] %v" → 20260228 14:30:45 [INFO] 日志消息
        /// <br/>4. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetPattern(long loggerHandle, string logPattern){
            var func = OLAPlugDLLHelper.GetFunction<LogSetPatternDelegate>("LogSetPattern");
            return func(OLAObject, loggerHandle, logPattern);
        }

        /// <summary>
        /// 设置单个日志文件最大大小
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="maxFileSizeMb">单个日志文件最大大小（MB）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：100MB
        /// <br/>2. 当日志文件达到此大小时，会自动创建新文件（滚动日志）
        /// <br/>3. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetMaxFileSize(long loggerHandle, int maxFileSizeMb){
            var func = OLAPlugDLLHelper.GetFunction<LogSetMaxFileSizeDelegate>("LogSetMaxFileSize");
            return func(OLAObject, loggerHandle, maxFileSizeMb);
        }

        /// <summary>
        /// 设置最多保留的日志文件数量
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="maxFiles">最多保留的日志文件数量</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：10个
        /// <br/>2. 超过此数量时，最旧的日志文件会被自动删除
        /// <br/>3. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetMaxFiles(long loggerHandle, int maxFiles){
            var func = OLAPlugDLLHelper.GetFunction<LogSetMaxFilesDelegate>("LogSetMaxFiles");
            return func(OLAObject, loggerHandle, maxFiles);
        }

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="level">日志级别，见OLALogLevel</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：OLA_LOG_LEVEL_INFO (2)
        /// <br/>2. 只有大于或等于此级别的日志才会被记录
        /// <br/>3. 此设置立即生效，无需重新初始化
        /// </remarks>
        public int LogSetLevel(long loggerHandle, int level){
            var func = OLAPlugDLLHelper.GetFunction<LogSetLevelDelegate>("LogSetLevel");
            return func(OLAObject, loggerHandle, level);
        }

        /// <summary>
        /// 获取当前日志级别
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>当前日志级别，见OLALogLevel，失败返回-1</returns>
        public int LogGetLevel(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogGetLevelDelegate>("LogGetLevel");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 设置输出目标
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="targetFlags">输出目标（OLALogTarget 组合）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：FILE(1) | CONSOLE (3)
        /// <br/>2. 可以使用位或运算组合多个目标，例如：FILE | CONSOLE
        /// <br/>3. 控制台输出支持彩色显示（不同级别显示不同颜色）
        /// <br/>4. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetTarget(long loggerHandle, int targetFlags){
            var func = OLAPlugDLLHelper.GetFunction<LogSetTargetDelegate>("LogSetTarget");
            return func(OLAObject, loggerHandle, targetFlags);
        }

        /// <summary>
        /// 设置是否启用异步日志
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="enableAsync">是否启用异步日志，1 启用，0 禁用</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：1（异步模式）
        /// <br/>2. 异步模式可以提高性能，但可能在程序崩溃时丢失部分日志
        /// <br/>3. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetAsync(long loggerHandle, int enableAsync){
            var func = OLAPlugDLLHelper.GetFunction<LogSetAsyncDelegate>("LogSetAsync");
            return func(OLAObject, loggerHandle, enableAsync);
        }

        /// <summary>
        /// 设置控制台颜色模式
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="colorMode">颜色模式，见OLALogColorMode</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：OLA_LOG_COLOR_ALWAYS (1) - 始终启用彩色输出
        /// <br/>2. 默认颜色方案：TRACE - 白色DEBUG - 青色INFO - 绿色WARN - 亮黄色ERROR - 亮红色CRITICAL - 红色背景上的亮白色
        /// <br/>3. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogSetColorMode(long loggerHandle, int colorMode){
            var func = OLAPlugDLLHelper.GetFunction<LogSetColorModeDelegate>("LogSetColorMode");
            return func(OLAObject, loggerHandle, colorMode);
        }

        /// <summary>
        /// 设置指定日志级别的控制台颜色
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="level">日志级别，见OLALogLevel</param>
        /// <param name="color">控制台颜色，见OLALogConsoleColor</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 仅在控制台输出启用且颜色模式不为 NEVER 时生效
        /// <br/>2. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// <br/>3. 示例：OLALogSetLevelColor(instance, 0, OLA_LOG_LEVEL_INFO, OLA_LOG_COLOR_CYAN);
        /// </remarks>
        public int LogSetLevelColor(long loggerHandle, int level, int color){
            var func = OLAPlugDLLHelper.GetFunction<LogSetLevelColorDelegate>("LogSetLevelColor");
            return func(OLAObject, loggerHandle, level, color);
        }

        /// <summary>
        /// 重置所有日志级别颜色为默认值
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 修改后立即生效，如果日志系统已初始化，会自动重新初始化
        /// </remarks>
        public int LogResetLevelColors(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogResetLevelColorsDelegate>("LogResetLevelColors");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 设置自动刷新间隔
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="flushIntervalSeconds">自动刷新间隔（秒）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：0（每条日志立即刷新到文件）
        /// <br/>2. 设置为 0：每条日志都立即写入文件（最安全，但性能较低）
        /// <br/>3. 设置为 > 0：只有 WARN 及以上级别的日志才会立即刷新，其他日志会缓冲
        /// <br/>4. 无论设置如何，都可以手动调用 OLALogFlush 强制刷新
        /// <br/>5. 此设置立即生效，无需重新初始化
        /// </remarks>
        public int LogSetFlushInterval(long loggerHandle, int flushIntervalSeconds){
            var func = OLAPlugDLLHelper.GetFunction<LogSetFlushIntervalDelegate>("LogSetFlushInterval");
            return func(OLAObject, loggerHandle, flushIntervalSeconds);
        }

        /// <summary>
        /// 写入 TRACE 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogTrace(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogTraceDelegate>("LogTrace");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 写入 DEBUG 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogDebug(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogDebugDelegate>("LogDebug");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 写入 INFO 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogInfo(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogInfoDelegate>("LogInfo");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 写入 WARN 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogWarn(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogWarnDelegate>("LogWarn");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 写入 ERROR 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogError(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogErrorDelegate>("LogError");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 写入 CRITICAL 级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogCritical(string message){
            var func = OLAPlugDLLHelper.GetFunction<LogCriticalDelegate>("LogCritical");
            return func(OLAObject, message);
        }

        /// <summary>
        /// 立即刷新日志缓冲区到文件
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 如果 flushIntervalSeconds 设置为 0（默认），日志会自动立即刷新，无需手动调用此函数
        /// <br/>2. 如果 flushIntervalSeconds > 0，可以调用此函数强制刷新缓冲区中的日志
        /// </remarks>
        public int LogFlush(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogFlushDelegate>("LogFlush");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 创建新的日志实例
        /// </summary>
        /// <param name="instanceName">实例名称（用于标识，如 "NetworkLogger"）</param>
        /// <returns>日志实例句柄，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 新创建的实例使用默认配置
        /// <br/>2. 实例句柄必须通过 OLALogDestroyInstance 释放
        /// <br/>3. 默认实例（句柄 = 0）无需创建，始终存在
        /// </remarks>
        public long LogCreateInstance(string instanceName){
            var func = OLAPlugDLLHelper.GetFunction<LogCreateInstanceDelegate>("LogCreateInstance");
            return func(OLAObject, instanceName);
        }

        /// <summary>
        /// 销毁日志实例并释放资源
        /// </summary>
        /// <param name="loggerHandle">要销毁的日志实例句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败（实例不存在或为默认实例）
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 不能销毁默认实例（句柄 = 0）
        /// <br/>2. 销毁后，该句柄将失效，不可再使用
        /// </remarks>
        public int LogDestroyInstance(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogDestroyInstanceDelegate>("LogDestroyInstance");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 设置日志根目录
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="baseDirectory">根目录路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认：./user_logs
        /// </remarks>
        public int LogSetBaseDirectory(long loggerHandle, string baseDirectory){
            var func = OLAPlugDLLHelper.GetFunction<LogSetBaseDirectoryDelegate>("LogSetBaseDirectory");
            return func(OLAObject, loggerHandle, baseDirectory);
        }

        /// <summary>
        /// 设置目录组织模式
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="dirMode">目录模式（可位或组合），见 OLALogDirMode</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认：OLA_LOG_DIR_FLAT (0)
        /// <br/>2. 示例：OLA_LOG_DIR_BY_DATE | OLA_LOG_DIR_BY_MODULE
        /// </remarks>
        public int LogSetDirMode(long loggerHandle, int dirMode){
            var func = OLAPlugDLLHelper.GetFunction<LogSetDirModeDelegate>("LogSetDirMode");
            return func(OLAObject, loggerHandle, dirMode);
        }

        /// <summary>
        /// 设置模块名称
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="moduleName">模块名称（用于目录组织）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于 OLA_LOG_DIR_BY_MODULE 模式
        /// </remarks>
        public int LogSetModuleName(long loggerHandle, string moduleName){
            var func = OLAPlugDLLHelper.GetFunction<LogSetModuleNameDelegate>("LogSetModuleName");
            return func(OLAObject, loggerHandle, moduleName);
        }

        /// <summary>
        /// 设置文件名模式（支持占位符）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="fileNamePattern">文件名模式</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认值：app.log
        /// <br/>2. 支持的占位符：{date} - 当前日期（格式：YYYY-MM-DD，如 2026-02-28）{time} - 当前时间（格式：HH-MM-SS，如 14-30-45）{datetime} - 日期时间（格式：YYYY-MM-DD_HH-MM-SS，如 2026-02-28_14-30-45）{module} - 模块名称（通过 OLALogSetModuleName 设置）{level} - 日志级别（TRACE, DEBUG, INFO, WARN, ERROR, CRITICAL）{index} - 文件序号（用于文件分割，从 1 开始递增）{pid} - 进程ID（当前进程的唯一标识符）{year} - 年份（4位数字，如 2026）{month} - 月份（01-12）{day} - 日期（01-31）{hour} - 小时（00-23）{minute} - 分钟（00-59）{second} - 秒（00-59）
        /// <br/>3. 示例用法："app_{date}.log" → app_2026-02-28.log"{module}_{date}_{index}.log" → network_2026-02-28_1.log"log_{datetime}_{pid}.log" → log_2026-02-28_14-30-45_12345.log"{year}{month}{day}_{level}.log" → 20260228_INFO.log
        /// <br/>4. 文件分割时 {index} 会自动递增：app_1.log, app_2.log, app_3.log...
        /// <br/>5. 如果不使用 {index}，分割时会自动在文件名后添加序号
        /// </remarks>
        public int LogSetFileNamePattern(long loggerHandle, string fileNamePattern){
            var func = OLAPlugDLLHelper.GetFunction<LogSetFileNamePatternDelegate>("LogSetFileNamePattern");
            return func(OLAObject, loggerHandle, fileNamePattern);
        }

        /// <summary>
        /// 设置文件分割模式
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="rotationMode">分割模式（可位或组合），见 OLALogRotationMode</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认：OLA_LOG_ROTATION_SIZE (1) - 仅按大小分割
        /// <br/>2. 可组合：OLA_LOG_ROTATION_SIZE | OLA_LOG_ROTATION_DAILY - 按大小和日期分割
        /// </remarks>
        public int LogSetRotationMode(long loggerHandle, int rotationMode){
            var func = OLAPlugDLLHelper.GetFunction<LogSetRotationModeDelegate>("LogSetRotationMode");
            return func(OLAObject, loggerHandle, rotationMode);
        }

        /// <summary>
        /// 设置文件追加模式
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="enableAppend">是否启用追加（1 启用，0 禁用）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 默认：1（启用追加）
        /// <br/>2. 启用时：程序重启后继续追加到现有文件
        /// <br/>3. 禁用时：程序重启后将现有文件重命名为备份，创建新文件
        /// </remarks>
        public int LogSetAppendMode(long loggerHandle, int enableAppend){
            var func = OLAPlugDLLHelper.GetFunction<LogSetAppendModeDelegate>("LogSetAppendMode");
            return func(OLAObject, loggerHandle, enableAppend);
        }

        /// <summary>
        /// 写入 TRACE 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogTraceEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogTraceExDelegate>("LogTraceEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 写入 DEBUG 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogDebugEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogDebugExDelegate>("LogDebugEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 写入 INFO 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogInfoEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogInfoExDelegate>("LogInfoEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 写入 WARN 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogWarnEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogWarnExDelegate>("LogWarnEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 写入 ERROR 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogErrorEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogErrorExDelegate>("LogErrorEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 写入 CRITICAL 级别日志（扩展版本，支持指定实例）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="message">日志消息</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogCriticalEx(long loggerHandle, string message){
            var func = OLAPlugDLLHelper.GetFunction<LogCriticalExDelegate>("LogCriticalEx");
            return func(OLAObject, loggerHandle, message);
        }

        /// <summary>
        /// 手动触发日志文件分割
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 立即关闭当前文件并创建新文件
        /// </remarks>
        public int LogRotateFile(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogRotateFileDelegate>("LogRotateFile");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 清理超过保留数量的旧日志文件
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <param name="keepCount">保留文件数量（-1 表示使用配置值）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int LogCleanupOldFiles(long loggerHandle, int keepCount){
            var func = OLAPlugDLLHelper.GetFunction<LogCleanupOldFilesDelegate>("LogCleanupOldFiles");
            return func(OLAObject, loggerHandle, keepCount);
        }

        /// <summary>
        /// 获取日志实例的当前文件路径
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>当前文件路径，失败返回空字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr接口释放内存
        /// </remarks>
        public string LogGetCurrentFilePath(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogGetCurrentFilePathDelegate>("LogGetCurrentFilePath");
            return PtrToStringUTF8(func(OLAObject, loggerHandle));
        }

        /// <summary>
        /// 获取当前日志文件大小（字节）
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>文件大小（字节），失败返回 -1</returns>
        public long LogGetCurrentFileSize(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogGetCurrentFileSizeDelegate>("LogGetCurrentFileSize");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 获取日志文件总数
        /// </summary>
        /// <param name="loggerHandle">日志实例句柄（0 表示默认实例）</param>
        /// <returns>文件数量，失败返回 -1</returns>
        public int LogGetTotalFilesCount(long loggerHandle){
            var func = OLAPlugDLLHelper.GetFunction<LogGetTotalFilesCountDelegate>("LogGetTotalFilesCount");
            return func(OLAObject, loggerHandle);
        }

        /// <summary>
        /// 把双精度浮点数转换成二进制形式（IEEE 754标准）
        /// </summary>
        /// <param name="double_value">需要转换的double值</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string DoubleToData(double double_value){
            var func = OLAPlugDLLHelper.GetFunction<DoubleToDataDelegate>("DoubleToData");
            return PtrToStringUTF8(func(OLAObject, double_value));
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
            var func = OLAPlugDLLHelper.GetFunction<FloatToDataDelegate>("FloatToData");
            return PtrToStringUTF8(func(OLAObject, float_value));
        }

        /// <summary>
        /// 把字符串转换成二进制形式.
        /// </summary>
        /// <param name="string_value">字符串值</param>
        /// <param name="type">字符串返回的表达类型
        ///<br/> 0: Ascii
        ///<br/> 1: Unicode
        ///<br/> 2: UTF8
        /// </param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string StringToData(string string_value, int type){
            var func = OLAPlugDLLHelper.GetFunction<StringToDataDelegate>("StringToData");
            return PtrToStringUTF8(func(OLAObject, string_value, type));
        }

        /// <summary>
        /// 把64位整数转换成32位整数.
        /// </summary>
        /// <param name="v">64位整数</param>
        /// <returns>32位整数</returns>
        public int Int64ToInt32(long v){
            var func = OLAPlugDLLHelper.GetFunction<Int64ToInt32Delegate>("Int64ToInt32");
            return func(OLAObject, v);
        }

        /// <summary>
        /// 把32位整数转换成64位整数.
        /// </summary>
        /// <param name="v">32位整数</param>
        /// <returns>64位整数</returns>
        public long Int32ToInt64(int v){
            var func = OLAPlugDLLHelper.GetFunction<Int32ToInt64Delegate>("Int32ToInt64");
            return func(OLAObject, v);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="data">要搜索的二进制数据,支持CE数据格式 比如"00 01 23 45 * ?? ?b c? * f1"等.</param>
        /// <returns>返回二进制字符串的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindData(long hwnd, string addr_range, string data){
            var func = OLAPlugDLLHelper.GetFunction<FindDataDelegate>("FindData");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, data));
        }

        /// <summary>
        /// 
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDataEx(long hwnd, string addr_range, string data, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<FindDataExDelegate>("FindDataEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, data, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的双精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="double_value_min">最小值</param>
        /// <param name="double_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDouble(long hwnd, string addr_range, double double_value_min, double double_value_max){
            var func = OLAPlugDLLHelper.GetFunction<FindDoubleDelegate>("FindDouble");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, double_value_min, double_value_max));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindDoubleEx(long hwnd, string addr_range, double double_value_min, double double_value_max, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<FindDoubleExDelegate>("FindDoubleEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, double_value_min, double_value_max, step, multi_thread, mode));
        }

        /// <summary>
        /// 搜索指定范围内的单精度浮点数.
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr_range">地址范围</param>
        /// <param name="float_value_min">最小值</param>
        /// <param name="float_value_max">最大值</param>
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindFloat(long hwnd, string addr_range, float float_value_min, float float_value_max){
            var func = OLAPlugDLLHelper.GetFunction<FindFloatDelegate>("FindFloat");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, float_value_min, float_value_max));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindFloatEx(long hwnd, string addr_range, float float_value_min, float float_value_max, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<FindFloatExDelegate>("FindFloatEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, float_value_min, float_value_max, step, multi_thread, mode));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindInt(long hwnd, string addr_range, long int_value_min, long int_value_max, int type){
            var func = OLAPlugDLLHelper.GetFunction<FindIntDelegate>("FindInt");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindIntEx(long hwnd, string addr_range, long int_value_min, long int_value_max, int type, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<FindIntExDelegate>("FindIntEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, int_value_min, int_value_max, type, step, multi_thread, mode));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindString(long hwnd, string addr_range, string string_value, int type){
            var func = OLAPlugDLLHelper.GetFunction<FindStringDelegate>("FindString");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, string_value, type));
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
        /// <returns>返回二进制字符串的指针，数据格式:字符串"addr1|addr2|addr3…|addrn"比如"123456|ff001122|dc12366"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string FindStringEx(long hwnd, string addr_range, string string_value, int type, int step, int multi_thread, int mode){
            var func = OLAPlugDLLHelper.GetFunction<FindStringExDelegate>("FindStringEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr_range, string_value, type, step, multi_thread, mode));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadData(long hwnd, string addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<ReadDataDelegate>("ReadData");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr, len));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>返回二进制字符串的指针，数据格式:读取到的数值,以16进制表示的字符串 每个字节以空格相隔比如"12 34 56 78 ab cd ef"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string ReadDataAddr(long hwnd, long addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<ReadDataAddrDelegate>("ReadDataAddr");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr, len));
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的数据字符串指针. 返回0表示读取失败.</returns>
        public long ReadDataAddrToBin(long hwnd, long addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<ReadDataAddrToBinDelegate>("ReadDataAddrToBin");
            return func(OLAObject, hwnd, addr, len);
        }

        /// <summary>
        /// 读取指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="len">长度</param>
        /// <returns>读取到的内存地址</returns>
        public long ReadDataToBin(long hwnd, string addr, int len){
            var func = OLAPlugDLLHelper.GetFunction<ReadDataToBinDelegate>("ReadDataToBin");
            return func(OLAObject, hwnd, addr, len);
        }

        /// <summary>
        /// 读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double ReadDouble(long hwnd, string addr){
            var func = OLAPlugDLLHelper.GetFunction<ReadDoubleDelegate>("ReadDouble");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的双精度浮点数</returns>
        public double ReadDoubleAddr(long hwnd, long addr){
            var func = OLAPlugDLLHelper.GetFunction<ReadDoubleAddrDelegate>("ReadDoubleAddr");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float ReadFloat(long hwnd, string addr){
            var func = OLAPlugDLLHelper.GetFunction<ReadFloatDelegate>("ReadFloat");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <returns>读取到的单精度浮点数</returns>
        public float ReadFloatAddr(long hwnd, long addr){
            var func = OLAPlugDLLHelper.GetFunction<ReadFloatAddrDelegate>("ReadFloatAddr");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 读取指定地址的长整型数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
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
            var func = OLAPlugDLLHelper.GetFunction<ReadIntDelegate>("ReadInt");
            return func(OLAObject, hwnd, addr, type);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadIntAddrDelegate>("ReadIntAddr");
            return func(OLAObject, hwnd, addr, type);
        }

        /// <summary>
        /// 读取指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
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
            var func = OLAPlugDLLHelper.GetFunction<ReadStringDelegate>("ReadString");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr, type, len));
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
            var func = OLAPlugDLLHelper.GetFunction<ReadStringAddrDelegate>("ReadStringAddr");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr, type, len));
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteData(long hwnd, string addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<WriteDataDelegate>("WriteData");
            return func(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="data">字符串数据地址</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteDataFromBin(long hwnd, string addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<WriteDataFromBinDelegate>("WriteDataFromBin");
            return func(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteDataAddr(long hwnd, long addr, string data){
            var func = OLAPlugDLLHelper.GetFunction<WriteDataAddrDelegate>("WriteDataAddr");
            return func(OLAObject, hwnd, addr, data);
        }

        /// <summary>
        /// 写入指定地址的数据
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="data">数据 二进制数据，以字符串形式描述，比如"12 34 56 78 90 ab cd"</param>
        /// <param name="len">数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteDataAddrFromBin(long hwnd, long addr, long data, int len){
            var func = OLAPlugDLLHelper.GetFunction<WriteDataAddrFromBinDelegate>("WriteDataAddrFromBin");
            return func(OLAObject, hwnd, addr, data, len);
        }

        /// <summary>
        /// 写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteDouble(long hwnd, string addr, double double_value){
            var func = OLAPlugDLLHelper.GetFunction<WriteDoubleDelegate>("WriteDouble");
            return func(OLAObject, hwnd, addr, double_value);
        }

        /// <summary>
        /// 写入指定地址的双精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="double_value">双精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteDoubleAddr(long hwnd, long addr, double double_value){
            var func = OLAPlugDLLHelper.GetFunction<WriteDoubleAddrDelegate>("WriteDoubleAddr");
            return func(OLAObject, hwnd, addr, double_value);
        }

        /// <summary>
        /// 写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteFloat(long hwnd, string addr, float float_value){
            var func = OLAPlugDLLHelper.GetFunction<WriteFloatDelegate>("WriteFloat");
            return func(OLAObject, hwnd, addr, float_value);
        }

        /// <summary>
        /// 写入指定地址的单精度浮点数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址</param>
        /// <param name="float_value">单精度浮点数</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteFloatAddr(long hwnd, long addr, float float_value){
            var func = OLAPlugDLLHelper.GetFunction<WriteFloatAddrDelegate>("WriteFloatAddr");
            return func(OLAObject, hwnd, addr, float_value);
        }

        /// <summary>
        /// 写入指定地址的整数
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteInt(long hwnd, string addr, int type, long value){
            var func = OLAPlugDLLHelper.GetFunction<WriteIntDelegate>("WriteInt");
            return func(OLAObject, hwnd, addr, type, value);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteIntAddr(long hwnd, long addr, int type, long value){
            var func = OLAPlugDLLHelper.GetFunction<WriteIntAddrDelegate>("WriteIntAddr");
            return func(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 写入指定地址的字符串
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="addr">地址，支持CE数据格式比如：[[[<module>+offset1]+offset2]+offset3]，<Game.exe>+1234+8+4，[<Game.exe>+1234]+8+4，[[<Game.exe>+1234]+8 ]+4，<Game.exe>+1234，[0x12345678]+10</param>
        /// <param name="type">字符串类型,取值如下
        ///<br/> 0: Ascii字符串
        ///<br/> 1: Unicode字符串
        ///<br/> 2: UTF8字符串
        /// </param>
        /// <param name="value">要写入的字符串</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteString(long hwnd, string addr, int type, string value){
            var func = OLAPlugDLLHelper.GetFunction<WriteStringDelegate>("WriteString");
            return func(OLAObject, hwnd, addr, type, value);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int WriteStringAddr(long hwnd, long addr, int type, string value){
            var func = OLAPlugDLLHelper.GetFunction<WriteStringAddrDelegate>("WriteStringAddr");
            return func(OLAObject, hwnd, addr, type, value);
        }

        /// <summary>
        /// 设置是否把所有内存接口函数中的窗口句柄当作进程ID
        /// </summary>
        /// <param name="enable">是否启用
        ///<br/> 0: 不启用
        ///<br/> 1: 启用
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetMemoryHwndAsProcessId(int enable){
            var func = OLAPlugDLLHelper.GetFunction<SetMemoryHwndAsProcessIdDelegate>("SetMemoryHwndAsProcessId");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 释放进程内存
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeProcessMemory(long hwnd){
            var func = OLAPlugDLLHelper.GetFunction<FreeProcessMemoryDelegate>("FreeProcessMemory");
            return func(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取模块基地址
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="module_name">模块名</param>
        /// <returns>成功返回模块基地址,失败返回0</returns>
        public long GetModuleBaseAddr(long hwnd, string module_name){
            var func = OLAPlugDLLHelper.GetFunction<GetModuleBaseAddrDelegate>("GetModuleBaseAddr");
            return func(OLAObject, hwnd, module_name);
        }

        /// <summary>
        /// 获取模块大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="module_name">模块名</param>
        /// <returns>成功返回模块大小,失败返回0</returns>
        public int GetModuleSize(long hwnd, string module_name){
            var func = OLAPlugDLLHelper.GetFunction<GetModuleSizeDelegate>("GetModuleSize");
            return func(OLAObject, hwnd, module_name);
        }

        /// <summary>
        /// 获取远程API地址
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="module_name">模块名</param>
        /// <param name="fun_name">函数名</param>
        /// <returns>成功返回远程API地址,失败返回0</returns>
        public long GetRemoteApiAddress(long hwnd, string module_name, string fun_name){
            var func = OLAPlugDLLHelper.GetFunction<GetRemoteApiAddressDelegate>("GetRemoteApiAddress");
            return func(OLAObject, hwnd, module_name, fun_name);
        }

        /// <summary>
        /// 在指定的窗口所在进程分配一段内存
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID. 默认是窗口句柄.如果要指定为进程ID,需要调用SetMemoryHwndAsProcessId</param>
        /// <param name="addr">预期的分配地址。如果是0表示自动分配，否则就尝试在此地址上分配内存</param>
        /// <param name="size">需要分配的内存大小</param>
        /// <param name="type">需要分配的内存类型，取值如下:
        ///<br/> 0: 可读可写可执行
        ///<br/> 1: 可读可执行，不可写
        ///<br/> 2: 可读可写,不可执行
        /// </param>
        /// <returns>分配的内存地址，如果是0表示分配失败</returns>
        public long VirtualAllocEx(long hwnd, long addr, int size, int type){
            var func = OLAPlugDLLHelper.GetFunction<VirtualAllocExDelegate>("VirtualAllocEx");
            return func(OLAObject, hwnd, addr, size, type);
        }

        /// <summary>
        /// 释放指定的内存
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要释放的内存地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int VirtualFreeEx(long hwnd, long addr){
            var func = OLAPlugDLLHelper.GetFunction<VirtualFreeExDelegate>("VirtualFreeEx");
            return func(OLAObject, hwnd, addr);
        }

        /// <summary>
        /// 修改指定的内存保护属性
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要修改的内存地址</param>
        /// <param name="size">需要修改的内存大小</param>
        /// <param name="newProtect">需要修改的内存类型，取值如下:
        ///<br/> 0x10: PAGE_EXECUTE 可执行
        ///<br/> 0x20: PAGE_EXECUTE_READ 可读,可执行
        ///<br/> 0x40: PAGE_READWRITE 可读可写,可执行
        ///<br/> 0x80: PAGE_EXECUTE_WRITECOPY
        /// </param>
        /// <param name="oldProtect">修改前的保护属性</param>
        /// <returns>成功返回修改之前的读写属性,失败返回-1</returns>
        public int VirtualProtectEx(long hwnd, long addr, int size, int newProtect, out int oldProtect){
            var func = OLAPlugDLLHelper.GetFunction<VirtualProtectExDelegate>("VirtualProtectEx");
            return func(OLAObject, hwnd, addr, size, newProtect, out oldProtect);
        }

        /// <summary>
        /// 查询指定的内存信息
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="addr">要查询的内存地址</param>
        /// <param name="pmbi">内存信息结构体指针</param>
        /// <returns>返回二进制字符串的指针，.内容是"BaseAddress,AllocationBase,AllocationProtect,RegionSize,State,Protect,Type"数值都是10进制表达</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串指针需调用FreeStringPtr释放内存
        /// </remarks>
        public string VirtualQueryEx(long hwnd, long addr, long pmbi){
            var func = OLAPlugDLLHelper.GetFunction<VirtualQueryExDelegate>("VirtualQueryEx");
            return PtrToStringUTF8(func(OLAObject, hwnd, addr, pmbi));
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
            var func = OLAPlugDLLHelper.GetFunction<CreateRemoteThreadDelegate>("CreateRemoteThread");
            return func(OLAObject, hwnd, lpStartAddress, lpParameter, dwCreationFlags, out lpThreadId);
        }

        /// <summary>
        /// 关闭一个内核对象
        /// </summary>
        /// <param name="handle">要关闭的对象句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int CloseHandle(long handle){
            var func = OLAPlugDLLHelper.GetFunction<CloseHandleDelegate>("CloseHandle");
            return func(OLAObject, handle);
        }

        /// <summary>
        /// 远程Hook API
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="targetAddr">目标地址</param>
        /// <param name="size">大小</param>
        /// <param name="hook_proc">当前进程内回调函数地址（整型传参便于跨语言）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 支持x86和x64目标进程为X64时回调函数为HookCallback64,目标进程为X86时回调函数为HookCallback32。回调在本进程内执行。C#等可使用 Marshal.GetFunctionPointerForDelegate 传入委托地址，并保持委托引用以防被GC回收。
        /// </remarks>
        public int HookRemoteApi(long hwnd, long targetAddr, long size, long hook_proc){
            var func = OLAPlugDLLHelper.GetFunction<HookRemoteApiDelegate>("HookRemoteApi");
            return func(OLAObject, hwnd, targetAddr, size, hook_proc);
        }

        /// <summary>
        /// 卸载远程Hook API
        /// </summary>
        /// <param name="hwnd">窗口句柄或者进程ID</param>
        /// <param name="targetAddr">目标地址</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int UnhookRemoteApi(long hwnd, long targetAddr){
            var func = OLAPlugDLLHelper.GetFunction<UnhookRemoteApiDelegate>("UnhookRemoteApi");
            return func(OLAObject, hwnd, targetAddr);
        }

        /// <summary>
        /// 下载文件（支持断点续传与进度）
        /// </summary>
        /// <param name="url">完整 URL</param>
        /// <param name="save_path">本地保存路径</param>
        /// <param name="callback"> 回调函数 void DownloadCallback(int64_t current, int64_t total, int64_t speed,int64_t user_data)</param>
        /// <param name="user_data">传给 callback 的用户数据,一般用于传递用户上下文数据</param>
        /// <returns>错误码（OLAHttpDownloadError）：0=成功，负数=失败（参见 OLAHttpDownloadError 枚举）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 回调参数：current 已下载字节数，total 总字节数(0 表示未知)，speed 当前下载速度(字节/秒)，user_data 由调用方传入
        /// </remarks>
        public int HttpDownloadFile(string url, string save_path, DownloadCallback callback, long user_data){
            var func = OLAPlugDLLHelper.GetFunction<HttpDownloadFileDelegate>("HttpDownloadFile");
            return func(OLAObject, url, save_path, callback, user_data);
        }

        /// <summary>
        /// 下载文件（带重试与超时）
        /// </summary>
        /// <param name="url">完整 URL</param>
        /// <param name="save_path">本地保存路径</param>
        /// <param name="callback"> 回调函数 void DownloadCallback(int64_t current, int64_t total, int64_t speed,int64_t user_data)</param>
        /// <param name="user_data">传给 callback 的用户数据,一般用于传递用户上下文数据</param>
        /// <param name="max_retries">断线后最大重试次数</param>
        /// <param name="connect_timeout_sec">连接超时秒，0 用默认</param>
        /// <param name="read_timeout_sec">读超时秒，0 用默认</param>
        /// <returns>错误码（OLAHttpDownloadError）：0=成功，负数=失败（参见 OLAHttpDownloadError 枚举）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 回调参数：current 已下载字节数，total 总字节数(0 表示未知)，speed 当前下载速度(字节/秒)，user_data 由调用方传入
        /// </remarks>
        public int HttpDownloadFileEx(string url, string save_path, DownloadCallback callback, long user_data, int max_retries, int connect_timeout_sec, int read_timeout_sec){
            var func = OLAPlugDLLHelper.GetFunction<HttpDownloadFileExDelegate>("HttpDownloadFileEx");
            return func(OLAObject, url, save_path, callback, user_data, max_retries, connect_timeout_sec, read_timeout_sec);
        }

        /// <summary>
        /// 发送简单 HTTP GET 请求，返回响应体字符串
        /// </summary>
        /// <param name="url">完整 URL</param>
        /// <returns>响应体字符串指针，失败返回 0，需要调用 FreeStringPtr 释放</returns>
        public string HttpGet(string url){
            var func = OLAPlugDLLHelper.GetFunction<HttpGetDelegate>("HttpGet");
            return PtrToStringUTF8(func(OLAObject, url));
        }

        /// <summary>
        /// 发送简单 HTTP POST 请求，返回响应体字符串
        /// </summary>
        /// <param name="url">完整 URL</param>
        /// <param name="body">请求体内容</param>
        /// <param name="content_type">Content-Type，例如 \"application/json\"</param>
        /// <returns>响应体字符串指针，失败返回 0，需要调用 FreeStringPtr 释放</returns>
        public string HttpPost(string url, string body, string content_type){
            var func = OLAPlugDLLHelper.GetFunction<HttpPostDelegate>("HttpPost");
            return PtrToStringUTF8(func(OLAObject, url, body, content_type));
        }

        /// <summary>
        /// 高级 HTTP 请求：支持自定义 Method、请求头（含 Cookie）、请求体
        /// </summary>
        /// <param name="method">方法，如 "GET"/"POST"/"PUT"/"DELETE"，大小写不敏感</param>
        /// <param name="url">完整 URL</param>
        /// <param name="headers">自定义请求头，多行字符串，每行 "Name: Value"，如 "Cookie: a=b\r\nUser-Agent:x\r\n"，可为空</param>
        /// <param name="body">请求体，GET 可传空</param>
        /// <param name="content_type">如 "application/json"，可为空</param>
        /// <param name="status_code">输出 HTTP 状态码，可为 NULL</param>
        /// <returns>响应体字符串指针，失败返回 0，需调用 FreeStringPtr 释放</returns>
        public string HttpRequestEx(string method, string url, string headers, string body, string content_type, out int status_code){
            var func = OLAPlugDLLHelper.GetFunction<HttpRequestExDelegate>("HttpRequestEx");
            return PtrToStringUTF8(func(OLAObject, method, url, headers, body, content_type, out status_code));
        }

        /// <summary>
        /// 创建 TCP 客户端（基于回调的事件驱动模式）
        /// </summary>
        /// <param name="callback">事件回调函数，插件内部会自动转发所有事件到此回调 void TcpClientCallback(int64_tclient_handle, int32_t event_type, int64_t data, int32_t data_len, int64_t user_data)</param>
        /// <param name="user_data">用户自定义数据，会在回调时传回</param>
        /// <param name="enable_packet_protocol">是否启用消息分包协议：1=启用（推荐），0=禁用（原始模式）</param>
        /// <returns>客户端句柄，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 回调事件类型：0 = 连接成功1 = 连接失败2 = 接收到数据（data 指向数据，data_len 为长度）3 = 连接断开4 = 发送完成
        /// <br/>2. 消息分包协议格式：[4字节长度前缀(小端序)][消息体]，可自动解决粘包问题
        /// <br/>3. 禁用后为原始模式，可能出现粘包问题，适用于与第三方系统通信
        /// </remarks>
        public long TcpClientCreate(TcpClientCallback callback, long user_data, int enable_packet_protocol){
            var func = OLAPlugDLLHelper.GetFunction<TcpClientCreateDelegate>("TcpClientCreate");
            return func(OLAObject, callback, user_data, enable_packet_protocol);
        }

        /// <summary>
        /// 连接到服务器（异步操作，结果通过回调通知）
        /// </summary>
        /// <param name="client_handle">客户端句柄</param>
        /// <param name="host">主机名或 IP</param>
        /// <param name="port">端口</param>
        /// <returns>0 失败（参数错误等），1 开始连接（最终结果通过回调通知）</returns>
        public int TcpClientConnect(long client_handle, string host, int port){
            var func = OLAPlugDLLHelper.GetFunction<TcpClientConnectDelegate>("TcpClientConnect");
            return func(OLAObject, client_handle, host, port);
        }

        /// <summary>
        /// 发送数据（异步操作）
        /// </summary>
        /// <param name="client_handle">客户端句柄</param>
        /// <param name="data">数据指针</param>
        /// <param name="data_len">数据长度</param>
        /// <returns>0 失败，1 成功加入发送队列</returns>
        public int TcpClientSend(long client_handle, long data, int data_len){
            var func = OLAPlugDLLHelper.GetFunction<TcpClientSendDelegate>("TcpClientSend");
            return func(OLAObject, client_handle, data, data_len);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="client_handle">客户端句柄</param>
        /// <returns>0 失败，1 成功</returns>
        public int TcpClientDisconnect(long client_handle){
            var func = OLAPlugDLLHelper.GetFunction<TcpClientDisconnectDelegate>("TcpClientDisconnect");
            return func(OLAObject, client_handle);
        }

        /// <summary>
        /// 销毁客户端（会自动断开连接）
        /// </summary>
        /// <param name="client_handle">客户端句柄</param>
        /// <returns>0 失败，1 成功</returns>
        public int TcpClientDestroy(long client_handle){
            var func = OLAPlugDLLHelper.GetFunction<TcpClientDestroyDelegate>("TcpClientDestroy");
            return func(OLAObject, client_handle);
        }

        /// <summary>
        /// 创建 TCP 服务端（基于回调的事件驱动模式）
        /// </summary>
        /// <param name="bind_addr">绑定地址，空或 "0.0.0.0" 表示所有接口</param>
        /// <param name="port">端口</param>
        /// <param name="callback">事件回调函数，插件内部会自动转发所有事件到此回调 void TcpServerCallback(int64_tserver_handle, int64_t conn_id, int32_t event_type, int64_t data, int32_t data_len, int64_tuser_data)</param>
        /// <param name="user_data">用户自定义数据，会在回调时传回</param>
        /// <param name="enable_packet_protocol">是否启用消息分包协议：1=启用（推荐），0=禁用（原始模式）</param>
        /// <returns>服务端句柄，失败返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 回调事件类型：0 = 新连接（conn_id 为新连接的 ID）1 = 接收到数据（data 指向数据，data_len 为长度）2 = 连接断开（conn_id 为断开的连接 ID）3 = 发送完成
        /// <br/>2. 消息分包协议格式：[4字节长度前缀(小端序)][消息体]，可自动解决粘包问题
        /// <br/>3. 禁用后为原始模式，可能出现粘包问题，适用于与第三方系统通信
        /// </remarks>
        public long TcpServerCreate(string bind_addr, int port, TcpServerCallback callback, long user_data, int enable_packet_protocol){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerCreateDelegate>("TcpServerCreate");
            return func(OLAObject, bind_addr, port, callback, user_data, enable_packet_protocol);
        }

        /// <summary>
        /// 向指定连接发送数据
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <param name="conn_id">连接 ID（从回调中获得）</param>
        /// <param name="data">数据指针</param>
        /// <param name="data_len">数据长度</param>
        /// <returns>0 失败，1 成功加入发送队列</returns>
        public int TcpServerSend(long server_handle, long conn_id, long data, int data_len){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerSendDelegate>("TcpServerSend");
            return func(OLAObject, server_handle, conn_id, data, data_len);
        }

        /// <summary>
        /// 断开指定连接
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <param name="conn_id">连接 ID</param>
        /// <returns>0 失败，1 成功</returns>
        public int TcpServerDisconnect(long server_handle, long conn_id){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerDisconnectDelegate>("TcpServerDisconnect");
            return func(OLAObject, server_handle, conn_id);
        }

        /// <summary>
        /// 停止服务端（会断开所有连接）
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <returns>0 失败，1 成功</returns>
        public int TcpServerStop(long server_handle){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerStopDelegate>("TcpServerStop");
            return func(OLAObject, server_handle);
        }

        /// <summary>
        /// 获取客户端地址信息
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <param name="conn_id">连接 ID</param>
        /// <returns>格式为 "IP:Port" 的字符串指针，失败返回 0，需要调用 FreeStringPtr 释放</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 示例返回值: "192.168.1.100:12345" 或 "[::1]:54321" (IPv6)
        /// </remarks>
        public string TcpServerGetClientAddress(long server_handle, long conn_id){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerGetClientAddressDelegate>("TcpServerGetClientAddress");
            return PtrToStringUTF8(func(OLAObject, server_handle, conn_id));
        }

        /// <summary>
        /// 获取所有连接的 ID 列表
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <returns>连接 ID 列表字符串指针，需要调用 FreeStringPtr 释放</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的列表字符串需要使用 FreeStringPtr 释放
        /// <br/>2. 示例返回值: "1,2,3"
        /// </remarks>
        public string TcpServerGetAllConnectionIds(long server_handle){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerGetAllConnectionIdsDelegate>("TcpServerGetAllConnectionIds");
            return PtrToStringUTF8(func(OLAObject, server_handle));
        }

        /// <summary>
        /// 销毁服务端（会自动停止服务）
        /// </summary>
        /// <param name="server_handle">服务端句柄</param>
        /// <returns>0 失败，1 成功</returns>
        public int TcpServerDestroy(long server_handle){
            var func = OLAPlugDLLHelper.GetFunction<TcpServerDestroyDelegate>("TcpServerDestroy");
            return func(OLAObject, server_handle);
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
            var func = OLAPlugDLLHelper.GetFunction<OcrDelegate>("Ocr");
            return PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromPtrDelegate>("OcrFromPtr");
            return PtrToStringUTF8(func(OLAObject, ptr));
        }

        /// <summary>
        /// 识别BMP数据中的文字
        /// </summary>
        /// <param name="ptr">BMP图片数据流地址</param>
        /// <param name="size">图片大小</param>
        /// <returns>识别到的文字(二进制字符串的指针)</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string OcrFromBmpData(long ptr, int size){
            var func = OLAPlugDLLHelper.GetFunction<OcrFromBmpDataDelegate>("OcrFromBmpData");
            return PtrToStringUTF8(func(OLAObject, ptr, size));
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
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrDetails(int x1, int y1, int x2, int y2){
            var func = OLAPlugDLLHelper.GetFunction<OcrDetailsDelegate>("OcrDetails");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2));
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
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrFromPtrDetails(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<OcrFromPtrDetailsDelegate>("OcrFromPtrDetails");
            var result = PtrToStringUTF8(func(OLAObject, ptr));
            if (string.IsNullOrEmpty(result))
            {
                return new OcrResult();
            }
            return JsonConvert.DeserializeObject<OcrResult>(result);
        }

        /// <summary>
        /// 识别BMP数据中的文字
        /// </summary>
        /// <param name="ptr">BMP图像数据指针</param>
        /// <param name="size">BMP图像数据大小</param>
        /// <returns>返回识别到的字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrFromBmpDataDetails(long ptr, int size){
            var func = OLAPlugDLLHelper.GetFunction<OcrFromBmpDataDetailsDelegate>("OcrFromBmpDataDetails");
            var result = PtrToStringUTF8(func(OLAObject, ptr, size));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrV5Delegate>("OcrV5");
            return PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2));
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
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrV5Details(int x1, int y1, int x2, int y2){
            var func = OLAPlugDLLHelper.GetFunction<OcrV5DetailsDelegate>("OcrV5Details");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrV5FromPtrDelegate>("OcrV5FromPtr");
            return PtrToStringUTF8(func(OLAObject, ptr));
        }

        /// <summary>
        /// 使用V5模型识别指定图像中的文字
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <returns>}</returns>
        /// <remarks>注意事项: 
        /// <br/>1. Regions集合为所有识别到的数据集 Score为识别评分,分值越高越准确, Center为识别结果中心点Size为识别范围 Angle为识别结果角度 Vertices为识别结果的4个顶点
        /// <br/>2. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public OcrResult OcrV5FromPtrDetails(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<OcrV5FromPtrDetailsDelegate>("OcrV5FromPtrDetails");
            var result = PtrToStringUTF8(func(OLAObject, ptr));
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
        /// <br/>39. OcrLayoutDictPath (string):布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string):表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
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
            var func = OLAPlugDLLHelper.GetFunction<GetOcrConfigDelegate>("GetOcrConfig");
            return PtrToStringUTF8(func(OLAObject, configKey));
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
        /// <br/>39. OcrLayoutDictPath (string):布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string):表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
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
            var func = OLAPlugDLLHelper.GetFunction<SetOcrConfigDelegate>("SetOcrConfig");
            return func(OLAObject, configStr);
        }

        /// <summary>
        /// 设置OCR配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
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
        /// <br/>39. OcrLayoutDictPath (string):布局字典路径，默认"./ppocr/utils/dict/layout_dict/layout_publaynet_dict.txt"
        /// <br/>40. OcrLayoutScoreThreshold (double): 布局评分阈值，范围0.0-1.0，默认0.5
        /// <br/>41. OcrLayoutNmsThreshold (double): 布局NMS阈值，范围0.0-1.0，默认0.5
        /// <br/>42. 表格相关参数
        /// <br/>43. OcrTableModelDir (string): 表格结构模型路径，默认""
        /// <br/>44. OcrTableMaxLen (int): 表格最大长度，默认488
        /// <br/>45. OcrTableBatchNum (int): 表格批处理数量，默认1
        /// <br/>46. OcrMergeNoSpanStructure (bool): 是否合并无跨度结构，默认true
        /// <br/>47. OcrTableCharDictPath (string):表格字符字典路径，默认"./ppocr/utils/dict/table_structure_dict_ch.txt"
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
            var func = OLAPlugDLLHelper.GetFunction<SetOcrConfigByKeyDelegate>("SetOcrConfigByKey");
            return func(OLAObject, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictDelegate>("OcrFromDict");
            return PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictDelegate>("OcrFromDict");
            return PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictDetailsDelegate>("OcrFromDictDetails");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictDetailsDelegate>("OcrFromDictDetails");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictPtrDelegate>("OcrFromDictPtr");
            return PtrToStringUTF8(func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictPtrDelegate>("OcrFromDictPtr");
            return PtrToStringUTF8(func(OLAObject, ptr, colorJson, dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictPtrDetailsDelegate>("OcrFromDictPtrDetails");
            var result = PtrToStringUTF8(func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), dict_name, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<OcrFromDictPtrDetailsDelegate>("OcrFromDictPtrDetails");
            var result = PtrToStringUTF8(func(OLAObject, ptr, colorJson, dict_name, matchVal));
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
        /// <param name="colorJson">颜色列表的json字符串</param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="outX">输出参数，返回的X坐标</param>
        /// <param name="outY">输出参数，返回的Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FindStr(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal, out int outX, out int outY){
            var func = OLAPlugDLLHelper.GetFunction<FindStrDelegate>("FindStr");
            return func(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal, out outX, out outY);
        }

        /// <summary>
        /// 查找文字
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson">颜色列表的json字符串</param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="outX">输出参数，返回的X坐标</param>
        /// <param name="outY">输出参数，返回的Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FindStr(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal, out int outX, out int outY){
            var func = OLAPlugDLLHelper.GetFunction<FindStrDelegate>("FindStr");
            return func(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal, out outX, out outY);
        }

        /// <summary>
        /// 查找指定文字的坐标
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="str">要查找的文字</param>
        /// <param name="colorJson">颜色列表的json字符串</param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>y (整型数): Y坐标</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrDetailDelegate>("FindStrDetail");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
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
        /// <param name="colorJson">颜色列表的json字符串</param>
        /// <param name="dict">字典名称</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>y (整型数): Y坐标</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrDetail(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrDetailDelegate>("FindStrDetail");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrAllDelegate>("FindStrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrAll(int x1, int y1, int x2, int y2, string str, string colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrAllDelegate>("FindStrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, str, colorJson, dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>查找到的结果（格式为二进制字符串指针）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrFromPtr(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrFromPtrDelegate>("FindStrFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>查找到的结果（格式为二进制字符串指针）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public MatchResult FindStrFromPtr(long source, string str, string colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrFromPtrDelegate>("FindStrFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, source, str, colorJson, dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrFromPtrAll(long source, string str, List<ColorModel> colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrFromPtrAllDelegate>("FindStrFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, source, str, JsonConvert.SerializeObject(colorJson), dict, matchVal));
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
        /// <param name="colorJson">颜色列表的JSON字符串，格式如：[{"StartColor": "3278FA", "EndColor": "6496FF","Type": 0}, {"StartColor": "3278FA", "EndColor": "6496FF", "Type": 1}]</param>
        /// <param name="dict">字库名称</param>
        /// <param name="matchVal">匹配值</param>
        /// <returns>]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public List<MatchResult> FindStrFromPtrAll(long source, string str, string colorJson, string dict, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindStrFromPtrAllDelegate>("FindStrFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, source, str, colorJson, dict, matchVal));
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
            var func = OLAPlugDLLHelper.GetFunction<FastNumberOcrFromPtrDelegate>("FastNumberOcrFromPtr");
            return func(OLAObject, source, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
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
            var func = OLAPlugDLLHelper.GetFunction<FastNumberOcrFromPtrDelegate>("FastNumberOcrFromPtr");
            return func(OLAObject, source, numbers, colorJson, matchVal);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="x1">图片</param>
        /// <param name="y1">区域左上角Y坐标</param>
        /// <param name="x2">区域右下角X坐标</param>
        /// <param name="y2">区域右下角Y坐标</param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, List<ColorModel> colorJson, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FastNumberOcrDelegate>("FastNumberOcr");
            return func(OLAObject, x1, y1, x2, y2, numbers, JsonConvert.SerializeObject(colorJson), matchVal);
        }

        /// <summary>
        /// 快速识别数字
        /// </summary>
        /// <param name="x1">图片</param>
        /// <param name="y1">区域左上角Y坐标</param>
        /// <param name="x2">区域右下角X坐标</param>
        /// <param name="y2">区域右下角Y坐标</param>
        /// <param name="numbers">0~9数字图片地址,多个数字用|分割,如img/0.png|img/1.png|img/2.png|img/3.png|img/4.png|img/5.png|img/6.png|img/7.png|img/8.png|img/9.png</param>
        /// <param name="colorJson">颜色json</param>
        /// <param name="matchVal">识别率</param>
        /// <returns>识别到的数字,如果失败返回-1</returns>
        public int FastNumberOcr(int x1, int y1, int x2, int y2, string numbers, string colorJson, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FastNumberOcrDelegate>("FastNumberOcr");
            return func(OLAObject, x1, y1, x2, y2, numbers, colorJson, matchVal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictName">字库名称</param>
        /// <param name="dictPath">文本字库路径</param>
        /// <returns>是否成功</returns>
        public int ImportTxtDict(string dictName, string dictPath){
            var func = OLAPlugDLLHelper.GetFunction<ImportTxtDictDelegate>("ImportTxtDict");
            return func(OLAObject, dictName, dictPath);
        }

        /// <summary>
        /// 导出txt文本字库
        /// </summary>
        /// <param name="dictName">字库名称</param>
        /// <param name="dictPath">文本字库路径</param>
        /// <returns>是否成功</returns>
        public int ExportTxtDict(string dictName, string dictPath){
            var func = OLAPlugDLLHelper.GetFunction<ExportTxtDictDelegate>("ExportTxtDict");
            return func(OLAObject, dictName, dictPath);
        }

        /// <summary>
        /// 对绑定窗口在指定区域进行截图并保存为图片
        /// </summary>
        /// <param name="x1">截图区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">截图区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">截图区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">截图区域右下角Y坐标（相对于窗口客户区）</param>
        /// <param name="file">输出文件路径，支持bmp/gif/jpg/jpeg/png</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 若目录不存在请确保先行创建；覆盖同名文件
        /// </remarks>
        public int Capture(int x1, int y1, int x2, int y2, string file){
            var func = OLAPlugDLLHelper.GetFunction<CaptureDelegate>("Capture");
            return func(OLAObject, x1, y1, x2, y2, file);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放；数据包含完整BMP文件头，可直接落盘
        /// </remarks>
        public int GetScreenDataBmp(int x1, int y1, int x2, int y2, out long data, out int dataLen){
            var func = OLAPlugDLLHelper.GetFunction<GetScreenDataBmpDelegate>("GetScreenDataBmp");
            return func(OLAObject, x1, y1, x2, y2, out data, out dataLen);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放；无文件头，按4字节边界对齐
        /// </remarks>
        public int GetScreenData(int x1, int y1, int x2, int y2, out long data, out int dataLen, out int stride){
            var func = OLAPlugDLLHelper.GetFunction<GetScreenDataDelegate>("GetScreenData");
            return func(OLAObject, x1, y1, x2, y2, out data, out dataLen, out stride);
        }

        /// <summary>
        /// 获取绑定窗口指定区域的图像数据句柄
        /// </summary>
        /// <param name="x1">区域左上角X坐标（相对于窗口客户区）</param>
        /// <param name="y1">区域左上角Y坐标（相对于窗口客户区）</param>
        /// <param name="x2">区域右下角X坐标（相对于窗口客户区）</param>
        /// <param name="y2">区域右下角Y坐标（相对于窗口客户区）</param>
        /// <returns>返回内部缓存的图像句柄；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回图像句柄,在不使用的时候需要手动释放
        /// </remarks>
        public long GetScreenDataPtr(int x1, int y1, int x2, int y2){
            var func = OLAPlugDLLHelper.GetFunction<GetScreenDataPtrDelegate>("GetScreenDataPtr");
            return func(OLAObject, x1, y1, x2, y2);
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
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 持续截图编码，性能开销较大
        /// </remarks>
        public int CaptureGif(int x1, int y1, int x2, int y2, string file, int delay, int time){
            var func = OLAPlugDLLHelper.GetFunction<CaptureGifDelegate>("CaptureGif");
            return func(OLAObject, x1, y1, x2, y2, file, delay, time);
        }

        /// <summary>
        /// 锁定当前屏幕图像
        /// </summary>
        /// <param name="enable">锁定标志
        ///<br/> 0: 取消锁定，清空锁定图像并释放内存
        ///<br/> 非0: 锁定当前屏幕图像，后续截图将返回锁定的图像
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 锁定后，CaptureMat等截图接口将返回锁定的图像数据
        /// </remarks>
        public int LockDisplay(int enable){
            var func = OLAPlugDLLHelper.GetFunction<LockDisplayDelegate>("LockDisplay");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 设置截图缓存时间
        /// </summary>
        /// <param name="cacheTime">缓存时间（毫秒）
        ///<br/> 0: 不缓存，实时截图
        ///<br/> >0: 缓存截图到指定的毫秒数，在缓存时间内返回缓存的图像
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 设置缓存后，在缓存时间内多次截图将返回同一帧图像，提高性能
        /// </remarks>
        public int SetSnapCacheTime(int cacheTime){
            var func = OLAPlugDLLHelper.GetFunction<SetSnapCacheTimeDelegate>("SetSnapCacheTime");
            return func(OLAObject, cacheTime);
        }

        /// <summary>
        /// 从图像句柄读取像素数据
        /// </summary>
        /// <param name="imgPtr">图像句柄（由加载/生成接口返回）</param>
        /// <param name="data">返回像素数据指针（输出，BGR顺序）</param>
        /// <param name="size">返回数据字节长度（输出）</param>
        /// <param name="stride">返回每行字节跨度（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. data需调用FreeImageData释放
        /// </remarks>
        public int GetImageData(long imgPtr, out long data, out int size, out int stride){
            var func = OLAPlugDLLHelper.GetFunction<GetImageDataDelegate>("GetImageData");
            return func(OLAObject, imgPtr, out data, out size, out stride);
        }

        /// <summary>
        /// 使用文件路径在源图中匹配模板图
        /// </summary>
        /// <param name="source">源图路径</param>
        /// <param name="templ">模板图路径</param>
        /// <param name="matchVal">匹配阈值（0~1）</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配结果（结构体/指针，失败返回0）</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 实现取决于type/angle/scale的组合策略
        /// </remarks>
        public MatchResult MatchImageFromPath(string source, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImageFromPathDelegate>("MatchImageFromPath");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
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
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配点列表字符串指针；未找到返回空字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回字符串需调用FreeStringPtr释放
        /// </remarks>
        public List<MatchResult> MatchImageFromPathAll(string source, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImageFromPathAllDelegate>("MatchImageFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
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
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配结果（结构体/指针，失败返回0）</returns>
        public MatchResult MatchImagePtrFromPath(long source, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImagePtrFromPathDelegate>("MatchImagePtrFromPath");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
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
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>匹配点列表字符串指针；未找到返回空字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回字符串需调用FreeStringPtr释放
        /// </remarks>
        public List<MatchResult> MatchImagePtrFromPathAll(long source, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImagePtrFromPathAllDelegate>("MatchImagePtrFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
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
            var func = OLAPlugDLLHelper.GetFunction<GetColorDelegate>("GetColor");
            return PtrToStringUTF8(func(OLAObject, x, y));
        }

        /// <summary>
        /// 获取绑定窗口指定坐标点的颜色值（返回指针）
        /// </summary>
        /// <param name="source">源对象的指针，通常是一个图像或画布对象</param>
        /// <param name="x">指定点的X坐标（相对于窗口客户区）</param>
        /// <param name="y">指定点的Y坐标（相对于窗口客户区）</param>
        /// <returns>返回指向颜色值的指针，数据在内部缓存中；失败返回0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的指针指向内部缓存，不应手动释放；数据为BGR三个字节
        /// </remarks>
        public string GetColorPtr(long source, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<GetColorPtrDelegate>("GetColorPtr");
            return PtrToStringUTF8(func(OLAObject, source, x, y));
        }

        /// <summary>
        /// 复制一份图像数据
        /// </summary>
        /// <param name="sourcePtr">原始图像句柄</param>
        /// <returns>返回新图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CopyImage(long sourcePtr){
            var func = OLAPlugDLLHelper.GetFunction<CopyImageDelegate>("CopyImage");
            return func(OLAObject, sourcePtr);
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
            var func = OLAPlugDLLHelper.GetFunction<FreeImagePathDelegate>("FreeImagePath");
            return func(OLAObject, path);
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
            var func = OLAPlugDLLHelper.GetFunction<FreeImageAllDelegate>("FreeImageAll");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<LoadImageDelegate>("LoadImage");
            return func(OLAObject, path);
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
            var func = OLAPlugDLLHelper.GetFunction<LoadImageFromBmpDataDelegate>("LoadImageFromBmpData");
            return func(OLAObject, data, dataSize);
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
            var func = OLAPlugDLLHelper.GetFunction<LoadImageFromRGBDataDelegate>("LoadImageFromRGBData");
            return func(OLAObject, width, height, scan0, stride);
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
            var func = OLAPlugDLLHelper.GetFunction<FreeImagePtrDelegate>("FreeImagePtr");
            return func(OLAObject, screenPtr);
        }

        /// <summary>
        /// 在绑定窗口中查找指定窗口图像（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果</returns>
        public MatchResult MatchWindowsFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsFromPtrDelegate>("MatchWindowsFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在指定图片中查找指定图像（使用内存数据）
        /// </summary>
        /// <param name="source">OLAImage对象的地址</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果</returns>
        public MatchResult MatchImageFromPtr(long source, long templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImageFromPtrDelegate>("MatchImageFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在指定图片中查找指定图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="source">OLAImage对象的地址</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>返回所有匹配结果字符串</returns>
        public List<MatchResult> MatchImageFromPtrAll(long source, long templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchImageFromPtrAllDelegate>("MatchImageFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, matchVal, type, angle, scale));
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
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>返回所有匹配点结果的字符串</returns>
        public List<MatchResult> MatchWindowsFromPtrAll(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsFromPtrAllDelegate>("MatchWindowsFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
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
        /// <param name="templ">模板图片的路径，可以是多个图片,比如"test.bmp|test2.bmp|test3.bmp"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果</returns>
        public MatchResult MatchWindowsFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsFromPathDelegate>("MatchWindowsFromPath");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
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
        /// <param name="templ">模板图片的路径，可以是多个图片,比如"test.bmp|test2.bmp|test3.bmp"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>返回所有匹配结果的字符串</returns>
        public List<MatchResult> MatchWindowsFromPathAll(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsFromPathAllDelegate>("MatchWindowsFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPtrDelegate>("MatchWindowsThresholdFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPtr(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPtrDelegate>("MatchWindowsThresholdFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, long templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPtrAllDelegate>("MatchWindowsThresholdFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">窗口模板图句柄</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPtrAll(int x1, int y1, int x2, int y2, string colorJson, long templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPtrAllDelegate>("MatchWindowsThresholdFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPathDelegate>("MatchWindowsThresholdFromPath");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns>匹配结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public MatchResult MatchWindowsThresholdFromPath(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPathDelegate>("MatchWindowsThresholdFromPath");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, string templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPathAllDelegate>("MatchWindowsThresholdFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), templ, matchVal, angle, scale));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel。</param>
        /// <param name="templ">图像文件路径</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <returns></returns>
        public List<MatchResult> MatchWindowsThresholdFromPathAll(int x1, int y1, int x2, int y2, string colorJson, string templ, double matchVal, double angle, double scale){
            var func = OLAPlugDLLHelper.GetFunction<MatchWindowsThresholdFromPathAllDelegate>("MatchWindowsThresholdFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, templ, matchVal, angle, scale));
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
            var func = OLAPlugDLLHelper.GetFunction<ShowMatchWindowDelegate>("ShowMatchWindow");
            return func(OLAObject, flag);
        }

        /// <summary>
        /// 计算两幅图像的结构相似性指数（SSIM）
        /// </summary>
        /// <param name="image1">第一幅图像句柄</param>
        /// <param name="image2">第二幅图像句柄</param>
        /// <returns>SSIM值（0~1），越接近1越相似</returns>
        public double CalculateSSIM(long image1, long image2){
            var func = OLAPlugDLLHelper.GetFunction<CalculateSSIMDelegate>("CalculateSSIM");
            return func(OLAObject, image1, image2);
        }

        /// <summary>
        /// 计算两幅图像直方图的相似度
        /// </summary>
        /// <param name="image1">图像1句柄</param>
        /// <param name="image2">图像2句柄</param>
        /// <returns>直方图相似度（0~1）</returns>
        public double CalculateHistograms(long image1, long image2){
            var func = OLAPlugDLLHelper.GetFunction<CalculateHistogramsDelegate>("CalculateHistograms");
            return func(OLAObject, image1, image2);
        }

        /// <summary>
        /// 计算两幅图像的均方误差（MSE）
        /// </summary>
        /// <param name="image1">第一幅图像句柄</param>
        /// <param name="image2">第二幅图像句柄</param>
        /// <returns>MSE值，越小越相似</returns>
        public double CalculateMSE(long image1, long image2){
            var func = OLAPlugDLLHelper.GetFunction<CalculateMSEDelegate>("CalculateMSE");
            return func(OLAObject, image1, image2);
        }

        /// <summary>
        /// 将内存图像保存为文件
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="path">输出文件路径，支持bmp/gif/jpg/jpeg/png</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SaveImageFromPtr(long ptr, string path){
            var func = OLAPlugDLLHelper.GetFunction<SaveImageFromPtrDelegate>("SaveImageFromPtr");
            return func(OLAObject, ptr, path);
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
            var func = OLAPlugDLLHelper.GetFunction<ReSizeDelegate>("ReSize");
            return func(OLAObject, ptr, width, height);
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
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColor(int x1, int y1, int x2, int y2, string color1, string color2, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindColorDelegate>("FindColor");
            return func(OLAObject, x1, y1, x2, y2, color1, color2, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找指定颜色列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="color1">颜色起始范围，颜色格式 RRGGBB</param>
        /// <param name="color2">颜色结束范围，颜色格式 RRGGBB</param>
        /// <returns>查找结果返回所有匹配点坐标的字符串，格式为"["x":10,"y":20],"[x":30,"y":40]"；未找到返回空字符串指针，需调用FreeStringPtr释放内存</returns>
        public List<Point> FindColorList(int x1, int y1, int x2, int y2, string color1, string color2){
            var func = OLAPlugDLLHelper.GetFunction<FindColorListDelegate>("FindColorList");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, color1, color2));
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
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindColorEx(int x1, int y1, int x2, int y2, string colorJson, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindColorExDelegate>("FindColorEx");
            return func(OLAObject, x1, y1, x2, y2, colorJson, dir, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorListExDelegate>("FindColorListEx");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson));
            if (string.IsNullOrEmpty(result))
            {
                return new List<Point>();
            }
            return JsonConvert.DeserializeObject<List<Point>>(result);
        }

        /// <summary>
        /// 在绑定窗口中对比多色点
        /// </summary>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpMultiColor(List<PointColorModel> pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<CmpMultiColorDelegate>("CmpMultiColor");
            return func(OLAObject, JsonConvert.SerializeObject(pointJson), sim);
        }

        /// <summary>
        /// 在绑定窗口中对比多色点
        /// </summary>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpMultiColor(string pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<CmpMultiColorDelegate>("CmpMultiColor");
            return func(OLAObject, pointJson, sim);
        }

        /// <summary>
        /// 在指定图片中对比多色点
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpMultiColorPtr(long image, List<PointColorModel> pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<CmpMultiColorPtrDelegate>("CmpMultiColorPtr");
            return func(OLAObject, image, JsonConvert.SerializeObject(pointJson), sim);
        }

        /// <summary>
        /// 在指定图片中对比多色点
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpMultiColorPtr(long image, string pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<CmpMultiColorPtrDelegate>("CmpMultiColorPtr");
            return func(OLAObject, image, pointJson, sim);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int FindMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson, double sim, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorDelegate>("FindMultiColor");
            return func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), sim, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明- PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int FindMultiColor(int x1, int y1, int x2, int y2, string colorJson, string pointJson, double sim, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorDelegate>("FindMultiColor");
            return func(OLAObject, x1, y1, x2, y2, colorJson, pointJson, sim, dir, out x, out y);
        }

        /// <summary>
        /// 在绑定窗口中查找多色点列表
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>返回识别到的坐标点列表的JSON字符串</returns>
        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, List<ColorModel> colorJson, List<PointColorModel> pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorListDelegate>("FindMultiColorList");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), sim));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>返回识别到的坐标点列表的JSON字符串</returns>
        public List<Point> FindMultiColorList(int x1, int y1, int x2, int y2, string colorJson, string pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorListDelegate>("FindMultiColorList");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorJson, pointJson, sim));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColorFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson, double sim, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorFromPtrDelegate>("FindMultiColorFromPtr");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), sim, dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找多色点
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <param name="x">返回找到的颜色点X坐标</param>
        /// <param name="y">返回找到的颜色点Y坐标</param>
        /// <returns>查找结果
        ///<br/>0: 未找到
        ///<br/>1: 找到
        /// </returns>
        public int FindMultiColorFromPtr(long ptr, string colorJson, string pointJson, double sim, int dir, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorFromPtrDelegate>("FindMultiColorFromPtr");
            return func(OLAObject, ptr, colorJson, pointJson, sim, dir, out x, out y);
        }

        /// <summary>
        /// 在内存图像中查找多色点列表
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>返回识别到的坐标点列表的JSON字符串</returns>
        public List<Point> FindMultiColorListFromPtr(long ptr, List<ColorModel> colorJson, List<PointColorModel> pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorListFromPtrDelegate>("FindMultiColorListFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), JsonConvert.SerializeObject(pointJson), sim));
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
        /// <param name="colorJson">颜色模型配置字符串，用于限定图像匹配中的颜色范围，格式说明见 颜色模型说明 -ColorModel</param>
        /// <param name="pointJson">点阵颜色列表，支持JSON格式或简化字符串格式，格式说明见 点阵颜色列表格式说明 -PointColorListFormat</param>
        /// <param name="sim">相似度阈值，范围0-1.0，默认1.0</param>
        /// <returns>返回识别到的坐标点列表的JSON字符串</returns>
        public List<Point> FindMultiColorListFromPtr(long ptr, string colorJson, string pointJson, double sim){
            var func = OLAPlugDLLHelper.GetFunction<FindMultiColorListFromPtrDelegate>("FindMultiColorListFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, ptr, colorJson, pointJson, sim));
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
            var func = OLAPlugDLLHelper.GetFunction<GetImageSizeDelegate>("GetImageSize");
            return func(OLAObject, ptr, out width, out height);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockDelegate>("FindColorBlock");
            return func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockDelegate>("FindColorBlock");
            return func(OLAObject, x1, y1, x2, y2, colorList, count, width, height, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockPtrDelegate>("FindColorBlockPtr");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockPtrDelegate>("FindColorBlockPtr");
            return func(OLAObject, ptr, colorList, count, width, height, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListDelegate>("FindColorBlockList");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListDelegate>("FindColorBlockList");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListPtrDelegate>("FindColorBlockListPtr");
            var result = PtrToStringUTF8(func(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListPtrDelegate>("FindColorBlockListPtr");
            var result = PtrToStringUTF8(func(OLAObject, ptr, colorList, count, width, height, type));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockExDelegate>("FindColorBlockEx");
            return func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockExDelegate>("FindColorBlockEx");
            return func(OLAObject, x1, y1, x2, y2, colorList, count, width, height, dir, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockPtrExDelegate>("FindColorBlockPtrEx");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, dir, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockPtrExDelegate>("FindColorBlockPtrEx");
            return func(OLAObject, ptr, colorList, count, width, height, dir, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListExDelegate>("FindColorBlockListEx");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListExDelegate>("FindColorBlockListEx");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, colorList, count, width, height, type, dir));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListPtrExDelegate>("FindColorBlockListPtrEx");
            var result = PtrToStringUTF8(func(OLAObject, ptr, JsonConvert.SerializeObject(colorList), count, width, height, type, dir));
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
            var func = OLAPlugDLLHelper.GetFunction<FindColorBlockListPtrExDelegate>("FindColorBlockListPtrEx");
            var result = PtrToStringUTF8(func(OLAObject, ptr, colorList, count, width, height, type, dir));
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
            var func = OLAPlugDLLHelper.GetFunction<GetColorNumDelegate>("GetColorNum");
            return func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorList));
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
            var func = OLAPlugDLLHelper.GetFunction<GetColorNumDelegate>("GetColorNum");
            return func(OLAObject, x1, y1, x2, y2, colorList);
        }

        /// <summary>
        /// 统计内存图像中指定颜色的像素数量
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNumPtr(long ptr, List<ColorModel> colorList){
            var func = OLAPlugDLLHelper.GetFunction<GetColorNumPtrDelegate>("GetColorNumPtr");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorList));
        }

        /// <summary>
        /// 统计内存图像中指定颜色的像素数量
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorList">要统计的颜色值（JSON格式）</param>
        /// <returns>返回指定颜色的像素数量</returns>
        public int GetColorNumPtr(long ptr, string colorList){
            var func = OLAPlugDLLHelper.GetFunction<GetColorNumPtrDelegate>("GetColorNumPtr");
            return func(OLAObject, ptr, colorList);
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
            var func = OLAPlugDLLHelper.GetFunction<CroppedDelegate>("Cropped");
            return func(OLAObject, image, x1, y1, x2, y2);
        }

        /// <summary>
        /// 根据多色点生成阈值图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColorPtr(long ptr, List<ColorModel> colorJson){
            var func = OLAPlugDLLHelper.GetFunction<GetThresholdImageFromMultiColorPtrDelegate>("GetThresholdImageFromMultiColorPtr");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 根据多色点生成阈值图像
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColorPtr(long ptr, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<GetThresholdImageFromMultiColorPtrDelegate>("GetThresholdImageFromMultiColorPtr");
            return func(OLAObject, ptr, colorJson);
        }

        /// <summary>
        /// 根据多色点生成阈值图像（从屏幕区域）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">要统计的颜色值（JSON格式）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, List<ColorModel> colorJson){
            var func = OLAPlugDLLHelper.GetFunction<GetThresholdImageFromMultiColorDelegate>("GetThresholdImageFromMultiColor");
            return func(OLAObject, x1, y1, x2, y2, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 根据多色点生成阈值图像（从屏幕区域）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="colorJson">要统计的颜色值（JSON格式）</param>
        /// <returns>返回阈值图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long GetThresholdImageFromMultiColor(int x1, int y1, int x2, int y2, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<GetThresholdImageFromMultiColorDelegate>("GetThresholdImageFromMultiColor");
            return func(OLAObject, x1, y1, x2, y2, colorJson);
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
            var func = OLAPlugDLLHelper.GetFunction<IsSameImageDelegate>("IsSameImage");
            return func(OLAObject, ptr, ptr2);
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
            var func = OLAPlugDLLHelper.GetFunction<ShowImageDelegate>("ShowImage");
            return func(OLAObject, ptr);
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
            var func = OLAPlugDLLHelper.GetFunction<ShowImageFromFileDelegate>("ShowImageFromFile");
            return func(OLAObject, file);
        }

        /// <summary>
        /// 将图像中指定颜色范围内的像素替换为新颜色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <param name="color">目标颜色（BGR十六进制字符串）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long SetColorsToNewColor(long ptr, List<ColorModel> colorJson, string color){
            var func = OLAPlugDLLHelper.GetFunction<SetColorsToNewColorDelegate>("SetColorsToNewColor");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson), color);
        }

        /// <summary>
        /// 将图像中指定颜色范围内的像素替换为新颜色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">颜色范围定义（JSON）</param>
        /// <param name="color">目标颜色（BGR十六进制字符串）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long SetColorsToNewColor(long ptr, string colorJson, string color){
            var func = OLAPlugDLLHelper.GetFunction<SetColorsToNewColorDelegate>("SetColorsToNewColor");
            return func(OLAObject, ptr, colorJson, color);
        }

        /// <summary>
        /// 保留图像中指定颜色，其余颜色变为黑色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">要保留的颜色范围（JSON）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long RemoveOtherColors(long ptr, List<ColorModel> colorJson){
            var func = OLAPlugDLLHelper.GetFunction<RemoveOtherColorsDelegate>("RemoveOtherColors");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(colorJson));
        }

        /// <summary>
        /// 保留图像中指定颜色，其余颜色变为黑色
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="colorJson">要保留的颜色范围（JSON）</param>
        /// <returns>返回处理后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long RemoveOtherColors(long ptr, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<RemoveOtherColorsDelegate>("RemoveOtherColors");
            return func(OLAObject, ptr, colorJson);
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
            var func = OLAPlugDLLHelper.GetFunction<DrawRectangleDelegate>("DrawRectangle");
            return func(OLAObject, ptr, x1, y1, x2, y2, thickness, color);
        }

        /// <summary>
        /// 在图像上绘制圆形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">圆心X坐标</param>
        /// <param name="y">圆心Y坐标</param>
        /// <param name="radius">半径</param>
        /// <param name="thickness">线条粗细，负值表示填充</param>
        /// <param name="color">绘制颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawCircle(long ptr, int x, int y, int radius, int thickness, string color){
            var func = OLAPlugDLLHelper.GetFunction<DrawCircleDelegate>("DrawCircle");
            return func(OLAObject, ptr, x, y, radius, thickness, color);
        }

        /// <summary>
        /// 在图像上绘制填充多边形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pointJson">多边形顶点坐标（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">填充颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawFillPoly(long ptr, List<Point> pointJson, string color){
            var func = OLAPlugDLLHelper.GetFunction<DrawFillPolyDelegate>("DrawFillPoly");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(pointJson), color);
        }

        /// <summary>
        /// 在图像上绘制填充多边形
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pointJson">多边形顶点坐标（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">填充颜色（BGR格式）</param>
        /// <returns>返回绘制后的图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long DrawFillPoly(long ptr, string pointJson, string color){
            var func = OLAPlugDLLHelper.GetFunction<DrawFillPolyDelegate>("DrawFillPoly");
            return func(OLAObject, ptr, pointJson, color);
        }

        /// <summary>
        /// 从图像中解码二维码
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回解码的二维码内容字符串，需调用FreeStringPtr释放内存；失败返回0</returns>
        public string DecodeQRCode(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<DecodeQRCodeDelegate>("DecodeQRCode");
            return PtrToStringUTF8(func(OLAObject, ptr));
        }

        /// <summary>
        /// 生成二维码图像
        /// </summary>
        /// <param name="str">要编码的文本内容</param>
        /// <param name="pixelsPerModule">模块像素大小</param>
        /// <returns>返回二维码图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CreateQRCode(string str, int pixelsPerModule){
            var func = OLAPlugDLLHelper.GetFunction<CreateQRCodeDelegate>("CreateQRCode");
            return func(OLAObject, str, pixelsPerModule);
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
            var func = OLAPlugDLLHelper.GetFunction<CreateQRCodeExDelegate>("CreateQRCodeEx");
            return func(OLAObject, str, pixelsPerModule, version, correction_level, mode, structure_number);
        }

        /// <summary>
        /// 在动画图像序列中查找匹配帧（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">动画模板/序列句柄</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <param name="delay">动画帧间隔，单位毫秒</param>
        /// <param name="time">总识别时间，单位毫秒</param>
        /// <param name="threadCount">用于查找的线程数</param>
        /// <returns>匹配结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 线程数需要根据delay帧率自行调整，过小会导致识别时间到期未识别完，过大会导致CPU占用过大
        /// <br/>2. 当x1, y1, x2, y2都传0时，将搜索整个窗口客户区
        /// <br/>3. 识别结果最长等待时间为time + 1000ms
        /// <br/>4. 匹配类型的选择：
        /// <br/>5. 灰度匹配速度最快，但精度较低
        /// <br/>6. 彩色匹配精度较高，但速度较慢
        /// <br/>7. 透明匹配适用于带透明通道的图片
        /// <br/>8. 线程数的选择：
        /// <br/>9. 建议根据动画帧率和CPU核心数来设置
        /// <br/>10. 一般建议设置为CPU核心数的1-2倍
        /// <br/>11. 角度参数影响匹配时间和精度：
        /// <br/>12. 角度越小，匹配次数越多，时间越长
        /// <br/>13. 角度为0时速度最快，但可能错过旋转的目标
        /// <br/>14. 缩放比例应与窗口实际缩放比例一致
        /// <br/>15. DLL调用返回的字符串指针需要调用 FreeStringPtr 释放内存
        /// <br/>16. 返回的坐标是相对于绑定窗口客户区的坐标
        /// </remarks>
        public MatchResult MatchAnimationFromPtr(int x1, int y1, int x2, int y2, long templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var func = OLAPlugDLLHelper.GetFunction<MatchAnimationFromPtrDelegate>("MatchAnimationFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
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
        /// <param name="templ">模板图片的路径，可以是多个图片,比如"test.bmp|test2.bmp|test3.bmp"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="type">匹配类型
        ///<br/> 1: 灰度匹配，速度快
        ///<br/> 2: 彩色匹配
        ///<br/> 3: 透明匹配
        ///<br/> 4: 透透明彩色权重匹配
        ///<br/> 5: 普通彩色匹配
        /// </param>
        /// <param name="angle">旋转角度，每次匹配后旋转指定角度继续进行匹配,直到匹配成功,角度越小匹配次数越多时间越长。0为不旋转速度最快</param>
        /// <param name="scale">窗口缩放比例，默认为1 可以通过GetScaleFromWindows接口读取当前窗口缩放</param>
        /// <param name="delay">动画帧间隔，单位毫秒</param>
        /// <param name="time">总识别时间，单位毫秒</param>
        /// <param name="threadCount">用于查找的线程数</param>
        /// <returns>匹配结果</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 线程数需要根据delay帧率自行调整，过小会导致识别时间到期未识别完，过大会导致CPU占用过大
        /// </remarks>
        public MatchResult MatchAnimationFromPath(int x1, int y1, int x2, int y2, string templ, double matchVal, int type, double angle, double scale, int delay, int time, int threadCount){
            var func = OLAPlugDLLHelper.GetFunction<MatchAnimationFromPathDelegate>("MatchAnimationFromPath");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, matchVal, type, angle, scale, delay, time, threadCount));
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
            var func = OLAPlugDLLHelper.GetFunction<RemoveImageDiffDelegate>("RemoveImageDiff");
            return func(OLAObject, image1, image2);
        }

        /// <summary>
        /// 获取图像的BMP格式数据
        /// </summary>
        /// <param name="imgPtr">OLAImage对象的地址</param>
        /// <param name="data">返回图片的数据指针</param>
        /// <param name="size">返回图片的数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int GetImageBmpData(long imgPtr, out long data, out int size){
            var func = OLAPlugDLLHelper.GetFunction<GetImageBmpDataDelegate>("GetImageBmpData");
            return func(OLAObject, imgPtr, out data, out size);
        }

        /// <summary>
        /// 获取图像的PNG格式数据
        /// </summary>
        /// <param name="imgPtr">OLAImage对象的地址</param>
        /// <param name="data">返回图片的数据指针</param>
        /// <param name="size">返回图片的数据长度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int GetImagePngData(long imgPtr, out long data, out int size){
            var func = OLAPlugDLLHelper.GetFunction<GetImagePngDataDelegate>("GetImagePngData");
            return func(OLAObject, imgPtr, out data, out size);
        }

        /// <summary>
        /// 释放由GetImageData等接口返回的图像数据指针
        /// </summary>
        /// <param name="screenPtr">图像数据指针</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int FreeImageData(long screenPtr){
            var func = OLAPlugDLLHelper.GetFunction<FreeImageDataDelegate>("FreeImageData");
            return func(OLAObject, screenPtr);
        }

        /// <summary>
        /// 对图像像素进行缩放处理
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="pixelsPerModule">像素缩放系数</param>
        /// <returns>处理后图像句柄，失败返回0</returns>
        public long ScalePixels(long ptr, int pixelsPerModule){
            var func = OLAPlugDLLHelper.GetFunction<ScalePixelsDelegate>("ScalePixels");
            return func(OLAObject, ptr, pixelsPerModule);
        }

        /// <summary>
        /// 创建图片
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="color">初始填充颜色（BGR格式）</param>
        /// <returns>返回新图像数据指针，需调用FreeImageData释放内存；失败返回0</returns>
        public long CreateImage(int width, int height, string color){
            var func = OLAPlugDLLHelper.GetFunction<CreateImageDelegate>("CreateImage");
            return func(OLAObject, width, height, color);
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
            var func = OLAPlugDLLHelper.GetFunction<SetPixelDelegate>("SetPixel");
            return func(OLAObject, image, x, y, color);
        }

        /// <summary>
        /// 批量设置图像中多个像素点的颜色
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="points">坐标点数组（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">颜色（BGR十六进制字符串）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetPixelList(long image, List<Point> points, string color){
            var func = OLAPlugDLLHelper.GetFunction<SetPixelListDelegate>("SetPixelList");
            return func(OLAObject, image, JsonConvert.SerializeObject(points), color);
        }

        /// <summary>
        /// 批量设置图像中多个像素点的颜色
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="points">坐标点数组（JSON），如[{"x":10,"y":10}]</param>
        /// <param name="color">颜色（BGR十六进制字符串）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetPixelList(long image, string points, string color){
            var func = OLAPlugDLLHelper.GetFunction<SetPixelListDelegate>("SetPixelList");
            return func(OLAObject, image, points, color);
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
            var func = OLAPlugDLLHelper.GetFunction<ConcatImageDelegate>("ConcatImage");
            return func(OLAObject, image1, image2, gap, color, dir);
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
            var func = OLAPlugDLLHelper.GetFunction<CoverImageDelegate>("CoverImage");
            return func(OLAObject, image1, image2, x, y, alpha);
        }

        /// <summary>
        /// 按角度旋转图像
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <returns>新图像句柄，失败返回0</returns>
        public long RotateImage(long image, double angle){
            var func = OLAPlugDLLHelper.GetFunction<RotateImageDelegate>("RotateImage");
            return func(OLAObject, image, angle);
        }

        /// <summary>
        /// 将图像编码为Base64字符串
        /// </summary>
        /// <param name="image">图像句柄</param>
        /// <returns>Base64字符串指针，需调用FreeStringPtr释放</returns>
        public string ImageToBase64(long image){
            var func = OLAPlugDLLHelper.GetFunction<ImageToBase64Delegate>("ImageToBase64");
            return PtrToStringUTF8(func(OLAObject, image));
        }

        /// <summary>
        /// 将Base64字符串解码为图像
        /// </summary>
        /// <param name="base64">Base64字符串</param>
        /// <returns>图像句柄，失败返回0</returns>
        public long Base64ToImage(string base64){
            var func = OLAPlugDLLHelper.GetFunction<Base64ToImageDelegate>("Base64ToImage");
            return func(OLAObject, base64);
        }

        /// <summary>
        /// 十六进制颜色解析为ARGB
        /// </summary>
        /// <param name="hex">十六进制颜色（如#AARRGGBB或#RRGGBB）</param>
        /// <param name="a">返回Alpha（输出）</param>
        /// <param name="r">返回Red（输出）</param>
        /// <param name="g">返回Green（输出）</param>
        /// <param name="b">返回Blue（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int Hex2ARGB(string hex, out int a, out int r, out int g, out int b){
            var func = OLAPlugDLLHelper.GetFunction<Hex2ARGBDelegate>("Hex2ARGB");
            return func(OLAObject, hex, out a, out r, out g, out b);
        }

        /// <summary>
        /// 十六进制颜色解析为RGB
        /// </summary>
        /// <param name="hex">十六进制颜色（如#RRGGBB）</param>
        /// <param name="r">返回Red（输出）</param>
        /// <param name="g">返回Green（输出）</param>
        /// <param name="b">返回Blue（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int Hex2RGB(string hex, out int r, out int g, out int b){
            var func = OLAPlugDLLHelper.GetFunction<Hex2RGBDelegate>("Hex2RGB");
            return func(OLAObject, hex, out r, out g, out b);
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
            var func = OLAPlugDLLHelper.GetFunction<ARGB2HexDelegate>("ARGB2Hex");
            return PtrToStringUTF8(func(OLAObject, a, r, g, b));
        }

        /// <summary>
        /// 将RGB颜色转换为十六进制字符串
        /// </summary>
        /// <param name="r">红色值</param>
        /// <param name="g">绿色值</param>
        /// <param name="b">蓝色值</param>
        /// <returns>十六进制字符串</returns>
        public string RGB2Hex(int r, int g, int b){
            var func = OLAPlugDLLHelper.GetFunction<RGB2HexDelegate>("RGB2Hex");
            return PtrToStringUTF8(func(OLAObject, r, g, b));
        }

        /// <summary>
        /// 将十六进制颜色转换为HSV颜色
        /// </summary>
        /// <param name="hex">十六进制颜色</param>
        /// <returns>HSV颜色</returns>
        public string Hex2HSV(string hex){
            var func = OLAPlugDLLHelper.GetFunction<Hex2HSVDelegate>("Hex2HSV");
            return PtrToStringUTF8(func(OLAObject, hex));
        }

        /// <summary>
        /// 将RGB颜色转换为HSV颜色
        /// </summary>
        /// <param name="r">红色值</param>
        /// <param name="g">绿色值</param>
        /// <param name="b">蓝色值</param>
        /// <returns>HSV颜色</returns>
        public string RGB2HSV(int r, int g, int b){
            var func = OLAPlugDLLHelper.GetFunction<RGB2HSVDelegate>("RGB2HSV");
            return PtrToStringUTF8(func(OLAObject, r, g, b));
        }

        /// <summary>
        /// 判断屏幕坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="x1">X坐标</param>
        /// <param name="y1">Y坐标</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpColor(int x1, int y1, string colorStart, string colorEnd){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorDelegate>("CmpColor");
            return func(OLAObject, x1, y1, colorStart, colorEnd);
        }

        /// <summary>
        /// 判断图像坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpColorPtr(long ptr, int x, int y, string colorStart, string colorEnd){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorPtrDelegate>("CmpColorPtr");
            return func(OLAObject, ptr, x, y, colorStart, colorEnd);
        }

        /// <summary>
        /// 判断屏幕坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="x1">X坐标</param>
        /// <param name="y1">Y坐标</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpColorEx(int x1, int y1, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorExDelegate>("CmpColorEx");
            return func(OLAObject, x1, y1, colorJson);
        }

        /// <summary>
        /// 判断图像坐标点颜色是否在指定范围
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpColorPtrEx(long ptr, int x, int y, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorPtrExDelegate>("CmpColorPtrEx");
            return func(OLAObject, ptr, x, y, colorJson);
        }

        /// <summary>
        /// 判断十六进制颜色是否在指定范围
        /// </summary>
        /// <param name="hex">颜色（十六进制）</param>
        /// <param name="colorJson">颜色（JSON）</param>
        /// <returns>操作结果
        ///<br/>0: 否
        ///<br/>1: 是
        /// </returns>
        public int CmpColorHexEx(string hex, string colorJson){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorHexExDelegate>("CmpColorHexEx");
            return func(OLAObject, hex, colorJson);
        }

        /// <summary>
        /// 判断十六进制颜色是否在指定范围
        /// </summary>
        /// <param name="hex">颜色（十六进制）</param>
        /// <param name="colorStart">起始颜色（含）</param>
        /// <param name="colorEnd">结束颜色（含）</param>
        /// <returns>操作结果
        ///<br/>0: 失败，未找到符合条件的颜色点
        ///<br/>1: 成功，找到符合条件的颜色点
        /// </returns>
        public int CmpColorHex(string hex, string colorStart, string colorEnd){
            var func = OLAPlugDLLHelper.GetFunction<CmpColorHexDelegate>("CmpColorHex");
            return func(OLAObject, hex, colorStart, colorEnd);
        }

        /// <summary>
        /// 基于种子点获取连通域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="points">种子点数组（JSON）</param>
        /// <param name="tolerance">容差阈值</param>
        /// <returns>连通域点数组字符串指针（JSON），需调用FreeStringPtr释放</returns>
        public long GetConnectedComponents(long ptr, List<Point> points, int tolerance){
            var func = OLAPlugDLLHelper.GetFunction<GetConnectedComponentsDelegate>("GetConnectedComponents");
            return func(OLAObject, ptr, JsonConvert.SerializeObject(points), tolerance);
        }

        /// <summary>
        /// 基于种子点获取连通域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="points">种子点数组（JSON）</param>
        /// <param name="tolerance">容差阈值</param>
        /// <returns>连通域点数组字符串指针（JSON），需调用FreeStringPtr释放</returns>
        public long GetConnectedComponents(long ptr, string points, int tolerance){
            var func = OLAPlugDLLHelper.GetFunction<GetConnectedComponentsDelegate>("GetConnectedComponents");
            return func(OLAObject, ptr, points, tolerance);
        }

        /// <summary>
        /// 基于几何与边缘特征检测指针（针状/箭头）方向
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x">参考点X坐标</param>
        /// <param name="y">参考点Y坐标</param>
        /// <returns>方向角（度）</returns>
        public double DetectPointerDirection(long ptr, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<DetectPointerDirectionDelegate>("DetectPointerDirection");
            return func(OLAObject, ptr, x, y);
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
            var func = OLAPlugDLLHelper.GetFunction<DetectPointerDirectionByFeaturesDelegate>("DetectPointerDirectionByFeatures");
            return func(OLAObject, ptr, templatePtr, x, y, useTemplate);
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
            var func = OLAPlugDLLHelper.GetFunction<FastMatchDelegate>("FastMatch");
            var result = PtrToStringUTF8(func(OLAObject, ptr, templatePtr, matchVal, type, angle, scale));
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
            var func = OLAPlugDLLHelper.GetFunction<FastROIDelegate>("FastROI");
            return func(OLAObject, ptr);
        }

        /// <summary>
        /// 获取ROI区域
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="x1">返回区域左上角X坐标（输出）</param>
        /// <param name="y1">返回区域左上角Y坐标（输出）</param>
        /// <param name="x2">返回区域右下角X坐标（输出）</param>
        /// <param name="y2">返回区域右下角Y坐标（输出）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int GetROIRegion(long ptr, out int x1, out int y1, out int x2, out int y2){
            var func = OLAPlugDLLHelper.GetFunction<GetROIRegionDelegate>("GetROIRegion");
            return func(OLAObject, ptr, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 获取前景点
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>前景点数组字符串指针（JSON，如[{"x":10,"y":10}]]），需调用FreeStringPtr释放</returns>
        public List<Point> GetForegroundPoints(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<GetForegroundPointsDelegate>("GetForegroundPoints");
            var result = PtrToStringUTF8(func(OLAObject, ptr));
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
            var func = OLAPlugDLLHelper.GetFunction<ConvertColorDelegate>("ConvertColor");
            return func(OLAObject, ptr, type);
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
            var func = OLAPlugDLLHelper.GetFunction<ThresholdDelegate>("Threshold");
            return func(OLAObject, ptr, thresh, maxVal, type);
        }

        /// <summary>
        /// 去除孤岛
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <param name="minArea">最小面积</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long RemoveIslands(long ptr, int minArea){
            var func = OLAPlugDLLHelper.GetFunction<RemoveIslandsDelegate>("RemoveIslands");
            return func(OLAObject, ptr, minArea);
        }

        /// <summary>
        /// 形态学梯度
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphGradient(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<MorphGradientDelegate>("MorphGradient");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学顶帽
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphTophat(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<MorphTophatDelegate>("MorphTophat");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学黑帽
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphBlackhat(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<MorphBlackhatDelegate>("MorphBlackhat");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 膨胀
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Dilation(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<DilationDelegate>("Dilation");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 腐蚀
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Erosion(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<ErosionDelegate>("Erosion");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 高斯模糊
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long GaussianBlur(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<GaussianBlurDelegate>("GaussianBlur");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 图像锐化
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long Sharpen(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<SharpenDelegate>("Sharpen");
            return func(OLAObject, ptr);
        }

        /// <summary>
        /// Canny边缘检测
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回边缘图像句柄，失败返回0</returns>
        public long CannyEdge(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<CannyEdgeDelegate>("CannyEdge");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 翻转图像
        /// </summary>
        /// <param name="ptr">图像指针</param>
        /// <param name="flipCode">翻转代码
        ///<br/> 0: X轴
        ///<br/> 1: Y轴
        ///<br/> 2: 同时翻转
        /// </param>
        /// <returns>返回翻转后的图像句柄，失败返回0</returns>
        public long Flip(long ptr, int flipCode){
            var func = OLAPlugDLLHelper.GetFunction<FlipDelegate>("Flip");
            return func(OLAObject, ptr, flipCode);
        }

        /// <summary>
        /// 形态学开运算
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphOpen(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<MorphOpenDelegate>("MorphOpen");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 形态学闭运算
        /// </summary>
        /// <param name="ptr">图像指针，由图像处理函数返回</param>
        /// <param name="kernelSize">形态学核的大小，通常为奇数（3、5、7等）</param>
        /// <returns>返回处理后的图像句柄，失败返回0</returns>
        public long MorphClose(long ptr, int kernelSize){
            var func = OLAPlugDLLHelper.GetFunction<MorphCloseDelegate>("MorphClose");
            return func(OLAObject, ptr, kernelSize);
        }

        /// <summary>
        /// 骨架化
        /// </summary>
        /// <param name="ptr">图像句柄</param>
        /// <returns>返回骨架化后的图像句柄，失败返回0</returns>
        public long Skeletonize(long ptr){
            var func = OLAPlugDLLHelper.GetFunction<SkeletonizeDelegate>("Skeletonize");
            return func(OLAObject, ptr);
        }

        /// <summary>
        /// 从路径拼接图片
        /// </summary>
        /// <param name="path">图片目录或通配路径</param>
        /// <param name="trajectory">返回轨迹数据指针（输出，可为0）</param>
        /// <returns>返回拼接后的图像句柄，失败返回0</returns>
        public long ImageStitchFromPath(string path, out long trajectory){
            var func = OLAPlugDLLHelper.GetFunction<ImageStitchFromPathDelegate>("ImageStitchFromPath");
            return func(OLAObject, path, out trajectory);
        }

        /// <summary>
        /// 创建拼接图片实例
        /// </summary>
        /// <returns>返回拼接实例句柄，失败返回0</returns>
        public long ImageStitchCreate(){
            var func = OLAPlugDLLHelper.GetFunction<ImageStitchCreateDelegate>("ImageStitchCreate");
            return func(OLAObject);
        }

        /// <summary>
        /// 拼接图片
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <param name="image">图像句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ImageStitchAppend(long imageStitch, long image){
            var func = OLAPlugDLLHelper.GetFunction<ImageStitchAppendDelegate>("ImageStitchAppend");
            return func(OLAObject, imageStitch, image);
        }

        /// <summary>
        /// 获取拼接图片结果
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <param name="trajectory">输出参数，可为0；返回轨迹数据的字符串指针，需使用 FreeStringPtr 释放</param>
        /// <returns>返回拼接后的图像句柄，失败返回0</returns>
        public long ImageStitchGetResult(long imageStitch, out long trajectory){
            var func = OLAPlugDLLHelper.GetFunction<ImageStitchGetResultDelegate>("ImageStitchGetResult");
            return func(OLAObject, imageStitch, out trajectory);
        }

        /// <summary>
        /// 释放拼接图片实例
        /// </summary>
        /// <param name="imageStitch">拼接实例句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ImageStitchFree(long imageStitch){
            var func = OLAPlugDLLHelper.GetFunction<ImageStitchFreeDelegate>("ImageStitchFree");
            return func(OLAObject, imageStitch);
        }

        /// <summary>
        /// 压缩二值化图像成字符串
        /// </summary>
        /// <param name="image">拼接实例句柄</param>
        /// <returns>压缩结果字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string BitPacking(long image){
            var func = OLAPlugDLLHelper.GetFunction<BitPackingDelegate>("BitPacking");
            return PtrToStringUTF8(func(OLAObject, image));
        }

        /// <summary>
        /// 解压缩字符串成二值化图像
        /// </summary>
        /// <param name="imageStr">BitPacking压缩结果</param>
        /// <returns>返回图像句柄,失败返回0</returns>
        public long BitUnpacking(string imageStr){
            var func = OLAPlugDLLHelper.GetFunction<BitUnpackingDelegate>("BitUnpacking");
            return func(OLAObject, imageStr);
        }

        /// <summary>
        /// 设置图片缓存开关
        /// </summary>
        /// <param name="enable">是否启用图片缓存
        ///<br/> 0: 关闭
        ///<br/> 1: 开启
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int SetImageCache(int enable){
            var func = OLAPlugDLLHelper.GetFunction<SetImageCacheDelegate>("SetImageCache");
            return func(enable);
        }

        /// <summary>
        /// 在指定图片中查找指定图像（使用内存数据）
        /// </summary>
        /// <param name="source">OLAImage对象的地址</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <returns>匹配结果</returns>
        public MatchResult FindImageFromPtr(long source, long templ, string deltaColor, double matchVal, int dir){
            var func = OLAPlugDLLHelper.GetFunction<FindImageFromPtrDelegate>("FindImageFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, deltaColor, matchVal, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在指定图片中查找指定图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="source">OLAImage对象的地址</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>返回所有匹配结果字符串</returns>
        public List<MatchResult> FindImageFromPtrAll(long source, long templ, string deltaColor, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindImageFromPtrAllDelegate>("FindImageFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, deltaColor, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在指定图片中查找指定图像（使用文件路径）
        /// </summary>
        /// <param name="source">源图片的路径</param>
        /// <param name="templ">模板图片的路径，可以是多个图片，比如”test.bmp|test2.bmp|test3.bmp”</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <returns>匹配结果</returns>
        public MatchResult FindImageFromPath(string source, string templ, string deltaColor, double matchVal, int dir){
            var func = OLAPlugDLLHelper.GetFunction<FindImageFromPathDelegate>("FindImageFromPath");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, deltaColor, matchVal, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在指定图片中查找指定图像的所有匹配位置（使用文件路径）
        /// </summary>
        /// <param name="source">源图片的路径</param>
        /// <param name="templ">模板图片的路径，可以是多个图片，比如”test.bmp|test2.bmp|test3.bmp”</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>返回所有匹配结果字符串</returns>
        public List<MatchResult> FindImageFromPathAll(string source, string templ, string deltaColor, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindImageFromPathAllDelegate>("FindImageFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, source, templ, deltaColor, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <returns>匹配结果</returns>
        public MatchResult FindWindowsFromPtr(int x1, int y1, int x2, int y2, long templ, string deltaColor, double matchVal, int dir){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowsFromPtrDelegate>("FindWindowsFromPtr");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, deltaColor, matchVal, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像的所有匹配位置（使用内存数据）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">OLAImage对象的地址,由LoadImage 等接口生成</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>返回所有匹配结果字符串</returns>
        public List<MatchResult> FindWindowsFromPtrAll(int x1, int y1, int x2, int y2, long templ, string deltaColor, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowsFromPtrAllDelegate>("FindWindowsFromPtrAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, deltaColor, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">模板图片的路径，可以是多个图片，比如”test.bmp|test2.bmp|test3.bmp”</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <param name="dir">查找方向
        ///<br/> 0: 从左到右,从上到下
        ///<br/> 1: 从左到右,从下到上
        ///<br/> 2: 从右到左,从上到下
        ///<br/> 3: 从右到左,从下到上
        ///<br/> 4: 从中心往外查找
        ///<br/> 5: 从上到下,从左到右
        ///<br/> 6: 从上到下,从右到左
        ///<br/> 7: 从下到上,从左到右
        ///<br/> 8: 从下到上,从右到左
        /// </param>
        /// <returns>匹配结果</returns>
        public MatchResult FindWindowsFromPath(int x1, int y1, int x2, int y2, string templ, string deltaColor, double matchVal, int dir){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowsFromPathDelegate>("FindWindowsFromPath");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, deltaColor, matchVal, dir));
            if (string.IsNullOrEmpty(result))
            {
                return new MatchResult();
            }
            return JsonConvert.DeserializeObject<MatchResult>(result);
        }

        /// <summary>
        /// 在绑定窗口中查找指定图像的所有匹配位置（使用文件路径）
        /// </summary>
        /// <param name="x1">搜索区域左上角X坐标</param>
        /// <param name="y1">搜索区域左上角Y坐标</param>
        /// <param name="x2">搜索区域右下角X坐标</param>
        /// <param name="y2">搜索区域右下角Y坐标</param>
        /// <param name="templ">模板图片的路径，可以是多个图片，比如”test.bmp|test2.bmp|test3.bmp”</param>
        /// <param name="deltaColor">颜色差值，格式为"RRGGBB"，如"101010"</param>
        /// <param name="matchVal">相似度，如0.85，最大为1</param>
        /// <returns>返回所有匹配结果字符串</returns>
        public List<MatchResult> FindWindowsFromPathAll(int x1, int y1, int x2, int y2, string templ, string deltaColor, double matchVal){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowsFromPathAllDelegate>("FindWindowsFromPathAll");
            var result = PtrToStringUTF8(func(OLAObject, x1, y1, x2, y2, templ, deltaColor, matchVal));
            if (string.IsNullOrEmpty(result))
            {
                return new List<MatchResult>();
            }
            return JsonConvert.DeserializeObject<List<MatchResult>>(result);
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryOpenKeyDelegate>("RegistryOpenKey");
            return func(OLAObject, rootKey, subKey);
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryCreateKeyDelegate>("RegistryCreateKey");
            return func(OLAObject, rootKey, subKey);
        }

        /// <summary>
        /// 关闭注册表键句柄
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 关闭后句柄失效，不可再使用
        /// </remarks>
        public int RegistryCloseKey(long key){
            var func = OLAPlugDLLHelper.GetFunction<RegistryCloseKeyDelegate>("RegistryCloseKey");
            return func(OLAObject, key);
        }

        /// <summary>
        /// 判断指定注册表键是否存在
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <returns>查询结果
        ///<br/>0: 表示不存在
        ///<br/>1: 表示存在
        /// </returns>
        public int RegistryKeyExists(int rootKey, string subKey){
            var func = OLAPlugDLLHelper.GetFunction<RegistryKeyExistsDelegate>("RegistryKeyExists");
            return func(OLAObject, rootKey, subKey);
        }

        /// <summary>
        /// 删除指定注册表键
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <param name="recursive">是否递归删除子键
        ///<br/> 0: 表示仅删除当前键
        ///<br/> 1: 表示递归删除
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 建议谨慎使用递归删除，避免误删系统关键配置
        /// </remarks>
        public int RegistryDeleteKey(int rootKey, string subKey, int recursive){
            var func = OLAPlugDLLHelper.GetFunction<RegistryDeleteKeyDelegate>("RegistryDeleteKey");
            return func(OLAObject, rootKey, subKey, recursive);
        }

        /// <summary>
        /// 设置字符串类型的注册表值（REG_SZ）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称，空字符串表示默认值</param>
        /// <param name="value">字符串值内容</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int RegistrySetString(long key, string valueName, string value){
            var func = OLAPlugDLLHelper.GetFunction<RegistrySetStringDelegate>("RegistrySetString");
            return func(OLAObject, key, valueName, value);
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetStringDelegate>("RegistryGetString");
            return PtrToStringUTF8(func(OLAObject, key, valueName));
        }

        /// <summary>
        /// 设置 32 位整型注册表值（REG_DWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <param name="value">要写入的 32 位整型值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int RegistrySetDword(long key, string valueName, int value){
            var func = OLAPlugDLLHelper.GetFunction<RegistrySetDwordDelegate>("RegistrySetDword");
            return func(OLAObject, key, valueName, value);
        }

        /// <summary>
        /// 读取 32 位整型注册表值（REG_DWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>读取到的数值；如果值不存在或类型不匹配，则返回 0</returns>
        public int RegistryGetDword(long key, string valueName){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetDwordDelegate>("RegistryGetDword");
            return func(OLAObject, key, valueName);
        }

        /// <summary>
        /// 设置 64 位整型注册表值（REG_QWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <param name="value">要写入的 64 位整型值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int RegistrySetQword(long key, string valueName, long value){
            var func = OLAPlugDLLHelper.GetFunction<RegistrySetQwordDelegate>("RegistrySetQword");
            return func(OLAObject, key, valueName, value);
        }

        /// <summary>
        /// 读取 64 位整型注册表值（REG_QWORD）
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>读取到的数值；如果值不存在或类型不匹配，则返回 0</returns>
        public long RegistryGetQword(long key, string valueName){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetQwordDelegate>("RegistryGetQword");
            return func(OLAObject, key, valueName);
        }

        /// <summary>
        /// 删除指定名称的注册表值
        /// </summary>
        /// <param name="key">注册表键句柄，由 RegistryOpenKey 或 RegistryCreateKey 返回</param>
        /// <param name="valueName">值名称</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 表示成功或值不存在
        /// </returns>
        public int RegistryDeleteValue(long key, string valueName){
            var func = OLAPlugDLLHelper.GetFunction<RegistryDeleteValueDelegate>("RegistryDeleteValue");
            return func(OLAObject, key, valueName);
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryEnumSubKeysDelegate>("RegistryEnumSubKeys");
            return PtrToStringUTF8(func(OLAObject, key));
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryEnumValuesDelegate>("RegistryEnumValues");
            return PtrToStringUTF8(func(OLAObject, key));
        }

        /// <summary>
        /// 设置环境变量，内部基于注册表与系统 API 实现
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <param name="value">环境变量值</param>
        /// <param name="systemWide">是否为系统级环境变量
        ///<br/> 0: 表示当前用户
        ///<br/> 1: 表示系统级
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int RegistrySetEnvironmentVariable(string name, string value, int systemWide){
            var func = OLAPlugDLLHelper.GetFunction<RegistrySetEnvironmentVariableDelegate>("RegistrySetEnvironmentVariable");
            return func(OLAObject, name, value, systemWide);
        }

        /// <summary>
        /// 获取环境变量的值
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <param name="systemWide">是否从系统级环境变量读取
        ///<br/> 0: 表示当前用户
        ///<br/> 1: 表示系统级
        /// </param>
        /// <returns>环境变量值的字符串句柄，如果不存在则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetEnvironmentVariable(string name, int systemWide){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetEnvironmentVariableDelegate>("RegistryGetEnvironmentVariable");
            return PtrToStringUTF8(func(OLAObject, name, systemWide));
        }

        /// <summary>
        /// 获取用户配置相关的注册表路径
        /// </summary>
        /// <returns>注册表路径字符串句柄，例如 "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserShell Folders"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetUserRegistryPath(){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetUserRegistryPathDelegate>("RegistryGetUserRegistryPath");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 获取系统配置相关的注册表路径
        /// </summary>
        /// <returns>注册表路径字符串句柄，例如 "Software\\Microsoft\\Windows\\CurrentVersion"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetSystemRegistryPath(){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetSystemRegistryPathDelegate>("RegistryGetSystemRegistryPath");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 备份注册表键到文件
        /// </summary>
        /// <param name="rootKey">根键类型，见 OlaRegistryRootKey</param>
        /// <param name="subKey">子键路径</param>
        /// <param name="filePath">备份文件路径（.reg 格式）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功 * @note 文件将以标准 .reg 格式保存，可以使用 regedit 导入
        /// </returns>
        public int RegistryBackupToFile(int rootKey, string subKey, string filePath){
            var func = OLAPlugDLLHelper.GetFunction<RegistryBackupToFileDelegate>("RegistryBackupToFile");
            return func(OLAObject, rootKey, subKey, filePath);
        }

        /// <summary>
        /// 从文件恢复注册表键
        /// </summary>
        /// <param name="filePath">备份文件路径（.reg 格式）</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功 * @note 文件必须是标准 .reg 格式
        /// </returns>
        public int RegistryRestoreFromFile(string filePath){
            var func = OLAPlugDLLHelper.GetFunction<RegistryRestoreFromFileDelegate>("RegistryRestoreFromFile");
            return func(OLAObject, filePath);
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryCompareKeysDelegate>("RegistryCompareKeys");
            return PtrToStringUTF8(func(OLAObject, rootKey1, subKey1, rootKey2, subKey2));
        }

        /// <summary>
        /// 搜索注册表键
        /// </summary>
        /// <param name="rootKey">根键类型</param>
        /// <param name="searchPath">搜索起始路径</param>
        /// <param name="searchPattern">搜索模式（支持通配符 * 和 ?）</param>
        /// <param name="recursive">是否递归搜索
        ///<br/> 0: 表示仅搜索当前层级
        ///<br/> 1: 表示递归
        /// </param>
        /// <returns>JSON 数组字符串句柄，包含匹配的键路径，例如 ["path1","path2"]</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistrySearchKeys(int rootKey, string searchPath, string searchPattern, int recursive){
            var func = OLAPlugDLLHelper.GetFunction<RegistrySearchKeysDelegate>("RegistrySearchKeys");
            return PtrToStringUTF8(func(OLAObject, rootKey, searchPath, searchPattern, recursive));
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
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetInstalledSoftwareDelegate>("RegistryGetInstalledSoftware");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 获取 Windows 版本信息
        /// </summary>
        /// <returns>JSON 对象字符串句柄，包含 Windows版本信息：productName、currentVersion、currentBuild、releaseId 等</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 返回的字符串句柄需使用 FreeStringPtr 释放
        /// </remarks>
        public string RegistryGetWindowsVersion(){
            var func = OLAPlugDLLHelper.GetFunction<RegistryGetWindowsVersionDelegate>("RegistryGetWindowsVersion");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="dbName">数据库文件路径</param>
        /// <param name="password">数据库密码</param>
        /// <returns>数据库对象，若打开失败，返回0</returns>
        public long CreateDatabase(string dbName, string password){
            var func = OLAPlugDLLHelper.GetFunction<CreateDatabaseDelegate>("CreateDatabase");
            return func(OLAObject, dbName, password);
        }

        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <param name="dbName">数据库文件路径</param>
        /// <param name="password">数据库密码</param>
        /// <returns>数据库对象，若打开失败，返回0</returns>
        public long OpenDatabase(string dbName, string password){
            var func = OLAPlugDLLHelper.GetFunction<OpenDatabaseDelegate>("OpenDatabase");
            return func(OLAObject, dbName, password);
        }

        /// <summary>
        /// 打开内存数据库连接
        /// </summary>
        /// <param name="address">数据库内存地址</param>
        /// <param name="size">数据库内存大小</param>
        /// <param name="password">数据库密码</param>
        /// <returns>数据库连接句柄，如果打开失败则返回 0</returns>
        public long OpenMemoryDatabase(long address, int size, string password){
            var func = OLAPlugDLLHelper.GetFunction<OpenMemoryDatabaseDelegate>("OpenMemoryDatabase");
            return func(OLAObject, address, size, password);
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
            var func = OLAPlugDLLHelper.GetFunction<GetDatabaseErrorDelegate>("GetDatabaseError");
            return PtrToStringUTF8(func(OLAObject, db));
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于关闭由 OpenDatabase 接口打开的数据库连接
        /// <br/>2. 关闭连接后，传入的数据库句柄将失效，不能再用于其他数据库操作
        /// <br/>3. 即使关闭操作失败，也应认为该连接已不可用，并丢弃句柄
        /// <br/>4. 为防止资源泄漏，每个成功打开的数据库连接都应调用此接口进行关闭
        /// </remarks>
        public int CloseDatabase(long db){
            var func = OLAPlugDLLHelper.GetFunction<CloseDatabaseDelegate>("CloseDatabase");
            return func(OLAObject, db);
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
            var func = OLAPlugDLLHelper.GetFunction<GetAllTableNamesDelegate>("GetAllTableNames");
            return PtrToStringUTF8(func(OLAObject, db));
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
            var func = OLAPlugDLLHelper.GetFunction<GetTableInfoDelegate>("GetTableInfo");
            return PtrToStringUTF8(func(OLAObject, db, tableName));
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
            var func = OLAPlugDLLHelper.GetFunction<GetTableInfoDetailDelegate>("GetTableInfoDetail");
            return PtrToStringUTF8(func(OLAObject, db, tableName));
        }

        /// <summary>
        /// 执行一条SQL语句（非查询类）
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于执行 INSERT, UPDATE, DELETE, CREATE TABLE 等修改数据库内容的SQL语句
        /// <br/>2. 对于 INSERT 语句，如果表有自增主键，新插入行的主键值可以通过其他接口获取
        /// <br/>3. 执行成功表示SQL语句被正确解析并执行，但不保证有数据行被实际修改
        /// <br/>4. 如果SQL语句语法错误或违反约束，将返回 0，可通过 GetDatabaseError 获取错误信息
        /// </remarks>
        public int ExecuteSql(long db, string sql){
            var func = OLAPlugDLLHelper.GetFunction<ExecuteSqlDelegate>("ExecuteSql");
            return func(OLAObject, db, sql);
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
            var func = OLAPlugDLLHelper.GetFunction<ExecuteScalarDelegate>("ExecuteScalar");
            return func(OLAObject, db, sql);
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
            var func = OLAPlugDLLHelper.GetFunction<ExecuteReaderDelegate>("ExecuteReader");
            return func(OLAObject, db, sql);
        }

        /// <summary>
        /// 读取结果集的下一行数据
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <returns>操作结果
        ///<br/>-1: 发生错误返回
        ///<br/>0: 没有更多数据返回
        ///<br/>1: 成功读取到下一行返回
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于遍历由 ExecuteReader 生成的结果集
        /// <br/>2. 调用此函数后，结果集的当前位置会移动到下一行
        /// <br/>3. 在首次调用 Read 前，结果集不指向任何有效数据行
        /// <br/>4. 返回 1 表示成功读取了一行，此时可以使用 GetXXXByColumnName 或 GetXXX 系列函数获取该行数据
        /// </remarks>
        public int Read(long stmt){
            var func = OLAPlugDLLHelper.GetFunction<ReadDelegate>("Read");
            return func(OLAObject, stmt);
        }

        /// <summary>
        /// 获取结果集中数据行的总数
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <returns>数据行的总数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回结果集中包含的总行数
        /// <br/>2. 对于大型结果集，此操作可能需要遍历整个结果集，性能开销较大
        /// <br/>3. 某些数据库驱动可能不支持直接获取总行数，此时可能返回 -1 或其他错误值
        /// <br/>4. 在调用 Read 遍历结果集前后调用此函数，返回值应相同
        /// </remarks>
        public int GetDataCount(long stmt){
            var func = OLAPlugDLLHelper.GetFunction<GetDataCountDelegate>("GetDataCount");
            return func(OLAObject, stmt);
        }

        /// <summary>
        /// 获取结果集中列的总数
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <returns>列的总数</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数返回结果集包含的列（字段）的数量
        /// <br/>2. 此值在结果集的生命周期内是固定的，不会改变
        /// <br/>3. 在首次调用 Read 前或后调用此函数均可，结果相同
        /// <br/>4. 获取列数后，可以通过 GetColumnName, GetColumnType 等函数获取每列的元信息
        /// </remarks>
        public int GetColumnCount(long stmt){
            var func = OLAPlugDLLHelper.GetFunction<GetColumnCountDelegate>("GetColumnCount");
            return func(OLAObject, stmt);
        }

        /// <summary>
        /// 根据列索引获取列名
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>列名的字符串指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于获取结果集中指定位置列的名称
        /// <br/>2. 索引从 0 开始，最大值为 GetColumnCount(reader) - 1
        /// <br/>3. 如果 columnIndex 超出范围，行为是未定义的，可能返回 NULL 或错误指针
        /// <br/>4. 返回的字符串指针由系统管理，调用者无需手动释放内存
        /// </remarks>
        public string GetColumnName(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetColumnNameDelegate>("GetColumnName");
            return PtrToStringUTF8(func(OLAObject, stmt, iCol));
        }

        /// <summary>
        /// 根据列索引获取列的索引（冗余函数，通常直接使用 columnIndex）
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的索引，如果列不存在则返回 -1</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于根据列名查找其在结果集中的位置（索引）
        /// <br/>2. 索引从 0 开始
        /// <br/>3. 如果结果集中存在同名列，此函数的行为可能不确定，通常返回第一个匹配的索引
        /// <br/>4. 此函数对于通过列名访问数据非常有用，可以避免硬编码列索引
        /// </remarks>
        public int GetColumnIndex(long stmt, string columnName){
            var func = OLAPlugDLLHelper.GetFunction<GetColumnIndexDelegate>("GetColumnIndex");
            return func(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列索引获取列的数据类型
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>数据类型的字符串表示</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于获取结果集中指定列的数据类型
        /// <br/>2. 类型通常为 INTEGER, TEXT, REAL, BLOB 等
        /// <br/>3. 返回的字符串指针由系统管理，调用者无需手动释放内存
        /// <br/>4. 此信息可用于在获取数据前进行类型检查或转换
        /// </remarks>
        public int GetColumnType(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetColumnTypeDelegate>("GetColumnType");
            return func(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 释放结果集资源
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于关闭和释放由 ExecuteReader 生成的结果集占用的资源
        /// <br/>2. 在完成对结果集的所有操作（如 Read, GetData 等）后，必须调用此函数
        /// <br/>3. 即使在遍历结果集前发生错误，也应调用 Finalize 来清理资源
        /// <br/>4. 调用此函数后，传入的 reader 句柄将失效，不能再使用
        /// </remarks>
        public int Finalize(long stmt){
            var func = OLAPlugDLLHelper.GetFunction<FinalizeDelegate>("Finalize");
            return func(OLAObject, stmt);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 double 值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>列的 double 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 double 类型
        /// <br/>2. 如果列的数据类型不是数值类型，系统会尝试进行转换
        /// <br/>3. 如果转换失败或数据为 NULL，返回值可能是 0.0 或其他默认值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public double GetDouble(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetDoubleDelegate>("GetDouble");
            return func(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 int32 值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>列的 int32 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 32 位整数类型
        /// <br/>2. 如果列的数据类型不是整数类型，系统会尝试进行转换
        /// <br/>3. 如果转换失败、数据为 NULL 或数值超出 int32 范围，行为是未定义的
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public int GetInt32(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetInt32Delegate>("GetInt32");
            return func(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的 int64 值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>列的 int64 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的数值，并转换为 64 位整数类型
        /// <br/>2. 适用于处理可能超出 32 位范围的大整数
        /// <br/>3. 如果列的数据类型不是整数类型，系统会尝试进行转换
        /// <br/>4. 如果转换失败、数据为 NULL 或数值超出 int64 范围，行为是未定义的
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public long GetInt64(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetInt64Delegate>("GetInt64");
            return func(OLAObject, stmt, iCol);
        }

        /// <summary>
        /// 根据列索引获取当前行指定列的字符串值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="iCol">列的索引，从 0 开始</param>
        /// <returns>字符串值的指针</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从当前数据行中提取指定列的文本数据
        /// <br/>2. 返回的字符串指针指向的数据由结果集管理，其生命周期与结果集相同
        /// <br/>3. 在调用 Finalize 释放结果集后，该指针将失效
        /// <br/>4. 如果列的数据类型不是文本类型，系统会将其转换为字符串
        /// <br/>5. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public string GetString(long stmt, int iCol){
            var func = OLAPlugDLLHelper.GetFunction<GetStringDelegate>("GetString");
            return PtrToStringUTF8(func(OLAObject, stmt, iCol));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
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
            var func = OLAPlugDLLHelper.GetFunction<GetDoubleByColumnNameDelegate>("GetDoubleByColumnName");
            return func(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的 int32 值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的 int32 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetInt32 相同，但通过列名而非索引来访问数据
        /// <br/>2. 使用列名可以避免硬编码列索引，使代码更灵活
        /// <br/>3. 如果列名不存在，行为是未定义的，可能返回 0 或其他错误值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public int GetInt32ByColumnName(long stmt, string columnName){
            var func = OLAPlugDLLHelper.GetFunction<GetInt32ByColumnNameDelegate>("GetInt32ByColumnName");
            return func(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的 int64 值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
        /// <param name="columnName">列的名称</param>
        /// <returns>列的 int64 值</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 功能与 GetInt64 相同，但通过列名而非索引来访问数据
        /// <br/>2. 适用于通过列名访问大整数类型的数据
        /// <br/>3. 如果列名不存在，行为是未定义的，可能返回 0 或其他错误值
        /// <br/>4. 调用此函数前必须确保已经通过 Read 成功读取到一行有效数据
        /// </remarks>
        public long GetInt64ByColumnName(long stmt, string columnName){
            var func = OLAPlugDLLHelper.GetFunction<GetInt64ByColumnNameDelegate>("GetInt64ByColumnName");
            return func(OLAObject, stmt, columnName);
        }

        /// <summary>
        /// 根据列名获取当前行指定列的字符串值
        /// </summary>
        /// <param name="stmt">结果集句柄，由 ExecuteReader 接口生成</param>
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
            var func = OLAPlugDLLHelper.GetFunction<GetStringByColumnNameDelegate>("GetStringByColumnName");
            return PtrToStringUTF8(func(OLAObject, stmt, columnName));
        }

        /// <summary>
        /// 初始化ola相关数据库,包括olg_config,ola_image表
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于在打开的数据库上创建OLA系统所需的表和索引
        /// <br/>2. 此操作是幂等的，如果数据库已初始化，则不会重复创建表
        /// <br/>3. 必须在使用任何依赖OLA数据库结构的接口前调用此函数
        /// <br/>4. 初始化失败通常是因为数据库文件不可写或磁盘空间不足
        /// </remarks>
        public int InitOlaDatabase(long db){
            var func = OLAPlugDLLHelper.GetFunction<InitOlaDatabaseDelegate>("InitOlaDatabase");
            return func(OLAObject, db);
        }

        /// <summary>
        /// 从指定目录初始化OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">图像文件所在的目录路径</param>
        /// <param name="cover">是否覆盖已存在的数据</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于批量导入指定目录下的所有图像文件到OLA数据库
        /// <br/>2. cover 参数控制是否覆盖数据库中已存在的同名图像数据
        /// <br/>3. 支持的图像格式通常包括 BMP, PNG, JPG 等常见格式
        /// <br/>4. 此操作可能耗时较长，取决于目录中文件的数量和大小
        /// </remarks>
        public int InitOlaImageFromDir(long db, string dir, int cover){
            var func = OLAPlugDLLHelper.GetFunction<InitOlaImageFromDirDelegate>("InitOlaImageFromDir");
            return func(OLAObject, db, dir, cover);
        }

        /// <summary>
        /// 移除指定文件夹下所有图片数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">包含要移除图像的目录路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于批量删除数据库中与指定目录关联的所有OLA图像数据
        /// <br/>2. 此操作会删除所有在该目录下导入或与该目录路径匹配的图像记录
        /// <br/>3. 删除操作是永久性的，无法恢复
        /// <br/>4. 在执行此操作前，请确保不再需要这些图像数据，且没有其他功能依赖于它们
        /// </remarks>
        public int RemoveOlaImageFromDir(long db, string dir){
            var func = OLAPlugDLLHelper.GetFunction<RemoveOlaImageFromDirDelegate>("RemoveOlaImageFromDir");
            return func(OLAObject, db, dir);
        }

        /// <summary>
        /// 将OLA图像数据从数据库导出到指定目录
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">包含要移除图像的目录路径</param>
        /// <param name="exportDir">导出的目标目录路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于将数据库中存储的所有OLA图像数据导出为文件
        /// <br/>2. 导出的文件将保存在 exportDir 指定的目录中
        /// <br/>3. 确保目标目录存在且有写入权限
        /// <br/>4. 此操作可用于备份图像数据或在不同系统间迁移数据
        /// </remarks>
        public int ExportOlaImageDir(long db, string dir, string exportDir){
            var func = OLAPlugDLLHelper.GetFunction<ExportOlaImageDirDelegate>("ExportOlaImageDir");
            return func(OLAObject, db, dir, exportDir);
        }

        /// <summary>
        /// 从文件导入单个OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">图像文件所在的目录路径</param>
        /// <param name="fileName">要导入的图像文件名</param>
        /// <param name="cover">是否覆盖已存在的图像数据</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于将单个图像文件导入到OLA数据库中，并指定其在库中的名称
        /// <br/>2. imagePath 必须指向一个有效的图像文件
        /// <br/>3. name 是该图像在数据库中的唯一标识符，后续操作将使用此名称
        /// <br/>4. cover 参数决定是否替换数据库中已存在的同名图像
        /// </remarks>
        public int ImportOlaImage(long db, string dir, string fileName, int cover){
            var func = OLAPlugDLLHelper.GetFunction<ImportOlaImageDelegate>("ImportOlaImage");
            return func(OLAObject, db, dir, fileName, cover);
        }

        /// <summary>
        /// 从数据库中获取指定名称的OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">图片目录路径</param>
        /// <param name="fileName">图片文件名</param>
        /// <returns>图像数据的指针，如果未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从OLA数据库中获取指定目录和文件名的图像数据，适用于从数据库中检索图像的场景。
        /// <br/>2. 如果图像不存在或操作失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息。
        /// <br/>3. 确保目录路径和文件名正确，且图像数据存在于数据库中，否则可能导致获取失败。
        /// <br/>4. 使用完返回的图像对象指针后，应妥善处理资源，避免内存泄漏。
        /// </remarks>
        public long GetOlaImage(long db, string dir, string fileName){
            var func = OLAPlugDLLHelper.GetFunction<GetOlaImageDelegate>("GetOlaImage");
            return func(OLAObject, db, dir, fileName);
        }

        /// <summary>
        /// 从数据库中移除指定名称的OLA图像数据
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dir">图像文件在数据库中的目录路径</param>
        /// <param name="fileName">图片文件名</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从OLA数据库中移除指定目录和文件名的图像数据，适用于删除单个图像数据的场景。
        /// <br/>2. 如果移除失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息。
        /// <br/>3. 确保目录路径和文件名正确，且图像数据存在于数据库中，否则可能导致移除失败。
        /// </remarks>
        public int RemoveOlaImage(long db, string dir, string fileName){
            var func = OLAPlugDLLHelper.GetFunction<RemoveOlaImageDelegate>("RemoveOlaImage");
            return func(OLAObject, db, dir, fileName);
        }

        /// <summary>
        /// 设置数据库配置项
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="key">配置项的键名</param>
        /// <param name="value">配置项的值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于在数据库中存储键值对形式的配置信息
        /// <br/>2. 配置信息通常用于保存应用程序的设置或状态
        /// <br/>3. 如果键已存在，此操作将更新其值
        /// <br/>4. 配置项的存储是持久化的，即使关闭数据库后依然存在
        /// </remarks>
        public int SetDbConfig(long db, string key, string value){
            var func = OLAPlugDLLHelper.GetFunction<SetDbConfigDelegate>("SetDbConfig");
            return func(OLAObject, db, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<GetDbConfigDelegate>("GetDbConfig");
            return PtrToStringUTF8(func(OLAObject, db, key));
        }

        /// <summary>
        /// 从数据库中移除指定的配置项
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="key">配置项的键名</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除数据库中存储的特定配置项
        /// <br/>2. 删除后，再次调用 GetDbConfig 将无法获取该键的值
        /// <br/>3. 如果指定的键不存在，函数可能返回成功或失败，具体取决于实现
        /// <br/>4. 此操作不会影响其他配置项
        /// </remarks>
        public int RemoveDbConfig(long db, string key){
            var func = OLAPlugDLLHelper.GetFunction<RemoveDbConfigDelegate>("RemoveDbConfig");
            return func(OLAObject, db, key);
        }

        /// <summary>
        /// 设置带作用域的数据库配置项
        /// </summary>
        /// <param name="key">配置项的键名</param>
        /// <param name="value">配置项的值</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 SetDbConfig 类似，但增加了作用域（scope）参数
        /// <br/>2. 作用域可用于对配置项进行分类或隔离，例如按模块、用户或环境划分
        /// <br/>3. 相同的键名在不同作用域下可以存储不同的值
        /// <br/>4. 此函数提供了更灵活的配置管理能力
        /// </remarks>
        public int SetDbConfigEx(string key, string value){
            var func = OLAPlugDLLHelper.GetFunction<SetDbConfigExDelegate>("SetDbConfigEx");
            return func(OLAObject, key, value);
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
            var func = OLAPlugDLLHelper.GetFunction<GetDbConfigExDelegate>("GetDbConfigEx");
            return PtrToStringUTF8(func(OLAObject, key));
        }

        /// <summary>
        /// 从数据库中移除带作用域的配置项
        /// </summary>
        /// <param name="key">配置项的键名</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除在特定作用域下存储的配置项
        /// <br/>2. 必须同时指定正确的作用域和键名才能成功删除
        /// <br/>3. 此操作只影响指定作用域下的特定键，不会影响其他作用域或无作用域的同名键
        /// <br/>4. 删除后，该作用域下的该键将不再存在
        /// </remarks>
        public int RemoveDbConfigEx(string key){
            var func = OLAPlugDLLHelper.GetFunction<RemoveDbConfigExDelegate>("RemoveDbConfigEx");
            return func(OLAObject, key);
        }

        /// <summary>
        /// 从指定目录中加载字库文件，并将其初始化到OLA数据库中
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="dict_path">字库图片文件夹路径</param>
        /// <param name="cover">是否覆盖已存在的图像数据</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从指定目录中加载字库图片文件，并将其初始化到OLA数据库中。适用于批量导入字库的场景
        /// <br/>2. cover 参数用于控制是否覆盖已存在的图像数据。设置为 1 时，会覆盖现有数据；设置为 0时，会跳过已存在的图像
        /// <br/>3. 如果初始化失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>4. 确保目录路径正确，且图像文件格式受支持，否则可能导致初始化失败
        /// </remarks>
        public int InitDictFromDir(long db, string dict_name, string dict_path, int cover){
            var func = OLAPlugDLLHelper.GetFunction<InitDictFromDirDelegate>("InitDictFromDir");
            return func(OLAObject, db, dict_name, dict_path, cover);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="dict_path">文本字库路径,如C:\\dicts\\mydict.txt</param>
        /// <param name="cover"></param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从txt字库文件中加载字库信息，并将其初始化到OLA数据库中。适用于批量导入字库的场景
        /// <br/>2. cover 参数用于控制是否覆盖已存在的图像数据。设置为 1 时，会覆盖现有数据；设置为 0时，会跳过已存在的图像。
        /// <br/>3. 如果初始化失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>4. 确保文本路径正确，且文本文件格式受支持，否则可能导致初始化失败
        /// </remarks>
        public int InitDictFromTxt(long db, string dict_name, string dict_path, int cover){
            var func = OLAPlugDLLHelper.GetFunction<InitDictFromTxtDelegate>("InitDictFromTxt");
            return func(OLAObject, db, dict_name, dict_path, cover);
        }

        /// <summary>
        /// 向指定字库中导入单个文字的图像
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="pic_file_name">要导入的图像文件名</param>
        /// <param name="cover">是否覆盖已存在的图像数据
        ///<br/> 0: 不覆盖
        ///<br/> 1: 覆盖
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于将指定目录中的字库图像文件导入到OLA数据库中，适用于单个字库图像文件的导入场景。
        /// <br/>2. cover 参数用于控制是否覆盖已存在的图像数据。设置为 1 时，会覆盖现有数据；设置为 0时，会跳过已存在的图像。
        /// <br/>3. 如果导入失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息。
        /// <br/>4. 确保目录路径和文件名正确，且图像文件格式受支持，否则可能导致导入失败。
        /// </remarks>
        public int ImportDictWord(long db, string dict_name, string pic_file_name, int cover){
            var func = OLAPlugDLLHelper.GetFunction<ImportDictWordDelegate>("ImportDictWord");
            return func(OLAObject, db, dict_name, pic_file_name, cover);
        }

        /// <summary>
        /// 将OLA数据库中的图像数据导出到指定目录
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="export_dir">导出路径</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于将OLA数据库中的图像数据导出到指定目录，适用于批量导出字库图像数据的场景
        /// <br/>2. 如果导出失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>3. 确保目录路径正确，且图像数据存在于数据库中，否则可能导致导出失败
        /// <br/>4. 导出的图像文件将保存在 exportDir 指定的目录中，确保目标目录有足够的存储空间
        /// </remarks>
        public int ExportDict(long db, string dict_name, string export_dir){
            var func = OLAPlugDLLHelper.GetFunction<ExportDictDelegate>("ExportDict");
            return func(OLAObject, db, dict_name, export_dir);
        }

        /// <summary>
        /// 从数据库中移除整个字库
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">要移除的字库名称</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于删除数据库中存储的整个字库及其所有图像数据
        /// <br/>2. 此操作是永久性的，会删除该字库下的所有字符图像
        /// <br/>3. 删除后，任何使用该字库的识别操作都将失败
        /// <br/>4. 在执行此操作前应确保没有其他进程或功能依赖于该字库
        /// </remarks>
        public int RemoveDict(long db, string dict_name){
            var func = OLAPlugDLLHelper.GetFunction<RemoveDictDelegate>("RemoveDict");
            return func(OLAObject, db, dict_name);
        }

        /// <summary>
        /// 移除词典词条
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="word">要移除的文字</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 用于从字库中删除特定字符的图像数据
        /// <br/>2. 此操作只影响指定字库中的指定字符，不会影响字库中的其他字符
        /// <br/>3. 删除后，OCR识别将无法再识别该字符
        /// <br/>4. 此接口适用于维护和更新字库，移除不再需要或错误的字符
        /// </remarks>
        public int RemoveDictWord(long db, string dict_name, string word){
            var func = OLAPlugDLLHelper.GetFunction<RemoveDictWordDelegate>("RemoveDictWord");
            return func(OLAObject, db, dict_name, word);
        }

        /// <summary>
        /// 读取字库图片
        /// </summary>
        /// <param name="db">数据库连接句柄，由 OpenDatabase 接口生成</param>
        /// <param name="dict_name">字库名称</param>
        /// <param name="word">要读取的文字</param>
        /// <param name="gap">文字间隔，单位为像素</param>
        /// <param name="dir">拼接方向
        ///<br/> 0: 水平拼接
        ///<br/> 1: 垂直拼接
        /// </param>
        /// <returns>图像对象的指针。如果操作失败，返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于从OLA数据库中获取指定字典名称和文字的图像数据，适用于从数据库中查找指定文字的场景
        /// <br/>2. 如果图像不存在或操作失败，函数将返回 0。可以通过 GetDatabaseError 函数获取详细的错误信息
        /// <br/>3. 确保字典名称和文字正确，且图像数据存在于数据库中，否则可能导致获取失败
        /// <br/>4. 使用完返回的图像对象指针后，应妥善处理资源，避免内存泄漏
        /// </remarks>
        public long GetDictImage(long db, string dict_name, string word, int gap, int dir){
            var func = OLAPlugDLLHelper.GetFunction<GetDictImageDelegate>("GetDictImage");
            return func(OLAObject, db, dict_name, word, gap, dir);
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
            var func = OLAPlugDLLHelper.GetFunction<OpenVideoDelegate>("OpenVideo");
            return func(OLAObject, videoPath);
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
            var func = OLAPlugDLLHelper.GetFunction<OpenCameraDelegate>("OpenCamera");
            return func(OLAObject, deviceIndex);
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
            var func = OLAPlugDLLHelper.GetFunction<CloseVideoDelegate>("CloseVideo");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<IsVideoOpenedDelegate>("IsVideoOpened");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<GetVideoInfoDelegate>("GetVideoInfo");
            return PtrToStringUTF8(func(OLAObject, videoHandle));
        }

        /// <summary>
        /// 获取视频宽度
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频宽度（像素），失败返回0</returns>
        public int GetVideoWidth(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoWidthDelegate>("GetVideoWidth");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频高度
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频高度（像素），失败返回0</returns>
        public int GetVideoHeight(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoHeightDelegate>("GetVideoHeight");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频帧率
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频帧率（FPS），失败返回0.0</returns>
        public double GetVideoFPS(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoFPSDelegate>("GetVideoFPS");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频总帧数
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频总帧数，失败返回0</returns>
        public int GetVideoTotalFrames(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoTotalFramesDelegate>("GetVideoTotalFrames");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取视频时长
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>视频时长（秒），失败返回0.0</returns>
        public double GetVideoDuration(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoDurationDelegate>("GetVideoDuration");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取当前帧位置
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>当前帧索引，失败返回-1</returns>
        public int GetCurrentFrameIndex(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetCurrentFrameIndexDelegate>("GetCurrentFrameIndex");
            return func(OLAObject, videoHandle);
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <returns>当前时间戳（秒），失败返回0.0</returns>
        public double GetCurrentTimestamp(long videoHandle){
            var func = OLAPlugDLLHelper.GetFunction<GetCurrentTimestampDelegate>("GetCurrentTimestamp");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadNextFrameDelegate>("ReadNextFrame");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadFrameAtIndexDelegate>("ReadFrameAtIndex");
            return func(OLAObject, videoHandle, frameIndex);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadFrameAtTimeDelegate>("ReadFrameAtTime");
            return func(OLAObject, videoHandle, timestamp);
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
            var func = OLAPlugDLLHelper.GetFunction<ReadCurrentFrameDelegate>("ReadCurrentFrame");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<SeekToFrameDelegate>("SeekToFrame");
            return func(OLAObject, videoHandle, frameIndex);
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
            var func = OLAPlugDLLHelper.GetFunction<SeekToTimeDelegate>("SeekToTime");
            return func(OLAObject, videoHandle, timestamp);
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
            var func = OLAPlugDLLHelper.GetFunction<SeekToBeginningDelegate>("SeekToBeginning");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<SeekToEndDelegate>("SeekToEnd");
            return func(OLAObject, videoHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<ExtractFramesToFilesDelegate>("ExtractFramesToFiles");
            return func(OLAObject, videoHandle, startFrame, endFrame, step, outputDir, imageFormat, jpegQuality);
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
            var func = OLAPlugDLLHelper.GetFunction<ExtractFramesByIntervalDelegate>("ExtractFramesByInterval");
            return func(OLAObject, videoHandle, intervalSeconds, outputDir, imageFormat);
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
            var func = OLAPlugDLLHelper.GetFunction<ExtractKeyFramesDelegate>("ExtractKeyFrames");
            return func(OLAObject, videoHandle, threshold, maxFrames, outputDir, imageFormat);
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
            var func = OLAPlugDLLHelper.GetFunction<SaveCurrentFrameDelegate>("SaveCurrentFrame");
            return func(OLAObject, videoHandle, outputPath, quality);
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
            var func = OLAPlugDLLHelper.GetFunction<SaveFrameAtIndexDelegate>("SaveFrameAtIndex");
            return func(OLAObject, videoHandle, frameIndex, outputPath, quality);
        }

        /// <summary>
        /// 将当前帧转换为Base64字符串
        /// </summary>
        /// <param name="videoHandle">视频句柄</param>
        /// <param name="format">图片格式（"png"、"jpg"等）</param>
        /// <returns>返回Base64编码的图片数据字符串指针，需调用FreeStringPtr释放；失败返回0</returns>
        public string FrameToBase64(long videoHandle, string format){
            var func = OLAPlugDLLHelper.GetFunction<FrameToBase64Delegate>("FrameToBase64");
            return PtrToStringUTF8(func(OLAObject, videoHandle, format));
        }

        /// <summary>
        /// 计算两帧之间的相似度
        /// </summary>
        /// <param name="frame1">第一帧图像句柄</param>
        /// <param name="frame2">第二帧图像句柄</param>
        /// <returns>相似度（0-1，1表示完全相同）</returns>
        public double CalculateFrameSimilarity(long frame1, long frame2){
            var func = OLAPlugDLLHelper.GetFunction<CalculateFrameSimilarityDelegate>("CalculateFrameSimilarity");
            return func(OLAObject, frame1, frame2);
        }

        /// <summary>
        /// 快速获取视频文件信息（无需打开整个视频）
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>返回包含视频信息的JSON字符串指针，需调用FreeStringPtr释放；失败返回0</returns>
        public string GetVideoInfoFromPath(string videoPath){
            var func = OLAPlugDLLHelper.GetFunction<GetVideoInfoFromPathDelegate>("GetVideoInfoFromPath");
            return PtrToStringUTF8(func(OLAObject, videoPath));
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
            var func = OLAPlugDLLHelper.GetFunction<IsValidVideoFileDelegate>("IsValidVideoFile");
            return func(OLAObject, videoPath);
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
            var func = OLAPlugDLLHelper.GetFunction<ExtractSingleFrameDelegate>("ExtractSingleFrame");
            return func(OLAObject, videoPath, frameIndex);
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
            var func = OLAPlugDLLHelper.GetFunction<ExtractThumbnailDelegate>("ExtractThumbnail");
            return func(OLAObject, videoPath);
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
            var func = OLAPlugDLLHelper.GetFunction<ConvertVideoDelegate>("ConvertVideo");
            return func(OLAObject, inputPath, outputPath, codec, fps);
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
            var func = OLAPlugDLLHelper.GetFunction<ResizeVideoDelegate>("ResizeVideo");
            return func(OLAObject, inputPath, outputPath, width, height);
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
            var func = OLAPlugDLLHelper.GetFunction<TrimVideoDelegate>("TrimVideo");
            return func(OLAObject, inputPath, outputPath, startTime, endTime);
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
            var func = OLAPlugDLLHelper.GetFunction<CreateVideoFromImagesDelegate>("CreateVideoFromImages");
            return func(OLAObject, imageDir, outputPath, fps, codec);
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
            var func = OLAPlugDLLHelper.GetFunction<DetectSceneChangesDelegate>("DetectSceneChanges");
            return PtrToStringUTF8(func(OLAObject, videoPath, threshold));
        }

        /// <summary>
        /// 计算视频平均亮度
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>平均亮度（0-255），失败返回-1</returns>
        public double CalculateAverageBrightness(string videoPath){
            var func = OLAPlugDLLHelper.GetFunction<CalculateAverageBrightnessDelegate>("CalculateAverageBrightness");
            return func(OLAObject, videoPath);
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
            var func = OLAPlugDLLHelper.GetFunction<DetectMotionDelegate>("DetectMotion");
            return PtrToStringUTF8(func(OLAObject, videoPath, threshold));
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
        /// <returns>操作结果
        ///<br/>0: 设置失败（可能原因：无效的窗口句柄、无效的状态标志、窗口已被销毁等）
        ///<br/>1: 设置成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在使用强制结束进程（flag=13）时要特别谨慎，确保已保存相关数据
        /// <br/>2. 某些状态组合可能会相互影响，建议按照逻辑顺序设置
        /// <br/>3. 窗口状态的改变可能会触发窗口的相关事件和回调
        /// <br/>4. 部分状态设置可能会受到系统或应用程序的安全策略限制
        /// </remarks>
        public int SetWindowState(long hwnd, int state){
            var func = OLAPlugDLLHelper.GetFunction<SetWindowStateDelegate>("SetWindowState");
            return func(OLAObject, hwnd, state);
        }

        /// <summary>
        /// 根据窗口标题或类名查找窗口
        /// </summary>
        /// <param name="class_name">窗口类名，支持模糊匹配。如果为空字符串，则匹配所有类名。</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串，则匹配所有标题。</param>
        /// <returns>返回找到的窗口句柄，如果未找到匹配的窗口，返回0</returns>
        public long FindWindow(string class_name, string title){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowDelegate>("FindWindow");
            return func(OLAObject, class_name, title);
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
            var func = OLAPlugDLLHelper.GetFunction<GetClipboardDelegate>("GetClipboard");
            return func(OLAObject);
        }

        /// <summary>
        /// 设置系统剪贴板的文本内容
        /// </summary>
        /// <param name="text">要设置的文本内容</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数将指定的文本字符串放入系统剪贴板
        /// <br/>2. 执行后，文本内容可被其他应用程序粘贴使用
        /// <br/>3. 函数会自动打开和关闭剪贴板
        /// <br/>4. 如果剪贴板被其他程序长时间占用，设置可能会失败
        /// </remarks>
        public int SetClipboard(string text){
            var func = OLAPlugDLLHelper.GetFunction<SetClipboardDelegate>("SetClipboard");
            return func(OLAObject, text);
        }

        /// <summary>
        /// 向指定窗口发送粘贴命令（模拟 Ctrl+V）
        /// </summary>
        /// <param name="hwnd">目标窗口的句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数向指定窗口发送 WM_PASTE 消息，触发其粘贴操作
        /// <br/>2. 目标窗口必须是可接收文本输入的控件（如编辑框）
        /// <br/>3. 执行前通常需要先调用 SetClipboard 将文本放入剪贴板
        /// <br/>4. 此操作是发送消息，不保证目标窗口一定会执行粘贴
        /// </remarks>
        public int SendPaste(long hwnd){
            var func = OLAPlugDLLHelper.GetFunction<SendPasteDelegate>("SendPaste");
            return func(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取给定窗口相关的窗口句柄，如父窗口、子窗口、相邻窗口等
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="flag">指定要获取的窗口类型
        ///<br/> 0: 获取父窗口
        ///<br/> 1: 获取第一个子窗口
        ///<br/> 2: 获取First窗口
        ///<br/> 3: 获取Last窗口
        ///<br/> 4: 获取下一个窗口
        ///<br/> 5: 获取上一个窗口
        ///<br/> 6: 获取拥有者窗口
        ///<br/> 7: 获取顶层窗口
        /// </param>
        /// <returns>返回指定类型的窗口句柄</returns>
        public long GetWindow(long hwnd, int flag){
            var func = OLAPlugDLLHelper.GetFunction<GetWindowDelegate>("GetWindow");
            return func(OLAObject, hwnd, flag);
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowTitleDelegate>("GetWindowTitle");
            return PtrToStringUTF8(func(OLAObject, hwnd));
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowClassDelegate>("GetWindowClass");
            return PtrToStringUTF8(func(OLAObject, hwnd));
        }

        /// <summary>
        /// 获取指定窗口的矩形区域（相对于屏幕）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x1">返回窗口左上角的X坐标</param>
        /// <param name="y1">返回窗口左上角的Y坐标</param>
        /// <param name="x2">返回窗口右下角的X坐标</param>
        /// <param name="y2">返回窗口右下角的Y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 窗口必须处于可见状态，否则获取可能失败
        /// <br/>2. 返回的坐标是相对于屏幕左上角的绝对坐标
        /// <br/>3. 返回的区域包括窗口的非客户区（标题栏、边框等）
        /// <br/>4. 如果只需要获取客户区域，请使用 GetClientRect 函数
        /// <br/>5. 对于多显示器系统，坐标值可能为负数，这表示窗口位于主显示器左侧或上方的显示器上
        /// </remarks>
        public int GetWindowRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            var func = OLAPlugDLLHelper.GetFunction<GetWindowRectDelegate>("GetWindowRect");
            return func(OLAObject, hwnd, out x1, out y1, out x2, out y2);
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowProcessPathDelegate>("GetWindowProcessPath");
            return PtrToStringUTF8(func(OLAObject, hwnd));
        }

        /// <summary>
        /// 获取指定窗口的当前状态
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="flag">要检查的窗口状态
        ///<br/> 0: 判断窗口是否存在（检查句柄的有效性）
        ///<br/> 1: 判断窗口是否处于激活状态（是否为前台窗口）
        ///<br/> 2: 判断窗口是否可见（是否显示在屏幕上）
        ///<br/> 3: 判断窗口是否最小化（是否处于最小化状态）
        ///<br/> 4: 判断窗口是否最大化（是否处于最大化状态）
        ///<br/> 5: 判断窗口是否置顶（是否总在最前）
        ///<br/> 6: 判断窗口是否无响应（是否处于"未响应"状态）
        ///<br/> 7: 判断窗口是否可用（是否能接收用户输入）
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 指定的状态条件不满足（或窗口句柄无效）
        ///<br/>1: 指定的状态条件满足
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 在检查窗口状态前，建议先使用flag=0确认窗口是否存在
        /// <br/>2. 某些状态可能会同时存在（如窗口可以同时是可见的和置顶的）
        /// <br/>3. 窗口的"无响应"状态检查可能需要一定时间
        /// <br/>4. 对于系统窗口或特权窗口，某些状态可能无法正确获取
        /// </remarks>
        public int GetWindowState(long hwnd, int flag){
            var func = OLAPlugDLLHelper.GetFunction<GetWindowStateDelegate>("GetWindowState");
            return func(OLAObject, hwnd, flag);
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
            var func = OLAPlugDLLHelper.GetFunction<GetForegroundWindowDelegate>("GetForegroundWindow");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowProcessIdDelegate>("GetWindowProcessId");
            return func(OLAObject, hwnd);
        }

        /// <summary>
        /// 获取指定窗口客户区的大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width">指向接收客户区宽度的变量</param>
        /// <param name="height">指向接收客户区高度的变量</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 客户区是窗口中用于显示内容的区域，不包括标题栏、边框和滚动条
        /// <br/>2. 获取的尺寸常用于绘制操作或调整内部控件布局
        /// <br/>3. 坐标相对于窗口客户区的左上角(0,0)
        /// <br/>4. 此函数对于UI自动化和截图定位至关重要
        /// </remarks>
        public int GetClientSize(long hwnd, out int width, out int height){
            var func = OLAPlugDLLHelper.GetFunction<GetClientSizeDelegate>("GetClientSize");
            return func(OLAObject, hwnd, out width, out height);
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
            var func = OLAPlugDLLHelper.GetFunction<GetMousePointWindowDelegate>("GetMousePointWindow");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<GetSpecialWindowDelegate>("GetSpecialWindow");
            return func(OLAObject, flag);
        }

        /// <summary>
        /// 获取指定窗口客户区的矩形区域
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x1">返回客户区左上角的X坐标，总是0</param>
        /// <param name="y1">返回客户区左上角的Y坐标，总是0</param>
        /// <param name="x2">返回客户区右下角的X坐标，即客户区宽度</param>
        /// <param name="y2">返回客户区右下角的Y坐标，即客户区高度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 窗口必须处于可见状态，否则获取可能失败
        /// <br/>2. 返回的坐标是相对于客户区左上角的相对坐标，(x1,y1)总是(0,0)
        /// <br/>3. (x2,y2)表示客户区的宽度和高度，而不是屏幕坐标
        /// <br/>4. 如果需要获取包含非客户区的窗口区域，请使用 GetWindowRect 函数
        /// <br/>5. 如果需要将客户区坐标转换为屏幕坐标，请使用 ClientToScreen 函数与 GetWindowRect
        /// </remarks>
        public int GetClientRect(long hwnd, out int x1, out int y1, out int x2, out int y2){
            var func = OLAPlugDLLHelper.GetFunction<GetClientRectDelegate>("GetClientRect");
            return func(OLAObject, hwnd, out x1, out y1, out x2, out y2);
        }

        /// <summary>
        /// 设置指定窗口的标题文本
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="title">要设置的新标题</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数会改变窗口标题栏上显示的文本
        /// <br/>2. 新标题会立即反映在UI上
        /// <br/>3. 并非所有窗口都允许修改标题，某些系统窗口或受保护的应用可能忽略此操作
        /// <br/>4. 修改标题可能会影响基于标题的窗口查找逻辑
        /// </remarks>
        public int SetWindowText(long hwnd, string title){
            var func = OLAPlugDLLHelper.GetFunction<SetWindowTextDelegate>("SetWindowText");
            return func(OLAObject, hwnd, title);
        }

        /// <summary>
        /// 设置指定窗口的大小和位置
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width">窗口的目标宽度（像素），包括边框，必须大于0</param>
        /// <param name="height">窗口的目标高度（像素），包括标题栏和边框，必须大于0</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数可以同时改变窗口的位置和大小
        /// <br/>2. 坐标是相对于屏幕的绝对坐标
        /// <br/>3. flags 参数可控制是否重绘窗口、是否发送消息等
        /// <br/>4. 此操作相当于直接调用 Windows API 的 MoveWindow
        /// </remarks>
        public int SetWindowSize(long hwnd, int width, int height){
            var func = OLAPlugDLLHelper.GetFunction<SetWindowSizeDelegate>("SetWindowSize");
            return func(OLAObject, hwnd, width, height);
        }

        /// <summary>
        /// 设置指定窗口客户区的大小
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="width">客户区的新宽度</param>
        /// <param name="height">客户区的新高度</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 SetWindowSize 不同，此函数设置的是客户区尺寸
        /// <br/>2. 系统会根据客户区大小自动调整窗口的整体大小以包含边框和标题栏
        /// <br/>3. 常用于确保窗口内容区域达到指定尺寸
        /// <br/>4. 设置后，窗口的总体尺寸会大于或等于指定的客户区尺寸
        /// </remarks>
        public int SetClientSize(long hwnd, int width, int height){
            var func = OLAPlugDLLHelper.GetFunction<SetClientSizeDelegate>("SetClientSize");
            return func(OLAObject, hwnd, width, height);
        }

        /// <summary>
        /// 设置窗口的透明度
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="alpha">透明度值，范围 0-255，0为完全透明，255为完全不透明</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数通过设置窗口的分层属性来实现透明效果
        /// <br/>2. 窗口必须支持分层属性（WS_EX_LAYERED）才能设置透明度
        /// <br/>3. 透明度影响整个窗口，包括标题栏和边框
        /// <br/>4. 此功能常用于制作半透明界面或浮动工具窗口
        /// </remarks>
        public int SetWindowTransparent(long hwnd, int alpha){
            var func = OLAPlugDLLHelper.GetFunction<SetWindowTransparentDelegate>("SetWindowTransparent");
            return func(OLAObject, hwnd, alpha);
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
            var func = OLAPlugDLLHelper.GetFunction<FindWindowExDelegate>("FindWindowEx");
            return func(OLAObject, parent, class_name, title);
        }

        /// <summary>
        /// 根据进程名称、窗口类名和标题查找可见窗口。此函数提供了一种灵活的方式来定位特定进程的窗口
        /// </summary>
        /// <param name="process_name">进程名称（如"notepad.exe"），精确匹配但不区分大小写</param>
        /// <param name="class_name">窗口类名，支持模糊匹配。如果为空字符串("")，则匹配所有类名</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串("")，则匹配所有标题</param>
        /// <returns>返回找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 进程名称必须包含扩展名（如".exe"），且不区分大小写
        /// <br/>2. 类名和标题支持模糊匹配，可以只包含部分文本
        /// <br/>3. 空字符串参数会匹配任意值，可用于通配搜索
        /// <br/>4. 如果有多个匹配的窗口，函数返回第一个找到的窗口
        /// <br/>5. 建议使用更具体的搜索条件以提高查找准确性
        /// <br/>6. 某些系统进程的窗口可能无法被找到
        /// <br/>7. 进程必须具有可见的主窗口才能被找到
        /// <br/>8. 可以结合 GetWindowState 验证找到的窗口
        /// </remarks>
        public long FindWindowByProcess(string process_name, string class_name, string title){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowByProcessDelegate>("FindWindowByProcess");
            return func(OLAObject, process_name, class_name, title);
        }

        /// <summary>
        /// 移动指定窗口到新的位置
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">窗口左上角的新x坐标</param>
        /// <param name="y">窗口左上角的新y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数只改变窗口的位置，不改变其大小
        /// <br/>2. 坐标是相对于屏幕的绝对坐标
        /// <br/>3. 移动操作会触发窗口的 WM_WINDOWPOSCHANGING/CHANGED 消息
        /// <br/>4. 此函数是 SetWindowSize 的一个特例（只改变位置）
        /// </remarks>
        public int MoveWindow(long hwnd, int x, int y){
            var func = OLAPlugDLLHelper.GetFunction<MoveWindowDelegate>("MoveWindow");
            return func(OLAObject, hwnd, x, y);
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
            var func = OLAPlugDLLHelper.GetFunction<GetScaleFromWindowsDelegate>("GetScaleFromWindows");
            return func(OLAObject, hwnd);
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowDpiAwarenessScaleDelegate>("GetWindowDpiAwarenessScale");
            return func(OLAObject, hwnd);
        }

        /// <summary>
        /// 枚举系统中所有正在运行的进程
        /// </summary>
        /// <param name="name">进程名</param>
        /// <returns></returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// <br/>2. 进程ID列表中的进程按启动时间排序，越早启动的进程排在越前面
        /// <br/>3. 某些系统进程可能无法被枚举，这取决于当前用户的权限
        /// <br/>4. 建议在使用此函数前，先使用 GetProcessInfo 函数获取进程的详细信息
        /// <br/>5. 如果需要查找特定窗口的进程，可以使用 GetWindowProcessId 函数
        /// </remarks>
        public string EnumProcess(string name){
            var func = OLAPlugDLLHelper.GetFunction<EnumProcessDelegate>("EnumProcess");
            return PtrToStringUTF8(func(OLAObject, name));
        }

        /// <summary>
        /// 枚举指定父窗口下的所有子窗口
        /// </summary>
        /// <param name="parent">父窗口句柄，获取的窗口必须是该窗口的子窗口。当为0时获取桌面的子窗口</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串，则不匹配标题</param>
        /// <param name="className">窗口类名，支持模糊匹配。如果为空字符串，则不匹配类名</param>
        /// <param name="filter">过滤条件，可以组合使用（值相加）
        ///<br/> 1: 匹配窗口标题（参数title有效）
        ///<br/> 2: 匹配窗口类名（参数class_name有效）
        ///<br/> 4: 只匹配第一个进程的窗口
        ///<br/> 8: 匹配顶级窗口（所有者窗口为0）
        ///<br/> 16: 匹配可见窗口
        /// </param>
        /// <returns>所有匹配的窗口句柄字符串，格式为"hwnd1,hwnd2,hwnd3"，如果没有找到匹配的窗口，返回空字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// <br/>2. 过滤条件可以组合使用，例如：1+8+16 表示匹配标题、顶级窗口和可见窗口
        /// <br/>3. 某些窗口可能无法被枚举，这取决于当前用户的权限和窗口的状态
        /// <br/>4. 建议在使用此函数前，先使用 GetWindowTitle 和 GetWindowClass 函数获取窗口信息
        /// <br/>5. 如果需要查找特定进程的窗口，可以使用 EnumWindowByProcess 函数
        /// </remarks>
        public string EnumWindow(long parent, string title, string className, int filter){
            var func = OLAPlugDLLHelper.GetFunction<EnumWindowDelegate>("EnumWindow");
            return PtrToStringUTF8(func(OLAObject, parent, title, className, filter));
        }

        /// <summary>
        /// 根据进程名称枚举其创建的所有窗口
        /// </summary>
        /// <param name="process_name">进程映像名，如"svchost.exe"。此参数精确匹配但不区分大小写</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串，则不匹配标题</param>
        /// <param name="class_name"></param>
        /// <param name="filter">过滤条件，可以组合使用（值相加）
        ///<br/> 1: 匹配窗口标题（参数title有效）
        ///<br/> 2: 匹配窗口类名（参数class_name有效）
        ///<br/> 4: 只匹配第一个进程的窗口
        ///<br/> 8: 匹配顶级窗口（所有者窗口为0）
        ///<br/> 16: 匹配可见窗口
        /// </param>
        /// <returns>返回所有匹配的窗口句柄字符串，格式为"hwnd1,hwnd2,hwnd3"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public string EnumWindowByProcess(string process_name, string title, string class_name, int filter){
            var func = OLAPlugDLLHelper.GetFunction<EnumWindowByProcessDelegate>("EnumWindowByProcess");
            return PtrToStringUTF8(func(OLAObject, process_name, title, class_name, filter));
        }

        /// <summary>
        /// 根据进程ID枚举其创建的所有窗口
        /// </summary>
        /// <param name="pid">进程ID。可以通过 GetWindowProcessId 函数获取</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串，则不匹配标题</param>
        /// <param name="class_name"></param>
        /// <param name="filter">过滤条件，可以组合使用（值相加）
        ///<br/> 1: 匹配窗口标题（参数title有效）
        ///<br/> 2: 匹配窗口类名（参数class_name有效）
        ///<br/> 4: 只匹配第一个进程的窗口
        ///<br/> 8: 匹配顶级窗口（所有者窗口为0）
        ///<br/> 16: 匹配可见窗口
        /// </param>
        /// <returns>返回所有匹配的窗口句柄字符串，格式为"hwnd1,hwnd2,hwnd3"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// <br/>2. 过滤条件可以组合使用，例如：1+8+16 表示匹配标题、顶级窗口和可见窗口
        /// <br/>3. 如果指定了进程ID为0，将枚举所有进程的窗口
        /// <br/>4. 建议在使用此函数前，先使用 GetWindowProcessId 函数获取正确的进程ID
        /// <br/>5. 如果需要查找特定进程的所有窗口，可以使用 EnumWindowByProcess 函数
        /// </remarks>
        public string EnumWindowByProcessId(long pid, string title, string class_name, int filter){
            var func = OLAPlugDLLHelper.GetFunction<EnumWindowByProcessIdDelegate>("EnumWindowByProcessId");
            return PtrToStringUTF8(func(OLAObject, pid, title, class_name, filter));
        }

        /// <summary>
        /// 高级窗口查找，支持多种条件和模糊匹配
        /// </summary>
        /// <param name="spec1">查找串1，内容取决于flag1的值</param>
        /// <param name="flag1">查找串1的类型，可选值
        ///<br/> 0: 标题
        ///<br/> 1: 程序名字（如notepad）
        ///<br/> 2: 类名
        ///<br/> 3: 程序路径（不含盘符，如\windows\system32）
        ///<br/> 4: 父句柄（十进制字符串）
        ///<br/> 5: 父窗口标题
        ///<br/> 6: 父窗口类名
        ///<br/> 7: 顶级窗口句柄（十进制字符串）
        ///<br/> 8: 顶级窗口标题
        ///<br/> 9: 顶级窗口类名
        /// </param>
        /// <param name="type1">查找串1的匹配方式
        ///<br/> 0: 精确匹配
        ///<br/> 1: 模糊匹配
        /// </param>
        /// <param name="spec2">查找串2，内容取决于flag2的值</param>
        /// <param name="flag2">查找串2的类型，可选值
        ///<br/> 0: 标题
        ///<br/> 1: 程序名字（如notepad）
        ///<br/> 2: 类名
        ///<br/> 3: 程序路径（不含盘符，如\windows\system32）
        ///<br/> 4: 父句柄（十进制字符串）
        ///<br/> 5: 父窗口标题
        ///<br/> 6: 父窗口类名
        ///<br/> 7: 顶级窗口句柄（十进制字符串）
        ///<br/> 8: 顶级窗口标题
        ///<br/> 9: 顶级窗口类名
        /// </param>
        /// <param name="type2">查找串2的匹配方式
        ///<br/> 0: 精确匹配
        ///<br/> 1: 模糊匹配
        /// </param>
        /// <param name="sort">排序方式
        ///<br/> 0: 不排序
        ///<br/> 1: 按窗口打开顺序排序
        /// </param>
        /// <returns>返回所有匹配的窗口句柄字符串,格式"hwnd1,hwnd2,hwnd3"</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string EnumWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2, int sort){
            var func = OLAPlugDLLHelper.GetFunction<EnumWindowSuperDelegate>("EnumWindowSuper");
            return PtrToStringUTF8(func(OLAObject, spec1, flag1, type1, spec2, flag2, type2, sort));
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
            var func = OLAPlugDLLHelper.GetFunction<GetPointWindowDelegate>("GetPointWindow");
            return func(OLAObject, x, y);
        }

        /// <summary>
        /// 获取指定进程的详细信息
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>返回格式为"进程名|进程路径|CPU占用率|内存占用量"，CPU占用率以百分比表示，内存占用量以字节为单位</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址，需要调用 FreeStringPtr 接口释放内存
        /// </remarks>
        public string GetProcessInfo(long pid){
            var func = OLAPlugDLLHelper.GetFunction<GetProcessInfoDelegate>("GetProcessInfo");
            return PtrToStringUTF8(func(OLAObject, pid));
        }

        /// <summary>
        /// 显示或隐藏系统任务栏上的程序图标
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="show">是否显示任务栏图标
        ///<br/> 0: 隐藏图标
        ///<br/> 1: 显示图标
        /// </param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int ShowTaskBarIcon(long hwnd, int show){
            var func = OLAPlugDLLHelper.GetFunction<ShowTaskBarIconDelegate>("ShowTaskBarIcon");
            return func(OLAObject, hwnd, show);
        }

        /// <summary>
        /// 根据进程ID查找其创建的窗口
        /// </summary>
        /// <param name="process_id">进程ID</param>
        /// <param name="className">窗口类名，支持模糊匹配。如果为空字符串("")，则匹配所有类名</param>
        /// <param name="title">窗口标题，支持模糊匹配。如果为空字符串("")，则匹配所有标题</param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 进程ID必须是当前运行的有效进程ID
        /// <br/>2. 类名和标题支持模糊匹配，可以只包含部分文本
        /// <br/>3. 空字符串参数会匹配任意值，可用于通配搜索
        /// <br/>4. 如果有多个匹配的窗口，函数返回第一个找到的窗口
        /// <br/>5. 建议先验证进程ID是否有效再进行查找
        /// <br/>6. 某些系统进程的窗口可能因权限问题无法被找到
        /// <br/>7. 进程必须具有可见的窗口才能被找到
        /// <br/>8. 可以结合 GetWindowState 和 SetWindowState 进行窗口操作
        /// </remarks>
        public long FindWindowByProcessId(long process_id, string className, string title){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowByProcessIdDelegate>("FindWindowByProcessId");
            return func(OLAObject, process_id, className, title);
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
            var func = OLAPlugDLLHelper.GetFunction<GetWindowThreadIdDelegate>("GetWindowThreadId");
            return func(OLAObject, hwnd);
        }

        /// <summary>
        /// 高级窗口查找，功能与 EnumWindowSuper 类似
        /// </summary>
        /// <param name="spec1">查找串1，内容取决于flag1的值</param>
        /// <param name="flag1">查找串1的类型，可选值
        ///<br/> 0: 标题
        ///<br/> 1: 程序名字（如notepad）
        ///<br/> 2: 类名
        ///<br/> 3: 程序路径（不含盘符，如\windows\system32）
        ///<br/> 4: 父句柄（十进制字符串）
        ///<br/> 5: 父窗口标题
        ///<br/> 6: 父窗口类名
        ///<br/> 7: 顶级窗口句柄（十进制字符串）
        ///<br/> 8: 顶级窗口标题
        ///<br/> 9: 顶级窗口类名
        /// </param>
        /// <param name="type1">查找串1的匹配方式
        ///<br/> 0: 精确匹配
        ///<br/> 1: 模糊匹配
        /// </param>
        /// <param name="spec2">查找串2，内容取决于flag2的值</param>
        /// <param name="flag2">查找串2的类型，可选值
        ///<br/> 0: 标题
        ///<br/> 1: 程序名字（如notepad）
        ///<br/> 2: 类名
        ///<br/> 3: 程序路径（不含盘符，如\windows\system32）
        ///<br/> 4: 父句柄（十进制字符串）
        ///<br/> 5: 父窗口标题
        ///<br/> 6: 父窗口类名
        ///<br/> 7: 顶级窗口句柄（十进制字符串）
        ///<br/> 8: 顶级窗口标题
        ///<br/> 9: 顶级窗口类名
        /// </param>
        /// <param name="type2">查找串2的匹配方式</param>
        /// <returns>找到的窗口句柄，未找到则返回 0</returns>
        /// <remarks>注意事项: 
        /// <br/>1. 两个条件必须同时满足才会返回窗口句柄
        /// <br/>2. 模糊匹配时，只要窗口属性包含指定的字符串即可匹配成功
        /// <br/>3. 程序路径匹配时不区分大小写，且不需要包含盘符
        /// <br/>4. 建议在使用此函数前，先使用 GetWindowTitle、GetWindowClass 等函数获取窗口信息
        /// <br/>5. 如果需要查找多个符合条件的窗口，可以使用 EnumWindowSuper 函数
        /// </remarks>
        public long FindWindowSuper(string spec1, int flag1, int type1, string spec2, int flag2, int type2){
            var func = OLAPlugDLLHelper.GetFunction<FindWindowSuperDelegate>("FindWindowSuper");
            return func(OLAObject, spec1, flag1, type1, spec2, flag2, type2);
        }

        /// <summary>
        /// 将客户区坐标转换为屏幕绝对坐标
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">指向客户区x坐标的变量，转换后存储屏幕x坐标</param>
        /// <param name="y">指向客户区y坐标的变量，转换后存储屏幕y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数用于坐标系转换，将相对于窗口客户区的坐标转为全局屏幕坐标
        /// <br/>2. 常用于将鼠标点击位置或控件位置映射到屏幕
        /// <br/>3. 转换考虑了窗口的位置、DPI缩放和多显示器布局
        /// <br/>4. 是实现精确UI自动化的基础
        /// </remarks>
        public int ClientToScreen(long hwnd, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<ClientToScreenDelegate>("ClientToScreen");
            return func(OLAObject, hwnd, out x, out y);
        }

        /// <summary>
        /// 将屏幕绝对坐标转换为客户区坐标
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="x">指向屏幕x坐标的变量，转换后存储客户区x坐标</param>
        /// <param name="y">指向屏幕y坐标的变量，转换后存储客户区y坐标</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 与 ClientToScreen 相反，将全局屏幕坐标转为相对于指定窗口客户区的坐标
        /// <br/>2. 常用于判断屏幕上的某个点是否在窗口客户区内
        /// <br/>3. 转换同样考虑了窗口位置、DPI和显示器设置
        /// <br/>4. 对于坐标计算和事件处理非常有用
        /// </remarks>
        public int ScreenToClient(long hwnd, out int x, out int y){
            var func = OLAPlugDLLHelper.GetFunction<ScreenToClientDelegate>("ScreenToClient");
            return func(OLAObject, hwnd, out x, out y);
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
            var func = OLAPlugDLLHelper.GetFunction<GetForegroundFocusDelegate>("GetForegroundFocus");
            return func(OLAObject);
        }

        /// <summary>
        /// 设置窗口的显示状态（可见性）
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="affinity">1-显示窗口, 0-隐藏窗口</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数直接控制窗口的可见性，类似于 SetWindowState(SW_SHOW/SW_HIDE)
        /// <br/>2. 隐藏窗口后，它将从屏幕上消失，但仍在进程中运行
        /// <br/>3. 显示隐藏的窗口可以使其重新出现
        /// <br/>4. 操作不会改变窗口的最小化或最大化状态
        /// </remarks>
        public int SetWindowDisplay(long hwnd, int affinity){
            var func = OLAPlugDLLHelper.GetFunction<SetWindowDisplayDelegate>("SetWindowDisplay");
            return func(OLAObject, hwnd, affinity);
        }

        /// <summary>
        /// 检查指定窗口是否处于“假死”状态
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <param name="time">识别间隔，单位毫秒</param>
        /// <returns>状态
        ///<br/>0: 正常
        ///<br/>1: 卡屏
        /// </returns>
        public int IsDisplayDead(int x1, int y1, int x2, int y2, int time){
            var func = OLAPlugDLLHelper.GetFunction<IsDisplayDeadDelegate>("IsDisplayDead");
            return func(OLAObject, x1, y1, x2, y2, time);
        }

        /// <summary>
        /// 获取指定窗口的刷新帧率（FPS）
        /// </summary>
        /// <param name="x1">查找区域的左上角X坐标</param>
        /// <param name="y1">查找区域的左上角Y坐标</param>
        /// <param name="x2">查找区域的右下角X坐标</param>
        /// <param name="y2">查找区域的右下角Y坐标</param>
        /// <returns>窗口的近似帧率，如 60, 30, 0（静态）等</returns>
        public int GetWindowsFps(int x1, int y1, int x2, int y2){
            var func = OLAPlugDLLHelper.GetFunction<GetWindowsFpsDelegate>("GetWindowsFps");
            return func(OLAObject, x1, y1, x2, y2);
        }

        /// <summary>
        /// 终止进程
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数强制结束指定ID的进程
        /// <br/>2. 终止后，进程及其所有资源将被系统回收
        /// <br/>3. 未保存的数据将会丢失
        /// <br/>4. 需要足够的权限才能终止某些系统或受保护的进程
        /// </remarks>
        public int TerminateProcess(long pid){
            var func = OLAPlugDLLHelper.GetFunction<TerminateProcessDelegate>("TerminateProcess");
            return func(OLAObject, pid);
        }

        /// <summary>
        /// 终止进程树
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数不仅终止指定进程，还递归终止其创建的所有子进程
        /// <br/>2. 用于彻底清理一个程序及其后台服务
        /// <br/>3. 操作非常强力，可能导致相关联的多个程序关闭
        /// <br/>4. 需要谨慎使用，避免误杀重要系统进程
        /// </remarks>
        public int TerminateProcessTree(long pid){
            var func = OLAPlugDLLHelper.GetFunction<TerminateProcessTreeDelegate>("TerminateProcessTree");
            return func(OLAObject, pid);
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
            var func = OLAPlugDLLHelper.GetFunction<GetCommandLineDelegate>("GetCommandLine");
            return PtrToStringUTF8(func(OLAObject, hwnd));
        }

        /// <summary>
        /// 检查字体平滑
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 字体平滑可使屏幕上的文字边缘更平滑，提高可读性
        /// <br/>2. 此设置影响所有应用程序的文本渲染
        /// <br/>3. 检查结果可用于调整自动化脚本的截图或OCR策略
        /// <br/>4. 在某些低分辨率或远程桌面场景下，此功能可能被关闭
        /// </remarks>
        public int CheckFontSmooth(){
            var func = OLAPlugDLLHelper.GetFunction<CheckFontSmoothDelegate>("CheckFontSmooth");
            return func(OLAObject);
        }

        /// <summary>
        /// 设置字体平滑
        /// </summary>
        /// <param name="enable">是否启用</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 该函数修改系统的全局字体渲染设置
        /// <br/>2. 更改后，新创建的窗口将使用新的设置
        /// <br/>3. 可能需要重启应用程序甚至系统才能完全生效
        /// <br/>4. 滥用此功能可能影响用户体验，应谨慎使用
        /// </remarks>
        public int SetFontSmooth(int enable){
            var func = OLAPlugDLLHelper.GetFunction<SetFontSmoothDelegate>("SetFontSmooth");
            return func(OLAObject, enable);
        }

        /// <summary>
        /// 启用调试权限
        /// </summary>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        /// <remarks>注意事项: 
        /// <br/>1. 调试权限（SeDebugPrivilege）允许进程调试或操作其他进程
        /// <br/>2. 此权限对于调用 TerminateProcess, EnumProcess 等函数通常是必需的
        /// <br/>3. 通常需要管理员权限才能成功启用
        /// <br/>4. 在进程启动后尽早调用此函数以确保后续操作的权限
        /// </remarks>
        public int EnableDebugPrivilege(){
            var func = OLAPlugDLLHelper.GetFunction<EnableDebugPrivilegeDelegate>("EnableDebugPrivilege");
            return func(OLAObject);
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
            var func = OLAPlugDLLHelper.GetFunction<SystemStartDelegate>("SystemStart");
            return func(OLAObject, applicationName, commandLine);
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
            var func = OLAPlugDLLHelper.GetFunction<CreateChildProcessDelegate>("CreateChildProcess");
            return func(OLAObject, applicationName, commandLine, currentDirectory, showType, parentProcessId);
        }

        /// <summary>
        /// 获取进程图标
        /// </summary>
        /// <param name="pid">进程ID</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <returns>进程图标</returns>
        public long GetProcessIconImage(long pid, int targetWidth, int targetHeight){
            var func = OLAPlugDLLHelper.GetFunction<GetProcessIconImageDelegate>("GetProcessIconImage");
            return func(OLAObject, pid, targetWidth, targetHeight);
        }

        /// <summary>
        /// 创建空的XML文档
        /// </summary>
        /// <returns>返回新创建的XML文档句柄，失败时返回0</returns>
        public long XmlCreateDocument(){
            var func = OLAPlugDLLHelper.GetFunction<XmlCreateDocumentDelegate>("XmlCreateDocument");
            return func();
        }

        /// <summary>
        /// 解析XML字符串
        /// </summary>
        /// <param name="str">要解析的XML字符串</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回解析后的XML文档句柄，失败时返回0</returns>
        public long XmlParse(string str, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlParseDelegate>("XmlParse");
            return func(str, out err);
        }

        /// <summary>
        /// 从文件加载并解析XML
        /// </summary>
        /// <param name="filepath">XML文件路径</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回解析后的XML文档句柄，失败时返回0</returns>
        public long XmlParseFile(string filepath, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlParseFileDelegate>("XmlParseFile");
            return func(filepath, out err);
        }

        /// <summary>
        /// 将XML文档序列化为字符串
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="compact">是否紧凑输出，0表示格式化，1表示紧凑</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回XML字符串，需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlToString(long doc, int compact, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlToStringDelegate>("XmlToString");
            return PtrToStringUTF8(func(doc, compact, out err));
        }

        /// <summary>
        /// 将XML文档保存到文件
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="filepath">保存的文件路径</param>
        /// <param name="compact">是否紧凑输出，0表示格式化，1表示紧凑</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果，1表示成功，0表示失败</returns>
        public int XmlSaveToFile(long doc, string filepath, int compact, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSaveToFileDelegate>("XmlSaveToFile");
            return func(doc, filepath, compact, out err);
        }

        /// <summary>
        /// 释放XML文档
        /// </summary>
        /// <param name="doc">要释放的XML文档句柄</param>
        /// <returns>操作结果
        ///<br/>0: 失败
        ///<br/>1: 成功
        /// </returns>
        public int XmlFree(long doc){
            var func = OLAPlugDLLHelper.GetFunction<XmlFreeDelegate>("XmlFree");
            return func(doc);
        }

        /// <summary>
        /// 获取XML文档的根元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回根元素句柄，失败时返回0</returns>
        public long XmlGetRootElement(long doc, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetRootElementDelegate>("XmlGetRootElement");
            return func(doc, out err);
        }

        /// <summary>
        /// 创建新的XML元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="name">元素名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回元素句柄，失败时返回0</returns>
        public long XmlCreateElement(long doc, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlCreateElementDelegate>("XmlCreateElement");
            return func(doc, name, out err);
        }

        /// <summary>
        /// 设置文档的根元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="element">要设置为根元素的元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlInsertRootElement(long doc, long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlInsertRootElementDelegate>("XmlInsertRootElement");
            return func(doc, element, out err);
        }

        /// <summary>
        /// 向元素添加子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="child">子元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlAppendChild(long parent, long child, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlAppendChildDelegate>("XmlAppendChild");
            return func(parent, child, out err);
        }

        /// <summary>
        /// 获取元素的第一个子元素
        /// </summary>
        /// <param name="element">父元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回子元素句柄，失败时返回0</returns>
        public long XmlGetFirstChild(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetFirstChildDelegate>("XmlGetFirstChild");
            return func(element, out err);
        }

        /// <summary>
        /// 获取元素的下一个兄弟元素
        /// </summary>
        /// <param name="element">当前元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回兄弟元素句柄，失败时返回0</returns>
        public long XmlGetNextSibling(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetNextSiblingDelegate>("XmlGetNextSibling");
            return func(element, out err);
        }

        /// <summary>
        /// 根据名称查找子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="name">要查找的元素名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回找到的元素句柄，失败时返回0</returns>
        public long XmlFindElement(long parent, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlFindElementDelegate>("XmlFindElement");
            return func(parent, name, out err);
        }

        /// <summary>
        /// 获取元素的名称
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回元素名称字符串，需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlGetElementName(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetElementNameDelegate>("XmlGetElementName");
            return PtrToStringUTF8(func(element, out err));
        }

        /// <summary>
        /// 获取元素的文本内容
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回文本内容字符串，需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlGetElementText(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetElementTextDelegate>("XmlGetElementText");
            return PtrToStringUTF8(func(element, out err));
        }

        /// <summary>
        /// 设置元素的文本内容
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="text">要设置的文本内容</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetElementText(long element, string text, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetElementTextDelegate>("XmlSetElementText");
            return func(element, text, out err);
        }

        /// <summary>
        /// 删除子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="child">要删除的子元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlRemoveChild(long parent, long child, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlRemoveChildDelegate>("XmlRemoveChild");
            return func(parent, child, out err);
        }

        /// <summary>
        /// 在指定子元素之前插入新元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="newChild">要插入的新元素句柄</param>
        /// <param name="refChild">参考子元素句柄，新元素将插入到它之前</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlInsertBefore(long parent, long newChild, long refChild, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlInsertBeforeDelegate>("XmlInsertBefore");
            return func(parent, newChild, refChild, out err);
        }

        /// <summary>
        /// 在指定子元素之后插入新元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="newChild">要插入的新元素句柄</param>
        /// <param name="refChild">参考子元素句柄，新元素将插入到它之后</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlInsertAfter(long parent, long newChild, long refChild, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlInsertAfterDelegate>("XmlInsertAfter");
            return func(parent, newChild, refChild, out err);
        }

        /// <summary>
        /// 获取元素的父元素
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回父元素句柄，失败时返回0</returns>
        public long XmlGetParent(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetParentDelegate>("XmlGetParent");
            return func(element, out err);
        }

        /// <summary>
        /// 获取元素的前一个兄弟元素
        /// </summary>
        /// <param name="element">当前元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回前一个兄弟元素句柄，失败时返回0</returns>
        public long XmlGetPreviousSibling(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetPreviousSiblingDelegate>("XmlGetPreviousSibling");
            return func(element, out err);
        }

        /// <summary>
        /// 获取元素的最后一个子元素
        /// </summary>
        /// <param name="element">父元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回最后一个子元素句柄，失败时返回0</returns>
        public long XmlGetLastChild(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetLastChildDelegate>("XmlGetLastChild");
            return func(element, out err);
        }

        /// <summary>
        /// 深度克隆元素（包括所有子元素和属性）
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="element">要克隆的元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回克隆的元素句柄，失败时返回0</returns>
        public long XmlCloneElement(long doc, long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlCloneElementDelegate>("XmlCloneElement");
            return func(doc, element, out err);
        }

        /// <summary>
        /// 检查元素是否有子元素
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回1表示有子元素，0表示没有</returns>
        public int XmlHasChildren(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlHasChildrenDelegate>("XmlHasChildren");
            return func(element, out err);
        }

        /// <summary>
        /// 获取元素的属性值
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回属性值字符串，需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlGetAttribute(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeDelegate>("XmlGetAttribute");
            return PtrToStringUTF8(func(element, name, out err));
        }

        /// <summary>
        /// 设置元素的属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">属性值</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetAttribute(long element, string name, string value, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetAttributeDelegate>("XmlSetAttribute");
            return func(element, name, value, out err);
        }

        /// <summary>
        /// 获取元素的整数类型属性值
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回整数值，失败时返回0</returns>
        public int XmlGetAttributeInt(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeIntDelegate>("XmlGetAttributeInt");
            return func(element, name, out err);
        }

        /// <summary>
        /// 设置元素的整数类型属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">整数值</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetAttributeInt(long element, string name, int value, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetAttributeIntDelegate>("XmlSetAttributeInt");
            return func(element, name, value, out err);
        }

        /// <summary>
        /// 获取元素的浮点数类型属性值
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回浮点数值，失败时返回0.0</returns>
        public double XmlGetAttributeDouble(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeDoubleDelegate>("XmlGetAttributeDouble");
            return func(element, name, out err);
        }

        /// <summary>
        /// 设置元素的浮点数类型属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">浮点数值</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetAttributeDouble(long element, string name, double value, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetAttributeDoubleDelegate>("XmlSetAttributeDouble");
            return func(element, name, value, out err);
        }

        /// <summary>
        /// 获取元素的布尔类型属性值
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回布尔值，失败时返回0</returns>
        public int XmlGetAttributeBool(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeBoolDelegate>("XmlGetAttributeBool");
            return func(element, name, out err);
        }

        /// <summary>
        /// 设置元素的布尔类型属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">布尔值（0表示false，非0表示true）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetAttributeBool(long element, string name, int value, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetAttributeBoolDelegate>("XmlSetAttributeBool");
            return func(element, name, value, out err);
        }

        /// <summary>
        /// 获取元素的64位整数类型属性值
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回64位整数值，失败时返回0</returns>
        public long XmlGetAttributeInt64(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeInt64Delegate>("XmlGetAttributeInt64");
            return func(element, name, out err);
        }

        /// <summary>
        /// 设置元素的64位整数类型属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="value">64位整数值</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetAttributeInt64(long element, string name, long value, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetAttributeInt64Delegate>("XmlSetAttributeInt64");
            return func(element, name, value, out err);
        }

        /// <summary>
        /// 检查元素是否有指定属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回1表示存在，0表示不存在</returns>
        public int XmlHasAttribute(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlHasAttributeDelegate>("XmlHasAttribute");
            return func(element, name, out err);
        }

        /// <summary>
        /// 删除元素的属性
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="name">属性名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlDeleteAttribute(long element, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlDeleteAttributeDelegate>("XmlDeleteAttribute");
            return func(element, name, out err);
        }

        /// <summary>
        /// 获取元素的所有属性名称
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回属性名称数组（以|分隔），需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlGetAttributeNames(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeNamesDelegate>("XmlGetAttributeNames");
            return PtrToStringUTF8(func(element, out err));
        }

        /// <summary>
        /// 获取元素的属性数量
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回属性数量，失败时返回0</returns>
        public int XmlGetAttributeCount(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetAttributeCountDelegate>("XmlGetAttributeCount");
            return func(element, out err);
        }

        /// <summary>
        /// 创建CDATA节点并添加到元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="element">元素句柄</param>
        /// <param name="content">CDATA内容</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetCDATA(long doc, long element, string content, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetCDATADelegate>("XmlSetCDATA");
            return func(doc, element, content, out err);
        }

        /// <summary>
        /// 创建注释节点并添加到元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="element">元素句柄</param>
        /// <param name="comment">注释内容</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlAddComment(long doc, long element, string comment, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlAddCommentDelegate>("XmlAddComment");
            return func(doc, element, comment, out err);
        }

        /// <summary>
        /// 创建XML声明
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="version">XML版本（如"1.0"）</param>
        /// <param name="encoding">编码（如"UTF-8"）</param>
        /// <param name="standalone">是否独立（0或1）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlSetDeclaration(long doc, string version, string encoding, int standalone, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlSetDeclarationDelegate>("XmlSetDeclaration");
            return func(doc, version, encoding, standalone, out err);
        }

        /// <summary>
        /// 使用路径查询元素
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="path">查询路径（如 "root/child/item"）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回找到的元素句柄，失败时返回0</returns>
        public long XmlQueryElement(long doc, string path, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlQueryElementDelegate>("XmlQueryElement");
            return func(doc, path, out err);
        }

        /// <summary>
        /// 获取元素的子元素数量
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回子元素数量，失败时返回0</returns>
        public int XmlGetChildCount(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetChildCountDelegate>("XmlGetChildCount");
            return func(element, out err);
        }

        /// <summary>
        /// 根据名称获取所有匹配的子元素数量
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="name">元素名称</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回匹配的子元素数量，失败时返回0</returns>
        public int XmlGetChildCountByName(long parent, string name, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetChildCountByNameDelegate>("XmlGetChildCountByName");
            return func(parent, name, out err);
        }

        /// <summary>
        /// 根据索引获取子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="index">子元素索引（从0开始）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回子元素句柄，失败时返回0</returns>
        public long XmlGetChildByIndex(long parent, int index, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetChildByIndexDelegate>("XmlGetChildByIndex");
            return func(parent, index, out err);
        }

        /// <summary>
        /// 根据名称和索引获取子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="name">元素名称</param>
        /// <param name="index">在同名元素中的索引（从0开始）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回子元素句柄，失败时返回0</returns>
        public long XmlGetChildByNameAndIndex(long parent, string name, int index, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetChildByNameAndIndexDelegate>("XmlGetChildByNameAndIndex");
            return func(parent, name, index, out err);
        }

        /// <summary>
        /// 查找具有指定属性值的子元素
        /// </summary>
        /// <param name="parent">父元素句柄</param>
        /// <param name="elementName">元素名称（可为NULL表示任意元素）</param>
        /// <param name="attrName">属性名称</param>
        /// <param name="attrValue">属性值</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回找到的元素句柄，失败时返回0</returns>
        public long XmlFindElementByAttribute(long parent, string elementName, string attrName, string attrValue, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlFindElementByAttributeDelegate>("XmlFindElementByAttribute");
            return func(parent, elementName, attrName, attrValue, out err);
        }

        /// <summary>
        /// 获取元素的深度（从根元素开始计数）
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回元素深度，根元素为0，失败时返回-1</returns>
        public int XmlGetElementDepth(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetElementDepthDelegate>("XmlGetElementDepth");
            return func(element, out err);
        }

        /// <summary>
        /// 获取元素的完整路径
        /// </summary>
        /// <param name="element">元素句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回路径字符串（如"/root/child/item"），需调用FreeStringPtr释放，失败时返回0</returns>
        public string XmlGetElementPath(long element, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetElementPathDelegate>("XmlGetElementPath");
            return PtrToStringUTF8(func(element, out err));
        }

        /// <summary>
        /// 比较两个元素是否相同（比较名称、属性和文本内容）
        /// </summary>
        /// <param name="element1">第一个元素句柄</param>
        /// <param name="element2">第二个元素句柄</param>
        /// <param name="deep">是否深度比较（包括所有子元素）</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回1表示相同，0表示不同</returns>
        public int XmlCompareElements(long element1, long element2, int deep, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlCompareElementsDelegate>("XmlCompareElements");
            return func(element1, element2, deep, out err);
        }

        /// <summary>
        /// 合并两个XML文档
        /// </summary>
        /// <param name="targetDoc">目标文档句柄</param>
        /// <param name="sourceDoc">源文档句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回操作结果错误码</returns>
        public int XmlMergeDocuments(long targetDoc, long sourceDoc, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlMergeDocumentsDelegate>("XmlMergeDocuments");
            return func(targetDoc, sourceDoc, out err);
        }

        /// <summary>
        /// 验证XML文档格式是否正确
        /// </summary>
        /// <param name="doc">XML文档句柄</param>
        /// <param name="err">错误码输出参数，可为0
        ///<br/> 0: 操作成功
        ///<br/> 1: 无效的句柄
        ///<br/> 2: XML解析失败
        ///<br/> 3: 类型不匹配
        ///<br/> 4: 元素不存在
        ///<br/> 5: 属性不存在
        ///<br/> 6: 未知错误
        /// </param>
        /// <returns>返回1表示有效，0表示无效</returns>
        public int XmlValidate(long doc, out int err){
            var func = OLAPlugDLLHelper.GetFunction<XmlValidateDelegate>("XmlValidate");
            return func(doc, out err);
        }

        /// <summary>
        /// 获取当前管理的XML对象数量（调试用）
        /// </summary>
        /// <returns>返回当前管理的XML对象数量</returns>
        public int XmlGetObjectCount(){
            var func = OLAPlugDLLHelper.GetFunction<XmlGetObjectCountDelegate>("XmlGetObjectCount");
            return func();
        }

        /// <summary>
        /// 清理所有XML对象（调试用）
        /// </summary>
        /// <returns></returns>
        public int XmlCleanupAll(){
            var func = OLAPlugDLLHelper.GetFunction<XmlCleanupAllDelegate>("XmlCleanupAll");
            return func();
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
            var func = OLAPlugDLLHelper.GetFunction<YoloLoadModelDelegate>("YoloLoadModel");
            return func(OLAObject, modelPath, outputPath, names_label, password, modelType, inferenceType, inferenceDevice);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloReleaseModelDelegate>("YoloReleaseModel");
            return func(OLAObject, modelHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloLoadModelMemoryDelegate>("YoloLoadModelMemory");
            return func(OLAObject, memoryAddr, size, modelType, inferenceType, inferenceDevice);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloInferDelegate>("YoloInfer");
            return PtrToStringUTF8(func(OLAObject, handle, imagePtr));
        }

        /// <summary>
        /// 检查模型是否有效
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>1 有效, 0 无效</returns>
        public int YoloIsModelValid(long modelHandle){
            var func = OLAPlugDLLHelper.GetFunction<YoloIsModelValidDelegate>("YoloIsModelValid");
            return func(OLAObject, modelHandle);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloListModelsDelegate>("YoloListModels");
            return PtrToStringUTF8(func(OLAObject));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloGetModelInfoDelegate>("YoloGetModelInfo");
            return PtrToStringUTF8(func(OLAObject, modelHandle));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloSetModelConfigDelegate>("YoloSetModelConfig");
            return func(OLAObject, modelHandle, configJson);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloGetModelConfigDelegate>("YoloGetModelConfig");
            return PtrToStringUTF8(func(OLAObject, modelHandle));
        }

        /// <summary>
        /// 模型预热
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <param name="iterations">预热迭代次数</param>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloWarmup(long modelHandle, int iterations){
            var func = OLAPlugDLLHelper.GetFunction<YoloWarmupDelegate>("YoloWarmup");
            return func(OLAObject, modelHandle, iterations);
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectDelegate>("YoloDetect");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, classes, confidence, iou, maxDetections));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectSimpleDelegate>("YoloDetectSimple");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectFromPtrDelegate>("YoloDetectFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, classes, confidence, iou, maxDetections));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectFromFileDelegate>("YoloDetectFromFile");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePath, classes, confidence, iou, maxDetections));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectFromBase64Delegate>("YoloDetectFromBase64");
            return PtrToStringUTF8(func(OLAObject, modelHandle, base64Data, classes, confidence, iou, maxDetections));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloDetectBatchDelegate>("YoloDetectBatch");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagesJson, classes, confidence, iou, maxDetections));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloClassifyDelegate>("YoloClassify");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, topK));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloClassifyFromPtrDelegate>("YoloClassifyFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, topK));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloClassifyFromFileDelegate>("YoloClassifyFromFile");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePath, topK));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloSegmentDelegate>("YoloSegment");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloSegmentFromPtrDelegate>("YoloSegmentFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloPoseDelegate>("YoloPose");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloPoseFromPtrDelegate>("YoloPoseFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloObbDelegate>("YoloObb");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloObbFromPtrDelegate>("YoloObbFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloKeyPointDelegate>("YoloKeyPoint");
            return PtrToStringUTF8(func(OLAObject, modelHandle, x1, y1, x2, y2, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloKeyPointFromPtrDelegate>("YoloKeyPointFromPtr");
            return PtrToStringUTF8(func(OLAObject, modelHandle, imagePtr, confidence, iou));
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
            var func = OLAPlugDLLHelper.GetFunction<YoloGetInferenceStatsDelegate>("YoloGetInferenceStats");
            return PtrToStringUTF8(func(OLAObject, modelHandle));
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        /// <param name="modelHandle">模型句柄</param>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloResetStats(long modelHandle){
            var func = OLAPlugDLLHelper.GetFunction<YoloResetStatsDelegate>("YoloResetStats");
            return func(OLAObject, modelHandle);
        }

        /// <summary>
        /// 获取最后一次错误信息
        /// </summary>
        /// <returns>错误信息字符串</returns>
        /// <remarks>注意事项: 
        /// <br/>1. DLL调用返回字符串指针地址,需要调用 FreeStringPtr接口释放内存
        /// </remarks>
        public string YoloGetLastError(){
            var func = OLAPlugDLLHelper.GetFunction<YoloGetLastErrorDelegate>("YoloGetLastError");
            return PtrToStringUTF8(func(OLAObject));
        }

        /// <summary>
        /// 清除错误信息
        /// </summary>
        /// <returns>1 成功, 0 失败</returns>
        public int YoloClearError(){
            var func = OLAPlugDLLHelper.GetFunction<YoloClearErrorDelegate>("YoloClearError");
            return func(OLAObject);
        }


    }
}
