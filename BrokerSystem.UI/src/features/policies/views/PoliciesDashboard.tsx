import { Container, Title, Text, Paper } from '@mantine/core'
import { FileText } from 'lucide-react'

export function PoliciesDashboard() {
    return (
        <Container size="xl" py="xl">
            <Title order={1} mb="sm">
                <FileText size={32} style={{ verticalAlign: 'middle', marginRight: 8 }} />
                Polisy
            </Title>
            <Text c="dimmed" mb="xl">
                Zarządzanie polisami ubezpieczeniowymi.
            </Text>

            <Paper p="xl" radius="md" withBorder>
                <Text c="dimmed" ta="center" fs="italic">
                    🚧 Ta sekcja jest w budowie. Wróć wkrótce!
                </Text>
            </Paper>
        </Container>
    )
}
