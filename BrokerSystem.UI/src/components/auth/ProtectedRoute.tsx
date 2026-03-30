import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';
import { useAuth } from '../../providers/AuthProvider';
import { Center, Loader } from '@mantine/core';

interface ProtectedRouteProps {
    requiredRole?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ requiredRole }) => {
    const { isAuthenticated, user, isLoading, isAdmin } = useAuth();
    const location = useLocation();

    if (isLoading) {
        return (
            <Center style={{ width: '100dvw', height: '100dvh' }}>
                <Loader size="xl" />
            </Center>
        );
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    if (requiredRole && user?.role.toLowerCase() !== requiredRole.toLowerCase() && !isAdmin) {
        return <Navigate to="/" replace />;
    }

    return <Outlet />;
};
