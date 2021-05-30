using Inkluzitron.Data.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Discord.WebSocket;
using Inkluzitron.Utilities;
using Inkluzitron.Extensions;
using Inkluzitron.Models;

namespace Inkluzitron.Services
{
    public sealed class GraphPaintingService
    {
        private ImagesService ImagesService { get; }

        public GraphPaintingService(ImagesService imagesService)
            => ImagesService = imagesService;

        public async Task<Bitmap> DrawAsync(SocketGuild guild, GraphPaintingStrategy strategy, IDictionary<string, IReadOnlyList<GraphItem>> toplistResults)
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
                if (toplistResults[k].Count == 0)
                    toplistResults.Remove(k);
            }

            // Download all needed avatars.
            using var avatars = new ValuesDisposingDictionary<ulong, Image>();

            foreach (var userId in toplistResults.SelectMany(x => x.Value).Select(x => x.UserId).Distinct())
            {
                using var rawAvatar = await ImagesService.GetAvatarAsync(guild, userId);
                using var rounded = rawAvatar.Frames[0].RoundImage();

                avatars[userId] = rounded.ResizeImage(ImagesService.DefaultAvatarSize);
            }

            // Obtain all quiz results to be displayed and sort them by category name.
            var topList = toplistResults.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase).ToList();

            var categoryWidth = (3 + topList.Max(kvp => kvp.Value.Count)) * strategy.AvatarSize;
            var categoryHeight = strategy.CategoryBoxHeight;
            var columnCount = Math.Min(strategy.ColumnCount, toplistResults.Count);
            var rowCount = (int)Math.Ceiling(topList.Count / (1f * columnCount));
            var imageWidth = (2f * strategy.CategoryBoxPadding) + ((columnCount - 1) * strategy.CategoryBoxPadding) + (columnCount * categoryWidth);
            var imageHeight = (2f * strategy.CategoryBoxPadding) + ((rowCount - 1) * strategy.CategoryBoxPadding) + (rowCount * categoryHeight);
            var image = new Bitmap((int)Math.Ceiling(imageWidth), (int)Math.Ceiling(imageHeight));

            using var g = Graphics.FromImage(image);
            g.Clear(strategy.BackgroundColor);

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

                    var categoryRect = new RectangleF(x, y, categoryWidth, categoryHeight);

                    DrawCategory(
                        g, strategy, categoryRect, topList[i].Key,
                        minValueInCategory, maxValueInCategory,
                        out var avatarsRect
                    );

                    DrawAvatars(
                        g, strategy, avatarsRect, new Size(strategy.AvatarSize, strategy.AvatarSize),
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

        static private void DrawGridLine(Graphics g, GraphPaintingStrategy strategy, RectangleF dst, float y, float value, bool isMajor, float gridLineLabelPadding)
        {
            var pen = isMajor ? strategy.GridLinePenMajor : strategy.GridLinePenMinor;
            var textForeground = isMajor ? strategy.GridLinePercentageForegroundMajor : strategy.GridLinePercentageBackgroundMinor;

            g.DrawLine(pen, dst.Left, y, dst.Right, y);

            var str = strategy.FormatGridLineValueLabel(value);
            var strSize = g.MeasureString(str, strategy.GridLinePercentageFont);
            g.DrawString(str, strategy.GridLinePercentageFont, textForeground, dst.Right + gridLineLabelPadding, y - (0.5f * strSize.Height));
        }

        static private void DrawCategoryGridLines(Graphics g, GraphPaintingStrategy strategy, RectangleF dst, float minValue, float maxValue, out RectangleF avatarsArea)
        {
            (minValue, maxValue) = strategy.SmoothenAxisLimits(minValue, maxValue);

            // Determine width of the column of grid line percentage labels.
            var gridLineLabelMaxWidth = new[] {
                g.MeasureString(strategy.FormatGridLineValueLabel(minValue), strategy.GridLinePercentageFont).Width,
                g.MeasureString(strategy.FormatGridLineValueLabel(maxValue), strategy.GridLinePercentageFont).Width
            }.Max();

            // Measure how wide is an 'X' and use this value as a spacer for these percentage labels.
            var gridLineLabelPadding = g.MeasureString("X", strategy.GridLinePercentageFont).Width;

            // Shrink the area where grid lines so that there's space left for percentage labels.
            dst.Width -= gridLineLabelMaxWidth - gridLineLabelPadding;

            // Determine the displayed ranged and adjust the number of grid lines so that the percentage values are nice.
            var innerGridLineCount = strategy.CalculateGridLineCount(minValue, maxValue);
            var step = (maxValue - minValue) / (innerGridLineCount + 1);

            // Finally, draw the grid lines.
            DrawGridLine(g, strategy, dst, dst.Bottom, minValue, true, gridLineLabelPadding);

            for (var i = 1; i <= innerGridLineCount; i++)
            {
                float v = minValue + (i * step);
                float y = dst.Bottom - (dst.Height * (v - minValue) / (maxValue - minValue));

                DrawGridLine(g, strategy, dst, y, v, false, gridLineLabelPadding);
            }

            DrawGridLine(g, strategy, dst, dst.Top, maxValue, true, gridLineLabelPadding);

            // Return the area that has been drawn over with grid lines.
            avatarsArea = dst;
        }

        static private void DrawCategory(Graphics g, GraphPaintingStrategy strategy, RectangleF dst, string categoryName, float minValue, float maxValue, out RectangleF avatarsArea)
        {
            // Initialize the dimensions of the category box and fill it.
            var graphArea = new RectangleF(dst.X, dst.Y, dst.Width, dst.Height);
            g.FillRectangle(strategy.CategoryBoxBackground, graphArea);

            // Measure the category heading, draw it and shrink the graph area so that it does not overlap.
            var headingSize = g.MeasureString(categoryName, strategy.CategoryBoxHeadingFont);
            var headingPadding = 0.2f * headingSize.Height;
            var headingHeightWithPadding = (2 * headingPadding) + headingSize.Height;
            graphArea.Y += headingHeightWithPadding;
            graphArea.Height -= headingHeightWithPadding;

            g.DrawString(
                categoryName, strategy.CategoryBoxHeadingFont, strategy.CategoryBoxHeadingForeground,
                graphArea.X + (0.5f * graphArea.Width) - (0.5f * headingSize.Width),
                graphArea.Top - headingSize.Height - headingPadding
            );

            // Reduce the width & center the graph area so that there is some (= 0.75*AvatarSize) padding left.
            var gridLinesArea = graphArea;
            gridLinesArea.Inflate(-0.75f * strategy.AvatarSize, (-0.75f * strategy.AvatarSize) - (0.50f * headingSize.Height));

            // Draw grid lines over the established area.
            DrawCategoryGridLines(g, strategy, gridLinesArea, minValue, maxValue, out avatarsArea);
        }

        static private void DrawAvatars(Graphics g, GraphPaintingStrategy strategy, RectangleF avatarsArea, Size avatarSize, float minValue, float maxValue, IEnumerable<(Image, string, float)> data)
        {
            (minValue, maxValue) = strategy.SmoothenAxisLimits(minValue, maxValue);

            // Enumerate the items so that we know their count and establish the divisor.
            var datas = data.ToArray();
            var areaWidthDivisor = datas.Length - 1;
            if (areaWidthDivisor < 1)
                areaWidthDivisor = 1;

            // The avatars are drawn centered around the calculated coordinates.
            // By shrinking the drawing area width by AvatarSize/2 from both sides, we avoid drawing out of the grid lines area.
            avatarsArea.Inflate(-0.5f * strategy.AvatarSize, 0);

            var itemSpacingX = avatarsArea.Width / areaWidthDivisor;    // Establish the spacing between centers of drawn avatars
            var x = avatarsArea.Left + (itemSpacingX * areaWidthDivisor); // Find out the X coordinate of the rightmost rendered item.
            if (datas.Length == 1)
                x = avatarsArea.Left; // .. and override it to the left if it does not have any friends.

            // We will be drawing the avatars from right to left. In case of overlaps, higher-ranked results will be drawn on top.

            var isAbove = datas.Length % 2 == 1; // Determine the initial username location (above/below) so that 1st result has username on top.
            var i = datas.Length;
            var maxUsernameWidth = 1.5f * avatarSize.Width;

            foreach ((var picture, var username, var value) in datas.Reverse())
            {
                var y = avatarsArea.Bottom - (avatarsArea.Height * (value - minValue) / (maxValue - minValue));

                g.DrawImage(
                    picture,
                    x - (0.5f * avatarSize.Width),
                    y - (0.5f * avatarSize.Height),
                    avatarSize.Width,
                    avatarSize.Height
                );

                // Try shrink the username if it does not fit, give up when you reach 5 characters.
                var un = username;
                var unStrSize = g.MeasureAndShrinkText(ref un, strategy.UsernameFont, maxUsernameWidth);

                g.DrawString(
                    un, strategy.UsernameFont, strategy.UsernameForeground,
                    x - (0.5f * unStrSize.Width),
                    isAbove
                      ? y - (0.5f * avatarSize.Height) - (1.1f * unStrSize.Height)
                      : y + (0.5f * avatarSize.Height) + (0.1f * unStrSize.Height)
                );

                // Draw the percentage now
                var userValueLabel = strategy.FormatUserValueLabel(value);
                var userValueLabelSize = g.MeasureString(userValueLabel, strategy.UserValueLabelFont);
                g.DrawString(
                    userValueLabel, strategy.UserValueLabelFont, strategy.UserValueLabelForeground,
                    x - (0.5f * userValueLabelSize.Width),
                    isAbove
                      ? y + (0.5f * avatarSize.Height) + (0.1f * userValueLabelSize.Height)
                      : y - (0.5f * avatarSize.Height) - (1.1f * userValueLabelSize.Height)
                );

                isAbove = !isAbove;
                x -= itemSpacingX;
                i--;
            }
        }
    }
}
