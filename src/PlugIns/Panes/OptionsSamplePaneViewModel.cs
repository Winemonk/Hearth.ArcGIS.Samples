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
        private readonly IOptionsSnapshot<SampleSettings>? _optionsSnapshot; // 在每个容器范围内只会初始化一次，不会发生变化

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
            OptionsSnapshotValue = JsonSerializer.Serialize(_optionsSnapshot?.Value, options); // 不会变化（因为当前实例使用的是应用程序全局范围）
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
