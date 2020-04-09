using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommandLineCalculator
{
	public enum Action : byte
	{
		Read,
		Write
	}

	public enum CommandType : byte
	{
		NotFound,
		Help,
		Random,
		Add,
		Median,
	}

	[Serializable]
	public class HelpMessage
	{
		public Action Type { get; private set; }
		public string Message { get; private set;}

		public HelpMessage(Action type, string msg)
		{
			Type = type;
			Message = msg;
		}
	}

	[Serializable]
	public abstract class Command
	{
		[NonSerialized]
		public Storage Storage;

		[NonSerialized]
		public UserConsole Console;

		public CommandType Type;

		public long LastRandValue
		{
			get;
			protected set;
		}

		protected Command(UserConsole console, Storage storage, long x)
		{
			Console = console;
			Storage = storage;
			Schedule = new List<Action>();
			LastRandValue = x;
		}

		[NonSerialized]
		protected CultureInfo Culture = CultureInfo.InvariantCulture;

		public List<Action> Schedule;

		public virtual bool IsComplete => Schedule.Count == 0;

		public abstract void Run();

		public virtual MemoryStream Save()
		{
			var mem = new MemoryStream();
			var binWriter = new BinaryWriter(mem);

			binWriter.Write((byte) Type);
			binWriter.Write(LastRandValue);
			binWriter.Write(Schedule.Count);
			Schedule.ForEach(x => { binWriter.Write((byte) x); });

			return mem;
		}

		protected int ReadNumber()
		{
			return int.Parse(Console.ReadLine(), Culture);
		}
	}

	[Serializable]
	public sealed class HelpCommand : Command
	{
		[NonSerialized] 
		private const string ExitMessage = "Чтобы выйти из режима помощи введите end";

		[NonSerialized] 
		private const string Commands = "Доступные команды: add, median, rand";

		public List<HelpMessage> _helpMessages;

		public override bool IsComplete => _helpMessages.Count == 0;

		public HelpCommand(UserConsole console, Storage storage, long x) : base(console, storage, x)
		{
			Type = CommandType.Help;
			_helpMessages = new List<HelpMessage>()
			{
				new HelpMessage(Action.Write, "Укажите команду, для которой хотите посмотреть помощь"),
				new HelpMessage(Action.Write, Commands),
				new HelpMessage(Action.Write, ExitMessage)
			};
			Save();
		}

		private void Help()
		{
			while (!IsComplete)
			{
				switch (_helpMessages.First().Type)
				{
					case Action.Read:
					{
						var command = Console.ReadLine();
						switch (command.Trim())
						{
							case "end":
								break;
							case "add":
								_helpMessages.Add(new HelpMessage(Action.Write, "Вычисляет сумму двух чисел"));
								_helpMessages.Add(new HelpMessage(Action.Write, ExitMessage));
								break;
							case "median":
								_helpMessages.Add(new HelpMessage(Action.Write,
									"Вычисляет медиану списка чисел"));
								_helpMessages.Add(new HelpMessage(Action.Write, ExitMessage));
								break;
							case "rand":
								_helpMessages.Add(new HelpMessage(Action.Write,
									"Генерирует список случайных чисел"));
								_helpMessages.Add(new HelpMessage(Action.Write, ExitMessage));
								break;
							default:
								_helpMessages.Add(new HelpMessage(Action.Write, "Такой команды нет"));
								_helpMessages.Add(new HelpMessage(Action.Write, Commands));
								_helpMessages.Add(new HelpMessage(Action.Write, ExitMessage));
								break;
						}
						break;
					}
					case Action.Write:
					{
						Console.WriteLine(_helpMessages.First().Message);

						if (_helpMessages.First().Message == ExitMessage)
						{
							_helpMessages.Add(new HelpMessage(Action.Read, string.Empty));
						}

						break;
					}
				}

				_helpMessages.RemoveAt(0);
				Save();
			}
		}

		public override MemoryStream Save()
		{
			var mem = base.Save();
			var binWriter = new BinaryWriter(mem);

			binWriter.Write(_helpMessages.Count);
			_helpMessages.ForEach(x =>
			{
				binWriter.Write((byte) x.Type);
				binWriter.Write(x.Message);
			});
			Storage.Write(mem.ToArray());
			return mem;
		}

		public static HelpCommand Load(UserConsole console, Storage storage)
		{
			var mem = new MemoryStream(storage.Read());
			var schedule = new List<Action>();
			var helpMessages = new List<HelpMessage>();
			using var binReader = new BinaryReader(mem);

			binReader.ReadByte();
			var x = binReader.ReadInt64();
			var count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				schedule.Add((Action) binReader.ReadByte());
			}

			count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				var helpMsg = new HelpMessage((Action) binReader.ReadByte(), binReader.ReadString());
				helpMessages.Add(helpMsg);
			}

			var command = new HelpCommand(console, storage, x)
			{
				_helpMessages = helpMessages,
				Schedule = schedule
			};
			command.Save();
			return command;
		}

		public override void Run()
		{
			Help();
		}
	}

	[Serializable]
	public sealed class AddCommand : Command
	{
		private List<int> _variables;

		public AddCommand(UserConsole console, Storage storage, long x) : base(console, storage, x)
		{
			Type = CommandType.Add;
			_variables = new List<int>();
			Schedule.Add(Action.Read);
			Schedule.Add(Action.Read);
			Schedule.Add(Action.Write);

			Save();
		}
		
		private void Add()
		{
			while (!IsComplete)
			{
				switch (Schedule.First())
				{
					case Action.Read:
					{
						_variables.Add(ReadNumber());
						break;
					}
					case Action.Write:
					{
						Console.WriteLine(_variables.Sum().ToString(Culture));
						break;
					}
				}

				Schedule.RemoveAt(0); 
				Save();
			}
		}

		public override MemoryStream Save()
		{
			var mem = base.Save();
			var binWriter = new BinaryWriter(mem);

			binWriter.Write(_variables.Count);
			_variables.ForEach(x =>
			{
				binWriter.Write(x);
			});
			Storage.Write(mem.ToArray());
			return mem;
		}

		public static AddCommand Load(UserConsole console, Storage storage)
		{
			var mem = new MemoryStream(storage.Read());
			var schedule = new List<Action>();
			var variables = new List<int>();
			using var binReader = new BinaryReader(mem);

			binReader.ReadByte();
			var x = binReader.ReadInt64();
			var count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				schedule.Add((Action) binReader.ReadByte());
			}

			count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				variables.Add(binReader.ReadInt32());
			}

			var command = new AddCommand(console, storage, x)
			{
				_variables = variables,
				Schedule = schedule
			};

			command.Save();
			return command;
		}

		public override void Run()
		{
			Add();
		}
	}
	
	[Serializable]
	public sealed class NotFoundCommand : Command
	{
		public NotFoundCommand(UserConsole console, Storage storage, long x) : base(console, storage, x)
		{
			Schedule.Add(Action.Write);
			Type = CommandType.NotFound;

			Save();
		}
		
		private void NotFound()
		{
			while (!IsComplete)
			{
				if (Schedule.First() == Action.Write) Console.WriteLine("Такой команды нет, используйте help для списка команд");
				Schedule.RemoveAt(0);
				Save();
			}
		}

		public override MemoryStream Save()
		{
			var mem = base.Save();
			Storage.Write(mem.ToArray());
			return mem;
		}

		public static NotFoundCommand Load(UserConsole console, Storage storage)
		{
			var mem = new MemoryStream(storage.Read());
			var schedule = new List<Action>();
			var variables = new List<int>();
			using var binReader = new BinaryReader(mem);

			binReader.ReadByte();
			var x = binReader.ReadInt64();
			var count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				schedule.Add((Action) binReader.ReadByte());
			}

			var command = new NotFoundCommand(console, storage, x)
			{
				Schedule = schedule
			};

			command.Save();
			return command;
		}

		public override void Run()
		{
			NotFound();
		}
	}

	[Serializable]
	public sealed class RandomCommand : Command
	{
		[NonSerialized]
		private const int A = 16807;

		[NonSerialized]
		private const int M = int.MaxValue;
		
		public RandomCommand(UserConsole console, Storage storage, long x) : base(console, storage, x)
		{
			Schedule.Add(Action.Read);
			Type = CommandType.Random;

			Save();
		}
		
		private void Random()
		{
			while (!IsComplete)
			{
				switch (Schedule.First())
				{
					case Action.Read:
					{
						var count = ReadNumber();
						Schedule.AddRange(Enumerable.Repeat(Action.Write, count));
						break;
					}
					case Action.Write:
					{
						Console.WriteLine(LastRandValue.ToString(Culture));
						LastRandValue = A * LastRandValue % M;
						break;
					}
				}

				Schedule.RemoveAt(0);
				Save();
			}
		}

		public override MemoryStream Save()
		{
			var mem = base.Save();
			Storage.Write(mem.ToArray());
			return mem;
		}

		public static RandomCommand Load(UserConsole console, Storage storage)
		{
			var mem = new MemoryStream(storage.Read());
			var schedule = new List<Action>();
			using var binReader = new BinaryReader(mem);

			binReader.ReadByte();
			var x = binReader.ReadInt64();
			var count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				schedule.Add((Action) binReader.ReadByte());
			}

			var command = new RandomCommand(console, storage, x)
			{
				Schedule = schedule
			};

			command.Save();
			return command;
		}

		public override void Run()
		{
			Random();
		}
	}

	[Serializable]
	public sealed class MedianCommand : Command
	{
		private List<int> _variables;
		private bool _isFirstLaunch;

		public MedianCommand(UserConsole console, Storage storage, long x) : base(console, storage, x)
		{
			_variables = new List<int>();
			Type = CommandType.Median;
			Schedule.Add(Action.Read);
			_isFirstLaunch = true;

			Save();
		}

		private void Median()
		{
			while (!IsComplete)
			{
				switch (Schedule.First())
				{
					case Action.Read:
					{
						if (_isFirstLaunch)
						{
							var count = ReadNumber();
							for (var i = 0; i < count; i++)
							{
								Schedule.Add(Action.Read);
							}
							Schedule.Add(Action.Write);
							_isFirstLaunch = false;
						}
						else
						{
							_variables.Add(ReadNumber());
						}

						break;
					}
					case Action.Write:
					{
						Console.WriteLine(CalculateMedian().ToString(Culture));
						break;
					}
				}

				Schedule.RemoveAt(0);
				Save();
			}
		}

		private double CalculateMedian()
		{
			_variables.Sort();
			var count = _variables.Count;
			if (count == 0)
				return 0;

			if (count % 2 == 1)
				return _variables[count / 2];

			return (_variables[count / 2 - 1] + _variables[count / 2]) / 2.0;
		}

		public override MemoryStream Save()
		{
			var mem = base.Save();
			var binWriter = new BinaryWriter(mem);

			binWriter.Write(_variables.Count);
			_variables.ForEach(x =>
			{
				binWriter.Write(x);
			});
			binWriter.Write(_isFirstLaunch);
			Storage.Write(mem.ToArray());
			return mem;
		}

		public static MedianCommand Load(UserConsole console, Storage storage)
		{
			var mem = new MemoryStream(storage.Read());
			var schedule = new List<Action>();
			var variables = new List<int>();
			using var binReader = new BinaryReader(mem);

			binReader.ReadByte();
			var x = binReader.ReadInt64();
			var count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				schedule.Add((Action) binReader.ReadByte());
			}

			count = binReader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				variables.Add(binReader.ReadInt32());
			}

			var firstLaunch = binReader.ReadBoolean();

			var command = new MedianCommand(console, storage, x)
			{
				_variables = variables,
				_isFirstLaunch = firstLaunch,
				Schedule = schedule
			};

			command.Save();
			return command;
		}

		public override void Run()
		{
			Median();
		}
	}

	public sealed class StatefulInterpreter : Interpreter
	{
		private Command Load(UserConsole userConsole, Storage storage, CommandType type)
		{
			switch (type)
			{
				case CommandType.Help:
					return HelpCommand.Load(userConsole, storage);
				case CommandType.Random:
					return RandomCommand.Load(userConsole, storage);
				case CommandType.Add:
					return AddCommand.Load(userConsole, storage);
				case CommandType.Median:
					return MedianCommand.Load(userConsole, storage);
				case CommandType.NotFound:
					return NotFoundCommand.Load(userConsole, storage);
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		public override void Run(UserConsole userConsole, Storage storage)
		{
			var x = 420L;
			var storedData = storage.Read();
			if (storedData.Length != 0)
			{
				var binaryReader = new BinaryReader(new MemoryStream(storedData));
				var typeByte = binaryReader.ReadByte();
				var type = (CommandType) typeByte;
				var command = Load(userConsole, storage, type);

				if (!command.IsComplete)
				{
					command.Run();
				}
				x = command.LastRandValue;
			}
			while (true)
			{
				Command command;
				var input = userConsole.ReadLine();
				switch (input.Trim())
				{
					case "exit":
						storage.Write(Array.Empty<byte>());
						return;
					case "add":
						command = new AddCommand(userConsole, storage, x);
						break;
					case "median":
						command = new MedianCommand(userConsole, storage, x);
						break;
					case "help":
						command = new HelpCommand(userConsole, storage, x);
						break;	
					case "rand":
						command = new RandomCommand(userConsole, storage, x);
						break;
					default:
						command = new NotFoundCommand(userConsole, storage, x);
						break;
				}
				command.Run();
				x = command.LastRandValue;
			}
		}
	}
}