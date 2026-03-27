import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost';

export const options = {
  vus: 20,
  duration: '2m',
  thresholds: {
    http_req_duration: ['p(95)<3000'],
  },
};

export default function () {
  const search = http.get(`${BASE_URL}/search?q=book`);
  check(search, { 'search 200': (r) => r.status === 200 });

  const match = search.body.match(/product-title">\s*<a href="(\/[^"]+)"/i);
  if (match && match[1]) {
    sleep(0.3 + Math.random() * 0.7);
    const product = http.get(`${BASE_URL}${match[1]}`);
    check(product, { 'product 200': (r) => r.status === 200 });
  }

  sleep(0.5 + Math.random() * 1.0);
}
