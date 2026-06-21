namespace StuffInABox.Application.Sharing;

/// <summary>The active share token for a space (the link the owner shares).</summary>
public sealed record InviteDto(string Token);

/// <summary>What a recipient sees before joining via a share link.</summary>
public sealed record InvitePreviewDto(Guid SpaceId, string SpaceName, bool AlreadyMember, bool IsOwner);

public sealed record AcceptInviteResult(Guid SpaceId, string SpaceName);

/// <summary>A member of a space. No PII is stored, so only the id + join time.</summary>
public sealed record MemberDto(Guid UserId, DateTimeOffset JoinedAt);
