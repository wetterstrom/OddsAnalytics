using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class TeamDataService
{
    public List<Team> Teams { get; } = new()
    {
        // Allsvenskan
        new("Malmö FF", "Allsvenskan", 2.25, 0.75, 1.85, 0.95),
        new("Djurgårdens IF", "Allsvenskan", 1.95, 0.90, 1.45, 1.05),
        new("Hammarby IF", "Allsvenskan", 2.05, 0.85, 1.35, 1.20),
        new("AIK", "Allsvenskan", 1.65, 0.90, 1.20, 1.10),
        new("IF Elfsborg", "Allsvenskan", 1.85, 1.10, 1.30, 1.35),
        new("BK Häcken", "Allsvenskan", 2.15, 1.40, 1.55, 1.50),
        new("IFK Göteborg", "Allsvenskan", 1.30, 1.35, 1.10, 1.45),
        new("IK Sirius", "Allsvenskan", 1.55, 1.35, 1.25, 1.50),

        // Premier League
        new("Manchester City", "Premier League", 2.65, 0.85, 2.10, 0.95),
        new("Arsenal", "Premier League", 2.45, 0.70, 1.90, 0.85),
        new("Liverpool", "Premier League", 2.55, 0.90, 2.05, 1.15),
        new("Aston Villa", "Premier League", 2.10, 1.20, 1.55, 1.40),
        new("Chelsea", "Premier League", 1.95, 1.25, 1.50, 1.40),
        new("Tottenham", "Premier League", 2.10, 1.35, 1.60, 1.55),
        new("Newcastle", "Premier League", 2.20, 1.15, 1.45, 1.55)
    };
}
