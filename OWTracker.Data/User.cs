using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using OWTracker;

namespace OWTracker.Data
{
    public class User
    {
        public User(long userId, string username, int? competitivePoints, string battleTag, bool editPermissions)
        {
            UserId = userId;
            Username = username;
            EditPermissions = editPermissions;
            CompetitivePoints = competitivePoints;
            BattleTag = battleTag;
        }

        public bool EditPermissions { get; }
        public long UserId { get; }
        public string Username { get; set; }
        public int? CompetitivePoints { get; set; }
        public string BattleTag { get; set; }
    }
}