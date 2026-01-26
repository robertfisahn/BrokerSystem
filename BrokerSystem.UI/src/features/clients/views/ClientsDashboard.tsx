import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Table, Container, Title, Text, Loader, Alert, Badge, Group, Paper, TextInput, Pagination, Select, Flex } from '@mantine/core'
import { Users, AlertCircle, Search, ArrowUpDown } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { getClients, ClientListItem, GetClientsParams } from '../api/clientsApi'
import { ClientsStatsCards } from '../components/ClientsStatsCards'

export function ClientsDashboard() {
    const navigate = useNavigate()
    const [page, setPage] = useState(1)
    const [search, setSearch] = useState('')
    const [sortBy, setSortBy] = useState('clientId')
    const [sortDescending, setSortDescending] = useState(false)
    const pageSize = 20

    const params: GetClientsParams = {
        page,
        pageSize,
        search: search || undefined,
        sortBy,
        sortDescending,
    }

    const { data, isLoading, error } = useQuery({
        queryKey: ['clients', params],
        queryFn: () => getClients(params),
    })

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearch(event.target.value)
        setPage(1)
    }

    const handleSortChange = (value: string | null) => {
        if (value) {
            const [field, order] = value.split('-')
            setSortBy(field)
            setSortDescending(order === 'desc')
            setPage(1)
        }
    }

    return (
        <Container size="xl" py="xl">
            <Group mb="lg">
                <Users size={32} />
                <Title order={1}>Klienci</Title>
            </Group>

            {/* Stats Cards */}
            <ClientsStatsCards />

            {/* Search and Sort Controls */}
            <Flex gap="md" mb="lg" wrap="wrap">
                <TextInput
                    placeholder="Szukaj po imieniu, nazwisku lub firmie..."
                    leftSection={<Search size={16} />}
                    value={search}
                    onChange={handleSearchChange}
                    style={{ flex: 1, minWidth: 250 }}
                />
                <Select
                    leftSection={<ArrowUpDown size={16} />}
                    placeholder="Sortuj..."
                    value={`${sortBy}-${sortDescending ? 'desc' : 'asc'}`}
                    onChange={handleSortChange}
                    data={[
                        { value: 'clientId-asc', label: 'ID (rosnąco)' },
                        { value: 'clientId-desc', label: 'ID (malejąco)' },
                        { value: 'firstName-asc', label: 'Imię (A-Z)' },
                        { value: 'firstName-desc', label: 'Imię (Z-A)' },
                        { value: 'lastName-asc', label: 'Nazwisko (A-Z)' },
                        { value: 'lastName-desc', label: 'Nazwisko (Z-A)' },
                        { value: 'companyName-asc', label: 'Firma (A-Z)' },
                        { value: 'companyName-desc', label: 'Firma (Z-A)' },
                    ]}
                    style={{ width: 200 }}
                />
            </Flex>

            {/* Loading State */}
            {isLoading && (
                <Group justify="center" py="xl">
                    <Loader size="lg" />
                    <Text>Ładowanie klientów...</Text>
                </Group>
            )}

            {/* Error State */}
            {error && (
                <Alert icon={<AlertCircle size={16} />} title="Błąd" color="red">
                    Nie udało się pobrać listy klientów.
                </Alert>
            )}

            {/* Table */}
            {data && (
                <>
                    <Text c="dimmed" size="sm" mb="sm">
                        Wyświetlono {data.items.length} z {data.totalCount} klientów
                    </Text>
                    <Paper shadow="sm" radius="md" withBorder>
                        <Table striped highlightOnHover>
                            <Table.Thead>
                                <Table.Tr>
                                    <Table.Th>ID</Table.Th>
                                    <Table.Th>Imię i Nazwisko / Firma</Table.Th>
                                    <Table.Th>Typ</Table.Th>
                                    <Table.Th>Kontakt</Table.Th>
                                    <Table.Th>Miasto</Table.Th>
                                    <Table.Th>Aktywne Polisy</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {data.items.map((client: ClientListItem) => (
                                    <Table.Tr
                                        key={client.clientId}
                                        onClick={() => navigate(`/clients/${client.clientId}`)}
                                        style={{ cursor: 'pointer' }}
                                    >
                                        <Table.Td>{client.clientId}</Table.Td>
                                        <Table.Td>
                                            {client.companyName
                                                ? client.companyName
                                                : `${client.firstName ?? ''} ${client.lastName ?? ''}`}
                                        </Table.Td>
                                        <Table.Td>
                                            <Badge color={client.clientType === 'VIP' ? 'yellow' : 'blue'} variant="light">
                                                {client.clientType}
                                            </Badge>
                                        </Table.Td>
                                        <Table.Td>{client.primaryContact ?? '-'}</Table.Td>
                                        <Table.Td>{client.city ?? '-'}</Table.Td>
                                        <Table.Td>
                                            <Badge color={client.activePoliciesCount > 0 ? 'green' : 'gray'}>
                                                {client.activePoliciesCount}
                                            </Badge>
                                        </Table.Td>
                                    </Table.Tr>
                                ))}
                            </Table.Tbody>
                        </Table>
                    </Paper>

                    {/* Pagination */}
                    {data.totalPages > 1 && (
                        <Group justify="center" mt="lg">
                            <Pagination
                                total={data.totalPages}
                                value={page}
                                onChange={setPage}
                            />
                        </Group>
                    )}
                </>
            )}
        </Container>
    )
}
