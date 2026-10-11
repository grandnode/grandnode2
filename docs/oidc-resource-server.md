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
* One identity provider at a time. Not covered: browser login of storefront or admin panel users, opaque tokens
  (see [Introspection](#8-introspection)).

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
| `DenyVendors` | `false` | Refuse customers that belong to a vendor. The shipped `BackendApi` entry sets `true`. |
| `DenyStoreManagers` | `false` | Refuse store manager accounts. The shipped `BackendApi` entry sets `true`. |

`offline_access` is never advertised in `scopes_supported` or in a challenge `scope`.

Startup validation (outside Development the application refuses to start, in Development it warns): `Authority`
absolute, https (an `http` Authority is accepted in Development only, with `RequireHttpsMetadata = false`); at least one
resource (configuration or plugins); each resource has a name and a valid prefix; names and prefixes unique and not
nested; `Resource` absolute without fragment; `ClockSkewSeconds` 0-300; claim names not empty.

### Audience

A token is accepted when its `aud` contains the resource URI or one of `Audiences`.

* With an empty `Resource` the resource URI is the **URL of the GrandNode store serving the request host** (field *Store
  URL* in the admin panel) plus `PathPrefix`, for example `https://shop.example.com/api`. The store is the one whose
  *Hosts* list the request host, compared with its port first (`localhost:44350`, as the installer records the host of
  the install request) and then without it (`shop.example.com`); the store cookie is never used. When no store lists the
  host, a single-store installation uses its only store; with several stores the request has no audience and every token
  is rejected (and the metadata endpoints answer 404). The URI itself always comes from the *Store URL*, never from the
  `Host` header. A request whose token is accepted runs in that same store (prices, settings, per-store accounts), even
  when it also carries another store's cookie; without an accepted token the store cookie keeps its usual priority.
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
      "RequireAdminPanelAccess": true,
      "DenyVendors": true,
      "DenyStoreManagers": true
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
   One identity per customer is enforced by the link's identifier, which is derived from the customer, so two
   concurrent requests with different subjects cannot both link the same customer.
   Links do not record the provider instance, so after switching `Authority` to another provider the customers
   linked by the previous one are refused until their link is removed.
7. With `RegisterCustomersPerStore` enabled an account of another store is refused (403) and lookups are store-scoped.

**Per-store identity.** With `RegisterCustomersPerStore` enabled the same person can have a separate customer in each
store, so one `sub` gets one link per store: the link records the store of the linked customer, and a request uses the
link of its own store. In a store where the subject has no link yet, the rules above link (or register) that store's
customer. A customer of no store, such as a back-office account, has one link without a store, and it works in every
store. A link without a store to a customer of another store (stored while per-store identity was off) is ignored, so
the store's own customer is linked next to it. Without per-store identity a link has no store and is used in every
store.

### 3.2 Automatic registration

`AutoRegister` is `false` by default. When no customer matches, the answer is 403 "No customer account matches the
token"; the customer has to exist already (it is then linked by the verified e-mail, section 3.1). Set `AutoRegister =
true` to opt in to registering an unknown user on first use; then:

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

The Web API's controllers apply no vendor or store scoping, so the shipped `BackendApi` resource sets `DenyVendors` and
`DenyStoreManagers` to `true`: vendor accounts and store managers are refused even with admin panel access. Keep both
on unless every endpoint the resource covers scopes its data.

### 3.5 Web API (`/api`)

* OIDC tokens are accepted on `/api` **next to** the existing `SecretKey` tokens, which keep working.
* Access still requires the `ManageAccessAdminPanel` permission and passes the admin IP allow-list.
* The per-user *API users* opt-in does **not** apply to OIDC tokens. Who may call the API is decided by which users
  the provider grants the `grandnode.api` scope, plus their GrandNode groups.
* `BackendAPI:Enabled = false` switches the Web API off for both token kinds. For OIDC tokens this happens before the
  token is looked at: the Web API module withdraws its paths (`IOidcDisabledPathProvider`), so every resource on or
  under `/api` is left out of the registry (logged at startup), no metadata is published for it, and a provider token
  there goes to the `SecretKey` scheme, which refuses it (401). No customer is registered, linked or looked up and the
  store is not pinned. The setting is read at startup; changing it needs a restart. An OIDC-only setup still needs
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

On a resource path the Web API's `Bearer` scheme hands a request to the `Oidc` scheme when it carries no readable
bearer JWT (so the 401 is the OIDC challenge) or when the token's `alg` is one the `Oidc` scheme accepts (RS256/384/512,
PS256/384/512, ES256/384/512). Every other token, HS\* in particular (GrandNode's own `SecretKey` tokens), stays with
the `Bearer` scheme whatever `iss` it claims; the `Oidc` scheme would refuse an HMAC token anyway. The algorithm is read
from the unvalidated header for routing only, once per request; the scheme the request lands on validates the token.
A bearer value that does not parse as a JWT (for example three arbitrary dot-separated segments) counts as no
token and gets the OIDC challenge. Only the `Bearer` scheme forwards: other JwtBearer schemes (the frontend API,
plugins) keep their own validation and challenge, also on a resource path.

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
2. **Client scope** *Client scopes > Create client scope*: name `grandnode.api`, type **Optional**, protocol
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
  `invalid_target` (at the token endpoint). A `resource` equal to the *Included Custom Audience* of the scope's Audience
  mapper (steps 2-3) is accepted, so that setup works with the feature on or off. Alternatively create a client that
  represents the resource and set its client attribute `resource_url` to the resource URI (Admin REST API or realm
  import); the application's client gets an Audience mapper with *Included Client Audience* pointing at that client.
  Without the feature Keycloak ignores `resource` and the audience comes from the scope's Audience mapper (steps 2-3),
  which is the setup used for GrandNode by default.
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
         "RequireAdminPanelAccess": true,
         "DenyVendors": true,
         "DenyStoreManagers": true
       }
     ]
   }
   ```

   `AuthorizedParties` is **mandatory** for Zitadel: the audience is a project id shared by every application of the
   project, so only the authorized party (`azp`, for Zitadel the `client_id` claim) binds the token to your client. With
   `Audiences` set the store URL based audience is still accepted next to it.

   **What gates access with Zitadel**, from the outside in: (1) the token is a valid JWT of the instance's issuer; (2)
   `aud` contains the project id (or the store resource URI); (3) `azp` is one of your clients; (4) the `scope` claim
   contains `grandnode.api`, which the target adds only for users holding the project role; (5) the GrandNode customer
   exists or is registered, is in a group with *Access admin panel* and the needed permissions. Step 4 is only as strong
   as who may grant the project role or edit Zitadel Actions. If you do not run the target, leave `RequiredScopes`
   **empty** for Zitadel (a required scope would reject every token) and rely on (1)-(3) and (5); then any user of the
   instance who can log in to an allowed client reaches the GrandNode permission check, and a user that is
   auto-registered holds the Registered group only, which has no admin panel access. `AutoRegister` is off by default;
   turn it on only when every user of the provider may get a store account.
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

Sources used for provider specifics (checked 2026-10-08): Keycloak 26.8 MCP guide and release notes, Zitadel v4.19.4
claims, scopes, Actions v2 and dynamic client registration documentation; MCP authorization 2026-07-28; RFC 9728,
RFC 8707, RFC 6750, RFC 7591.
