import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MantineProvider, createTheme } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './api/queryClient'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import './index.css'
import App from './App.tsx'

const theme = createTheme({
    primaryColor: 'blue',
    fontFamily: 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif',
})



createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <QueryClientProvider client={queryClient}>
            <MantineProvider theme={theme} defaultColorScheme="dark">
                <Notifications position="top-right" zIndex={1000} />
                <App />
            </MantineProvider>
        </QueryClientProvider>
    </StrictMode>,
)
