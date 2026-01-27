import { Paper, Title, SimpleGrid, useMantineTheme } from '@mantine/core';
import {
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
    PieChart,
    Pie,
    Cell,
    Legend
} from 'recharts';
import { MonthlySales, ClientTypeDistribution, PolicyStatusDistribution } from '../api/dashboardApi';

interface DashboardChartsProps {
    monthlySales: MonthlySales[];
    clientTypes: ClientTypeDistribution[];
    policyStatuses: PolicyStatusDistribution[];
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8'];

const CustomPieTooltip = ({ active, payload, theme }: any) => {
    if (active && payload && payload.length) {
        const color = payload[0].payload.fill || payload[0].color;

        return (
            <div style={{
                backgroundColor: theme.colors.dark[7],
                border: `2px solid ${color}`,
                borderRadius: '8px',
                padding: '8px 12px',
                boxShadow: '0 4px 6px rgba(0,0,0,0.3)',
                color: theme.white
            }}>
                <div style={{ fontWeight: 700, fontSize: '14px', marginBottom: '2px' }}>
                    {payload[0].name}
                </div>
                <div style={{ fontSize: '12px', opacity: 0.8 }}>
                    Liczba: <span style={{ fontWeight: 600, color: theme.white }}>{payload[0].value}</span>
                </div>
            </div>
        );
    }
    return null;
};

export function DashboardCharts({ monthlySales, clientTypes, policyStatuses }: DashboardChartsProps) {
    const theme = useMantineTheme();

    const renderLegend = (value: string, entry: any) => {
        const { payload } = entry;
        const count = payload.clientCount || payload.policyCount;
        return <span style={{ color: theme.colors.dark[0], fontSize: '12px' }}>{value} ({count})</span>;
    };

    return (
        <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="lg" mt="xl">
            <Paper p="md" radius="md" withBorder style={{ height: 400 }}>
                <Title order={3} mb="md">Sprzedaż Miesięczna</Title>
                <ResponsiveContainer width="100%" height="80%">
                    <LineChart data={monthlySales} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={theme.colors.dark[4]} />
                        <XAxis
                            dataKey="month"
                            stroke={theme.colors.dark[2]}
                            fontSize={12}
                            tickLine={false}
                            axisLine={false}
                        />
                        <YAxis
                            stroke={theme.colors.dark[2]}
                            fontSize={12}
                            tickLine={false}
                            axisLine={false}
                            tickFormatter={(value) => `${(value / 1000).toFixed(0)}k`}
                        />
                        <Tooltip
                            contentStyle={{ backgroundColor: theme.colors.dark[7], border: `1px solid ${theme.colors.dark[4]}` }}
                            itemStyle={{ color: theme.white }}
                        />
                        <Line
                            type="monotone"
                            dataKey="totalPremium"
                            stroke={theme.colors.blue[6]}
                            strokeWidth={3}
                            dot={{ r: 4, fill: theme.colors.blue[6] }}
                            activeDot={{ r: 6, strokeWidth: 0 }}
                            name="Premia (PLN)"
                        />
                    </LineChart>
                </ResponsiveContainer>
            </Paper>

            <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="lg">
                <Paper p="md" radius="md" withBorder style={{ height: 400 }}>
                    <Title order={3} mb="md">Typy Klientów</Title>
                    <ResponsiveContainer width="100%" height="80%">
                        <PieChart>
                            <Pie
                                data={clientTypes}
                                dataKey="clientCount"
                                nameKey="clientType"
                                cx="50%"
                                cy="50%"
                                innerRadius={60}
                                outerRadius={80}
                                paddingAngle={5}
                                stroke="none"
                            >
                                {clientTypes.map((_, index) => (
                                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                ))}
                            </Pie>
                            <Tooltip content={<CustomPieTooltip theme={theme} />} />
                            <Legend
                                verticalAlign="bottom"
                                iconType="circle"
                                formatter={renderLegend}
                            />
                        </PieChart>
                    </ResponsiveContainer>
                </Paper>

                <Paper p="md" radius="md" withBorder style={{ height: 400 }}>
                    <Title order={3} mb="md">Statusy Polis</Title>
                    <ResponsiveContainer width="100%" height="80%">
                        <PieChart>
                            <Pie
                                data={policyStatuses}
                                dataKey="policyCount"
                                nameKey="policyStatus"
                                cx="50%"
                                cy="50%"
                                innerRadius={60}
                                outerRadius={80}
                                paddingAngle={5}
                                stroke="none"
                            >
                                {policyStatuses.map((_, index) => (
                                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                ))}
                            </Pie>
                            <Tooltip content={<CustomPieTooltip theme={theme} />} />
                            <Legend
                                verticalAlign="bottom"
                                iconType="circle"
                                formatter={renderLegend}
                            />
                        </PieChart>
                    </ResponsiveContainer>
                </Paper>
            </SimpleGrid>
        </SimpleGrid>
    );
}
