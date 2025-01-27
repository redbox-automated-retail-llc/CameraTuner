using Redbox.DirectShow.Interop;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Redbox.DirectShow
{
    public class FilterInfo : IComparable
    {
        public FilterInfo(string monikerString)
        {
            this.MonikerString = monikerString;
            this.Name = this.GetName(monikerString);
        }

        public static object CreateFilter(string filterMoniker)
        {
            object ppvResult = (object)null;
            IBindCtx ppbc = (IBindCtx)null;
            IMoniker ppmk = (IMoniker)null;
            int pchEaten = 0;
            if (Win32.CreateBindCtx(0, out ppbc) == 0)
            {
                if (Win32.MkParseDisplayName(ppbc, filterMoniker, ref pchEaten, out ppmk) == 0)
                {
                    Guid guid = typeof(IBaseFilter).GUID;
                    ppmk.BindToObject((IBindCtx)null, (IMoniker)null, ref guid, out ppvResult);
                    Marshal.ReleaseComObject((object)ppmk);
                }
                Marshal.ReleaseComObject((object)ppbc);
            }
            return ppvResult;
        }

        public int CompareTo(object value)
        {
            FilterInfo filterInfo = (FilterInfo)value;
            return filterInfo == null ? 1 : this.Name.CompareTo(filterInfo.Name);
        }

        public string Name { get; private set; }

        public string MonikerString { get; private set; }

        internal FilterInfo(IMoniker moniker)
        {
            this.MonikerString = this.GetMonikerString(moniker);
            this.Name = this.GetName(moniker);
        }

        private string GetMonikerString(IMoniker moniker)
        {
            string ppszDisplayName;
            moniker.GetDisplayName((IBindCtx)null, (IMoniker)null, out ppszDisplayName);
            return ppszDisplayName;
        }

        private string GetName(IMoniker moniker)
        {
            object ppvObj = (object)null;
            try
            {
                Guid guid = typeof(IPropertyBag).GUID;
                moniker.BindToStorage((IBindCtx)null, (IMoniker)null, ref guid, out ppvObj);
                IPropertyBag propertyBag = (IPropertyBag)ppvObj;
                object obj = (object)"";
                ref object local = ref obj;
                IntPtr zero = IntPtr.Zero;
                int errorCode = propertyBag.Read("FriendlyName", ref local, zero);
                if (errorCode != 0)
                    Marshal.ThrowExceptionForHR(errorCode);
                string str = (string)obj;
                return str != null && str.Length >= 1 ? str : throw new ApplicationException();
            }
            catch (Exception ex)
            {
                return "";
            }
            finally
            {
                if (ppvObj != null)
                    Marshal.ReleaseComObject(ppvObj);
            }
        }

        private string GetName(string monikerString)
        {
            IBindCtx ppbc = (IBindCtx)null;
            IMoniker ppmk = (IMoniker)null;
            string name = "";
            int pchEaten = 0;
            if (Win32.CreateBindCtx(0, out ppbc) == 0)
            {
                if (Win32.MkParseDisplayName(ppbc, monikerString, ref pchEaten, out ppmk) == 0)
                {
                    name = this.GetName(ppmk);
                    Marshal.ReleaseComObject((object)ppmk);
                    ppmk = (IMoniker)null;
                }
                Marshal.ReleaseComObject((object)ppbc);
            }
            return name;
        }
    }
}
