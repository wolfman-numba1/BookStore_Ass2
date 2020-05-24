using Bank.Services.Interfaces;
using DeliveryCo.Services.Interfaces;
using EmailService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using BookStore.Business.Components;
using System.Text;

namespace BookStore.Business.Components
{
    public class ExternalServiceFactory
    {
        private static ExternalServiceFactory sFactory = new ExternalServiceFactory();

        public static ExternalServiceFactory Instance
        {
            get
            {
                return sFactory;
            }
        }



        public IEmailService EmailService
        {
            get
            {
                //  get { return new EmailServiceClient(); }
                return GetMsmqService<IEmailService>("net.msmq://localhost/private/EmailMessageQueue");
                // return GetTcpService<IEmailService>("net.tcp://localhost:9040/EmailService");
            }
        }

        public ITransferService TransferService
        {
            get
            {
               
                return GetMsmqService<ITransferService>("net.msmq://localhost/private/BankTransferTransacted");
              
                // return GetTcpService<ITransferService>("net.tcp://localhost:9020/TransferService");
            }
        }

        public IDeliveryService DeliveryService
        {
            get
            {
                return GetMsmqService<IDeliveryService>("net.msmq://localhost/private/DeliveryService");
            }
        }



        private T GetTcpService<T>(String pAddress)
        {
            NetTcpBinding tcpBinding = new NetTcpBinding() { TransactionFlow = true };
            EndpointAddress address = new EndpointAddress(pAddress);
            return new ChannelFactory<T>(tcpBinding, pAddress).CreateChannel();
        }

        private T GetMsmqService<T>(String pAddress)
        {
            NetMsmqBinding msmqBinding = new NetMsmqBinding();
            msmqBinding.Security.Mode = NetMsmqSecurityMode.None;
            EndpointAddress address = new EndpointAddress(pAddress);
            return new ChannelFactory<T>(msmqBinding, pAddress).CreateChannel();
        }
    }
}
