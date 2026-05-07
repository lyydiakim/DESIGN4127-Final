using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Tracks per-player end-of-game behavior summaries for the final results screen.
/// </summary>
public static class PlayerGameStats
{
    public struct PlayerReport
    {
        public int RentContribution;
        public int MoneySpent;
        public int EarnedMoney;
        public int EarnedEnergy;
        public int EarnedNetwork;
        public int WorkedFactoryCount;
        public int WorkedArtStoreCount;
        public int WorkedDeliCount;
        public int WorkedGroceryCount;
    }

    private sealed class PlayerStats
    {
        public readonly Dictionary<string, int> StationUses = new Dictionary<string, int>();
        public readonly Dictionary<string, int> StationMoneySpent = new Dictionary<string, int>();
        public readonly Dictionary<string, int> ResourcesEarned = new Dictionary<string, int>();
        public int TotalMoneySpent;
        public int RentContribution;
        public int TotalTransactions;
    }

    private static readonly PlayerStats[] _players = new PlayerStats[4];

    private static PlayerStats EnsurePlayer(int playerIndex)
    {
        int i = Mathf.Clamp(playerIndex, 0, 3);
        if (_players[i] == null) _players[i] = new PlayerStats();
        return _players[i];
    }

    private static void AddToDict(Dictionary<string, int> dict, string key, int amount)
    {
        if (amount == 0) return;
        if (string.IsNullOrWhiteSpace(key)) key = "Unknown";
        dict.TryGetValue(key, out int existing);
        dict[key] = existing + amount;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < _players.Length; i++)
            _players[i] = null;
    }

    public static void RecordTransaction(
        playerController pc,
        string stationName,
        List<ResourceCost> costs,
        List<ResourceCost> rewards,
        bool isRentPayment = false,
        int rentContributionValue = 0)
    {
        if (pc == null) return;
        int i = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        var p = EnsurePlayer(i);

        p.TotalTransactions++;
        AddToDict(p.StationUses, stationName, 1);

        if (costs != null)
        {
            foreach (var c in costs)
            {
                if (c?.resource == null || c.amount <= 0) continue;
                string resourceName = c.resource.name;
                if (resourceName == "Money")
                {
                    p.TotalMoneySpent += c.amount;
                    AddToDict(p.StationMoneySpent, stationName, c.amount);
                    if (isRentPayment) p.RentContribution += c.amount;
                }
            }
        }

        if (rewards != null)
        {
            foreach (var r in rewards)
            {
                if (r?.resource == null || r.amount <= 0) continue;
                AddToDict(p.ResourcesEarned, r.resource.name, r.amount);
            }
        }

        if (rentContributionValue > 0)
            p.RentContribution += rentContributionValue;
    }

    private static bool HasAnyData()
    {
        for (int i = 0; i < _players.Length; i++)
        {
            if (_players[i] != null && _players[i].TotalTransactions > 0)
                return true;
        }
        return false;
    }

    public static string BuildResultsRichText(bool teamPaidRentOnTime)
    {
        var sb = new StringBuilder(1024);
        sb.AppendLine("<size=115%><b>Game Result</b></size>");
        sb.AppendLine(teamPaidRentOnTime
            ? "<b>Team paid rent on time:</b> YES"
            : "<b>Team paid rent on time:</b> NO");
        sb.AppendLine();

        if (!HasAnyData())
        {
            sb.AppendLine("<i>No player activity data recorded.</i>");
            return sb.ToString();
        }

        for (int i = 0; i < _players.Length; i++)
        {
            var p = _players[i];
            sb.AppendLine($"<b>Player {i + 1}</b>");
            if (p == null || p.TotalTransactions == 0)
            {
                sb.AppendLine("No recorded station transactions.");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"Rent contribution: {p.RentContribution}");
            sb.AppendLine($"Total money spent: {p.TotalMoneySpent}");

            if (p.StationUses.Count > 0)
            {
                sb.AppendLine("Worked / used stations:");
                foreach (var kv in p.StationUses)
                    sb.AppendLine($"- {kv.Key}: {kv.Value}x");
            }

            if (p.StationMoneySpent.Count > 0)
            {
                sb.AppendLine("Money spent by station:");
                foreach (var kv in p.StationMoneySpent)
                    sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }

            if (p.ResourcesEarned.Count > 0)
            {
                sb.AppendLine("Resources earned:");
                foreach (var kv in p.ResourcesEarned)
                    sb.AppendLine($"- {kv.Key}: +{kv.Value}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("<size=90%><i>Press X to restart</i></size>");
        return sb.ToString();
    }

    public static PlayerReport GetReport(int playerIndex)
    {
        var report = new PlayerReport();
        int i = Mathf.Clamp(playerIndex, 0, 3);
        var p = _players[i];
        if (p == null) return report;

        report.RentContribution = p.RentContribution;
        report.MoneySpent = p.TotalMoneySpent;
        report.EarnedMoney = GetFromDict(p.ResourcesEarned, "Money");
        report.EarnedEnergy = GetFromDict(p.ResourcesEarned, "Energy");
        report.EarnedNetwork = GetFromDict(p.ResourcesEarned, "Network");
        report.WorkedFactoryCount = GetStationCount(p.StationUses, "factory");
        report.WorkedArtStoreCount = GetStationCount(p.StationUses, "art store");
        report.WorkedDeliCount = GetStationCount(p.StationUses, "deli");
        report.WorkedGroceryCount = GetStationCount(p.StationUses, "big grocery", "grocery");
        return report;
    }

    private static int GetFromDict(Dictionary<string, int> dict, string key)
    {
        if (dict == null || string.IsNullOrEmpty(key)) return 0;
        return dict.TryGetValue(key, out int v) ? v : 0;
    }

    private static int GetStationCount(Dictionary<string, int> dict, params string[] keyParts)
    {
        if (dict == null || keyParts == null || keyParts.Length == 0) return 0;
        int total = 0;
        foreach (var kv in dict)
        {
            string name = kv.Key.ToLowerInvariant();
            foreach (var part in keyParts)
            {
                if (!string.IsNullOrWhiteSpace(part) && name.Contains(part))
                {
                    total += kv.Value;
                    break;
                }
            }
        }
        return total;
    }
}
