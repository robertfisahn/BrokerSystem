import { useQuery } from '@tanstack/react-query'
import { Table, Container, Title, Text, Loader, Alert, Badge, Group, Paper } from '@mantine/core'
import { Users, AlertCircle } from 'lucide-react'
import { getClients, ClientListItem } from '../api/clientsApi'

export function ClientsDashboard() {
    const { data: clients, isLoading, error } = useQuery({
        queryKey: ['clients'],
        queryFn: getClients,
    })

    if (isLoading) {
        return (
            <Container size="xl" py="xl">
                <Group justify="center" py="xl">
                    <Loader size="lg" />
                    <Text>Ładowanie klientów...</Text>
                </Group>
            </Container>
        )
    }

    if (error) {
        return (
            <Container size="xl" py="xl">
                <Alert icon={<AlertCircle size={16} />} title="Błąd" color="red">
                    Nie udało się pobrać listy klientów.
                </Alert>
            </Container>
        )
    }

    return (
        <Container size="xl" py="xl">
            <Group mb="lg">
                <Users size={32} />
                <Title order={1}>Dashboard Klientów</Title>
            </Group>

            <Text c="dimmed" mb="xl">
                Łączna liczba klientów: {clients?.length ?? 0}
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
                        {clients?.map((client: ClientListItem) => (
                            <Table.Tr key={client.clientId}>
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
        </Container>
    )
}
