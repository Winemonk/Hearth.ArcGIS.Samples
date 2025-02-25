# Hearth ArcGIS 框架扩展（DryIoC、Options、Nlog、AutoMapper...）

## 1 使用IoC、DI

### 1.1 服务注册

#### 1.1.1 标记服务 

**1. 方式一**

需要注册服务类型时，在服务类型上添加`[Service]`标记：

```csharp
namespace Hearth.ArcGIS.Samples.Services
{
    public interface IHelloService
    {
        void SayHello();
    }
}
```

```csharp
using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service]
    public class HelloService : IHelloService
    {
        public void SayHello()
        {
            MessageBox.Show("Hello, World!", this.GetType().Name);
        }
    }
}
```

`ServiceAttribute`服务标记特性

```csharp
namespace Hearth.ArcGIS
{
    /// <summary>
    /// 服务标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ServiceAttribute : Attribute
    {
        /// <summary>
        /// 服务注册键
        /// </summary>
        public string? ServiceKey { get; set; }

        /// <summary>
        /// 服务注册类型
        /// </summary>
        public Type? ServiceType { get; set; }

        /// <summary>
        /// 服务重用模式
        /// </summary>
        public ReuseEnum Reuse { get; set; }

        /// <summary>
        /// 服务特性
        /// </summary>
        /// <param name="serviceType"> 服务注册类型 </param>
        /// <param name="serviceKey"> 服务注册键 </param>
        /// <param name="reuse"> 服务重用模式 </param>
        public ServiceAttribute(Type? serviceType = null, string? serviceKey = null, ReuseEnum reuse = ReuseEnum.Default)
        {
            ServiceType = serviceType;
            ServiceKey = serviceKey;
            Reuse = reuse;
        }
    }
}
```


`ReuseEnum`服务重用模式：

```csharp
using DryIoc;

namespace Hearth.ArcGIS
{
    /// <summary>
    /// 重用模式枚举
    /// </summary>
    public enum ReuseEnum
    {
        /// <summary>
        /// 默认。
        /// </summary>
        Default,

        /// <summary>
        /// 与作用域相同，但需要 <see cref="ThreadScopeContext"/> 。
        /// </summary>
        InThread,

        /// <summary>
        /// 作用域为任何作用域，可以有名称也可以没有名称。
        /// </summary>
        Scoped,

        /// <summary>
        /// 与 <see cref="Scoped"/> 相同，但在没有可用作用域的情况下，将回退到 <see cref="Singleton"/> 重用。
        /// </summary>
        ScopedOrSingleton,

        /// <summary>
        /// 容器中单例。
        /// </summary>
        Singleton,

        /// <summary>
        /// 瞬态，即不会重复使用。
        /// </summary>
        Transient,
    }
}
```

**1. 方式二**

使需要注册服务类型实现`ITransientService`、`ISingletonService`、`IScopedService`、`IScopedOrSingletonService`、`IInThreadService`接口：

```csharp
using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    public class HelloService : IHelloService, ITransientService
    {
        public void SayHello()
        {
            MessageBox.Show("Hello, World!", this.GetType().Name);
        }
    }
}
```

#### 1.1.2 注册服务

在模块加载时调用`HearthApp.Container.RegisterAssemblyAndRefrencedAssembliesTypes(Assembly assembly)`方法，自动注册模块`Assembly`及所引用的全部`Assembly`中的服务类型。

注册程序集类型：

```csharp
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("Hearth_ArcGIS_Samples_Module");

        public Module1()
        {
            HearthApp.App.RegisterAssemblyAndRefrencedAssembliesTypes(this.GetType().Assembly);
        }
    }
}
```

### 1.2 依赖注入

#### 1.2.1 SDK底层创建实例类型依赖注入

在使用 ArcGISProSDK 进行 Addin 开发时，由 SDK 创建的`Button`、`Pane`、`Dockpane`等`ArcGIS.Desktop.Framework.Contracts.PlugIn`组件在整个工程中充当的是`ViewModel`角色，而底层是根据`Type`使用`Activator.CreateInstance()`方法创建的实例，仅支持无参构造函数，因此无法直接使用构造函数注入。

解决思路有两个：
1. 使用类似`PostSharp`切片的方式，使用源生成器，在编译时将依赖注入逻辑编织到代码中；
    * 优点：编码简单，只需要在类上加一个切片特性即可；
    * 缺点：逻辑相对复杂，由于是编译时将切面逻辑代码编制到源码中，导致调试热重载无法使用；
2. 在使用SDK底层创建实例（Button、Pane等）时，在构造函数中主动调用依赖注入；
    * 优点：逻辑简单，在底层创建实例类型（Button、Pane等）上再封装个通用类即可；
    * 缺点：编码麻烦了点；

由于方式1无法使用调试热重载，导致debug困难，所以选择方式2实现；

```csharp
using ArcGIS.Desktop.Framework.Contracts;
using Hearth.ArcGIS.Samples.Services;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class SampleButton1 : Button, IInjectable // IScopeInjectable 使用作用域注入
    {
        [Inject]
        private readonly IHelloService? _helloService;

        public SampleButton1()
        {
            this.InjectServices();
            // this.InjectPropertiesAndFields(); // 不需要[Inject]特性标注注入字段/属性，但字段/属性也不能使用 readonly/init
        }

        protected override void OnClick()
        {
            _helloService?.SayHello();
        }
    }
}
```

为了方便编码，同时防止忽略了`this.InjectServices();`调用注入方法，可以提取一个基类；

```csharp
using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples.PlugIns.Contracts
{
    public class InjectableButton : Button, IInjectable
    {
        public InjectableButton()
        {
            this.InjectPropertiesAndFields();
        }
    }
}
```

简化代码：

```csharp
using Hearth.ArcGIS.Samples.PlugIns.Contracts;
using Hearth.ArcGIS.Samples.Services;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class SampleButton1 : InjectableButton
    {
        private IHelloService? _helloService;

        protected override void OnClick()
        {
            _helloService?.SayHello();
        }
    }
}
```

#### 1.2.2 `InjectAttribute`特性

```csharp
namespace Hearth.ArcGIS
{
    /// <summary>
    /// 自动注入特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
        /// <summary>
        /// 服务注册键
        /// </summary>
        public object? Key { get; set; }

        /// <summary>
        /// 服务类型
        /// </summary>
        public Type? ServiceType { get; set; }

        /// <summary>
        /// 注入服务特性
        /// </summary>
        /// <param name="key"> 服务注册键 </param>
        /// <param name="serviceType"> 服务类型 </param>
        public InjectAttribute(object? key = null, Type? serviceType = null)
        {
            Key = key;
            ServiceType = serviceType;
        }
    }
}
```

#### 1.2.3 服务类型依赖注入

在自定义的服务类型中（如`IHelloService`），可以直接使用构造函数注入，也可以和`1.1.1`中一样，使用属性注入，**推荐使用构造函数注入**。

```csharp
namespace Hearth.ArcGIS.Samples.Services
{
    public interface IHearthHelloService
    {
        void SayHello();
    }
}
```

构造函数注入：

```csharp
using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service(typeof(IHearthHelloService))]
    public class HearthHelloService : IHearthHelloService
    {
        private readonly IHelloService _helloService;
        public HearthHelloService(IHelloService helloService)
        {
            _helloService = helloService;
        }

        public void SayHello()
        {
            _helloService.SayHello();
            MessageBox.Show("Hello, Hearth!", this.GetType().Name);
        }
    }
}
```

属性注入：

```csharp
using ArcGIS.Desktop.Framework.Dialogs;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service(typeof(IHearthHelloService))]
    public class HearthHelloService : IHearthHelloService, IInjectable
    {
        [Inject]
        private readonly IHelloService _helloService;
        
        public HearthHelloService()
        {
            this.InjectServices();
        }

        public void SayHello()
        {
            _helloService.SayHello();
            MessageBox.Show("Hello, Hearth!", this.GetType().Name);
        }
    }
}
```

#### 1.2.4 视图模型类型依赖注入

对于非SDK底层自动创建的 View 组件，如自定义的`UserControl`、`Window`等，可以在View的`xaml`中使用`ViewModelLocator`（视图模型定位器）来绑定View的视图模型。视图模型类型也可以使用`Service`标记来进行自定义注册，对于未注册的视图模型类型，`Hearth`会对绑定的视图模型进行默认注册、注入。

```csharp
using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples.Dialogs
{
    public class SampleWindow1ViewModel : ViewModelBase
    {
        private string _sampleText = "Sample Text";
        public string SampleText
        {
            get => _sampleText;
            set => SetProperty(ref _sampleText, value, () => SampleText);
        }
    }
}
```

```xml
<Window
    x:Class="Hearth.ArcGIS.Samples.Dialogs.SampleWindow1"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:ha="clr-namespace:Hearth.ArcGIS;assembly=Hearth.ArcGIS"
    xmlns:local="clr-namespace:Hearth.ArcGIS.Samples.Dialogs"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    Title="SampleWindow1"
    Width="800"
    Height="450"
    ha:ViewModelLocator.AutoWireViewModel="True"
    mc:Ignorable="d">
    <!--  在设计时，将DataContext设置为SampleWindow1ViewModel，可以预览属性绑定效果，在运行时不会生效。  -->
    <d:Window.DataContext>
        <local:SampleWindow1ViewModel />
    </d:Window.DataContext>
    <Grid>
        <TextBlock
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            FontSize="24"
            FontWeight="Bold"
            Text="{Binding SampleText}" />
    </Grid>
</Window>
```

### 1.3 自定义容器初始化

HearthApp已经内置了DryIoc容器初始化、Nlog、ViewModelLocationProvider集成，当然也支持自定义初始化。

实现`ContainerBuilderBase`与`HearthAppBase`：

```csharp
using DryIoc;

namespace Hearth.ArcGIS.Samples
{
    public class CustomContainerBuilder : ContainerBuilder
    {
        public override Container Build()
        {
            Container container = new Container(
                rules => rules
                    .With(
                        FactoryMethod.ConstructorWithResolvableArgumentsIncludingNonPublic,
                        null,
                        PropertiesAndFields.All()));
            return container;
        }
    }
}
```

```csharp
namespace Hearth.ArcGIS.Samples
{
    public class CustomHearthApp : HearthAppBase
    {
        private static CustomHearthApp? _instance;
        public static CustomHearthApp Instance => _instance ??= new CustomHearthApp(new CustomContainerBuilder());
        public CustomHearthApp(ContainerBuilderBase containerBuilder) : base(containerBuilder)
        {

        }
    }
}
```

在使用依赖注入之前完成容器初始化、服务注册。

```csharp
CustomHearthApp.Instance.Container.RegisterAssemblyAndRefrencedAssembliesTypes(this.GetType().Assembly);
```


## 2 使用Options配置

### 2.1 创建配置类

```csharp
namespace Hearth.ArcGIS.Samples.Configs
{
    public class SampleSettings
    {
        public string Value1 { get; set; }
        public int Value2 { get; set; }
        public double Value3 { get; set; }
        public string[] Value4 { get; set; }
    }
}
```

### 2.2 在模块初始化时注册配置

```csharp
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Hearth.ArcGIS.Samples.Configs;
using Microsoft.Extensions.Configuration;

namespace Hearth.ArcGIS.Samples
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        public static Module1 Current => _this ??= (Module1)FrameworkApplication.FindModule("Hearth_ArcGIS_Samples_Module");

        public Module1()
        {
            // samplesettings.json文件内容
            // "SampleSettings": {
            //     "Value1": "asd",
            //     "Value2": 123,
            //     "Value3": 123.456,
            //     "Value4": [
            //         "asd",
            //         "zxc",
            //         "qwe"
            //     ]
            // }
            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("samplesettings.json", true, true).Build();
            HearthApp.App.Configure<SampleSettings>(configuration.GetSection(typeof(SampleSettings).Name));

            HearthApp.App.RegisterAssemblyAndRefrencedAssembliesTypes(this.GetType().Assembly);
        }
    }
}
```

### 2.3 配置使用样例

```xml
<UserControl
    x:Class="Hearth.ArcGIS.Samples.PlugIns.Panes.OptionsSamplePaneView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:extensions="clr-namespace:ArcGIS.Desktop.Extensions;assembly=ArcGIS.Desktop.Extensions"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:ui="clr-namespace:Hearth.ArcGIS.Samples.PlugIns.Panes"
    d:DesignHeight="600"
    d:DesignWidth="600"
    mc:Ignorable="d">
    <d:UserControl.DataContext>
        <ui:OptionsSamplePaneViewModel />
    </d:UserControl.DataContext>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <extensions:DesignOnlyResourceDictionary Source="pack://application:,,,/ArcGIS.Desktop.Framework;component\Themes\Default.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition />
            <ColumnDefinition />
        </Grid.ColumnDefinitions>
        <GroupBox Header="全局">
            <StackPanel Orientation="Vertical">
                <Button
                    Margin="3"
                    Command="{Binding RefreshOptionsCommand}"
                    Content="刷新"
                    Style="{StaticResource Esri_Button}" />
                <Label Content="OptionsValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding OptionsValue}" />
                <Label Content="OptionsMonitorValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding OptionsMonitorValue}" />
                <Label Content="OptionsSnapshotValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding OptionsSnapshotValue}" />
            </StackPanel>
        </GroupBox>
        <GroupBox Grid.Column="1" Header="作用域">
            <StackPanel Orientation="Vertical">
                <Button
                    Margin="3"
                    Command="{Binding RefreshScopeOptionsCommand}"
                    Content="刷新"
                    Style="{StaticResource Esri_Button}" />
                <Label Content="ScopeOptionsValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding ScopeOptionsValue}" />
                <Label Content="ScopeOptionsMonitorValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding ScopeOptionsMonitorValue}" />
                <Label Content="ScopeOptionsSnapshotValue: " />
                <TextBox
                    Height="180"
                    Margin="3"
                    Text="{Binding ScopeOptionsSnapshotValue}" />
            </StackPanel>
        </GroupBox>
    </Grid>
</UserControl>
```

```csharp
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework;
using DryIoc;
using Hearth.ArcGIS.Samples.Configs;
using Hearth.ArcGIS.Samples.PlugIns.Contracts;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Windows.Input;

namespace Hearth.ArcGIS.Samples.PlugIns.Panes
{
    internal class OptionsSamplePaneViewModel : InjectableViewStatePane
    {
        [Inject]
        private readonly IOptions<SampleSettings>? _options; // 在整个应用程序生命周期内只会初始化一次，不会发生变化
        [Inject]
        private readonly IOptionsMonitor<SampleSettings>? _optionsMonitor; // 实时监测配置变化，变化时可以触发通知
        [Inject]
        private readonly IOptionsSnapshot<SampleSettings>? _optionsSnapshot; // 在每个容器作用域内只会初始化一次，不会发生变化

        private IDisposable? _optionsMonitorDisposable;

        public OptionsSamplePaneViewModel() : base(null) { }
        public OptionsSamplePaneViewModel(CIMView cimView) : base(cimView)
        {
            _optionsMonitorDisposable = _optionsMonitor.OnChange(settings =>
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };
                string json = JsonSerializer.Serialize(settings, options);
                Notification notification = new Notification
                {
                    Title = "配置更新",
                    Message = json,
                    ImageUrl = @"pack://application:,,,/ArcGIS.Desktop.Resources;component/Images/GenericInformation32.png"
                };
                FrameworkApplication.AddNotification(notification);
            });
        }

        protected override void OnClosed()
        {
            _optionsMonitorDisposable?.Dispose();
            base.OnClosed();
        }

        private string _optionsValue;
        public string OptionsValue
        {
            get => _optionsValue;
            set => SetProperty(ref _optionsValue, value);
        }

        private string _optionsMonitorValue;
        public string OptionsMonitorValue
        {
            get => _optionsMonitorValue;
            set => SetProperty(ref _optionsMonitorValue, value);
        }

        private string _optionsSnapshotValue;
        public string OptionsSnapshotValue
        {
            get => _optionsSnapshotValue;
            set => SetProperty(ref _optionsSnapshotValue, value);
        }


        private string _scopeOptionsValue;
        public string ScopeOptionsValue
        {
            get => _scopeOptionsValue;
            set => SetProperty(ref _scopeOptionsValue, value);
        }

        private string _scopeOptionsMonitorValue;
        public string ScopeOptionsMonitorValue
        {
            get => _scopeOptionsMonitorValue;
            set => SetProperty(ref _scopeOptionsMonitorValue, value);
        }

        private string _scopeOptionsSnapshotValue;
        public string ScopeOptionsSnapshotValue
        {
            get => _scopeOptionsSnapshotValue;
            set => SetProperty(ref _scopeOptionsSnapshotValue, value);
        }

        public ICommand RefreshOptionsCommand => new RelayCommand(RefreshOptions);
        public ICommand RefreshScopeOptionsCommand => new RelayCommand(RefreshScopeOptions);

        private void RefreshOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            // 修改配置文件后刷新
            OptionsValue = JsonSerializer.Serialize(_options?.Value, options); // 不会变化
            OptionsMonitorValue = JsonSerializer.Serialize(_optionsMonitor?.CurrentValue, options); // 会实时变化
            OptionsSnapshotValue = JsonSerializer.Serialize(_optionsSnapshot?.Value, options); // 不会变化（因为当前实例使用的是应用程序全局作用域）
        }

        private void RefreshScopeOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            // 修改配置文件后刷新
            using (IResolverContext resolver = HearthApp.AppContainer.OpenScope())
            {
                IOptions<SampleSettings> scopeOptions = resolver.Resolve<IOptions<SampleSettings>>();
                IOptionsMonitor<SampleSettings> scopeOptionsMonitor = resolver.Resolve<IOptionsMonitor<SampleSettings>>();
                IOptionsSnapshot<SampleSettings> scopeOptionsSnapshot = resolver.Resolve<IOptionsSnapshot<SampleSettings>>();
                ScopeOptionsValue = JsonSerializer.Serialize(scopeOptions.Value, options); // 不会变化
                ScopeOptionsMonitorValue = JsonSerializer.Serialize(scopeOptionsMonitor.CurrentValue, options); // 会实时变化
                ScopeOptionsSnapshotValue = JsonSerializer.Serialize(scopeOptionsSnapshot.Value, options); // 会实时变化
            }
        }
    }
}
```

## 3 使用日志

```csharp
using Microsoft.Extensions.Logging;

namespace Hearth.ArcGIS.Samples.Services
{
    [Service(typeof(TestLogService))]
    public class TestLogService
    {
        private readonly ILogger<TestLogService> _logger;
        public TestLogService(ILogger<TestLogService> logger)
        {
            _logger = logger;
        }

        public void WriteLog()
        {
            _logger?.LogTrace("Configured Type Logger Class LogTrace");
            _logger?.LogDebug("Configured Type Logger Class LogDebug");
            _logger?.LogInformation("Configured Type Logger Class LogInformation");
            _logger?.LogWarning("Configured Type Logger Class LogWarning");
            _logger?.LogError("Configured Type Logger Class LogError");
            _logger?.LogCritical("Configured Type Logger Class LogCritical");
        }
    }
}
```

```csharp
using Hearth.ArcGIS.Samples.PlugIns.Contracts;
using Hearth.ArcGIS.Samples.Services;
using Microsoft.Extensions.Logging;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class LogSampleButton : InjectableButton
    {
        [Inject]
        private readonly ILogger? _logger;
        [Inject]
        private readonly ILogger<LogSampleButton>? _typeLogger;
        [Inject]
        private readonly TestLogService? _testLogService;


        protected override void OnClick()
        {
            _logger?.LogTrace("Default Logger LogTrace");
            _logger?.LogDebug("Default Logger LogDebug");
            _logger?.LogInformation("Default Logger LogInformation");
            _logger?.LogWarning("Default Logger LogWarning");
            _logger?.LogError("Default Logger LogError");
            _logger?.LogCritical("Default Logger LogCritical");

            _typeLogger?.LogTrace("Not configured Type Logger Class LogTrace");
            _typeLogger?.LogDebug("Not configured Type Logger Class LogDebug");
            _typeLogger?.LogInformation("Not configured Type Logger Class LogInformation");
            _typeLogger?.LogWarning("Not configured Type Logger Class LogWarning");
            _typeLogger?.LogError("Not configured Type Logger Class LogError");
            _typeLogger?.LogCritical("Not configured Type Logger Class LogCritical");

            _testLogService?.WriteLog();
        }
    }
}
```

**日志配置（.\Pro\bin\nlog.config）：**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      xsi:schemaLocation="http://www.nlog-project.org/schemas/NLog.xsd NLog.xsd"
      autoReload="true"
      throwExceptions="false"
      internalLogLevel="OFF" internalLogFile="c:\temp\nlog-internal.log">
	<targets>
		<target xsi:type="File" name="f_all" fileName="${basedir}/GISAPP/logs/all-${shortdate}.log"
				archiveNumbering="Sequence" archiveEvery="Day" maxArchiveDays="30" archiveAboveSize="104857600"
				layout="[${longdate}] ${threadid} ${level} ${callsite} ${callsite-linenumber} ${message} ${exception}" />
		<target xsi:type="File" name="f_default" fileName="${basedir}/GISAPP/logs/default-${shortdate}.log"
				archiveNumbering="Sequence" archiveEvery="Day" maxArchiveDays="30" archiveAboveSize="104857600"
				layout="[${longdate}] ${threadid} ${level} ${callsite} ${callsite-linenumber} ${message} ${exception}" />
		<target xsi:type="File" name="f_test" fileName="${basedir}/GISAPP/logs/test-${shortdate}.log"
				archiveNumbering="Sequence" archiveEvery="Day" maxArchiveDays="30" archiveAboveSize="104857600"
				layout="[${longdate}] ${threadid} ${level} ${callsite} ${callsite-linenumber} ${message} ${exception}" />
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="f_all" />
		<logger name="." minlevel="Trace" writeTo="f_default" />
		<logger name="Hearth.ArcGIS.Samples.Services.TestLogService" minlevel="Trace" writeTo="f_test" />
	</rules>
</nlog>
```

**日志：**

all-2025-02-19.log
```bash
[2025-02-19 15:34:01.9110] 1 Trace Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 19 Default Logger LogTrace 
[2025-02-19 15:34:02.0040] 1 Debug Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 20 Default Logger LogDebug 
[2025-02-19 15:34:02.0040] 1 Info Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 21 Default Logger LogInformation 
[2025-02-19 15:34:02.0040] 1 Warn Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 22 Default Logger LogWarning 
[2025-02-19 15:34:02.0040] 1 Error Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 23 Default Logger LogError 
[2025-02-19 15:34:02.0141] 1 Fatal Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 24 Default Logger LogCritical 
[2025-02-19 15:34:02.0141] 1 Trace Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 26 Not configured Type Logger Class LogTrace 
[2025-02-19 15:34:02.0141] 1 Debug Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 27 Not configured Type Logger Class LogDebug 
[2025-02-19 15:34:02.0141] 1 Info Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 28 Not configured Type Logger Class LogInformation 
[2025-02-19 15:34:02.0141] 1 Warn Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 29 Not configured Type Logger Class LogWarning 
[2025-02-19 15:34:02.0141] 1 Error Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 30 Not configured Type Logger Class LogError 
[2025-02-19 15:34:02.0141] 1 Fatal Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 31 Not configured Type Logger Class LogCritical 
[2025-02-19 15:34:02.0141] 1 Trace Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 16 Configured Type Logger Class LogTrace 
[2025-02-19 15:34:02.0141] 1 Debug Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 17 Configured Type Logger Class LogDebug 
[2025-02-19 15:34:02.0141] 1 Info Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 18 Configured Type Logger Class LogInformation 
[2025-02-19 15:34:02.0141] 1 Warn Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 19 Configured Type Logger Class LogWarning 
[2025-02-19 15:34:02.0141] 1 Error Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 20 Configured Type Logger Class LogError 
[2025-02-19 15:34:02.0141] 1 Fatal Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 21 Configured Type Logger Class LogCritical 
```

default-2025-02-19.log
```bash
[2025-02-19 15:34:01.9110] 1 Trace Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 19 Default Logger LogTrace 
[2025-02-19 15:34:02.0040] 1 Debug Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 20 Default Logger LogDebug 
[2025-02-19 15:34:02.0040] 1 Info Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 21 Default Logger LogInformation 
[2025-02-19 15:34:02.0040] 1 Warn Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 22 Default Logger LogWarning 
[2025-02-19 15:34:02.0040] 1 Error Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 23 Default Logger LogError 
[2025-02-19 15:34:02.0141] 1 Fatal Hearth.ArcGIS.Samples.PlugIns.Menus.LogSampleButton.OnClick 24 Default Logger LogCritical 
```

test-2025-02-19.log
```bash
[2025-02-19 15:34:02.0141] 1 Trace Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 16 Configured Type Logger Class LogTrace 
[2025-02-19 15:34:02.0141] 1 Debug Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 17 Configured Type Logger Class LogDebug 
[2025-02-19 15:34:02.0141] 1 Info Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 18 Configured Type Logger Class LogInformation 
[2025-02-19 15:34:02.0141] 1 Warn Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 19 Configured Type Logger Class LogWarning 
[2025-02-19 15:34:02.0141] 1 Error Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 20 Configured Type Logger Class LogError 
[2025-02-19 15:34:02.0141] 1 Fatal Hearth.ArcGIS.Samples.Services.TestLogService.WriteLog 21 Configured Type Logger Class LogCritical 
```

## 4 使用AutoMapper

```csharp
namespace Hearth.ArcGIS.Samples
{
    public class Person
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime Birthday { get; set; }
    }
}
```

```csharp
using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples
{
    public class PersonVO : ViewModelBase
    {
        private Guid _id;
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        private int _age;
        public int Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }
        private DateTime _birthday;
        public DateTime Birthday
        {
            get => _birthday;
            set => SetProperty(ref _birthday, value);
        }
    }
}
```

```csharp
using AutoMapper;

namespace Hearth.ArcGIS.Samples
{
    public class PersonProfile : Profile
    {
        public PersonProfile()
        {
            CreateMap<Person, PersonVO>();
            CreateMap<PersonVO, Person>();
        }
    }
}
```

```csharp
HearthApp.CONTAINER.ConfigureMapper(typeof(PersonProfile));
```
