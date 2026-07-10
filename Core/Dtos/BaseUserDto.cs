namespace Core.Dtos;

/// <summary>
/// DTO base para validação de usuário.
/// </summary>
public class BaseUserDto
{
    /// <summary>
    /// Nome do usuário.
    /// </summary>
    public required string Name  { get; set; }
    
    /// <summary>
    /// Cpf do usuário.
    /// </summary>
    public required string Cpf  { get; set; }
    
    /// <summary>
    /// Email de login do usuário.
    /// </summary>
    public required string Email  { get; set; }
    
    /// <summary>
    /// Senha do usuário.
    /// </summary>
    public required string Password  { get; set; }
    public required string ConfirmPassword  { get; set; }
}