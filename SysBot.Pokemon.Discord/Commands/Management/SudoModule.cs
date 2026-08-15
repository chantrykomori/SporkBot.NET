using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public class SudoModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    [SlashCommand("blacklist", "Blacklists a Discord user.")]
    [RequireSudo]
    public async Task BlackListUser(
        [Summary("id", "User ID to blacklist")]ulong userId)
    {
        SysCordSettings.Settings.UserBlacklist.AddIfNew([GetReference(userId)]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("blacklist_comment", "Adds a comment for a blacklisted Discord user ID.")]
    [RequireSudo]
    public async Task BlackListUsers(
        [Summary("id", "User ID to blacklist")] ulong userId,
        [Summary("comment", "Comment to add")] string comment)
    {
        var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == userId);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that ID ({userId}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [SlashCommand("unblacklist", "Removes a mentioned Discord user from the blacklist.")]
    [RequireSudo]
    public async Task UnBlackListUsers(
        [Summary("id", "User ID to remove")] ulong userId)
    {
        var obj = new List<RemoteControlAccess> { GetReference(userId) };
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => obj.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("blacklist_summary", "Prints the list of blacklisted Discord users.")]
    [RequireSudo]
    public async Task PrintBlacklist()
    {
        var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [SlashCommand("ban_id", "Bans online user IDs.")]
    [RequireSudo]
    public async Task BanOnlineIDs([Summary("Comma Separated Online IDs")] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("banned_id_comment", "Adds a comment for a banned online user ID.")]
    [RequireSudo]
    public async Task BanOnlineIDs(ulong id, string comment)
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var obj = hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Unable to find a user with that online ID ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
    }

    [SlashCommand("unban_id", "Bans online user IDs.")]
    [RequireSudo]
    public async Task UnBanOnlineIDs([Summary("Comma Separated Online IDs")] string content)
    {
        var IDs = GetIDs(content);
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("banned_id_list", "Prints the list of banned online IDs.")]
    [RequireSudo]
    public async Task PrintBannedOnlineIDs()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var lines = hub.Config.TradeAbuse.BannedIDs.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [SlashCommand("forget_user", "Forgets users that were previously encountered.")]
    [RequireSudo]
    public async Task ForgetPreviousUser([Summary("Comma Separated Online IDs")] string content)
    {
        var IDs = GetIDs(content);
        foreach (var ID in IDs)
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllNID(ID);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllNID(ID);
        }
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("previous_user_list", "Prints a list of previously encountered users.")]
    [RequireSudo]
    public async Task PrintPreviousUsers()
    {
        bool found = false;
        var lines = PokeRoutineExecutorBase.PreviousUsers.Summarize().ToList();
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Previous Users:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        lines = PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize().ToList();
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Previous Distribution Users:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }
        if (!found)
            await ReplyAsync("No previous users found.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(ulong id) => new()
    {
        ID = id,
        Name = "Manual",
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    protected static IEnumerable<ulong> GetIDs(string content)
    {
        return content.Split([",", ", ", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
    }
}
