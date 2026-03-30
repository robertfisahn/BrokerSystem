import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { notifications } from '@mantine/notifications';

const apiClient = axios.create({
    baseURL: 'http://localhost:5199/api',
    withCredentials: true, // Crucial for sending/receiving httpOnly cookies
    headers: {
        'Content-Type': 'application/json',
    },
});

interface FailedRequest {
    resolve: (token: string | null) => void;
    reject: (error: unknown) => void;
}

let isRefreshing = false;
let failedQueue: FailedRequest[] = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token);
        }
    });
    failedQueue = [];
};

// Request Interceptor: Add JWT Token
apiClient.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('broker_system_token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Response Interceptor: Error Handling & Refresh Logic
apiClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        if (!originalRequest) {
            return Promise.reject(error);
        }

        // If 401 Unauthorized and not already retrying
        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                    .then(token => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return apiClient(originalRequest);
                    })
                    .catch(err => Promise.reject(err));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                // Try to refresh the token
                const response = await axios.post('http://localhost:5199/api/auth/refresh', {}, {
                    withCredentials: true
                });

                const { token, expiresAt } = response.data;

                localStorage.setItem('broker_system_token', token);
                const user = JSON.parse(localStorage.getItem('broker_system_user') || '{}');
                user.expiresAt = expiresAt;
                localStorage.setItem('broker_system_user', JSON.stringify(user));

                processQueue(null, token);

                originalRequest.headers.Authorization = `Bearer ${token}`;
                return apiClient(originalRequest);
            } catch (refreshError) {
                processQueue(refreshError, null);
                localStorage.removeItem('broker_system_token');
                localStorage.removeItem('broker_system_user');
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        // For other errors, show notification
        const errorData = error.response?.data as { error?: string } | undefined;
        const message = errorData?.error || 'Wystąpił nieoczekiwany błąd';
        if (error.response?.status !== 401) {
            notifications.show({
                title: 'Błąd',
                message: message,
                color: 'red',
            });
        }

        return Promise.reject(error);
    }
);

export default apiClient;
