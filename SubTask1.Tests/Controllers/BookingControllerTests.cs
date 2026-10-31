using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using NSubstitute;
using webapp_f26_exam.Controllers;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;

namespace SubTask1.Tests.Controllers;

public class BookingControllerTests
{
    private const string UserId = "user-1";

    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly BookingController _controller;

    public BookingControllerTests()
    {
        // UserManager has no interface, so we substitute the class and only fake GetUserId
        var userManager = Substitute.For<UserManager<IdentityUser>>(
            Substitute.For<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
        userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns(UserId);

        _controller = new BookingController(
            _roomRepository,
            _bookingRepository,
            Substitute.For<ILogger<BookingController>>(),
            userManager);

        // The controller writes to TempData, which needs an HttpContext in unit tests
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
    }

    private static Booking CreateFutureBooking() => new()
    {
        Id = 10,
        RoomId = 1,
        Start = DateTime.Now.AddDays(1),
        End = DateTime.Now.AddDays(1).AddHours(2),
        Purpose = "Exam prep"
    };

    // Positive: booking a free room should save the booking and show the confirmation page
    [Fact]
    public async Task Create_WhenBookingIsValid_RedirectsToBookingConfirmed()
    {
        // Arrange
        var booking = CreateFutureBooking();
        _roomRepository.GetRoomByIdAsync(1).Returns(new Room { Id = 1, RoomCode = "PH-101" });
        _bookingRepository.HasConflictAsync(1, booking.Start, booking.End).Returns(false);
        _bookingRepository.CreateBookingAsync(booking).Returns(true);

        // Act
        var result = await _controller.Create(booking);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("BookingConfirmed", redirect.ActionName);
        Assert.Equal(UserId, booking.UserId);
        await _bookingRepository.Received(1).CreateBookingAsync(booking);
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
}
