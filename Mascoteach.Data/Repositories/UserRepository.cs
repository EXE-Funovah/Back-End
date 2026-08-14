using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Mascoteach.Data.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MascoteachDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsDeleted == false);
        }

        public Task<User?> GetByEmailIncludingDeletedAsync(string email)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByGoogleSubjectAsync(string googleSubject)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject && u.IsDeleted == false);
        }

        public Task<User?> GetByGoogleSubjectIncludingDeletedAsync(string googleSubject)
        {
            return _context.Users.FirstOrDefaultAsync(
                u => u.GoogleSubject == googleSubject);
        }

        public async Task<User?> GetByResetTokenHashAsync(string resetTokenHash)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ResetTokenHash == resetTokenHash && u.IsDeleted == false);
        }

        public async Task<User?> GetByEmailVerificationTokenHashAsync(string emailVerificationTokenHash)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == emailVerificationTokenHash && u.IsDeleted == false);
        }

        public async Task<User?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetAccountDeletionGraphAsync(int id)
        {
            return await _context.Users
                .AsSplitQuery()
                .Include(u => u.Documents)
                    .ThenInclude(d => d.Quizzes)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(question => question.Options)
                .Include(u => u.Documents)
                    .ThenInclude(d => d.Quizzes)
                        .ThenInclude(q => q.QuizAttempts)
                .Include(u => u.Documents)
                    .ThenInclude(d => d.Quizzes)
                        .ThenInclude(q => q.LiveSessions)
                            .ThenInclude(session => session.SessionParticipants)
                .Include(u => u.LiveSessions)
                    .ThenInclude(session => session.SessionParticipants)
                .Include(u => u.PaymentOrders)
                .Include(u => u.QuizAttempts)
                .Include(u => u.UserStat)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsDeleted == false);
        }

        public async Task<bool> TransferOwnedClassesBeforeDeactivationAsync(int teacherId)
        {
            var ownedClasses = await _context.Classes
                .Where(classroom => classroom.TeacherId == teacherId && !classroom.IsDeleted)
                .Include(classroom => classroom.ClassTeachers)
                    .ThenInclude(membership => membership.Teacher)
                .ToListAsync();

            var transfers = ownedClasses
                .Select(classroom => new
                {
                    Classroom = classroom,
                    NextOwner = classroom.ClassTeachers
                        .Where(membership =>
                            membership.TeacherId != teacherId
                            && !membership.IsDeleted
                            && !membership.Teacher.IsDeleted
                            && membership.Teacher.Role == "Teacher")
                        .OrderBy(membership => membership.JoinedAt)
                        .ThenBy(membership => membership.Id)
                        .FirstOrDefault()
                })
                .ToList();

            if (transfers.Any(item => item.NextOwner == null))
                return false;

            var now = DateTime.UtcNow;
            foreach (var transfer in transfers)
            {
                var currentOwner = transfer.Classroom.ClassTeachers.FirstOrDefault(membership =>
                    membership.TeacherId == teacherId && !membership.IsDeleted);
                if (currentOwner != null)
                    currentOwner.Role = "Teacher";

                transfer.NextOwner!.Role = "Owner";
                transfer.Classroom.TeacherId = transfer.NextOwner.TeacherId;
                transfer.Classroom.UpdatedAt = now;
            }

            return true;
        }

        public void HardDeleteAccountGraph(User user)
        {
            var documents = user.Documents.ToList();
            var quizzes = documents
                .SelectMany(document => document.Quizzes)
                .DistinctBy(quiz => quiz.Id)
                .ToList();
            var questions = quizzes
                .SelectMany(quiz => quiz.Questions)
                .DistinctBy(question => question.Id)
                .ToList();
            var options = questions
                .SelectMany(question => question.Options)
                .DistinctBy(option => option.Id)
                .ToList();
            var liveSessions = user.LiveSessions
                .Concat(quizzes.SelectMany(quiz => quiz.LiveSessions))
                .DistinctBy(session => session.Id)
                .ToList();
            var sessionParticipants = liveSessions
                .SelectMany(session => session.SessionParticipants)
                .DistinctBy(participant => participant.Id)
                .ToList();
            var quizAttempts = user.QuizAttempts
                .Concat(quizzes.SelectMany(quiz => quiz.QuizAttempts))
                .DistinctBy(attempt => attempt.Id)
                .ToList();
            var paymentOrders = user.PaymentOrders.ToList();

            // Classroom/flashcard relations must be removed before their users,
            // quizzes, and questions because rollout foreign keys use NO ACTION.
            var ownedQuizIds = quizzes.Select(quiz => quiz.Id).ToArray();
            var ownedQuestionIds = questions.Select(question => question.Id).ToArray();
            var ownedClasses = _context.Classes
                .Where(classroom => classroom.TeacherId == user.Id)
                .ToList();
            var ownedClassIds = ownedClasses.Select(classroom => classroom.Id).ToArray();
            var flashcardAssignments = _context.FlashcardAssignments
                .Where(assignment =>
                    assignment.AssignedBy == user.Id
                    || ownedClassIds.Contains(assignment.ClassId)
                    || ownedQuizIds.Contains(assignment.QuizId))
                .ToList();
            var assignmentIds = flashcardAssignments
                .Select(assignment => assignment.Id)
                .ToArray();
            var flashcardProgress = _context.FlashcardStudyProgresses
                .Where(progress =>
                    progress.StudentId == user.Id
                    || assignmentIds.Contains(progress.AssignmentId)
                    || ownedQuestionIds.Contains(progress.QuestionId))
                .ToList();
            var classMembers = _context.ClassMembers
                .Where(member =>
                    member.StudentId == user.Id
                    || ownedClassIds.Contains(member.ClassId))
                .ToList();
            var classTeachers = _context.ClassTeachers
                .Where(membership =>
                    membership.TeacherId == user.Id
                    || ownedClassIds.Contains(membership.ClassId))
                .ToList();

            if (flashcardProgress.Count > 0)
                _context.FlashcardStudyProgresses.RemoveRange(flashcardProgress);
            if (classMembers.Count > 0)
                _context.ClassMembers.RemoveRange(classMembers);
            if (classTeachers.Count > 0)
                _context.ClassTeachers.RemoveRange(classTeachers);
            if (flashcardAssignments.Count > 0)
                _context.FlashcardAssignments.RemoveRange(flashcardAssignments);
            if (ownedClasses.Count > 0)
                _context.Classes.RemoveRange(ownedClasses);
            if (options.Count > 0)
                _context.Options.RemoveRange(options);
            if (questions.Count > 0)
                _context.Questions.RemoveRange(questions);
            if (sessionParticipants.Count > 0)
                _context.SessionParticipants.RemoveRange(sessionParticipants);
            if (quizAttempts.Count > 0)
                _context.QuizAttempts.RemoveRange(quizAttempts);
            if (liveSessions.Count > 0)
                _context.LiveSessions.RemoveRange(liveSessions);
            if (quizzes.Count > 0)
                _context.Quizzes.RemoveRange(quizzes);
            if (documents.Count > 0)
                _context.Documents.RemoveRange(documents);
            if (paymentOrders.Count > 0)
                _context.PaymentOrders.RemoveRange(paymentOrders);
            if (user.UserStat != null)
                _context.UserStats.Remove(user.UserStat);

            _context.Users.Remove(user);
        }
    }
}
