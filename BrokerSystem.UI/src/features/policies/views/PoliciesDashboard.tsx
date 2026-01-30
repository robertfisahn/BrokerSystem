import { Container, Title, Text, Card, Table, Pagination, Group, TextInput, Badge, ActionIcon, Menu, Button, Loader } from '@mantine/core'
import { FileText, Search, MoreVertical, Download, Eye, Plus } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { getPolicies, GetPoliciesParams, exportPolicy } from '../api/policiesApi'
import { useDisclosure } from '@mantine/hooks'
import CreatePolicyModal from '../components/CreatePolicyModal';
import { PolicyPreviewModal } from '../components/PolicyPreviewModal';

export function PoliciesDashboard() {
    const [opened, { open, close }] = useDisclosure(false);
    const [previewOpened, { open: openPreview, close: closePreview }] = useDisclosure(false);
    const [selectedPolicy, setSelectedPolicy] = useState<{ id: number, number: string } | null>(null);
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState('');
    const pageSize = 10;

    const params: GetPoliciesParams = {
        page,
        pageSize,
        search: search || undefined
    };

    const { data, isLoading, error } = useQuery({
        queryKey: ['policies', params],
        queryFn: () => getPolicies(params)
    });

    return (
        <Container size="xl" py="xl">
            <Group justify="space-between" mb="xl">
                <div>
                    <Title order={1} mb="xs">
                        <FileText size={32} style={{ verticalAlign: 'middle', marginRight: 12, color: '#228be6' }} />
                        Polisy
                    </Title>
                    <Text c="dimmed">Przeglądaj i zarządzaj umowami ubezpieczeniowymi klientów.</Text>
                </div>
                <Button leftSection={<Plus size={18} />} variant="filled" onClick={open}>
                    Nowa Polisa
                </Button>
            </Group>

            <Card withBorder radius="md" p="md" mb="xl">
                <TextInput
                    placeholder="Szukaj po numerze polisy lub nazwisku klienta..."
                    leftSection={<Search size={16} />}
                    mb="md"
                    value={search}
                    onChange={(e) => {
                        setSearch(e.currentTarget.value);
                        setPage(1);
                    }}
                />

                <Table.ScrollContainer minWidth={800}>
                    <Table verticalSpacing="sm" highlightOnHover>
                        <Table.Thead>
                            <Table.Tr>
                                <Table.Th>Numer Polisy</Table.Th>
                                <Table.Th>Klient</Table.Th>
                                <Table.Th>Typ</Table.Th>
                                <Table.Th ta="right">Suma (Składka)</Table.Th>
                                <Table.Th ta="right">Okres Od</Table.Th>
                                <Table.Th ta="right">Okres Do</Table.Th>
                                <Table.Th ta="center">Status</Table.Th>
                                <Table.Th ta="center">Akcje</Table.Th>
                            </Table.Tr>
                        </Table.Thead>
                        <Table.Tbody>
                            {isLoading ? (
                                <Table.Tr>
                                    <Table.Td colSpan={8} ta="center" py="xl">
                                        <Group justify="center">
                                            <Loader size="sm" />
                                            <Text size="sm">Ładowanie danych...</Text>
                                        </Group>
                                    </Table.Td>
                                </Table.Tr>
                            ) : error ? (
                                <Table.Tr>
                                    <Table.Td colSpan={8} ta="center" py="xl">
                                        <Text c="red">Wystąpił błąd podczas pobierania danych.</Text>
                                    </Table.Td>
                                </Table.Tr>
                            ) : !data || data.items.length === 0 ? (
                                <Table.Tr>
                                    <Table.Td colSpan={8} ta="center" py="xl">Nie znaleziono żadnych polis.</Table.Td>
                                </Table.Tr>
                            ) : data.items.map((policy) => (
                                <Table.Tr key={policy.policyId}>
                                    <Table.Td fw={500} style={{ fontFamily: 'monospace' }}>{policy.policyNumber}</Table.Td>
                                    <Table.Td>{policy.clientName}</Table.Td>
                                    <Table.Td>
                                        <Text size="sm" c="dimmed">{policy.policyType}</Text>
                                    </Table.Td>
                                    <Table.Td ta="right" fw={700} c="blue">
                                        {policy.totalPremium.toLocaleString('pl-PL', { style: 'currency', currency: 'PLN' })}
                                    </Table.Td>
                                    <Table.Td ta="right" style={{ whiteSpace: 'nowrap' }}>
                                        {new Date(policy.startDate).toLocaleDateString()}
                                    </Table.Td>
                                    <Table.Td ta="right" style={{ whiteSpace: 'nowrap' }}>
                                        {new Date(policy.endDate).toLocaleDateString()}
                                    </Table.Td>
                                    <Table.Td ta="center">
                                        <Badge
                                            color={policy.status === 'Active' ? 'green' : 'blue'}
                                            variant="dot"
                                            size="lg"
                                        >
                                            {policy.status}
                                        </Badge>
                                    </Table.Td>
                                    <Table.Td ta="center">
                                        <Menu shadow="md" width={200} position="bottom-end">
                                            <Menu.Target>
                                                <ActionIcon variant="light" color="gray">
                                                    <MoreVertical size={16} />
                                                </ActionIcon>
                                            </Menu.Target>

                                            <Menu.Dropdown>
                                                <Menu.Label>Zarządzanie</Menu.Label>
                                                <Menu.Item
                                                    leftSection={<Eye size={14} />}
                                                    onClick={() => {
                                                        setSelectedPolicy({ id: policy.policyId, number: policy.policyNumber });
                                                        openPreview();
                                                    }}
                                                >
                                                    Szczegóły / Podgląd
                                                </Menu.Item>
                                                <Menu.Item
                                                    leftSection={<Download size={14} />}
                                                    color="blue"
                                                    onClick={() => exportPolicy(policy.policyId)}
                                                >
                                                    Pobierz (PDF)
                                                </Menu.Item>
                                            </Menu.Dropdown>
                                        </Menu>
                                    </Table.Td>
                                </Table.Tr>
                            ))}
                        </Table.Tbody>
                    </Table>
                </Table.ScrollContainer>

                <Group justify="center" mt="xl">
                    <Pagination
                        total={Math.ceil((data?.totalCount || 0) / pageSize)}
                        value={page}
                        onChange={setPage}
                        mt="md"
                    />
                </Group>
            </Card>
            <CreatePolicyModal opened={opened} onClose={close} />
            <PolicyPreviewModal
                opened={previewOpened}
                onClose={closePreview}
                policyId={selectedPolicy?.id || null}
                policyNumber={selectedPolicy?.number}
            />
        </Container>
    );
}
