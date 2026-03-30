import { NavLink, useNavigate } from 'react-router-dom'
import { Stack, Text, UnstyledButton, Group, Avatar, Box, ActionIcon, Tooltip } from '@mantine/core'
import { Home, Users, FileText, AlertTriangle, LogOut, ShieldCheck, LucideIcon } from 'lucide-react'
import { useAuth } from '../../providers/AuthProvider'

interface NavItem {
    icon: LucideIcon;
    label: string;
    to: string;
}

export function Sidebar() {
    const { user, logout, isAdmin } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    const navItems: NavItem[] = [
        { icon: Home, label: 'Dashboard', to: '/' },
        { icon: Users, label: 'Klienci', to: '/clients' },
        { icon: FileText, label: 'Polisy', to: '/policies' },
        { icon: AlertTriangle, label: 'Szkody', to: '/claims' },
    ];

    if (isAdmin) {
        navItems.push({ icon: ShieldCheck, label: 'Agenci', to: '/agents' });
    }

    return (
        <Stack h="100%" justify="space-between">
            <Box>
                <Box mb="xl">
                    <Text size="xl" fw={700} c="white">
                        🏢 BrokerSystem
                    </Text>
                    <Text size="xs" c="dimmed" fw={500}>
                        Portal Zarządzania Ubezpieczeniami
                    </Text>
                </Box>

                <Stack gap="xs">
                    {navItems.map((item) => (
                        <NavLink
                            key={item.to}
                            to={item.to}
                            style={{ textDecoration: 'none' }}
                        >
                            {({ isActive }) => (
                                <UnstyledButton
                                    style={{
                                        display: 'block',
                                        width: '100%',
                                        padding: '0.75rem 1rem',
                                        borderRadius: 8,
                                        backgroundColor: isActive
                                            ? 'var(--mantine-color-blue-filled)'
                                            : 'transparent',
                                        color: isActive
                                            ? 'white'
                                            : 'var(--mantine-color-dark-1)',
                                        transition: 'background-color 0.15s ease',
                                    }}
                                >
                                    <Group gap="sm">
                                        <item.icon size={20} />
                                        <Text size="sm" fw={500}>{item.label}</Text>
                                    </Group>
                                </UnstyledButton>
                            )}
                        </NavLink>
                    ))}
                </Stack>
            </Box>

            <Box pt="md" style={{ borderTop: '1px solid var(--mantine-color-dark-5)' }}>
                <Group justify="space-between" wrap="nowrap">
                    <Group gap="sm" wrap="nowrap" style={{ overflow: 'hidden' }}>
                        <Avatar color={isAdmin ? "red" : "blue"} radius="xl">
                            {user?.displayName?.charAt(0).toUpperCase() || '?'}
                        </Avatar>
                        <Box style={{ overflow: 'hidden' }}>
                            <Text size="sm" fw={700} truncate c="white">
                                {user?.displayName}
                            </Text>
                            <Text size="xs" c="dimmed" truncate>
                                {isAdmin ? 'Administrator' : 'Agent Ubezpieczeniowy'}
                            </Text>
                        </Box>
                    </Group>

                    <Tooltip label="Wyloguj się">
                        <ActionIcon
                            variant="subtle"
                            color="gray"
                            onClick={handleLogout}
                            size="lg"
                        >
                            <LogOut size={18} />
                        </ActionIcon>
                    </Tooltip>
                </Group>
            </Box>
        </Stack>
    )
}

