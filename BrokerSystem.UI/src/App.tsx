import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Layout } from './components/layout/Layout'
import { HomePage } from './features/dashboard/views/HomePage'
import { ClientsDashboard } from './features/clients/views/ClientsDashboard'
import { ClientDetailsView } from './features/clients/views/ClientDetailsView'
import { PoliciesDashboard } from './features/policies/views/PoliciesDashboard'
import { ClaimsDashboard } from './features/claims/views/ClaimsDashboard'
import { LoginPage } from './features/auth/views/LoginPage'
import { ProtectedRoute } from './components/auth/ProtectedRoute'

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<LoginPage />} />

                <Route element={<ProtectedRoute />}>
                    <Route path="/" element={<Layout />}>
                        <Route index element={<HomePage />} />
                        <Route path="clients" element={<ClientsDashboard />} />
                        <Route path="clients/:id" element={<ClientDetailsView />} />
                        <Route path="policies" element={<PoliciesDashboard />} />
                        <Route path="claims" element={<ClaimsDashboard />} />
                    </Route>
                </Route>
            </Routes>
        </BrowserRouter>
    )
}

export default App
