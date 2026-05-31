using AutoMapper;
using Mascoteach.Service.Mappers;

namespace Mascoteach.Tests;

public static class TestHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<QuizProfile>();
            cfg.AddProfile<QuestionProfile>();
            cfg.AddProfile<OptionProfile>();
            cfg.AddProfile<DocumentProfile>();
            cfg.AddProfile<LiveSessionProfile>();
            cfg.AddProfile<SessionParticipantProfile>();
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<AuthProfile>();
            cfg.AddProfile<GameTemplateProfile>();
        });
        return config.CreateMapper();
    }
}
