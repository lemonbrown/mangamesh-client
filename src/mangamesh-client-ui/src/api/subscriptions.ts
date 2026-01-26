import type { Subscription, SeriesSearchResult } from '../types/api';
import { mockApi } from './mock';

export async function getSubscriptions(): Promise<Subscription[]> {
    return mockApi.getSubscriptions();
}

export async function addSubscription(subscription: Subscription): Promise<void> {
    return mockApi.addSubscription(subscription);
}

export async function removeSubscription(subscription: Subscription): Promise<void> {
    return mockApi.removeSubscription(subscription);
}

export async function updateSubscription(seriesId: string, autoFetchScanlators: string[]): Promise<void> {
    return mockApi.updateSubscription(seriesId, autoFetchScanlators);
}

export async function searchSeries(query: string): Promise<SeriesSearchResult[]> {
    return mockApi.searchSeries(query);
}
