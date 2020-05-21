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
        private IWarehouseProvider WarehouseProvider
        {
            get
            {
                return ServiceFactory.GetService<IWarehouseProvider>();
            }
        }

        public int ConfirmOrder(Order pOrder)
        {
            return 1;
        }

        public int GetStockLevelForBook(int warehouseId, int bookId)
        {
            return WarehouseProvider.GetStockLevelForBook(warehouseId, bookId);
        }
    }
}
