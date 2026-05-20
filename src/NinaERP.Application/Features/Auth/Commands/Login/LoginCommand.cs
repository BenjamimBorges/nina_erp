using MediatR;
using NinaERP.Contracts.Responses;

namespace NinaERP.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;
