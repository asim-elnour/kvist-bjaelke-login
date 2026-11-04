import "./index.css";
import { useAuth } from "./auth/AuthProvider";
import { LoginPage } from "./pages/LoginPage";
import { UsersPage } from "./pages/UsersPage";

export function App() {
    const { token } = useAuth();

    return (
        <div className="app">
            <h1>Kvist & Bjælke</h1>
            {token ? <UsersPage /> : <LoginPage />}
        </div>
    );
}

export default App;
