import { useState, useEffect } from 'react';
import { importChapter, getImportedChapters } from '../api/import';
import type { ImportChapterRequest } from '../types/api';

export default function ImportChapter() {
    const [form, setForm] = useState<ImportChapterRequest>({
        seriesId: '',
        scanlatorId: '',
        language: '',
        chapterNumber: 0,
        sourcePath: '',
        displayName: '',
        releaseType: 'manual'
    });
    const [submitting, setSubmitting] = useState(false);
    const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);
    const [history, setHistory] = useState<import('../types/api').ImportedChapter[]>([]);
    const [search, setSearch] = useState('');

    useEffect(() => {
        loadHistory();
    }, []);

    async function loadHistory() {
        try {
            const data = await getImportedChapters();
            setHistory(data);
        } catch (e) {
            console.error('Failed to load history', e);
        }
    }

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setMessage(null);

        try {
            await importChapter(form);
            setMessage({ type: 'success', text: 'Chapter imported successfully' });
            // Reset numeric field but maybe keep others?
            setForm(prev => ({ ...prev, chapterNumber: prev.chapterNumber + 1 }));
            loadHistory();
        } catch (e) {
            setMessage({ type: 'error', text: 'Failed to import chapter' });
        } finally {
            setSubmitting(false);
        }
    }

    const filteredHistory = history.filter(item =>
        item.seriesId.toLowerCase().includes(search.toLowerCase()) ||
        item.displayName.toLowerCase().includes(search.toLowerCase())
    );

    return (
        <div className="max-w-4xl mx-auto space-y-8">
            <div>
                <h1 className="text-2xl font-bold text-gray-900 mb-6">Import Chapter</h1>
                <div className="bg-white p-8 rounded-lg shadow-sm border border-gray-200">
                    {message && (
                        <div className={`mb-6 p-4 rounded-md ${message.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
                            {message.text}
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="space-y-6">
                        <div className="grid grid-cols-2 gap-6">
                            <div className="col-span-2">
                                <label className="block text-sm font-medium text-gray-700 mb-1">Series ID</label>
                                <input
                                    type="text"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.seriesId}
                                    onChange={e => setForm({ ...form, seriesId: e.target.value })}
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Scanlator ID</label>
                                <input
                                    type="text"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.scanlatorId}
                                    onChange={e => setForm({ ...form, scanlatorId: e.target.value })}
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Language</label>
                                <input
                                    type="text"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.language}
                                    onChange={e => setForm({ ...form, language: e.target.value })}
                                    required
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Display Name (Optional)</label>
                                <input
                                    type="text"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.displayName}
                                    placeholder={`${form.seriesId} ${form.chapterNumber}`.trim()}
                                    onChange={e => setForm({ ...form, displayName: e.target.value })}
                                />
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Release Type</label>
                                <select
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.releaseType}
                                    onChange={e => setForm({ ...form, releaseType: e.target.value as 'manual' | 'automatic' })}
                                    required
                                >
                                    <option value="manual">Manual</option>
                                    <option value="automatic">Automatic</option>
                                </select>
                            </div>

                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-1">Chapter Number</label>
                                <input
                                    type="number"
                                    step="0.1"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                    value={form.chapterNumber}
                                    onChange={e => setForm({ ...form, chapterNumber: parseFloat(e.target.value) })}
                                    required
                                />
                            </div>

                            <div className="col-span-2">
                                <label className="block text-sm font-medium text-gray-700 mb-1">Source Folder Path</label>
                                <input
                                    type="text"
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500 font-mono text-sm"
                                    placeholder="/path/to/chapter/folder"
                                    value={form.sourcePath}
                                    onChange={e => setForm({ ...form, sourcePath: e.target.value })}
                                    required
                                />
                                <p className="mt-1 text-xs text-gray-500">Absolute path to the directory containing chapter images.</p>
                            </div>
                        </div>

                        <div className="pt-4">
                            <button
                                type="submit"
                                disabled={submitting}
                                className="w-full px-4 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 transition-colors disabled:opacity-50"
                            >
                                {submitting ? 'Importing...' : 'Start Import'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>

            <div>
                <h2 className="text-xl font-bold text-gray-900 mb-4">Import History</h2>
                <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
                    <div className="p-4 border-b border-gray-200 bg-gray-50">
                        <input
                            type="text"
                            placeholder="Filter imports..."
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500 text-sm"
                            value={search}
                            onChange={e => setSearch(e.target.value)}
                        />
                    </div>

                    <div className="divide-y divide-gray-200">
                        {filteredHistory.length === 0 ? (
                            <div className="p-8 text-center text-gray-500">
                                No imports found matching your search.
                            </div>
                        ) : (
                            filteredHistory.map((item, i) => (
                                <div key={i} className="p-4 hover:bg-gray-50 transition-colors">
                                    <div className="flex justify-between items-start">
                                        <div>
                                            <h3 className="font-medium text-gray-900">{item.displayName}</h3>
                                            <p className="text-sm text-gray-600 mt-1">
                                                {item.seriesId} • {item.scanlatorId} • {item.language.toUpperCase()}
                                            </p>
                                            <p className="text-xs text-gray-400 mt-1 font-mono break-all">
                                                {item.sourcePath}
                                            </p>
                                        </div>
                                        <div className="flex flex-col items-end">
                                            <span className={`px-2 py-0.5 text-xs rounded-full ${item.releaseType === 'manual'
                                                ? 'bg-blue-100 text-blue-800'
                                                : 'bg-purple-100 text-purple-800'
                                                }`}>
                                                {item.releaseType}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
