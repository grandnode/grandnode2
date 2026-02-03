/**
 * k6 load test – GrandNode storefront
 * Simulates browsing: homepage, catalog, product page, search.
 * Configure via env: BASE_URL, VUS, DURATION, or SCENARIO=1k (1k users) / SCENARIO=90k.
 *
 * Run 1k users (local):  k6 run -e SCENARIO=1k storefront.js
 * Run (local k6):        k6 run storefront.js
 * With env:              k6 run -e BASE_URL=http://127.0.0.1:8080 -e VUS=500 -e DURATION=3m storefront.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://127.0.0.1:8080';

// Optional fixed load (overridden by scenarios if present)
const VUS = __ENV.VUS ? parseInt(__ENV.VUS, 10) : 50;
const DURATION = __ENV.DURATION || '2m';

const defaultScenario = {
  default: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '1m', target: Math.min(VUS, 100) },
      { duration: DURATION, target: VUS },
      { duration: '30s', target: 0 },
    ],
    gracefulRampDown: '30s',
    gracefulStop: '30s',
    startTime: '0s',
  },
};
// 1k users: for local testing (ramp to 1000 VUs, hold 3m, ramp down)
const scenario1k = {
  ramp_1k: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '2m', target: 1000 },
      { duration: '3m', target: 1000 },
      { duration: '1m', target: 0 },
    ],
    gracefulRampDown: '30s',
    gracefulStop: '30s',
    startTime: '0s',
  },
};

const scenario90k = {
  ramp_90k: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '5m', target: 500 },
      { duration: '20m', target: 2000 },
      { duration: '30m', target: 3000 },
      { duration: '10m', target: 1000 },
      { duration: '2m', target: 0 },
    ],
    gracefulRampDown: '1m',
    gracefulStop: '30s',
    startTime: '0s',
  },
};

const scenarioMap = {
  '1k': scenario1k,
  '90k': scenario90k,
};
export const options = {
  scenarios: scenarioMap[__ENV.SCENARIO] || defaultScenario,
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<5000'],
  },
};

// (e.g. 90k requests or 90k “user actions” over time)
export default function () {
  const res = http.get(`${BASE_URL}/`);
  check(res, { 'homepage status 200': (r) => r.status === 200 });
  sleep(0.5 + Math.random() * 1.5);

  const catalog = http.get(`${BASE_URL}/catalog`);
  check(catalog, { 'catalog ok': (r) => r && r.status < 500 });
  sleep(0.3 + Math.random() * 1);

  const search = http.get(`${BASE_URL}/search?q=test`);
  check(search, { 'search ok': (r) => r && r.status < 500 });
  sleep(0.5 + Math.random() * 2);
}
