using Core.Models;

namespace AccountsApi.Service;

public class CorrelationIdService : ICorrelationIdService
{
    private static string _correlationId;
    
    public string Get() => _correlationId;

    public void Set(string correlationId) => _correlationId = correlationId;
}