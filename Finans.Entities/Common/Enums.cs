using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Entities.Common
{
    public enum CompanyType
    {
        AdminTenant = 0,
        Dealer = 1,
        SubDealer = 2,
        AccountingFirm = 3,
        ClientCompany = 4
    }

    public enum IntegrationTechnology
    {
        Api = 0,
        WebService = 1,
        Dll = 2
    }

    public enum AuthMethod
    {
        None = 0,
        Basic = 1,
        ApiKey = 2,
        OAuth2 = 3,
        ClientCertificate = 4
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    public enum TransferStatus
    {
        Success = 0,
        Failed = 1,
        Partial = 2,
        Pending = 3
    }
}
