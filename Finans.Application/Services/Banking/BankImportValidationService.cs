using Finans.Application.Abstractions.Banking;
using Finans.Entities.Banking;

namespace Finans.Application.Services.Banking
{
    public sealed class BankImportValidationService : IBankImportValidationService
    {
        public IReadOnlyList<string> Validate(
            Bank bank,
            BankAccount account,
            BankCredential credential)
        {
            var errors = new List<string>();

            if (bank == null)
            {
                errors.Add("Bank kaydı boş.");
                return errors;
            }

            if (account == null)
            {
                errors.Add("BankAccount kaydı boş.");
                return errors;
            }

            if (credential == null)
            {
                errors.Add("BankCredential kaydı boş.");
                return errors;
            }

            if (!bank.IsActive || bank.IsDeleted)
                errors.Add("Banka aktif değil.");

            if (string.IsNullOrWhiteSpace(bank.ProviderCode))
                errors.Add("ProviderCode boş.");

            if (!account.IsActive || account.IsDeleted)
                errors.Add("Banka hesabı aktif değil.");

            if (string.IsNullOrWhiteSpace(account.AccountNumber))
                errors.Add("AccountNumber boş.");

            if (!credential.IsActive || credential.IsDeleted)
                errors.Add("Credential aktif değil.");

            if (string.IsNullOrWhiteSpace(credential.Username))
                errors.Add("Credential Username boş.");

            if (string.IsNullOrWhiteSpace(credential.Password))
                errors.Add("Credential Password boş.");

            if (bank.RequiresLink && string.IsNullOrWhiteSpace(bank.DefaultLink))
                errors.Add("Bu banka için DefaultLink zorunlu.");

            if (bank.RequiresTLink && string.IsNullOrWhiteSpace(bank.DefaultTLink))
                errors.Add("Bu banka için DefaultTLink zorunlu.");

            if (bank.RequiresAccountNumber && string.IsNullOrWhiteSpace(account.AccountNumber))
                errors.Add("Bu banka için AccountNumber zorunlu.");

            return errors;
        }
    }
}