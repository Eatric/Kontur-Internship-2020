using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static System.Int32;

namespace KizhiPart2
{
	public sealed class Command
	{
		public string Value { get; }
		public int Position {get;}

		public Command(string cmd, int pos)
		{
			Value = cmd;
			Position = pos;
		}
	}
	public sealed class Function
	{
		public string Name { get; }
		private readonly List<Command> _commands;

		public Function(string name)
		{
			Name = name;
			_commands = new List<Command>();
		}

		public void AddCommand(string command, int pos)
		{
			_commands.Add(new Command(command, pos));
		}

		public List<Command> GetCommands()
		{
			return _commands;
		}
	}

	public sealed class CalledFunction
	{
		private int _currentLine;
		private Function Function { get; }

		public CalledFunction(Function calledFunction)
		{
			Function = calledFunction;
			_currentLine = 0;
		}

		public void Reset()
		{
			_currentLine = 0;
		}

		public Command GetCommand()
		{
			return (Function.GetCommands())[_currentLine++];
		}

		public bool IsEnd()
		{
			return _currentLine == Function.GetCommands().Count;
		}
	}

	public sealed class Variable
	{
		public string Name { get; }
		public int Value { get; set; }
		public int LastChanged { get; set; }

		public Variable(string name, int value)
		{
			Name = name;
			Value = value;
		}

		public void Print(TextWriter writer)
		{
			writer.WriteLine(Value);
		}
	}

	public class Interpreter
	{
		private readonly TextWriter _writer;
		private const string DEFAULT_FUNCTION = "DEFAULT";

		private readonly List<Variable> _variables;
		private readonly List<Function> _functions;
		private readonly Stack<CalledFunction> _calledFunctions;
		private Stack<CalledFunction> _functionsToRun;

		public delegate void FunctionCallDelegate();
		public event FunctionCallDelegate OnFunctionCall;

		private bool _isSetCodeEnd;
		private bool _shouldExecute;
		private int _position;

		public Interpreter(TextWriter writer)
		{
			_writer = writer;
			_variables = new List<Variable>();
			_functions = new List<Function>();
			_calledFunctions = new Stack<CalledFunction>();
			_functionsToRun = new Stack<CalledFunction>();

			_isSetCodeEnd = false;
			_shouldExecute = false;
			_position = 0;

			_functions.Add(new Function(DEFAULT_FUNCTION));
			_calledFunctions.Push(new CalledFunction(_functions.First()));
			OnFunctionCall += OnFunctionCalled;
		}

		private void OnFunctionCalled()
		{
			_functionsToRun = new Stack<CalledFunction>(_calledFunctions.Reverse());
		}

		private void EndCommand()
		{
			_isSetCodeEnd = true;
			_functionsToRun = new Stack<CalledFunction>(_calledFunctions.Reverse());
		}

		private void DefinitionCommand(string functionName)
		{
			_functions.Add(new Function(functionName));
		}

		private void SetCommand(string variableName, int value)
		{
			var variable = GetVariable(variableName) ?? new Variable(variableName, value)
			{
				LastChanged = _position
			};

			variable.Value = value;
			_variables.Add(variable);
		}

		private void SubCommand(string variableName, int value)
		{
			var variable = GetVariable(variableName);

			if (variable == null)
			{
				_writer.WriteLine("Переменная отсутствует в памяти");
				return;
			}

			variable.Value -= value;
			variable.LastChanged = _position;
		}

		private void PrintCommand(string variableName)
		{
			var variable = _variables.Find(x => x.Name == variableName);

			if (variable == null)
			{
				_writer.WriteLine("Переменная отсутствует в памяти");
				return;
			}

			variable.Print(_writer);
		}

		private void CallCommand(string functionName)
		{
			var function = GetFunction(functionName);

			if (function == null)
			{
				_writer.WriteLine("Функция отсутствует в памяти");
				return;
			}

			var calledFunction = new CalledFunction(function);
			_calledFunctions.Push(calledFunction);
			OnFunctionCall?.Invoke();
		}

		private void RemCommand(string variableName)
		{
			var variable = GetVariable(variableName);

			if (variable == null)
			{
				_writer.WriteLine("Переменная отсутствует в памяти");
				return;
			}

			_variables.Remove(variable);
		}

		private void Run()
		{
			while (_shouldExecute)
			{
				if (_functionsToRun.Count == 0) return;
				var currentFunction = _functionsToRun.Peek();
				if (!currentFunction.IsEnd())
				{
					var command = currentFunction.GetCommand();

					_position = command.Position;
					InterpreteCommand(command.Value);

					continue;
				}

				_functionsToRun.Pop();

				if (_functionsToRun.Count == 0)
				{
					_shouldExecute = false;
					Clear();
				}
			}
		}

		public void ExecuteLine(string command)
		{
			if (command == "set code") return;

			// 0 - название команды
			// 1 - имя переменной
			// 2 - аргумент
			var commands = command.Split('\n');
			foreach (var cmd in commands)
			{
				var trimmedCommand =
					SeparateCommand(cmd, out var commandWithArguments, out var isFunctionCommand);

				switch (commandWithArguments[0])
				{
					case "end":

						EndCommand();
						
						continue;
					case "def":

						if (commandWithArguments.Length != 2)
						{
							_writer.WriteLine("Ошибка! Команда def должна иметь следующий синтаксис: def [Function_Name]");
							continue;
						}

						DefinitionCommand(commandWithArguments[1]);
						_position++;
						
						continue;
					case "run":

						_shouldExecute = true;
						Run();

						continue;
				}

				if (!_isSetCodeEnd)
				{
					if (!isFunctionCommand)
					{
						_functions.First().AddCommand(trimmedCommand, _position); // Первая функция всегда главная
					}
					else
					{
						_functions.Last().AddCommand(trimmedCommand, _position); // Последняя функция всегда актуальная
					}

					_position++;
				}
				else
				{
					InterpreteCommand(cmd);
				}
			}
			_writer.Flush();
		}

		private void InterpreteCommand(string command)
		{
			SeparateCommand(command, out var commandWithArguments, out var isFunctionCommand);
			int nbr;

			switch (commandWithArguments[0])
			{
				case "set":
					if (commandWithArguments.Length != 3)
					{
						_writer.WriteLine("Ошибка! Команда set должна иметь следующий синтаксис: set [Variable_Name] [Value]");
						return;
					}

					TryParse(commandWithArguments[2], out nbr);
					SetCommand(commandWithArguments[1], nbr);
					break;
				case "sub":
					if (commandWithArguments.Length != 3)
					{
						_writer.WriteLine("Ошибка! Команда sub должна иметь следующий синтаксис: sub [Variable_Name] [Value]");
						return;
					}

					TryParse(commandWithArguments[2], out nbr);
					SubCommand(commandWithArguments[1], nbr);
					break;
				case "print":
					if (commandWithArguments.Length != 2)
					{
						_writer.WriteLine("Ошибка! Команда print должна иметь следующий синтаксис: print [Variable_Name]");
						return;
					}

					PrintCommand(commandWithArguments[1]);
					break;
				case "rem":
					if (commandWithArguments.Length != 2)
					{
						_writer.WriteLine("Ошибка! Команда rem должна иметь следующий синтаксис: rem [Variable_Name]");
						return;
					}

					RemCommand(commandWithArguments[1]);
					break;
				case "call":
					if (commandWithArguments.Length != 2)
					{
						_writer.WriteLine("Ошибка! Команда call должна иметь следующий синтаксис:  call [Function_Name]");
						return;
					}

					CallCommand(commandWithArguments[1]);
					break;
			}
			
		}

		private static string SeparateCommand(string command, out string[] commandWithArguments, out bool isFunctionCommand)
		{
			var trimmedCommand = command.TrimEnd();
			isFunctionCommand = trimmedCommand.StartsWith("    ");
			trimmedCommand = trimmedCommand.TrimStart();
			commandWithArguments = trimmedCommand.Split();
			return trimmedCommand;
		}

		private Function GetFunction(string functionName)
		{
			return _functions.FirstOrDefault(x => x.Name == functionName);
		}

		private Variable GetVariable(string variableName)
		{
			return GetVariables().FirstOrDefault(x => x.Name == variableName);
		}

		private List<Variable> GetVariables()
		{
			return _variables;
		}

		private void Clear()
		{
			foreach (var function in _calledFunctions)
			{
				function.Reset();
			}
			_functionsToRun = new Stack<CalledFunction>(_calledFunctions.Reverse());
			_variables.Clear();
			
			_position = 0;
		}
	}
}