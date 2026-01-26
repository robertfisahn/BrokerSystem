import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'

export function Layout() {
    return (
        <div style={{ display: 'flex', minHeight: '100vh' }}>
            <Sidebar />
            <main style={{ flex: 1, backgroundColor: 'var(--mantine-color-dark-8)' }}>
                <Outlet />
            </main>
        </div>
    )
}
