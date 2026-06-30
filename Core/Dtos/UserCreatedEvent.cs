namespace Core.Dtos;

public record UserCreatedEvent
(
    Guid UserId,
    string Nome,
    string Email
);