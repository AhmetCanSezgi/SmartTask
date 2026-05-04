using FluentValidation;
using MediatR;
using SmartTask.Application.Common.Interfaces;
using SmartTask.Application.DTOs;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Exceptions;

namespace SmartTask.Application.Features.Auth.Commands;

// ── REGISTER ────────────────────────────────────────────────────────────────

public record RegisterCommand(string Username, string Email, string Password) : IRequest<Result<AuthDto>>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _password;
    private readonly IJwtService _jwt;

    public RegisterHandler(IUnitOfWork uow, IPasswordService password, IJwtService jwt)
        => (_uow, _password, _jwt) = (uow, password, jwt);

    public async Task<Result<AuthDto>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await _uow.Repository<User>()
            .ExistsAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (exists)
            return Result<AuthDto>.Failure("Email is already in use.");

        var user = User.Create(request.Username, request.Email, _password.Hash(request.Password));
        await _uow.Repository<User>().AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<AuthDto>.Success(new AuthDto(
            _jwt.GenerateAccessToken(user),
            _jwt.GenerateRefreshToken(),
            user.Username));
    }
}

// ── LOGIN ────────────────────────────────────────────────────────────────────

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthDto>>;

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _password;
    private readonly IJwtService _jwt;

    public LoginHandler(IUnitOfWork uow, IPasswordService password, IJwtService jwt)
        => (_uow, _password, _jwt) = (uow, password, jwt);

    public async Task<Result<AuthDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        var users = await _uow.Repository<User>()
            .FindAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);
        var user = users.FirstOrDefault();

        if (user is null || !_password.Verify(request.Password, user.PasswordHash))
            return Result<AuthDto>.Failure("Invalid email or password.");

        return Result<AuthDto>.Success(new AuthDto(
            _jwt.GenerateAccessToken(user),
            _jwt.GenerateRefreshToken(),
            user.Username));
    }
}
