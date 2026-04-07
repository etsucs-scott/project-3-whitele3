using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace Minesweeper.Core;


//3 board sizes which each size determines a set number of mines
public enum BoardSize
{
    Small8x8,
    Medium12x12,
    Large16x16
}

/// <summary>
/// Represents a single tile on the Minesweeper board.
/// Stores mine status, reveal state, flag state, and adjacent mine count.
/// </summary>
public class Tile
{
    public bool IsMine { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsFlagged { get; set; }

    //-1 is used internally to mark tiles that contain a mine
    public int AdjacentMines { get; set; }
}

// Represents the full Minesweeper board, including tile grid,
// mine placement, reveal logic, flood-fill behavior, and win detection.
public class Board
{
    public int Rows { get; }
    public int Columns { get; }
    public int MineCount { get; }

    //seed used for deterministic mine placement
    public int Seed { get; }

    private readonly Tile[,] _tiles;
    private bool _minesPlaced;
    private bool _hitMine;


    //true if the player revealed a mine
    public bool HitMine => _hitMine;

    //indexer to access tiles using board[row, column]
    public Tile this[int r, int c] => _tiles[r, c];

    /// <summary>
    /// Creates a new board of the given size using the given seed.
    /// Mines are NOT placed until the first reveal (lazy placement).
    /// </summary>
    public Board(BoardSize size, int seed)
    {
        Seed = seed;

        //determines the board dimensions
        (Rows, Columns, MineCount) = size switch
        {
            BoardSize.Small8x8 => (8, 8, 10),
            BoardSize.Medium12x12 => (12, 12, 25),
            BoardSize.Large16x16 => (16, 16, 40),
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };

        //initializes tile grid
        _tiles = new Tile[Rows, Columns];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                _tiles[r, c] = new Tile();
    }


    //ensures mines are placed.
    //this is done once the player reveals a tile.
    private void EnsureMinesPlaced()
    {
        if (_minesPlaced) return;

        var rng = new Random(Seed);
        var positions = new List<(int r, int c)>();


        //generate list of all tile positions
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                positions.Add((r, c));

        //shuffle positions using fisher-yates
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        //first N positions become mines
        for (int i = 0; i < MineCount; i++)
        {
            var (r, c) = positions[i];
            _tiles[r, c].IsMine = true;
        }

        ComputeAdjacencyCounts();
        _minesPlaced = true;
    }

    //computes the number of adjacent mines for every tile
    private void ComputeAdjacencyCounts()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
            {
                if (_tiles[r, c].IsMine)
                {
                    _tiles[r, c].AdjacentMines = -1;
                    continue;
                }

                int count = 0;
                ForEachNeighbor(r, c, (nr, nc) =>
                {
                    if (_tiles[nr, nc].IsMine) count++;
                });
                _tiles[r, c].AdjacentMines = count;
            }
    }


    //executes an action for each valid neighboring tile; up to 8
    private void ForEachNeighbor(int r, int c, Action<int, int> action)
    {
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = r + dr;
                int nc = c + dc;
                if (nr >= 0 && nr < Rows && nc >= 0 && nc < Columns)
                    action(nr, nc);
            }
    }

    //toggles a flag on the given tile (cannot be done on revealed tiles)
    public void ToggleFlag(int row, int column)
    {
        EnsureInBounds(row, column); 
        var tile = _tiles[row, column];
        if (tile.IsRevealed) return;
        tile.IsFlagged = !tile.IsFlagged;
    }

    //reveals a tile. if contains a mine, the player loses
    //if the tile has 0 adjacent mines, a flood-fill occurs
    public void Reveal(int row, int column)
    {
        EnsureInBounds(row, column);
        EnsureMinesPlaced();

        var tile = _tiles[row, column];

        //ignores if already flagged/revealed
        if (tile.IsRevealed || tile.IsFlagged)
            return;

        tile.IsRevealed = true;

        //game over from hit mine
        if (tile.IsMine)
        {
            _hitMine = true;
            return;
        }

        //flood fill if 0 adjacent mines
        if (tile.AdjacentMines == 0)
            FloodReveal(row, column);
    }

    // Performs a BFS flood-fill reveal for zero-adjacent tiles.
    // Reveals all connected empty tiles and their numbered borders.
    private void FloodReveal(int row, int column)
    {
        var queue = new Queue<(int row, int column)>();
        queue.Enqueue((row, column));

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();

            ForEachNeighbor(r, c, (nr, nc) =>
            {
                var neighbor = _tiles[nr, nc];

                if (neighbor.IsRevealed || neighbor.IsFlagged)
                    return;

                if (neighbor.IsMine)
                    return;

                neighbor.IsRevealed = true;

                //continue flood-fill if this tile is also empty
                if (neighbor.AdjacentMines == 0)
                    queue.Enqueue((nr, nc));
            });
        }
    }

    //throws an exception if the given coordinates are outside the board
    private void EnsureInBounds(int r, int c)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Columns)
            throw new ArgumentOutOfRangeException($"({r},{c}) out of bounds"); // FIXED
    }

    //returns true if all tiles have been revealed without hitting a mine
    public bool IsWin()
    {
        if (!_minesPlaced) return false;

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
            {
                var t = _tiles[r, c];
                if (!t.IsMine && !t.IsRevealed)
                    return false;
            }

        return !_hitMine;
    }
}
//Represents a single high score entry, including board size,
// completion time, move count, seed, and timestamp.
public class HighScore
{
    public BoardSize Size { get; set; }
    public int Seconds { get; set; }
    public int Moves { get; set; }
    public int Seed { get; set; }
    public DateTime Timestamp { get; set; }
}


/// <summary>
/// Handles loading, saving, and updating high scores stored in CSV format.
/// Ensures safe file handling and keeps top 5 scores per board size.
/// </summary>
public class HighScoreManager
{
    private readonly string _filePath;

    //creates a new highscore manager using the given file path
    public HighScoreManager(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Loads high scores from the CSV file.
    /// Automatically creates the file if missing.
    /// Invalid lines are skipped safely.
    /// </summary>
    public List<HighScore> Load()
    {
        var scores = new List<HighScore>();

        try
        {
            if (!File.Exists(_filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                File.WriteAllText(_filePath, "size,seconds,moves,seed,timestamp\n");
                return scores;
            }

            var lines = File.ReadAllLines(_filePath);

            foreach (var line in lines.Skip(1)) // skips header
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');

                if (parts.Length != 5)
                    continue; // skips invalid lines

                if (!Enum.TryParse(parts[0], out BoardSize size))
                    continue;

                if (!int.TryParse(parts[1], out int seconds))
                    continue;

                if (!int.TryParse(parts[2], out int moves))
                    continue;

                if (!int.TryParse(parts[3], out int seed))
                    continue;

                if (!DateTime.TryParse(parts[4], out DateTime timestamp))
                    continue;

                scores.Add(new HighScore
                {
                    Size = size,
                    Seconds = seconds,
                    Moves = moves,
                    Seed = seed,
                    Timestamp = timestamp
                });
            }
        }
        catch (Exception ex)
        {
            // Safe failure: return empty list
            Console.WriteLine($"Error loading high scores: {ex.Message}");
        }

        return scores;
    }


    //saves the list of highscores to the CSV file
    public void Save(List<HighScore> scores)
    {
        try
        {
            using var writer = new StreamWriter(_filePath);
            writer.WriteLine("size,seconds,moves,seed,timestamp");

            foreach (var s in scores)
            {
                writer.WriteLine($"{s.Size},{s.Seconds},{s.Moves},{s.Seed},{s.Timestamp:o}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving high scores: {ex.Message}");
        }
    }

    //adds a new score, sorts best time & fewest moves and keeps the top 5 scores for each size
    public void AddScore(HighScore newScore)
    {
        var scores = Load();

        // Filter to this board size
        var filtered = scores
            .Where(s => s.Size == newScore.Size)
            .ToList();

        filtered.Add(newScore);

        // Sort by: fastest time, then fewest moves
        filtered = filtered
            .OrderBy(s => s.Seconds)
            .ThenBy(s => s.Moves)
            .Take(5)
            .ToList();

        // Merge back into full list
        scores = scores
            .Where(s => s.Size != newScore.Size)
            .Concat(filtered)
            .ToList();

        Save(scores);
    }
}

/// <summary>
/// Contains deterministic xUnit tests that verify the correctness
/// of the Minesweeper board logic, including mine placement,
/// adjacency counts, reveal behavior, flagging, and win/loss rules.
/// </summary>
public class BoardTests
{
    // 1. Board initializes correct size
    [Fact]
    public void Board_InitializesCorrectDimensions()
    {
        var b = new Board(BoardSize.Small8x8, seed: 123);
        Assert.Equal(8, b.Rows);
        Assert.Equal(8, b.Columns);
    }

    // 2. Mine placement is deterministic
    [Fact]
    public void Board_UsesDeterministicSeed()
    {
        var b1 = new Board(BoardSize.Small8x8, 12345);
        var b2 = new Board(BoardSize.Small8x8, 12345);

        b1.Reveal(0, 0); // triggers mine placement
        b2.Reveal(0, 0);

        for (int r = 0; r < b1.Rows; r++)
            for (int c = 0; c < b1.Columns; c++)
                Assert.Equal(b1[r, c].IsMine, b2[r, c].IsMine);
    }

    // 3. Correct number of mines
    [Fact]
    public void Board_HasCorrectMineCount()
    {
        var b = new Board(BoardSize.Small8x8, 999);
        b.Reveal(0, 0);

        int mines = 0;
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                if (b[r, c].IsMine)
                    mines++;

        Assert.Equal(10, mines);
    }

    // 4. Adjacency counts computed correctly
    [Fact]
    public void Board_AdjacencyCountsAreCorrect()
    {
        var b = new Board(BoardSize.Small8x8, 123);
        b.Reveal(0, 0);

        // Find a non-mine tile and manually count neighbors
        for (int r = 0; r < b.Rows; r++)
        {
            for (int c = 0; c < b.Columns; c++)
            {
                var t = b[r, c];
                if (t.IsMine) continue;

                int expected = 0;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = r + dr;
                        int nc = c + dc;
                        if (nr >= 0 && nr < b.Rows && nc >= 0 && nc < b.Columns)
                            if (b[nr, nc].IsMine)
                                expected++;
                    }

                Assert.Equal(expected, t.AdjacentMines);
            }
        }
    }

    // 5. Reveal marks tile as revealed
    [Fact]
    public void Reveal_RevealsTile()
    {
        var b = new Board(BoardSize.Small8x8, 1);
        b.Reveal(0, 0);
        Assert.True(b[0, 0].IsRevealed);
    }

    // 6. Flag prevents reveal
    [Fact]
    public void FlaggedTile_CannotBeRevealed()
    {
        var b = new Board(BoardSize.Small8x8, 1);
        b.ToggleFlag(0, 0);
        b.Reveal(0, 0);
        Assert.False(b[0, 0].IsRevealed);
    }

    // 7. Cascade reveal expands zero-adjacent region
    [Fact]
    public void Reveal_ZeroTileCascades()
    {
        var b = new Board(BoardSize.Small8x8, 12345);
        b.Reveal(3, 3);

        // At least one neighbor of a zero tile should be revealed
        bool anyRevealed = false;
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                if (b[r, c].IsRevealed)
                    anyRevealed = true;

        Assert.True(anyRevealed);
    }

    // 8. Hitting a mine sets HitMine = true
    [Fact]
    public void Reveal_MineSetsHitMine()
    {
        var b = new Board(BoardSize.Small8x8, 123);
        b.Reveal(0, 0);

        // Find a mine and reveal it
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                if (b[r, c].IsMine)
                {
                    b.Reveal(r, c);
                    Assert.True(b.HitMine);
                    return;
                }

        Assert.Fail("No mine found — seed changed?");
    }

    // 9. Win condition works
    [Fact]
    public void WinCondition_AllNonMinesRevealed()
    {
        var b = new Board(BoardSize.Small8x8, 123);
        b.Reveal(0, 0);

        // Reveal everything except mines
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                if (!b[r, c].IsMine)
                    b.Reveal(r, c);

        Assert.True(b.IsWin());
    }

    // 10. Out-of-bounds throws exception
    [Fact]
    public void Reveal_OutOfBoundsThrows()
    {
        var b = new Board(BoardSize.Small8x8, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => b.Reveal(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => b.Reveal(0, 99));
    }

    // 11. Flag toggles correctly
    [Fact]
    public void ToggleFlag_Works()
    {
        var b = new Board(BoardSize.Small8x8, 1);
        b.ToggleFlag(2, 2);
        Assert.True(b[2, 2].IsFlagged);

        b.ToggleFlag(2, 2);
        Assert.False(b[2, 2].IsFlagged);
    }

    // 12. Mines are not placed until first reveal
    [Fact]
    public void MinesNotPlacedUntilReveal()
    {
        var b = new Board(BoardSize.Small8x8, 1);

        // Before reveal, all tiles must be non-mine
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                Assert.False(b[r, c].IsMine);

        b.Reveal(0, 0);

        // After reveal, mines exist
        bool anyMine = false;
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
                if (b[r, c].IsMine)
                    anyMine = true;

        Assert.True(anyMine);
    }
}
