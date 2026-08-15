using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public class EtumrepDumpModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("etumrep_dump", "Dumps the Pokémon you show via Link Trade, with the option to run EtumrepMMO and PermuteMMO.")]
    [RequireQueueRole(nameof(DiscordManager.RolesEtumrepDump))]
    public async Task EtumrepDumpAsync(int code)
    {
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.EtumrepDump, PokeTradeType.EtumrepDump).ConfigureAwait(false);
    }

    // remainder attribute before param
    [SlashCommand("etumrep_dump", "Dumps the Pokémon you show via Link Trade, with the option to run EtumrepMMO and PermuteMMO.")]
    [RequireQueueRole(nameof(DiscordManager.RolesEtumrepDump))]
    public async Task EtumrepDumpAsync([Summary("Trade Code")] string code)
    {
        int tradeCode = PKHeX.Core.Util.ToInt32(code);
        var sig = Context.User.GetFavor();
        await QueueHelper<T>.AddToQueueAsync(Context, tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sig, new T(), PokeRoutineType.EtumrepDump, PokeTradeType.EtumrepDump).ConfigureAwait(false);
    }

    [SlashCommand("etumrep_dump", "Dumps the Pokémon you show via Link Trade, with the option to run EtumrepMMO and PermuteMMO.")]
    [RequireQueueRole(nameof(DiscordManager.RolesEtumrepDump))]
    public async Task EtumrepDumpAsync()
    {
        var code = Info.GetRandomTradeCode();
        await EtumrepDumpAsync(code).ConfigureAwait(false);
    }

    [SlashCommand("etumrep_dump_queue_list", "Prints the users in the Etumrep Dump queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.EtumrepDump);
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
