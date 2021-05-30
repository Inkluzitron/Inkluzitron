using Inkluzitron.Models;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace Inkluzitron.Modules.Points
{
    public class PointsGraphPaintingStrategy : GraphPaintingStrategy
    {
        public PointsGraphPaintingStrategy()
            : base(
                  new Font("Comic Sans MS", 15),
                  new Font("Comic Sans MS", 20, FontStyle.Bold),
                  new Font("Comic Sans MS", 20),
                  new Font("Comic Sans MS", 15)
            )
        {
            CategoryBoxHeight = 1024;
        }

        public override int CalculateColumnCount(IDictionary<string, List<GraphItem>> results)
            => 1;

        public override int CalculateRowCount(IDictionary<string, List<GraphItem>> results)
            => 1;

        public override int CalculateGridLineCount(float lowerLimit, float upperLimit)
        {
            var thousands = (int)Math.Ceiling((upperLimit - lowerLimit) / 1000);
            return (thousands * 4) - 1;
        }

        public override float ClampAxisValue(float value)
            => value;

        static private string CreateLabel(float value, string order)
            => $"{value.ToString("F2", CultureInfo.InvariantCulture)}{order}";

        static private string FormatValue(float value)
        {
            const int Million = 1_000_000;
            const int Thousand = 1_000;
            float absVal = Math.Abs(value);

            if (absVal > Million)
                return CreateLabel(value / Million, "m");
            else if (absVal > Thousand)
                return CreateLabel(value / Thousand, "k");
            else
                return value.ToString("F0");
        }

        public override string FormatGridLineValueLabel(float value)
            => FormatValue(value);

        public override string FormatUserValueLabel(float value)
            => FormatValue(value);

        public override (float, float) SmoothenAxisLimits(float minValue, float maxValue)
        {
            minValue -= minValue % 1000f;
            maxValue += 1000f - (maxValue % 1000f);
            return (minValue, maxValue);
        }
    }
}
