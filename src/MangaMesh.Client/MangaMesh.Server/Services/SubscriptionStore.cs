using MangaMesh.Client.Services;
using MangaMesh.Server.Models;
using MangaMesh.Server.Stores;

namespace MangaMesh.Server.Services
{
    public class SubscriptionStore : ISubscriptionStore
    {
        private readonly ReplicationService _replicationService;

        public SubscriptionStore(ReplicationService replicationService)
        {
            _replicationService = replicationService;
        }

        public Task<IReadOnlyList<SubscriptionDto>> GetAllAsync(CancellationToken ct = default)
        {
            var subs = _replicationService.GetSubscriptions()
                .Select(s => new SubscriptionDto(s.SeriesId, s.Language))
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<SubscriptionDto>>(subs);
        }

        public Task<bool> AddAsync(SubscriptionDto subscription, CancellationToken ct = default)
        {
            var added = _replicationService.SubscribeToReleaseLine(
                subscription.SeriesId,
                subscription.Language
            );

            return Task.FromResult(added);
        }

        public Task<bool> RemoveAsync(SubscriptionDto subscription, CancellationToken ct = default)
        {
            var removed = _replicationService.UnsubscribeFromReleaseLine(
                subscription.SeriesId,
                subscription.Language
            );

            return Task.FromResult(removed);
        }
    }

}
