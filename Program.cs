using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class Program
{
    private static DiscordSocketClient client = new();
    public static string? FollowedUser;
    public static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        client.Log += Log;
        client.Ready += Client_Ready;
        client.SlashCommandExecuted += SlashCommandHandler;

        await client.LoginAsync(TokenType.Bot, config["BotToken"]);
        await client.StartAsync();

        await Task.Delay(-1);
    }

    public static async Task Client_Ready()
    {
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("top");
        globalCommand.WithDescription("View forum posts sorted by reaction count");
        globalCommand.AddOption("channel", ApplicationCommandOptionType.Channel, "hmm");

        await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
    }
    private static async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "top":
                await HandleTop(command);
                break;
        }
    }
    private static async Task HandleTop(SocketSlashCommand command)
    {

        await command.DeferAsync();
        var channelOption = command.Data.Options.FirstOrDefault(o => o.Name == "channel");
        if (channelOption != null)
        {
            var channel = channelOption.Value as SocketForumChannel;
            if (channel != null)
            {
                var threads = await channel.GetActiveThreadsAsync();
                foreach (var thread in threads)
                {
                    Console.WriteLine(thread);
                    Console.WriteLine(thread.Id);
                    Console.WriteLine(thread.CreatedAt);
                    var messages = await thread.GetMessagesAsync(1).FlattenAsync();
                    var firstMessage = messages.FirstOrDefault();
                    if (firstMessage != null)
                    {
                        int totalReactionCount = firstMessage.Reactions.Sum(r => r.Value.ReactionCount);
                        Console.WriteLine($"The first post in the thread has {totalReactionCount} reactions.");
                    }
                }
            }
        }
        await command.FollowupAsync("yaaay");
    }
    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}