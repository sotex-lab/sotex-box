using backend.Services.Batching;
using Shouldly;

namespace unit_tests;

[TestClass]
public class DeviceBatcherTests
{
    [TestMethod]
    public void Should_ReturnEmpty_If_EmptySent()
    {
        var deviceBatcher = new DeviceBatcher<string>();

        var response = deviceBatcher.NextBatch(new List<string>(), 'A', 5);

        response.IsSuccessful.ShouldBeTrue();

        response.Value.Count().ShouldBe(0);
    }

    [TestMethod]
    public void Should_Error_BadStartingChar()
    {
        var deviceBatcher = new DeviceBatcher<string>();
        var batch = new List<string>() { "AAAAA" };

        var badStartingChars = new[] { '+', '-', 'a', 'ć', '\\' };

        foreach (var badChar in badStartingChars)
        {
            var response = deviceBatcher.NextBatch(batch, badChar, 5);
            response.IsSuccessful.ShouldBeFalse("For char: " + badChar);
            response.Error.ShouldBe(BatchingError.BadStartingChar);
        }
    }

    [TestMethod]
    public void Should_ReturnCorrectBatch()
    {
        var deviceBatcher = new DeviceBatcher<string>();
        var batch = new List<string>()
        {
            "AAAA",
            "BBB",
            "DDD",
            "EEEEE",
            "0124",
            "1111",
            "FFFFF",
            "99"
        };

        var testCases = new[]
        {
            new
            {
                Starting = 'A',
                Max = 1,
                Want = new[] { "AAAA" }
            },
            new
            {
                Starting = 'A',
                Max = 5,
                Want = new[] { "AAAA", "BBB", "DDD", "EEEEE" }
            },
            new
            {
                Starting = 'E',
                Max = 16,
                Want = new[] { "EEEEE", "0124", "1111", "FFFFF", "99" }
            },
            new
            {
                Starting = 'A',
                Max = 15,
                Want = new[] { "AAAA", "BBB", "DDD", "EEEEE", "0124", "1111", "FFFFF" }
            }
        };

        foreach (var testCase in testCases)
        {
            var response = deviceBatcher.NextBatch(batch, testCase.Starting, (uint)testCase.Max);
            response.IsSuccessful.ShouldBeTrue();
            response.Value.ShouldBe(testCase.Want);
        }
    }

    [DataTestMethod]
    [DataRow('A', 1, 'B')]
    [DataRow('A', 17, 'A')]
    [DataRow('F', 1, '0')]
    [DataRow('0', 6, '6')]
    [DataRow('9', 1, 'A')]
    public void Should_CalculateCorrectlyNextKey(char start, int maxChars, char expected)
    {
        var deviceBatcher = new DeviceBatcher<string>();

        var result = deviceBatcher.NextKey(start, (uint)maxChars);

        result.ShouldBe(expected);
    }
}
