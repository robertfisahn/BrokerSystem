import { Container, Title, Text, SimpleGrid, Paper, Group, ThemeIcon } from '@mantine/core'
import { Users, FileText, AlertTriangle, TrendingUp } from 'lucide-react'

const stats = [
    { icon: Users, label: 'Klienci', value: '2,000', color: 'blue' },
    { icon: FileText, label: 'Polisy', value: '5,432', color: 'green' },
    { icon: AlertTriangle, label: 'Zgłoszone szkody', value: '156', color: 'orange' },
    { icon: TrendingUp, label: 'Prowizje (PLN)', value: '1,234,567', color: 'violet' },
]

export function HomePage() {
    return (
        <Container size="xl" py="xl">
            <Title order={1} mb="sm">Dashboard</Title>
            <Text c="dimmed" mb="xl">
                Witaj w systemie zarządzania polisami ubezpieczeniowymi.
            </Text>

            <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg">
                {stats.map((stat) => (
                    <Paper key={stat.label} p="md" radius="md" withBorder>
                        <Group>
                            <ThemeIcon size="lg" radius="md" variant="light" color={stat.color}>
                                <stat.icon size={20} />
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
        </Container>
    )
}
