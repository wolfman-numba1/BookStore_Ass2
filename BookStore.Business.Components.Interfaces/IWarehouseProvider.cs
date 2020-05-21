using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Entities;

namespace BookStore.Business.Components.Interfaces
{
    public interface IWarehouseProvider
    {

        int ConfirmOrder(Order pOrder);

        int GetStockLevelForBook(int warehouseId, int bookId);
    }
}
