import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getChapters } from '../api/chapters';
import { getSubscriptions, updateSubscription } from '../api/subscriptions';
import type { ChapterSummary, Subscription } from '../types/api';

export default function SeriesDetails() {
    const { seriesId } = useParams<{ seriesId: string }>();
    const [chapters, setChapters] = useState<ChapterSummary[]>([]);
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        async function load() {
            if (!seriesId) return;
            try {
                const [chapterData, subs] = await Promise.all([
                    getChapters(seriesId),
                    getSubscriptions()
                ]);
                setChapters(chapterData);
                const sub = subs.find(s => s.seriesId === seriesId);
                setSubscription(sub || null);
            } catch (e) {
                setError('Failed to load series data');
            } finally {
                setLoading(false);
            }
        }
        load();
    }, [seriesId]);

    async function toggleAutoFetch(scanlatorId: string) {
        if (!subscription) return;

        const current = new Set(subscription.autoFetchScanlators);
        if (current.has(scanlatorId)) {
            current.delete(scanlatorId);
        } else {
            current.add(scanlatorId);
        }

        const newAutoFetch = Array.from(current);

        // Optimistic update
        const updatedSub = { ...subscription, autoFetchScanlators: newAutoFetch };
        setSubscription(updatedSub);

        try {
            await updateSubscription(subscription.seriesId, newAutoFetch);
        } catch (e) {
            alert('Failed to update subscription settings');
            // Revert on error could be implemented here
        }
    }

    if (loading) return <div className="p-8 text-gray-500">Loading chapters...</div>;
    if (error) return <div className="p-8 text-red-500">{error}</div>;

    // Derive unique scanlators from chapters
    const scanlators = Array.from(new Set(chapters.map(c => c.scanlatorId))).sort();

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">{seriesId}</h1>
                    <div className="text-sm text-gray-500 mt-1">
                        {chapters.length} chapters available
                    </div>
                </div>
                <Link to="/subscriptions" className="text-blue-600 hover:underline">
                    Back to Subscriptions
                </Link>
            </div>

            {/* Subscription Settings */}
            {subscription && (
                <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                    <h3 className="font-medium text-blue-900 mb-2">Auto-Fetch Settings</h3>
                    <div className="flex gap-4 flex-wrap">
                        {scanlators.map(scanlator => (
                            <label key={scanlator} className="flex items-center space-x-2 cursor-pointer">
                                <input
                                    type="checkbox"
                                    checked={subscription.autoFetchScanlators.includes(scanlator)}
                                    onChange={() => toggleAutoFetch(scanlator)}
                                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                />
                                <span className="text-sm text-gray-700">{scanlator}</span>
                            </label>
                        ))}
                        {scanlators.length === 0 && <span className="text-sm text-gray-500">No scanlators found to configure.</span>}
                    </div>
                </div>
            )}

            <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
                {chapters.length === 0 ? (
                    <div className="p-8 text-center text-gray-500">No chapters found.</div>
                ) : (
                    <div className="divide-y divide-gray-100">
                        {chapters.map((chapter) => (
                            <Link
                                key={chapter.manifestHash}
                                to={`/read/${chapter.manifestHash}`}
                                className="block p-4 hover:bg-gray-50 transition-colors flex justify-between items-center"
                            >
                                <div>
                                    <div className="font-medium text-gray-900">
                                        Chapter {chapter.chapterNumber}
                                        {chapter.title && <span className="text-gray-500 font-normal ml-2">- {chapter.title}</span>}
                                    </div>
                                    <div className="text-xs text-gray-400 mt-1 space-x-2">
                                        <span>{chapter.scanlatorId}</span>
                                        <span>•</span>
                                        <span>{new Date(chapter.uploadedAt).toLocaleDateString()}</span>
                                    </div>
                                </div>
                                <span className="text-gray-400 text-sm font-mono">
                                    {chapter.manifestHash.substring(0, 8)}...
                                </span>
                            </Link>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
