using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using webapp_f26_exam.Controllers;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;
using webapp_f26_exam.ViewModels;
using Xunit;

namespace SubTask1.Tests.Controllers;

public class BookingControllerTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly ILogger<BookingController> _logger = Substitute.For<ILogger<BookingController>>();
    private readonly UserManager<IdentityUser> _userManager;
    private readonly BookingController _controller;

    private const string UserId = "test-user-id";

    public BookingControllerTests()
    {
        var userStore = Substitute.For<IUserStore<IdentityUser>>();
        _userManager = Substitute.For<UserManager<IdentityUser>>(userStore, null, null, null, null, null, null, null, null);
        _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns(UserId);

        _controller = new BookingController(_roomRepository, _bookingRepository, _logger, _userManager);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, UserId), new Claim(ClaimTypes.Name, "test@example.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
    }

    private static BookingCreateViewModel CreateFutureBooking()
    {
        var tomorrow = DateTime.Today.AddDays(1).AddHours(10);
        return new()
        {
            RoomId = 1,
            Start = tomorrow,
            End = tomorrow.AddHours(2),
            Purpose = "Exam prep"
        };
    }

    // Positive: booking a free room should save the booking and show the confirmation page
    [Fact]
    public async Task Create_WhenBookingIsValid_RedirectsToBookingConfirmed()
    {
        // Arrange
        var booking = CreateFutureBooking();
        _roomRepository.GetRoomByIdAsync(1).Returns(new Room { Id = 1, RoomCode = "PH-101" });
        _bookingRepository.HasConflictAsync(1, booking.Start, booking.End).Returns(false);
        _bookingRepository.CreateBookingAsync(Arg.Any<Booking>()).Returns(true);

        // Act
        var result = await _controller.Create(booking);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        await _bookingRepository.Received(1).CreateBookingAsync(Arg.Is<Booking>(b =>
            b.RoomId == 1 && b.UserId == UserId && b.Purpose == "Exam prep"));
    }

    // Negative: booking a room that is already taken at that time should be rejected
    [Fact]
    public async Task Create_WhenTimeSlotIsAlreadyBooked_ReturnsViewWithError()
    {
        // Arrange
        var booking = CreateFutureBooking();
        _roomRepository.GetRoomByIdAsync(1).Returns(new Room { Id = 1 });
        _bookingRepository.HasConflictAsync(1, booking.Start, booking.End).Returns(true);

        // Act
        var result = await _controller.Create(booking);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        await _bookingRepository.DidNotReceive().CreateBookingAsync(Arg.Any<Booking>());
    }

    // Negative: booking in the past should be rejected
    [Fact]
    public async Task Create_WhenStartTimeIsInThePast_ReturnsViewWithError()
    {
        // Arrange
        var pastBooking = new BookingCreateViewModel
        {
            RoomId = 1,
            Start = DateTime.Now.AddHours(-2),
            End = DateTime.Now.AddHours(-1),
            Purpose = "Past booking"
        };
        _roomRepository.GetRoomByIdAsync(1).Returns(new Room { Id = 1 });

        // Act
        var result = await _controller.Create(pastBooking);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("Start"));
        await _bookingRepository.DidNotReceive().CreateBookingAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task RemoveParticipant_WhenUserIsOrganizer_RemovesParticipantAndRedirects()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 5,
            UserId = UserId
        };
        _bookingRepository.GetBookingByIdAsync(5).Returns(booking);
        _bookingRepository.RemoveParticipantAsync(5, "participant-123").Returns(true);

        // Act
        var result = await _controller.RemoveParticipant(5, "participant-123");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        await _bookingRepository.Received(1).RemoveParticipantAsync(5, "participant-123");
    }

    [Fact]
    public async Task RemoveParticipant_WhenUserIsNotOrganizer_ReturnsUnauthorized()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 5,
            UserId = "different-organizer"
        };
        _bookingRepository.GetBookingByIdAsync(5).Returns(booking);

        // Act
        var result = await _controller.RemoveParticipant(5, "participant-123");

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        await _bookingRepository.DidNotReceive().RemoveParticipantAsync(Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ToggleJoinFlag_WhenUserIsOrganizer_TogglesAndUpdates()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 5,
            UserId = UserId,
            JoinFlag = false
        };
        _bookingRepository.GetBookingByIdAsync(5).Returns(booking);
        _bookingRepository.UpdateBookingAsync(booking).Returns(true);

        // Act
        var result = await _controller.ToggleJoinFlag(5);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        Assert.True(booking.JoinFlag);
        await _bookingRepository.Received(1).UpdateBookingAsync(booking);
    }

    [Fact]
    public async Task SendChatMessage_WhenUserIsNotParticipant_ReturnsForbid()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 7,
            UserId = "organizer-id",
            Participants = new List<BookingParticipant>()
        };
        _bookingRepository.GetBookingByIdAsync(7).Returns(booking);

        // Act
        var result = await _controller.SendChatMessage(7, "Hello everyone");

        // Assert
        Assert.IsType<ForbidResult>(result);
        await _bookingRepository.DidNotReceive().AddMessageAsync(Arg.Any<BookingChat>());
    }

    [Fact]
    public async Task SendChatMessage_WhenUserIsParticipant_AddsMessageAndRedirects()
    {
        // Arrange
        var booking = new Booking
        {
            Id = 7,
            UserId = "organizer-id",
            Participants = new List<BookingParticipant>
            {
                new() { BookingId = 7, UserId = UserId }
            }
        };
        _bookingRepository.GetBookingByIdAsync(7).Returns(booking);
        _bookingRepository.AddMessageAsync(Arg.Any<BookingChat>()).Returns(true);

        // Act
        var result = await _controller.SendChatMessage(7, "Joining the study session!");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        await _bookingRepository.Received(1).AddMessageAsync(Arg.Is<BookingChat>(m =>
            m.BookingId == 7 &&
            m.UserId == UserId &&
            m.Message == "Joining the study session!"));
    }

    [Fact]
    public async Task SendChatMessage_WhenMessageIsEmpty_RedirectsWithChatError()
    {
        // Act
        var result = await _controller.SendChatMessage(7, "   ");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        Assert.Equal("Message cannot be empty.", _controller.TempData["ChatError"]);
        await _bookingRepository.DidNotReceive().AddMessageAsync(Arg.Any<BookingChat>());
    }

    [Fact]
    public async Task EditChatMessage_WhenUserIsNotAuthor_ReturnsForbid()
    {
        // Arrange
        var message = new BookingChat
        {
            Id = 15,
            BookingId = 7,
            UserId = "other-user",
            Message = "Original text"
        };
        _bookingRepository.GetMessageByIdAsync(15).Returns(message);

        // Act
        var result = await _controller.EditChatMessage(15, "Unauthorized update");

        // Assert
        Assert.IsType<ForbidResult>(result);
        await _bookingRepository.DidNotReceive().UpdateMessageAsync(Arg.Any<BookingChat>());
    }

    [Fact]
    public async Task DeleteChatMessage_WhenUserIsAuthor_DeletesMessageAndRedirects()
    {
        // Arrange
        var message = new BookingChat
        {
            Id = 20,
            BookingId = 7,
            UserId = UserId,
            Message = "My message"
        };
        _bookingRepository.GetMessageByIdAsync(20).Returns(message);
        _bookingRepository.DeleteMessageAsync(20).Returns(true);

        // Act
        var result = await _controller.DeleteChatMessage(20);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        await _bookingRepository.Received(1).DeleteMessageAsync(20);
    }
}
