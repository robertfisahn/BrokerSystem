import React, { useState } from 'react';
import {
    TextInput,
    PasswordInput,
    Paper,
    Title,
    Text,
    Container,
    Button,
    LoadingOverlay,
    Box
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../api/authApi';
import { useAuth } from '../../../providers/AuthProvider';

export const LoginPage: React.FC = () => {
    const [isLoading, setIsLoading] = useState(false);
    const { login } = useAuth();
    const navigate = useNavigate();

    const form = useForm({
        initialValues: {
            username: '',
            password: '',
        },

        validate: {
            username: (val) => (val.length <= 0 ? 'Wpisz nazwę użytkownika' : null),
            password: (val) => (val.length <= 0 ? 'Wpisz hasło' : null),
        },
    });

    const handleSubmit = async (values: typeof form.values) => {
        setIsLoading(true);
        try {
            const data = await authApi.login(values.username, values.password);

            login(data.token, {
                displayName: data.displayName,
                expiresAt: data.expiresAt,
                role: data.role,
                agentId: data.agentId
            });

            notifications.show({
                title: 'Zalogowano pomyślnie',
                message: `Witaj ponownie, ${data.displayName}!`,
                color: 'green',
            });

            navigate('/');
        } catch (error: unknown) {
            const axiosError = error as import('axios').AxiosError<{ error?: string }>;
            notifications.show({
                title: 'Błąd logowania',
                message: axiosError.response?.data?.error || 'Nieprawidłowy login lub hasło',
                color: 'red',
            });
            console.error('Login error', error);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Container size={420} my={80}>
            <Box pos="relative">
                <LoadingOverlay visible={isLoading} overlayProps={{ blur: 2 }} />
                <Title
                    ta="center"
                    style={{ fontWeight: 900 }}
                >
                    Witaj w BrokerSystem
                </Title>
                <Text c="dimmed" size="sm" ta="center" mt={5}>
                    Zaloguj się do swojego konta
                </Text>

                <Paper withBorder shadow="md" p={30} mt={30} radius="md">
                    <form onSubmit={form.onSubmit(handleSubmit)}>
                        <TextInput
                            label="Użytkownik"
                            placeholder="Twój login"
                            required
                            {...form.getInputProps('username')}
                        />
                        <PasswordInput
                            label="Hasło"
                            placeholder="Twoje hasło"
                            required
                            mt="md"
                            {...form.getInputProps('password')}
                        />
                        <Button fullWidth mt="xl" type="submit">
                            Zaloguj się
                        </Button>
                    </form>
                </Paper>

                <Text color="dimmed" size="xs" ta="center" mt="xl">
                    Przykładowe dane: <br />
                    Agent: <b>agent1</b> / <b>agent123</b> <br />
                    Admin: <b>admin</b> / <b>admin123</b>
                </Text>
            </Box>
        </Container>
    );
};

export default LoginPage;
