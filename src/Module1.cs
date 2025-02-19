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
