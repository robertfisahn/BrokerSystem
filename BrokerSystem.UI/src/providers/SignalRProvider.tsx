import React, { createContext, useEffect, useContext } from 'react';
import { signalRService } from '../infrastructure/signalr/signalrService';
import { notifications } from '@mantine/notifications';

const SignalRContext = createContext<void | null>(null);

export const SignalRProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    useEffect(() => {
        const initSignalR = async () => {
            await signalRService.startConnection();

            signalRService.onNotification((title, message) => {
                notifications.show({
                    title: title,
                    message: message,
                    color: 'blue',
                    autoClose: 10000,
                });
            });
        };

        initSignalR();

        return () => {
            signalRService.stopConnection();
        };
    }, []);

    return <SignalRContext.Provider value={null}>{children}</SignalRContext.Provider>;
};

export const useSignalR = () => useContext(SignalRContext);
