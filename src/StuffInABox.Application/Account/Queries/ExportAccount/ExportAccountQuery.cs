using MediatR;

namespace StuffInABox.Application.Account.Queries.ExportAccount;

/// <summary>GDPR data portability: returns everything the app stores about the user.</summary>
public sealed record ExportAccountQuery : IRequest<AccountExport>;

public sealed record AccountExport(
    AccountInfo Account,
    SettingsExport? Settings,
    IReadOnlyList<SpaceExport> Spaces,
    IReadOnlyList<MembershipExport> SharedWithMe,
    DateTimeOffset ExportedAt);

public sealed record AccountInfo(Guid UserId, string Provider, string? Email, DateTimeOffset CreatedAt);

public sealed record SettingsExport(string Theme, string Design);

public sealed record SpaceExport(string Name, string Code, string Icon, IReadOnlyList<BoxExport> Boxes);

public sealed record BoxExport(int Number, string Label, IReadOnlyList<ItemExport> Items);

public sealed record ItemExport(string Name, IReadOnlyList<string> Tags, bool HasPhoto);

public sealed record MembershipExport(Guid SpaceId, string SpaceName, DateTimeOffset JoinedAt);
