using EMS.Application.Leaves;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Reflection;

namespace EMS.Tests;

public class LeaveServiceTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<LeaveService>> _mockLogger;
    private readonly LeaveService _leaveService;

    public LeaveServiceTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockLogger = new Mock<ILogger<LeaveService>>();
        _leaveService = new LeaveService(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public void CalculateLeaveDays_ExcludesWeekends()
    {
        // Arrange
        // Friday to Monday = 4 days total (Fri, Sat, Sun, Mon), but only 2 working days (Fri, Mon)
        var start = new DateTime(2026, 7, 3); // Friday
        var end = new DateTime(2026, 7, 6);   // Monday

        var method = typeof(LeaveService).GetMethod("CalculateLeaveDays", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (int)method!.Invoke(_leaveService, new object[] { start, end })!;

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var employee = new Employee { Id = Guid.NewGuid(), UserId = userId, FullName = "Test Employee" };
        var leaveTypeId = Guid.NewGuid();

        _mockContext.Setup(c => c.Employees).ReturnsDbSet(new List<Employee> { employee });
        _mockContext.Setup(c => c.LeaveRequests).ReturnsDbSet(new List<LeaveRequest>());
        
        // Setup balance with 1 day remaining (Total 10, Used 9)
        var balance = new LeaveBalance { EmployeeId = employee.Id, LeaveTypeId = leaveTypeId, Year = DateTime.UtcNow.Year, TotalDays = 10, UsedDays = 9 };
        _mockContext.Setup(c => c.LeaveBalances).ReturnsDbSet(new List<LeaveBalance> { balance });

        var request = new CreateLeaveRequest
        {
            LeaveTypeId = leaveTypeId,
            StartDate = new DateTime(2026, 8, 3), // Monday
            EndDate = new DateTime(2026, 8, 4),   // Tuesday (requires 2 days)
            Reason = "Vacation"
        };

        // Act
        Func<Task> act = async () => await _leaveService.CreateLeaveRequestAsync(request, userId);

        // Assert
        await act.Should().ThrowAsync<Exception>().Where(e => e.Message.Contains("Insufficient leave balance"));
    }
}
