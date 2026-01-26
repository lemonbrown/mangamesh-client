import type { NodeStatus } from '../types/api';
import { mockApi } from './mock';

export async function getNodeStatus(): Promise<NodeStatus> {
    // Return mock data for now
    return mockApi.getNodeStatus();

    /* Real implementation commented out
    const response = await fetch('/api/node/status');
    if (!response.ok) {
        throw new Error('Failed to fetch node status');
    }
    return response.json();
    */
}
