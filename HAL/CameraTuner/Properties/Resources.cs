using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Redbox.HAL.CameraTuner.Properties
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
                if (Redbox.HAL.CameraTuner.Properties.Resources.resourceMan == null)
                    Redbox.HAL.CameraTuner.Properties.Resources.resourceMan = new ResourceManager("Redbox.HAL.CameraTuner.Properties.Resources", typeof(Redbox.HAL.CameraTuner.Properties.Resources).Assembly);
                return Redbox.HAL.CameraTuner.Properties.Resources.resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get => Redbox.HAL.CameraTuner.Properties.Resources.resourceCulture;
            set => Redbox.HAL.CameraTuner.Properties.Resources.resourceCulture = value;
        }
    }
}
