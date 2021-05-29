using System;
using System.Collections.Generic;
using System.Drawing;
using Inkluzitron.Models;

namespace Inkluzitron.Services
{
    public abstract class GraphPaintingStrategy : IDisposable
    {
        private bool _disposed;

        public int ColumnCount { get; init; } = 5;
        public Color BackgroundColor { get; set; } = Color.Black;
        public int CategoryBoxPadding { get; set; } = 20;
        public int CategoryBoxHeight { get; set; } = 350;

        public Font GridLinePercentageFont { get; set; }
        public Brush GridLinePercentageForegroundMajor { get; set; } = new SolidBrush(Color.FromArgb(0x70AAAAAA));
        public Brush GridLinePercentageBackgroundMinor { get; set; } = new SolidBrush(Color.FromArgb(0x32AAAAAA));
        public Pen GridLinePenMinor { get; set; } = new Pen(Color.FromArgb(0x32AAAAAA));
        public Pen GridLinePenMajor { get; set; } = new Pen(Color.FromArgb(0x70AAAAAA));

        public Font CategoryBoxHeadingFont { get; set; }
        public Brush CategoryBoxBackground { get; set; } = new SolidBrush(Color.FromArgb(0x7F333333));
        public Brush CategoryBoxHeadingForeground { get; set; } = new SolidBrush(Color.FromArgb(0x7FEEEEEE));

        public int AvatarSize { get; set; } = 64;
        public Font UsernameFont { get; set; }
        public Brush UsernameForeground { get; set; } = new SolidBrush(Color.FromArgb(0x7FFFFFDD));
        public Font UserValueLabelFont { get; set; }
        public Brush UserValueLabelForeground { get; set; } = new SolidBrush(Color.FromArgb(0x70AAAAAA));

        public abstract int CalculateColumnCount(IDictionary<string, List<GraphItem>> results);
        public abstract int CalculateRowCount(IDictionary<string, List<GraphItem>> results);
        public abstract (float, float) SmoothenAxisLimits(float minValue, float maxValue);
        public abstract float ClampAxisValue(float value);
        public abstract int CalculateGridLineCount(float lowerLimit, float upperLimit);
        public abstract string FormatGridLineValueLabel(float value);
        public abstract string FormatUserValueLabel(float value);

        protected GraphPaintingStrategy(Font gridLinePercentrageFont, Font categoryBoxHeadingFont, Font usernameFont, Font avatarPercentageFont)
        {
            GridLinePercentageFont = gridLinePercentrageFont;
            CategoryBoxHeadingFont = categoryBoxHeadingFont;
            UsernameFont = usernameFont;
            UserValueLabelFont = avatarPercentageFont;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                GridLinePercentageFont?.Dispose();
                GridLinePercentageForegroundMajor?.Dispose();
                GridLinePercentageBackgroundMinor?.Dispose();
                GridLinePenMinor?.Dispose();
                GridLinePenMajor?.Dispose();
                CategoryBoxHeadingFont?.Dispose();
                CategoryBoxBackground?.Dispose();
                CategoryBoxHeadingForeground?.Dispose();
                UsernameFont?.Dispose();
                UsernameForeground?.Dispose();
                UserValueLabelFont?.Dispose();
                UserValueLabelForeground?.Dispose();
            }

            _disposed = true;
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
