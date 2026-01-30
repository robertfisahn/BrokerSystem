import { Modal, Button, Group, Loader, Center, Text, Stack, Badge } from '@mantine/core';
import { Download } from 'lucide-react';
import { useEffect, useState } from 'react';
import { getPolicyPdfUrl } from '../api/policiesApi';

interface PolicyPreviewModalProps {
    opened: boolean;
    onClose: () => void;
    policyId: number | null;
    policyNumber?: string;
}

export function PolicyPreviewModal({ opened, onClose, policyId, policyNumber }: PolicyPreviewModalProps) {
    const [pdfUrl, setPdfUrl] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (opened && policyId) {
            loadPdf();
        }

        return () => {
            if (pdfUrl) {
                window.URL.revokeObjectURL(pdfUrl);
            }
        };
    }, [opened, policyId]);

    const loadPdf = async () => {
        if (!policyId) return;
        setLoading(true);
        try {
            const url = await getPolicyPdfUrl(policyId);
            setPdfUrl(url);
        } catch (error) {
            console.error('Failed to load PDF', error);
        } finally {
            setLoading(false);
        }
    };

    return (
        <Modal
            opened={opened}
            onClose={onClose}
            title={
                <Group gap="xs">
                    <Text fw={700}>Podgląd Dokumentu</Text>
                    {policyNumber && <Badge variant="light" color="blue">Polisa: {policyNumber}</Badge>}
                </Group>
            }
            size="xl"
            padding={0}
            styles={{
                header: { padding: '16px 20px', borderBottom: '1px solid #e9ecef' },
                body: { height: '80vh', overflow: 'hidden' }
            }}
        >
            {loading ? (
                <Center h="100%">
                    <Stack align="center" gap="xs">
                        <Loader size="lg" />
                        <Text size="sm" c="dimmed">Generowanie podglądu PDF...</Text>
                    </Stack>
                </Center>
            ) : pdfUrl ? (
                <Stack h="100%" gap={0}>
                    <Group justify="space-between" p="xs" bg="gray.0" style={{ borderBottom: '1px solid #e9ecef' }}>
                        <Text size="sm" c="dimmed" ml="xs">Podgląd Certyfikatu Ubezpieczeniowego</Text>
                        <Group gap="xs">
                            <Button
                                variant="light"
                                color="blue"
                                size="xs"
                                leftSection={<Download size={14} />}
                                component="a"
                                href={pdfUrl}
                                download={`Polisa_${policyNumber || policyId}.pdf`}
                            >
                                Pobierz PDF
                            </Button>
                        </Group>
                    </Group>
                    <iframe
                        id="pdf-preview-iframe"
                        src={`${pdfUrl}#toolbar=1&navpanes=0&scrollbar=1`}
                        style={{ width: '100%', height: '100%', border: 'none' }}
                        title="PDF Preview"
                    />
                </Stack>
            ) : (
                <Center h="100%">
                    <Text c="red">Błąd podczas ładowania dokumentu.</Text>
                </Center>
            )}
        </Modal>
    );
}
