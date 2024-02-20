```

BenchmarkDotNet v0.13.12, Ubuntu 22.04.4 LTS (Jammy Jellyfish)
12th Gen Intel Core i7-12700H, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2


```
| Method  | InitialSize | Implementation                       | amount | Mean         | Error        | StdDev       | Median       | Gen0    | Gen1   | Allocated |
|-------- |------------ |------------------------------------- |------- |-------------:|-------------:|-------------:|-------------:|--------:|-------:|----------:|
| **Add_N**   | **100**         | **EventCoordinatorConcurrentDictionary** | **10**     |  **6,700.00 ns** |   **402.043 ns** | **1,166.400 ns** |  **6,502.30 ns** |  **0.2899** |      **-** |    **3712 B** |
| **Add_N**   | **100**         | **EventCoordinatorConcurrentDictionary** | **100**    | **21,493.46 ns** |   **676.503 ns** | **1,994.684 ns** | **21,269.50 ns** |  **1.0986** |      **-** |   **14107 B** |
| **Add_N**   | **100**         | **EventCoordinatorConcurrentDictionary** | **1000**   | **56,868.17 ns** | **1,108.665 ns** | **1,037.046 ns** | **56,672.22 ns** | **10.0098** | **0.1221** |  **126309 B** |
| **Add_One** | **100**         | **EventCoordinatorConcurrentDictionary** | **?**      |     **53.75 ns** |     **0.975 ns** |     **1.334 ns** |     **53.32 ns** |  **0.0076** |      **-** |      **96 B** |
| **Add_N**   | **100**         | **EventCoordinatorMutex**                | **10**     |  **4,995.22 ns** |   **188.683 ns** |   **556.337 ns** |  **4,807.59 ns** |  **0.2594** |      **-** |    **3309 B** |
| **Add_N**   | **100**         | **EventCoordinatorMutex**                | **100**    | **18,878.24 ns** |   **726.818 ns** | **2,143.039 ns** | **18,731.93 ns** |  **0.8240** |      **-** |   **10566 B** |
| **Add_N**   | **100**         | **EventCoordinatorMutex**                | **1000**   | **38,567.99 ns** |   **765.102 ns** | **1,145.168 ns** | **38,331.85 ns** |  **7.5684** | **0.0610** |   **94441 B** |
| **Add_One** | **100**         | **EventCoordinatorMutex**                | **?**      |     **17.49 ns** |     **0.034 ns** |     **0.032 ns** |     **17.50 ns** |  **0.0051** |      **-** |      **64 B** |
| **Add_N**   | **100**         | **EventCoordinatorReaderWriterLock**     | **10**     |  **6,437.93 ns** |   **219.122 ns** |   **642.648 ns** |  **6,198.87 ns** |  **0.2899** |      **-** |    **3766 B** |
| **Add_N**   | **100**         | **EventCoordinatorReaderWriterLock**     | **100**    |           **NA** |           **NA** |           **NA** |           **NA** |      **NA** |     **NA** |        **NA** |
| **Add_N**   | **100**         | **EventCoordinatorReaderWriterLock**     | **1000**   |           **NA** |           **NA** |           **NA** |           **NA** |      **NA** |     **NA** |        **NA** |
| **Add_One** | **100**         | **EventCoordinatorReaderWriterLock**     | **?**      |     **51.04 ns** |     **0.311 ns** |     **0.276 ns** |     **51.10 ns** |  **0.0076** |      **-** |      **96 B** |
| **Add_N**   | **10000**       | **EventCoordinatorConcurrentDictionary** | **10**     |  **6,737.37 ns** |   **219.005 ns** |   **645.741 ns** |  **6,562.13 ns** |  **0.3281** |      **-** |    **4171 B** |
| **Add_N**   | **10000**       | **EventCoordinatorConcurrentDictionary** | **100**    | **22,448.28 ns** |   **712.412 ns** | **2,100.564 ns** | **22,358.23 ns** |  **1.3428** |      **-** |   **17058 B** |
| **Add_N**   | **10000**       | **EventCoordinatorConcurrentDictionary** | **1000**   | **59,378.91 ns** | **1,184.528 ns** | **3,182.158 ns** | **58,801.66 ns** | **10.4980** |      **-** |  **133028 B** |
| **Add_One** | **10000**       | **EventCoordinatorConcurrentDictionary** | **?**      |     **62.00 ns** |     **0.220 ns** |     **0.195 ns** |     **62.04 ns** |  **0.0101** |      **-** |     **128 B** |
| **Add_N**   | **10000**       | **EventCoordinatorMutex**                | **10**     |  **5,464.49 ns** |   **225.515 ns** |   **650.661 ns** |  **5,190.12 ns** |  **0.2899** |      **-** |    **3683 B** |
| **Add_N**   | **10000**       | **EventCoordinatorMutex**                | **100**    | **20,463.57 ns** |   **706.944 ns** | **2,084.439 ns** | **20,285.31 ns** |  **1.0986** |      **-** |   **13795 B** |
| **Add_N**   | **10000**       | **EventCoordinatorMutex**                | **1000**   | **40,370.36 ns** |   **786.752 ns** |   **841.816 ns** | **40,579.07 ns** |  **8.0566** | **0.1221** |  **100818 B** |
| **Add_One** | **10000**       | **EventCoordinatorMutex**                | **?**      |     **27.84 ns** |     **0.145 ns** |     **0.129 ns** |     **27.87 ns** |  **0.0076** |      **-** |      **96 B** |
| **Add_N**   | **10000**       | **EventCoordinatorReaderWriterLock**     | **10**     |  **6,701.33 ns** |   **235.912 ns** |   **695.593 ns** |  **6,364.40 ns** |  **0.3204** |      **-** |    **4066 B** |
| **Add_N**   | **10000**       | **EventCoordinatorReaderWriterLock**     | **100**    |           **NA** |           **NA** |           **NA** |           **NA** |      **NA** |     **NA** |        **NA** |
| **Add_N**   | **10000**       | **EventCoordinatorReaderWriterLock**     | **1000**   |           **NA** |           **NA** |           **NA** |           **NA** |      **NA** |     **NA** |        **NA** |
| **Add_One** | **10000**       | **EventCoordinatorReaderWriterLock**     | **?**      |     **63.96 ns** |     **0.172 ns** |     **0.161 ns** |     **63.99 ns** |  **0.0101** |      **-** |     **128 B** |
| **Add_N**   | **100000**      | **EventCoordinatorConcurrentDictionary** | **10**     |  **6,293.26 ns** |   **236.646 ns** |   **694.041 ns** |  **6,013.30 ns** |  **0.3281** |      **-** |    **4107 B** |
| **Add_N**   | **100000**      | **EventCoordinatorConcurrentDictionary** | **100**    | **22,452.06 ns** |   **619.333 ns** | **1,826.117 ns** | **22,555.13 ns** |  **1.4343** |      **-** |   **18046 B** |
| **Add_N**   | **100000**      | **EventCoordinatorConcurrentDictionary** | **1000**   | **52,098.20 ns** | **1,033.150 ns** | **1,889.173 ns** | **52,411.59 ns** | **11.2305** |      **-** |  **141306 B** |
| **Add_One** | **100000**      | **EventCoordinatorConcurrentDictionary** | **?**      |     **67.44 ns** |     **0.302 ns** |     **0.252 ns** |     **67.39 ns** |  **0.0107** |      **-** |     **136 B** |
| **Add_N**   | **100000**      | **EventCoordinatorMutex**                | **10**     |  **4,724.82 ns** |    **75.103 ns** |    **80.359 ns** |  **4,705.35 ns** |  **0.2899** |      **-** |    **3643 B** |
| **Add_N**   | **100000**      | **EventCoordinatorMutex**                | **100**    | **18,839.55 ns** |   **581.042 ns** | **1,713.215 ns** | **18,361.50 ns** |  **1.1597** |      **-** |   **14548 B** |
| **Add_N**   | **100000**      | **EventCoordinatorMutex**                | **1000**   | **39,517.13 ns** |   **788.836 ns** | **1,422.433 ns** | **39,263.24 ns** |  **8.7280** | **0.1221** |  **108882 B** |
| **Add_One** | **100000**      | **EventCoordinatorMutex**                | **?**      |     **27.40 ns** |     **0.061 ns** |     **0.051 ns** |     **27.41 ns** |  **0.0083** |      **-** |     **104 B** |
| **Add_N**   | **100000**      | **EventCoordinatorReaderWriterLock**     | **10**     |  **6,893.68 ns** |   **239.329 ns** |   **698.135 ns** |  **6,715.11 ns** |  **0.3357** |      **-** |    **4272 B** |
| **Add_N**   | **100000**      | **EventCoordinatorReaderWriterLock**     | **100**    | **22,724.46 ns** |   **647.550 ns** | **1,909.316 ns** | **22,648.35 ns** |  **1.4343** |      **-** |   **18126 B** |
| **Add_N**   | **100000**      | **EventCoordinatorReaderWriterLock**     | **1000**   |           **NA** |           **NA** |           **NA** |           **NA** |      **NA** |     **NA** |        **NA** |
| **Add_One** | **100000**      | **EventCoordinatorReaderWriterLock**     | **?**      |     **59.38 ns** |     **0.193 ns** |     **0.171 ns** |     **59.38 ns** |  **0.0107** |      **-** |     **136 B** |

Benchmarks with issues:
  Benchmarks.Add_N: DefaultJob [InitialSize=100, Implementation=EventCoordinatorReaderWriterLock, amount=100]
  Benchmarks.Add_N: DefaultJob [InitialSize=100, Implementation=EventCoordinatorReaderWriterLock, amount=1000]
  Benchmarks.Add_N: DefaultJob [InitialSize=10000, Implementation=EventCoordinatorReaderWriterLock, amount=100]
  Benchmarks.Add_N: DefaultJob [InitialSize=10000, Implementation=EventCoordinatorReaderWriterLock, amount=1000]
  Benchmarks.Add_N: DefaultJob [InitialSize=100000, Implementation=EventCoordinatorReaderWriterLock, amount=1000]
