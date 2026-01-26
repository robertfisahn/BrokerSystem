import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: (failureCount, error: any) => {
                const status = error.response?.status;
                if (status === 404 || status === 401 || status === 403) return false;
                return failureCount < 1;
            },
            refetchOnWindowFocus: false,
            staleTime: 1000 * 60 * 5,
        },
    },
});
