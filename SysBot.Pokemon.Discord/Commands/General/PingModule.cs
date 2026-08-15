using System.Threading.Tasks;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

public class PingModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Makes the bot respond, indicating that it is running.")]
    public async Task PingAsync()
    {
        await ReplyAsync("Pong!").ConfigureAwait(false);
    }
}
