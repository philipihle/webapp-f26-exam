# SubTask1.Tests

Unit tests for the MVC application in `SubTask1`.

## Tools

- **xUnit** – test framework (`[Fact]` marks a test)
- **NSubstitute** – mocking library, as recommended in the lecture (Demo: Unit Testing)

The repositories (`IRoomRepository`, `IBookingRepository`) are replaced with NSubstitute substitutes. This means the controllers are tested in isolation, without a real database.

## Structure

```
SubTask1.Tests/
└── Controllers/
    ├── BookingControllerTests.cs
    └── RoomsControllerTests.cs
```

Each test follows the **Arrange – Act – Assert** pattern and is named `MethodUnderTest_Scenario_ExpectedResult`.

## Tests

| Controller | Test | Type | What it checks |
|---|---|---|---|
| BookingController | `Create_WhenBookingIsValid_RedirectsToBookingConfirmed` | Positive | Booking a free room saves the booking and shows the confirmation page |
| BookingController | `Create_WhenTimeSlotIsAlreadyBooked_ReturnsViewWithError` | Negative | Booking a room that is already taken at that time is rejected |
| RoomsController | `Index_WhenFilteringOnBuildingCapacityAndAmenity_ReturnsOnlyMatchingRoom` | Positive | Searching with several filters returns only the matching room |
| RoomsController | `Index_WhenNoRoomMatchesFilters_ReturnsEmptyList` | Negative | A search that matches no rooms returns an empty list |

## How to run

Requires the .NET 10 SDK. From the repository root:

```bash
dotnet test SubTask1.Tests
```

Expected output:

```
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```

In VS Code you can also run and debug the tests from the Test Explorer (the flask icon), which requires the C# Dev Kit extension.
