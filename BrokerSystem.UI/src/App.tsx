import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Layout } from './components/layout/Layout'
import { HomePage } from './features/dashboard/views/HomePage'
import { ClientsDashboard } from './features/clients/views/ClientsDashboard'
import { PoliciesDashboard } from './features/policies/views/PoliciesDashboard'
import { ClaimsDashboard } from './features/claims/views/ClaimsDashboard'

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Layout />}>
                    <Route index element={<HomePage />} />
                    <Route path="clients" element={<ClientsDashboard />} />
                    <Route path="policies" element={<PoliciesDashboard />} />
                    <Route path="claims" element={<ClaimsDashboard />} />
                </Route>
            </Routes>
        </BrowserRouter>
    )
}

export default App
