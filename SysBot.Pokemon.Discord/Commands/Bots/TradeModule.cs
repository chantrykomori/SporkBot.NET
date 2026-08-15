using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PKHeX.Core;
using NetUtil = SysBot.Pokemon.Discord.NetUtil;

namespace SysBot.Pokemon.Discord;

[Group("trade-module","Queues new Link Code trades")]
public class TradeModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("trade_queue", "Prints the users in the trade queues.")]
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

    [SlashCommand("trade", "Makes the bot trade you the provided Pokémon file.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsyncAttach(
        [Summary(".pk File")] IAttachment file,
        [Summary("Trade Code")] int code = 0)
    {
        var sig = Context.User.GetFavor();
        return TradeAsyncAttach(code, sig, Context.User, file);
    }

    [SlashCommand("trade_showdown", "Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync(
        [Summary("Showdown Set")] string content,
        [Summary("Trade Code")] int code = 0)
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0 || set.Species is 0)
        {
            var sb = new StringBuilder(128);
            sb.AppendLine("Unable to parse Showdown Set.");
            if (set.InvalidLines.Count != 0)
            {
                sb.AppendLine("Invalid lines detected:\n```");
                foreach (var line in set.InvalidLines)
                    // this may not be the correct thing to pass, but BattleTemplateParseError is incompatible
                    sb.AppendLine(line.Value);
                sb.AppendLine("```");
            }
            if (set.Species is 0)
                sb.AppendLine("Species could not be identified. Check your spelling.");

            var msg = sb.ToString();
            await ReplyAsync(msg).ConfigureAwait(false);
            return;
        }

        try
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            bool pla = typeof(T) == typeof(PA8);

            if (!pla && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                TradeExtensions<T>.EggTrade(pkm, template);

            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];
            pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            bool memes = Info.Hub.Config.Trade.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
            if (memes)
                return;

            if (pkm is not T pk || !la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => $"That {spec} set took too long to generate.",
                    "VersionMismatch" => "Request refused: PKHeX and Auto-Legality Mod version mismatch.",
                    _ => $"I wasn't able to create a {spec} from that set.",
                };
                var imsg = $"Oops! {reason}";
                if (result == "Failed")
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                await ReplyAsync(imsg).ConfigureAwait(false);
                return;
            }
            pk.ResetPartyStats();

            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    // unsure if the summary for this one is correct, since the summary did not exist in the original
    [SlashCommand("ban_trade", "Bans a user from trading with the bot")]
    [RequireSudo]
    public async Task BanTradeAsync([Summary("User ID")] ulong userId, string comment)
    {
        SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew([GetReference(userId, comment)]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(ulong id, string comment) => new()
    {
        ID = id,
        Name = id.ToString(),
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
    };

    // command logic has been reworked in order to make use of slash command context. users must now be server members in order to use this code, and they are called by user ID which is more easily exposed in the UI.
    [SlashCommand("trade_user", "Makes the bot trade a server member the attached file.")]
    [RequireSudo]
    public async Task TradeAsyncAttachUser(
        [Summary("User ID")] ulong userId,
        [Summary(".pk File")]IAttachment file,
        [Summary("Trade Code")] int code = 0)
    {
        // there may be a better way to implement IUser here
        var usr = Context.Guild.GetUser(userId);
        var sig = usr.GetFavor();
        if (code == 0)
        {
            var randomCode = Info.GetRandomTradeCode();
            await TradeAsyncAttach(randomCode, sig, usr, file).ConfigureAwait(false);
        }
        else
        {
            await TradeAsyncAttach(code, sig, usr, file).ConfigureAwait(false);
        }
    }

    private async Task TradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr, IAttachment attachment)
    {
        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }

        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr).ConfigureAwait(false);
    }

    private static T? GetRequest(Download<PKM> dl)
    {
        if (!dl.Success)
            return null;
        return dl.Data switch
        {
            null => null,
            T pk => pk,
            _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
        };
    }

    private async Task AddTradeToQueueAsync(int code, string trainerName, T pk, RequestSignificance sig, SocketUser usr)
    {
        var cfg = Info.Hub.Config.Trade;
        var la = new LegalityAnalysis(pk);
        if (!la.Valid)
        {
            // Disallow trading illegal Pokémon.
            await ReplyAsync($"{typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
            return;
        }
        if (!pk.CanBeTraded(la.EncounterOriginal))
        {
            // Disallow anything that cannot be traded from the game (e.g. Fusions).
            await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
            return;
        }
        if (cfg.DisallowNonNatives && (la.EncounterOriginal.Context != pk.Context || pk.GO))
        {
            // Allow the owner to prevent trading entities that require a HOME Tracker even if the file has one already.
            await ReplyAsync($"{typeof(T).Name} attachment is not native, and cannot be traded!").ConfigureAwait(false);
            return;
        }
        if (cfg.DisallowTracked && pk is IHomeTrack { HasTracker: true })
        {
            // Allow the owner to prevent trading entities that already have a HOME Tracker.
            await ReplyAsync($"{typeof(T).Name} attachment is tracked by HOME, and cannot be traded!").ConfigureAwait(false);
            return;
        }

        await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr).ConfigureAwait(false);
    }
}
