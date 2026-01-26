import { useState } from 'react';
import { importChapter } from '../api/import';
import type { ImportChapterRequest } from '../types/api';

export default function ImportChapter() {
    const [form, setForm] = useState<ImportChapterRequest>({
        seriesId: '',
        scanlatorId: '',
        language: '',
        chapterNumber: 0,
        sourcePath: ''
    });
    const [submitting, setSubmitting] = useState(false);
    const [message, setMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setSubmitting(true);
        setMessage(null);

        try {
            await importChapter(form);
            setMessage({ type: 'success', text: 'Chapter imported successfully' });
            // Reset numeric field but maybe keep others?
            setForm(prev => ({ ...prev, chapterNumber: prev.chapterNumber + 1 }));
        } catch (e) {
            setMessage({ type: 'error', text: 'Failed to import chapter' });
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <div className="max-w-2xl mx-auto">
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
    );
}
