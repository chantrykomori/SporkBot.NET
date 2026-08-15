using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using NetUtil = SysBot.Pokemon.Discord.Helpers.NetUtil;

namespace SysBot.Pokemon.Discord;

public class LegalityCheckModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("verify_legality", "Verifies the attachment for legality.")]
    public async Task LegalityCheck(
        [Summary("file", "File to be checked")] IAttachment file,
        [Summary("verbose", "Whether to return a verbose output (default is false)")] bool verbose = false)
    {
        var download = await NetUtil.DownloadPKMAsync(file).ConfigureAwait(false);
        if (!download.Success)
        {
            await ReplyAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        var la = new LegalityAnalysis(pkm);
        var builder = new EmbedBuilder
        {
            Color = la.Valid ? Color.Green : Color.Red,
            Description = $"Legality Report for {download.SanitizedFileName}:",
        };

        builder.AddField(x =>
        {
            x.Name = la.Valid ? "Valid" : "Invalid";
            x.Value = la.Report(verbose);
            x.IsInline = false;
        });

        await ReplyAsync("Here's the legality report!", false, builder.Build()).ConfigureAwait(false);
    }
}
