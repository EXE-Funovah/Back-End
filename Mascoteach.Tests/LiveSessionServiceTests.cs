using AutoMapper;
using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Service.DTOs;
using Mascoteach.Service.Implementations;
using Moq;
using Xunit;

namespace Mascoteach.Tests;

public class LiveSessionServiceTests
{
    private readonly Mock<ILiveSessionRepository> _repo = new();
    private readonly IMapper _mapper = TestHelper.CreateMapper();
    private readonly LiveSessionService _sut;

    public LiveSessionServiceTests()
    {
        _sut = new LiveSessionService(_repo.Object, _mapper);
    }

    private LiveSession MakeSession(int teacherId = 10) => new()
    {
        Id = 1, TeacherId = teacherId, QuizId = 1, TemplateId = 1,
        GamePin = "123456", Status = "Waiting", CreatedAt = DateTime.Now
    };

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_OwnerTeacher_ReturnsTrue()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession());
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    [Fact]
    public async Task UpdateAsync_WrongTeacher_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.False(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((LiveSession?)null);

        Assert.False(await _sut.UpdateAsync(1, 10, new LiveSessionUpdateRequest { Status = "Active" }));
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_OwnerTeacher_ReturnsTrue()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession());
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.DeleteAsync(1, 10));
    }

    [Fact]
    public async Task DeleteAsync_WrongTeacher_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.False(await _sut.DeleteAsync(1, 10));
    }

    // ── ToggleDeleteAsync ──

    [Fact]
    public async Task ToggleDeleteAsync_OwnerTeacher_Toggles()
    {
        var session = MakeSession();
        session.IsDeleted = false;
        _repo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(session);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleDeleteAsync(1, 10);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task ToggleDeleteAsync_WrongTeacher_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(MakeSession(teacherId: 99));

        Assert.Null(await _sut.ToggleDeleteAsync(1, 10));
    }

    // ── UpdateStatusByPinAsync ──

    [Fact]
    public async Task UpdateStatusByPinAsync_ValidPin_UpdatesStatus()
    {
        var session = MakeSession();
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        Assert.True(await _sut.UpdateStatusByPinAsync("123456", "Active"));
        Assert.Equal("Active", session.Status);
    }

    [Fact]
    public async Task UpdateStatusByPinAsync_InvalidPin_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByPinAsync("000000")).ReturnsAsync((LiveSession?)null);

        Assert.False(await _sut.UpdateStatusByPinAsync("000000", "Active"));
    }

    // ── GetByPinAsync ──

    [Fact]
    public async Task GetByPinAsync_EndedSession_ReturnsNull()
    {
        var session = MakeSession();
        session.Status = "Ended";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);

        Assert.Null(await _sut.GetByPinAsync("123456"));
    }

    [Fact]
    public async Task GetByPinAsync_ActiveSession_ReturnsResponse()
    {
        var session = MakeSession();
        session.Status = "Active";
        _repo.Setup(r => r.GetByPinAsync("123456")).ReturnsAsync(session);

        var result = await _sut.GetByPinAsync("123456");

        Assert.NotNull(result);
        Assert.Equal("123456", result!.GamePin);
    }
}
