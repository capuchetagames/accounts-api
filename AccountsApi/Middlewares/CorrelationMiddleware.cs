namespace UsersApi.Middlewares;

public class CorrelationMiddleware
{
    private const string HeaderName    = "X-Correlation-Id";
    public  const string ItemsKey      = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // salva no Items para os outros middlewares lerem
        context.Items[ItemsKey] = correlationId;

        // devolve no header da resposta
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }
}