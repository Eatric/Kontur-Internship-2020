using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static System.Int32;

namespace KizhiPart1
{
	public sealed class Variable
	{
		public string Name { get; }
		public int Value { get; set; }

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

		private readonly List<Variable> _variables;

		public Interpreter(TextWriter writer)
		{
			_writer = writer;
			_variables = new List<Variable>();

		}

		private void SetCommand(string variableName, int value)
		{
			var variable = GetVariable(variableName) ?? new Variable(variableName, value);

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

		public void ExecuteLine(string command)
		{
			// 0 - название команды
			// 1 - имя переменной
			// 2 - аргумент
			var commands = command.Split('\n');
			foreach (var cmd in commands)
			{
				InterpreteCommand(cmd);
			}
			_writer.Flush();
		}

		private void InterpreteCommand(string command)
		{
			SeparateCommand(command, out var commandWithArguments);
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
			}
			
		}

		private static string SeparateCommand(string command, out string[] commandWithArguments)
		{
			var trimmedCommand = command.Trim();
			commandWithArguments = trimmedCommand.Split();
			return trimmedCommand;
		}

		private Variable GetVariable(string variableName)
		{
			return GetVariables().FirstOrDefault(x => x.Name == variableName);
		}

		public List<Variable> GetVariables()
		{
			return _variables;
		}
	}
}