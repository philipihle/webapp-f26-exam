using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using webapp_f26_exam.Controllers;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;
using webapp_f26_exam.ViewModels;

namespace SubTask1.Tests.Controllers;

public class RoomsControllerTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly RoomsController _controller;

    public RoomsControllerTests()
    {
        var rooms = new List<Room>
        {
            new() { Id = 1, Name = "Small room", Building = "P35", Capacity = 2, Amenities = new() { "Whiteboard" } },
            new() { Id = 2, Name = "Group room", Building = "P35", Capacity = 6, Amenities = new() { "Whiteboard", "Screen" } },
            new() { Id = 3, Name = "Big room", Building = "P46", Capacity = 10, Amenities = new() { "Screen" } }
        };
        _roomRepository.GetActiveRoomsAsync().Returns(rooms);
        _bookingRepository.GetAllBookingsAsync().Returns(new List<Booking>());

        _controller = new RoomsController(_roomRepository, _bookingRepository);
    }

    private static RoomsListViewModel GetModel(IActionResult result)
    {
        var viewResult = Assert.IsType<ViewResult>(result);
        return Assert.IsType<RoomsListViewModel>(viewResult.Model);
    }

    // Positive: searching with several filters should only return the room that matches all of them
    [Fact]
    public async Task Index_WhenFilteringOnBuildingCapacityAndAmenity_ReturnsOnlyMatchingRoom()
    {
        var result = await _controller.Index("P35", "3-6", new List<string> { "Screen" }, null, null);

        var model = GetModel(result);
        var room = Assert.Single(model.Rooms);
        Assert.Equal(2, room.Id);
    }

    // Negative: a search that matches no rooms should return an empty list
    [Fact]
    public async Task Index_WhenNoRoomMatchesFilters_ReturnsEmptyList()
    {
        var result = await _controller.Index("P46", "1-2", null, null, null);

        var model = GetModel(result);
        Assert.Empty(model.Rooms);
    }
}
