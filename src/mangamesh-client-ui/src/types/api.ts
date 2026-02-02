export interface NodeStatus {
    nodeId: string;
    peerCount: number;
    seededManifests: number;
    storageUsedMb: number;
}

export interface Subscription {
    seriesId: string;
    autoFetchScanlators: string[]; // List of scanlator IDs enabled for auto-fetch
}

export interface ImportChapterRequest {
    seriesId: string;
    scanlatorId: string;
    language: string;
    chapterNumber: number;
    sourcePath: string;
    displayName: string;
    releaseType: string;
}

export interface StorageStats {
    totalMb: number;
    usedMb: number;
    manifestCount: number;
}

export interface ChapterSummary {
    manifestHash: string;
    seriesId: string;
    scanlatorId: string;
    language: string;
    chapterNumber: number;
    title?: string;
    uploadedAt: string; // ISO Date string
}

export interface ChapterMetadata {
    manifestHash: string; // Added for convenience in UI
    seriesId: string;
    scanlatorId: string;
    language: string;
    chapterNumber: number;
    pageCount: number;
    pages: string[]; // List of page identifiers/filenames
}

export interface SeriesSearchResult {
    seriesId: string;
    title: string;
    seedCount: number;
    chapterCount: number;
    lastUploadedAt: string; // ISO Date
}

export interface SeriesDetailsResponse {
    seriesId: string;
    title: string;
    description?: string;
    status?: string;
    year?: number;
}

export interface ChapterSummaryResponse {
    chapterId: string;
    chapterNumber: string;
    volume?: string;
    title?: string;
}

export interface ChapterManifest {
    manifestHash: string;
    language: string;
    scanGroup?: string;
    isVerified?: boolean;
    quality: string;
    uploadedAt: string;
}

export interface ChapterDetailsResponse {
    chapterId: string;
    seriesId: string;
    chapterNumber: string;
    title?: string;
    manifests: ChapterManifest[];
    pages: string[];
}

export interface ImportedChapter {
    seriesId: string;
    scanlatorId: string;
    language: string;
    chapterNumber: number;
    sourcePath: string;
    displayName: string;
    releaseType: string;
}

export interface MangaMetadata {
    source: number;
    externalMangaId: string;
    title: string;
    altTitles: string[];
    status: string;
    year: number;
}

export interface MangaChapter {
    chapterId: string;
    mangaId: string;
    source: number;
    chapterNumber: string;
    volume: string;
    title: string;
    language: string;
    publishDate: string;
}

export interface MangaDetails {
    mangaId: string;
    source: number;
    language: string;
    chapters: MangaChapter[];
}

export interface KeyPair {
    publicKeyBase64: string;
    privateKeyBase64?: string;
}

export interface KeyChallenge {
    challengeId: string;
    nonce: string;
    expiresAt: string;
}

export interface VerifySignatureResponse {
    valid: boolean;
}

export interface AnalyzedChapterDto {
    sourcePath: string;
    suggestedChapterNumber: number;
    fileCount: number;
}

export interface NodeStatus {
    nodeId: string;
    isConnected: boolean;
    lastPingUtc: string | null;
}

export interface NodeStatus {
    nodeId: string;
    isConnected: boolean;
    lastPingUtc: string | null;
}
