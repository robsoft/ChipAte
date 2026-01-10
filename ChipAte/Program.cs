using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;


var services = new ServiceCollection();

services.AddLogging(b =>
{
    b.AddDebug();
    b.AddConsole();
    b.SetMinimumLevel(LogLevel.Information);
});

services.AddSingleton<ChipAte.Chip8Wrapper>();


using var provider = services.BuildServiceProvider();

using var game = provider.GetRequiredService<ChipAte.Chip8Wrapper>();
game.Run();

