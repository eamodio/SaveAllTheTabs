using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;

namespace SaveAllTheTabs.Polyfills
{
    [Guid("2b0505e2-991e-4d8b-9cf8-826d1ec6f5f9")]
    [ComVisible(true)]
    public interface IPackageProviderService { }

    [Guid("b082db19-6eee-4fb0-8ac3-eb2e2c8c43a3")]
    public class PackageProviderService : IPackageProviderService
    {
        public SaveAllTheTabsPackage Package { get; }

        public PackageProviderService(SaveAllTheTabsPackage package)
        {
            Package = package;

            ((IServiceContainer)package).AddService(typeof(PackageProviderService), this, true);
        }
    }
}
