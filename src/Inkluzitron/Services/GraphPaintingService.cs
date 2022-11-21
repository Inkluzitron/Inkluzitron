using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Inkluzitron.Utilities;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using ImageMagick;

namespace Inkluzitron.Services
{
    public sealed class GraphPaintingService
    {
        private ImagesService ImagesService { get; }

        public GraphPaintingService(ImagesService imagesService)
            => ImagesService = imagesService;

        public async Task<MagickImage> DrawAsync(SocketGuild guild, GraphPaintingStrategy strategy, IDictionary<string, IReadOnlyList<GraphItem>> toplistResults)
        {
            if (guild == null)
                throw new ArgumentNullException(nameof(guild));
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));
            if (toplistResults is null)
                throw new ArgumentNullException(nameof(toplistResults));

            // Do not draw empty category boxes, skip categories that have no results.
            foreach (var k in toplistResults.Keys.ToList())
            {
                // Skip ghost users
                toplistResults[k] = toplistResults[k].Where(item => item.UserDisplayName != null).ToList().AsReadOnly();

                if (toplistResults[k].Count == 0)
                    toplistResults.Remove(k);
            }

            // Download all needed avatars.
            using var avatars = new ValuesDisposingDictionary<ulong, IMagickImage<byte>>();

            foreach (var userId in toplistResults.SelectMany(x => x.Value).Select(x => x.UserId).Distinct())
            {
                using var rawAvatar = await ImagesService.GetAvatarAsync(guild, userId);
                var avatar = rawAvatar.Frames[0].Clone();
                avatar.Resize(strategy.AvatarSize, strategy.AvatarSize);
                avatar.RoundImage();
                avatars[userId] = avatar;
            }

            // Obtain all quiz results to be displayed and sort them by category name.
            var topList = toplistResults.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase).ToList();

            var categoryWidth = (3 + topList.Max(kvp => kvp.Value.Count)) * strategy.AvatarSize;
            var categoryHeight = strategy.CategoryBoxHeight;
            var columnCount = Math.Min(strategy.ColumnCount, toplistResults.Count);
            var rowCount = (int)Math.Ceiling(topList.Count / (1f * columnCount));
            var imageWidth = (2f * strategy.CategoryBoxPadding) + ((columnCount - 1) * strategy.CategoryBoxPadding) + (columnCount * categoryWidth);
            var imageHeight = (2f * strategy.CategoryBoxPadding) + ((rowCount - 1) * strategy.CategoryBoxPadding) + (rowCount * categoryHeight);
            var image = new MagickImage(strategy.BackgroundColor, (int)Math.Ceiling(imageWidth), (int)Math.Ceiling(imageHeight));

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var x = strategy.CategoryBoxPadding + (columnIndex * (categoryWidth + strategy.CategoryBoxPadding));

                for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var y = strategy.CategoryBoxPadding + (rowIndex * (categoryHeight + strategy.CategoryBoxPadding));

                    var i = (rowIndex * columnCount) + columnIndex;
                    if (i >= topList.Count)
                        continue;

                    var minValueInCategory = Convert.ToSingle(topList[i].Value.Min(v => v.Value));
                    var maxValueInCategory = Convert.ToSingle(topList[i].Value.Max(v => v.Value));

                    var categoryRect = new MagickGeometry(x, y, categoryWidth, categoryHeight);

                    DrawCategory(
                        image, strategy, categoryRect, topList[i].Key,
                        minValueInCategory, maxValueInCategory,
                        out var avatarsRect
                    );

                    DrawAvatars(
                        image, strategy, avatarsRect, new MagickGeometry(strategy.AvatarSize, strategy.AvatarSize),
                        minValueInCategory,
                        maxValueInCategory,
                        topList[i].Value.Select(
                            x => (
                                avatars[x.UserId],
                                x.UserDisplayName,
                                Convert.ToSingle(x.Value)
                            )
                        )
                    );
                }
            }

            return image;
        }

        static private IDrawables<byte> DrawGridLine(IDrawables<byte> drawable, GraphPaintingStrategy strategy, IMagickGeometry dst, float y, float value, bool isMajor, double gridLineLabelPadding)
        {
            var lineColor = isMajor ? strategy.GridLineColorMajor : strategy.GridLineColorMinor;
            var textForeground = isMajor ? strategy.GridLinePercentageForegroundMajor : strategy.GridLinePercentageForegroundMinor;
            var str = strategy.FormatGridLineValueLabel(value);
            var strMetrics = drawable.FontTypeMetrics(str);

            return drawable
                .StrokeColor(lineColor)
                .Line(dst.X, y, dst.X + dst.Width, y)
                .StrokeColor(MagickColors.Transparent)
                .FillColor(textForeground)
                .Text(dst.X + dst.Width + gridLineLabelPadding, y + (0.3f * strMetrics.TextHeight), str);
        }

        static private void DrawCategoryGridLines(IMagickImage<byte> image, GraphPaintingStrategy strategy, IMagickGeometry dst, float minValue, float maxValue, out IMagickGeometry avatarsArea)
        {
            (minValue, maxValue) = strategy.SmoothenAxisLimits(minValue, maxValue);

            var drawable = new Drawables()
                .Density(100)
                .Font(strategy.GridLinePercentageFont)
                .FontPointSize(strategy.GridLinePercentageFontSize);

            // Determine width of the column of grid line percentage labels.
            var gridLineLabelMaxWidth = new[] {
                drawable.FontTypeMetrics(strategy.FormatGridLineValueLabel(minValue)).TextWidth,
                drawable.FontTypeMetrics(strategy.FormatGridLineValueLabel(maxValue)).TextWidth
            }.Max();

            // Measure how wide is an 'X' and use this value as a spacer for these percentage labels.
            var gridLineLabelPadding = drawable.FontTypeMetrics("X").TextWidth;

            // Shrink the area where grid lines so that there's space left for percentage labels.
            dst.Width -= (int)(gridLineLabelMaxWidth - gridLineLabelPadding);

            // Determine the displayed ranged and adjust the number of grid lines so that the percentage values are nice.
            var innerGridLineCount = strategy.CalculateGridLineCount(minValue, maxValue);
            var step = (maxValue - minValue) / (innerGridLineCount + 1);

            // Finally, draw the grid lines.
            drawable = DrawGridLine(drawable, strategy, dst, dst.Y + dst.Height, minValue, true, gridLineLabelPadding);

            for (var i = 1; i <= innerGridLineCount; i++)
            {
                float v = minValue + (i * step);
                float y = dst.Y + dst.Height - (dst.Height * (v - minValue) / (maxValue - minValue));

                drawable = DrawGridLine(drawable, strategy, dst, y, v, false, gridLineLabelPadding);
            }

            drawable = DrawGridLine(drawable, strategy, dst, dst.Y, maxValue, true, gridLineLabelPadding);
            drawable.Draw(image);

            // Return the area that has been drawn over with grid lines.
            avatarsArea = dst;
        }

        static private void DrawCategory(IMagickImage<byte> image, GraphPaintingStrategy strategy, IMagickGeometry dst, string categoryName, float minValue, float maxValue, out IMagickGeometry avatarsArea)
        {
            // Initialize the dimensions of the category box and fill it.
            var graphArea = new MagickGeometry(dst.X, dst.Y, dst.Width, dst.Height);

            var drawable = new Drawables()
                .Density(100)
                .FillColor(strategy.CategoryBoxBackground)
                .Rectangle(graphArea.X, graphArea.Y, graphArea.X + graphArea.Width, graphArea.Y + graphArea.Height)
                .FillColor(strategy.CategoryBoxHeadingForeground)
                .Font(strategy.CategoryBoxHeadingFont)
                .FontPointSize(strategy.CategoryBoxHeadingFontSize)
                .TextAlignment(TextAlignment.Center);

            // Measure the category heading, draw it and shrink the graph area so that it does not overlap.
            var headingSize = drawable.FontTypeMetrics(categoryName);
            var headingPadding = 0.2f * headingSize.TextHeight;
            var headingHeightWithPadding = (int)((2 * headingPadding) + headingSize.TextHeight);
            graphArea.Y += headingHeightWithPadding;
            graphArea.Height -= headingHeightWithPadding;

            drawable
                .Text(
                    graphArea.X + 0.5f * graphArea.Width,
                    graphArea.Y - headingPadding,
                    categoryName)
                .Draw(image);

            // Reduce the width & center the graph area so that there is some (= 0.75*AvatarSize) padding left.
            var gridLinesArea = graphArea;
            gridLinesArea.X += (int)(0.75f * strategy.AvatarSize);
            gridLinesArea.Width -= (int)(0.75f * strategy.AvatarSize * 2);
            gridLinesArea.Y -= (int)((-0.75f * strategy.AvatarSize) - (0.50f * headingSize.TextHeight));
            gridLinesArea.Height += (int)(((-0.75f * strategy.AvatarSize) - (0.50f * headingSize.TextHeight)) * 2);

            // Draw grid lines over the established area.
            DrawCategoryGridLines(image, strategy, gridLinesArea, minValue, maxValue, out avatarsArea);
        }

        static private void DrawAvatars(IMagickImage<byte> image, GraphPaintingStrategy strategy, IMagickGeometry avatarsArea, IMagickGeometry avatarSize, float minValue, float maxValue, IEnumerable<(IMagickImage<byte>, string, float)> data)
        {
            (minValue, maxValue) = strategy.SmoothenAxisLimits(minValue, maxValue);

            // Enumerate the items so that we know their count and establish the divisor.
            var datas = data.ToArray();
            var areaWidthDivisor = datas.Length - 1;
            if (areaWidthDivisor < 1)
                areaWidthDivisor = 1;

            // The avatars are drawn centered around the calculated coordinates.
            // By shrinking the drawing area width by AvatarSize/2 from both sides, we avoid drawing out of the grid lines area.
            avatarsArea.X += strategy.AvatarSize / 2;
            avatarsArea.Width -= strategy.AvatarSize;

            var itemSpacingX = avatarsArea.Width / areaWidthDivisor;    // Establish the spacing between centers of drawn avatars
            var x = avatarsArea.X + (itemSpacingX * areaWidthDivisor); // Find out the X coordinate of the rightmost rendered item.
            if (datas.Length == 1)
                x = avatarsArea.X; // .. and override it to the left if it does not have any friends.

            // We will be drawing the avatars from right to left. In case of overlaps, higher-ranked results will be drawn on top.

            var isAbove = datas.Length % 2 == 1; // Determine the initial username location (above/below) so that 1st result has username on top.
            var i = datas.Length;
            var maxUsernameWidth = (int)(1.5 * avatarSize.Width);

            var nicknameDrawable = new Drawables()
                .Font(strategy.UsernameFont)
                .FontPointSize(strategy.UsernameFontSize);

            foreach ((var picture, var username, var value) in datas.Reverse())
            {
                var y = avatarsArea.Y + avatarsArea.Height - (int)(avatarsArea.Height * (value - minValue) / (maxValue - minValue));
                
                image.Composite(picture, x - avatarSize.Width / 2, y - avatarSize.Height / 2, CompositeOperator.Over);

                var nickHeight = nicknameDrawable.FontTypeMetrics(username).TextHeight;

                // Try shrink the username if it does not fit, give up when you reach 5 characters.
                image.DrawEnhancedText(
                    username,
                    Gravity.Center,
                    (int)(x - 0.5 * maxUsernameWidth),
                    isAbove
                        ? (int)(y - (0.6 * avatarSize.Height) - (1.1 * nickHeight))
                        : (int)(y + (0.4 * avatarSize.Height) + (0.4 * nickHeight)),
                    strategy.UsernameForeground,
                    strategy.UsernameFont,
                    strategy.UsernameFontSize,
                    maxUsernameWidth);

                // Draw the percentage now
                var drawable = new Drawables()
                    .Density(100)
                    .Font(strategy.UserValueLabelFont)
                    .FontPointSize(strategy.UserValueLabelFontSize)
                    .FillColor(strategy.UserValueLabelForeground)
                    .TextAlignment(TextAlignment.Center);

                var userValueLabel = strategy.FormatUserValueLabel(value);
                var userValueLabelSize = drawable.FontTypeMetrics(userValueLabel);

                drawable
                    .Text(
                        x,
                        isAbove
                            ? y + (0.4 * avatarSize.Height) + userValueLabelSize.TextHeight
                            : y - (0.6 * avatarSize.Height) - (0.1 * userValueLabelSize.TextHeight),
                        userValueLabel)
                    .Draw(image);

                isAbove = !isAbove;
                x -= itemSpacingX;
                i--;
            }
        }
    }
}
