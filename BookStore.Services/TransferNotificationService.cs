using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookStore.Business.Components.Interfaces;
using Bank.Services.Interfaces;
using Microsoft.Practices.ServiceLocation;


namespace BookStore.Services
{
    class TransferNotificationService : ITransferNotificationService
    {
       ITransferNotificationProvider Provider
        {
            get { return ServiceLocator.Current.GetInstance<ITransferNotificationProvider>(); }
        }


        public void NotifyTransferSuccess(string reference)
        {
            Provider.NotifyTransferSuccess(reference);
        }

        public void NotifyTransferFailed(string reference, string reason)
        {
            Provider.NotifyTransferFailed(reference, reason);
        }

    }
}
