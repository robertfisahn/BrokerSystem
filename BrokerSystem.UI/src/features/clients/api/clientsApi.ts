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

export const getClients = async (params: GetClientsParams = {}): Promise<PaginatedResult<ClientListItem>> => {
    const response = await apiClient.get<PaginatedResult<ClientListItem>>('/clients', { params });
    return response.data;
};

export const getClientsStats = async (): Promise<ClientsStats> => {
    const response = await apiClient.get<ClientsStats>('/clients/stats');
    return response.data;
};
