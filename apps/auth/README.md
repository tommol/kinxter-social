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

The panel manages persisted realm routing, MFA policy, signup availability,
and confidential OIDC clients. Realm and client changes are applied to the
running auth service immediately and remain active after a restart.

## OIDC clients

Client registrations are stored in PostgreSQL. `AuthClients` keeps the realm
assignment and administrative metadata, while OpenIddict's
`OpenIddictApplications` table keeps the protocol registration and hashed
client secret.

Clients configured under `Auth:Realms:*:Clients` are bootstrap data only. A
missing client is created on startup, but an existing database registration is
never overwritten from configuration. Further changes belong in `/control`.

The control plane can create and edit clients, enable or disable them, and
rotate their secret. A generated secret is displayed once. Update the
application's `AUTH_CLIENT_SECRET` after creating a client or rotating its
secret. Web applications also need the realm issuer and client ID:

```dotenv
AUTH_ISSUER=http://localhost:8081/realms/public
AUTH_CLIENT_ID=kinxter-web
AUTH_CLIENT_SECRET=copy-the-value-shown-by-control
```

Plain HTTP is accepted automatically by the Next.js clients in development.
For an intentional non-production `next start` deployment, set
`AUTH_ALLOW_INSECURE_HTTP=true`. Production issuers should use HTTPS.

The Next.js web and admin applications use `openid-client` for OIDC discovery,
Authorization Code with PKCE, callback and ID Token validation, and
RP-Initiated Logout. Client IDs are stable and cannot be renamed in the panel;
create a replacement registration when a new identifier is needed.
