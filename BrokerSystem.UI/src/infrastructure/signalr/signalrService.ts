import * as signalR from '@microsoft/signalr';

class SignalRService {
    private connection: signalR.HubConnection | null = null;
    private baseUrl: string = 'http://localhost:5199/broker-hub';

    public async startConnection(): Promise<void> {
        if (this.connection && (
            this.connection.state === signalR.HubConnectionState.Connected ||
            this.connection.state === signalR.HubConnectionState.Connecting ||
            this.connection.state === signalR.HubConnectionState.Reconnecting
        )) {
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.baseUrl)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        try {
            await this.connection.start();
            console.log('SignalR connected successfully to: ', this.baseUrl);
        } catch (err: any) {
            if (err.message && err.message.includes("stopped during negotiation")) {
                return;
            }
            console.error('SignalR connection error: ', err);
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    public onNotification(callback: (title: string, message: string) => void): void {
        this.connection?.off('ReceiveNotification');
        this.connection?.on('ReceiveNotification', (title, message) => {
            console.log('SignalR message received:', { title, message });
            callback(title, message);
        });
    }

    public stopConnection(): void {
        this.connection?.off('ReceiveNotification');
        this.connection?.stop();
    }
}

export const signalRService = new SignalRService();
