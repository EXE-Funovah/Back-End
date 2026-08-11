using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Interfaces;

namespace Mascoteach.Service.Implementations
{
    public class LiveSessionService : ILiveSessionService
    {
        private readonly ILiveSessionRepository _liveSessionRepository;
        private readonly IMapper _mapper;

        public LiveSessionService(ILiveSessionRepository liveSessionRepository, IMapper mapper)
        {
            _liveSessionRepository = liveSessionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LiveSessionResponse>> GetAllAsync()
        {
            var sessions = await _liveSessionRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<LiveSessionResponse>>(sessions);
        }

        public async Task<IEnumerable<LiveSessionResponse>> GetByTeacherIdAsync(int teacherId)
        {
            var sessions = await _liveSessionRepository.GetByTeacherIdAsync(teacherId);
            return _mapper.Map<IEnumerable<LiveSessionResponse>>(sessions);
        }

        public async Task<LiveSessionResponse?> GetByIdAsync(int id)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<LiveSessionReportResponse?> GetReportAsync(int id, int teacherId)
        {
            var session = await _liveSessionRepository.GetReportByIdAsync(id);
            if (session == null || session.TeacherId != teacherId)
                return null;

            var participants = session.SessionParticipants
                .Where(participant => !participant.IsDeleted)
                .ToList();
            var answers = participants
                .SelectMany(participant => participant.SessionAnswers)
                .Where(answer => answer.SessionId == session.Id)
                .ToList();
            var questions = session.Quiz.Questions
                .Where(question => !question.IsDeleted)
                .OrderBy(question => question.Position)
                .ThenBy(question => question.Id)
                .ToList();

            var participantReports = participants
                .Select(participant =>
                {
                    var participantAnswers = participant.SessionAnswers
                        .Where(answer => answer.SessionId == session.Id)
                        .OrderBy(answer => answer.Question.Position)
                        .ThenBy(answer => answer.AnsweredAt)
                        .Select(answer => new LiveSessionAnswerReportResponse
                        {
                            QuestionId = answer.QuestionId,
                            Position = answer.Question.Position,
                            QuestionText = answer.Question.QuestionText,
                            SelectedOptionId = answer.SelectedOptionId,
                            SelectedOptionText = answer.SelectedOption.OptionText,
                            IsCorrect = answer.IsCorrect,
                            ScoreAwarded = answer.ScoreAwarded,
                            AnsweredAt = answer.AnsweredAt
                        })
                        .ToList();
                    var correctCount = participantAnswers.Count(answer => answer.IsCorrect);

                    return new LiveSessionParticipantReportResponse
                    {
                        ParticipantId = participant.Id,
                        StudentName = participant.StudentName,
                        TotalScore = participant.TotalScore ?? participantAnswers.Sum(answer => answer.ScoreAwarded),
                        AnsweredCount = participantAnswers.Count,
                        CorrectCount = correctCount,
                        IncorrectCount = participantAnswers.Count - correctCount,
                        AccuracyRate = Percentage(correctCount, participantAnswers.Count),
                        Answers = participantAnswers
                    };
                })
                .OrderByDescending(participant => participant.TotalScore)
                .ThenByDescending(participant => participant.CorrectCount)
                .ThenBy(participant => participant.StudentName)
                .ToList();

            for (var index = 0; index < participantReports.Count; index++)
                participantReports[index].Rank = index + 1;

            var questionReports = questions
                .Select(question =>
                {
                    var questionAnswers = answers
                        .Where(answer => answer.QuestionId == question.Id)
                        .ToList();
                    var correctCount = questionAnswers.Count(answer => answer.IsCorrect);

                    return new LiveSessionQuestionReportResponse
                    {
                        QuestionId = question.Id,
                        Position = question.Position,
                        QuestionText = question.QuestionText,
                        AnsweredCount = questionAnswers.Count,
                        CorrectCount = correctCount,
                        IncorrectCount = questionAnswers.Count - correctCount,
                        UnansweredCount = Math.Max(0, participants.Count - questionAnswers.Count),
                        CorrectRate = Percentage(correctCount, questionAnswers.Count)
                    };
                })
                .ToList();

            var totalCorrectAnswers = answers.Count(answer => answer.IsCorrect);

            return new LiveSessionReportResponse
            {
                SessionId = session.Id,
                QuizId = session.QuizId,
                QuizTitle = session.Quiz.Title,
                GamePin = session.GamePin,
                Status = session.Status,
                CreatedAt = session.CreatedAt,
                TotalQuestions = questions.Count,
                TotalParticipants = participants.Count,
                TotalAnswers = answers.Count,
                CorrectAnswers = totalCorrectAnswers,
                CorrectRate = Percentage(totalCorrectAnswers, answers.Count),
                AverageScore = participantReports.Count == 0
                    ? 0
                    : Math.Round(participantReports.Average(participant => (decimal)participant.TotalScore), 2),
                Participants = participantReports,
                Questions = questionReports
            };
        }

        public async Task<LiveSessionResponse?> GetByPinAsync(string pin)
        {
            var session = await _liveSessionRepository.GetByPinAsync(pin);
            if (session == null) return null;
            if (session.Status == "Ended") return null;
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<LiveSessionResponse> CreateAsync(int teacherId, LiveSessionCreateRequest request)
        {
            var gamePin = await GenerateUniquePinAsync();

            var session = _mapper.Map<LiveSession>(request);
            session.TeacherId = teacherId;
            session.GamePin = gamePin;
            session.Status = "Waiting";
            session.CreatedAt = DateTime.Now;

            await _liveSessionRepository.AddAsync(session);
            await _liveSessionRepository.SaveChangesAsync();
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<bool> UpdateAsync(int id, int teacherId, LiveSessionUpdateRequest request)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            if (session == null || session.TeacherId != teacherId) return false;

            if (!CanTransition(session.Status, request.Status)) return false;

            if (string.Equals(session.Status, request.Status, StringComparison.OrdinalIgnoreCase))
                return true;

            session.Status = request.Status;

            _liveSessionRepository.Update(session);
            return await _liveSessionRepository.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id, int teacherId)
        {
            var session = await _liveSessionRepository.GetByIdAsync(id);
            if (session == null || session.TeacherId != teacherId) return false;

            _liveSessionRepository.Delete(session);
            return await _liveSessionRepository.SaveChangesAsync() > 0;
        }

        public async Task<LiveSessionResponse?> ToggleDeleteAsync(int id, int teacherId)
        {
            var session = await _liveSessionRepository.GetByIdIncludingDeletedAsync(id);
            if (session == null || session.TeacherId != teacherId) return null;

            session.IsDeleted = !session.IsDeleted;
            _liveSessionRepository.Update(session);
            await _liveSessionRepository.SaveChangesAsync();
            return _mapper.Map<LiveSessionResponse>(session);
        }

        public async Task<bool> UpdateStatusByPinAsync(string gamePin, string status)
        {
            var session = await _liveSessionRepository.GetByPinAsync(gamePin);
            if (session == null) return false;

            if (!CanTransition(session.Status, status)) return false;

            if (string.Equals(session.Status, status, StringComparison.OrdinalIgnoreCase))
                return true;

            session.Status = status;
            _liveSessionRepository.Update(session);
            return await _liveSessionRepository.SaveChangesAsync() > 0;
        }

        private async Task<string> GenerateUniquePinAsync()
        {
            var random = new Random();
            string pin;
            do
            {
                pin = random.Next(100000, 999999).ToString();
            }
            while (await _liveSessionRepository.GetByPinAsync(pin) != null);
            return pin;
        }

        private static bool CanTransition(string currentStatus, string nextStatus)
        {
            if (string.Equals(currentStatus, nextStatus, StringComparison.OrdinalIgnoreCase))
                return true;

            return (string.Equals(currentStatus, "Waiting", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(nextStatus, "Active", StringComparison.OrdinalIgnoreCase))
                || (string.Equals(currentStatus, "Active", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(nextStatus, "Ended", StringComparison.OrdinalIgnoreCase));
        }

        private static decimal Percentage(int numerator, int denominator)
        {
            return denominator == 0
                ? 0
                : Math.Round(numerator * 100m / denominator, 2);
        }
    }
}
