using Inkluzitron.Models;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class BdsmGraphPaintingStrategy : GraphPaintingStrategy
    {
        public BdsmGraphPaintingStrategy(FontService fontService)
            : base(
                  new Font(fontService.OpenSansCondensedLight, 20),
                  new Font(fontService.OpenSansCondensed, 20, FontStyle.Bold),
                  new Font(fontService.OpenSansCondensedLight, 20),
                  new Font(fontService.OpenSansCondensedLight, 20)
            )
        {
        }

        public override int CalculateColumnCount(IDictionary<string, List<GraphItem>> results)
            => Math.Min(ColumnCount, results.Count);

        public override int CalculateGridLineCount(float lowerLimit, float upperLimit)
        {
            var range = Clamp(upperLimit - lowerLimit, 0.1f, 1.0f);
            var tenths = (int)Math.Round(range / 0.1f);
            return tenths switch
            {
                1 => 1,
                2 => 3,
                _ => tenths - 1
            };
        }

        public override int CalculateRowCount(IDictionary<string, List<GraphItem>> results)
            => (int)Math.Ceiling(results.Count / (1f * CalculateColumnCount(results)));

        public override float ClampAxisValue(float value)
            => Clamp(value, 0f, 1f);

        public override string FormatGridLineValueLabel(float value)
            => value.ToString("P0", CultureInfo.InvariantCulture);

        public override string FormatUserValueLabel(float value)
            => (100 * value).ToString("N0");

        public override (float, float) SmoothenAxisLimits(float minValue, float maxValue)
        {
            minValue -= minValue % 0.1f; // round down to nearest multiple of 0.1
            maxValue += 0.1f - (maxValue % 0.1f); // round up to nearest multiple of 0.9
            return (minValue, maxValue);
        }
    }
}
