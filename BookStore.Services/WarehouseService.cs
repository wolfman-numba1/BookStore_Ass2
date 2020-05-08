using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Services.Interfaces;
using BookStore.Business.Components.Interfaces;
using Microsoft.Practices.ServiceLocation;
using BookStore.Services.MessageTypes;

namespace BookStore.Services
{
    public class WarehouseService : WarehouseService
    {
        private IWarehouseProvider WarehouseProvider
        {
            get
            {
                return ServiceFactory.GetService<IWarehouseProvider>();
            }
        }

        List<(Warehouse, Book)> ConfirmOrder(Order pOrder)
        {
            return WarehouseProvider.ConfirmOrder(pOrder);
        }

        int GetStockLevelForBook(int warehouseId, int bookId)
        {
            return WarehouseProvider.GetStockLevelForBook(warehouseId, bookId);
        }
    }
}
