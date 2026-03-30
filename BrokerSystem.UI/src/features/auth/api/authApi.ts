import apiClient from "../../../api/apiClient";

export interface LoginResponse {
    token: string;
    expiresAt: string;
    displayName: string;
    role: string;
    agentId?: number;
}

export const authApi = {
    login: async (username: string, password: string): Promise<LoginResponse> => {
        const response = await apiClient.post<LoginResponse>('/auth/login', {
            username,
            password
        });
        return response.data;
    },

    refresh: async (): Promise<LoginResponse> => {
        const response = await apiClient.post<LoginResponse>('/auth/refresh');
        return response.data;
    },

    logout: async (): Promise<void> => {
        await apiClient.post('/auth/logout');
    }
};
