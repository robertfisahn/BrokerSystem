import axios from 'axios';
import { notifications } from '@mantine/notifications';

const apiClient = axios.create({
    baseURL: 'http://localhost:5199/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const message = error.response?.data?.error || 'Wystąpił nieoczekiwany błąd';

        notifications.show({
            title: 'Błąd',
            message: message,
            color: 'red',
        });

        return Promise.reject(error);
    }
);

export default apiClient;
