This test is meant to simulate the real world worst case scenario for our backend. The backend is fine-tuned for 1000 devices connected to [SSE](https://en.wikipedia.org/wiki/Server-sent_events) endpoint. In this test we are running 1200 connections to the endpoint for 1 hour. The goal of the test was to see if we would drop any packets for continous 1 hour and if the application would be able to handle 120% load of the fine-tune desired load.

## Setup - Hardware
The backend, alongisde all other production services like [`nginx`](https://www.nginx.com/), [`prometheus`](https://prometheus.io/) and [`grafana`](https://grafana.com/) were deployed to AWS EC2 instance. For this test we chose the following configuration:

| Model | vCPU | Memory (GiB) | Instance Storage (GB) | Network Bandwidth (Gbps) | EBS Bandwidth (Mbps) |
| ----- | ---- | ------------ | --------------------- | ------------------------ | ------------- |
| c5.large | 4 | 8            | EBS-Only              | Up to 10                 | Up to 4750 |

To see and compare all the choices there are on AWS consolidate this [link](https://aws.amazon.com/ec2/instance-types/).

## Setup - Software
The code for all the tools we run in production was checked in the repository and pulled on ec2 instance using `git`. Apart from that we've used [`docker`](https://www.docker.com/) and its [`docker compose`](https://docs.docker.com/compose/) to run the stack. The service that was open to the internet was `nginx` which is an entrypoint to the whole system.

## Test run
The test was run from two physical machines, both laptops of roughly the same architecture
```bash
Ubuntu 22.04.4 LTS (Jammy Jellyfish)
12th Gen Intel Core i7-12700H, 1 CPU, 20 logical and 14 physical cores
```
Each laptop simulated 600 connections to the backend for 1 continous hour using [`k6`](https://k6.io/). The command that was used to run the test from one laptop is:
```bash
k6 run load-tests/sse/connectionTest.js --vus 600 --iterations 600 --duration 60m --env BACKEND_URL=http://ec2-18-156-71-117.eu-central-1.compute.amazonaws.com  --env NOOP_INTERVAL=15 --env SECONDS=3600 --env PREFIX=<laptop-prefix>
```
### Math
Since we are limited by the lack of support for propper SSE handler in `k6` we had to take into account the amount of load this will take on the laptop itself so it doesn't get killed by [Linux OOM Killer](https://neo4j.com/developer/kb/linux-out-of-memory-killer/).
```bash
noop = "data: 0\n\n"                                      # Data that comes with each noop
noopSize = noop.length * 1B = 9B                          # Size of 1 noop in Bytes
totalSeconds = 3600                                       # Desired seconds for a test to run
testShutdown = totalSeconds - 5 = 3595                    # Shutdown signal for one test
noopInterval = 15                                         # 1 noop per 15 seconds
vus = 600                                                 # Total VUs per laptop
expectedNoopsPerVu = testShutdown / noopInterval = 239    # Total expected noops per test
totalNoops = vus * expectedNoopsPerVu = 143400            # Total noops per laptop
totalMemory = totalNoops * noopSize = 1290600B ~ 1.23MB   # Total memory consumption per laptop
```

## Results
After 1 hour of testing the results were as follows:
```bash

     ✓ Delete status was 200
     ✓ Get status was 200
     ✓ Received all messages

     checks.........................: 100.00% ✓ 1800     ✗ 0  
     data_received..................: 2.9 MB  817 B/s
     data_sent......................: 207 kB  58 B/s
     http_req_blocked...............: avg=797.26ms min=26.53ms med=1.04s   max=3.13s  p(90)=2.07s    p(95)=2.08s  
     http_req_connecting............: avg=796.1ms  min=26.41ms med=1.04s   max=3.13s  p(90)=2.07s    p(95)=2.08s  
     http_req_duration..............: avg=29m57s   min=27.01ms med=29m57s  max=59m58s p(90)=59m56s   p(95)=59m56s  
       { expected_response:true }...: avg=29m57s   min=27.01ms med=29m57s  max=59m58s p(90)=59m56s   p(95)=59m56s  
     http_req_failed................: 0.00%   ✓ 0        ✗ 1200
     http_req_receiving.............: avg=29m51s   min=20.04µs med=29m50s  max=59m44s p(90)=59m43s   p(95)=59m43s  
     http_req_sending...............: avg=155.16µs min=15.37µs med=92.26µs max=1.41ms p(90)=310.81µs p(95)=508.01µs
     http_req_tls_handshaking.......: avg=0s       min=0s      med=0s      max=0s     p(90)=0s       p(95)=0s  
     http_req_waiting...............: avg=6.67s    min=26.8ms  med=7.09s   max=14.5s  p(90)=13.75s   p(95)=13.93s  
     http_reqs......................: 1200    0.333439/s
     iteration_duration.............: avg=59m56s   min=59m55s  med=59m56s  max=59m58s p(90)=59m57s   p(95)=59m58s  
     iterations.....................: 600     0.166719/s
     vus............................: 31      min=31     max=600
     vus_max........................: 600     min=600    max=600


running (0h59m58.9s), 000/600 VUs, 600 complete and 0 interrupted iterations
default ✓ [======================================] 600 VUs  0h59m58.9s/1h0m0s  600/600 shared iters
```
Result interpretation:

* `data_received`: In the math above we calculated roughly 2 MB of data expected, but that is only the size of the _bodies_ received. We couldn't take into account all the headers that will arive since AWS places custom headers for each request and that accounts for the remaining 0.9 MB of data
* `http_req_duration`: This was the goal of the test, we see that for `p(95)` (the 95th procentile, or in other words _for 95% of requests_) the request lasted the whole 59m56s which aligns with the test `testShutdown` of 3595 seconds, 1 second miss is due to all tests requesting their own shutdown in parallel and backend has the dictionary behind a `Mutex` which causes the delay.
* `iteration_duration`: This confirms the `http_req_duration` and ensures that the test doesn't do anything apart from preparation, test and assertion, where preparation and assertion for `p(95)` lasted for roughly 2 seconds.

The whole test metrics for 1 laptop can be found in [`test-output.json`](/assets/test-output.json){:download="test-output.json"}

<figure markdown="span">
  ![CPU utilization](/sotex-box/assets/cpu-util.png)
  <figcaption>CPU utilization for the test duration</figcaption>
</figure>

For the whole test duration we can see that there were 2 spikes in the backend CPU utilization. The average utilization was around `14.3%`. Explaination of the spikes:

* spike at `20:52`: related to gen 1 [garbage collection](https://www.codeproject.com/Articles/1060/Garbage-Collection-in-NET)
* spike at `21:17`: roughly the time where all the tests ended which created 1200 http delete requests to the backend

<figure markdown="span">
  ![Memory consumption](/sotex-box/assets/memory.png)
  <figcaption>Memory consumption</figcaption>
</figure>

During the whole test memory consumed `500MB` of memory maximum for all connected devices. If you take a look at the [hardware spec](#setup-hardware) this means that there is plenty of resources left for `nginx`, `prometheus` and `grafana` to work alongside future tuning of database and serving other resources from the backend.
