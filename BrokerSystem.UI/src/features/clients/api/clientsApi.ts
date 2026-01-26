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

export const getClients = async (): Promise<ClientListItem[]> => {
    const response = await apiClient.get<ClientListItem[]>('/clients');
    return response.data;
};
