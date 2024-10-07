public class ConsoleOutput
{
  private List<string> _columns = new();
  private List<string[]> _rows = new();

  public ConsoleOutput(params string[] columns)
  {
    _columns = columns.ToList();
  }

  public void AddRow(params string[] values)
  {
    _rows.Add(values);
  }

  public void Write()
  {
    var columnWidths = _columns.Select(c => c.Length).ToList();

    foreach (var row in _rows.Where(e => e.Length > 1))
    {
      for (var i = 0; i < row.Length; i++)
      {
        columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
      }
    }

    var totalWidth = columnWidths.Sum() + columnWidths.Count * 3 + 1;

    Console.WriteLine(new string('-', totalWidth));

    Console.Write("|");

    for (var i = 0; i < _columns.Count(); i++)
    {
      Console.Write($" {_columns[i].PadRight(columnWidths[i])} |");
    }

    Console.WriteLine();

    Console.WriteLine(new string('-', totalWidth));

    foreach (var row in _rows.Where(e => e.Length > 1))
    {
      Console.Write("|");

      for (var i = 0; i < row.Length; i++)
      {
        Console.Write($" {row[i].PadRight(columnWidths[i])}");

        if (row.Length > 1)
        {
          Console.Write(" |");
        }
        else
        {
          Console.Write("|".PadLeft(totalWidth - row[i].Length - 2));
        }
      }

      Console.WriteLine();
    }

    Console.WriteLine(new string('-', totalWidth));

    foreach (var row in _rows.Where(e => e.Length == 1))
    {
      Console.Write("|");

      for (var i = 0; i < row.Length; i++)
      {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($" {row[i].PadRight(columnWidths[i])}");
        Console.ForegroundColor = ConsoleColor.White;

        if (row.Length > 1)
        {
          Console.Write(" |");
        }
        else
        {
          Console.Write("|".PadLeft(totalWidth - row[i].Length - 2));
        }
      }

      Console.WriteLine();
    }

    Console.WriteLine(new string('-', totalWidth));
  }
}
