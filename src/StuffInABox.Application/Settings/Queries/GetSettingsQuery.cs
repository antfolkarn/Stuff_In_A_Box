using MediatR;

namespace StuffInABox.Application.Settings.Queries;

public sealed record GetSettingsQuery : IRequest<SettingsDto>;
