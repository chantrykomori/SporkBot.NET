using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public class LegalizerModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    [SlashCommand("legalize", "Tries to legalize the attached pkm data.")]
    public async Task LegalizeAsync(
        [Summary(".pkm Data")]IAttachment file)
    {
        await Context.Channel.ReplyWithLegalizedSetAsync(file).ConfigureAwait(false);
    }

    [SlashCommand("convert_showdown", "Tries to convert the Showdown Set to pkm data.")]
    public Task ConvertShowdown(
        [Summary("Showdown Set")] string content,
        [Summary("Generation/Format")] byte gen = 0)
    {
        return gen != 0
            ? Context.Channel.ReplyWithLegalizedSetAsync(content, gen)
            : Context.Channel.ReplyWithLegalizedSetAsync<T>(content);
    }
}
