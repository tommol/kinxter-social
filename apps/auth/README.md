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
OAuth 2.0/OpenID Connect clients, and users of the `backoffice` realm. Realm
and client changes are applied to the running auth service immediately and
remain active after a restart.

### Backoffice users

Admin application users are regular identity users isolated in the
`backoffice` realm. They are separate from the `AuthAdministrator` accounts
that protect `/control`. Public signup remains disabled for this realm.

Use the backoffice realm page in `/control` to invite a user, copy the one-time
activation link, and assign one or more least-privilege roles:

- `super_admin`: full backoffice access and administrator management
- `ops`: monitoring and operational diagnostics
- `moderator`: content and community moderation
- `support`: user support and account management
- `read_only`: read-only monitoring, moderation, and user access

The legacy `admin` role remains recognized for existing deployments, but it is
not offered for new assignments. Roles are translated into `permission`
claims in access tokens. API endpoints authorize permissions in addition to
the `backoffice` realm and `kinxter.admin` scope.

Changing roles, disabling an account, resetting MFA, or manually revoking
sessions rotates the ASP.NET Identity security stamp and revokes all stored
OpenIddict tokens and authorizations for that user. Backoffice access tokens
live for five minutes and refresh tokens for eight hours; a disabled or deleted
user cannot use a refresh token to obtain a new access token.

## OIDC clients

Client registrations are stored in PostgreSQL. `AuthClients` keeps the realm
assignment and administrative metadata, while OpenIddict's
`OpenIddictApplications` table keeps the protocol registration and hashed
client secret.

Clients configured under `Auth:Realms:*:Clients` are bootstrap data only. A
missing client is created on startup, but an existing database registration is
never overwritten from configuration. Further changes belong in `/control`.

The control plane can create and edit clients, enable or disable them, select
their allowed scopes, and combine these supported flows:

- Authorization Code with mandatory PKCE
- Refresh Token (with Authorization Code or Device Code)
- Client Credentials
- Device Authorization

Clients can be `Public` (SPA, native app, CLI or device) or `Confidential`
(server-side web app or service). Public clients never receive a secret and
cannot use Client Credentials. Confidential clients receive a generated
secret that is displayed once and stored only as a hash. Changing a public
client to confidential generates a new secret; changing it back to public
removes the old secret. Secret rotation is available only to confidential
clients and invalidates the previous value immediately.

Update the application's `AUTH_CLIENT_SECRET` after creating a confidential
client, changing its type to confidential, or rotating its secret. Web
applications also need the realm issuer and client ID:

```dotenv
AUTH_ISSUER=http://localhost:8081/realms/kinxter
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

Interactive authorization requests may include the standard `ui_locales`
parameter (`pl` or `en`). Kinxter.Auth keeps that locale across login,
registration, validation errors, and MFA screens. The public web client also
uses `screen_hint=signup` when a user selects a registration CTA so the same
PKCE authorization flow opens on account creation instead of sign-in.

OAuth-only machine clients may omit the `openid` scope. A typical token request
for a confidential Client Credentials client is:

```bash
curl --user "$CLIENT_ID:$CLIENT_SECRET" \
  --data-urlencode grant_type=client_credentials \
  --data-urlencode scope=kinxter.api \
  "$AUTH_ISSUER/connect/token"
```

Device clients start at `$AUTH_ISSUER/connect/device`; the returned
`verification_uri` points to the realm-specific device approval screen.
Implicit and Resource Owner Password flows are intentionally not enabled.
