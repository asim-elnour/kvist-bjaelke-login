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

# CDS.infrastructure

## IP plan

I split the company into VLANs, so each type of device has its own network. All addresses are private (10.x.x.x). VLAN 570 and 571 were given, and I followed the same pattern for the rest.

| VLAN | Network | Gateway | Devices | Addresses | Why it's separate |
|---|---|---|---|---|---|
| 570 Clients | 10.5.70.0/24 | 10.5.70.5 | 15 desktops, 10 laptops | DHCP | Normal work |
| 571 Servers | 10.5.71.0/24 | 10.5.71.5 | 3 servers | Static | Valuable data |
| 572 Printers | 10.5.72.0/28 | 10.5.72.5 | 3 printers | Static | Often poorly secured |
| 573 Phones | 10.5.73.0/27 | 10.5.73.5 | 12 IP phones | DHCP | Calls need priority |
| 574 Cameras | 10.5.74.0/28 | 10.5.74.5 | 6 cameras | Static | Private video |
| 575 Guests | 10.5.75.0/24 | 10.5.75.5 | Visitors' devices | DHCP | Can't be trusted |
| 576 Management | 10.5.76.0/27 | 10.5.76.5 | Access points, switches, router, firewall | Static | Only IT should reach it |

Devices that others must always find get static addresses. Devices that come and go use DHCP.

## Subnetting

| Network | Address | Mask | Hosts | Devices today |
|---|---|---|---|---|
| Phones | 10.5.73.0 | 255.255.255.224 (/27) | 30 | 12 |
| Cameras | 10.5.74.0 | 255.255.255.240 (/28) | 14 | 6 |

Both sizes fit today's devices, with room to double them and still have an address for the gateway. Clients and guests get a /24 with 254 hosts, because their numbers change the most.

## Network diagram

```mermaid
flowchart TB
    Internet((Internet)) --- Router[Router]
    Router --- Firewall[Firewall and gateway]
    Firewall ---|Trunk| Core[Core switch]
    Core ---|Trunk| Office[Office switch]
    Core --- Servers["VLAN 571 Servers<br/>10.5.71.0/24<br/>3 servers, one runs the login platform"]
    Office --- Clients["VLAN 570 Clients<br/>10.5.70.0/24<br/>15 desktops, 10 laptops"]
    Office --- Printers["VLAN 572 Printers<br/>10.5.72.0/28<br/>3 printers"]
    Office --- Phones["VLAN 573 Phones<br/>10.5.73.0/27<br/>12 IP phones"]
    Office --- Cameras["VLAN 574 Cameras<br/>10.5.74.0/28<br/>6 cameras"]
    Office --- APs["VLAN 576 Management<br/>10.5.76.0/27<br/>4 access points"]
    APs -.->|Staff Wi-Fi| Clients
    APs -.->|Guest Wi-Fi| Guests["VLAN 575 Guests<br/>10.5.75.0/24<br/>Guest devices"]
```

## Firewall and VLANs

**Why are VLAN 570 and 571 separate?** So a client with malware isn't on the same network as the servers.

**Why does traffic between VLANs need routing?** Each VLAN is its own network, so a switch can't pass traffic between them. The gateway routes it.

**What does the firewall do?** Traffic between the VLANs goes through it, and it decides what is allowed. Here it allows VLAN 570 to reach VLAN 571 on TCP 8000-8100.

**Why allow only specific ports?** Every open port is a possible way in, so only the ports the platform needs are open.



## References

Anthropic. (2026). Claude (claude-opus-5-5) [Large language model]. https://claude.ai

Used as a helper during this assignment, because I worked alone.
