using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Contracts.Transfer
{
    public sealed class CreateTransferBatchResultDto
    {
        public int BatchId { get; set; }
        public string BatchNo { get; set; } = "";
        public int ItemCount { get; set; }
    }
}
