using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("queue_module", "Clears and toggles Queue features.")]
public class QueueModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("queue_status", "Checks the user's position in the queue.")]
    public async Task GetTradePositionAsync()
    {
        var msg = Context.User.Mention + " - " + Info.GetPositionString(Context.User.Id);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [SlashCommand("queue_clear", "Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    public async Task ClearTradeAsync()
    {
        string msg = ClearTrade();
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    // moving this to require ID number until i learn if the interaction framework supports overloading
    [SlashCommand("queue_clear_other_user", "Clears another user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Discord user ID")] ulong id)
    {
        string msg = ClearTrade(id);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [SlashCommand("queue_clear_all", "Clears all users from the trade queues.")]
    [RequireSudo]
    public async Task ClearAllTradesAsync()
    {
        Info.ClearAllQueues();
        await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
    }

    [SlashCommand("queue_toggle", "Toggles on/off the ability to join the trade queue.")]
    [RequireSudo]
    public Task ToggleQueueTradeAsync()
    {
        var state = Info.ToggleQueue();
        var msg = state
            ? "Users are now able to join the trade queue."
            : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

        return Context.Channel.EchoAndReply(msg);
    }

    [SlashCommand("queue_mode", "Changes how queueing is controlled (manual/threshold/interval).")]
    [RequireSudo]
    public async Task ChangeQueueModeAsync([Summary("Queue mode")] QueueOpening mode)
    {
        SysCord<T>.Runner.Hub.Config.Queues.QueueToggleMode = mode;
        await ReplyAsync($"Changed queue mode to {mode}.").ConfigureAwait(false);
    }

    [SlashCommand("queue_user_list", "Private messages the list of users in the queue.")]
    [RequireSudo]
    public async Task ListUserQueue()
    {
        var lines = SysCord<T>.Runner.Hub.Queues.Info.GetUserList("(ID {0}) - Code: {1} - {2} - {3}");
        var msg = string.Join("\n", lines);
        if (msg.Length < 3)
            await ReplyAsync("Queue list is empty.").ConfigureAwait(false);
        else
            await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
    }

    private string ClearTrade()
    {
        var userID = Context.User.Id;
        return ClearTrade(userID);
    }

    private static string ClearTrade(ulong userID)
    {
        var result = Info.ClearTrade(userID);
        return GetClearTradeMessage(result);
    }

    private static string GetClearTradeMessage(QueueResultRemove result)
    {
        return result switch
        {
            QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Did not remove from all queues.",
            QueueResultRemove.CurrentlyProcessingRemoved => "Looks like you're currently being processed!",
            QueueResultRemove.Removed => "Removed you from the queue.",
            _ => "Sorry, you are not currently in the queue.",
        };
    }
}
