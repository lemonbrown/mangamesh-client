import type { ImportChapterRequest } from '../types/api';
import { mockApi } from './mock';

export async function importChapter(request: ImportChapterRequest): Promise<void> {
    console.log('Mock importing chapter:', request);
    return mockApi.importChapter();
}
