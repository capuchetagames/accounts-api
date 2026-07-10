namespace Core.Dtos;

public class LoginDto
{
    /// <summary>
    /// O nome de usuário (username).
    /// </summary>
    public required string Name  { get; set; }
    /// <summary>
    /// O Cpf do usuário
    /// </summary>
    public required string Cpf  { get; set; }
    
    /// <summary>
    /// A senha do usuário.
    /// </summary>
    public required string Password  { get; set; }
}