# Kinxter.Auth

## Control plane

Kinxter.Auth includes an optional administrative control plane at `/control`.
It uses its own administrator table, authentication scheme, and cookie, so a
session in any auth realm never grants control-plane access.

Local development enables the panel through `pnpm dev` with:

- URL: `http://localhost:8081/control`
- username: `admin`
- password: `kinxter-control-dev-password`

The bootstrap credentials create the administrator only when it does not
already exist. Changing the environment variables later does not rotate an
existing password.

For a deployed environment, the panel is disabled by default. Enable it with a
unique bootstrap password of at least 12 characters:

```dotenv
AUTH_ADMIN_ENABLED=true
AUTH_ADMIN_BOOTSTRAP_USERNAME=admin
AUTH_ADMIN_BOOTSTRAP_PASSWORD=replace-with-a-unique-secret
```

The current panel manages persisted realm routing, MFA policy, and signup
availability. Realm changes are applied to the running auth service
immediately and remain active after a restart.
