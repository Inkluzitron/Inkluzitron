using System.Collections.Generic;
using ImageMagick;
using Inkluzitron.Models;

namespace Inkluzitron.Services
{
    public abstract class GraphPaintingStrategy
    {
        public int ColumnCount { get; init; } = 5;
        public MagickColor BackgroundColor { get; set; } = MagickColors.Black;
        public int CategoryBoxPadding { get; set; } = 20;
        public int CategoryBoxHeight { get; set; } = 350;

        public DrawableFont GridLinePercentageFont { get; set; }
        public double GridLinePercentageFontSize { get; set; } = 20;
        public MagickColor GridLinePercentageForegroundMajor { get; set; } = new MagickColor("#AAAAAA70");
        public MagickColor GridLinePercentageBackgroundMinor { get; set; } = new MagickColor("#AAAAAA32");
        public MagickColor GridLineColorMinor { get; set; } = new MagickColor("#AAAAAA32");
        public MagickColor GridLineColorMajor { get; set; } = new MagickColor("#AAAAAA70");

        public DrawableFont CategoryBoxHeadingFont { get; set; }
        public double CategoryBoxHeadingFontSize { get; set; } = 20;
        public MagickColor CategoryBoxBackground { get; set; } = new MagickColor("#3333337F");
        public MagickColor CategoryBoxHeadingForeground { get; set; } = new MagickColor("#EEEEEE7F");

        public int AvatarSize { get; set; } = 64;
        public DrawableFont UsernameFont { get; set; }
        public double UsernameFontSize { get; set; } = 20;
        public MagickColor UsernameForeground { get; set; } = new MagickColor("#FFFFDD7F");
        public DrawableFont UserValueLabelFont { get; set; }
        public double UserValueLabelFontSize { get; set; } = 20;
        public MagickColor UserValueLabelForeground { get; set; } = new MagickColor("#AAAAAA70");

        public abstract int CalculateColumnCount(IDictionary<string, List<GraphItem>> results);
        public abstract int CalculateRowCount(IDictionary<string, List<GraphItem>> results);
        public abstract (float, float) SmoothenAxisLimits(float minValue, float maxValue);
        public abstract float ClampAxisValue(float value);
        public abstract int CalculateGridLineCount(float lowerLimit, float upperLimit);
        public abstract string FormatGridLineValueLabel(float value);
        public abstract string FormatUserValueLabel(float value);

        protected GraphPaintingStrategy(DrawableFont gridLinePercentrageFont, DrawableFont categoryBoxHeadingFont, DrawableFont usernameFont, DrawableFont avatarPercentageFont)
        {
            GridLinePercentageFont = gridLinePercentrageFont;
            CategoryBoxHeadingFont = categoryBoxHeadingFont;
            UsernameFont = usernameFont;
            UserValueLabelFont = avatarPercentageFont;
        }

        static protected float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;

            return value;
        }
    }
}
