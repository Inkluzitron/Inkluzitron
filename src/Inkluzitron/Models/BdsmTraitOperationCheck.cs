using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Inkluzitron.Models
{
    public class BdsmTraitOperationCheck
    {
        private readonly BdsmTestOrgSettings _settings;
        private readonly BdsmTraitOperationCheckTranslations _translations;
        private readonly Gender _userGender;
        private readonly Gender _targetGender;

        public BdsmTraitOperationCheck(
            BdsmTestOrgSettings settings,
            BdsmTraitOperationCheckTranslations translations,
            Gender userGender,
            Gender targetGender
        )
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _translations = translations ?? throw new ArgumentNullException(nameof(translations));
            _userGender = userGender;
            _targetGender = targetGender;
        }

        public string UserDisplayName { get; set; }
        public string TargetDisplayName { get; set; }
        public BdsmTraitOperationCheckResult Result { get; set; }

        public bool Backfired => Result == BdsmTraitOperationCheckResult.RollFailed;
        public bool CanProceedNormally => !Backfired
            && Result != BdsmTraitOperationCheckResult.TargetDidNotConsent
            && Result != BdsmTraitOperationCheckResult.UserDidNotConsent
            && Result != BdsmTraitOperationCheckResult.UserNegativePoints;

        public List<BdsmTraitOperationFactor> Factors { get; } = new();

        public int RolledValue { get; set; }
        public int RollMaximum { get; set; }
        public int RequiredValue { get; set; }
        public int PointsToSubtract => Backfired ? RequiredValue - RolledValue : 0;

        private double Janchsinus(double score)
            => 1 - Math.Pow(1 - (_settings.JanchsinusCoefficient * Math.Abs(score)), 3);

        private double Contributions => Factors.Count > 0
            ? Factors.Sum(f => f.Contribution)
            : 0;

        private double Weights => Factors.Count > 0
            ? Factors.Sum(f => f.Weight)
            : 1;

        public void Compute()
        {
            // All contributions fall into <-1, 1>
            // All weights fall into <0, 1>
            // The weighted average (=score) therefore also falls into <-1, 1>

            var score = Contributions / Weights;
            if (score >= 0)
            {
                Result = BdsmTraitOperationCheckResult.InCompliance;
                return;
            }

            const int rollMaximum = 100;
            var requiredRollPercentage = Janchsinus(score);
            requiredRollPercentage = Math.Max(requiredRollPercentage, 0);
            requiredRollPercentage = Math.Min(requiredRollPercentage, 1);

            RequiredValue = (int)Math.Ceiling(rollMaximum * requiredRollPercentage);
            RolledValue = ThreadSafeRandom.Next(1, rollMaximum + 1);
            RollMaximum = rollMaximum;

            if (RolledValue >= RequiredValue)
                Result = BdsmTraitOperationCheckResult.RollSucceeded;
            else
                Result = BdsmTraitOperationCheckResult.RollFailed;
        }

        public override string ToString()
            => ToString(false);

        public string ToStringWithTraitInfluenceTable()
            => ToString(true);

        private string ToString(bool printTraitInfluenceTable)
        {
            string format;

            switch (Result)
            {
                case BdsmTraitOperationCheckResult.UserHasNoTest:
                    return string.Format(_translations.MissingTest, UserDisplayName);

                case BdsmTraitOperationCheckResult.TargetHasNoTest:
                    return string.Format(_translations.MissingTest, TargetDisplayName);

                case BdsmTraitOperationCheckResult.Self:
                    return string.Format(_translations.Self, UserDisplayName);

                case BdsmTraitOperationCheckResult.InCompliance:
                    return string.Format(
                        _translations.InCompliance,
                        UserDisplayName, TargetDisplayName,
                        ComposeComputationDetails()
                    );

                case BdsmTraitOperationCheckResult.RollSucceeded:
                    format = _translations.RollSucceeded;
                    break;

                case BdsmTraitOperationCheckResult.RollFailed:
                    format = _translations.RollFailed;
                    break;

                case BdsmTraitOperationCheckResult.UserDidNotConsent:
                    return string.Format(_translations.MissingUserConsent, new FormatByValue(_userGender));

                case BdsmTraitOperationCheckResult.TargetDidNotConsent:
                    return string.Format(_translations.MissingTargetConsent,
                        TargetDisplayName,
                        new FormatByValue(_targetGender));

                case BdsmTraitOperationCheckResult.UserNegativePoints:
                    return _translations.NegativePoints;

                default:
                    return base.ToString();
            }

            return string.Format(
                format,
                UserDisplayName, TargetDisplayName,
                RolledValue, RollMaximum, RequiredValue, PointsToSubtract,
                printTraitInfluenceTable
                    ? "\n\n" + ComposeComputationDetails()
                    : string.Empty,
                new FormatByValue(_userGender),
                new FormatByValue(_targetGender)
            );
        }

        public string ComposeComputationDetails()
        {
            var firstFactor = Factors.FirstOrDefault();
            var orderedFactors = firstFactor?.Contribution <= 0
                ? Factors.OrderBy(f => f.Contribution)
                : Factors.OrderByDescending(f => f.Contribution);

            return "```" + FormatTable(GenerateCells(orderedFactors)) + "```";
        }

        static private string FormatDoubleWithSign(double x)
            => string.Format("{0}{1,5:F3}", x < 0 ? "" : "+", x);

        static private string FormatDoubleWithoutSign(double x)
            => string.Format("{0,5:F3}", x);

        private IEnumerable<IEnumerable<string>> GenerateCells(IEnumerable<BdsmTraitOperationFactor> factors)
        {
            var valueWidth = factors
                .SelectMany(f => f.Values.Values)
                .Max(v => Math.Max(v.User.Length, v.Target.Length));

            foreach (var factor in factors)
            {
                yield return Enumerable.Empty<string>();

                var rowNumber = 0;
                foreach (var item in factor.Values)
                {
                    yield return Enumerable.Empty<string>()
                        .Append(item.Key)
                        .Append($"{item.Value.User.PadLeft(valueWidth)} vs. {item.Value.Target}")
                        .Concat(
                            rowNumber++ switch
                            {
                                0 => Enumerable.Empty<string>().Append("s = " + FormatDoubleWithSign(factor.Score)),
                                1 => Enumerable.Empty<string>().Append("w =  " + FormatDoubleWithoutSign(factor.Weight)),
                                _ => Enumerable.Empty<string>()
                            }
                        );
                }
            }

            var result = Contributions / Weights;

            yield return Enumerable.Empty<string>();

            yield return Enumerable.Empty<string>()
                .Append("∑(s*w)")
                .Append(FormatDoubleWithSign(Contributions));

            yield return Enumerable.Empty<string>()
                .Append("∑(w)")
                .Append(FormatDoubleWithSign(Weights));

            yield return Enumerable.Empty<string>()
                .Append("x̄")
                .Append(FormatDoubleWithSign(result));

            if (result < 0)
                yield return Enumerable.Empty<string>()
                    .Append(string.Format(
                        "{0}({1})",
                        nameof(Janchsinus),
                        FormatDoubleWithoutSign(Math.Abs(result))
                    ))
                    .Append(
                        Janchsinus(result).ToIntPercentage().ToString()
                    );
        }

        static private string FormatTable(IEnumerable<IEnumerable<string>> cells)
        {
            const string columnPadding = "  ";

            var rows = cells.Select(c => c.ToList()).ToList();
            var columnCount = rows.Max(rowCells => rowCells.Count);
            var columnWidths = Enumerable.Range(0, columnCount).Select(
                columnIndex => rows.Max(
                    rowCells => columnIndex >= rowCells.Count
                        ? 0
                        : new StringInfo(rowCells[columnIndex]).LengthInTextElements
                )
            ).ToList();

            var builder = new StringBuilder();

            foreach (var row in rows)
            {
                for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
                {
                    if (columnIndex > 0)
                        builder.Append(columnPadding);

                    builder.Append(row[columnIndex]);
                    builder.Append(new string(' ', columnWidths[columnIndex] - new StringInfo(row[columnIndex]).LengthInTextElements));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
