namespace Core.Dtos;

/// <summary>
/// Resposta para exibir dados seguros de um usuário.
/// </summary>
public class UserResponseDto
{
    /// <summary>
    /// O ID único do usuário.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// O Cpf do usuário.
    /// </summary>
    public int Cpf { get; set; }

    /// <summary>
    /// O nome de usuário (username).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// O email de login do usuário.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// O nível de permissão do usuário.
    /// </summary>
    public Role Role { get; set; }
}