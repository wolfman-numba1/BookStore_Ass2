using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bank.Business.Entities;
using System.ServiceModel;

namespace Bank.Services.Interfaces
{
    [ServiceContract]
    public interface IOutcomeNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyOperationOutcome(String reference, OperationOutcome pOutcome);
    }
}
