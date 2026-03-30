/**
 * Shared API response types used across multiple features.
 * This file provides a single source of truth for common data structures
 * returned by the BrokerSystem backend.
 */

export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}
