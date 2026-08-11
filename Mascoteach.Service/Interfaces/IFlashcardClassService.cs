using Mascoteach.Service.DTOs;

namespace Mascoteach.Service.Interfaces;

public interface IFlashcardClassService
{
    Task<ClassResponse> CreateClassAsync(int teacherId, ClassCreateRequest request);
    Task<IReadOnlyList<ClassResponse>> GetTeacherClassesAsync(int teacherId);
    Task<ClassDetailResponse?> GetTeacherClassAsync(int classId, int teacherId);
    Task<ClassResponse> JoinClassAsync(int studentId, ClassJoinRequest request);
    Task<IReadOnlyList<ClassResponse>> GetStudentClassesAsync(int studentId);
    Task<bool> RemoveMemberAsync(int classId, int studentId, int teacherId);
    Task<FlashcardAssignmentResponse> AssignFlashcardAsync(
        int classId,
        int teacherId,
        FlashcardAssignmentCreateRequest request);
    Task<IReadOnlyList<FlashcardAssignmentResponse>> GetClassAssignmentsAsync(int classId, int teacherId);
    Task<IReadOnlyList<FlashcardAssignmentResponse>> GetStudentAssignmentsAsync(int studentId);
    Task<FlashcardStudyResponse?> GetStudyAsync(int assignmentId, int studentId);
    Task<FlashcardProgressResponse?> UpdateProgressAsync(
        int assignmentId,
        int questionId,
        int studentId,
        FlashcardProgressUpdateRequest request);
}
