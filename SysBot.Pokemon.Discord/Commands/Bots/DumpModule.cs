using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("dump", "Queues new Dump trades")]
public class DumpModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("dump", "Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesDump))]
    public Task DumpAsync(int code = 0)
    {
        if (code == 0)
        {
            var randomTradeCode = Info.GetRandomTradeCode();
            return DumpAsync(randomTradeCode);
        }
        var sig = Context.User.GetFavor();
        return QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.Dump, PokeTradeType.Dump);
    }

    [SlashCommand("dump_queue_list", "Prints the users in the Dump queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Dump);
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
