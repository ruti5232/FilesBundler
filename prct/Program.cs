using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

var bundleOption = new Option<FileInfo>("--output", "File path and name");
bundleOption.AddAlias("-o");
var noteOption = new Option<bool>("--note", "if to write the source of the file");
noteOption.AddAlias("-n");
var sortOption = new Option<string>(
    "--sort",
    "Sort files by either 'alphabetical' or 'extension'.")
    .FromAmong("alphabetical", "extension");
sortOption.AddAlias("-s");
sortOption.SetDefaultValue("alphabetical");
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "if to remove the empty lines from the source file and it wont appear in the bundle file");
removeEmptyLinesOption.AddAlias("-rel");
var authorOption = new Option<string>("--author", "name of the author for the bundle file");
authorOption.AddAlias("-a");
var allowedLanguages = new List<string> { "csharp", "fsharp", "vb", "pwsh", "sql", "all" };
var extensionLanguagesList = new List<string> { "cs", "fs", "vb", "pwsh", "sql", "all" };
var languageOption = new Option<string>(
    "--lang",
    "An option that must be one of the values of a static list.")
    .FromAmong(allowedLanguages.ToArray());
languageOption.AddAlias("-l");
languageOption.AllowMultipleArgumentsPerToken = true;
languageOption.IsRequired = true;

var bundleCommand = new Command("bundle", "bundle cod files to a single file");
var createRspCommand = new Command("create-rsp", "Create a response file");

var outputOption = new Option<FileInfo>("--output", "Path to save the response file");
createRspCommand.AddOption(outputOption);

bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);
string currentDirectory = System.Environment.CurrentDirectory;
Console.WriteLine("Current directory: " + currentDirectory);
bundleCommand.SetHandler((output, lang, note, sort, removeEmptyLines, author) =>
{
    try
    {
        if (lang != null && allowedLanguages.Contains(lang))
        {
            Console.WriteLine($"Selected language: {lang}");
        }
        else
        {
            throw new ArgumentException("Invalid or missing language option.");
        }
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine("Allowed languages:");
        foreach (var mylang in allowedLanguages)
        {
            Console.WriteLine(mylang);
        }
    }
    try
    {
        if (note)
            Console.WriteLine($"Note option set to: {note}");
       
            Console.WriteLine("Note option not specified");
    }
    catch (FormatException)
    {
        Console.WriteLine("Invalid value for --note option. Please provide a valid boolean value (True/False).");
    }
    List<string> csFiles = new List<string>();
    try
    {
        var extensionLanguage = "";
        switch (lang)
        {
            case "csharp":
                { extensionLanguage = extensionLanguagesList[0]; break; }
            case "fsharp":
                { extensionLanguage = extensionLanguagesList[1]; break; }
            case "vb":
                { extensionLanguage = extensionLanguagesList[2]; break; }
            case "pwsh":
                { extensionLanguage = extensionLanguagesList[3]; break; }
            case "sql":
                { extensionLanguage = extensionLanguagesList[4]; break; }
            case "all":
                { extensionLanguage = extensionLanguagesList[5]; break; }
        }
        foreach (var extension in extensionLanguagesList)
        {
            var allFiles = Directory.GetFiles(currentDirectory, extensionLanguage == "all" ? $"*.{extension}" : $"*.{extensionLanguage}", SearchOption.AllDirectories)
                                 .Where(file => !IsExcludedFolder(file, "bin") && !IsExcludedFolder(file, "obj") && !IsExcludedFolder(file, "debug"));
            csFiles.AddRange(allFiles);
            if (extensionLanguage != "all") break; else continue;
        }
        if (csFiles.Any())
        {
            Console.WriteLine($"Found {extensionLanguage} files in the root folder:");

            foreach (string file in csFiles)
            {
                Console.WriteLine(file);
            }
        }
        else
        {
            Console.WriteLine($"No {extensionLanguage} files found in the root folder.");
        }
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Access to the directory is unauthorized.");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Directory not found.");
    }
    catch (IOException)
    {
        Console.WriteLine("An IO error occurred.");
    }
    try
    {

        if (sort != null && (sort == "alphabetical" || sort == "extension"))
        {
            Console.WriteLine($"Selected language: {sort}");
            if (sort == "alphabetical")
            {
                csFiles = csFiles.OrderBy(file => Path.GetFileName(file)).ToList();
            }
            else if (sort == "extension")
            {
                csFiles = csFiles.OrderBy(file => Path.GetExtension(file)).ToList();
            }
        }
        else
        {
            throw new ArgumentException("Invalid or missing sort option.");
        }
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine("Allowed sort:");
        Console.WriteLine("alphabetical");
        Console.WriteLine("extension");
    }
    try
    {
        using (StreamWriter sw = new StreamWriter(output.FullName, true)) 
        {
            if (author != null)
                sw.WriteLine("//Author : " + author);
            foreach (string file in csFiles)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(file))
                    {
                        string line;
                        if (note)
                            sw.WriteLine("//" + file);
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }
                            sw.WriteLine(line);
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"File not found: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IO Exception: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected Exception: {ex.Message}");
                }
            }
        }
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("ERROR:file path is invalid" + ex.Message);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Exception: " + ex.Message);
    }
    finally
    {
        Console.WriteLine("Executing finally block.");
    }
}, bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

createRspCommand.SetHandler((output) =>
{
    try
    {
        using (StreamWriter sw = new StreamWriter(output.FullName))
        {
            var bundleOutput = PromptForValue<string>("route/name bundle file");
            var lang = PromptForValue<string>("Language");
            var note = PromptForValue<bool>("Include note (true/false)");
            var sort = PromptForValue<string>("Sort by (alphabetical/extension)");
            var removeEmptyLines = PromptForValue<bool>("Remove empty lines (true/false)");
            var author = PromptForValue<string>("Author");

            sw.WriteLine($"--output {bundleOutput}");
            sw.WriteLine($"--lang {lang}");
            sw.WriteLine($"--note {note}");
            sw.WriteLine($"--sort {sort}");
            sw.WriteLine($"--remove-empty-lines {removeEmptyLines}");
            sw.WriteLine($"--author {author}");
        }

        Console.WriteLine($"Response file created at {output.FullName}");
    }
    catch (Exception e)
    {
        Console.WriteLine("Error: " + e.Message);
    }
}, outputOption);

var rootCommand = new RootCommand("root command for file bundle CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);

static bool IsExcludedFolder(string path, string folderName)
{
    return path.Contains($"\\{folderName}\\", StringComparison.OrdinalIgnoreCase);
}
static T PromptForValue<T>(string promptMessage)
{
    Console.Write($"{promptMessage}: ");
    var input = Console.ReadLine();

    try
    {
        return (T)Convert.ChangeType(input, typeof(T));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return default; 
    }
}


