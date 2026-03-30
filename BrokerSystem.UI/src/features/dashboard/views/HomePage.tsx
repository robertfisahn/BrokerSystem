import { Container, Title, Text, Loader, Center, Group } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { getDashboardStats, getAgentDashboard } from '../api/dashboardApi'
import AdminDashboard from '../components/AdminDashboard'
import AgentDashboard from '../components/AgentDashboard'
import { useAuth } from '../../../providers/AuthProvider'

export function HomePage() {
    const { user } = useAuth();
    const isAdmin = user?.role === 'Admin';

    const adminQuery = useQuery({
        queryKey: ['dashboardStats'],
        queryFn: getDashboardStats,
        staleTime: 5 * 60 * 1000,
        enabled: isAdmin
    });

    const agentQuery = useQuery({
        queryKey: ['agentDashboard'],
        queryFn: getAgentDashboard,
        staleTime: 5 * 60 * 1000,
        enabled: !isAdmin
    });

    const activeQuery = isAdmin ? adminQuery : agentQuery;

    if (activeQuery.isLoading) {
        return (
            <Center style={{ height: '50vh' }}>
                <Loader size="xl" />
            </Center>
        )
    }

    if (activeQuery.error || !activeQuery.data) {
        return (
            <Container size="xl" py="xl">
                <Text c="red">Błąd podczas ładowania danych dashboardu.</Text>
            </Container>
        )
    }

    return (
        <Container size="xl" py="xl">
            <Group justify="space-between" align="flex-start" mb="xl">
                <div>
                    <Title order={1} mb={4}>Dashboard</Title>
                    <Text c="dimmed">
                        Witaj {user?.displayName}! {isAdmin ? 'Panel administracyjny systemu BrokerSystem.' : 'Twój osobisty asystent ubezpieczeniowy.'}
                    </Text>
                </div>
            </Group>

            {isAdmin && adminQuery.data ? (
                <AdminDashboard data={adminQuery.data} />
            ) : agentQuery.data ? (
                <AgentDashboard data={agentQuery.data} />
            ) : null}
        </Container>
    )
}

