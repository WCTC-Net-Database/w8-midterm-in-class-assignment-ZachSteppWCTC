using System.ComponentModel.Design;
using System.Reflection.Emit;
using W8_assignment_template.Data;
using W8_assignment_template.Helpers;
using W8_assignment_template.Interfaces;
using W8_assignment_template.Models.Characters;

namespace W8_assignment_template.Services;

public class GameEngine
{
    private readonly IContext _context;
    private readonly MapManager _mapManager;
    private readonly MenuManager _menuManager;
    private readonly OutputManager _outputManager;

    private readonly IRoomFactory _roomFactory;
    private ICharacter _player;
    private ICharacter _goblin;
    private ICharacter _rat;
    private ICharacter _sludge;

    private List<IRoom> _rooms;

    public GameEngine(IContext context, IRoomFactory roomFactory, MenuManager menuManager, MapManager mapManager, OutputManager outputManager)
    {
        _roomFactory = roomFactory;
        _menuManager = menuManager;
        _mapManager = mapManager;
        _outputManager = outputManager;
        _context = context;
    }

    public void Run()
    {
        if (_menuManager.ShowMainMenu())
        {
            SetupGame();
        }
    }

    private void AttackCharacter()
    {
        // TODO Update this method to allow for attacking a selected monster in the room.
        // TODO e.g. "Which monster would you like to attack?"
        // TODO Right now it just attacks the first monster in the room.
        // TODO It is ok to leave this functionality if there is only one monster in the room.

        // Keeping this functionality for only one monster, allowing for the character to cancel their attack if decided.
        var targets = _player.CurrentRoom.Characters;
        if (targets != null && targets.Count != 0)
        {
            _outputManager.Clear();
            _outputManager.WriteLine("Select your target to attack.", ConsoleColor.Cyan);

            int targetselection = 0;
            while (true)
            {
                if (targets.Count == 0)
                {
                    // If there are no enemies left...
                    _outputManager.WriteLine($"{_player.CurrentRoom.Name} has been cleared of enemies.", ConsoleColor.Green);
                    break;
                }
                else
                {
                    _outputManager.WriteLine($"0. Cancel Attack");
                    for (int i = 1; i < targets.Count + 1; i++)
                    {
                        _outputManager.WriteLine($"{i}. {targets[i - 1].Name}");
                    }
                    _outputManager.Display();
                    // Prevents errors in user input, looking for an integer relating to the list of enemies.
                    try
                    {
                        targetselection = Convert.ToInt32(Console.ReadLine());
                        if (targetselection == 0)
                        {
                            _outputManager.Clear();
                            _outputManager.WriteLine($"{_player.Name} steps away from the fight.", ConsoleColor.Green);
                            // Reprints the characters still in the room after leaving a fight, using targets list. Could make a new method for this feature if it is used more. 
                            foreach (var target in targets)
                            {
                                _outputManager.WriteLine($"{target.Name} is here.", ConsoleColor.Red);
                            }
                            break;
                        }
                        // Checks if input is outside of range
                        else if (targetselection > targets.Count)
                        {
                            _outputManager.Clear();
                            _outputManager.WriteLine("Select your target to attack. Please enter an integer for one of the characters present.", ConsoleColor.Cyan);
                        }
                        // Attacks present target if user input is correct
                        else
                        {
                            _outputManager.Clear();
                            _player.Attack(targets[targetselection - 1]);
                            _player.CurrentRoom.RemoveCharacter(targets[targetselection - 1]);
                        }
                    }
                    catch (FormatException)
                    {
                        _outputManager.Clear();
                        _outputManager.WriteLine("Select your target to attack. Please enter an integer.", ConsoleColor.Cyan);
                    }
                }
            }
        }
        else
        {
            // Likely won't be accessed, but here for completion. Same as text in gameloop.
            _outputManager.Clear();
            _outputManager.WriteLine("No characters to attack.", ConsoleColor.Red);
        }
    }

    private void GameLoop()
    {
        while (true)
        {
            _mapManager.DisplayMap();
            _outputManager.WriteLine("Choose an action:", ConsoleColor.Cyan);
            _outputManager.WriteLine("1. Move North");
            _outputManager.WriteLine("2. Move South");
            _outputManager.WriteLine("3. Move East");
            _outputManager.WriteLine("4. Move West");

            // Check if there are characters in the current room to attack
            if (_player.CurrentRoom.Characters.Any(c => c != _player))
            {
                _outputManager.WriteLine("5. Attack");
            }

            _outputManager.WriteLine("6. Exit Game");

            _outputManager.Display();

            var input = Console.ReadLine();

            string? direction = null;
            switch (input)
            {
                case "1":
                    direction = "north";
                    break;
                case "2":
                    direction = "south";
                    break;
                case "3":
                    direction = "east";
                    break;
                case "4":
                    direction = "west";
                    break;
                case "5":
                    if (_player.CurrentRoom.Characters.Any(c => c != _player))
                    {
                        AttackCharacter();
                    }
                    else
                    {
                        _outputManager.Clear();
                        _outputManager.WriteLine("No characters to attack.", ConsoleColor.Red);
                    }

                    break;
                case "6":
                    _outputManager.WriteLine("Exiting game...", ConsoleColor.Red);
                    _outputManager.Display();
                    Environment.Exit(0);
                    break;
                default:
                    _outputManager.WriteLine("Invalid selection. Please choose a valid option.", ConsoleColor.Red);
                    break;
            }

            // Update map manager with the current room after movement
            if (!string.IsNullOrEmpty(direction))
            {
                _outputManager.Clear();
                _player.Move(direction);
                _mapManager.UpdateCurrentRoom(_player.CurrentRoom);
            }
        }
    }

    private void LoadMonsters()
    {
        _goblin = _context.Characters.OfType<Goblin>().FirstOrDefault();
        _rat = _context.Characters.OfType<Rat>().FirstOrDefault();
        _sludge = _context.Characters.OfType<Sludge>().FirstOrDefault();

        var random = new Random();
        var randomRoom = _rooms[random.Next(_rooms.Count)];
        randomRoom.AddCharacter(_goblin); // Use helper method
        _rooms[0].North.North.AddCharacter(_rat);
        _rooms[0].North.North.AddCharacter(_sludge);

        // TODO Load your two new monsters here into the same room
    }

    private void SetupGame()
    {
        var startingRoom = SetupRooms();
        _mapManager.UpdateCurrentRoom(startingRoom);

        _player = _context.Characters.OfType<Player>().FirstOrDefault();
        _player.Move(startingRoom);
        _outputManager.WriteLine($"{_player.Name} has entered the game.", ConsoleColor.Green);

        // Load monsters into random rooms 
        LoadMonsters();

        // Pause for a second before starting the game loop
        Thread.Sleep(1000);
        GameLoop();
    }

    private IRoom SetupRooms()
    {
        // TODO Update this method to create more rooms and connect them together

        var entrance = _roomFactory.CreateRoom("entrance", _outputManager);
        var treasureRoom = _roomFactory.CreateRoom("treasure", _outputManager);
        var dungeonRoom = _roomFactory.CreateRoom("dungeon", _outputManager);
        var library = _roomFactory.CreateRoom("library", _outputManager);
        var armory = _roomFactory.CreateRoom("armory", _outputManager);
        var garden = _roomFactory.CreateRoom("garden", _outputManager);
        var storage = _roomFactory.CreateRoom("storage", _outputManager);
        var sewer = _roomFactory.CreateRoom("sewer", _outputManager);

        entrance.North = treasureRoom;
        entrance.West = library;
        entrance.East = garden;

        treasureRoom.South = entrance;
        treasureRoom.West = dungeonRoom;
        treasureRoom.North = sewer;
        treasureRoom.East = storage;

        dungeonRoom.East = treasureRoom;
        
        sewer.South = treasureRoom;

        storage.West = treasureRoom;
        storage.South = garden;

        library.East = entrance;
        library.South = armory;

        armory.North = library;

        garden.West = entrance;
        garden.North = storage;

        // Store rooms in a list for later use
        _rooms = new List<IRoom> { entrance, treasureRoom, dungeonRoom, library, armory, garden };

        return entrance;
    }
}
