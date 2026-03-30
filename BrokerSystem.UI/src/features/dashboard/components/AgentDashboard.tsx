import React from 'react';
import {
    Grid,
    Card,
    Text,
    Group,
    Title,
    Stack,
    Table,
    Badge,
    ScrollArea,
    ThemeIcon,
    Paper
} from '@mantine/core';
import {
    Users,
    FileText,
    DollarSign,
    Calendar,
    History
} from 'lucide-react';
import { AgentDashboardResponse } from '../api/dashboardApi';

interface AgentDashboardProps {
    data: AgentDashboardResponse;
}

const AgentDashboard: React.FC<AgentDashboardProps> = ({ data }) => {
    const { stats, expiringPolicies, recentActivities } = data;

    return (
        <Stack gap="lg">
            {/* Stats Overview */}
            <Grid>
                <Grid.Col span={{ base: 12, md: 4 }}>
                    <Paper withBorder p="md" radius="md">
                        <Group justify="space-between">
                            <div>
                                <Text size="xs" c="dimmed" fw={700} tt="uppercase">
                                    Liczba Klientów
                                </Text>
                                <Text fw={700} size="xl">
                                    {stats.totalClients}
                                </Text>
                            </div>
                            <ThemeIcon color="blue" variant="light" size={48} radius="md">
                                <Users size={28} />
                            </ThemeIcon>
                        </Group>
                    </Paper>
                </Grid.Col>

                <Grid.Col span={{ base: 12, md: 4 }}>
                    <Paper withBorder p="md" radius="md">
                        <Group justify="space-between">
                            <div>
                                <Text size="xs" c="dimmed" fw={700} tt="uppercase">
                                    Aktywne Polisy
                                </Text>
                                <Text fw={700} size="xl">
                                    {stats.activePolicies}
                                </Text>
                            </div>
                            <ThemeIcon color="teal" variant="light" size={48} radius="md">
                                <FileText size={28} />
                            </ThemeIcon>
                        </Group>
                    </Paper>
                </Grid.Col>

                <Grid.Col span={{ base: 12, md: 4 }}>
                    <Paper withBorder p="md" radius="md">
                        <Group justify="space-between">
                            <div>
                                <Text size="xs" c="dimmed" fw={700} tt="uppercase">
                                    Suma Składek (Łącznie)
                                </Text>
                                <Text fw={700} size="xl">
                                    {stats.totalPremium.toLocaleString('pl-PL', { minimumFractionDigits: 2 })} PLN
                                </Text>
                            </div>
                            <ThemeIcon color="grape" variant="light" size={48} radius="md">
                                <DollarSign size={28} />
                            </ThemeIcon>
                        </Group>
                    </Paper>
                </Grid.Col>
            </Grid>

            <Grid>
                {/* Expiring Policies */}
                <Grid.Col span={{ base: 12, md: 8 }}>
                    <Card withBorder radius="md" p="md">
                        <Group justify="space-between" mb="md">
                            <Group>
                                <Calendar size={20} style={{ color: 'var(--mantine-color-blue-6)' }} />
                                <Title order={4}>Polisy Kończące się (Następne 30 dni)</Title>
                            </Group>
                            <Badge color="red" variant="filled">{expiringPolicies.length} pilne</Badge>
                        </Group>

                        <ScrollArea>
                            <Table verticalSpacing="sm">
                                <Table.Thead>
                                    <Table.Tr>
                                        <Table.Th>Numer Polisy</Table.Th>
                                        <Table.Th>Klient</Table.Th>
                                        <Table.Th>Data Końca</Table.Th>
                                        <Table.Th>Status</Table.Th>
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {expiringPolicies.map((policy) => (
                                        <Table.Tr key={policy.policyId}>
                                            <Table.Td fw={500}>{policy.policyNumber}</Table.Td>
                                            <Table.Td>{policy.clientName}</Table.Td>
                                            <Table.Td>{new Date(policy.endDate).toLocaleDateString()}</Table.Td>
                                            <Table.Td>
                                                <Badge color={policy.daysLeft <= 7 ? 'red' : 'orange'} variant="light">
                                                    zostało {policy.daysLeft} dni
                                                </Badge>
                                            </Table.Td>
                                        </Table.Tr>
                                    ))}
                                    {expiringPolicies.length === 0 && (
                                        <Table.Tr>
                                            <Table.Td colSpan={4} align="center">
                                                <Text c="dimmed">Brak polis kończących się wkrótce</Text>
                                            </Table.Td>
                                        </Table.Tr>
                                    )}
                                </Table.Tbody>
                            </Table>
                        </ScrollArea>
                    </Card>
                </Grid.Col>

                {/* Recent Activities */}
                <Grid.Col span={{ base: 12, md: 4 }}>
                    <Card withBorder radius="md" p="md">
                        <Group justify="space-between" mb="md">
                            <Group>
                                <History size={20} style={{ color: 'var(--mantine-color-teal-6)' }} />
                                <Title order={4}>Ostatnie Aktywności</Title>
                            </Group>
                            <Badge variant="outline" color="teal">Ostatnie 7 dni</Badge>
                        </Group>

                        <Stack gap="xs">
                            {recentActivities.map((activity, index) => (
                                <Paper key={index} withBorder p="xs" radius="sm">
                                    <Group justify="space-between" mb={2}>
                                        <Text size="xs" fw={700} tt="uppercase" c="dimmed">{activity.type}</Text>
                                        <Text size="xs" c="dimmed">{new Date(activity.createdAt).toLocaleTimeString()}</Text>
                                    </Group>
                                    <Text size="sm">{activity.description}</Text>
                                </Paper>
                            ))}
                            {recentActivities.length === 0 && (
                                <Text c="dimmed" size="sm" ta="center" py="xl">Brak ostatnich aktywności</Text>
                            )}
                        </Stack>
                    </Card>
                </Grid.Col>
            </Grid >
        </Stack >
    );
};

export default AgentDashboard;
