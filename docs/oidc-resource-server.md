# OpenID Connect resource server

How to let APIs, integrations, applications and MCP-class clients call GrandNode with access tokens issued by an
external OpenID Connect provider. Guides for Keycloak 26.8 and Zitadel (v4.19) and a Docker Compose test environment
are included.

## 1. What it does

GrandNode acts only as an OAuth 2.1 **resource server**. It validates access tokens (JWT) issued by one external
identity provider and never issues tokens, registers clients or shows a consent screen.

* A token identifies a **customer**; the customer's **groups and permissions in GrandNode** decide what the call may do.
* Protected resources are configured in `appsettings.json` (the Web API, `/api`) or registered by plugins (their own
  endpoints, for example a server for MCP-class clients).
* GrandNode publishes RFC 9728 protected resource metadata, so clients discover which provider to use. A 401 carries
  a `WWW-Authenticate` header with `resource_metadata`.
* Off by default (`OidcResourceServer:Enabled = false`): no scheme, no metadata endpoints, no behavior change.
* One identity provider at a time. Not covered here: opaque tokens (see [Introspection](#8-introspection)) and
  login to the admin panels. Browser sign-in of storefront users is a separate plugin
  ([section 11](#11-browser-sign-in-openid-connect-login-plugin)).

Accepted signing algorithms: RS256/384/512, PS256/384/512, ES256/384/512. Tokens signed with HS\*, or with `alg: none`,
are refused by the OpenID Connect scheme.

## 2. Configuration reference

Section `OidcResourceServer` of `appsettings.json` (environment variables use `__`, for example
`OidcResourceServer__Authority`).

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | Turns the feature on. |
| `Authority` | empty | The provider's **issuer**. Discovery document: `{Authority}/.well-known/openid-configuration`. Must equal the `issuer` value of that document and the `iss` of the tokens, character for character (see [Routing](#46-routing-of-tokens)). Absolute URL, `https` outside Development. |
| `RequireHttpsMetadata` | `true` | Require https for the discovery document. Set `false` only for an `http` Authority in Development. |
| `ClockSkewSeconds` | `60` | Tolerated clock difference when checking `exp`/`nbf`. 0-300. |
| `Claims:Subject` | `sub` | Claim holding the stable user id. |
| `Claims:Email` | `email` | Claim holding the e-mail address. |
| `Claims:EmailVerified` | `email_verified` | Claim holding the verification flag (JSON boolean or the string `true`, case-insensitive). |
| `RequireEmailVerified` | `true` | An unverified e-mail never links to an account and never registers one. See [linking rules](#31-linking-rules). |
| `AutoRegister` | `false` | Register an unknown user on first use (customer group **Registered** only). |
| `Resources` | one entry for `/api` | Protected resources, below. |

Each entry of `Resources` (plugins register the same object, section 5):

| Key | Default | Meaning |
|---|---|---|
| `Name` | required | Unique name, for example `BackendApi`. |
| `PathPrefix` | required | Starts with `/`, not `/` alone. Matched case-insensitively on a segment boundary: `/api` covers `/api` and `/api/x`, not `/apix`. Prefixes must not overlap. |
| `Resource` | empty | Canonical resource URI (RFC 8707), absolute, no fragment. Empty: the audience is the **store URL + `PathPrefix`**, see below. |
| `Audiences` | `[]` | Extra accepted `aud` values, for example a Zitadel project id. |
| `AuthorizedParties` | `[]` | If not empty, the token's `azp` (fallback `client_id`) must be listed. |
| `ScopesSupported` | `[]` | Advertised in the metadata document. |
| `RequiredScopes` | `[]` | Every listed scope must be in the token's `scope` (space separated) or `scp` (string or repeated claim). Compared exactly. Missing: 403 `insufficient_scope`. |
| `RequireAdminPanelAccess` | `false` | The customer needs the `ManageAccessAdminPanel` permission. |
| `DenyVendors` | `false` | Refuse customers that belong to a vendor. |
| `DenyStoreManagers` | `false` | Refuse store manager accounts. |

`offline_access` is never advertised in `scopes_supported` or in a challenge `scope`.

Startup validation (outside Development the application refuses to start, in Development it warns): `Authority`
absolute, https (an `http` Authority is accepted in Development only, with `RequireHttpsMetadata = false`); at least one resource (configuration or plugins); each resource has a name and a valid prefix;
names and prefixes unique and not nested; `Resource` absolute without fragment; `ClockSkewSeconds` 0-300; claim names
not empty.

### Audience

A token is accepted when its `aud` contains the resource URI or one of `Audiences`.

* With an empty `Resource` the resource URI is the **URL of the GrandNode store serving the request host** (field
  *Store URL* in the admin panel) plus `PathPrefix`, for example `https://shop.example.com/api`. The store is the one
  whose *Hosts* list the request host, compared with its port first (`localhost:44350`, as the installer records the
  host of the install request) and then without it (`shop.example.com`); the store cookie is never used. When no
  store lists the host, a single-store installation uses its only store; with several stores the request has no
  audience and every token is rejected (and the metadata endpoints answer 404). The URI itself always comes from the *Store URL*, never from the `Host`
  header. A request whose token is accepted runs in that same store (prices, settings, per-store accounts), even when
  it also carries another store's cookie; without an accepted token the store cookie keeps its usual priority.
* Behind a TLS-terminating proxy the *Store URL* must be the public `https://` URL, or the audience will not match.
  Also set `Security:UseForwardedHeaders = true` so that `X-Forwarded-Proto` makes the request scheme `https`
  (ASP.NET Core applies it only from a trusted proxy, loopback by default): the metadata endpoint picks between
  resources sharing a path by the scheme and host the request reached, and an `http` scheme there does not match an
  `https` resource URI. `X-Forwarded-Host` is not applied, so the proxy has to pass the original `Host` header, the
  one listed in the store's *Hosts*.
* Multi-store: every store has its own audience (`https://shop-a.example.com/api`, `https://shop-b.example.com/api`),
  chosen by the host the request reached, so a token issued for store B is not accepted on store A's host. To accept
  one audience only, set `Resource` explicitly.
* A store without a *Store URL* and a resource without `Resource` have no resource URI: tokens are rejected and the
  metadata endpoint answers 404, with a warning in the log naming the resource. A request for a host no store serves
  also gets 404, logged at Debug level only, since any anonymous request can name such a host.
* Comparison is canonical: scheme and host lower-case, default port dropped, no trailing slash, path exact.

### Example for the Web API

```json
"OidcResourceServer": {
  "Enabled": true,
  "Authority": "https://idp.example.com/realms/shop",
  "RequireEmailVerified": true,
  "AutoRegister": false,
  "Resources": [
    {
      "Name": "BackendApi",
      "PathPrefix": "/api",
      "Resource": "https://shop.example.com/api",
      "ScopesSupported": [ "grandnode.api" ],
      "RequiredScopes": [ "grandnode.api" ],
      "RequireAdminPanelAccess": true
    }
  ]
}
```

**Always set `RequiredScopes`** (and, with client-id style audiences such as Zitadel's, `AuthorizedParties`). With
client-id style audiences an ID token of the same client could otherwise pass the audience check. `RequiredScopes`
is only as strong as the provider's rule for issuing the scope: a provider that puts the scope into every access token
of its instance turns it into a formality (see Zitadel, section 7, for what gates access there).

## 3. From token to customer

After the signature, issuer, lifetime, audience, authorized party and scopes are valid, the token is mapped to a
customer in this order.

### 3.1 Linking rules

1. No `sub` claim: 401.
2. A stored link for `sub` (provider `ExternalAuth.OpenIdConnect`, exact, case-sensitive match) selects the customer.
   The customer must be active, not deleted, registered, and not a system account; otherwise 403. An inactive customer
   is never reactivated.
3. Without a link the e-mail claim is needed (missing: 401). With `RequireEmailVerified = true` it must be verified
   (otherwise 401).
4. A customer with the same e-mail address is linked on first use, and the link is stored.
   * With `RequireEmailVerified = true` this **includes back-office accounts** (administrators, vendors, store staff):
     the identity provider is trusted for verified e-mail addresses. Secure the provider accordingly (verify e-mail
     addresses, restrict self-registration and e-mail changes).
   * With `RequireEmailVerified = false` back-office accounts are **never** linked by e-mail.
5. System accounts and guests are never mapped.
6. One identity per customer: when the customer is already linked to another `sub`, the new subject is refused (403).
   Links do not record the provider instance, so after switching `Authority` to another provider the customers
   linked by the previous one are refused until their link is removed.
7. With `RegisterCustomersPerStore` enabled an account of another store is refused (403) and lookups are store-scoped.

**Per-store identity.** With `RegisterCustomersPerStore` enabled the same person can have a separate customer in each
store, so one `sub` gets one link per store: the link records the store of the linked customer, and a request uses the
link of its own store. In a store where the subject has no link yet, the rules above link (or register) that store's
customer. A customer of no store, such as a back-office account, has one link without a store, and it works in every
store. Without per-store identity a link has no store and is used in every store, as before.

### 3.2 Automatic registration

`AutoRegister` is `false` by default. When no customer matches, the answer is 403 "No customer account matches the token"; the
customer has to exist already (it is then linked by the verified e-mail, section 3.1). Set `AutoRegister = true` to opt in to registering
an unknown user on first use; then:

* the new customer gets the **Registered** group only, never an admin or vendor group;
* store registration set to *Disabled* gives 403;
* the e-mail address becomes the user name; when that user name is already taken, 403;
* the account is created with a random password; the user does not sign in with it.

### 3.3 Granting access

A token alone grants no rights. To let a user call the API:

1. let the provider issue the user a token with the `grandnode.api` scope (provider side, below);
2. in GrandNode add the (auto-registered or existing) customer to a **customer group** whose permissions include
   *Access admin panel* and the permissions the calls need (or use a group created for API access).

### 3.4 Resource conditions

After the mapping the resource's conditions apply; a refusal is 403 without `WWW-Authenticate`:
`DenyVendors`, `DenyStoreManagers`, `RequireAdminPanelAccess`.

### 3.5 Web API (`/api`)

* OIDC tokens are accepted on `/api` **next to** the existing `SecretKey` tokens, which keep working.
* Access still requires the `ManageAccessAdminPanel` permission and passes the admin IP allow-list.
* The per-user *API users* opt-in does **not** apply to OIDC tokens. Who may call the API is decided by which users
  the provider grants the `grandnode.api` scope, plus their GrandNode groups.
* `BackendAPI:Enabled = false` switches the Web API off for both token kinds. An OIDC-only setup still needs
  `BackendAPI:Enabled = true` and a strong `BackendAPI:SecretKey` (at least 32 characters; startup check outside
  Development), although no SecretKey token has to be issued.
* The Web API module itself is off by default: set `FeatureManagement:Grand.Module.Api = true` (environment variable
  `FeatureManagement__Grand.Module.Api`). Without it `/api/...` answers with the storefront's 404 page.

## 4. Errors, metadata, routing

### 4.1 Errors

| Situation | Status | `WWW-Authenticate` |
|---|---|---|
| No token on a resource | 401 | `Bearer resource_metadata="...", scope="<required scopes>"` |
| Bad signature, issuer, lifetime or algorithm; wrong audience or authorized party; missing `sub` or e-mail; unverified e-mail | 401 | `Bearer error="invalid_token", error_description="...", resource_metadata="..."` |
| Required scope missing | 403 | `Bearer error="insufficient_scope", error_description="...", scope="<all required scopes>", resource_metadata="..."` |
| Customer inactive or deleted, registration off, link conflict, resource condition refused | 403 | none |
| GrandNode permission check inside the endpoint | 403 | unchanged |
| The provider's discovery document or signing keys cannot be obtained (provider down, wrong `Authority`) | 503 | none |

The body is `application/problem+json` (ProblemDetails). `error_description` is a fixed text; a token is never logged
or echoed. The 503 carries `Retry-After: 30` and is logged at Error level with the metadata address; clients keep
their token and retry, instead of discarding a valid token as `invalid_token`. The provider is tried again on the next
request, so tokens are judged normally as soon as it answers.

### 4.2 Metadata URLs

| URL | Document |
|---|---|
| `/.well-known/oauth-protected-resource{path of the resource URI}` | Metadata of one resource, for example `/.well-known/oauth-protected-resource/api` for `https://shop.example.com/api` |
| `/.well-known/oauth-protected-resource` | Store root: `resource` is the store URL, scopes are the union of all `ScopesSupported`. 404 when the store URL has a path (`https://example.com/shop`): the root URL describes the bare host (RFC 9728 section 3.3), and that store is described at `/.well-known/oauth-protected-resource/shop` |

As RFC 9728 section 3.1 requires, the well-known segment goes between the host and the path of the resource URI, at
the host of that URI:

| Resource URI | Metadata URL |
|---|---|
| `https://shop.example.com/api` (store URL + `PathPrefix`) | `https://shop.example.com/.well-known/oauth-protected-resource/api` |
| `https://example.com/shop/api` (store under `/shop`) | `https://example.com/.well-known/oauth-protected-resource/shop/api` |
| `https://api.example.com/v1` (explicit `Resource`) | `https://api.example.com/.well-known/oauth-protected-resource/v1` |

The `resource_metadata` value of the challenge is this URL. A request is matched on the path of each resource URI;
when two resources share a path, the one on the host the request reached wins. With an explicit `Resource` on another
host, that host has to reach GrandNode for the document to be served.

Anonymous, `application/json`:

```json
{
  "resource": "https://shop.example.com/api",
  "authorization_servers": [ "https://idp.example.com/realms/shop" ],
  "scopes_supported": [ "grandnode.api" ],
  "bearer_methods_supported": [ "header" ],
  "resource_name": "BackendApi"
}
```

Unknown paths and a disabled feature give 404.

### 4.3 PathBase

When GrandNode is published under a path base (for example `https://example.com/shop`), the metadata URL is at the
host root: `https://example.com/.well-known/oauth-protected-resource/shop/api`, both in the challenge and for clients
that build it themselves. The application cannot answer outside its path base, so the reverse proxy has to route
`/.well-known/oauth-protected-resource/...` at the host root to the application (keeping the full path, here
`/.well-known/oauth-protected-resource/shop/api`). The same holds for a store whose *Store URL* has a path
(`https://example.com/shop`): its documents are at `/.well-known/oauth-protected-resource/shop/...` on the host root,
and the root document `/.well-known/oauth-protected-resource` answers 404 for it.

### 4.4 Endpoints without `[Authorize]`

Under a resource prefix an endpoint that has no `[Authorize]` attribute does **not** answer 401 for a refused token:
the request continues as cookie or guest customer. Protect plugin endpoints with
`[Authorize(AuthenticationSchemes = "Oidc")]` (section 5).

### 4.5 Impersonation

Impersonation is not applied to token-authenticated customers. The OIDC request customer resolver runs before the
built-in cookie step in `WorkContextSetter.CurrentCustomer`, and impersonation happens only for the cookie customer
(pinned by `OidcWorkContextTests.ValidToken_TokenCustomerNeverImpersonated`).

### 4.6 Routing of tokens

On a resource path a request is handled by the `Oidc` scheme when it carries no readable bearer JWT (so the 401 is the
OIDC challenge), when the token's `iss` equals `Authority`, or when the token is signed with RS/PS/ES. Tokens signed
with HS\* by another issuer (GrandNode's own `SecretKey` tokens) stay with the old scheme.
A bearer value that does not parse as a JWT (for example three arbitrary dot-separated segments) counts as no
token and gets the OIDC challenge.

`Authority` must equal the `issuer` of the provider's discovery document. In container setups the application's
`Authority` URL and the token `iss` must be the same string: use the public host name for both (for example
`http://localhost:8080/realms/grandnode` for Keycloak started by `docs/oidc/docker-compose.yml`).

## 5. Protecting a plugin's endpoints

A plugin registers a provider with its resources and protects its controllers with the `Oidc` scheme.

```csharp
public class MyResources : IOidcProtectedResourceProvider
{
    public IEnumerable<OidcProtectedResource> GetResources() => [
        new OidcProtectedResource {
            Name = "MyPlugin", PathPrefix = "/my-plugin",
            ScopesSupported = ["myplugin.use"], RequiredScopes = ["myplugin.use"],
            RequireAdminPanelAccess = true, DenyVendors = true, DenyStoreManagers = true
        }
    ];
}

// in the plugin's IStartupApplication.ConfigureServices
services.AddSingleton<IOidcProtectedResourceProvider, MyResources>();

// on the plugin's controllers
[Authorize(AuthenticationSchemes = "Oidc")]
```

The plugin's resource appears in the metadata documents and challenges like a configured one. A plugin that defines
scope hierarchies checks them itself: scope comparison in the resource server is exact.

## 6. Keycloak 26.8

Tested version: Keycloak 26.8.0 (image `quay.io/keycloak/keycloak:26.8.0`). Documentation:
<https://www.keycloak.org/securing-apps/mcp-authz-server>.

1. **Realm.** Create a realm, for example `grandnode`. Issuer: `https://idp.example.com/realms/grandnode`; this is
   `OidcResourceServer:Authority`.
2. **Client scope** *Clients scopes > Create client scope*: name `grandnode.api`, type **Optional**, protocol
   OpenID Connect, *Include in token scope* on.
3. **Audience mapper** in that scope (*Mappers > Configure a new mapper > Audience*): *Included Custom Audience* =
   the resource URI, for example `https://shop.example.com/api` (store URL + prefix, or `Resource`); *Add to access
   token* on, *Add to ID token* off. Add one mapper per resource.
4. **Interactive applications** (public client): *Clients > Create client*, *Client authentication* off, *Standard flow*
   on, valid redirect URIs of the application; under *Advanced > Advanced settings* set *Proof Key for Code Exchange
   Code Challenge Method* to `S256`. Add `grandnode.api` to the client's optional scopes. The application requests
   `scope=openid grandnode.api`.
5. **E-mail in the access token.** The default `email` client scope adds `email` and `email_verified` to access
   tokens; keep it assigned to the client. Users must have a verified e-mail address (*Users > Email verified*, or
   enable *Verify email* in the realm login settings).
6. **GrandNode.** `Authority` as in step 1, `RequiredScopes: ["grandnode.api"]`, `Resource` as in step 3 (or empty
   when the audience is the store URL + prefix).

Experimental features (Keycloak marks both as experimental; enable with `--features=resource-indicators,cimd`):

* **Resource indicators (RFC 8707)**: Keycloak then processes the `resource` parameter and rejects unknown values with
  `invalid_target` (at the token endpoint). A `resource` equal to the *Included Custom Audience* of the scope's
  Audience mapper (steps 2-3) is accepted, so that setup works with the feature on or off. Alternatively create a client that represents the resource and set its client attribute `resource_url` to the
  resource URI (Admin REST API or realm import); the application's client gets an Audience mapper with *Included
  Client Audience* pointing at that client. Without the feature Keycloak ignores `resource` and the audience comes from
  the scope's Audience mapper (steps 2-3), which is the setup used for GrandNode by default.
* **Dynamic client registration (RFC 7591)**, for clients that register themselves: *Clients > Client registration >
  Anonymous access policies*: *Allowed Client Scopes* must include `grandnode.api`; *Trusted Hosts* the host names or
  addresses the registration requests come from; *Allowed Registration Web Origins* the web origin of browser based
  clients. DCR is deprecated in MCP 2026-07-28 but still allowed.
* **Client ID Metadata Documents (CIMD)**: *Realm settings > Client policies > Profiles*: create a profile with the
  executor `client-id-metadata-document` (*Trusted domains*, *Restrict same domain*, *Allow http scheme* off in
  production; *Only Allow Confidential Client* off for public clients using PKCE; *Resource indicator allow list*
  needs `resource-indicators`). Then *Policies*: create a policy with the condition `client-id-uri` (*URI scheme*
  `https`, *Trusted domains*; an empty list never matches) and the profile.

## 7. Zitadel (v4.19)

Tested version: Zitadel v4.19.4 (image `ghcr.io/zitadel/zitadel:v4.19.4`). Documentation:
<https://zitadel.com/docs/apis/openidoauth/claims>, <https://zitadel.com/docs/apis/openidoauth/scopes>,
<https://zitadel.com/docs/guides/integrate/actions/migrate-from-v1>.

Zitadel differs from Keycloak in four points that shape the configuration:

* Access tokens are **opaque by default**; GrandNode needs JWT.
* The `resource` parameter is ignored; the audience is the project id (and the client ids).
* A JWT access token has **no `email`, `email_verified` and `scope` claims**.
* Custom scopes are not defined by the provider, so a `scope` claim can only come from an Action.

Steps:

1. **Project and application.** Create a project (note its id) and turn on **Assert Roles on Authentication** in its
   settings (Zitadel sends the user's grants to the Actions target only then, or when the client requests
   `urn:zitadel:iam:org:projects:roles`). Create an application of type *Web* or *User Agent* with
   authentication method *PKCE* (public client, S256), the application's redirect URIs (`http` redirect URIs need
   *Development Mode*) and **Auth Token Type = JWT** (*Token Settings*). Note the client id.
2. **Audience.** Zitadel puts the project id and the client ids of all applications of the project into `aud`.
   Requesting the scope `urn:zitadel:iam:org:project:id:{projectId}:aud` makes the project id explicit; it is
   harmless when it is already there. Zitadel sends the client in `client_id`, not `azp`.
3. **E-mail, entitlement and the Actions v2 target.** Zitadel Actions v1 (`api.v1.claims.setClaim`, complement token
   flow) is deprecated and planned for removal in Zitadel v5. Use an Actions v2 target on the function
   `preaccesstoken`. `docs/oidc/zitadel-email-target.js` is a small HTTP target that answers with `append_claims`:
   `email` and `email_verified` (from the payload), and `scope = grandnode.api` **only when the user has the project
   role `grandnode.api` in the GrandNode project** (`user_grants` of the payload; role key `GRANDNODE_ROLE`, project
   `GRANDNODE_PROJECT_ID`, both environment variables of the target). Without the role, or without
   `GRANDNODE_PROJECT_ID`, no scope is added and GrandNode answers 403 `insufficient_scope`. To use it: create the
   role `grandnode.api` in the project (step 1), grant it to the users who may call the API, and set
   `GRANDNODE_PROJECT_ID` in the `.env` file of the test environment. `preaccesstoken` runs for access tokens only, so
   ID tokens never carry the scope. In the test environment the target runs as the service `zitadel-email-target`.
   Create the target and the execution with the management API (token of an admin or service user with the right to
   manage actions):

   ```bash
   curl -X POST 'http://localhost:8081/v2/actions/targets' \
     -H 'Content-Type: application/json' -H 'Authorization: Bearer <TOKEN>' \
     -d '{ "name": "grandnode-claims", "restCall": { "interruptOnError": true },
           "endpoint": "http://zitadel-email-target:8090/", "timeout": "10s" }'

   curl -X PUT 'http://localhost:8081/v2/actions/executions' \
     -H 'Content-Type: application/json' -H 'Authorization: Bearer <TOKEN>' \
     -d '{ "condition": { "function": { "name": "preaccesstoken" } }, "targets": [ "<target id>" ] }'
   ```

   A "call" target uses the response; a webhook ignores it. Zitadel refuses targets on private addresses by default
   (`HTTPClient.DenyList`); the test compose file overrides it (test only, it restores SSRF exposure: do not copy to
   production). In production use an HTTPS endpoint and verify the
   `ZITADEL-Signature` header (the sample does not).
4. **GrandNode.**

   ```json
   "OidcResourceServer": {
     "Enabled": true,
     "Authority": "https://zitadel.example.com",
     "Resources": [
       {
         "Name": "BackendApi",
         "PathPrefix": "/api",
         "Audiences": [ "<project id>" ],
         "AuthorizedParties": [ "<client id>" ],
         "ScopesSupported": [ "grandnode.api" ],
         "RequiredScopes": [ "grandnode.api" ],
         "RequireAdminPanelAccess": true
       }
     ]
   }
   ```

   `AuthorizedParties` is **mandatory** for Zitadel: the audience is a project id shared by every application of the
   project, so only the authorized party (`azp`, for Zitadel the `client_id` claim) binds the token to your client. With `Audiences` set the store URL based audience is still
   accepted next to it.

   **What gates access with Zitadel**, from the outside in: (1) the token is a valid JWT of the instance's issuer;
   (2) `aud` contains the project id (or the store resource URI); (3) `azp` is one of your clients; (4) the `scope`
   claim contains `grandnode.api`, which the target adds only for users holding the project role; (5) the GrandNode
   customer exists or is registered, is in a group with *Access admin panel* and the needed permissions. Step 4 is only
   as strong as who may grant the project role or edit Zitadel Actions. If
   you do not run the target, leave `RequiredScopes` **empty** for Zitadel (a required scope would reject every token)
   and rely on (1)-(3) and (5); then any user of the instance who can log in to an allowed client reaches the
   GrandNode permission check. With `AutoRegister` enabled, a user registered automatically holds the Registered group
   only, which has no admin panel access. `AutoRegister` is off by default; turn it on only when every user of the
   provider may get a store account.
5. **Dynamic client registration** (RFC 7591) is **off by default** in Zitadel. Two modes: with a token of a user
   holding the `project.app.register_dynamic` permission, or open (`dynamicClientRegistration.allowUnauthenticated =
   true`), which MCP-class clients need. See <https://zitadel.com/docs/guides/integrate/dynamic-client-registration>.
   Clients registered this way still need the audience scope of step 2.

Fallback for a Zitadel version that still runs Actions v1 (property names from the v1 reference; the role check uses
`ctx.v1.user.grants`, replace the project id): trigger *Complement Token > Pre access token creation*:

```js
function addClaims(ctx, api) {
  if (!ctx.v1.user || !ctx.v1.user.human) return;
  api.v1.claims.setClaim('email', ctx.v1.user.human.email);
  api.v1.claims.setClaim('email_verified', ctx.v1.user.human.isEmailVerified);
  var entitled = (ctx.v1.user.grants || []).some(function (g) {
    return g.projectId === '<project id>' && g.roles && g.roles.indexOf('grandnode.api') >= 0;
  });
  if (entitled) api.v1.claims.setClaim('scope', 'grandnode.api');
}
```

## 8. Introspection

Not supported. Keycloak and Zitadel (application set to JWT) issue JWT access tokens that are validated locally
against the provider's JWKS. Introspection would need GrandNode client credentials at the provider and a provider round
trip per request (or a cache with its own revocation window). A provider that can issue opaque tokens only is not
supported.

## 9. Deprecated: API users

API users (`SecretKey` tokens signed by GrandNode, *API users* in the admin panel, `BackendAPI` settings) are
**deprecated** in favor of the resource server. They keep working on the same endpoints until they are removed in a
future major version. To migrate: configure the resource server, let the integration obtain tokens from the provider
(client credentials or authorization code with PKCE), add the integration's customer to a group with the permissions
the calls need, then remove the API user.

## 10. Test environment

`docs/oidc/docker-compose.yml` starts Keycloak 26.8.0 (`http://localhost:8080`), Zitadel v4.19.4
(`http://localhost:8081`) with PostgreSQL 17, and the Zitadel claim target. Placeholder credentials, http only, for
tests only:

Create a `docs/oidc/.env` file with your own values (the compose file refuses to start while one is missing):

```ini
KC_ADMIN_USER=admin
KC_ADMIN_PASSWORD=<choose a password>
ZITADEL_DB_PASSWORD=<choose a password>
ZITADEL_MASTERKEY=<exactly 32 characters>
ZITADEL_ADMIN_PASSWORD=<password with upper and lower case, digit and symbol>
# after creating the GrandNode project in Zitadel, then `docker compose ... up -d zitadel-email-target`
GRANDNODE_PROJECT_ID=
```

```bash
docker compose -f docs/oidc/docker-compose.yml up -d
```

Ports are published on `127.0.0.1` only.

Use the URLs above as `Authority` (`http://localhost:8080/realms/<realm>` for Keycloak, `http://localhost:8081` for
Zitadel) with `RequireHttpsMetadata: false` in Development.

## 11. Browser sign-in (OpenID Connect login plugin)

The plugin `Authentication.OpenIdConnect` (system name `ExternalAuth.OpenIdConnect`) lets customers and staff sign in
to the storefront with the same provider that issues the API tokens. It is separate from the resource server: the
resource server validates access tokens, the plugin runs the browser flow (authorization code with PKCE) and gives the
visitor a normal GrandNode session. The provider proves who the user is; the customer's **groups and permissions in
GrandNode** still decide what the user may do.

### 11.1 What the visitor sees

* A **sign-in button** next to the other external authentication methods.
* The provider and its claim names are the ones of `OidcResourceServer` (`Authority`, `RequireHttpsMetadata`,
  `Claims:*`). The plugin adds one client of its own (section 11.3). `RequireEmailVerified` applies to API tokens
  only; browser sign-in links an account automatically only when the provider reports the e-mail as verified,
  whatever that setting says.
* The link between the provider's `sub` and the customer is stored in the same place as for API tokens (provider name
  `ExternalAuth.OpenIdConnect`). An account linked by browser sign-in is the account API tokens map to, and the other
  way round.
* "My account" lists the connection and lets the customer remove it. After removing it, the button leads to linking
  again. The account always has a password (random until the customer sets one with "Forgot password"), so removing the
  connection never locks the customer out.
* Routes used: `/oidc/login`, `/signin-oidc` (callback of the OpenID Connect handler), `/oidc/callback`, `/oidc/link`,
  `/oidc/link/complete`, `/oidc/failed`.

### 11.2 Account rules

After the provider signed the user in, the result depends on the GrandNode side:

| Situation | Result |
|---|---|
| A link for the provider's `sub` exists | The linked customer is signed in. Staff included; the admin panel access follows their existing permissions. The customer must be active, not deleted and registered. |
| No link; an account with the same e-mail exists, the provider **verified** the e-mail, the account is **not staff** and does not use GrandNode two-factor authentication | The account is linked and signed in at once. |
| No link; the account is **staff** (admin panel access, vendor, store staff), the account uses **GrandNode two-factor authentication** (enabled for the customer and in the store), or the provider did **not** verify the e-mail | "Connect your account": the page ends any current store session, and the visitor signs in **once** with the store password (and the two-factor code, when enabled). The connection is then completed (`/oidc/link/complete`); the signed-in account must have the same e-mail address. |
| No account with that e-mail | The regular registration form opens with the e-mail taken from the provider (read-only) and **no password fields**. Store registration type "Standard" becomes e-mail validation when the provider did not verify the e-mail; "E-mail validation" becomes standard when it did; "Admin approval" stays admin approval either way. New accounts get the **Registered** group only. |
| Registration disabled in the store, account inactive, e-mail already linked to another identity | The failure page (fixed text; the reason is written to the log). |

Notes:

* The provider's identity is kept for **15 minutes** (absolute) in the `ExternalAuthentication` cookie, which is not
  persistent. It only decides who is registering or connecting; an e-mail value posted with the registration form is
  ignored.
* A provider that sends `email_verified` as `"True"`, `true` or not at all is handled: the comparison ignores case and a
  missing value means unverified.
* **"Admin approval" and an unverified e-mail.** The account waits for approval and GrandNode sends no activation
  e-mail (its activation link would approve the account), so GrandNode does not validate an e-mail address the
  provider reported as unverified. Administrators should check the address before approving such an account.
* A customer who uses GrandNode two-factor authentication connects the provider identity **once** with the store
  password and the two-factor code, like staff; after that the provider sign-in alone signs the customer in.
* With `RegisterCustomersPerStore` enabled all lookups are limited to the store of the request.
* Provider tokens are **not stored**.
* The Web API setting `OidcResourceServer:AutoRegister` is `false` by default (section 3.2); browser sign-in registers
  new users through the registration form instead.

### 11.3 Configuration

1. Build the plugin and install it in the admin panel (*Configuration > Plugins*). The plugin output contains only its
   own assembly and `Microsoft.AspNetCore.Authentication.OpenIdConnect.dll`; it uses the `Microsoft.IdentityModel.*`
   assemblies of the application.
2. Enable it in *Configuration > External authentication*. The display order of the button is set on the plugin's
   configuration page.
3. Configure the provider (section 11.4 or 11.5) and add the `OidcLogin` section. The secret goes into an environment
   variable, never into the database or the admin panel.

Section `OidcLogin` of `appsettings.json`:

| Key | Default | Meaning |
|---|---|---|
| `ClientId` | empty | Client id of the **confidential** client created for browser sign-in. |
| `ClientSecret` | empty | Its secret. Prefer the environment variable `OidcLogin__ClientSecret`. |
| `Scopes` | `openid profile email` | Space separated scopes requested at the provider. `openid` and `email` are needed. |

```json
"OidcResourceServer": {
  "Authority": "https://idp.example.com/realms/grandnode",
  "RequireHttpsMetadata": true
},
"OidcLogin": {
  "ClientId": "grandnode-login",
  "ClientSecret": "",
  "Scopes": "openid profile email"
}
```

`Authority`, `RequireHttpsMetadata` and the claim names come from `OidcResourceServer`. `OidcResourceServer:Enabled`
does not have to be on for browser sign-in; the plugin only reads `Authority` and the other values above. It does not
read `RequireEmailVerified`: automatic linking always needs an e-mail the provider reports as verified.

**Not configured.** When `Authority` or `ClientId` is empty the plugin registers no sign-in scheme and the button leads
to the failure page; it never causes an error page.

Handler settings (fixed): authorization code flow with PKCE (S256), response mode `query`, `sub`, e-mail,
`email_verified`, `given_name` and `family_name` are also read from the **user info endpoint**, provider tokens are not
saved, claims are not renamed. State and nonce are checked by the handler.

The redirect URI of the client is `https://<store-url>/signin-oidc` for **every** store URL (each store of a
multi-store installation, each host name).

### 11.4 Keycloak 26.8

Use the realm of section 6. Create a **separate** client; do not reuse the public client of the API.

1. *Clients > Create client*: client type OpenID Connect, client id `grandnode-login`.
2. Capability config: *Client authentication* **on** (confidential), *Standard flow* **on**; the other flows can stay
   off.
3. Login settings: *Valid redirect URIs* `https://<store-url>/signin-oidc` (one entry per store URL), *Web origins*
   the store origin `https://<store-url>`.
4. *Advanced > Advanced settings*: set *Proof Key for Code Exchange Code Challenge Method* to `S256`.
5. *Credentials* tab: copy the client secret into `OidcLogin__ClientSecret`.
6. The default `profile` and `email` client scopes are assigned to a new client; keep them. `email_verified` is part of
   the ID token and of the user info answer by default. Users need a verified e-mail address (*Users > Email
   verified*, or enable *Verify email* in the realm login settings) to be linked automatically.
7. Enforce MFA for staff in the realm (section 11.6).

### 11.5 Zitadel v4.19

Use the project of section 7. Create a **separate** application for the browser flow.

1. In the project add an application of type **Web** with authentication method **Code** (authorization code with a
   client secret). PKCE is sent by the plugin. Copy the client id and the generated client secret
   (`OidcLogin__ClientSecret`; the secret is shown once).
2. Redirect URIs: `https://<store-url>/signin-oidc` (an `http` redirect needs *Development Mode*). Post logout URIs
   are not used (logout is local).
3. The plugin reads the claims from the ID token or, when they are absent there, from the user info endpoint (the
   handler merges both); Zitadel answers the user info endpoint for the scope `email`. To have the ID token carry the
   user information as well, enable the application's option to put user information into the ID token (token
   settings of the application). The default scopes `openid profile email` are sufficient.
4. **The Actions v2 target of section 7 is not required for browser sign-in.** It runs on `preaccesstoken`, that is for
   access tokens only, and the plugin does not use the access token. Verify this on your instance: after a test
   sign-in with a verified e-mail the application log must not contain "OpenID Connect sign-in without a subject or an
   e-mail". If it does, enable the ID token option of step 3 and check that the user's e-mail is marked verified in
   Zitadel (an unverified e-mail is sent with `email_verified = false`).
5. Users need a verified e-mail address in Zitadel to be linked automatically. Zitadel's hosted login asks a user
   whose e-mail is not verified to confirm it with a code before it completes the sign-in, so `email_verified` is
   normally `true` for sign-ins through Zitadel. Enforce MFA for staff in the login policy of the organization
   (section 11.6).

### 11.6 Security notes

* **After linking, staff sign in with the provider only.** The button signs a linked staff account in without the
  GrandNode password and **without GrandNode two-factor authentication**. Enforce MFA at the provider for every user
  who may be linked to a staff account. The first link of a staff account always needs the store password (and its 2FA).
* A staff account is never linked by e-mail alone, but the one who completes the password sign-in and holds the
  provider identity owns the link; keep the provider's e-mail verification and e-mail change policy strict (same advice
  as section 3.1).
* **Logout is local.** Signing out of GrandNode does not end the provider session; the next click on the button signs
  in again without a prompt while the provider session lasts.
* Provider tokens are not stored. The only data kept from the provider is the `sub`/e-mail link, plus the pending
  identity for at most 15 minutes.
* After sign-in only a **local** `returnUrl` is followed; anything else leads to the store home page.
* New accounts created through this path get the Registered group only.
* Keep `ClientSecret` out of source control: environment variable or secret store.

### 11.7 Troubleshooting

| Symptom | Check |
|---|---|
| The button leads to the "Sign-in failed" page at once | `OidcResourceServer:Authority` and `OidcLogin:ClientId` set? The plugin installed and enabled in *External authentication*? Without both settings no sign-in scheme exists. |
| "Sign-in failed" after the provider page | The reason is in the application log (`OpenID Connect sign-in failed`, with the provider's error), never on the page. Common: wrong client secret, the user cancelled at the provider, the state/nonce cookie lost (a proxy, or a host name different from the one that started the flow). |
| Provider error about the redirect URI | The redirect URI at the provider must equal `https://<store-url>/signin-oidc` exactly: scheme, host, port, no trailing slash. Behind a TLS-terminating proxy GrandNode must see `https` (forwarded headers) or it builds an `http` URI. |
| Failure caused by token lifetime ("not yet valid", "expired") | Clock skew between the application server and the provider; synchronize the clocks (NTP). |
| Discovery document cannot be loaded | `Authority` must equal the `issuer` of `{Authority}/.well-known/openid-configuration`; `http` needs `RequireHttpsMetadata: false` (Development only); in container setups use a host name both the application and the browser resolve. |
| Visitor is sent to the registration form although the account exists | The e-mail differs, or `RegisterCustomersPerStore` limits lookups to another store. |
| Visitor is asked to connect accounts though the e-mail is verified | The account is staff or uses GrandNode two-factor authentication, or the provider sends `email_verified` as false or not at all (for Zitadel check the user info answer). |
| After sign-in the visitor lands on the home page, not on the requested page | The `returnUrl` was not local. |

Sources used for provider specifics (checked 2026-10-08): Keycloak 26.8 MCP guide and release notes, Zitadel v4.19.4
claims, scopes, Actions v2 and dynamic client registration documentation; MCP authorization 2026-07-28; RFC 9728,
RFC 8707, RFC 6750, RFC 7591.
