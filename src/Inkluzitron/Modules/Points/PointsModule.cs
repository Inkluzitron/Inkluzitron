using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using System.IO;
using System.Threading.Tasks;
using SysDraw = System.Drawing;

namespace Inkluzitron.Modules.Points
{
    [Group("body")]
    [Name("Body")]
    [Summary("Body se počítají stejně jako u GrillBot. Za každou reakci uživatel obdrží 0 až 10 bodů, za zprávu 0 až 25 bodů. Po odeslání zprávy " +
        "bot počítá jedno minutový cooldown. U reakce je cooldown 30 vteřin.")]
    public class PointsModule : ModuleBase
    {
        private PointsService PointsService { get; }

        public PointsModule(PointsService pointsService)
        {
            PointsService = pointsService;
        }

        [Command("kde")]
        [Alias("gde")]
        [Summary("Aktuální stav bodů uživatele.")]
        public async Task GetPointsAsync(IUser member = null)
        {
            if (member == null) member = Context.User;
            using var points = await PointsService.GetPointsAsync(member);

            if (points == null)
            {
                await ReplyAsync($"Uživatel `{member.GetDisplayName()}` ještě nemá žádné body.");
                return;
            }

            using var tmpFile = new TemporaryFile("png");
            points.Save(tmpFile.Path, SysDraw.Imaging.ImageFormat.Png);

            await ReplyFileAsync(tmpFile.Path);
        }
    }
}
