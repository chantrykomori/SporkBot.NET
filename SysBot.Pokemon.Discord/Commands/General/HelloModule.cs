using System.Threading.Tasks;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

public class HelloModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hello", "Say hello to the bot and get a response.")]
    public async Task PingAsync()
    {
        var str = SysCordSettings.Settings.HelloResponse;
        var msg = string.Format(str, Context.User.Mention);
        await ReplyAsync(msg).ConfigureAwait(false);
    }
}
