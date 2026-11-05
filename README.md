# Kvist & Bjælke login platform

# CDS.security

## The platform

A web app where employees log in with a username and password and see a table of users. What else they can do depends on their role.

| Role | Can do |
|---|---|
| user | Read the users table |
| superuser | Read, and create, edit and delete users with the role user |
| admin | Read, and create, edit and delete everyone |

Nobody can sign up alone. The first admin comes from settings when the app starts. After that, only a logged-in superuser or admin can add people.

## Screenshots

These are from the client VM on VLAN 570.

The login page:

![Login page](images/client-login.png)

A wrong password gives an error:

![Wrong password](images/client-wrong-password.png)

An admin has buttons on everyone, but can't delete their own account:

![Admin](images/client-admin.png)

A superuser only has buttons on users:

![Superuser](images/client-superuser.png)

A user only sees the table:

![User](images/client-user.png)

## Technology

| Part | Choice | Why |
|---|---|---|
| Backend | C#, ASP.NET Web API, .NET 10 | JWT and role checks are built in |
| Frontend | React, TypeScript, Bun | Types catch mistakes before the page runs |
| Database | SQLite with EF Core | One file and no extra server. EF Core sends values as parameters, which protects against SQL injection |
| Hosting | Docker Compose | Runs the same on my PC and on the server |

## Security features

### Passwords

- Passwords are hashed with Argon2id: 64 MB of memory, 3 iterations, parallelism 4.
- Every password gets its own random 16-byte salt, so two users with the same password get different hashes.
- The database only stores `hash.salt`, never the password.
- The API never sends the hash back. Responses only have id, username and role.

### Login

- A correct login returns a JWT signed with HS256. It holds the user id and role, and it expires after 1 hour.
- A wrong username and a wrong password give the same error, so the page doesn't show which usernames exist.
- The token is kept in sessionStorage, and logging out removes it.

### Roles

- No token or a bad token gives 401. A forbidden action gives 403.
- One small method, `RoleRules.CanChange`, decides who may change whom. It checks the role of the person being changed, not only that you are logged in.
- An edit checks both the person's current role and the new role, so a superuser can't make someone an admin.
- The server reads your role from the database on every request, so a lowered role or a deleted account counts right away.
- You can't delete yourself or change your own role, so there is always an admin left.
- The page hides buttons you can't use, but the server is what really decides.

### Secrets

- The JWT secret and first admin password are in `.env`, which git ignores.
- The container runs as a normal user, not root.
- The OpenAPI page is only on in development.

## Alternatives

| What I did | Alternative | Why |
|---|---|---|
| Hashing with Argon2id | scrypt, bcrypt or PBKDF2 | Argon2id is usually ranked first, because cracking it takes a lot of memory as well as time |
| JWT for login | Sessions stored on the server | With sessions the server can end a login at once. A JWT keeps working until it expires, even after logout, but the server doesn't have to store it |
| Plain HTTP in the lab | HTTPS | HTTP sends passwords and tokens unencrypted over the network, so a real company would need HTTPS |
| SQLite | PostgreSQL | PostgreSQL handles many users at the same time better, but SQLite is enough for this small platform |
| One container for the page and API | A separate container for each | One container needs only one port and no CORS setup. Separate containers could be scaled on their own |

## Tests

Tests run on every pull request with GitHub Actions. There are unit tests for the role rules and hashing, and integration tests for login and the users API with an in-memory database.

## How to run it

1. Copy `.env.example` to `.env`.
2. Fill in `JwtSecret` with at least 32 random characters, and choose an `AdminPassword`.
3. Run `docker compose up -d --build`.
4. Open `http://<machine IP>:8000`.

## References

Anthropic. (2026). Claude (claude-opus-5-5) [Large language model]. https://claude.ai

Used as a helper during this assignment, because I worked alone.
