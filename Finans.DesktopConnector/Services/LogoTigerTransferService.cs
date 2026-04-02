using Finans.Application.Abstractions.Transfer;
using Finans.Contracts.Transfer;
using Finans.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// NOT: UnityObjects COM referansı projeye eklenmediğinden,
// Logo Tiger nesneleri 'dynamic' olarak kullanılmaktadır.
// Derleme zamanı tipi kontrolü yapılmaz; runtime'da COM DLL yüklü olmalıdır.
// COM DLL'i eklemek isterseniz:
//   Finans.DesktopConnector.csproj içine:
//   <COMReference Include="UnityObjects">...</COMReference>
//   veya NuGet paketi varsa PackageReference ekleyiniz.

namespace Finans.DesktopConnector.Services
{
    /// <summary>
    /// Logo Tiger'a banka hareketlerini muhasebe fişi olarak aktarır.
    /// Gerçek voucher mapping burada yapılır.
    /// </summary>
    public sealed class LogoTigerTransferService : ILogoTigerTransferService
    {
        private readonly FinansDbContext _db;
        private readonly IConfiguration _configuration;

        public LogoTigerTransferService(
            FinansDbContext db,
            IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<ErpTransferExecutionResultDto> TransferBankTransactionAsync(
            int companyId,
            int bankTransactionId,
            string? currentCode,
            string? glCode,
            string? bankAccountCode,
            CancellationToken ct = default)
        {
            var tx = await _db.BankTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId &&
                    x.Id == bankTransactionId &&
                    !x.IsDeleted,
                    ct);

            if (tx == null)
            {
                return Fail("BankTransaction bulunamadı.");
            }

            var validationError = ValidateTransferInputs(tx, glCode, bankAccountCode);
            if (validationError != null)
            {
                return Fail(validationError);
            }

            // dynamic: UnityApplication COM nesnesi
            dynamic? unityApp = null;

            try
            {
                // Tip: UnityObjects.UnityApplication (COM)
                var unityType = Type.GetTypeFromProgID("UnityObjects.UnityApplication");
                if (unityType == null)
                    return Fail("Logo Tiger COM bileşeni (UnityObjects.UnityApplication) bulunamadı. ProgID kayıtlı değil.");

                unityApp = Activator.CreateInstance(unityType);

                var login = Login(unityApp!);
                if (!login.IsSuccess)
                    return login;

                // DataObjectType.doGLVoucher = 24 (Logo Tiger sabit değeri)
                const int doGLVoucher = 24;
                dynamic? data = unityApp!.NewDataObject(doGLVoucher);
                if (data == null)
                    return Fail("Logo voucher nesnesi oluşturulamadı.");

                data.New();

                FillVoucherHeader(data, tx, bankAccountCode);
                FillVoucherLines(data, tx, currentCode, glCode!, bankAccountCode!);

                var postResult = PostVoucher(data, unityApp);
                if (!postResult.IsSuccess)
                    return postResult;

                return new ErpTransferExecutionResultDto
                {
                    IsSuccess = true,
                    VoucherNo = postResult.VoucherNo,
                    ReferenceNo = tx.ReferenceNumber,
                    Message = "Logo Tiger aktarımı başarılı."
                };
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
            finally
            {
                try
                {
                    if (unityApp != null)
                    {
                        unityApp.CompanyLogout();
                        unityApp.UserLogout();
                    }
                }
                catch
                {
                    // sessiz geç
                }
            }
        }

        private ErpTransferExecutionResultDto Login(dynamic unityApp)
        {
            var userName = _configuration["LogoTiger:UserName"];
            var password = _configuration["LogoTiger:Password"];
            var firmNr = _configuration.GetValue<int>("LogoTiger:FirmNr");
            var periodNr = _configuration.GetValue<int>("LogoTiger:PeriodNr");

            if (string.IsNullOrWhiteSpace(userName))
                return Fail("LogoTiger:UserName boş.");

            if (string.IsNullOrWhiteSpace(password))
                return Fail("LogoTiger:Password boş.");

            var ok = unityApp.Login(userName, password, firmNr, periodNr);
            if (!ok)
            {
                var err = unityApp.GetLastErrorString();
                return Fail($"Logo login başarısız. {err}");
            }

            return Success();
        }

        private static string? ValidateTransferInputs(dynamic tx, string? glCode, string? bankAccountCode)
        {
            if (string.IsNullOrWhiteSpace(glCode))
                return "GL kodu boş.";

            if (string.IsNullOrWhiteSpace(bankAccountCode))
                return "Banka hesap kodu boş.";

            if (tx.Amount == null || tx.Amount == 0)
                return "Aktarım tutarı sıfır olamaz.";

            if (string.IsNullOrWhiteSpace(tx.DebitCredit))
                return "Banka hareket yönü (DebitCredit) boş.";

            return null;
        }

        private static void FillVoucherHeader(
            dynamic data,
            dynamic tx,
            string? bankAccountCode)
        {
            SetField(data, "TYPE", 1);
            SetField(data, "NUMBER", "~");
            SetField(data, "DATE", tx.TransactionDate);
            SetField(data, "TIME", DateTime.Now);
            SetField(data, "DOC_NUMBER", tx.ReferenceNumber);
            SetField(data, "AUXIL_CODE", bankAccountCode);
            SetField(data, "AUTH_CODE", "FINANS");
            SetField(data, "NOTES1", tx.Description);
            SetField(data, "CURRSEL_TOTALS", 1);
            SetField(data, "RC_XRATE", 1);
        }

        private static void FillVoucherLines(
            dynamic data,
            dynamic tx,
            string? currentCode,
            string glCode,
            string bankAccountCode)
        {
            // dynamic: Lines COM nesnesi
            dynamic lines = data.DataFields.FieldByName("TRANSACTIONS").Lines;

            var amount = Convert.ToDecimal(tx.Amount);
            var isCredit = string.Equals((string?)tx.DebitCredit, "C", StringComparison.OrdinalIgnoreCase);

            if (isCredit)
            {
                // Para bankaya girmiş
                AppendLine(
                    lines,
                    accountCode: bankAccountCode,
                    currentCode: currentCode,
                    description: tx.Description,
                    docNumber: tx.ReferenceNumber,
                    debit: amount,
                    credit: 0m);

                AppendLine(
                    lines,
                    accountCode: glCode,
                    currentCode: currentCode,
                    description: tx.Description,
                    docNumber: tx.ReferenceNumber,
                    debit: 0m,
                    credit: amount);
            }
            else
            {
                // Para bankadan çıkmış
                AppendLine(
                    lines,
                    accountCode: glCode,
                    currentCode: currentCode,
                    description: tx.Description,
                    docNumber: tx.ReferenceNumber,
                    debit: amount,
                    credit: 0m);

                AppendLine(
                    lines,
                    accountCode: bankAccountCode,
                    currentCode: currentCode,
                    description: tx.Description,
                    docNumber: tx.ReferenceNumber,
                    debit: 0m,
                    credit: amount);
            }
        }

        private static void AppendLine(
            dynamic lines,
            string accountCode,
            string? currentCode,
            string? description,
            string? docNumber,
            decimal debit,
            decimal credit)
        {
            // dynamic: Lines.AppendLine() COM nesnesi
            dynamic line = lines.AppendLine();

            SetLineField(line, "ACCOUNT_CODE", accountCode);
            SetLineField(line, "ARP_CODE", currentCode);
            SetLineField(line, "DESCRIPTION", description);
            SetLineField(line, "DOC_NUMBER", docNumber);
            SetLineField(line, "DEBIT", debit);
            SetLineField(line, "CREDIT", credit);
        }

        private static ErpTransferExecutionResultDto PostVoucher(dynamic data, dynamic unityApp)
        {
            var postOk = data.Post();
            if (!postOk)
            {
                var err = ReadValidationErrors(data);
                if (string.IsNullOrWhiteSpace(err))
                    err = unityApp.GetLastErrorString();

                return Fail($"Logo post hatası: {err}");
            }

            var voucherNo =
                ReadFieldAsString(data, "NUMBER")
                ?? ReadFieldAsString(data, "FICHENO")
                ?? "~";

            return new ErpTransferExecutionResultDto
            {
                IsSuccess = true,
                VoucherNo = voucherNo,
                Message = "Voucher başarıyla oluşturuldu."
            };
        }

        private static string? ReadValidationErrors(dynamic data)
        {
            try
            {
                var validateErrors = data.ValidateErrors;
                if (validateErrors == null || validateErrors.Count == 0)
                    return null;

                var errors = new List<string>();
                for (var i = 0; i < validateErrors.Count; i++)
                {
                    errors.Add(validateErrors[i].Error);
                }

                return string.Join(" | ", errors);
            }
            catch
            {
                return null;
            }
        }

        private static void SetField(dynamic data, string fieldName, object? value)
        {
            try
            {
                data.DataFields.FieldByName(fieldName).Value = value;
            }
            catch
            {
                // sürüm farklarında sessiz tolerans
            }
        }

        private static void SetLineField(dynamic line, string fieldName, object? value)
        {
            try
            {
                line.FieldByName(fieldName).Value = value;
            }
            catch
            {
                // sürüm farklarında sessiz tolerans
            }
        }

        private static string? ReadFieldAsString(dynamic data, string fieldName)
        {
            try
            {
                return data.DataFields.FieldByName(fieldName).Value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static ErpTransferExecutionResultDto Fail(string message)
        {
            return new ErpTransferExecutionResultDto
            {
                IsSuccess = false,
                Message = message
            };
        }

        private static ErpTransferExecutionResultDto Success()
        {
            return new ErpTransferExecutionResultDto
            {
                IsSuccess = true
            };
        }
    }
}