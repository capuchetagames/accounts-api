using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountsApi.Service.Validator;
using Core;
using Core.Dtos;
using Core.Entity;
using Core.Models;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace AccountsApi.Controllers;

/// <summary>
/// Gerencia as operações CRUD para os usuários da plataforma.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IRabbitMqService _rabbitMq;
    
    private IValidator<BaseUserDto> _validator;
    private readonly IPasswordHasher<User> _passwordHasher;
    
    private readonly ILogger<AccountController> _logger;
    
    public AccountController(IAccountRepository accountRepository, IValidator<BaseUserDto>  validator,
        IPasswordHasher<User>  passwordHasher, IRabbitMqService rabbitMq, ILogger<AccountController> logger)
    {
        _accountRepository = accountRepository;
        _validator = validator;
        _passwordHasher = passwordHasher;
        _rabbitMq = rabbitMq;
        _logger = logger;
    }
    
    /// <summary>
    /// Lista todos os usuários cadastrados.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a usuários com permissão de 'Admin'.<br/>
    /// ATENÇÃO: Retorna a entidade User completa, incluindo hash de senha.
    /// </remarks>
    /// <returns>Uma lista de objetos User.</returns>
    [HttpGet] 
    [Authorize(Policy = nameof(Role.Admin))]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get()
    {
        try
        {
            var users = _accountRepository.GetAll();
            var responseDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Cpf = u.Cpf,
                Email = u.Email,
                Role = u.Role
            });
            
            return Ok(responseDtos);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    
    /// <summary>
    /// Busca um usuário específico pelo ID.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a usuários com permissão de 'Admin'.
    /// </remarks>
    /// <param name="id">O ID (Guid) do usuário.</param>
    /// <returns>O objeto User.</returns>
    [HttpGet("{id:Guid}")]
    [Authorize(Policy = nameof(Role.Admin))]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get([FromRoute] Guid id)
    {
        try
        {
            var user = _accountRepository.GetById(id);

            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Cpf = user.Cpf,
                Email = user.Email,
                Role = user.Role
            };
            
            return Ok(responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }

    /// <summary>
    /// Cria um novo usuário com permissões específicas.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'. Permite definir o nível de permissão (Admin, Manager ou Donor).
    /// </remarks>
    /// <param name="userInput">Dados do novo usuário, incluindo permissão.</param>
    /// <returns>O usuário recém-criado.</returns>
    [HttpPost]
    [Authorize(Policy = nameof(Role.Admin))]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Post([FromBody] UserInput userInput)
    {
        try
        {
            var validationResult =  _validator.Validate(userInput);
            
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            
            var userGuid = ValidateUserToken(Role.Admin);
            
            
            
            if(userGuid == Guid.Empty)
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                return Unauthorized($"user: {userGuid} userRole: {userRole}");
            }
            
            
            var user = new User()
            {
                Name = userInput.Name,
                Email = userInput.Email,
                Cpf = CpfValidator.Normalize(userInput.Cpf),
                PasswordHash = "",
                Role = userInput.Role,
                CreatedBy = userGuid,
                IsActive = true
            };
            
            user.PasswordHash = _passwordHasher.HashPassword(user, userInput.Password);
            
            _accountRepository.Add(user);
            
            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Cpf = CpfValidator.Format(user.Cpf),
                Email = user.Email,
                Role = user.Role
            };
            
            return CreatedAtAction(nameof(Get), new { id = user.Id }, responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    
    private Guid ValidateUserToken(Role validateRole)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        var role = new Role();
            
        if(!string.IsNullOrEmpty(userRole))
        {
            role = Enum.Parse<Role>(userRole);
        }
            
        //var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        var userGuid = Guid.Empty;
            
        if(!string.IsNullOrEmpty(userId) || role == validateRole)
        {
            userGuid = Guid.Parse(userId);
        }
        
        return userGuid;
    }
    
    
    /// <summary>
    /// Registra um novo usuário na plataforma.
    /// </summary>
    /// <remarks>
    /// Endpoint público para auto-registro.<br/>
    /// Usuários criados por aqui sempre terão a permissão 'Donor'.
    /// </remarks>
    /// <param name="userDto">Dados básicos do novo usuário (nome, Cpf, email, senha).</param>
    /// <returns>O usuário recém-criado.</returns>
    [HttpPost("/auth/register")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterNewUser([FromBody] BaseUserDto userDto)
    {
        try
        {
            var validationResult =  _validator.Validate(userDto);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            var user = new User()
            {
                Name = userDto.Name,
                Email = userDto.Email,
                Cpf = userDto.Cpf,
                PasswordHash = "",
                Role = Role.Donor,
                IsActive = true
            };
            
            user.PasswordHash = _passwordHasher.HashPassword(user, userDto.Password);
            
            _accountRepository.Add(user);
            
            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Cpf = CpfValidator.Format(user.Cpf),
                Email = user.Email,
                Role = user.Role
            };
            
            // simula persistência
            var @event = new UserCreatedEvent(
                responseDto.Id,
                responseDto.Name,
                responseDto.Email
            );

            await _rabbitMq.PublishAsync(
                exchange: "users.events",
                routingKey: "user.created",
                message: @event
            );
            
            
            _logger.LogInformation($"Usuário {responseDto.Name} ({responseDto.Email}) criado com sucesso.");
            
            return CreatedAtAction(nameof(Get), new { id = user.Id }, responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }

        
    }
    
    /// <summary>
    /// Atualiza um usuário existente.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'.
    /// </remarks>
    /// <param name="userInput">Dados do usuário a ser atualizado.</param>
    /// <returns>Nenhum conteúdo.</returns>
    [HttpPut]
    [Authorize(Policy = nameof(Role.Admin))]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Put([FromBody] UpdateUserInput userInput)
    {
        try
        {
            var validationResult =  _validator.Validate(userInput);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            var user = _accountRepository.GetUserByCpf(userInput.Cpf);

            if (user == null)
            {
                return NotFound($"Usuário {userInput.Name} ({userInput.Email}) não encontrado.");
            }

            user.Name = userInput.Name;
            user.Email = userInput.Email;
            user.Cpf = userInput.Cpf;
            user.Role = userInput.Role;
            user.IsActive = userInput.IsActive;
            
            if (!string.IsNullOrEmpty(userInput.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, userInput.Password);
            }
            
            _accountRepository.Update(user);
            
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }

    /// <summary>
    /// Exclui um usuário pelo ID.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'.
    /// </remarks>
    /// <param name="id">O ID (Guid) do usuário a ser excluído.</param>
    /// <returns>Nenhum conteúdo.</returns>
    [HttpDelete("{id:Guid}")]
    [Authorize(Policy = nameof(Role.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Delete([FromRoute] Guid id)
    {
        try
        {
            var user = _accountRepository.GetById(id);
            
            if (user == null)
            {
                return NotFound($"Usuário Id: {id} não encontrado.");
            }

            _accountRepository.Delete(id);
            
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }


    //TODO remove
    private async void Teste()
    {
        var factory = new ConnectionFactory() { 
            HostName = "rabbitmq",
            UserName = "admin",
            Password = "admin"};
        
        factory.AutomaticRecoveryEnabled = true;

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        
        // await channel.QueueDeclareAsync(
        //     queue: "new-user-queue",
        //     durable: true,
        //     exclusive: false,
        //     autoDelete: false,
        //     arguments: null
        // );
        
        
        await channel.ExchangeDeclareAsync(
            exchange: "users.events",
            type: ExchangeType.Topic,
            durable: true
        );
        
        var message = "Hello RabbitMQ -novo USUARIO";
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "users.events",
            routingKey: "new-user-queue",
            body: body
        );
        
    }
}