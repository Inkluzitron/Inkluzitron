using Inkluzitron.Data.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Inkluzitron.Services;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class GraphPaintingService : IDisposable
    {
        private ProfilePictureService ProfilePictureService { get; }

        public Color BackgroundColor { get; init; } = Color.Black;
        public int CategoryBoxPadding { get; init; } = 20;
        public int CategoryBoxHeight { get; init; } = 300;

        public Font GridLinePercentageFont { get; init; } = new Font(SystemFonts.DefaultFont.FontFamily, 20);
        public Brush GridLinePercentageForegroundMajor { get; init; } = new SolidBrush(Color.FromArgb(0x70AAAAAA));
        public Brush GridLinePercentageBackgroundMinor { get; init; } = new SolidBrush(Color.FromArgb(0x32AAAAAA));
        public Pen GridLinePenMinor { get; init; } = new Pen(Color.FromArgb(0x32AAAAAA));
        public Pen GridLinePenMajor { get; init; } = new Pen(Color.FromArgb(0x70AAAAAA));

        public Font CategoryBoxHeadingFont { get; init; } = new Font(SystemFonts.DefaultFont.FontFamily, 20);
        public Brush CategoryBoxBackground { get; init; } = new SolidBrush(Color.FromArgb(0x7F333333));
        public Brush CategoryBoxHeadingForeground { get; init; } = new SolidBrush(Color.FromArgb(0x7FEEEEEE));

        public int AvatarSize { get; init; } = 64;
        public Font UsernameFont { get; init; } = new Font(SystemFonts.DefaultFont.FontFamily, 20);
        public Brush UsernameForeground { get; init; } = new SolidBrush(Color.FromArgb(0x7FFFFFDD));

        public int ColumnCount { get; init; } = 5;

        protected GraphPaintingService(ProfilePictureService profilePictureService)
        {
            ProfilePictureService = profilePictureService;
        }

        public GraphPaintingService(ProfilePictureService profilePictureService, FontService fontService) : this(profilePictureService)
        {
            CategoryBoxHeadingFont?.Dispose();
            GridLinePercentageFont?.Dispose();
            UsernameFont?.Dispose();

            CategoryBoxHeadingFont = new Font(fontService.OpenSansCondensed, 20);
            GridLinePercentageFont = new Font(fontService.OpenSansCondensedLight, 20);
            UsernameFont = new Font(fontService.OpenSansCondensedLight, 20);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            GridLinePercentageFont.Dispose();
            GridLinePercentageForegroundMajor.Dispose();
            GridLinePercentageBackgroundMinor.Dispose();
            GridLinePenMinor.Dispose();
            GridLinePenMajor.Dispose();
            CategoryBoxHeadingFont.Dispose();
            CategoryBoxBackground.Dispose();
            CategoryBoxHeadingForeground.Dispose();
            UsernameFont.Dispose();
            UsernameForeground.Dispose();
        }

        public async Task<Bitmap> DrawAsync(IDictionary<string, List<QuizDoubleItem>> toplistResults, float reportingThreshold)
        {
            // Do not draw empty category boxes, skip categories that have no results.
            foreach (var k in toplistResults.Keys.ToList())
                if (toplistResults[k].Count == 0)
                    toplistResults.Remove(k);

            // Download all needed avatars.
            var avatars = new Dictionary<ulong, Image>();
            var userIds = toplistResults.SelectMany(x => x.Value).Select(x => x.Parent.SubmittedById).Distinct();
            foreach (var userId in userIds)
                avatars[userId] = await ProfilePictureService.GetProfilePictureAsync(userId);

            // Obtain all quiz results to be displayed and sort them by category name.
            var topList = toplistResults.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase).ToList();

            var categoryWidth = (3 + topList.Max(kvp => kvp.Value.Count)) * AvatarSize;
            var categoryHeight = CategoryBoxHeight;
            var columnCount = Math.Min(ColumnCount, toplistResults.Count);
            var rowCount = (int)Math.Ceiling(topList.Count / (1f * columnCount));
            var imageWidth = 2f * CategoryBoxPadding + (columnCount - 1) * CategoryBoxPadding + columnCount * categoryWidth;
            var imageHeight = 2f * CategoryBoxPadding + (rowCount - 1) * CategoryBoxPadding + rowCount * categoryHeight;
            var image = new Bitmap((int)Math.Ceiling(imageWidth), (int)Math.Ceiling(imageHeight));

            using var g = Graphics.FromImage(image);
            g.Clear(BackgroundColor);

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var x = CategoryBoxPadding + columnIndex * (categoryWidth + CategoryBoxPadding);

                for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var y = CategoryBoxPadding + rowIndex * (categoryHeight + CategoryBoxPadding);

                    var i = rowIndex * columnCount + columnIndex;
                    if (i >= topList.Count)
                        continue;

                    var minValueInCategory = Convert.ToSingle(topList[i].Value.Min(v => v.Value));
                    var maxValueInCategory = Convert.ToSingle(topList[i].Value.Max(v => v.Value));

                    var categoryRect = new RectangleF(x, y, categoryWidth, categoryHeight);

                    DrawCategory(
                        g, categoryRect, topList[i].Key,
                        minValueInCategory, maxValueInCategory,
                        out var avatarsRect
                    );

                    DrawAvatars(
                        g, avatarsRect, new Size(AvatarSize, AvatarSize),
                        minValueInCategory,
                        maxValueInCategory,
                        topList[i].Value.Select(
                            x => (
                                avatars[x.Parent.SubmittedById],
                                x.Parent.SubmittedByName,
                                Convert.ToSingle(x.Value)
                            )
                        )
                    );
                }
            }

            return image;
        }

        static private (float, float) SmoothenRange(float minValue, float maxValue)
        {
            minValue -= minValue % 0.1f; // round down to nearest multiple of 0.1
            maxValue += 0.1f - (maxValue % 0.1f); // round up to nearest multiple of 0.9
            return (minValue, maxValue);
        }

        static private float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }

        private void DrawGridLine(Graphics g, RectangleF dst, float y, float value, bool isMajor, float gridLineLabelPadding)
        {
            var pen = isMajor ? GridLinePenMajor : GridLinePenMinor;
            var textForeground = isMajor ? GridLinePercentageForegroundMajor : GridLinePercentageBackgroundMinor;

            g.DrawLine(pen, dst.Left, y, dst.Right, y);

            var str = value.ToString("P0", CultureInfo.InvariantCulture);
            var strSize = g.MeasureString(str, GridLinePercentageFont);
            g.DrawString(str, GridLinePercentageFont, textForeground, dst.Right + gridLineLabelPadding, y - (0.5f * strSize.Height));
        }

        private void DrawCategoryGridLines(Graphics g, RectangleF dst, float minValue, float maxValue, out RectangleF avatarsArea)
        {
            (minValue, maxValue) = SmoothenRange(minValue, maxValue);

            // Determine width of the column of grid line percentage labels.
            var gridLineLabelMaxWidth = new[] {
                g.MeasureString(minValue.ToString("P0", CultureInfo.InvariantCulture), GridLinePercentageFont).Width,
                g.MeasureString(maxValue.ToString("P0", CultureInfo.InvariantCulture), GridLinePercentageFont).Width
            }.Max();

            // Measure how wide is an 'X' and use this value as a spacer for these percentage labels.
            var gridLineLabelPadding = g.MeasureString("X", GridLinePercentageFont).Width;

            // Shrink the area where grid lines so that there's space left for percentage labels.
            dst.Width -= gridLineLabelMaxWidth - gridLineLabelPadding;

            // Determine the displayed ranged and adjust the number of grid lines so that the percentage values are nice.
            var range = Clamp(maxValue - minValue, 0.1f, 1.0f);
            var tenths = (int) Math.Round(range / 0.1f);
            var innerGridLineCount = tenths switch
            {
                1 => 1,
                2 => 3,
                _ => tenths - 1
            };
            var step = range / (innerGridLineCount + 1);

            // Finally, draw the grid lines.
            DrawGridLine(g, dst, dst.Bottom, minValue, true, gridLineLabelPadding);

            for (var i = 1; i <= innerGridLineCount; i++)
            {
                float v = minValue + i * step;
                float y = dst.Bottom - dst.Height * (v - minValue) / (maxValue - minValue);

                DrawGridLine(g, dst, y, v, false, gridLineLabelPadding);
            }

            DrawGridLine(g, dst, dst.Top, maxValue, true, gridLineLabelPadding);

            // Return the area that has been drawn over with grid lines.
            avatarsArea = dst;
        }

        private void DrawCategory(Graphics g, RectangleF dst, string categoryName, float minValue, float maxValue, out RectangleF avatarsArea)
        {
            // Initialize the dimensions of the category box and fill it.
            var graphArea = new RectangleF(dst.X, dst.Y, dst.Width, dst.Height);
            g.FillRectangle(CategoryBoxBackground, graphArea);

            // Measure the category heading, draw it and shrink the graph area so that it does not overlap.
            var headingSize = g.MeasureString(categoryName, CategoryBoxHeadingFont);
            var headingPadding = 0.1f * headingSize.Height;
            var headingHeightWithPadding = 2*headingPadding + headingSize.Height;
            graphArea.Y += headingHeightWithPadding;
            graphArea.Height -= headingHeightWithPadding;

            g.DrawString(
                categoryName, CategoryBoxHeadingFont, CategoryBoxHeadingForeground,
                graphArea.X + 0.5f*graphArea.Width - 0.5f*headingSize.Width,
                graphArea.Top - headingSize.Height - headingPadding
            );

            // Reduce the width & center the graph area so that there is some (= 0.75*AvatarSize) padding left.
            var gridLinesArea = graphArea;
            gridLinesArea.Inflate(-0.75f * AvatarSize, -0.75f * AvatarSize);

            // Draw grid lines over the established area.
            DrawCategoryGridLines(g, gridLinesArea, minValue, maxValue, out avatarsArea);
        }

        private void DrawAvatars(Graphics g, RectangleF avatarsArea, Size avatarSize, float minValue, float maxValue, IEnumerable<(Image, string, float)> data)
        {
            (minValue, maxValue) = SmoothenRange(minValue, maxValue);

            // Enumerate the items so that we know their count and establish the divisor.
            var datas = data.ToArray();
            var areaWidthDivisor = datas.Length - 1;
            if (areaWidthDivisor < 1)
                areaWidthDivisor = 1;

            // The avatars are drawn centered around the calculated coordinates.
            // By shrinking the drawing area width by AvatarSize/2 from both sides, we avoid drawing out of the grid lines area.
            avatarsArea.Inflate(-0.5f * AvatarSize, 0);

            var itemSpacingX = avatarsArea.Width / areaWidthDivisor;    // Establish the spacing between centers of drawn avatars
            var x = avatarsArea.Left + itemSpacingX * areaWidthDivisor; // Find out the X coordinate of the rightmost rendered item.
            if (datas.Length == 1)
                x = avatarsArea.Left; // .. and override it to the left if it does not have any friends.


            // We will be drawing the avatars from right to left. In case of overlaps, higher-ranked results will be drawn on top.


            var isAbove = datas.Length % 2 == 1; // Determine the initial username location (above/below) so that 1st result has username on top.
            var i = datas.Length;
            var maxUsernameWidth = 1.5f * avatarSize.Width;

            foreach ((var picture, var username, var value) in datas.Reverse())
            {
                var y = avatarsArea.Bottom - avatarsArea.Height * (value - minValue) / (maxValue - minValue);

                g.DrawImage(
                    picture,
                    x - 0.5f * avatarSize.Width,
                    y - 0.5f * avatarSize.Height,
                    avatarSize.Width,
                    avatarSize.Height
                );

                // Try shrink the username if it does not fit, give up when you reach 5 characters.
                var un = username;
                var pctgStrSize = g.MeasureString(un, UsernameFont);
                while (un.Length >= 5 && pctgStrSize.Width > maxUsernameWidth) {
                    un = un.Substring(0, un.Length - 1);
                    pctgStrSize = g.MeasureString(un, UsernameFont);
                }

                g.DrawString(
                    un, UsernameFont, UsernameForeground,
                    x - 0.5f * pctgStrSize.Width,
                    isAbove
                      ? y - 0.5f * avatarSize.Height - 1.1f * pctgStrSize.Height
                      : y + 0.5f * avatarSize.Height + 0.1f * pctgStrSize.Height
                );

                isAbove = !isAbove;
                x -= itemSpacingX;
                i--;
            }
        }
    }
}
