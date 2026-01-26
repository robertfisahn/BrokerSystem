import { Container, Title, Text, Paper } from '@mantine/core'
import { AlertTriangle } from 'lucide-react'

export function ClaimsDashboard() {
    return (
        <Container size="xl" py="xl">
            <Title order={1} mb="sm">
                <AlertTriangle size={32} style={{ verticalAlign: 'middle', marginRight: 8 }} />
                Szkody
            </Title>
            <Text c="dimmed" mb="xl">
                Zarządzanie zgłoszeniami szkód i roszczeniami.
            </Text>

            <Paper p="xl" radius="md" withBorder>
                <Text c="dimmed" ta="center" fs="italic">
                    🚧 Ta sekcja jest w budowie. Wróć wkrótce!
                </Text>
            </Paper>
        </Container>
    )
}
