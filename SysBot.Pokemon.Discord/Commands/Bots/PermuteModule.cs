using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

// need to fix failure state where the bot can't send a DM
// right now it uses the old method to post a message in the guild it was invoked in
// it needs to give a failure message in the new interaction way
public class PermuteModule<T> : InteractionModuleBase<SocketInteractionContext> where T : PKM, new()
{
    [SlashCommand("permute", "Gets shiny path results for the specified filter and provided JSON.")]
    [RequireQueueRole(nameof(DiscordManager.RolesEtumrepDump))]
    public async Task PermuteAsync()
    {
        var ch = await Context.User.CreateDMChannelAsync().ConfigureAwait(false);
        var selectMenuBuilder = PermuteUtil.GetPermuteServiceSelectMenu();
        var component = new ComponentBuilder().WithSelectMenu(selectMenuBuilder).Build();

        try
        {
            await ch.SendMessageAsync("**Permute Command Service**", false, null, null, null, null, component).ConfigureAwait(false);
        }
        catch (HttpException ex)
        {
            await Context.Channel.SendMessageAsync($"Could not send a DM: {ex.Message}").ConfigureAwait(false);
        }

    }
}
