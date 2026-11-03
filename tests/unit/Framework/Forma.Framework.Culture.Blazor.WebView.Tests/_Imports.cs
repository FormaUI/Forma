global using Xunit;

// An application sets the process's default cultures, which every test here reads, so the tests run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
