using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticMobileApp.Services.LocationStreaming
{
    public interface IGpsListener
    {
        event EventHandler<Location> LocationChanged;

        // Методы управления
        void StartListening();
        void StopListening();
    }
}
