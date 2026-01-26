import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
    Container, Title, Text, Group, Button, Tabs, Paper, SimpleGrid,
    Badge, Table, Loader, Alert, Stack, Divider
} from '@mantine/core'
import {
    ArrowLeft, User, FileText, AlertTriangle, MapPin, Phone,
    Mail, Calendar, Briefcase, CreditCard
} from 'lucide-react'
import { getClient360 } from '../api/clientsApi'

export function ClientDetailsView() {
    const { id } = useParams<{ id: string }>()
    const navigate = useNavigate()
    const clientId = parseInt(id || '0')

    const { data: client, isLoading, error } = useQuery({
        queryKey: ['client-360', clientId],
        queryFn: () => getClient360(clientId),
        enabled: clientId > 0,
    })

    if (isLoading) {
        return (
            <Container size="xl" py="xl">
                <Group justify="center" py="xl">
                    <Loader size="xl" />
                    <Text>Pobieranie szczegółów klienta...</Text>
                </Group>
            </Container>
        )
    }

    if (error || !client) {
        return (
            <Container size="xl" py="xl">
                <Alert icon={<AlertTriangle size={16} />} title="Błąd" color="red">
                    Nie udało się pobrać danych klienta.
                </Alert>
                <Button variant="subtle" mt="md" onClick={() => navigate('/clients')}>
                    Wróć do listy
                </Button>
            </Container>
        )
    }

    const fullName = client.companyName
        ? client.companyName
        : `${client.firstName} ${client.lastName}`

    const getTaxIdLabel = (taxId?: string) => {
        if (!taxId) return 'Identyfikator'
        const clean = taxId.replace(/[^0-9]/g, '')
        if (clean.length === 11) return 'PESEL'
        if (clean.length === 10) return 'NIP'
        return 'ID Podatkowe'
    }

    return (
        <Container size="xl" py="xl">
            {/* Header */}
            <Group justify="space-between" mb="xl">
                <Group>
                    <Button
                        variant="subtle"
                        leftSection={<ArrowLeft size={16} />}
                        onClick={() => navigate('/clients')}
                    >
                        Wróć
                    </Button>
                    <Divider orientation="vertical" />
                    <Stack gap={0}>
                        <Title order={2}>{fullName}</Title>
                        <Text c="dimmed" size="sm">ID: {client.clientId} • Rejestracja: {client.registrationDate}</Text>
                    </Stack>
                </Group>
                <Badge size="xl" radius="md" color={client.clientType === 'VIP' ? 'yellow' : 'blue'}>
                    {client.clientType}
                </Badge>
            </Group>

            {/* Dashboard Tabs */}
            <Tabs defaultValue="personal" variant="outline" radius="md">
                <Tabs.List mb="md">
                    <Tabs.Tab value="personal" leftSection={<User size={16} />}>Dane osobowe</Tabs.Tab>
                    <Tabs.Tab value="policies" leftSection={<FileText size={16} />}>Polisy ({client.policies.length})</Tabs.Tab>
                    <Tabs.Tab value="claims" leftSection={<AlertTriangle size={16} />}>Szkody</Tabs.Tab>
                </Tabs.List>

                {/* PERSONAL TAB */}
                <Tabs.Panel value="personal">
                    <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg">
                        <Paper p="md" withBorder radius="md">
                            <Title order={4} mb="md">Informacje podstawowe</Title>
                            <Stack gap="sm">
                                <DetailItem
                                    icon={<CreditCard size={16} />}
                                    label={getTaxIdLabel(client.taxId)}
                                    value={client.taxId || '-'}
                                />
                                <DetailItem icon={<Calendar size={16} />} label="Data rejestracji" value={client.registrationDate} />
                                <DetailItem icon={<Briefcase size={16} />} label="Typ" value={client.clientType || '-'} />
                            </Stack>
                        </Paper>

                        <Paper p="md" withBorder radius="md">
                            <Title order={4} mb="md">Kontakt</Title>
                            <Stack gap="sm">
                                {client.contacts.map((contact, idx) => (
                                    <DetailItem
                                        key={idx}
                                        icon={contact.contactType?.toLowerCase().includes('mail') ? <Mail size={16} /> : <Phone size={16} />}
                                        label={contact.contactType || 'Kontakt'}
                                        value={contact.contactValue || '-'}
                                        badge={contact.isPrimary ? <Badge size="xs" color="green">Główny</Badge> : null}
                                    />
                                ))}
                            </Stack>
                        </Paper>

                        <Paper p="md" withBorder radius="md" style={{ gridColumn: 'span 1 / span 2' }}>
                            <Title order={4} mb="md">Adresy</Title>
                            <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
                                {client.addresses.map((addr, idx) => (
                                    <Group key={idx} align="start" wrap="nowrap">
                                        <MapPin size={24} color="gray" />
                                        <Stack gap={0}>
                                            <Text fw={600} size="sm">{addr.street}</Text>
                                            <Text size="sm">{addr.postalCode} {addr.city}</Text>
                                            <Text size="xs" c="dimmed">{addr.country}</Text>
                                            {addr.isCurrent && <Badge size="xs" mt={4}>Aktualny</Badge>}
                                        </Stack>
                                    </Group>
                                ))}
                            </SimpleGrid>
                        </Paper>
                    </SimpleGrid>
                </Tabs.Panel>

                {/* POLICIES TAB */}
                <Tabs.Panel value="policies">
                    <Paper withBorder radius="md" style={{ overflow: 'hidden' }}>
                        <Table striped highlightOnHover>
                            <Table.Thead>
                                <Table.Tr>
                                    <Table.Th>Numer</Table.Th>
                                    <Table.Th>Typ</Table.Th>
                                    <Table.Th>Okres</Table.Th>
                                    <Table.Th>Składka</Table.Th>
                                    <Table.Th>Status</Table.Th>
                                </Table.Tr>
                            </Table.Thead>
                            <Table.Tbody>
                                {client.policies.map((policy) => (
                                    <Table.Tr key={policy.policyId}>
                                        <Table.Td fw={600}>{policy.policyNumber}</Table.Td>
                                        <Table.Td>{policy.policyType}</Table.Td>
                                        <Table.Td>
                                            {policy.startDate} - {policy.endDate}
                                        </Table.Td>
                                        <Table.Td fw={600}>{policy.premiumAmount} PLN</Table.Td>
                                        <Table.Td>
                                            <Badge color={policy.status === 'Active' ? 'green' : 'gray'}>
                                                {policy.status}
                                            </Badge>
                                        </Table.Td>
                                    </Table.Tr>
                                ))}
                                {client.policies.length === 0 && (
                                    <Table.Tr><Table.Td colSpan={5} ta="center" py="xl">Brak aktywnych polis.</Table.Td></Table.Tr>
                                )}
                            </Table.Tbody>
                        </Table>
                    </Paper>
                </Tabs.Panel>

                {/* CLAIMS TAB */}
                <Tabs.Panel value="claims">
                    <Stack>
                        {client.policies.flatMap(p => p.claims.map(cl => ({ ...cl, policyNumber: p.policyNumber }))).map((claim) => (
                            <Paper key={claim.claimId} p="md" withBorder radius="md">
                                <Group justify="space-between">
                                    <Group>
                                        <AlertTriangle size={24} color="var(--mantine-color-orange-6)" />
                                        <Stack gap={0}>
                                            <Text fw={700}>Szkoda: {claim.claimNumber}</Text>
                                            <Text size="xs" c="dimmed">Data zdarzenia: {claim.incidentDate} • Polisa: {claim.policyNumber}</Text>
                                        </Stack>
                                    </Group>
                                    <Group>
                                        <Stack gap={0} align="end">
                                            <Badge color={claim.status?.includes('Approve') ? 'green' : 'orange'}>{claim.status}</Badge>
                                            {claim.approvedAmount && <Text fw={600} size="sm" mt={4}>{claim.approvedAmount} PLN</Text>}
                                        </Stack>
                                    </Group>
                                </Group>
                            </Paper>
                        ))}
                        {client.policies.every(p => p.claims.length === 0) && (
                            <Paper p="xl" withBorder radius="md">
                                <Text ta="center" c="dimmed">Brak zarejestrowanych szkód.</Text>
                            </Paper>
                        )}
                    </Stack>
                </Tabs.Panel>
            </Tabs>
        </Container>
    )
}

function DetailItem({ icon, label, value, badge }: { icon: any, label: string, value: string, badge?: any }) {
    return (
        <Group wrap="nowrap" align="start">
            <Text c="dimmed">{icon}</Text>
            <Stack gap={0}>
                <Group gap="xs">
                    <Text size="xs" c="dimmed" fw={700} tt="uppercase">{label}</Text>
                    {badge}
                </Group>
                <Text size="sm" fw={500}>{value}</Text>
            </Stack>
        </Group>
    )
}
