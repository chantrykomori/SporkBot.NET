using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
{
    [SlashCommand("add_sudo", "Adds user to global sudo")]
    [RequireOwner]
    public async Task SudoUser(
        [Summary("id", "User ID to add")] ulong userId)
    {
        SysCordSettings.Settings.GlobalSudoList.AddIfNew([GetReference(userId)]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("remove_sudo", "Removes mentioned user from global sudo")]
    [RequireOwner]
    public async Task RemoveSudoUsers(
        [Summary("id", "User ID to remove")] ulong userId)
    {
        var obj = new List<RemoteControlAccess> { GetReference(userId) };
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => obj.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("add_channel", "Adds a channel to the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task AddChannel()
    {
        var obj = GetReference(Context.Channel);
        SysCordSettings.Settings.ChannelWhitelist.AddIfNew([obj]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("remove_channel", "Removes a channel from the list of channels that are accepting commands.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task RemoveChannel()
    {
        var obj = GetReference(Context.Channel);
        SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("leave", "Leaves the current server.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task Leave()
    {
        await ReplyAsync("Goodbye.").ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("leave_guild", "Leaves guild based on supplied ID.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task LeaveGuild(string userInput)
    {
        if (!ulong.TryParse(userInput, out ulong id))
        {
            await ReplyAsync("Please provide a valid Guild ID.").ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await ReplyAsync($"Provided input ({userInput}) is not a valid guild ID or the bot is not in the specified guild.").ConfigureAwait(false);
            return;
        }

        await ReplyAsync($"Leaving {guild}.").ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("leave_all", "Leaves all servers the bot is currently in.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task LeaveAll()
    {
        await ReplyAsync("Leaving all servers.").ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds)
            await guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("add_tradecord", "Adds a channel to the list of TradeCord channels that are accepting commands.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task AddTC()
    {
        var obj = GetReference(Context.Channel);
        SysCordSettings.Settings.TradeCordChannels.AddIfNew(new[] { obj });
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("remove_tradecord", "Removes a channel from the list of TradeCord channels that are accepting commands.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task RemoveTC()
    {
        var obj = GetReference(Context.Channel);
        SysCordSettings.Settings.TradeCordChannels.RemoveAll(z => z.ID == obj.ID);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("sudoku", "Causes the entire process to end itself!")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task ExitProgram()
    {
        await Context.Channel.EchoAndReply("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
        Environment.Exit(0);
    }

    private RemoteControlAccess GetReference(ulong id) => new()
    {
        ID = id,
        Name = "Manual",
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
