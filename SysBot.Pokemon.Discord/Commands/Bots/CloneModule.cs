using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("clone", "Queues new Clone trades")]
public class CloneModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    // on command:
    // generate random code, send it, and wait for user to connect
    [SlashCommand("clone", "Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public Task CloneAsync(int code = 0)
    {
        if (code == 0)
        {
            var randomTradeCode = Info.GetRandomTradeCode();
            return CloneAsync(randomTradeCode);
        }
        var sig = Context.User.GetFavor();
        return QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone);
    }

    [SlashCommand("clone_queue_list", "Prints the users in the Clone queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Clone);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}
