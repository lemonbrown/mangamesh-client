import { useEffect, useState } from 'react';
import { getStorageStats } from '../api/storage';
import type { StorageStats } from '../types/api';
import StorageBar from '../components/StorageBar';

export default function Storage() {
    const [stats, setStats] = useState<StorageStats | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        async function load() {
            try {
                const data = await getStorageStats();
                setStats(data);
            } catch (e) {
                setError('Failed to load storage stats');
            } finally {
                setLoading(false);
            }
        }
        load();
    }, []);

    if (loading) return <div className="text-gray-500">Loading...</div>;
    if (error) return <div className="text-red-500">{error}</div>;
    if (!stats) return null;

    return (
        <div className="space-y-6">
            <h1 className="text-2xl font-bold text-gray-900">Storage</h1>

            <div className="bg-white p-8 rounded-lg shadow-sm border border-gray-200">
                <div className="mb-8">
                    <StorageBar usedMb={stats.usedMb} totalMb={stats.totalMb} />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-6 border-t border-gray-100">
                    <div>
                        <div className="text-sm font-medium text-gray-500">Total Capacity</div>
                        <div className="mt-1 text-2xl font-mono text-gray-900">{(stats.totalMb / 1024).toFixed(2)} GB</div>
                    </div>
                    <div>
                        <div className="text-sm font-medium text-gray-500">Used Space</div>
                        <div className="mt-1 text-2xl font-mono text-gray-900">{(stats.usedMb / 1024).toFixed(2)} GB</div>
                    </div>
                    <div>
                        <div className="text-sm font-medium text-gray-500">Manifests</div>
                        <div className="mt-1 text-2xl font-mono text-gray-900">{stats.manifestCount}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}
