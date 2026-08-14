using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IFlashcardClassService
{
    Task<ClassResponse> CreateClassAsync(int teacherId, ClassCreateRequest request);
    Task<IReadOnlyList<ClassResponse>> GetTeacherClassesAsync(int teacherId);
    Task<ClassDetailResponse?> GetTeacherClassAsync(int classId, int teacherId);
    Task<ClassResponse> UpdateClassAsync(int classId, int ownerTeacherId, ClassUpdateRequest request);
    Task<ClassTeacherResponse> AddTeacherAsync(
        int classId,
        int ownerTeacherId,
        ClassTeacherAddRequest request);
    Task<bool> RemoveTeacherAsync(int classId, int teacherId, int ownerTeacherId);
    Task<ClassTeacherResponse> TransferOwnershipAsync(
        int classId,
        int ownerTeacherId,
        ClassOwnershipTransferRequest request);
    Task<bool> LeaveClassAsTeacherAsync(int classId, int teacherId);
    Task<IReadOnlyList<ClassSearchResponse>> SearchClassesAsync(string query);
    Task<ClassResponse> JoinClassAsync(int studentId, ClassJoinRequest request);
    Task<IReadOnlyList<ClassResponse>> GetStudentClassesAsync(int studentId);
    Task<bool> LeaveClassAsStudentAsync(int classId, int studentId);
    Task<bool> RemoveMemberAsync(int classId, int studentId, int teacherId);
    Task<FlashcardAssignmentResponse> AssignFlashcardAsync(
        int classId,
        int teacherId,
        FlashcardAssignmentCreateRequest request);
    Task<IReadOnlyList<FlashcardAssignmentResponse>> GetClassAssignmentsAsync(int classId, int teacherId);
    Task<bool> RemoveFlashcardAssignmentAsync(int classId, int assignmentId, int teacherId);
    Task<IReadOnlyList<FlashcardAssignmentResponse>> GetStudentAssignmentsAsync(int studentId);
    Task<FlashcardStudyResponse?> GetStudyAsync(int assignmentId, int studentId);
    Task<FlashcardProgressResponse?> UpdateProgressAsync(
        int assignmentId,
        int questionId,
        int studentId,
        FlashcardProgressUpdateRequest request);
}
