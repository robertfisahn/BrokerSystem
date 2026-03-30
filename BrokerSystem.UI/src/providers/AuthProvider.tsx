import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../features/auth/api/authApi';

interface User {
    displayName: string;
    expiresAt: string;
    role: string;
    agentId?: number;
}

interface AuthContextType {
    user: User | null;
    token: string | null;
    isAuthenticated: boolean;
    login: (token: string, user: User) => void;
    logout: () => void;
    isLoading: boolean;
    isAdmin: boolean;
    isAgent: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const savedToken = localStorage.getItem('broker_system_token');
        const savedUser = localStorage.getItem('broker_system_user');

        if (savedToken && savedUser) {
            try {
                const parsedUser = JSON.parse(savedUser) as User;
                if (new Date(parsedUser.expiresAt) > new Date() && parsedUser.displayName && parsedUser.role) {
                    setToken(savedToken);
                    setUser(parsedUser);
                } else {
                    logout();
                }
            } catch (e) {
                logout();
            }
        }
        setIsLoading(false);
    }, []);

    const login = (newToken: string, newUser: User) => {
        setToken(newToken);
        setUser(newUser);
        localStorage.setItem('broker_system_token', newToken);
        localStorage.setItem('broker_system_user', JSON.stringify(newUser));
    };

    const queryClient = useQueryClient();

    const logout = async () => {
        try {
            await authApi.logout();
        } catch (e) {
            console.error('Logout failed', e);
        } finally {
            setToken(null);
            setUser(null);
            localStorage.removeItem('broker_system_token');
            localStorage.removeItem('broker_system_user');

            // CRITICAL: Clear all React Query caches to prevent stale data between roles
            queryClient.clear();
        }
    };


    return (
        <AuthContext.Provider
            value={{
                user,
                token,
                isAuthenticated: !!token,
                isAdmin: user?.role === 'Admin',
                isAgent: user?.role === 'Agent',
                login,
                logout,
                isLoading
            }}
        >
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
