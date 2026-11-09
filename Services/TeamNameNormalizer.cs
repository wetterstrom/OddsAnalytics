using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public interface ITeamMatcher
{
    Team? FindMatchingTeam(string oddsTeamName, IEnumerable<Team> availableTeams);
}

public class TeamNameNormalizer : ITeamMatcher
{
    private static readonly string[] IgnoredSuffixes =
        { " fc", " afc", " if", " bk", " ik", " hc", " aik", " sk", " hf", " aif", " ff", " boIS" };

    public Team? FindMatchingTeam(string oddsTeamName, IEnumerable<Team> availableTeams)
    {
        if (string.IsNullOrWhiteSpace(oddsTeamName)) return null;

        var teamList = availableTeams.ToList();
        var normalizedTarget = Normalize(oddsTeamName);

        // 1. Exakt matchning på normaliserat namn
        var exact = teamList.FirstOrDefault(t => Normalize(t.Name) == normalizedTarget);
        if (exact != null) return exact;

        // 2. Delsträngsmatchning (t.ex. "Manchester Utd" vs "Manchester United")
        return teamList.FirstOrDefault(t =>
        {
            var candidate = Normalize(t.Name);
            return candidate.Contains(normalizedTarget) || normalizedTarget.Contains(candidate);
        });
    }

    private static string Normalize(string input)
    {
        var lower = RemoveDiacritics(input.ToLowerInvariant().Trim());
        lower = lower.Replace("&", "and").Replace("-", " ");

        foreach (var suffix in IgnoredSuffixes)
        {
            if (lower.EndsWith(suffix))
            {
                lower = lower[..^suffix.Length].Trim();
            }
        }

        return lower;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}