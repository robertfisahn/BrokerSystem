import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MantineProvider, createTheme } from '@mantine/core'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import '@mantine/core/styles.css'
import './index.css'
import App from './App.tsx'

const theme = createTheme({
    primaryColor: 'blue',
    fontFamily: 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif',
})

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <QueryClientProvider client={queryClient}>
            <MantineProvider theme={theme} defaultColorScheme="dark">
                <App />
            </MantineProvider>
        </QueryClientProvider>
    </StrictMode>,
)
