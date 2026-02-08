using Spectre.Console.Cli;
using VideoToBlog.Commands;

// If no arguments, run interactive mode
if (args.Length == 0)
{
    var interactiveApp = new CommandApp<InteractiveCommand>();
    interactiveApp.Configure(config =>
    {
        config.SetApplicationName("ytblog-pilot");
        config.SetApplicationVersion("1.0.0");
    });
    return await interactiveApp.RunAsync(args);
}

// Otherwise, run traditional command-line mode
var app = new CommandApp<ConvertCommand>();

app.Configure(config =>
{
    config.SetApplicationName("ytblog-pilot");
    config.SetApplicationVersion("1.0.0");
    
    config.AddExample("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
    config.AddExample("https://youtu.be/dQw4w9WgXcQ", "--html", "-v");
    config.AddExample("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "--output", "./myblog", "--template", "custom");
});

return await app.RunAsync(args);
