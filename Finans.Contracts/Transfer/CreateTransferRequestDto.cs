using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finans.Contracts.Transfer
{
    /// <summary>
    /// Neden var?
    /// - Kullanıcının "Aktar" butonuna bastığında oluşacak komutu taşır.
    /// - Logo aktarımı Gün 3'te; bugün sadece batch/item yaratıyoruz.
    /// </summary>
    public sealed class CreateTransferRequestDto
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }

        public int BankTransactionId { get; set; }

        // Kullanıcı seçimleri (Logo tarafında kullanılacak)
        public string? CurrentCode { get; set; }
        public string? GlCode { get; set; }
        public string? BankAccCode { get; set; }
    }
}
