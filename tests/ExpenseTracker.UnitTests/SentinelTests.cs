using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Models;
using NSubstitute;

namespace ExpenseTracker.UnitTests;

public class SentinelTests
{
    [Fact]
    public async Task Pipeline_WhenTestsRun_ResolvesFrameworkAndProjectReferences()
    {
        // Arrange
        var expected = new User("sentinel") { Id = 1 };
        var repository = Substitute.For<IUserRepository>();
        repository.GetById(1).Returns(expected);

        // Act
        var result = await repository.GetById(1);

        // Assert
        result.Should().BeSameAs(expected);
    }
}
