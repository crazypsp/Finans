using Finans.Entities.Banking;

namespace Finans.Application.Abstractions.Banking
{
    public interface IBankImportValidationService
    {
        IReadOnlyList<string> Validate(
            Bank bank,
            BankAccount account,
            BankCredential credential);
    }
}