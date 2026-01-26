import { NavLink } from 'react-router-dom'
import { Stack, Text, UnstyledButton, Group } from '@mantine/core'
import { Home, Users, FileText, AlertTriangle } from 'lucide-react'

const navItems = [
    { icon: Home, label: 'Dashboard', to: '/' },
    { icon: Users, label: 'Klienci', to: '/clients' },
    { icon: FileText, label: 'Polisy', to: '/policies' },
    { icon: AlertTriangle, label: 'Szkody', to: '/claims' },
]

export function Sidebar() {
    return (
        <nav style={{
            width: 250,
            minHeight: '100vh',
            backgroundColor: 'var(--mantine-color-dark-7)',
            borderRight: '1px solid var(--mantine-color-dark-5)',
            padding: '1rem',
        }}>
            <Text size="xl" fw={700} mb="xl" c="white">
                🏢 BrokerSystem
            </Text>
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
        </nav>
    )
}
