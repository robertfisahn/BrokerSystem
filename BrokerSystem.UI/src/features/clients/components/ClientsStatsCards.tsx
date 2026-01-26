import { useQuery } from '@tanstack/react-query'
import { SimpleGrid, Paper, Group, ThemeIcon, Text, Skeleton } from '@mantine/core'
import { Users, Crown, Building2, CalendarPlus } from 'lucide-react'
import { getClientsStats } from '../api/clientsApi'

const statConfig = [
    { key: 'totalClients', label: 'Wszyscy klienci', icon: Users, color: 'blue' },
    { key: 'vipClients', label: 'Klienci VIP', icon: Crown, color: 'yellow' },
    { key: 'corporateClients', label: 'Firmy', icon: Building2, color: 'violet' },
    { key: 'newClientsThisMonth', label: 'Nowi w tym miesiącu', icon: CalendarPlus, color: 'green' },
] as const

export function ClientsStatsCards() {
    const { data: stats, isLoading } = useQuery({
        queryKey: ['clients-stats'],
        queryFn: getClientsStats,
    })

    if (isLoading) {
        return (
            <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg" mb="xl">
                {[1, 2, 3, 4].map((i) => (
                    <Skeleton key={i} height={80} radius="md" />
                ))}
            </SimpleGrid>
        )
    }

    return (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg" mb="xl">
            {statConfig.map((stat) => (
                <Paper key={stat.key} p="md" radius="md" withBorder>
                    <Group>
                        <ThemeIcon size="lg" radius="md" variant="light" color={stat.color}>
                            <stat.icon size={20} />
                        </ThemeIcon>
                        <div>
                            <Text c="dimmed" size="xs" tt="uppercase" fw={700}>
                                {stat.label}
                            </Text>
                            <Text fw={700} size="xl">
                                {stats?.[stat.key]?.toLocaleString() ?? 0}
                            </Text>
                        </div>
                    </Group>
                </Paper>
            ))}
        </SimpleGrid>
    )
}
