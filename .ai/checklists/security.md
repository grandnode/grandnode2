# Checklist: Security

Run when the change touches authentication, authorization, user input, scoped data, secrets, payments, or file handling.

Complementary to `.ai/skills/security-review.md` (procedure) and `.ai/knowledge/security.md` (patterns).

---

## Authorization

- [ ] Every admin controller carries `[AuthorizeAdmin]`, `[Area("...")]`, and `[PermissionAuthorize(...)]`.
- [ ] The permission used is the right one — not a broader permission that happened to be at hand.
- [ ] New actions on an existing controller are covered by the class-level attribute, or carry their own.
- [ ] Authorization is enforced in the controller, not only by hiding a link in the view.
- [ ] A new permission has a `PermissionProvider` entry **and** a migration, so existing installations receive it.

## Trust boundaries

- [ ] Ids arriving in a request (`storeId`, `vendorId`, `customerId`, entity ids) are re-checked against server-side context before any write.
- [ ] A vendor cannot read or modify another vendor's records by changing an id.
- [ ] A customer cannot reach another customer's orders, addresses, downloads, or documents.
- [ ] A store owner cannot reach another store's data.
- [ ] Mass-assignment is bounded — the bound model does not expose fields the caller must not set.

## Input handling

- [ ] Every model crossing the boundary has a validator covering the new fields.
- [ ] Guard clauses on public service methods.
- [ ] No query built by concatenating user input.
- [ ] Uploaded files: extension and content type checked, size bounded, filename not used as a path.
- [ ] Redirect targets are validated — no open redirect from a returned URL parameter.

## Output

- [ ] User-supplied content is encoded by default.
- [ ] `Html.Raw` is used only on operator-authored or already-sanitized content.
- [ ] Error messages returned to the customer do not disclose internal paths, ids, or stack traces.
- [ ] Database-sourced HTML that may contain `{{ }}` is `v-pre`-guarded so it is not compiled as a Vue template.

## Secrets and sensitive data

- [ ] No credential, API key, connection string, or pepper committed.
- [ ] Secrets read from configuration, not constants.
- [ ] No secret, token, password, card data, or full personal record in a log message or an exception message.
- [ ] Personal data exposure respects the customer's consent settings where the feature is consent-gated.

## Authentication

- [ ] Password handling goes through the central verification path — no ad-hoc hashing or comparison.
- [ ] Failed login returns a result value and does not disclose whether the account exists.
- [ ] Session, cookie, and two-factor behavior unchanged unless that is the point of the change.
- [ ] External authentication tokens are not logged or persisted beyond what the flow requires.

## Web hygiene

- [ ] Forms and AJAX mutations carry antiforgery tokens.
- [ ] State-changing endpoints are POST, not GET.
- [ ] No third-party script added without a consent gate where one is required.
- [ ] No external CDN reference introduced in a storefront view.

## Payments

- [ ] No card data stored or logged.
- [ ] Amounts are re-derived server-side, never taken from the request.
- [ ] Provider callbacks verify their signature or shared secret before acting.
- [ ] A repeated callback cannot double-apply a payment.

## Plugins

- [ ] A plugin cannot escalate privilege through the services it registers.
- [ ] `Install()` / `Uninstall()` do not touch data outside the plugin's own settings and resources.
- [ ] Provider `LimitedToStores` / `LimitedToGroups` are honoured by consumers of the provider.

## Verification

- [ ] Each finding here was confirmed by reading the code, not inferred from a name.
- [ ] Anything that could not be verified is stated as unverified in the PR.
