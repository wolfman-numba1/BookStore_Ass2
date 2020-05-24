using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Bank.Services.Interfaces;

namespace Bank.Business.Components
{
    public class TransferNotificationServiceFactory
    {

        public static ITransferNotificationService GetTransferNotificationService(String pAddress)
        {
            NetMsmqBinding msmqCallbackBinding = new NetMsmqBinding();
            msmqCallbackBinding.Security.Mode = NetMsmqSecurityMode.None;

            ChannelFactory<ITransferNotificationService> lChannelFactory = new ChannelFactory<ITransferNotificationService>(msmqCallbackBinding, new EndpointAddress(pAddress));
            return lChannelFactory.CreateChannel();
        }

    }
}
