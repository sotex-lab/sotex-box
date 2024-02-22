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
