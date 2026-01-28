import type { ImportChapterRequest, ImportedChapter } from '../types/api';


export async function importChapter(request: ImportChapterRequest): Promise<void> {
    const response = await fetch('/api/import/chapter', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        throw new Error(`Import failed: ${response.statusText}`);
    }
}

export async function getImportedChapters(): Promise<ImportedChapter[]> {
    const response = await fetch('/api/import/chapters');

    if (!response.ok) {
        throw new Error(`Failed to fetch history: ${response.statusText}`);
    }

    return await response.json();
}
