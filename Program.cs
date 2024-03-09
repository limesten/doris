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
        globalCommand.WithName("most-commented-threads");
        globalCommand.WithDescription("View forum threads sorted by the most comments");
        globalCommand.AddOption("forum-channel", ApplicationCommandOptionType.Channel, "choose a forum channel", isRequired: true, channelTypes: new List<ChannelType> { ChannelType.Forum });

        await client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
    }
    private static async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "most-commented-threads":
                await HandleMostCommentedThreads(command);
                break;
        }
    }
    private static async Task HandleMostCommentedThreads(SocketSlashCommand command)
    {
        try
        {
            await command.DeferAsync();
            var channelOption = command.Data.Options.FirstOrDefault(o => o.Name == "forum-channel");
            if (channelOption == null || channelOption.Value == null)
            {
                throw new NullReferenceException("channelOption or channelOption.Value was null");
            }

            var channel = channelOption.Value as SocketForumChannel;
            if (channel == null)
            {
                throw new NullReferenceException("channel was null");
            }

            var threadMessageCounts = new Dictionary<string, int>();

            var activeThreads = await channel.GetActiveThreadsAsync();
            foreach (var thread in activeThreads)
            {
                threadMessageCounts.Add($"<#{thread.Id}>", thread.MessageCount + 1);
            }

            var archivedThreads = await channel.GetPublicArchivedThreadsAsync();
            foreach (var thread in archivedThreads)
            {
                threadMessageCounts.Add($"<#{thread.Id}>", thread.MessageCount + 1);
            }


            var sortedMessageCounts = threadMessageCounts.OrderByDescending(kvp => kvp.Value);
            string response = string.Join(Environment.NewLine, sortedMessageCounts.Select(kvp => $"{kvp.Key}: Messages: {kvp.Value}"));

            await command.FollowupAsync(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in HandleOrderByReactions: {ex.Message}");
            await command.FollowupAsync("An error occurred while processing your request.");
        }
    }
    private static Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}