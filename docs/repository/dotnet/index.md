We use [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and that is the technology we use mainly for the backend side.

## Directory layout
<!-- To update the list bellow run in root directory:
tree dotnet -L 1 -a
Remember to remove .gitignored files if they render-->
```bash
dotnet
├── backend # Main api code
├── benchmarks # Benchmarks used for various performance testing
├── e2e-tester # End to end tester related to backend stack
├── integration-tests # Integration tests related to backend
├── model # Place where we keep core models and contracts used to transmit data
├── persistence # Place where we keep database specific code
├── sse-handler # Library for handling server sent events
└── unit-tests # Unit tests for libraries
```

## High level backend overview
The backend is implemented in [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet) framework and is built as an API server that is used for serving information to both real users and hardware devices. It is a glue that ties together information and orchestrates events. Here you will find the controllers that receive requests for managing resources that the server maintains.

## Benchmarks
[Benchmarks](https://en.wikipedia.org/wiki/Benchmarking) are a place where we measure how different implementations compare to one another giving us a way to easily devise which implementation should make it to prod. Bare in mind that we cannot measure every singe bit of the code, but we try to keep a high threashold for performance and having these metrics are of great importance. Not everything can be measured with benchmarks and for some metrics we need a live environment so there is a plan to add benchmarks in form of tests that will measure how well the server performs during heavy load.

## Testing
There is a heavy emphasis on both unit and integration testing. This doesn't mean we follow [TDD](https://en.wikipedia.org/wiki/Test-driven_development) but we just want to ensure the code going to an environment does what it is supposed to. On top of that each bug or incident we receive we will create a test for it making sure that the same bug doesn't repeat itself.

Sometimes when writing tests developers tend to adhere to [DRY](https://en.wikipedia.org/wiki/Don't_repeat_yourself) principles too much. Try to aim for maintainable and readable tests rather than adhere to a principle.

To run all the tests you can use `make`:
```bash
make dotnet-tests
```

### Unit testing
For unit testing, we use [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest). All the tests are in `unit-tests` project and can be run with `make`:
```bash
make dotnet-unit-tests
```

### Integration testing
For integration testing, we use [XUnit](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0). Apart from that we use [Testcontainers](https://dotnet.testcontainers.org/) to mock the dependencies of our system. All the tests are in `integration-tests` and can be run with `make`:
```bash
make dotnet-integration-tests
```

Keep in mind that we cannot cover all the possible scenarious with integration tests that can occur in real life scenarious. If we added authentication and authorization to all controllers we don't need to have a test for **EVERY** controller saying something in the lines of pseudo code:
```c#
[Fact]
public async Task Should_NotDoStuff_AuthError() {
    // Arange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("protected/resource");

    // Assert
    response.StatusCode.ShouldBeIn(new [] { HttpStatusCode.FORBIDDEN, HttpStatusCode.UNAUTHORIZED })
}
```

Try to write tests that cover the business logic and add value to the codebase ensuring that future changes to some code don't accidentally break the logic. Writing meaningless tests results in denied PR's.

### Load testing
For load testing, we use [k6](https://grafana.com/docs/k6/latest/). Since we know the goal we want to reach we need these kinds of tests to ensure that our implementation can work in desired circumstances. Keep in mind that our solution doesn't have to, and probably won't work for parameters greater than defined by these tests. If backend can work under load specified by these tests it is expected that it will work in production as well.

For this tool there is no `make` action. One needs to run it on their own with their own desired parameters. To run the tests you should:

1. The stack needs to run: `make compose-up`
2. Run the `k6` tool:
```bash
k6 run load-tests/sse/connectionTest.js --vus 10 --iterations 10 --env BACKEND_URL=<backend-url> --env NOOP_INTERVAL=<noop-interval> --env SECONDS=<seconds>
```

Parameter explaination:

1. `--vus`: number of virtual users to run. Each virtual user will be a separate [goroutine](https://go.dev/tour/concurrency/1) and will create a connection to the backend.
2. `--iterations`: number of iterations to run. It should be atleast equal to `--vus`. If `iterations` is larger than `vus` then some users will run the test logic twice.
3. `--env`: represent environment variables for the test

Environment variable explaination:

1. `BACKEND_URL`: depending on how you run backend this option can and will vary. If you run `make run-backend` this should have value of `http://localhost:5029`. If you run with `make compose-up` this should have value of `http://localhost:8080`. If you want to run tests against staging you should provide the dns name of staging and so on.
2. `NOOP_INTERVAL`: specified noop interval in seconds. This number represents on how many seconds does the server send a `noop` signal. By default the server sends a `noop` each 15s so if you haven't provided an override then this parameter should have the value of `15`.
3. `SECONDS`: the duration of the test in seconds. If you want to run a test for 10 minutes you should provide value of `600` here.

Example run for stack run with `make compose-up` for `30` users for `10` minutes:
```bash
k6 run load-tests/sse/connectionTest.js --vus 30 --iterations 30 --env BACKEND_URL=http://localhost:8080 --env NOOP_INTERVAL=15 --env SECONDS=600
```

**Important notes**:

Keep in mind that this test can be quite heavy. The test runs the threads in parallel and keeps them open for `SECONDS` amount of time. Since the server sends the `noop` signal at fixed rates you can easily calculate how much data will the test create. The noop signal looks like `data: "noop"\n\n` and by default the server sends 1 noop each 15 seconds.
```bash
noop = "data: \"noop\"\\n\\n"                       # 14 bytes
NOOP_INTERVAL = 15                                  # 1 noop per 15 seconds
SECONDS = 600                                       # 10 minutes
vus = 10                                            # 10 virtual users
iterations = 10                                     # 10 iterations
bytes_per_user = SECONDS * noop / NOOP_INTERVAL     # 560 bytes
total_bytes = bytes_per_user * vus                  # 5600 bytes ~ 5.46 kB
```
What this means is that one should be careful on how many users he spins and how long does the test last since it can be heavy on the machine running the test itself.

### E2E testing
For e2e testing we use a custom written framework that is tailored for our stack. It is making use of [dind](https://medium.com/@shivam77kushwah/docker-inside-docker-e0483c51cc2c). Since e2e tests cover the broader use cases of a certain software feature we need to treat them separately. The goal is to test complete use-cases in a dedicated environment. The tester has the ability to spin up multiple instances that will be used for running tests against. Usually e2e tests are expensive and could take some time but their mission is to catch production level bugs so they don't have to be run as often.

To run e2e tests you simply run:
```bash
make dotnet-e2e-tests
```
Since you will be running a couple of environment instances locally it is expected to see a higher cpu usage like the following:
```bash
docker stats --no-stream
CONTAINER ID   NAME                                                       CPU %     MEM USAGE / LIMIT     MEM %     NET I/O           BLOCK I/O       PIDS
437f4a57c351   e2e-tester-2                                               2.13%     749.1MiB / 31.06GiB   2.35%     75.6kB / 41kB     108MB / 182MB   450
fe8eccc6fb95   e2e-tester-1                                               20.01%    964.1MiB / 31.06GiB   3.03%     151kB / 43kB      331MB / 231MB   452
8a68ef0e37ed   e2e-tester-0                                               1.91%     972.7MiB / 31.06GiB   3.06%     80.2kB / 51kB     341MB / 230MB   452
31dce92219b1   testcontainers-ryuk-2cc9deca-ab4e-46a3-a8eb-2792f0b0cb73   0.00%     9.723MiB / 31.06GiB   0.03%     10.6kB / 4.62kB   6.51MB / 0B     9
```
At the end of the run there will be an output that present the overview of the run:
```bash
+ ------ + -------                    +
| Legend | Meaning                    |
+ ------ + -------                    +
| ✅     | successful                 |
+ ------ + -------                    +
| ❌     | failed                     |
+ ------ + -------                    +
| ❕     | failed but allowed to fail |
+ ------ + -------                    +

+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +
| Name            | Result | Duration          | Retries | Error                                                     | Description                                                |
+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +
| Ping backend    | ✅     | 00:00:00.0027491s | 0       | /                                                         | When the stack is up, backend should be available          |
+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +
| Ping Grafana    | ❕     | 00:00:00s         | 3       | response.IsSuccessStatusCode should be True but was False | When the stack is up, Grafana should be reachable          |
+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +
| Ping Prometheus | ✅     | 00:00:00.0046954s | 0       | /                                                         | When the stack is up, Prometheus should be pingable        |
+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +
| Create ad       | ✅     | 00:00:15.0352998s | 0       | /                                                         | Create a new ad and fully process it with a background job |
+ ----            + ------ + --------          + ------- + -----                                                     + -----------                                                +

+ ------    + ----- + ---------- +
| Result    | Count | Procentage |
+ ------    + ----- + ---------- +
| Succeeded | 3     | 75%        |
+ ------    + ----- + ---------- +
| Failed    | 1     | 25%        |
+ ------    + ----- + ---------- +
```
Some of the tests are allowed to fail, here there is a grafana ping that is failing due to misconfiguration for development testing purposes which is not needed for this environment. Tests marked as allowed to fail won't result in _overall_ outcome of the test run. Usually these will be [flaky tests](https://www.datadoghq.com/knowledge-center/flaky-tests/).

There isn't a general rule on when to write an e2e test and when not to. There are some things that are mission critical and should be covered with an e2e test but it really depends what is being tested. If the logic is being tested usually it is okay to write just a unit test. If there is a lot of services talking it is better to have an integration test. If there is a business goal that needs to be fulfilled and there is are timings that need to be tested (for e.g. cron firing) there is a need for an e2e test.

A good example of an e2e test is a `Create ad` test that has to test what happens when the user creates an ad, receives a link from backend to which to upload his ad to and then wait for the backend to store additional info that comes from the blob storage via a queue.
