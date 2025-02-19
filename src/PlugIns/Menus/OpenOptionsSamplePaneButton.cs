using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace Hearth.ArcGIS.Samples.PlugIns.Menus
{
    internal class OpenOptionsSamplePaneButton : Button
    {
        protected override void OnClick()
        {
            string paneID = "Hearth_ArcGIS_Samples_PlugIns_Panes_OptionsSamplePane";
            List<Pane> panes = FrameworkApplication.Panes.Find(paneID);
            if (panes.Any())
            {
                panes.First().Activate();
                return;
            }
            var view = new CIMGenericView
            {
                ViewType = paneID,
            };
            FrameworkApplication.Panes.Create(paneID, new object[] { view });
        }
    }
}
