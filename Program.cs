
// Сначала загружаем все реквизиты из файлов
var requisites = new List<Requisite>();

Console.OutputEncoding = System.Text.Encoding.UTF8;

var files = Directory.GetFiles(@"..\..\..\Metadata", "*.txt");

foreach (var file in files)
{ 
	requisites.Add(new Requisite(file));
}


// Теперь можно сформировать документ
var document = new Document();

document.Input(requisites);

document.Print();

Console.ReadLine();




public class Requisite
{
	private string _name = string.Empty;
	private string _description = string.Empty;
	private SortedDictionary<string, string> _pickList = new SortedDictionary<string, string>();
	private SortedDictionary<string, string> _prerequisites = new SortedDictionary<string, string>();
	private SortedDictionary<string, string> _prerequisitesOneOf = new SortedDictionary<string, string>();
	private string _value = string.Empty;
	private string _displayedValue = string.Empty;

	public string Name { get { return _name; } }

	public bool LowPriority { get; set; } = false;

	private enum FilePart
	{ 
		Description, Cond, Pick, Unknown
	}

	public Requisite(string filename)
	{
		if (!File.Exists(filename))
		{
			return;
		}

		_name = Path.GetFileNameWithoutExtension(filename);

		// Console.WriteLine("Загрузка {0}", _name);

		var lines = File.ReadAllLines(filename);

		var part = FilePart.Description;

		Func<string, string, Requisite>? f = Prereq;

		foreach (var line in lines)
		{
			var ln = line.Trim();

			if (ln.Length == 0)
			{
				continue;
			}

			var data = ln.Contains(':') ? ln.Substring(ln.IndexOf(':') + 1) : ln;
			string[] items;

			if (part == FilePart.Description)
			{
				_description = ln;
				part = FilePart.Unknown;
			}
			else if (ln.StartsWith("Условия:"))
			{
				part = FilePart.Cond;
				f = Prereq;
			}
			else if (ln.StartsWith("Спрашивать позже"))
			{
				LowPriority = true;
			}
			else if (ln.StartsWith("Одно из условий:"))
			{
				part = FilePart.Cond;
				f = PrereqOneOf;
			}
			else if (ln.StartsWith("Варианты:"))
			{
				part = FilePart.Pick;
			}
			else
			{
				if (part == FilePart.Cond)
				{
					items = data.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					for (var i = 0; i < items.Length; i++)
					{
						var item = items[i];

						var lc = item.ToLowerInvariant();

						if (lc == "заполнено")
						{
							i++;
							if (i >= items.Length)
							{
								break;
							}
							item = items[i];
							if (f != null)
							{
								_ = f(item, "*");
							}
						}
						else
						{
							var name = item;
							i++;
							if (i >= items.Length)
							{
								break;
							}
							item = items[i];
							var value = item;
							if (f != null)
							{
								f(name, value);
							}
						}
					}
				}
				else if (part == FilePart.Pick)
				{
					items = data.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

					if (items.Length == 2)
					{
						Pick(items[1], items[0]);
					}
				}
			}
		}
	}

	public Requisite(string name, string description)
	{
		_name = name;
		_description = description;
	}

	public Requisite Pick(string name, string value)
	{
		_pickList.Add(name, value);
		return this;
	}

	public Requisite Prereq(string name, string values)
	{
		// Console.WriteLine("prereq: {0} = {1}", name, values);
		_prerequisites.Add(name, values);
		return this;
	}

	public Requisite PrereqOneOf(string name, string values)
	{
		// Console.WriteLine("prereq1: {0} = {1}", name, values);
		_prerequisitesOneOf.Add(name, values);
		return this;
	}

	public bool IsRoot
	{
		get { return _prerequisites.Count == 0 && _prerequisitesOneOf.Count == 0; }
	}

	private bool PrerequisitesMet(List<Requisite> allreq, SortedDictionary<string, string> prerequisites, bool oneof)
	{
		var result = true;
		
		foreach (var req in prerequisites)
		{
			var names = req.Key.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			var values = req.Value.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			foreach (var name in names)
			{
				var item = allreq.Find(x => req.Key.Contains(x._name));

				if (item == null)
				{
					result = false;
					break;
				}

				if (item._value.Length == 0)
				{
					result = false;
					break;
				}

				if (req.Value == "*")
				{
					continue;
				}

				if (!values.Contains(item._value))
				{
					result = false;
					break;
				}

				if (oneof)
				{
					result = true;
					break;
				}
			}
			if (oneof && result)
			{
				break;
			}
		}

		return result;
	}

	public bool PrerequisitesMet(List<Requisite> allreq)
	{
		return
			PrerequisitesMet(allreq, _prerequisites, false) &&
			PrerequisitesMet(allreq, _prerequisitesOneOf, true);
	}

	public bool Input()
	{
		string value;

		while (true)
		{
			Console.WriteLine(_description + " ?");

			if (_pickList.Count > 1)
			{
				// Это поле со списком
				for (var i = 0; i < _pickList.Count; i++)
				{
					Console.WriteLine("{0}. {1}", i + 1, _pickList.ElementAt(i).Key);
				}

				value = Console.ReadKey().KeyChar.ToString();

				int index = 0;
				int.TryParse(value, out index);
				index--;
				if (index >= 0 && index < _pickList.Count)
				{
					_value = _pickList.Values.ElementAt(index);
					_displayedValue = _pickList.Keys.ElementAt(index);
					Console.WriteLine(" - {0}", _displayedValue);
					break;
				}
				else
				{
					Console.WriteLine(" <- неправильно. Введите от 1 до {0}", _pickList.Count);
				}
			}
			else
			{
				value = Console.ReadLine() ?? "";
				value = value.Trim();

				_value = value;
				_displayedValue = value;
				break;
			}
		}

		return true;
	}

	public void Print()
	{ 
		Console.WriteLine("{0}: {1}", _description, _displayedValue);
	}
}

public class Document
{
	public List<Requisite> Requisites { get; set; } = new List<Requisite>();

	public void Input(List<Requisite> allreq)
	{
		// Сначала корневой элемент
		foreach (var req in allreq.Where(x => x.IsRoot))
		{
			req.Input();
			Requisites.Add(req);
			Console.WriteLine();
		}

		// Потом детали, чтобы не зациклить программу, если есть ошибка
		// Высокий приоритет
		for (;;)
		{
			var found = false;
			foreach (var req in allreq.Where(x => !x.IsRoot && !x.LowPriority))
			{
				if (Requisites.Exists(x => x.Name == req.Name))
				{
					continue;
				}
				if (req.PrerequisitesMet(Requisites))
				{
					req.Input();
					Requisites.Add(req);
					Console.WriteLine();
					found = true;
				}
			}
			if (found)
			{
				continue;
			}

			// Если не найдено, то низкий приоритет
			foreach (var req in allreq.Where(x => !x.IsRoot && x.LowPriority))
			{
				if (Requisites.Exists(x => x.Name == req.Name))
				{
					continue;
				}
				if (req.PrerequisitesMet(Requisites))
				{
					req.Input();
					Requisites.Add(req);
					Console.WriteLine();
					found = true;
				}
			}
			if (!found)
			{
				break;
			}
		}
	}

	public void Print()
	{
		foreach (var req in Requisites)
		{
			req.Print();
		}
	}
}
