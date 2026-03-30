import { AppShell } from '@mantine/core'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'

export function Layout() {
    return (
        <AppShell
            navbar={{ width: 250, breakpoint: 'sm' }}
            padding="md"
        >
            <AppShell.Navbar p="md" style={{ backgroundColor: 'var(--mantine-color-dark-7)' }}>
                <Sidebar />
            </AppShell.Navbar>

            <AppShell.Main bg="dark.8">
                <Outlet />
            </AppShell.Main>
        </AppShell>
    )
}
