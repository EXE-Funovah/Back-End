using Mascoteach.Data.Models;

namespace Mascoteach.Data.Interfaces;

public interface IFlashcardClassRepository
{
    Task<bool> ClassCodeExistsAsync(string classCode);
    Task AddClassAsync(Class classroom);
    Task<IReadOnlyList<Class>> GetTeacherClassesAsync(int teacherId);
    Task<Class?> GetOwnedClassAsync(int classId, int teacherId);
    Task<Class?> GetOwnedClassForUpdateAsync(int classId, int teacherId);
    Task<Class?> GetAccessibleClassAsync(int classId, int teacherId);
    Task<User?> GetActiveTeacherByEmailAsync(string email);
    Task<ClassTeacher?> GetClassTeacherIncludingDeletedAsync(int classId, int teacherId);
    Task AddClassTeacherAsync(ClassTeacher classTeacher);
    Task<IReadOnlyList<Class>> SearchActiveClassesAsync(string query, int limit);
    Task<Class?> GetActiveClassByIdAsync(int classId);
    Task<ClassMember?> GetMemberIncludingDeletedAsync(int classId, int studentId);
    Task AddMemberAsync(ClassMember member);
    Task<IReadOnlyList<Class>> GetStudentClassesAsync(int studentId);
    Task<Quiz?> GetOwnedPublishedFlashcardAsync(int quizId, int teacherId);
    Task<FlashcardAssignment?> GetActiveAssignmentAsync(int classId, int quizId);
    Task<FlashcardAssignment?> GetActiveAssignmentByIdAsync(int classId, int assignmentId);
    Task AddAssignmentAsync(FlashcardAssignment assignment);
    Task<IReadOnlyList<FlashcardAssignment>> GetClassAssignmentsAsync(int classId);
    Task<IReadOnlyList<FlashcardAssignment>> GetStudentAssignmentsAsync(int studentId);
    Task<FlashcardAssignment?> GetStudentAssignmentAsync(int assignmentId, int studentId);
    Task<FlashcardStudyProgress?> GetProgressAsync(int assignmentId, int studentId, int questionId);
    Task AddProgressAsync(FlashcardStudyProgress progress);
    Task<int> SaveChangesAsync();
}
