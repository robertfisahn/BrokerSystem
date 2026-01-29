import apiClient from '../../../api/apiClient';

export interface PolicyListItem {
    policyId: number;
    policyNumber: string;
    clientName: string;
    policyType: string;
    totalPremium: number;
    startDate: string;
    endDate: string;
    status: string;
}

export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}

export interface GetPoliciesParams {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDescending?: boolean;
}

export const getPolicies = async (params: GetPoliciesParams): Promise<PaginatedResult<PolicyListItem>> => {
    const response = await apiClient.get<PaginatedResult<PolicyListItem>>('/policies', { params });
    return response.data;
};

