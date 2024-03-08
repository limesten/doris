using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

public class Program
{
    private static DiscordSocketClient client = new();
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
        globalCommand.WithName("order-by-reactions");
        globalCommand.WithDescription("View forum posts sorted by reaction count");
        globalCommand.AddOption("channel", ApplicationCommandOptionType.Channel, "choose a forum channel");

        await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
    }
    private static async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "order-by-reactions":
                await HandleOrderByReactions(command);
                break;
        }
    }
    private static async Task HandleOrderByReactions(SocketSlashCommand command)
    {
        await command.DeferAsync();
        var channelOption = command.Data.Options.FirstOrDefault(o => o.Name == "channel");
        if (channelOption == null || channelOption.Value == null)
        {
            await command.FollowupAsync("OrderByReactions failed");
            return;
        }

        var channel = channelOption.Value as SocketForumChannel;
        if (channel == null)
        {
            await command.FollowupAsync("OrderByReactions failed");
            return;
        }

        var threadReactionCounts = new Dictionary<string, int>();

        var threads = await channel.GetActiveThreadsAsync();
        foreach (var thread in threads)
        {
            var messages = await thread.GetMessagesAsync(1).FlattenAsync();
            var firstMessage = messages.FirstOrDefault();
            if (firstMessage != null)
            {
                int totalReactionCount = firstMessage.Reactions.Sum(r => r.Value.ReactionCount);
                threadReactionCounts.Add($"<#{thread.Id}>", totalReactionCount);
            }
        }

        var sortedReactionCounts = threadReactionCounts.OrderByDescending(kvp => kvp.Value);
        string response = string.Join(Environment.NewLine, sortedReactionCounts.Select(kvp => $"{kvp.Key}: Reactions: {kvp.Value}"));

        await command.FollowupAsync(response);
    }
    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}