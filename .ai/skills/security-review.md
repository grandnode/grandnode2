# Security Review

## Purpose
Identify security vulnerabilities, unsafe defaults, authorization gaps, data exposure risks, and misuse of cryptography or secrets in a software repository.

## When To Use
Use this skill when reviewing authentication, authorization, session handling, input validation, output encoding, file handling, secrets, cryptography, dependency changes, network calls, logging, or access to sensitive data.

Use this skill for changes that expose APIs, process user input, change permissions, handle payments, process personal data, or modify infrastructure configuration.

## When Not To Use
Do not use this skill for purely cosmetic UI changes, documentation-only edits, test fixture updates with no production effect, or isolated refactors that do not affect behavior or trust boundaries.

Do not use this skill as a replacement for legal, privacy, or compliance review when formal certification is required.

## Inputs Required
- Repository root.
- Change set or target files to review.
- Threat model or trust boundaries, if available.
- Authentication and authorization model.
- Data classification or sensitive data definitions, if available.
- Dependency and deployment configuration relevant to the change.

## Instructions

### Mandatory Rules
1. Identify the entry points affected by the change.
2. Identify trust boundaries crossed by the change.
3. Identify sensitive data read, written, logged, transmitted, or stored.
4. Verify authentication is required where appropriate.
5. Verify authorization checks enforce the intended resource owner, role, tenant, or permission.
6. Check for injection risks in database queries, commands, templates, paths, URLs, headers, and serialized data.
7. Check input validation and output encoding at all external boundaries.
8. Check file upload, download, archive, and path handling for traversal, type confusion, overwrite, and size risks.
9. Check secret handling for hardcoded values, unsafe storage, accidental logging, and exposure in configuration.
10. Check cryptographic usage for approved primitives, correct randomness, safe key handling, and no custom algorithms.
11. Check dependency changes for known vulnerable packages or unsafe transitive exposure when dependency data is available.
12. Check error handling and logging for sensitive information disclosure.
13. Prioritize findings by exploitability and impact.
14. Provide file and line references for each finding when possible.
15. State when no security issues are found.

### Recommendations
1. Prefer deny-by-default authorization.
2. Prefer centralized validation, encoding, and policy enforcement patterns already used by the repository.
3. Recommend tests that prove exploit paths are blocked.
4. Separate confirmed vulnerabilities from hardening suggestions.
5. Include severity, attack scenario, and remediation for each confirmed issue.

## Constraints
- Never provide exploit instructions beyond what is needed to prove impact and remediation.
- Never expose real secrets, tokens, credentials, or personal data in the output.
- Never assume a control exists without verifying it in code or configuration.
- Never mark an issue as fixed by a caller-side control when the server-side trust boundary still accepts unsafe input.
- Never recommend custom cryptography.

## Expected Output
Produce a security review report with:
- Findings ordered by severity.
- Each finding containing affected asset, attack path, impact, evidence, and remediation.
- Hardening recommendations separated from vulnerabilities.
- Open questions or assumptions, if any.
- A validation checklist result.

## Validation Checklist
- [ ] Entry points were identified.
- [ ] Trust boundaries were checked.
- [ ] Authentication and authorization were checked.
- [ ] Sensitive data handling was checked.
- [ ] Injection, path, file, and serialization risks were checked.
- [ ] Secret handling was checked.
- [ ] Logging and error disclosure were checked.
- [ ] Findings include concrete remediation.
- [ ] No secrets or personal data are disclosed in the report.

## Examples

### Example 1: Missing Tenant Check
Input: An endpoint loads an order by ID and returns it to any authenticated user.

Output finding: The endpoint authenticates the caller but does not verify tenant or owner access for the order. A user can request another tenant's order ID. Add a tenant-scoped query or explicit authorization check and add a negative access test.

### Example 2: Unsafe File Path
Input: A download action joins a user-supplied filename with a storage directory.

Output finding: The filename is not normalized or constrained to the storage root. A crafted path can read unintended files. Resolve the full path, enforce the storage root prefix, reject traversal segments, and test traversal attempts.

