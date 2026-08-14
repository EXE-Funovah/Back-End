using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories;

public sealed class FlashcardClassRepository : IFlashcardClassRepository
{
    private readonly MascoteachDbContext _context;

    public FlashcardClassRepository(MascoteachDbContext context)
    {
        _context = context;
    }

    public Task<bool> ClassCodeExistsAsync(string classCode) =>
        _context.Classes.AnyAsync(item => item.ClassCode == classCode);

    public Task AddClassAsync(Class classroom) =>
        _context.Classes.AddAsync(classroom).AsTask();

    public async Task<IReadOnlyList<Class>> GetTeacherClassesAsync(int teacherId) =>
        await ClassQuery()
            .Where(item => item.TeacherId == teacherId
                || item.ClassTeachers.Any(membership =>
                    membership.TeacherId == teacherId && !membership.IsDeleted))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

    public Task<Class?> GetOwnedClassAsync(int classId, int teacherId) =>
        ClassQuery().FirstOrDefaultAsync(item => item.Id == classId && item.TeacherId == teacherId);

    public Task<Class?> GetOwnedClassForUpdateAsync(int classId, int teacherId) =>
        _context.Classes
            .Include(item => item.Teacher)
            .Include(item => item.ClassTeachers)
                .ThenInclude(membership => membership.Teacher)
            .Include(item => item.ClassMembers.Where(member => !member.IsDeleted))
            .Include(item => item.FlashcardAssignments.Where(assignment => !assignment.IsDeleted))
            .FirstOrDefaultAsync(item =>
                item.Id == classId && item.TeacherId == teacherId && !item.IsDeleted);

    public Task<Class?> GetAccessibleClassAsync(int classId, int teacherId) =>
        ClassQuery().FirstOrDefaultAsync(item =>
            item.Id == classId
            && (item.TeacherId == teacherId
                || item.ClassTeachers.Any(membership =>
                    membership.TeacherId == teacherId && !membership.IsDeleted)));

    public Task<User?> GetActiveTeacherByEmailAsync(string email) =>
        _context.Users.FirstOrDefaultAsync(user =>
            !user.IsDeleted
            && user.Role == "Teacher"
            && user.Email == email);

    public Task<ClassTeacher?> GetClassTeacherIncludingDeletedAsync(int classId, int teacherId) =>
        _context.ClassTeachers
            .Include(membership => membership.Teacher)
            .FirstOrDefaultAsync(membership =>
                membership.ClassId == classId && membership.TeacherId == teacherId);

    public Task AddClassTeacherAsync(ClassTeacher classTeacher) =>
        _context.ClassTeachers.AddAsync(classTeacher).AsTask();

    public async Task<IReadOnlyList<Class>> SearchActiveClassesAsync(string query, int limit)
    {
        var normalized = query.Trim();
        return await ClassQuery()
            .Where(item =>
                item.JoinPasswordHash != null
                && (item.Name.Contains(normalized)
                    || item.Teacher.FullName.Contains(normalized)
                    || item.ClassTeachers.Any(membership =>
                        !membership.IsDeleted
                        && membership.Teacher.FullName.Contains(normalized))))
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Teacher.FullName)
            .Take(limit)
            .ToListAsync();
    }

    public Task<Class?> GetActiveClassByIdAsync(int classId) =>
        ClassQuery().FirstOrDefaultAsync(item => item.Id == classId && item.JoinPasswordHash != null);

    public Task<ClassMember?> GetMemberIncludingDeletedAsync(int classId, int studentId) =>
        _context.ClassMembers.FirstOrDefaultAsync(item =>
            item.ClassId == classId && item.StudentId == studentId);

    public Task AddMemberAsync(ClassMember member) =>
        _context.ClassMembers.AddAsync(member).AsTask();

    public async Task<IReadOnlyList<Class>> GetStudentClassesAsync(int studentId) =>
        await ClassQuery()
            .Where(item => item.ClassMembers.Any(member =>
                member.StudentId == studentId && !member.IsDeleted))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

    public Task<Quiz?> GetOwnedPublishedFlashcardAsync(int quizId, int teacherId) =>
        _context.Quizzes
            .Include(item => item.Document)
            .Include(item => item.Questions.Where(question => !question.IsDeleted))
                .ThenInclude(question => question.Options.Where(option => !option.IsDeleted))
            .FirstOrDefaultAsync(item =>
                item.Id == quizId
                && !item.IsDeleted
                && !item.Document.IsDeleted
                && item.Document.OwnerId == teacherId
                && item.ActivityType == "Flashcard"
                && item.Status == "Teacher_Approved");

    public Task<FlashcardAssignment?> GetActiveAssignmentAsync(int classId, int quizId) =>
        _context.FlashcardAssignments.FirstOrDefaultAsync(item =>
            item.ClassId == classId && item.QuizId == quizId && !item.IsDeleted);

    public Task<FlashcardAssignment?> GetActiveAssignmentByIdAsync(int classId, int assignmentId) =>
        _context.FlashcardAssignments.FirstOrDefaultAsync(item =>
            item.Id == assignmentId && item.ClassId == classId && !item.IsDeleted);

    public Task AddAssignmentAsync(FlashcardAssignment assignment) =>
        _context.FlashcardAssignments.AddAsync(assignment).AsTask();

    public async Task<IReadOnlyList<FlashcardAssignment>> GetClassAssignmentsAsync(int classId) =>
        await AssignmentQuery()
            .Where(item => item.ClassId == classId)
            .OrderByDescending(item => item.AssignedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FlashcardAssignment>> GetStudentAssignmentsAsync(int studentId) =>
        await AssignmentQuery(studentId)
            .Where(item => item.Class.ClassMembers.Any(member =>
                member.StudentId == studentId && !member.IsDeleted))
            .OrderByDescending(item => item.AssignedAt)
            .ToListAsync();

    public Task<FlashcardAssignment?> GetStudentAssignmentAsync(int assignmentId, int studentId) =>
        AssignmentQuery(studentId).FirstOrDefaultAsync(item =>
            item.Id == assignmentId
            && item.Class.ClassMembers.Any(member =>
                member.StudentId == studentId && !member.IsDeleted));

    public Task<FlashcardStudyProgress?> GetProgressAsync(
        int assignmentId,
        int studentId,
        int questionId) =>
        _context.FlashcardStudyProgresses.FirstOrDefaultAsync(item =>
            item.AssignmentId == assignmentId
            && item.StudentId == studentId
            && item.QuestionId == questionId);

    public Task AddProgressAsync(FlashcardStudyProgress progress) =>
        _context.FlashcardStudyProgresses.AddAsync(progress).AsTask();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    private IQueryable<Class> ClassQuery() =>
        _context.Classes
            .AsNoTracking()
            .Where(item => !item.IsDeleted && !item.Teacher.IsDeleted)
            .Include(item => item.Teacher)
            .Include(item => item.ClassTeachers.Where(membership =>
                !membership.IsDeleted && !membership.Teacher.IsDeleted))
                .ThenInclude(membership => membership.Teacher)
            .Include(item => item.ClassMembers.Where(member =>
                !member.IsDeleted && !member.Student.IsDeleted))
                .ThenInclude(member => member.Student)
            .Include(item => item.FlashcardAssignments.Where(assignment => !assignment.IsDeleted));

    private IQueryable<FlashcardAssignment> AssignmentQuery(int? studentId = null)
    {
        IQueryable<FlashcardAssignment> query = _context.FlashcardAssignments
            .AsNoTracking()
            .Where(item =>
                !item.IsDeleted
                && !item.Class.IsDeleted
                && !item.Quiz.IsDeleted
                && !item.Quiz.Document.IsDeleted)
            .Include(item => item.Class)
                .ThenInclude(classroom => classroom.ClassMembers.Where(member => !member.IsDeleted))
            .Include(item => item.AssignedByNavigation)
            .Include(item => item.Quiz)
                .ThenInclude(quiz => quiz.Questions.Where(question => !question.IsDeleted))
                    .ThenInclude(question => question.Options.Where(option => !option.IsDeleted));

        if (studentId.HasValue)
        {
            var value = studentId.Value;
            query = query.Include(item => item.FlashcardStudyProgresses.Where(progress =>
                progress.StudentId == value));
        }

        return query;
    }
}
