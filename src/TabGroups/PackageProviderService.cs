using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TabGroups
{
    [Guid("2b0505e2-991e-4d8b-9cf8-826d1ec6f5f9")]
    [ComVisible(true)]
    public interface IPackageProviderService
    {
        //void PassNameAndOpenToolWindow(string name);
    }

    //public interface SPackageProviderService
    //{
    //}

    [Guid("b082db19-6eee-4fb0-8ac3-eb2e2c8c43a3")]
    public class PackageProviderService : IPackageProviderService//, SPackageProviderService
    {
        public TabGroupsPackage Package { get; }

        public PackageProviderService(TabGroupsPackage package)
        {
            Package = package;

            ((IServiceContainer)package).AddService(typeof(PackageProviderService), this, true);
        }
    }
}
