using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Hearth.ArcGIS.Samples.Configs;
using Hearth.ArcGIS.Samples.Mappers;
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
            //CustomHearthApp.Instance.Container.Configure<SampleSettings>(configuration.GetSection(typeof(SampleSettings).Name));
            //CustomHearthApp.Instance.Container.RegisterAssemblyAndRefrencedAssembliesTypes(this.GetType().Assembly);
            HearthApp.CONTAINER.Configure<SampleSettings>(configuration.GetSection(typeof(SampleSettings).Name));
            HearthApp.CONTAINER.RegisterAssemblyAndRefrencedAssembliesTypes(this.GetType().Assembly);

            //HearthApp.CONTAINER.ConfigureMapper<PersonProfile>();
            HearthApp.CONTAINER.ConfigureMapper(typeof(PersonProfile));
        }
    }
}
