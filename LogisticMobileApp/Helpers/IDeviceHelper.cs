using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticMobileApp.Helpers
{
    public interface IDeviceHelper
    {
        string GetDeviceName();
        Task<string> GetOrCreateDeviceIdentifierAsync();
    }
}