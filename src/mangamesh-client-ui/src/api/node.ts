import type { NodeStatus } from '../types/api';

const API_BASE_URL = 'https://localhost:7124'; // Client API port

export async function getNodeStatus(): Promise<NodeStatus> {
    const response = await fetch(`${API_BASE_URL}/api/node/status`);
    if (!response.ok) {
        throw new Error('Failed to fetch node status');
    }
    return await response.json();
}
