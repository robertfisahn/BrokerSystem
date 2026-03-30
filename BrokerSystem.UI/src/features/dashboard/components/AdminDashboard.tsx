import React from 'react';
import { SimpleGrid, Paper, Group, ThemeIcon, Text } from '@mantine/core';
import { Users, FileText, AlertTriangle, TrendingUp } from 'lucide-react';
import { DashboardStatsResponse } from '../api/dashboardApi';
import { DashboardCharts } from './DashboardCharts';

interface AdminDashboardProps {
    data: DashboardStatsResponse;
}

const AdminDashboard: React.FC<AdminDashboardProps> = ({ data }) => {
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
    ];

    return (
        <>
            <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="lg" mb="xl">
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
        </>
    );
};

export default AdminDashboard;
