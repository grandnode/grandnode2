# SCIM 2.0 provisioning of staff accounts

How to let Microsoft Entra ID or Okta create, update and lock out the GrandNode accounts of your employees, and give
them their GrandNode roles from the identity provider's groups.

## 1. What it does

GrandNode is a SCIM 2.0 **service provider** (RFC 7643, RFC 7644) at `/scim/v2`. The identity provider that already signs
your staff in through OpenID Connect ([docs/oidc-resource-server.md](oidc-resource-server.md), sections 11 and 12) pushes:

* **Users.** A new employee gets an account before the first sign-in. Name and e-mail follow the identity provider.
  Deactivating or deleting the user at the provider locks the account out at once (section 4).
* **Groups.** A group grants nothing until an administrator maps it to a customer group in
  **Admin > Configuration > External authentication > SCIM provisioning**. From then on, membership of the group gives
  the role and leaving the group takes it away.

What it is not:

* Not for storefront customers, not for B2B customer companies, and not for more than one identity provider. It
  provisions the employees of the company that runs the store, through the one provider of `OidcResourceServer`.
* No SAML. Sign-in stays OpenID Connect.
* No `/Bulk`, `/Me`, sorting, ETags, `/.search` or password changes. The discovery documents say so.

## 2. Configuration

1. Configure and test the OpenID Connect sign-in first (`OidcResourceServer` and `OidcLogin`). Without it every SCIM
   request is refused.
2. Turn the module on: `FeatureManagement:Grand.Module.Scim = true`. The module is built with the solution
   (`src/Modules/Grand.Module.Scim`).
3. Set section `Scim`:

| Key | Default | Meaning |
|---|---|---|
| `Token` | empty | The Bearer token the identity provider sends. At least 32 characters; shorter means every request is refused. Keep it out of source control (`Scim__Token` or a secret store). Generate it, for example, with `openssl rand -base64 48`. |
| `ExternalIdIsSubject` | `true` | The SCIM `externalId` of a user is its OpenID Connect subject. Every SCIM write links the account to it, so the first sign-in needs no password. See section 3. |
| `MaxResults` | `100` | Largest page of a list response. |

4. Restart, open **SCIM provisioning** in the admin panel, and copy the **SCIM endpoint (tenant URL)**.

## 3. Identity providers

### 3.1 Microsoft Entra ID

* Enterprise application of GrandNode > **Provisioning** > Automatic. Tenant URL: the endpoint from the admin screen;
  Secret token: `Scim:Token`. **Test connection** must pass (it filters for a random `userName` and expects an empty list).
* **Attribute mapping (users):** map `objectId` → `externalId`. Entra's default maps `mailNickname` there, which is not the
  OpenID Connect identity.
* **OpenID Connect:** set `OidcResourceServer:Claims:Subject = "oid"`. Entra's `sub` is different for every application and
  never equals the object id. The `oid` claim comes with the `profile` scope, which `OidcLogin:Scopes` requests by default.
* `userName` is the account's e-mail. Entra sends the UPN. If your UPNs are not mailboxes, map `userName` to `mail`.
* Groups: assign the groups to the application. Entra reads them with `excludedAttributes=members`, and GrandNode
  supports that.

### 3.2 Okta

* SCIM 2.0 app integration > **Provisioning** > Integration. SCIM connector base URL: the endpoint. Unique identifier field:
  `userName`. Authentication mode: HTTP Header with the token. Enable Create, Update and Deactivate users, and Push Groups.
* Okta fills its own `externalId` with the id GrandNode returns and does not send its user id by default. Choose one:
  * map the SCIM `externalId` to the Okta user id (Profile Editor of the app, expression `user.getInternalProperty("id")`).
    The Okta user id is Okta's OpenID Connect `sub`, so keep `ExternalIdIsSubject = true`. Check in your tenant that the
    attribute is sent;
  * or set `Scim:ExternalIdIsSubject = false` and `OidcLogin:SignInMode = ProviderOnly`, so an account is linked at its first
    sign-in by its provider-verified e-mail. In `Mixed` mode a staff account links only with its password, and a SCIM
    account has none anybody knows. GrandNode logs a warning at startup for that combination.

## 4. Behaviour

**Create.** No account with the e-mail: a registered, active account is created. It belongs to no store, so with per-store
identity it is usable on every store host. Its password is random and nobody knows it, and no welcome e-mail is sent. An
account with the e-mail already exists: **SCIM takes it over** and manages it from then on (section 5). Refused with
`409 uniqueness`: system accounts, deleted accounts, break-glass accounts (`OidcLogin:BreakGlassAccounts`), a store-bound
account with per-store identity, an account linked to another OpenID Connect identity, a second user with the same
`externalId`.

**Update.** E-mail (and a user name that was the e-mail), first and last name. A new e-mail that belongs to another account
is refused with `409`.

**Deactivate** (`active: false`). The account becomes inactive, which the cookie, Frontend API and Backend API checks refuse on
the next request. Its OpenID Connect sessions end, the provider access tokens of its subject issued so far are refused, and
its Frontend API refresh token is dropped.

**Reactivate** (`active: true`). Only an account SCIM deactivated. An account an administrator disabled by hand stays disabled,
and SCIM reports it as inactive.

**Delete.** The account is deactivated, loses every role SCIM granted and its OpenID Connect link, and leaves every group. The
customer itself is kept with its orders and history. A later create with the same e-mail takes it over again.

**Unknown attributes** of a user (title, department, phone numbers...) are accepted and ignored.

## 5. Roles

Map each group on the admin screen to one of:

| Customer group | Also needs | Members get |
|---|---|---|
| Administrators | - | the Admin panel |
| Store managers | a store | the Store panel of that store (`StaffStoreId`) |
| Vendors | a vendor | the Vendor panel of that vendor (`VendorId`) |
| a custom (non-system) group | - | that group |

Sales managers cannot be mapped, because they need a sales employee.

* **Granted by SCIM only.** SCIM remembers what it granted and takes away only that. Groups an administrator assigned by hand
  stay. Removing or changing a mapping takes its role away from every member at once.
* **Takeover.** When SCIM takes over an existing account, its Administrators / Store managers / Vendors roles, its store and
  vendor, and every mapped group it holds count as granted by SCIM. An employee who is in no mapped group loses them
  immediately, which is what "managed by SCIM" means.
* **Conflicts.** The back-office roles must be one of: Administrators alone, Store managers with one store, Vendors with one
  vendor. If a user's groups give anything else (Administrators and Store managers, two stores, Vendors and Store managers),
  SCIM grants **none** of these roles, still grants the custom groups, and lists the account under **Role conflicts** on the
  admin screen.

## 6. Security

* Only the Bearer token of `Scim:Token` is accepted, compared in constant time. Discovery endpoints need it too. Rotate it
  at the provider and in appsettings together.
* The endpoint is refused entirely while the token is shorter than 32 characters or the OpenID Connect sign-in is not
  configured. Startup logs an error in that case.
* Logs carry customer ids, never e-mails or tokens.
* Request bodies are limited to 1 MB.

## 7. Endpoints

| Method | Path | Notes |
|---|---|---|
| GET | `/scim/v2/Users` | `filter=userName eq "..."` or `externalId eq "..."`, `startIndex`, `count` |
| GET, PUT, PATCH, DELETE | `/scim/v2/Users/{id}` | `id` is the customer id. PUT without `active` keeps the current state |
| POST | `/scim/v2/Users` | `201` with `Location` |
| GET | `/scim/v2/Groups` | `filter=displayName eq "..."` or `externalId eq "..."`, `excludedAttributes=members` |
| GET, PUT, PATCH, DELETE | `/scim/v2/Groups/{id}` | PATCH: `members` add/remove/replace, `members[value eq "id"]` remove, rename |
| POST | `/scim/v2/Groups` | members that are not SCIM-managed users are ignored |
| GET | `/scim/v2/ServiceProviderConfig`, `/ResourceTypes`, `/Schemas` | discovery |

Errors use the SCIM error schema: `400 invalidFilter` / `invalidValue` / `invalidSyntax`, `401`, `404`, `409 uniqueness`.

## 8. Storage

Two collections, created on first use (no migration):

* `ScimUser`: one document per managed account, its id **is** the customer id. It holds the `externalId` and what SCIM granted.
* `ScimGroup`: the groups received and their mapping.

Turning the module off stops provisioning. Accounts, roles and the stored records stay as they are.
