using DeliveryCo.Services.Interfaces;
using EmailService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace DeliveryCo.Business.Components
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
                return GetTcpService<IEmailService>("net.tcp://localhost:9040/EmailService");
            }
        }

        private T GetTcpService<T>(String pAddress)
        {
            NetTcpBinding tcpBinding = new NetTcpBinding() { TransactionFlow = true };
            EndpointAddress address = new EndpointAddress(pAddress);
            return new ChannelFactory<T>(tcpBinding, pAddress).CreateChannel();
        }
    }
}
