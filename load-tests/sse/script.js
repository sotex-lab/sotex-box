import http from 'k6/http';
import { setTimeout, setInterval, clearInterval } from 'k6/experimental/timers';
import { check } from 'k6';

export const options = {
  vus: 10,
  iterations: 10
}

export default async function() {
  const id = __VU
  const url = __ENV.BACKEND_URL
  const second = 1000
  const totalSecs = __ENV.SECONDS
  const setTimeoutTotal = totalSecs - 5

  const tags = {
    deviceId: id
  }

  // Get random number between 0 and setTimeoutTotal to send some data on that intervals
  const messageAmount = Math.floor(Math.random() * setTimeoutTotal)

  console.log(`Test case explaination for ${__VU}: timeout: ${totalSecs}s, setTimeout: ${setTimeoutTotal}, messageAmount: ${messageAmount}`)

  // Set closing timeout
  setTimeout(() => {
    const del = http.del(`${url}/event/forcedisconnect?id=${id}`)
    check(del, {
      'Delete status was 200': (r) => r.status == 200
    }, tags)
  }, setTimeoutTotal * second)

  // Set interval for writing data to devices
  const intervalId = setInterval(() => {
    const sentMessage = http.get(`${url}/event/writedata?id=${id}&message=test`)
    check(sentMessage, {
      'Sent message status was 200' : (r) => r.status == 200
    }, tags)
  }, setTimeoutTotal * second / messageAmount)

  const res = await http.asyncRequest('GET', `${url}/event/connect?id=${id}`, {}, {
    timeout: `${totalSecs}s`
  })

  clearInterval(intervalId)

  check(res, {
    'Get status was 200': (r) => r.status == 200,
    'Received all messages': (r) => {
      let totalLines = 0
      for (let line of r.body.split('\n\n')) {
        if (line.includes("data: \"test\"")) {
          totalLines += 1
        }
      }

      // Expect to drop less than 2 messages
      return Math.abs(messageAmount - totalLines) < 2
    }
  }, tags);
}
