using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.API.DTOs;

namespace UltimateTicTacToe.API.Hubs
{
    public class GameHub : Hub
    {
        public async Task MakeMove(string gameId, MoveCoordinate boardCoordinate, MoveCoordinate cellCoordinate, string currentPlayer)
        {
            // Make move logic
            Console.WriteLine($"Move has been made by {currentPlayer} in game {gameId}");
            Console.WriteLine($"BoardCoordinate: {boardCoordinate.X}:{boardCoordinate.Y} || CellCoordinate: {cellCoordinate.X}:{cellCoordinate.Y}");
            Console.WriteLine();

            // Validate and process the move here
            //bool isValidMove = ValidateMove(move);

            if (true) // isValidMove
            {
                // Broadcast the move to all connected clients
                await Clients.All.SendAsync("ReceiveMove", true);
            }
            else
            {
                // Optionally, send an error message or handle invalid move
            }

        }

        //private bool ValidateMove(Move move)
        //{
        //    return true;
        //}

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("ReceiveMessage", $"{Context.ConnectionId} has connected");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Handle disconnection
            return base.OnDisconnectedAsync(exception);
        }
    }
}
