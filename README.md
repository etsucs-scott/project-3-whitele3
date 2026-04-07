[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/ozVFrFMv)
# CSCI 1260 — Project

## Project Instructions
All project requirements, grading criteria, and submission details are provided on **D2L**.  
Refer to D2L as the *authoritative source* for this assignment.

This repository is intentionally minimal. You are responsible for:
- Creating the solution and projects
- Designing the class structure
- Implementing the required functionality

---

## Getting Started (CLI)

You may use **Visual Studio**, **VS Code**, or the **terminal**.

### Create a solution
```bash
dotnet new sln -n ProjectName
```

### Create a project (example: console app)
```bash
dotnet new console -n ProjectName.App
```

### Add the project to the solution
```bash
dotnet sln add ProjectName.App
```

### Build and run
```bash
dotnet build
dotnet run --project ProjectName.App
```

## Notes
- Commit early and commit often.
- Your repository history is part of your submission.
- Update this README with build/run instructions specific to your project.



## EDIT

## How to Build and Run the Game

Running from Visual Studio
Open the solution.
Right click Minesweeper.ConsoleUI 
 Set as Startup Project.
Press F5 or Run.

Running from Terminal
Navigate to the ConsoleUI project:
Code
cd Minesweeper.ConsoleUI
dotnet run

## Gameplay Overview
When the game starts, you will see a menu:

1) 8x8
2) 12x12
3) 16x16
Seed (blank = time):
Board Sizes
Option	Size	Mines
1	    8x8	    10
2	    12x12	25
3	    16x16	40


Seed Usage
The game asks for a seed:

Enter an integer which makes board generate deterministically

Leave blank = seed is generated from system time

The seed is displayed before the game begins:
Using seed: 12345
This can create:

Replayable boards
Verifiable high scores
Deterministic unit tests

Input Commands (0 Indexed Coordinates)
All coordinates use 0 based indexing:

Code
r row col    reveal tile
f row col    flag/unflag tile
q            quit to menu

Examples:
r 2 3
f 1 1
q

## Board Symbols
Symbol	Meaning
#	Hidden tile
f	Flagged tile
b	Bomb (only shown when you lose)
.	Revealed tile with 0 adjacent mines
1-8	Number of adjacent mines


## High Scores
High scores are stored in:

data/highscores.csv
The file is created automatically if missing.

## CSV Format
Header:

size,seconds,moves,seed,timestamp
Each entry contains:

size = Small8x8, Medium12x12, Large16x16

seconds to completion time

moves = number of actions taken

seed = board seed used

timestamp = ISO-8601 format


## High Score Rules

Fastest time wins
Tie-breaker: fewer moves

Only top 5 scores per board size are kept

## Unit Tests (xUnit)
Unit tests validate:

Board generation
Deterministic mine placement
Adjacency counts
Cascade reveal behavior
Flagging rules
Win/loss detection
Bounds checking
Lazy mine placement (mines placed on first reveal)

Running Tests
From the solution root:

dotnet test
Or in Visual Studio:

Code
Test 
Run All Tests


## Project Structure

Minesweeper/

   Minesweeper.Core
        Board.cs
        Tile.cs
        HighScore.cs
        HighScoreManager.cs
        BoardTests.cs

   Minesweeper.ConsoleUI
        Program.cs


Notes
All game logic is isolated in Minesweeper.Core (required) with tests added to ensure working correctly.
ConsoleUI handles only input and rendering.
