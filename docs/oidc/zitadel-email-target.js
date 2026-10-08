// Zitadel Actions v2 target for the "preaccesstoken" function (test environment helper, Node 18+, no dependencies).
//
// Zitadel puts neither `email`/`email_verified` nor a `scope` claim into JWT access tokens. The GrandNode resource
// server links a user on first use by the verified e-mail address and checks RequiredScopes, so this target appends:
//   email, email_verified  - from the payload Zitadel sends (userinfo, falling back to the human user)
//   scope                  - the fixed value GRANDNODE_SCOPE (default "grandnode.api"); ID tokens never pass through
//                            this function, so they do not carry it and are refused by RequiredScopes
// Zitadel calls it as a "Call" target (the response is used): see docs/oidc-resource-server.md.
// Listens on PORT (default 8090). Signature verification (ZITADEL-Signature header) is NOT done: test use only.
const http = require('node:http');

const scope = process.env.GRANDNODE_SCOPE || 'grandnode.api';
const port = Number(process.env.PORT || 8090);

function claimsFor(payload) {
  const info = payload.userinfo || {};
  const human = (payload.user && payload.user.human) || {};
  const email = info.email || (human.email && (human.email.email || human.email)) || '';
  const verifiedRaw = info.email_verified !== undefined
    ? info.email_verified
    : human.email && (human.email.is_verified ?? human.email.isVerified);
  const claims = [{ key: 'scope', value: scope }];
  if (typeof email === 'string' && email) {
    claims.push({ key: 'email', value: email });
    claims.push({ key: 'email_verified', value: verifiedRaw === true });
  }
  return { append_claims: claims };
}

http.createServer((req, res) => {
  const chunks = [];
  req.on('data', (c) => chunks.push(c));
  req.on('end', () => {
    try {
      const payload = JSON.parse(Buffer.concat(chunks).toString('utf8') || '{}');
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(claimsFor(payload)));
    } catch (e) {
      res.writeHead(400);
      res.end();
    }
  });
}).listen(port, () => console.log(`zitadel-email-target listening on ${port}`));
