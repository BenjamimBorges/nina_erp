using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace ERP.API;

public sealed class CsrfTokenService
{
    private readonly IDataProtector _protector;

    public CsrfTokenService(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector("NinaERP.Csrf.v1");

    public string Create() => _protector.Protect(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try { _ = _protector.Unprotect(token); return true; }
        catch (CryptographicException) { return false; }
    }
}
