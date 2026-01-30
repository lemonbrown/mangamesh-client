import { useState } from 'react';
import { Link } from 'react-router-dom';
import { searchSeries } from '../api/series';
import type { SeriesSearchResult } from '../types/api';

export default function Series() {
    const [results, setResults] = useState<SeriesSearchResult[]>([]);
    const [loading, setLoading] = useState(false);
    const [query, setQuery] = useState('');

    async function handleSearch(q: string) {
        setQuery(q);
        if (!q) {
            setResults([]);
            return;
        }

        setLoading(true);
        try {
            const data = await searchSeries(q);
            setResults(data);
        } catch (e) {
            console.error(e);
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="space-y-8">
            <div>
                <h1 className="text-2xl font-bold text-gray-900 mb-4">Search Series</h1>

                <div className="bg-white p-6 rounded-lg shadow-sm border border-gray-200">
                    <div className="relative flex-1">
                        <input
                            type="text"
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                            placeholder="Search by series name or ID..."
                            value={query}
                            onChange={(e) => handleSearch(e.target.value)}
                        />
                        {loading && (
                            <div className="absolute right-3 top-2.5 text-gray-400">
                                <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <div>
                <h2 className="text-lg font-medium text-gray-900 mb-4">Search Results</h2>
                {loading && <div className="text-gray-500">Loading...</div>}

                {!loading && query && results.length === 0 && (
                    <div className="text-gray-500 italic">No series found.</div>
                )}

                <div className="grid gap-4">
                    {results.map((series) => (
                        <div key={series.seriesId} className="flex items-center justify-between bg-white p-4 rounded-lg shadow-sm border border-gray-200">
                            <div className="flex-1">
                                <div className="font-medium text-lg">
                                    <Link
                                        to={`/series/${series.seriesId}`}
                                        className="text-blue-600 hover:underline"
                                    >
                                        {series.title}
                                    </Link>
                                </div>
                                <div className="text-xs text-gray-500 font-mono mt-0.5">ID: {series.seriesId}</div>
                                <div className="text-sm text-gray-500 mt-1">
                                    Last updated: {new Date(series.lastUploadedAt).toLocaleDateString()}
                                </div>
                            </div>
                            <div className="text-right text-sm text-gray-500">
                                <div><span className="font-semibold text-green-600">{series.seedCount}</span> seeds</div>
                                <div>{series.chapterCount} chapters</div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
