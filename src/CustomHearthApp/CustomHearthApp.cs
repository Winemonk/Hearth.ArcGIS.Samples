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
