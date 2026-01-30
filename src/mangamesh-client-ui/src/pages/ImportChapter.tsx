import { useState, useEffect } from 'react';
import { importChapter, getImportedChapters } from '../api/import';
import { searchSeries, getSeriesChapters } from '../api/series';
import { getKeys, requestChallenge, solveChallenge, verifySignature } from '../api/keys';
import type { ImportChapterRequest, SeriesSearchResult, ChapterSummaryResponse, KeyPair } from '../types/api';
import { debounce } from 'lodash';

export default function ImportChapter() {
    // Metadata (Series) search state
    const [metadataResults, setMetadataResults] = useState<SeriesSearchResult[]>([]);
    const [isSearchingMetadata, setIsSearchingMetadata] = useState(false);
    const [showMetadataDropdown, setShowMetadataDropdown] = useState(false);

    // Chapter selection state
    const [availableChapters, setAvailableChapters] = useState<ChapterSummaryResponse[]>([]);
    const [chapterInputValue, setChapterInputValue] = useState('');
    const [showChapterDropdown, setShowChapterDropdown] = useState(false);
    const [isLoadingChapters, setIsLoadingChapters] = useState(false);

    // Debounced search function
    const debouncedSearch = debounce(async (query: string) => {
        if (!query || query.length < 2) {
            setMetadataResults([]);
            return;
        }

        setIsSearchingMetadata(true);
        try {
            const results = await searchSeries(query);
            setMetadataResults(results);
            setShowMetadataDropdown(true);
        } catch (e) {
            console.error(e);
        } finally {
            setIsSearchingMetadata(false);
        }
    }, 500);

    // Handle input change
    const handleSeriesIdChange = (value: string) => {
        setForm({ ...form, seriesId: value });
        debouncedSearch(value);
    };

    const selectMetadata = async (meta: SeriesSearchResult) => {
        setForm({ ...form, seriesId: meta.title });
        setShowMetadataDropdown(false);

        // Fetch chapters for the selected series
        if (meta.seriesId) {
            setIsLoadingChapters(true);
            try {
                const chapters = await getSeriesChapters(meta.seriesId);
                // Sort chapters descending by chapter number (parsing potential non-numeric)
                const sorted = [...chapters].sort((a, b) => {
                    const numA = parseFloat(a.chapterNumber) || 0;
                    const numB = parseFloat(b.chapterNumber) || 0;
                    return numB - numA;
                });
                setAvailableChapters(sorted);
                setChapterInputValue(''); // Reset chapter input
                setForm(prev => ({ ...prev, chapterNumber: 0 }));
            } catch (e) {
                console.error("Failed to load chapters", e);
                // Optional: show error to user
            } finally {
                setIsLoadingChapters(false);
            }
        }
    };

    const handleChapterChange = (value: string) => {
        setChapterInputValue(value);
        const num = parseFloat(value);
        if (!isNaN(num)) {
            setForm({ ...form, chapterNumber: num });
        }
    };

    const selectChapter = (chapter: ChapterSummaryResponse) => {
        const displayValue = chapter.chapterNumber;
        setChapterInputValue(displayValue);
        setForm(prev => ({ ...prev, chapterNumber: parseFloat(chapter.chapterNumber) || 0 }));
        setShowChapterDropdown(false);
    };

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

    // Signature verification state
    const [keys, setKeys] = useState<KeyPair | null>(null);
    const [privateKeyInput, setPrivateKeyInput] = useState('');
    const [isVerifying, setIsVerifying] = useState(false);
    const [signatureStatus, setSignatureStatus] = useState<'idle' | 'success' | 'error'>('idle');
    const [verificationError, setVerificationError] = useState<string | null>(null);

    useEffect(() => {
        loadHistory();
        loadKeys();
    }, []);

    async function loadKeys() {
        try {
            const k = await getKeys();
            setKeys(k);
        } catch (e) {
            console.error('Failed to load keys', e);
        }
    }

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

        if (signatureStatus !== 'success') {
            setMessage({ type: 'error', text: 'Please verify your signature before importing' });
            return;
        }

        setSubmitting(true);
        setMessage(null);

        try {
            await importChapter({
                ...form,
                displayName: form.displayName || `${form.seriesId} Ch. ${form.chapterNumber}`,
                releaseType: form.releaseType || 'manual'
            });
            setMessage({ type: 'success', text: 'Chapter imported successfully' });
            setForm(prev => ({ ...prev, chapterNumber: prev.chapterNumber + 1 }));
            setChapterInputValue((form.chapterNumber + 1).toString());
            loadHistory();
        } catch (e) {
            setMessage({ type: 'error', text: 'Failed to import chapter' });
        } finally {
            setSubmitting(false);
        }
    }

    async function handleVerifySignature() {
        if (!keys?.publicKeyBase64) {
            setVerificationError('No public key found for this node');
            setSignatureStatus('error');
            return;
        }

        if (!privateKeyInput) {
            setVerificationError('Please enter your private key');
            setSignatureStatus('error');
            return;
        }

        setIsVerifying(true);
        setVerificationError(null);
        setSignatureStatus('idle');

        try {
            // Step 1: Request Challenge
            const challenge = await requestChallenge(keys.publicKeyBase64);

            // Step 2: Solve Challenge
            const signature = await solveChallenge(challenge.nonce, privateKeyInput);

            // Step 3: Verify Signature
            const result = await verifySignature(keys.publicKeyBase64, challenge.challengeId, signature);

            if (result.valid) {
                setSignatureStatus('success');
            } else {
                setSignatureStatus('error');
                setVerificationError('Signature verification failed');
            }
        } catch (e: any) {
            console.error('Signature verification error', e);
            setSignatureStatus('error');
            setVerificationError(e.message || 'Verification failed');
        } finally {
            setIsVerifying(false);
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

                {signatureStatus !== 'success' ? (
                    <div className="bg-white p-8 rounded-lg shadow-sm border border-gray-200">
                        <div className="mb-8 p-6 bg-blue-50 rounded-lg border border-blue-100">
                            <h2 className="text-lg font-semibold text-blue-900 mb-2">Verification Required</h2>
                            <p className="text-blue-800">
                                Importing on the MangaMesh network is currently restricted to approved signatures.
                                Please apply the <a href="https://discord.gg/mangamesh" className="underline font-bold" target="_blank" rel="noopener noreferrer">MangaMesh Discord</a> for access.
                            </p>
                        </div>

                        <div className="space-y-6">
                            <div>
                                <label className="block text-sm font-medium text-gray-700 mb-2">Enter Private Key (Base64)</label>
                                <div className="flex gap-3">
                                    <input
                                        type="password"
                                        className="flex-1 px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
                                        placeholder="Paste your node private key here..."
                                        value={privateKeyInput}
                                        onChange={e => {
                                            setPrivateKeyInput(e.target.value);
                                            setSignatureStatus('idle');
                                        }}
                                    />
                                    <button
                                        type="button"
                                        onClick={handleVerifySignature}
                                        disabled={isVerifying || !privateKeyInput}
                                        className="px-6 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
                                    >
                                        {isVerifying ? 'Verifying...' : 'Verify Signature'}
                                    </button>
                                </div>
                                {verificationError && (
                                    <p className="mt-2 text-sm text-red-600 font-medium">{verificationError}</p>
                                )}
                            </div>

                            <div className="pt-4 border-t border-gray-100">
                                <p className="text-xs text-gray-500 mb-1">Your Node Public Key:</p>
                                <code className="block p-2 bg-gray-50 rounded text-[10px] break-all text-gray-600 border border-gray-200">
                                    {keys?.publicKeyBase64 || 'Loading public key...'}
                                </code>
                            </div>
                        </div>
                    </div>
                ) : (
                    <>
                        <div className="bg-white p-8 rounded-lg shadow-sm border border-gray-200">
                            {message && (
                                <div className={`mb-6 p-4 rounded-md ${message.type === 'success' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
                                    {message.text}
                                </div>
                            )}

                            <form onSubmit={handleSubmit} className="space-y-6">
                                <div className="grid grid-cols-2 gap-6">
                                    <div className="col-span-2 relative">
                                        <label className="block text-sm font-medium text-gray-700 mb-1">Series ID / Metadata Search</label>
                                        <div className="relative">
                                            <input
                                                type="text"
                                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                                value={form.seriesId}
                                                onChange={e => handleSeriesIdChange(e.target.value)}
                                                onFocus={() => { if (metadataResults.length > 0) setShowMetadataDropdown(true); }}
                                                placeholder="Search for series metadata..."
                                                required
                                                autoComplete="off"
                                            />
                                            {isSearchingMetadata && (
                                                <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                                                    <div className="animate-spin h-4 w-4 border-2 border-blue-500 rounded-full border-t-transparent"></div>
                                                </div>
                                            )}
                                        </div>

                                        {showMetadataDropdown && metadataResults.length > 0 && (
                                            <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-md shadow-lg max-h-60 overflow-y-auto">
                                                {metadataResults.map((meta, idx) => (
                                                    <div
                                                        key={idx}
                                                        className="p-2 hover:bg-gray-100 cursor-pointer border-b border-gray-100 last:border-0"
                                                        onClick={() => selectMetadata(meta)}
                                                    >
                                                        <div className="font-medium text-gray-900">{meta.title}</div>
                                                        <div className="text-xs text-gray-500 flex justify-between">
                                                            <span>Seeds: {meta.seedCount} • Chapters: {meta.chapterCount}</span>
                                                            <span className="bg-gray-100 px-1 rounded text-gray-600">ID: {meta.seriesId}</span>
                                                        </div>
                                                    </div>
                                                ))}
                                                <div
                                                    className="p-2 bg-gray-50 text-center text-xs text-blue-600 cursor-pointer hover:bg-gray-100 border-t border-gray-200"
                                                    onClick={() => setShowMetadataDropdown(false)}
                                                >
                                                    Close Search
                                                </div>
                                            </div>
                                        )}
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



                                    <div className="relative">
                                        <label className="block text-sm font-medium text-gray-700 mb-1">Chapter Number</label>
                                        <input
                                            type="text"
                                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                                            value={chapterInputValue}
                                            onChange={e => handleChapterChange(e.target.value)}
                                            onFocus={() => { if (availableChapters.length > 0) setShowChapterDropdown(true); }}
                                            placeholder="Select or type chapter number"
                                            required
                                            autoComplete="off"
                                        />
                                        {isLoadingChapters && (
                                            <div className="absolute right-3 top-9">
                                                <div className="animate-spin h-4 w-4 border-2 border-blue-500 rounded-full border-t-transparent"></div>
                                            </div>
                                        )}

                                        {showChapterDropdown && availableChapters.length > 0 && (
                                            <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-md shadow-lg max-h-60 overflow-y-auto">
                                                {availableChapters
                                                    .filter(ch => ch.chapterNumber.includes(chapterInputValue))
                                                    .map((ch) => (
                                                        <div
                                                            key={ch.chapterId}
                                                            className="p-3 hover:bg-gray-50 cursor-pointer flex items-center justify-between border-b border-gray-100 last:border-0"
                                                            onClick={() => selectChapter(ch)}
                                                        >
                                                            <span className="font-medium">Ch. {ch.chapterNumber}</span>
                                                        </div>
                                                    ))}
                                                <div
                                                    className="p-2 bg-gray-50 text-center text-xs text-blue-600 cursor-pointer hover:bg-gray-100 border-t border-gray-200"
                                                    onClick={() => setShowChapterDropdown(false)}
                                                >
                                                    Close List
                                                </div>
                                            </div>
                                        )}
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

                                <div className="pt-4 flex items-center justify-between border-t border-gray-100">
                                    <div className="flex items-center text-xs text-green-600 font-medium">
                                        <svg className="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                                        </svg>
                                        Signature Verified
                                    </div>
                                    <button
                                        type="submit"
                                        disabled={submitting}
                                        className="px-8 py-2 bg-blue-600 text-white font-medium rounded-md hover:bg-blue-700 transition-colors disabled:opacity-50"
                                    >
                                        {submitting ? 'Importing...' : 'Start Import'}
                                    </button>
                                </div>
                            </form>
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
                    </>
                )}
            </div>
        </div>
    );
}
