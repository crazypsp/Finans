using Finans.Application.Models.Banking;

namespace Finans.Infrastructure.Banking.Legacy
{
    public sealed class LegacyBankRow
    {
        public string? BNKCODE { get; set; }
        public string? HESAPNO { get; set; }
        public string? URF { get; set; }
        public string? SUBECODE { get; set; }
        public string? FRMIBAN { get; set; }
        public string? FRMVKN { get; set; }
        public string? CURRENCYCODE { get; set; }
        public string? PROCESSID { get; set; }
        public string? PROCESSREFNO { get; set; }
        public string? PROCESSIBAN { get; set; }
        public string? PROCESSVKN { get; set; }
        public string? PROCESSAMAOUNT { get; set; }
        public string? PROCESSBALANCE { get; set; }
        public string? PROCESSDESC { get; set; }
        public string? PROCESSDESC2 { get; set; }
        public string? PROCESSDESC3 { get; set; }
        public string? PROCESSDESC4 { get; set; }
        public string? PROCESSDEBORCRED { get; set; }
        public DateTime? PROCESSTIME { get; set; }
        public DateTime? PROCESSTIME2 { get; set; }
        public string? PROCESSTIMESTR { get; set; }
        public string? PROCESSTIMESTR2 { get; set; }
        public string? PROCESSTYPECODE { get; set; }
        public string? PROCESSTYPECODEMT940 { get; set; }
        public int Durum { get; set; }
    }
}
