import http from 'k6/http';
import { setTimeout } from 'k6/experimental/timers';
import { check } from 'k6';

export const options = {
  vus: 10,
  iterations: 10,
  duration: '60m'
}

export default async function() {
  const id = __VU
  const url = __ENV.BACKEND_URL
  const noopInterval = __ENV.NOOP_INTERVAL
  const second = 1000
  const totalSecs = __ENV.SECONDS
  const setTimeoutTotal = totalSecs - 5

  const expectedNoops = Math.floor(setTimeoutTotal / noopInterval)

  const tags = {
    deviceId: id
  }

  console.log(`Test case explaination for ${__VU}: timeout: ${totalSecs}s, setTimeout: ${setTimeoutTotal}, expectedNoops: ${expectedNoops}`)

  // Set closing timeout
  setTimeout(() => {
    const del = http.del(`${url}/event/forcedisconnect?id=${id}`)
    check(del, {
      'Delete status was 200': (r) => r.status == 200
    }, tags)
  }, setTimeoutTotal * second)

  const res = await http.asyncRequest('GET', `${url}/event/connect?id=${id}`, {}, {
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
