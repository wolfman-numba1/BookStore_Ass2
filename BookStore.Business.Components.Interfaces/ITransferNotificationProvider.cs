using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BookStore.Business.Components.Interfaces
{
   public interface ITransferNotificationProvider
    {
        void NotifyTransferSuccess(string reference, string customerEmail);

        void NotifyTransferFailed(string reference, string reason, string customerEmail);
    }
}
