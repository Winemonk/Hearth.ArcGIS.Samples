using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Core;

namespace Hearth.ArcGIS.Samples.PlugIns.Contracts
{
    public class InjectableViewStatePane : ViewStatePane, IInjectable
    {
        public InjectableViewStatePane(CIMView cimView) : base(cimView)
        {
            this.InjectServices();
        }

        public override CIMView ViewState
        {
            get
            {
                _cimView.InstanceID = (int)InstanceID;
                return _cimView;
            }
        }
    }
}
