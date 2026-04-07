using System;
using Minesweeper.Core;

namespace Minesweeper.ConsoleUI;

class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Menu:");
            Console.WriteLine("1) 8x8");
            Console.WriteLine("2) 12x12");
            Console.WriteLine("3) 16x16");
            Console.Write("Choose board size: ");

            BoardSize size = Console.ReadLine() switch
            {
                "1" => BoardSize.Small8x8,
                "2" => BoardSize.Medium12x12,
                "3" => BoardSize.Large16x16,
                _ => BoardSize.Small8x8
            };

            Console.Write("Seed (blank = time): ");
            string seedInput = Console.ReadLine()!;
            int seed = string.IsNullOrWhiteSpace(seedInput)
                ? Environment.TickCount
                : int.Parse(seedInput);

            Console.WriteLine($"Using seed: {seed}");
            var board = new Board(size, seed);

            PlayGame(board);
        }
    }

    static void PlayGame(Board board)
    {
        int moves = 0;
        var startTime = DateTime.Now;

        while (true)
        {
            Console.Clear();
            PrintBoard(board);

            if (board.HitMine)
            {
                Console.WriteLine("💥 You hit a mine! Game over.");
                Console.WriteLine("Press ENTER to return to menu.");
                Console.ReadLine();
                return;
            }

            if (board.IsWin())
            {
                var time = (int)(DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"You win! Time: {time}s, Moves: {moves}");
                Console.WriteLine("Press ENTER to return to menu.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("\nCommands: r row col | f row col | q");
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().ToLower() == "q")
                return;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                Console.WriteLine("Invalid command. Press ENTER.");
                Console.ReadLine();
                continue;
            }

            string cmd = parts[0];
            if (!int.TryParse(parts[1], out int row) ||
                !int.TryParse(parts[2], out int col))
            {
                Console.WriteLine("Invalid coordinates. Press ENTER.");
                Console.ReadLine();
                continue;
            }

            try
            {
                if (cmd == "r")
                    board.Reveal(row, col);
                else if (cmd == "f")
                    board.ToggleFlag(row, col);
                else
                    Console.WriteLine("Unknown command.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press ENTER.");
                Console.ReadLine();
            }

            moves++;
        }
    }

    static void PrintBoard(Board board)
    {
        Console.Write("   ");
        for (int c = 0; c < board.Columns; c++)
            Console.Write($"{c} ");
        Console.WriteLine();

        for (int r = 0; r < board.Rows; r++)
        {
            Console.Write($"{r,2} ");
            for (int c = 0; c < board.Columns; c++)
            {
                var t = board[r, c];

                if (!t.IsRevealed)
                {
                    Console.Write(t.IsFlagged ? "f " : "# ");
                }
                else if (t.IsMine)
                {
                    Console.Write("b ");
                }
                else if (t.AdjacentMines == 0)
                {
                    Console.Write(". ");
                }
                else
                {
                    Console.Write($"{t.AdjacentMines} ");
                }
            }
            Console.WriteLine();
        }
    }
}
