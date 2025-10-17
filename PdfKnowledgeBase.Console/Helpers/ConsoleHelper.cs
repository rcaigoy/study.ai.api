namespace PdfKnowledgeBase.Console.Helpers;

/// <summary>
/// Helper class for console operations and user interface.
/// </summary>
public class ConsoleHelper
{
    private bool _showProgress = false;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Clears the console screen.
    /// </summary>
    public void ClearScreen()
    {
        System.Console.Clear();
    }

    /// <summary>
    /// Displays the application header.
    /// </summary>
    public void DisplayHeader()
    {
        WriteLine("=== PDF Knowledge Base Console Application ===", ConsoleColor.Cyan);
        WriteLine("Interactive PDF document analysis and querying tool", ConsoleColor.Gray);
        WriteLine();
    }

    /// <summary>
    /// Displays information about the current session.
    /// </summary>
    public void DisplayCurrentSession(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            WriteLine($"📄 Current Session: {sessionId}", ConsoleColor.Green);
            WriteLine();
        }
        else
        {
            WriteLine("📄 No PDF document loaded", ConsoleColor.Yellow);
            WriteLine();
        }
    }

    /// <summary>
    /// Displays the main menu and returns the user's choice.
    /// </summary>
    public string DisplayMainMenu()
    {
        WriteLine("Main Menu:", ConsoleColor.White);
        WriteLine("1. Load PDF Document", ConsoleColor.White);
        WriteLine("2. Query Knowledge Base", ConsoleColor.White);
        WriteLine("3. Generate Quiz Questions", ConsoleColor.White);
        WriteLine("4. Generate Flashcards", ConsoleColor.White);
        WriteLine("5. Query by Chapter", ConsoleColor.White);
        WriteLine("6. Summarize Chapter", ConsoleColor.White);
        WriteLine("7. Manage Session", ConsoleColor.White);
        WriteLine("8. Show Session Information", ConsoleColor.White);
        WriteLine("9. Help", ConsoleColor.White);
        WriteLine("A. Advanced ChatGPT Features", ConsoleColor.White);
        WriteLine("0. Exit", ConsoleColor.White);
        WriteLine();

        return GetStringInput("Select an option (0-9, A): ");
    }

    /// <summary>
    /// Displays a message with specified color.
    /// </summary>
    public void DisplayMessage(string message, ConsoleColor color = ConsoleColor.White)
    {
        WriteLine(message, color);
    }

    /// <summary>
    /// Displays an empty line.
    /// </summary>
    public void DisplayMessage()
    {
        WriteLine();
    }

    /// <summary>
    /// Displays an error message.
    /// </summary>
    public void DisplayError(string message)
    {
        WriteLine($"❌ Error: {message}", ConsoleColor.Red);
    }

    /// <summary>
    /// Displays a success message.
    /// </summary>
    public void DisplaySuccess(string message)
    {
        WriteLine($"✅ {message}", ConsoleColor.Green);
    }

    /// <summary>
    /// Displays a warning message.
    /// </summary>
    public void DisplayWarning(string message)
    {
        WriteLine($"⚠️ Warning: {message}", ConsoleColor.Yellow);
    }

    /// <summary>
    /// Gets string input from the user with enhanced error handling.
    /// </summary>
    public string GetStringInput(string prompt, string? defaultValue = null)
    {
        try
        {
            Write(prompt, ConsoleColor.Cyan);
            
            if (!string.IsNullOrEmpty(defaultValue))
            {
                Write($"[{defaultValue}] ", ConsoleColor.Gray);
            }

            var input = System.Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(defaultValue))
            {
                return defaultValue;
            }

            return input ?? string.Empty;
        }
        catch (Exception ex)
        {
            DisplayError($"Input error: {ex.Message}");
            return defaultValue ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets integer input from the user.
    /// </summary>
    public int GetIntegerInput(string prompt, int defaultValue)
    {
        while (true)
        {
            var input = GetStringInput(prompt, defaultValue.ToString());
            
            if (int.TryParse(input, out var result) && result > 0)
            {
                return result;
            }
            
            DisplayError("Please enter a valid positive integer.");
        }
    }

    /// <summary>
    /// Gets boolean input from the user (y/n).
    /// </summary>
    public bool GetBooleanInput(string prompt, bool defaultValue = false)
    {
        while (true)
        {
            var input = GetStringInput($"{prompt} (y/n)", defaultValue ? "y" : "n").ToLowerInvariant();
            
            if (input == "y" || input == "yes")
            {
                return true;
            }
            if (input == "n" || input == "no")
            {
                return false;
            }
            
            DisplayError("Please enter 'y' for yes or 'n' for no.");
        }
    }

    /// <summary>
    /// Waits for a key press from the user.
    /// </summary>
    public async Task WaitForKeyPressAsync(string message = "Press any key to continue...")
    {
        WriteLine(message, ConsoleColor.Gray);
        System.Console.ReadKey();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Shows a progress indicator.
    /// </summary>
    public void ShowProgressIndicator()
    {
        _showProgress = true;
        Task.Run(async () =>
        {
            var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var index = 0;
            
            while (_showProgress && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                System.Console.Write($"\r{spinner[index]} Processing...");
                index = (index + 1) % spinner.Length;
                await Task.Delay(100, _cancellationTokenSource.Token);
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Hides the progress indicator.
    /// </summary>
    public void HideProgressIndicator()
    {
        _showProgress = false;
        System.Console.Write("\r" + new string(' ', 50) + "\r"); // Clear the progress line
    }

    /// <summary>
    /// Displays a table of data.
    /// </summary>
    public void DisplayTable(string[] headers, string[][] rows)
    {
        if (headers.Length == 0 || rows.Length == 0)
        {
            DisplayMessage("No data to display.");
            return;
        }

        // Calculate column widths
        var columnWidths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            columnWidths[i] = Math.Max(headers[i].Length, rows.Max(row => i < row.Length ? row[i].Length : 0));
        }

        // Display header
        Write("┌", ConsoleColor.Gray);
        for (int i = 0; i < headers.Length; i++)
        {
            Write(new string('─', columnWidths[i] + 2), ConsoleColor.Gray);
            if (i < headers.Length - 1)
                Write("┬", ConsoleColor.Gray);
        }
        WriteLine("┐", ConsoleColor.Gray);

        Write("│", ConsoleColor.Gray);
        for (int i = 0; i < headers.Length; i++)
        {
            Write($" {headers[i].PadRight(columnWidths[i])} ", ConsoleColor.White);
            Write("│", ConsoleColor.Gray);
        }
        WriteLine();

        Write("├", ConsoleColor.Gray);
        for (int i = 0; i < headers.Length; i++)
        {
            Write(new string('─', columnWidths[i] + 2), ConsoleColor.Gray);
            if (i < headers.Length - 1)
                Write("┼", ConsoleColor.Gray);
        }
        WriteLine("┤", ConsoleColor.Gray);

        // Display rows
        foreach (var row in rows)
        {
            Write("│", ConsoleColor.Gray);
            for (int i = 0; i < headers.Length; i++)
            {
                var cellValue = i < row.Length ? row[i] : "";
                Write($" {cellValue.PadRight(columnWidths[i])} ", ConsoleColor.White);
                Write("│", ConsoleColor.Gray);
            }
            WriteLine();
        }

        Write("└", ConsoleColor.Gray);
        for (int i = 0; i < headers.Length; i++)
        {
            Write(new string('─', columnWidths[i] + 2), ConsoleColor.Gray);
            if (i < headers.Length - 1)
                Write("┴", ConsoleColor.Gray);
        }
        WriteLine("┘", ConsoleColor.Gray);
    }

    /// <summary>
    /// Displays a list of items with numbering.
    /// </summary>
    public void DisplayNumberedList<T>(IEnumerable<T> items, string title = "")
    {
        if (!string.IsNullOrEmpty(title))
        {
            DisplayMessage(title);
        }

        var itemList = items.ToList();
        for (int i = 0; i < itemList.Count; i++)
        {
            WriteLine($"{i + 1}. {itemList[i]}", ConsoleColor.White);
        }
    }

    /// <summary>
    /// Displays formatted text with word wrapping.
    /// </summary>
    public void DisplayWrappedText(string text, int maxWidth = 80, ConsoleColor color = ConsoleColor.White)
    {
        var words = text.Split(' ');
        var currentLine = "";
        
        foreach (var word in words)
        {
            if ((currentLine + word).Length <= maxWidth)
            {
                currentLine += (currentLine == "" ? "" : " ") + word;
            }
            else
            {
                if (currentLine != "")
                {
                    WriteLine(currentLine, color);
                }
                currentLine = word;
            }
        }
        
        if (currentLine != "")
        {
            WriteLine(currentLine, color);
        }
    }

    private void Write(string text, ConsoleColor color = ConsoleColor.White)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(text);
        System.Console.ForegroundColor = originalColor;
    }

    private void WriteLine(string text = "", ConsoleColor color = ConsoleColor.White)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(text);
        System.Console.ForegroundColor = originalColor;
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
