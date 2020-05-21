using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Services.Interfaces;
using BookStore.Business.Components.Interfaces;
using Microsoft.Practices.ServiceLocation;
using BookStore.Services.MessageTypes;

namespace BookStore.Services
{
    public class WarehouseService : IWarehouseService
    {
        public int foo()
        {
            return 1;
        }
    }
}
