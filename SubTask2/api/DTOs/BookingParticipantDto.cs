namespace api.DTOs;

// Sent to the frontend. Joining and leaving only need the booking id from the URL,
// so the client never sends this DTO in a request body.
public class BookingParticipantDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string? UserName { get; set; }
    public DateTime JoinedAtDateTime { get; set; }
}