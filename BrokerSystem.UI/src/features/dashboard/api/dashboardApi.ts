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

// Agent Dashboard
export interface AgentStats {
    totalClients: number;
    activePolicies: number;
    totalPremium: number;
}

export interface ExpiringPolicy {
    policyId: number;
    policyNumber: string;
    clientName: string;
    endDate: string;
    daysLeft: number;
}

export interface RecentActivity {
    type: string;
    description: string;
    createdAt: string;
}

export interface AgentDashboardResponse {
    stats: AgentStats;
    expiringPolicies: ExpiringPolicy[];
    recentActivities: RecentActivity[];
}

export const getAgentDashboard = async (): Promise<AgentDashboardResponse> => {
    const response = await apiClient.get<AgentDashboardResponse>('/dashboard/agent');
    return response.data;
};
