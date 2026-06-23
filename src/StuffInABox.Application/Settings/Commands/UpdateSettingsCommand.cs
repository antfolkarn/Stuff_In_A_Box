using MediatR;

namespace StuffInABox.Application.Settings.Commands;

public sealed record UpdateSettingsCommand(string Theme, string Design, string? DisplayName = null) : IRequest<SettingsDto>;
