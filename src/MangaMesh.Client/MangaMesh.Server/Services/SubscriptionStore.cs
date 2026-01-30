using MangaMesh.Client.Abstractions;
using MangaMesh.Client.Models;
using MangaMesh.Client.Services;
using MangaMesh.Server.Models;
using MangaMesh.Shared.Stores;

namespace MangaMesh.Server.Services
{
    public class SubscriptionStore : ISubscriptionStore
    {


        public async Task<IReadOnlyList<SeriesSubscription>> GetAllAsync(CancellationToken ct = default)
        {

            var subs =
                (await JsonFileStore.LoadAsync<SeriesSubscription>(AppContext.BaseDirectory + "\\data\\subscriptions.json"))
                    .ToList()
                    .AsReadOnly();

            return subs;
        }

        public async Task AddAsync(SeriesSubscription subscription, CancellationToken ct = default)
        {
            var subs =
                    (await JsonFileStore.LoadAsync<SeriesSubscription>(AppContext.BaseDirectory + "\\data\\subscriptions.json"))
                        .ToList();

            var sub = new SeriesSubscription() { Language = subscription.Language, SeriesId =  subscription.SeriesId };

            subs.Add(sub);

            await JsonFileStore.SaveAsync(AppContext.BaseDirectory + "\\data\\subscriptions.json", subs);
        }

        public async Task RemoveAsync(SeriesSubscription subscription, CancellationToken ct = default)
        {
            var subs =
                  (await JsonFileStore.LoadAsync<SeriesSubscription>(AppContext.BaseDirectory + "\\data\\subscriptions.json"))
                      .ToList();

            subs = subs.Where(n => n.SeriesId != subscription.SeriesId).ToList();

            await JsonFileStore.SaveAsync(AppContext.BaseDirectory + "\\data\\subscriptions.json", subs);
        }

        public Task<bool> ExistsAsync(SeriesSubscription releaseLine, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

}
