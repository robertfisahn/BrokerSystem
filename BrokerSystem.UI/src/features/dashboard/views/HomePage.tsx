import { Container, Title, Text, SimpleGrid, Paper, Group, ThemeIcon, Loader, Center } from '@mantine/core'
import { Users, FileText, AlertTriangle, TrendingUp } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { getDashboardStats } from '../api/dashboardApi'
import { DashboardCharts } from '../components/DashboardCharts'

export function HomePage() {
    const { data, isLoading, error } = useQuery({
        queryKey: ['dashboardStats'],
        queryFn: getDashboardStats
    })

    if (isLoading) {
        return (
            <Center style={{ height: '50vh' }}>
                <Loader size="xl" />
            </Center>
        )
    }

    if (error || !data) {
        return (
            <Container size="xl" py="xl">
                <Text c="red">Błąd podczas ładowania danych dashboardu.</Text>
            </Container>
        )
    }

    const stats = [
        {
            icon: Users,
            label: 'Klienci',
            value: data.kpis.totalClients.toLocaleString(),
            color: 'blue'
        },
        {
            icon: FileText,
            label: 'Polisy',
            value: data.kpis.totalPolicies.toLocaleString(),
            color: 'green'
        },
        {
            icon: AlertTriangle,
            label: 'Aktywne szkody',
            value: data.kpis.activeClaims.toLocaleString(),
            color: 'orange'
        },
        {
            icon: TrendingUp,
            label: 'Premia (PLN)',
            value: data.kpis.totalPremiumVolume.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
            color: 'violet'
        },
    ]

    return (
        <Container size="xl" py="xl">
            <Title order={1} mb="sm">Dashboard</Title>
            <Text c="dimmed" mb="xl">
                Witaj w systemie zarządzania polisami ubezpieczeniowymi.
            </Text>

            <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg">
                {stats.map((stat) => (
                    <Paper key={stat.label} p="md" radius="md" withBorder shadow="xs">
                        <Group>
                            <ThemeIcon size="xl" radius="md" variant="light" color={stat.color}>
                                <stat.icon size={24} />
                            </ThemeIcon>
                            <div>
                                <Text c="dimmed" size="xs" tt="uppercase" fw={700}>
                                    {stat.label}
                                </Text>
                                <Text fw={700} size="xl">
                                    {stat.value}
                                </Text>
                            </div>
                        </Group>
                    </Paper>
                ))}
            </SimpleGrid>

            <DashboardCharts
                monthlySales={data.monthlySales}
                clientTypes={data.clientTypeDistribution}
                policyStatuses={data.policyStatusDistribution}
            />
        </Container>
    )
}
