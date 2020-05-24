using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Bank.Services.Interfaces
{

    [ServiceContract]
    public interface ITransferNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyTransferSuccess(string reference, string customerEmail);

        [OperationContract(IsOneWay = true)]
        void NotifyTransferFailed(string reference, String reason, string customerEmail);
    }
}
