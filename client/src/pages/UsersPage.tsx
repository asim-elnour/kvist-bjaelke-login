import { useEffect, useState, type FormEvent } from "react";
import { useAuth } from "../auth/AuthProvider";
import type { User } from "../models/user";

const ROLES = ["user", "superuser", "admin"];

// The same rule as on the server. Here it only hides buttons; the server still decides.
function canChange(actorRole: string, targetRole: string) {
    if (actorRole === "admin") return true;
    if (actorRole === "superuser") return targetRole === "user";
    return false;
}

function errorMessage(status: number) {
    if (status === 400) return "Please fill in all the fields.";
    if (status === 403) return "You are not allowed to do that.";
    if (status === 404) return "That user no longer exists.";
    if (status === 409) return "That username is taken.";
    return "Something went wrong. Please try again.";
}

export function UsersPage() {
    const { api, logout } = useAuth();
    const [me, setMe] = useState<User | null>(null);
    const [users, setUsers] = useState<User[]>([]);
    const [error, setError] = useState("");

    // The form. editingId is null when it creates a new user.
    const [editingId, setEditingId] = useState<number | null>(null);
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [role, setRole] = useState("user");

    async function load() {
        const meResponse = await api("/api/users/me");
        const usersResponse = await api("/api/users");

        if (meResponse.ok && usersResponse.ok) {
            setMe(await meResponse.json());
            setUsers(await usersResponse.json());
        }
    }

    useEffect(() => {
        load().catch(() => setError("Could not reach the server."));
    }, []);

    function clearForm() {
        setEditingId(null);
        setUsername("");
        setPassword("");
        setRole("user");
    }

    function startEditing(user: User) {
        setEditingId(user.id);
        setUsername(user.username);
        setPassword("");
        setRole(user.role);
    }

    // Sends a change to the server, then reloads the table. Returns true if it worked.
    async function send(path: string, method: string, body?: object) {
        setError("");

        try {
            const response = await api(path, { method, body: JSON.stringify(body) });

            if (!response.ok) {
                setError(errorMessage(response.status));
                return false;
            }

            await load();
            return true;
        } catch {
            setError("Could not reach the server.");
            return false;
        }
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const body = { username, password, role };
        const saved = editingId === null
            ? await send("/api/users", "POST", body)
            : await send(`/api/users/${editingId}`, "PUT", body);

        if (saved) clearForm();
    }

    async function handleDelete(user: User) {
        if (window.confirm(`Delete ${user.username}?`)) {
            await send(`/api/users/${user.id}`, "DELETE");
        }
    }

    if (!me) return <p>{error || "Loading..."}</p>;

    // You can't change your own role, so editing yourself only offers the role you have.
    const roleOptions = editingId === me.id ? [me.role] : ROLES.filter(r => canChange(me.role, r));

    return (
        <div>
            <p>
                Logged in as <strong>{me.username}</strong> ({me.role}){" "}
                <button onClick={logout}>Log out</button>
            </p>

            <table>
                <thead>
                <tr>
                    <th>Username</th>
                    <th>Role</th>
                    <th></th>
                </tr>
                </thead>
                <tbody>
                {users.map(user => (
                    <tr key={user.id}>
                        <td>{user.username}</td>
                        <td>{user.role}</td>
                        <td>
                            {canChange(me.role, user.role) && (
                                <button onClick={() => startEditing(user)}>Edit</button>
                            )}{" "}
                            {canChange(me.role, user.role) && user.id !== me.id && (
                                <button onClick={() => handleDelete(user)}>Delete</button>
                            )}
                        </td>
                    </tr>
                ))}
                </tbody>
            </table>

            {error && <p className="error">{error}</p>}

            {canChange(me.role, "user") && (
                <form className="user-form" onSubmit={handleSubmit}>
                    <h2>{editingId === null ? "Add a user" : "Edit user"}</h2>

                    <label htmlFor="username">Username</label>
                    <input
                        id="username"
                        value={username}
                        onChange={event => setUsername(event.target.value)}
                        required
                    />

                    <label htmlFor="password">
                        {editingId === null ? "Password" : "New password (leave empty to keep the old one)"}
                    </label>
                    <input
                        id="password"
                        type="password"
                        autoComplete="new-password"
                        value={password}
                        onChange={event => setPassword(event.target.value)}
                        required={editingId === null}
                    />

                    <label htmlFor="role">Role</label>
                    <select id="role" value={role} onChange={event => setRole(event.target.value)}>
                        {roleOptions.map(option => (
                            <option key={option}>{option}</option>
                        ))}
                    </select>

                    <button type="submit">Save</button>
                    {editingId !== null && (
                        <button type="button" onClick={clearForm}>
                            Cancel
                        </button>
                    )}
                </form>
            )}
        </div>
    );
}
