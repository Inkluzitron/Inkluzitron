using Discord.WebSocket;
using ImageMagick;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Msagl.Core;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class FamilyTreeService
    {
        private readonly DatabaseFactory DbFactory;
        private readonly ImagesService ImagesService;
        private readonly UsersService UsersService;

        private readonly MagickColor MemberNicknameColor = MagickColors.White;
        private readonly MagickColor NotAMemberNicknameColor = MagickColors.Gray;
        private readonly MagickColor EdgeColor = MagickColors.Gray;

        private const string NicknameFontFamily = "Open Sans";
        private const int NicknameFontSize = 10;
        private readonly DrawableFont NicknameFont = new (NicknameFontFamily) { Weight = FontWeight.Bold };
        private const float NodeHeight = 128f;
        private const float NodeWidth = 128f;

        public FamilyTreeService(DatabaseFactory dbFactory, ImagesService imagesService, UsersService usersService)
        {
            DbFactory = dbFactory;
            ImagesService = imagesService;
            UsersService = usersService;
        }

        public async Task<TemporaryFile> RenderFamilyTreeAsync(SocketGuild guild)
        {
            if (guild == null)
                throw new ArgumentNullException(nameof(guild));

            await guild.DownloadUsersAsync();
            var guildUserIds = guild.Users.Select(u => u.Id).ToHashSet();
            var avatarSize = new MagickGeometry(100, 100);

            using var dbContext = DbFactory.Create();
            var graph = new GeometryGraph() { Margins = 64 };

            var invitesSource = dbContext.Invites.Include(i => i.UsedBy).Include(i => i.GeneratedBy);
            var invites = invitesSource.AsQueryable().Where(i => !i.GeneratedAt.HasValue)
                .Concat(invitesSource.AsQueryable().Where(i => i.GeneratedAt.HasValue).OrderBy(i => i.GeneratedAt));

            var userDisplayNames = new Dictionary<ulong, string>();

            await foreach (var invite in invites.ToAsyncEnumerable())
            {
                if (!invite.UsedByUserId.HasValue)
                    continue;
                if (invite.UsedBy is null || invite.GeneratedBy is null)
                    continue;

                var parentNode = await GetOrCreateNodeAsync(graph, invite.GeneratedBy, userDisplayNames);
                var childNode = await GetOrCreateNodeAsync(graph, invite.UsedBy, userDisplayNames);
                graph.Edges.Add(new Edge(parentNode, childNode));
            }

            LayoutHelpers.CalculateLayout(
                graph,
                new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings(),
                new CancelToken()
            );

            // The computed graph layout generally does not start at (0,0).
            var compensationX = 0 - graph.Nodes.Min(n => n.BoundingBox.Center.X - NodeWidth);
            var compensationY = 0 - graph.Nodes.Min(n => n.BoundingBox.Center.Y - NodeHeight);

            using var image = new MagickImage(
                MagickColors.Black,
                (int)Math.Ceiling(graph.Width),
                (int)Math.Ceiling(graph.Height)
            );

            IDrawables<byte> drawables = new Drawables()
                .StrokeWidth(1.5)
                .StrokeColor(EdgeColor);

            // Draw lines underneath everything else
            foreach (var edge in graph.Edges)
            {
                var from = edge.Source.Center;
                var to = edge.Target.Center;
                drawables = drawables.Line(
                    compensationX + from.X,
                    compensationY + from.Y,
                    compensationX + to.X,
                    compensationY + to.Y
                );
            }

            drawables.Draw(image);

            // Follow up with avatars
            foreach (var node in graph.Nodes)
            {
                var user = (User)node.UserData;
                var bbox = node.BoundingBox;

                using var avatar = await ImagesService.GetAvatarAsync(guild, user.Id);
                using var firstFrame = avatar.Frames[0].Clone();
                firstFrame.Resize(avatarSize);
                firstFrame.RoundImage();

                var avatarDestinationX = (int)Math.Round(compensationX + bbox.Center.X - 0.5 * avatarSize.Width);
                var avatarDestinationY = (int)Math.Round(compensationY + bbox.Center.Y - 0.5 * avatarSize.Height);

                if (!guildUserIds.Contains(user.Id))
                    firstFrame.Grayscale();

                image.Composite(firstFrame, avatarDestinationX, avatarDestinationY, CompositeOperator.Over);
            }

            // Finish with user display names
            var nicknameBoxWidth = (int)Math.Round(0.9 * NodeWidth);
            foreach (var node in graph.Nodes)
            {
                var user = (User)node.UserData;
                var bbox = node.BoundingBox;

                int tx = (int)Math.Round(compensationX + bbox.Center.X - 0.45 * NodeWidth);
                int ty = (int)Math.Round(compensationY + bbox.Center.Y + 0.50 * avatarSize.Height + 10);

                image.DrawEnhancedText(
                    userDisplayNames[user.Id],
                    Gravity.Center,
                    tx, ty,
                    guildUserIds.Contains(user.Id) ? MemberNicknameColor : NotAMemberNicknameColor,
                    NicknameFont, NicknameFontSize, nicknameBoxWidth
                );
            }

            TemporaryFile result = null;

            try
            {
                result = new TemporaryFile("png");
                image.Write(result.Path, MagickFormat.Png);
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
        }

        private async Task<Node> GetOrCreateNodeAsync(GeometryGraph graph, User user, Dictionary<ulong, string> userDisplayNames)
        {
            var node = graph.FindNodeByUserData(user);

            if (node is null)
            {
                var userDisplayName = user.Name ?? await UsersService.GetDisplayNameAsync(user.Id);
                var nodeGeometry = CurveFactory.CreateRectangle(NodeWidth, NodeHeight, new Microsoft.Msagl.Core.Geometry.Point(0.5, 0.5));

                node = new Node(nodeGeometry, user);
                userDisplayNames.Add(user.Id, userDisplayName);
                graph.Nodes.Add(node);
            }

            return node;
        }
    }
}
