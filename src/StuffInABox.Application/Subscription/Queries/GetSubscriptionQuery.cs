using MediatR;

namespace StuffInABox.Application.Subscription.Queries;

/// <summary>The signed-in user's current plan, their usage against its limits, and the
/// full tier list (for the "upgrade" comparison in Settings).</summary>
public sealed record GetSubscriptionQuery : IRequest<SubscriptionDto>;
