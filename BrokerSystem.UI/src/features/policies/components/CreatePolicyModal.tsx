import React from 'react';
import { Modal, Button, TextInput, NumberInput, Select, Stack, Group } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createPolicy, getPolicyLookups, CreatePolicyData } from '../api/policiesApi';
import { notifications } from '@mantine/notifications';

interface CreatePolicyModalProps {
    opened: boolean;
    onClose: () => void;
}

interface PolicyFormValues {
    policyNumber: string;
    clientId: string;
    policyTypeId: string;
    agentId: string;
    premiumAmount: number;
    sumInsured: number;
    startDate: Date | null;
    endDate: Date | null;
    paymentFrequency: string;
}

const CreatePolicyModal: React.FC<CreatePolicyModalProps> = ({ opened, onClose }) => {
    const queryClient = useQueryClient();

    const { data: lookups, isLoading: isLoadingLookups } = useQuery({
        queryKey: ['policyLookups'],
        queryFn: getPolicyLookups,
        enabled: opened,
    });

    const form = useForm<PolicyFormValues>({
        initialValues: {
            policyNumber: '',
            clientId: '',
            policyTypeId: '',
            agentId: '',
            premiumAmount: 0,
            sumInsured: 0,
            startDate: new Date(),
            endDate: new Date(new Date().setFullYear(new Date().getFullYear() + 1)),
            paymentFrequency: 'Annual',
        },
        validate: {
            policyNumber: (value) => (value.length < 3 ? 'Numer polisy jest za krótki' : null),
            clientId: (value) => (!value ? 'Wybierz klienta' : null),
            policyTypeId: (value) => (!value ? 'Wybierz typ polisy' : null),
            premiumAmount: (value) => (value <= 0 ? 'Kwota musi być większa od 0' : null),
        },
    });

    const mutation = useMutation({
        mutationFn: createPolicy,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['policies'] });
            notifications.show({
                title: 'Sukces',
                message: 'Polisa została utworzona pomyślnie',
                color: 'green',
            });
            form.reset();
            onClose();
        },
        onError: (error: any) => {
            notifications.show({
                title: 'Błąd',
                message: error.response?.data?.message || 'Wystąpił błąd podczas tworzenia polisy',
                color: 'red',
            });
        },
    });

    const handleSubmit = (values: PolicyFormValues) => {
        const apiData: CreatePolicyData = {
            ...values,
            clientId: parseInt(values.clientId),
            policyTypeId: parseInt(values.policyTypeId),
            agentId: parseInt(values.agentId),
            startDate: values.startDate?.toISOString() || '',
            endDate: values.endDate?.toISOString() || '',
        };
        mutation.mutate(apiData);
    };

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title="Nowa Polisa"
            size="lg"
            overlayProps={{
                backgroundOpacity: 0.55,
                blur: 3,
            }}
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <TextInput
                        label="Numer Polisy"
                        placeholder="np. POL/2024/001"
                        required
                        {...form.getInputProps('policyNumber')}
                    />

                    <Group grow>
                        <Select
                            label="Klient"
                            placeholder="Wybierz klienta"
                            data={lookups?.clients.map(c => ({ value: c.id.toString(), label: c.name })) || []}
                            searchable
                            required
                            disabled={isLoadingLookups}
                            {...form.getInputProps('clientId')}
                        />

                        <Select
                            label="Typ Polisy"
                            placeholder="Wybierz typ"
                            data={lookups?.policyTypes.map(t => ({ value: t.id.toString(), label: t.name })) || []}
                            required
                            disabled={isLoadingLookups}
                            {...form.getInputProps('policyTypeId')}
                        />
                    </Group>

                    <Group grow>
                        <NumberInput
                            label="Składka (PLN)"
                            placeholder="0.00"
                            decimalScale={2}
                            min={0}
                            required
                            {...form.getInputProps('premiumAmount')}
                        />
                        <NumberInput
                            label="Suma Ubezpieczenia"
                            placeholder="0.00"
                            decimalScale={2}
                            min={0}
                            required
                            {...form.getInputProps('sumInsured')}
                        />
                    </Group>

                    <Group grow>
                        <DateInput
                            label="Data Rozpoczęcia"
                            placeholder="Wybierz datę"
                            required
                            {...form.getInputProps('startDate')}
                        />
                        <DateInput
                            label="Data Zakończenia"
                            placeholder="Wybierz datę"
                            required
                            {...form.getInputProps('endDate')}
                        />
                    </Group>

                    <Select
                        label="Agent"
                        placeholder="Wybierz agenta"
                        data={lookups?.agents.map(a => ({ value: a.id.toString(), label: a.name })) || []}
                        required
                        disabled={isLoadingLookups}
                        {...form.getInputProps('agentId')}
                    />

                    <Select
                        label="Częstotliwość Płatności"
                        data={[
                            { value: 'Annual', label: 'Rocznie' },
                            { value: 'Monthly', label: 'Miesięcznie' },
                            { value: 'Quarterly', label: 'Kwartalnie' },
                        ]}
                        {...form.getInputProps('paymentFrequency')}
                    />

                    <Group justify="flex-end" mt="md">
                        <Button variant="subtle" onClick={onClose}>Anuluj</Button>
                        <Button type="submit" loading={mutation.isPending}>Utwórz Polisę</Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
};

export default CreatePolicyModal;
