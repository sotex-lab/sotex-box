We use [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and that is the technology we use mainly for the backend side.

## Directory layout
<!-- To update the list bellow run in root directory:
tree dotnet -L 1 -a
Remember to remove .gitignored files if they render-->
```bash
dotnet
├── backend # Main api code
├── benchmarks # Benchmarks used for various performance testing
├── integration-tests # Integration tests related to backend
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
For integration testing, we use [XUnit](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0). All the tests are in `integration-tests` and can be run with `make`:
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
