using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IFlashcardClassRepository
{
    Task<bool> ClassCodeExistsAsync(string classCode);
    Task AddClassAsync(Class classroom);
    Task<IReadOnlyList<Class>> GetTeacherClassesAsync(int teacherId);
    Task<Class?> GetOwnedClassAsync(int classId, int teacherId);
    Task<IReadOnlyList<Class>> SearchActiveClassesAsync(string query, int limit);
    Task<Class?> GetActiveClassByIdAsync(int classId);
    Task<ClassMember?> GetMemberIncludingDeletedAsync(int classId, int studentId);
    Task AddMemberAsync(ClassMember member);
    Task<IReadOnlyList<Class>> GetStudentClassesAsync(int studentId);
    Task<Quiz?> GetOwnedPublishedFlashcardAsync(int quizId, int teacherId);
    Task<FlashcardAssignment?> GetActiveAssignmentAsync(int classId, int quizId);
    Task AddAssignmentAsync(FlashcardAssignment assignment);
    Task<IReadOnlyList<FlashcardAssignment>> GetClassAssignmentsAsync(int classId);
    Task<IReadOnlyList<FlashcardAssignment>> GetStudentAssignmentsAsync(int studentId);
    Task<FlashcardAssignment?> GetStudentAssignmentAsync(int assignmentId, int studentId);
    Task<FlashcardStudyProgress?> GetProgressAsync(int assignmentId, int studentId, int questionId);
    Task AddProgressAsync(FlashcardStudyProgress progress);
    Task<int> SaveChangesAsync();
}
