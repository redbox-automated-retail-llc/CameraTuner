using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Redbox.HAL.Common.GUI.Functions.Properties
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class Resources
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        internal Resources()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (Redbox.HAL.Common.GUI.Functions.Properties.Resources.resourceMan == null)
                    Redbox.HAL.Common.GUI.Functions.Properties.Resources.resourceMan = new ResourceManager("Redbox.HAL.Common.GUI.Functions.Properties.Resources", typeof(Redbox.HAL.Common.GUI.Functions.Properties.Resources).Assembly);
                return Redbox.HAL.Common.GUI.Functions.Properties.Resources.resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get => Redbox.HAL.Common.GUI.Functions.Properties.Resources.resourceCulture;
            set => Redbox.HAL.Common.GUI.Functions.Properties.Resources.resourceCulture = value;
        }
    }
}
