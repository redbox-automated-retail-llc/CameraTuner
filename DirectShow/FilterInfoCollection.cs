using Microsoft.Win32;
using Redbox.DirectShow.Interop;
using Redbox.HAL.Component.Model.Extensions;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Redbox.DirectShow
{
    public class FilterInfoCollection : CollectionBase
    {
        private static bool? m_useOldOption;

        public FilterInfoCollection(Guid category) => this.CollectFilters(category);

        public FilterInfo this[int index] => (FilterInfo)this.InnerList[index];

        private void CollectFilters(Guid category)
        {
            object o = (object)null;
            IEnumMoniker enumMoniker = (IEnumMoniker)null;
            IMoniker[] rgelt = new IMoniker[1];
            try
            {
                o = Activator.CreateInstance(Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum) ?? throw new ApplicationException("Failed creating device enumerator"));
                if (((ICreateDevEnum)o).CreateClassEnumerator(ref category, out enumMoniker, 0) != 0)
                    throw new ApplicationException("No devices of the category");
                IntPtr zero = IntPtr.Zero;
                while (enumMoniker.Next(1, rgelt, zero) == 0)
                {
                    if (rgelt[0] == null)
                    {
                        if (FilterInfoCollection.UseOld)
                            break;
                    }
                    else
                    {
                        this.InnerList.Add((object)new FilterInfo(rgelt[0]));
                        Marshal.ReleaseComObject((object)rgelt[0]);
                        rgelt[0] = (IMoniker)null;
                    }
                }
                this.InnerList.Sort();
            }
            catch
            {
            }
            finally
            {
                if (o != null)
                    Marshal.ReleaseComObject(o);
                if (enumMoniker != null)
                    Marshal.ReleaseComObject((object)enumMoniker);
                if (rgelt[0] != null)
                {
                    Marshal.ReleaseComObject((object)rgelt[0]);
                    rgelt[0] = (IMoniker)null;
                }
            }
        }

        private static bool UseOld
        {
            get
            {
                if (FilterInfoCollection.m_useOldOption.HasValue)
                    return FilterInfoCollection.m_useOldOption.Value;
                RegistryKey registryKey = (RegistryKey)null;
                try
                {
                    registryKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Redbox\\HAL");
                    if (registryKey == null)
                    {
                        FilterInfoCollection.m_useOldOption = new bool?(false);
                        return FilterInfoCollection.m_useOldOption.Value;
                    }
                    object obj = registryKey.GetValue("UseOldEnumerator");
                    if (obj == null)
                    {
                        FilterInfoCollection.m_useOldOption = new bool?(false);
                        return FilterInfoCollection.m_useOldOption.Value;
                    }
                    FilterInfoCollection.m_useOldOption = new bool?(ConversionHelper.ChangeType<bool>(obj));
                    return FilterInfoCollection.m_useOldOption.Value;
                }
                catch (Exception ex)
                {
                    FilterInfoCollection.m_useOldOption = new bool?(false);
                    return FilterInfoCollection.m_useOldOption.Value;
                }
                finally
                {
                    registryKey?.Close();
                }
            }
        }
    }
}
