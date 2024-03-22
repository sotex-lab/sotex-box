import http from 'k6/http';
import { setTimeout } from 'k6/experimental/timers';
import { check } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

export const options = {
  vus: 10,
  iterations: 10,
  duration: '60m'
}

export default async function() {
  const id = uuidv4();
  const url = __ENV.BACKEND_URL
  const noopInterval = __ENV.NOOP_INTERVAL
  const second = 1000
  const totalSecs = __ENV.SECONDS
  const setTimeoutTotal = totalSecs - 5
  const prefix_id = (__ENV.PREFIX !== undefined && __ENV.PREFIX !== null) ? __ENV.PREFIX : '';
  const totalId = prefix_id + id

  const expectedNoops = Math.floor(setTimeoutTotal / noopInterval)

  const tags = {
    deviceId: totalId
  }

  console.log(`Test case explaination for ${totalId}: timeout: ${totalSecs}s, setTimeout: ${setTimeoutTotal}, expectedNoops: ${expectedNoops}`)

  // Set closing timeout
  setTimeout(() => {
    const del = http.del(`${url}/event/forcedisconnect?id=${totalId}`)
    check(del, {
      'Delete status was 200': (r) => r.status == 200
    }, tags)
  }, setTimeoutTotal * second)

  const res = await http.asyncRequest('GET', `${url}/event/connect?id=${totalId}`, {}, {
    timeout: `${totalSecs}s`
  })

  check(res, {
    'Get status was 200': (r) => r.status == 200,
    'Received all messages': (r) => {
      let totalLines = 0
      for (let line of r.body.split('\n\n')) {
        if (line.includes("data: \"noop\"")) {
          totalLines += 1
        }
      }

      // Expect to drop less than 2 messages
      return Math.abs(expectedNoops - totalLines) < 2
    }
  }, tags);
}
