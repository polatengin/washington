using System.Text;

public class ConsoleOutput
{
  private List<string> _columns = [];
  private List<string[]> _rows = [];

  public ConsoleOutput(params string[] columns)
  {
    _columns = columns.ToList();
  }

  public void PrintLogo()
  {
    Console.ForegroundColor = ConsoleColor.DarkBlue;
    Console.WriteLine(@"
                                 _____          _     ______     _   _                 _
    /\                          / ____|        | |   |  ____|   | | (_)               | |
   /  \    _____   _ _ __ ___  | |     ___  ___| |_  | |__   ___| |_ _ _ __ ___   __ _| |_ ___  _ __
  / /\ \  |_  / | | | '__/ _ \ | |    / _ \/ __| __| |  __| / __| __| | '_ ` _ \ / _` | __/ _ \| '__|
 / ____ \  / /| |_| | | |  __/ | |___| (_) \__ \ |_  | |____\__ \ |_| | | | | | | (_| | || (_) | |
/_/    \_\/___|\__,_|_|  \___|  \_____\___/|___/\__| |______|___/\__|_|_| |_| |_|\__,_|\__\___/|_|
  ");
    Console.ForegroundColor = ConsoleColor.White;
  }

  public void AddRow(params string[] values)
  {
    _rows.Add(values);
  }

  private double _grandTotal = -1;
  public void AddGrandTotalRow(double total)
  {
    _grandTotal = total;
  }

  private string _rowSeperator = string.Empty;

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

    var longestContent = _rows.Where(e => e.Length == 1).Select(e => e[0].Length).Max() + 4;

    var columnWidth = columnWidths.Sum() + columnWidths.Count * 3 + 1;

    var totalWidth = Math.Max(columnWidth, longestContent);

    this._rowSeperator = new string('-', totalWidth);

    Console.WriteLine(this._rowSeperator);

    Console.Write("|");

    for (var i = 0; i < _columns.Count(); i++)
    {
      if (i == 0)
      {
        if (longestContent > columnWidth)
        {
          Console.Write($" {_columns[i].PadRight(columnWidths[i] + (longestContent - columnWidth))} |");
        }
        else
        {
          Console.Write($" {_columns[i].PadRight(columnWidths[i])} |");
        }
      }
      else
      {
        Console.Write($" {_columns[i].PadRight(columnWidths[i])} |");
      }
    }

    Console.WriteLine();

    Console.WriteLine(this._rowSeperator);

    foreach (var row in _rows.Where(e => e.Length > 1))
    {
      Console.Write("|");

      for (var i = 0; i < row.Length; i++)
      {
        if (i == 0)
        {
          if (longestContent > columnWidth)
          {
            Console.Write($" {row[i].PadRight(columnWidths[i] + (longestContent - columnWidth))}");
          }
          else
          {
            Console.Write($" {row[i].PadRight(columnWidths[i])}");
          }
        }
        else if (i == row.Length - 1)
        {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.Write($" {row[i].PadLeft(columnWidths[i])}");
          Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
          Console.Write($" {row[i].PadRight(columnWidths[i])}");
        }

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

    Console.WriteLine(this._rowSeperator);

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

    Console.WriteLine(this._rowSeperator);

    if (_grandTotal > -1)
    {
      Console.Write("|");

      Console.ForegroundColor = ConsoleColor.Yellow;
      var grandTotalText = _grandTotal.ToString("C2");
      Console.Write(" Grand Total: ".PadRight(totalWidth - grandTotalText.Length - 3));
      Console.Write($"{grandTotalText} ");
      Console.ForegroundColor = ConsoleColor.White;

      Console.WriteLine("|");

      Console.WriteLine(this._rowSeperator);
    }
  }

  public override string ToString()
  {
    var sb = new StringBuilder();

    sb.Append("|");

    for (var i = 0; i < _columns.Count(); i++)
    {
      sb.Append($" {_columns[i].PadRight(_columns[i].Length)} |");
    }

    sb.AppendLine();

    sb.Append("|");

    for (var i = 0; i < _columns.Count(); i++)
    {
      sb.Append(" --- |");
    }

    sb.AppendLine();

    foreach (var row in _rows.Where(e => e.Length > 1))
    {
      sb.Append("|");

      for (var i = 0; i < row.Length; i++)
      {
        sb.Append($" {row[i].PadRight(row[i].Length)} |");
      }

      sb.AppendLine();
    }

    if (_grandTotal > -1)
    {
      sb.Append("| | | | |");

      sb.Append($" Grand Total | {_grandTotal.ToString("C2")} |");

      sb.AppendLine();
    }

    return sb.ToString();
  }
}
