using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OWTracker.Data
{
    public class Game
    {
        public long GameID { get; set; }
        public long UserID { get; set; }
        public short Season { get; set; }
        public System.DateTimeOffset Date { get; set; }
        public short SkillRating { get; set; }
        public bool? GameWon { get; set; }
        public string Heroes { get; set; }
        public string Map { get; set; }
        public bool? AttackFirst { get; set; }
        public byte? Rounds { get; set; }
        public string Score { get; set; }
        public int? GameLength { get; set; }
        public byte? GroupSize { get; set; }
        public string Notes { get; set; }
        public Game(long userId, short season, DateTimeOffset date, short skillRating, bool? gameWon, string heroes, string map, bool? attackFirst, byte? rounds, string score, int? gameLength, byte? groupSize, string notes)
        {
            UserID = userId;
            Season = season;
            Date = date;
            SkillRating = skillRating;
            GameWon = gameWon;
            Heroes = heroes;
            Map = map;
            AttackFirst = attackFirst;
            Rounds = rounds;
            Score = score;
            GameLength = gameLength;
            GroupSize = groupSize;
            Notes = notes;
        }

        public Game() { }
        [JsonIgnore]
        public byte? BlueTeamScore => byte.TryParse(Score?.Split('-')[0], out byte b) ? b : (byte?)null;
        [JsonIgnore]
        public byte? RedTeamScore => byte.TryParse(Score?.Split('-')[1], out byte b) ? b : (byte?)null;
        [JsonIgnore]
        public int? Minutes => GameLength / 60;
        [JsonIgnore]
        public int? Seconds => GameLength % 60;

        [JsonIgnore]
        public IEnumerable<string> HeroesList => Heroes?.Split(',').Select(s => s.Trim()).ToArray() ?? new string[0];

        [JsonIgnore]
        public int? SkillRatingDifference { get; set; }

        public override string ToString() => $"{GameID}\t{Season}\t{Date}\t{SkillRating}\t{(GameWon == true ? "WIN" : GameWon == false ? "LOSS" : "DRAW")}\t{(Heroes == null ? "" : string.Join(", ", Heroes))}\t{Map ?? ""}\t{(AttackFirst == true ? "ATTACK" : AttackFirst == false ? "DEFEND" : "")}\t{Rounds?.ToString() ?? ""}\t{Score?.ToString() ?? ""}\t{(GameLength == null ? "" : (Minutes + ":" + Seconds))}\t{GroupSize?.ToString() ?? ""}\t{Notes ?? ""}";

        public string ToTooltipString(bool b) =>
            b ? $"Season: {Season}\nDate: {Date:MMM d}\nSR: {SkillRating}\nResult: {(GameWon == true ? "WIN" : GameWon == false ? "LOSS" : "DRAW")}" : $"Season: {Season}\nDate: {Date:MMM d}\nResult: {(GameWon == true ? "WIN" : GameWon == false ? "LOSS" : "DRAW")}"; //\nHeroes: {(Heroes == null ? "" : string.Join(", ", Heroes))}\nMap: {Map ?? ""}\nStarting Side: {(AttackFirst == true ? "ATTACK" : AttackFirst == false ? "DEFEND" : "")}\nRounds: {Rounds?.ToString() ?? ""}\nScore: {Score?.ToString() ?? ""}\nLength: {(GameLength == null ? "" : (Minutes + ":" + Seconds))}\nGroup Size: {GroupSize?.ToString() ?? ""}";
    }
}