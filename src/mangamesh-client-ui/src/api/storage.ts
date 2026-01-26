import type { StorageStats } from '../types/api';
import { mockApi } from './mock';

export async function getStorageStats(): Promise<StorageStats> {
    return mockApi.getStorageStats();
}
