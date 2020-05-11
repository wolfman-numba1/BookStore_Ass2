using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using BookStore.Services.MessageTypes;

namespace BookStore.Services.Interfaces
{
    [ServiceContract]
    public interface IWarehouseService
    {
        [OperationContract]
        
        List<(Warehouse, Book)> ConfirmOrder(Order pOrder);

        [OperationContract]

        int GetStockLevelForBook(int warehouseId, int bookId);
    }
}
