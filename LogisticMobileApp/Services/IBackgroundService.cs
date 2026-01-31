using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticMobileApp.Services
{
    public interface IBackgroundService
    {
        void Start(string driverName, string licensePlate);
        void Stop();
    }
}
