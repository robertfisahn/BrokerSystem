import apiClient from '../../../api/apiClient';

export interface ClientListItem {
    clientId: number;
    firstName: string | null;
    lastName: string | null;
    companyName: string | null;
    clientType: string | null;
    primaryContact: string | null;
    city: string | null;
    activePoliciesCount: number;
}

export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

export interface GetClientsParams {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDescending?: boolean;
}

export interface ClientsStats {
    totalClients: number;
    vipClients: number;
    corporateClients: number;
    activePoliciesTotal: number;
    newClientsThisMonth: number;
}

export interface Client360 {
    clientId: number;
    firstName: string | null;
    lastName: string;
    companyName?: string;
    taxId?: string;
    registrationDate: string;
    clientType: string | null;
    contacts: Client360Contact[];
    addresses: Client360Address[];
    policies: Client360Policy[];
}

export interface Client360Contact {
    contactType: string | null;
    contactValue: string | null;
    isPrimary: boolean;
}

export interface Client360Address {
    street: string | null;
    city: string | null;
    postalCode: string | null;
    country: string | null;
    isCurrent: boolean;
}

export interface Client360Policy {
    policyId: number;
    policyNumber: string | null;
    policyType: string | null;
    status: string | null;
    premiumAmount: number;
    startDate: string;
    endDate: string;
    claims: Client360Claim[];
}

export interface Client360Claim {
    claimId: number;
    claimNumber: string;
    status: string;
    approvedAmount?: number;
    incidentDate: string;
}

export const getClients = async (params: GetClientsParams = {}): Promise<PaginatedResult<ClientListItem>> => {
    const response = await apiClient.get<PaginatedResult<ClientListItem>>('/clients', { params });
    return response.data;
};

export const getClientsStats = async (): Promise<ClientsStats> => {
    const response = await apiClient.get<ClientsStats>('/clients/stats');
    return response.data;
};

export const getClient360 = async (id: number): Promise<Client360> => {
    const response = await apiClient.get<Client360>(`/clients/${id}/360`);
    return response.data;
};
