namespace MotorTownDiscordBot;

using System;
using System.Diagnostics;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MotorTownDiscordBot.MotorTown;


public class BotInteraction
{
    private readonly DiscordSocketClient _client;
    private readonly WebAPI _webAPI;

    public BotInteraction(DiscordSocketClient client, WebAPI webAPI)
    {
        _client = client;
        _webAPI = webAPI;
        _client.SlashCommandExecuted += SlashCommandHandler;
    }


    public async Task RegisterCommands()
    {
        var kickCommand = new SlashCommandBuilder();
        kickCommand.WithName("kick");
        kickCommand.WithDescription("Kick player on the server");
        kickCommand.AddOption("player-id", ApplicationCommandOptionType.String, "Player id on the server", true);
        kickCommand.WithContextTypes([InteractionContextType.Guild]);

        var banCommand = new SlashCommandBuilder();
        banCommand.WithName("ban");
        banCommand.WithDescription("Ban player on the server");
        banCommand.AddOption("player-id", ApplicationCommandOptionType.String, "Player id on the server", true);
        banCommand.AddOption("reason", ApplicationCommandOptionType.String, "Ban reason", false);
        banCommand.AddOption("hours", ApplicationCommandOptionType.Integer, "Hours for temp ban", false);

        var unbanCommand = new SlashCommandBuilder();
        unbanCommand.WithName("unban");
        unbanCommand.WithDescription("Unban player on the server");
        unbanCommand.AddOption("player-id", ApplicationCommandOptionType.String, "Player id on the server", true);

        var playerListCommand = new SlashCommandBuilder();
        playerListCommand.WithName("player-list");
        playerListCommand.WithDescription("List of players on the server");

        var banListCommand = new SlashCommandBuilder();
        banListCommand.WithName("ban-list");
        banListCommand.WithDescription("List of banned players on the server");

        var announceCommand = new SlashCommandBuilder();
        announceCommand.WithName("announce");
        announceCommand.WithDescription("Send announcement message to server chat");
        announceCommand.AddOption("message", ApplicationCommandOptionType.String, "Message to the in game chat");

        try
        {
            // With global commands we don't need the guild.
            await _client.BulkOverwriteGlobalApplicationCommandsAsync([
                kickCommand.Build(),
                banCommand.Build(),
                playerListCommand.Build(),
                banListCommand.Build(),
                unbanCommand.Build(),
                announceCommand.Build()
             ]);
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch (HttpException exception)
        {
            Debug.WriteLine(exception);
            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(exception.Message);
        }
    }


    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await HandleInteraction(command);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Console.WriteLine(e.Message);
            }
        });

        return;
    }

    public async Task HandleInteraction(SocketSlashCommand command)
    {

        switch (command.Data.Name)
        {
            case "kick":
                await HandleKickCommand(command);
                break;
            case "ban":
                await HandleBanCommand(command);
                break;
            case "unban":
                await HandleUnbanCommand(command);
                break;
            case "player-list":
                await HandlePlayerListCommand(command);
                break;
            case "ban-list":
                await HandleBanListCommand(command);
                break;
            case "announce":
                await HandleAnnounceCommand(command);
                break;
        }

        return;
    }

    private async Task HandleAnnounceCommand(SocketSlashCommand command)
    {
        var message = command.Data.Options.FirstOrDefault(o => o.Name == "message")?.Value?.ToString();
        if (message == null || message.Length == 0)
        {
            return;
        }

        await _webAPI.SendMessage(message);
        await command.RespondAsync("Message sent");
    }

    private async Task HandleBanListCommand(SocketSlashCommand command)
    {
        var players = await _webAPI.GetPlayerBanList();
        if (players == null || players.Length == 0)
        {
            await command.RespondAsync("No player banned on the server");
            return;
        }

        var names = players.Select(p => $"{p.name} ({p.unique_id})").ToArray();
        await command.RespondAsync(string.Join('\n', names));
    }

    private async Task HandlePlayerListCommand(SocketSlashCommand command)
    {

        var players = await _webAPI.GetPlayerList();
        if (players == null || players.Length == 0)
        {
            await command.RespondAsync("No player on the server");
            return;
        }

        var names = players.Select(p => $"{p.name} ({p.unique_id})").ToArray();
        await command.RespondAsync(string.Join('\n', names));
    }

    public async Task HandleKickCommand(SocketSlashCommand command)
    {
        var playerId = command.Data.Options.FirstOrDefault(o => o.Name == "player-id")?.Value?.ToString();

        if (playerId == null)
        {
            await command.RespondAsync("Player id is required");
            return;
        }


        await _webAPI.PlayerKick(playerId);
        await command.RespondAsync($"Player kicked");
    }

    private async Task HandleUnbanCommand(SocketSlashCommand command)
    {
        var playerId = command.Data.Options.FirstOrDefault(o => o.Name == "player-id")?.Value?.ToString();

        if (playerId == null)
        {
            await command.RespondAsync("Player id is required");
            return;
        }

        await _webAPI.PlayerUnban(playerId);
        await command.RespondAsync($"Player ({playerId}) banned");
    }

    public async Task HandleBanCommand(SocketSlashCommand command)
    {
        var playerId = command.Data.Options.First(o => o.Name == "player-id")?.Value?.ToString();
        string? reason = command.Data.Options.First(o => o.Name == "reason")?.Value?.ToString();
        string? hours = command.Data.Options.First(o => o.Name == "hours")?.Value.ToString();

        if (playerId == null)
        {
            await command.RespondAsync("Player id is required");
            return;
        }

        BanOptions options = new BanOptions();
        options.reason = reason;

        try
        {
            options.hours = int.Parse(hours);
        }
        catch
        {
            // nothing to do
        }

        await _webAPI.PlayerBan(playerId, options);
        await command.RespondAsync($"Player ({playerId}) banned");
    }
}