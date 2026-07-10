using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core;
using Core.Dtos;
using Core.Entity;
using Core.Models;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AccountsApi.Controllers;

/// <summary>
/// Gerencia a autenticação de usuários e a geração de tokens JWT.
/// </summary>
[ApiController]
[Route("/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAccountRepository _accountRepository;
    
    private IValidator<LoginDto>  _loginDtoValidator;

    private IBaseLogger<AuthController> _logger;
    
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(IConfiguration configuration, IAccountRepository  accountRepository,
        IValidator<LoginDto> loginDtoValidator, IBaseLogger<AuthController> logger, IPasswordHasher<User> passwordHasher)
    {
        _configuration = configuration;
        _accountRepository =  accountRepository;
        _loginDtoValidator =  loginDtoValidator;
        _logger =  logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    /// <remarks>
    /// Este endpoint valida as credenciais do usuário.<br/>
    /// Em caso de sucesso, gera um token JWT com validade de 30 minutos,<br/>
    /// contendo as claims 'sub' (username) e 'role' (permissão).
    /// </remarks>
    /// <param name="loginDto">Objeto contendo o 'Name' (username) e 'Password'.</param>
    /// <returns>Um objeto contendo o token JWT.</returns>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var validationResult = await _loginDtoValidator.ValidateAsync(loginDto);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
        
            _logger.LogInformation($"Tentativa de login para o usuário: {loginDto.Name}");
        
            var user = _accountRepository.GetUserByCpf(loginDto.Cpf);
        
            if(user == null)
            {
                _logger.LogWarning($"Falha na autenticação: {loginDto.Name}. Usuário não encontrado.");
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }
            
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning($"Falha na autenticação: {loginDto.Name}. Senha inválida.");
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }
            
            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation($"Usuário {loginDto.Name} autenticado. Atualizando hash de senha para novo padrão.");
                // Atualiza o hash no banco com o novo padrão, de forma transparente
                user.PasswordHash = _passwordHasher.HashPassword(user, loginDto.Password);
                _accountRepository.Update(user); // Salva o novo hash
            }
            
            _logger.LogInformation($"Usuário {loginDto.Name} autenticado com sucesso.");
        
        
            var token = GenerateToken(user.Id, user.Role);

            var loginResponse = new LoginResponseDto { Token = token };
            
            return Ok(loginResponse);

        }
        catch (Exception e)
        {
            _logger.LogError($"{e.Message} / Erro inesperado durante o login.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocorreu um erro interno."});
        }
    }
    
    /// <summary>
    /// Gera um token JWT para o usuário especificado.
    /// </summary>
    /// <param name="id">O id do usuário (será usado como 'sub').</param>
    /// <param name="role">O nível de permissão (será usado como 'role').</param>
    /// <returns>Uma string contendo o token JWT.</returns>
    private string GenerateToken(Guid id, Role role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer:_configuration["Jwt:Issuer"],
            claims:claims,
            expires:DateTime.Now.AddMinutes(30),
            signingCredentials:credential);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida um token JWT e retorna as informações do usuário.
    /// </summary>
    /// <remarks>
    /// Este endpoint é usado por outros microserviços para validar tokens JWT.<br/>
    /// Retorna as informações do usuário se o token for válido.
    /// </remarks>
    /// <param name="token">Token JWT a ser validado (sem o prefixo 'Bearer ').</param>
    /// <returns>Informações do usuário se o token for válido.</returns>
    [HttpPost("validate")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenValidationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Unauthorized(new { message = "Token não fornecido." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out SecurityToken validatedToken);
            
            var id = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
            {
                return Unauthorized(new { message = "Token inválido - claims obrigatórias ausentes." });
            }

            // Buscar usuário no banco para garantir que ainda existe e está ativo
            var user = _accountRepository.GetById(Guid.Parse(id));
            if (user == null)
            {
                _logger.LogWarning($"Token válido mas usuário {id} não encontrado no banco.");
                return Unauthorized(new { message = "Usuário não encontrado." });
            }

            var response = new TokenValidationResponseDto
            {
                IsValid = true,
                Username = user.Name,
                IsActive = user.IsActive,
                Role = role,
                UserId = user.Id,
                TokenId = jti
            };

            _logger.LogInformation($"Token validado com sucesso para usuário: {user.Name}  - {role}");
            return Ok(response);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expirado recebido para validação.");
            return Unauthorized(new { message = "Token expirado." });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning($"Token inválido recebido: {ex.Message}");
            return Unauthorized(new { message = "Token inválido." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro inesperado durante validação de token: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno do servidor." });
        }
    }
}