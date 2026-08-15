using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("seed-check-module","Queues new Seed Check trades")]
public class SeedCheckModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("seed_check", "Checks the seed for a Pokémon.")]
    [RequireQueueRole(nameof(DiscordManager.RolesSeed))]
    public Task SeedCheckAsync(int code = 0)
    {
        if (code == 0)
        {
            var randomTradeCode = Info.GetRandomTradeCode();
            return SeedCheckAsync(randomTradeCode);
        }
        var sig = Context.User.GetFavor();
        return QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.SeedCheck, PokeTradeType.Seed);
    }

    [SlashCommand("seed_check_queue", "Prints the users in the Seed Check queue.")]
    [RequireSudo]
    public async Task GetSeedListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.SeedCheck);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [SlashCommand("find_frame", "Prints the next shiny frame from the provided seed.")]
    public async Task FindFrameAsync(string seedString)
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        seedString = seedString.ToLower();
        if (seedString.StartsWith("0x"))
            seedString = seedString[2..];

        var seed = PKHeX.Core.Util.GetHexValue64(seedString);

        var r = new SeedSearchResult(Z3SearchResult.Success, seed, -1, hub.Config.SeedCheckSWSH.ResultDisplayMode);
        var msg = r.ToString();

        var embed = new EmbedBuilder { Color = Color.LighterGrey };

        embed.AddField(x =>
        {
            x.Name = $"Seed: {seed:X16}";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync($"Here are the details for `{r.Seed:X16}`:", embed: embed.Build()).ConfigureAwait(false);
    }
}
