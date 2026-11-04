import "./index.css";
import { useState, type FormEvent } from "react";
import { useAuth } from "./auth/AuthProvider";

export function App() {
    const { token, login, logout } = useAuth();
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError("");

        try {
            await login({ username, password });
            setPassword("");
        } catch (error) {
            setError(error instanceof Error ? error.message : "Login failed.");
        }
    }

    return (
        <div className="app">
            <h1>Kvist & Bjælke</h1>

            {token ? (
                <div>
                    <p>You are logged in.</p>
                    <button onClick={logout}>Log out</button>
                </div>
            ) : (
                <form className="login-form" onSubmit={handleSubmit}>
                    <label htmlFor="username">Username</label>
                    <input
                        id="username"
                        autoComplete="username"
                        value={username}
                        onChange={event => setUsername(event.target.value)}
                        required
                    />

                    <label htmlFor="password">Password</label>
                    <input
                        id="password"
                        type="password"
                        autoComplete="current-password"
                        value={password}
                        onChange={event => setPassword(event.target.value)}
                        required
                    />

                    {error && <p className="error">{error}</p>}

                    <button type="submit">Log in</button>
                </form>
            )}
        </div>
    );
}

export default App;
