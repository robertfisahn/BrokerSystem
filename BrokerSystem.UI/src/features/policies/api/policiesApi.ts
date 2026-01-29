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

export interface CreatePolicyData {
    policyNumber: string;
    clientId: number;
    policyTypeId: number;
    agentId: number;
    premiumAmount: number;
    sumInsured: number;
    startDate: string;
    endDate: string;
    paymentFrequency: string;
}

export interface LookupDto {
    id: number;
    name: string;
}

export interface PolicyLookups {
    clients: LookupDto[];
    policyTypes: LookupDto[];
    agents: LookupDto[];
}

export const createPolicy = async (data: CreatePolicyData): Promise<number> => {
    const response = await apiClient.post<number>('/policies', data);
    return response.data;
};

export const getPolicyLookups = async (): Promise<PolicyLookups> => {
    const response = await apiClient.get<PolicyLookups>('/policies/lookups');
    return response.data;
};

