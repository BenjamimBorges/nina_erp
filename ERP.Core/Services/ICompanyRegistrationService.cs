using ERP.Shared.Dtos;

namespace ERP.Core.Services
{
    public interface ICompanyRegistrationService
    {
        Task<CompanyRegistrationResult> RegisterAsync(
            CompanyRegistrationRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public sealed class CompanyRegistrationResult
    {
        public bool Success { get; init; }
        public bool Conflict { get; init; }
        public string Message { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, string[]> Errors { get; init; } =
            new Dictionary<string, string[]>();

        public static CompanyRegistrationResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
            new() { Message = "Revise os dados informados.", Errors = errors };

        public static CompanyRegistrationResult Duplicate() =>
            new() { Conflict = true, Message = "Não foi possível cadastrar a empresa com os dados informados." };

        public static CompanyRegistrationResult Created() =>
            new() { Success = true, Message = "Empresa cadastrada com sucesso. Faça login para continuar." };
    }
}
