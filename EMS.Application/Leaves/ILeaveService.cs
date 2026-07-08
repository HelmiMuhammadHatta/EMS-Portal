using EMS.Application.Employees;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Leaves;

public interface ILeaveService
{
    Task<IEnumerable<LeaveTypeResponse>> GetLeaveTypesAsync(Guid? employeeId = null);
    Task<PaginatedResponse<LeaveRequestResponse>> GetLeaveRequestsAsync(LeaveRequestListQuery query, Guid requesterUserId, bool isRequesterAdmin);
    Task<Guid> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid requesterUserId);
    Task ApproveLeaveRequestAsync(Guid id, Guid requesterUserId, bool isRequesterAdmin);
    Task RejectLeaveRequestAsync(Guid id, RejectLeaveRequest request, Guid requesterUserId, bool isRequesterAdmin);
    Task CancelLeaveRequestAsync(Guid id, Guid requesterUserId);
    Task<IEnumerable<LeaveBalanceResponse>> GetLeaveBalancesAsync(Guid employeeId, int year);
    Task InitializeLeaveBalancesAsync(int year);
}
