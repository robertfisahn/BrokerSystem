import apiClient from '../../../api/apiClient';

export interface MonthlySales {
    month: string;
    totalPremium: number;
    policyCount: number;
}

export interface ClientTypeDistribution {
    clientType: string;
    clientCount: number;
}

export interface PolicyStatusDistribution {
    policyStatus: string;
    policyCount: number;
}

export interface DashboardKpis {
    totalClients: number;
    totalPolicies: number;
    activeClaims: number;
    totalPremiumVolume: number;
}

export interface DashboardStatsResponse {
    monthlySales: MonthlySales[];
    clientTypeDistribution: ClientTypeDistribution[];
    policyStatusDistribution: PolicyStatusDistribution[];
    kpis: DashboardKpis;
}

export const getDashboardStats = async (): Promise<DashboardStatsResponse> => {
    const response = await apiClient.get<DashboardStatsResponse>('/dashboard/stats');
    return response.data;
};
