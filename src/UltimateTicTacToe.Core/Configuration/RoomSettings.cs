namespace UltimateTicTacToe.Core.Configuration;

public class RoomSettings
{
    public int RoomTtlMinutes { get; set; } = 5;

    public int MaxRegularRooms { get; set; } = 75;

    public int MaxPrivateRooms { get; set; } = 50;
}

