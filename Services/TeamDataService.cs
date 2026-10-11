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
        new("Newcastle", "Premier League", 2.20, 1.15, 1.45, 1.55), 

        // SHL (14 lag)
        new Team("Brynäs IF", "SHL", 3.1, 2.3, 2.7, 2.6),
        new Team("Färjestad BK", "SHL", 3.3, 2.4, 2.8, 2.5),
        new Team("Frölunda HC", "SHL", 3.0, 2.2, 2.6, 2.4),
        new Team("Luleå HF", "SHL", 2.9, 2.3, 2.5, 2.5),
        new Team("Skellefteå AIK", "SHL", 3.1, 2.4, 2.7, 2.6),
        new Team("Rögle BK", "SHL", 2.8, 2.6, 2.4, 2.8),
        new Team("Timrå IK", "SHL", 2.9, 2.7, 2.5, 2.8),
        new Team("Växjö Lakers", "SHL", 2.7, 2.1, 2.4, 2.3),
        new Team("Örebro HK", "SHL", 2.6, 2.6, 2.3, 2.7),
        new Team("Linköping HC", "SHL", 2.8, 2.9, 2.4, 3.0),
        new Team("HV71", "SHL", 2.6, 3.0, 2.3, 3.1),
        new Team("Malmö Redhawks", "SHL", 2.6, 2.9, 2.3, 3.0),
        new Team("Modo Hockey", "SHL", 2.5, 3.1, 2.2, 3.3),
        new Team("Djurgårdens IF", "SHL", 2.9, 2.7, 2.5, 2.9),

        // HockeyAllsvenskan (14 lag)
        new Team("Leksands IF", "HockeyAllsvenskan", 3.4, 2.3, 2.9, 2.5),
        new Team("AIK", "HockeyAllsvenskan", 3.2, 2.5, 2.8, 2.6),
        new Team("BIK Karlskoga", "HockeyAllsvenskan", 3.1, 2.5, 2.7, 2.7),
        new Team("IF Björklöven", "HockeyAllsvenskan", 3.5, 2.6, 3.0, 2.7),
        new Team("Mora IK", "HockeyAllsvenskan", 3.1, 2.6, 2.7, 2.8),
        new Team("Södertälje SK", "HockeyAllsvenskan", 3.3, 2.4, 2.8, 2.5),
        new Team("IK Oskarshamn", "HockeyAllsvenskan", 3.2, 2.7, 2.7, 2.9),
        new Team("Kalmar HC", "HockeyAllsvenskan", 3.0, 2.9, 2.5, 3.0),
        new Team("Nybro Vikings IF", "HockeyAllsvenskan", 2.8, 2.8, 2.4, 3.0),
        new Team("Västerås IK", "HockeyAllsvenskan", 2.8, 3.0, 2.4, 3.1),
        new Team("Almtuna IS", "HockeyAllsvenskan", 2.5, 3.0, 2.2, 3.2),
        new Team("Tingsryds AIF", "HockeyAllsvenskan", 2.4, 3.2, 2.1, 3.4),
        new Team("Östersunds IK", "HockeyAllsvenskan", 2.3, 3.3, 2.0, 3.5),
        new Team("Vimmerby HC", "HockeyAllsvenskan", 2.3, 3.3, 2.0, 3.4),

        // NHL (Exempel på topplag/standardlag för matchning)
        new Team("Washington Capitals", "NHL", 3.2, 2.7, 2.8, 2.9),
        new Team("Seattle Kraken", "NHL", 2.9, 2.9, 2.5, 3.1),
        new Team("New York Rangers", "NHL", 3.3, 2.5, 2.9, 2.7),
        new Team("Vancouver Canucks", "NHL", 3.2, 2.6, 2.8, 2.8),
        new Team("Colorado Avalanche", "NHL", 3.4, 2.7, 3.0, 2.9),
        new Team("Edmonton Oilers", "NHL", 3.5, 2.8, 3.1, 3.0),
        new Team("Florida Panthers", "NHL", 3.3, 2.5, 2.9, 2.6),
        new Team("Dallas Stars", "NHL", 3.2, 2.5, 2.8, 2.7),
        new Team("Boston Bruins", "NHL", 3.1, 2.6, 2.7, 2.8),
        new Team("Toronto Maple Leafs", "NHL", 3.3, 2.8, 2.9, 2.9)
            };
}
