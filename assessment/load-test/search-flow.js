import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost';

const SEARCH_TERMS = ['laptop', 'computer', 'phone', 'camera', 'shoes', 'book', 'gift card'];
const CATEGORY_PATHS = ['/computers', '/electronics', '/apparel', '/digital-downloads'];

// Three VU profiles with cyclic, alternated ramp patterns.
// "searchers" peak while "browsers" dip and vice versa,
// "product_viewers" follows a sine-like pattern offset from both.
export const options = {
  scenarios: {
    searchers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 8 },
        { duration: '2m', target: 12 },
        { duration: '2m', target: 4 },
        { duration: '2m', target: 14 },
        { duration: '2m', target: 6 },
        { duration: '2m', target: 10 },
        { duration: '2m', target: 2 },
        { duration: '2m', target: 0 },
      ],
      exec: 'searchFlow',
    },
    browsers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 4 },
        { duration: '2m', target: 6 },
        { duration: '2m', target: 10 },
        { duration: '2m', target: 3 },
        { duration: '2m', target: 8 },
        { duration: '2m', target: 5 },
        { duration: '2m', target: 7 },
        { duration: '2m', target: 0 },
      ],
      exec: 'browseFlow',
    },
    product_viewers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 3 },
        { duration: '2m', target: 8 },
        { duration: '2m', target: 5 },
        { duration: '2m', target: 10 },
        { duration: '2m', target: 4 },
        { duration: '2m', target: 6 },
        { duration: '1m', target: 0 },
      ],
      exec: 'productViewFlow',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.10'],
  },
};

// Scenario 1: keyword search -> click product
export function searchFlow() {
  const term = SEARCH_TERMS[Math.floor(Math.random() * SEARCH_TERMS.length)];
  const search = http.get(`${BASE_URL}/search?q=${term}`);
  check(search, { 'search 200': (r) => r.status === 200 });

  const match = search.body.match(/product-title">\s*<a href="(\/[^"]+)"/i);
  if (match && match[1]) {
    sleep(0.3 + Math.random() * 0.7);
    const product = http.get(`${BASE_URL}${match[1]}`);
    check(product, { 'product from search 200': (r) => r.status === 200 });
  }

  sleep(0.5 + Math.random() * 1.5);
}

// Scenario 2: category browsing (triggers search without keywords)
export function browseFlow() {
  const home = http.get(`${BASE_URL}/`);
  check(home, { 'homepage 200': (r) => r.status === 200 });
  sleep(0.3 + Math.random() * 0.5);

  const category = CATEGORY_PATHS[Math.floor(Math.random() * CATEGORY_PATHS.length)];
  const catPage = http.get(`${BASE_URL}${category}`);
  check(catPage, { 'category 200': (r) => r.status === 200 });

  // Sometimes also do a search without keywords
  if (Math.random() < 0.4) {
    sleep(0.2 + Math.random() * 0.5);
    const emptySearch = http.get(`${BASE_URL}/search`);
    check(emptySearch, { 'empty search 200': (r) => r.status === 200 });
  }

  sleep(0.5 + Math.random() * 1.0);
}

// Scenario 3: direct product page views (triggers pricing calculation)
export function productViewFlow() {
  const term = SEARCH_TERMS[Math.floor(Math.random() * SEARCH_TERMS.length)];
  const search = http.get(`${BASE_URL}/search?q=${term}`);
  check(search, { 'search for product 200': (r) => r.status === 200 });

  const matches = search.body.matchAll(/product-title">\s*<a href="(\/[^"]+)"/gi);
  const products = [...matches].map((m) => m[1]);

  // Visit up to 3 products from results
  const toVisit = products.slice(0, Math.min(3, products.length));
  for (const path of toVisit) {
    sleep(0.5 + Math.random() * 1.0);
    const product = http.get(`${BASE_URL}${path}`);
    check(product, { 'product detail 200': (r) => r.status === 200 });
  }

  sleep(0.5 + Math.random() * 1.5);
}
