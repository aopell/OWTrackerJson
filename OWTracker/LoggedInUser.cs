using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWTracker.Data;

namespace OWTracker
{
    public class LoggedInUser
    {
        private User User;
        private GameCollection allGames;

        public string Username => User.Username;
        public string BattleTag => User.BattleTag;
        public long UserId => User.UserId;
        public bool EditPermissions => User.EditPermissions;
        public int? CompetitivePoints => User.CompetitivePoints;

        public LoggedInUser(User user)
        {
            User = user;
            RefreshGames();
        }

        public static async Task<LoggedInUser> Login(string username, string password)
        {
            var u = await Config.DataSource.LoginAsync(username, password);
            return u == null ? null : new LoggedInUser(u);
        }

        public static async Task<LoggedInUser> Login(string username)
        {
            var u = await Config.DataSource.GetReadOnlyUserInfoAsync(username);
            return u == null ? null : new LoggedInUser(u);
        }

        public static async Task<LoggedInUser> Register(string username, string password)
        {
            var u = await Config.DataSource.RegisterAsync(username, password);
            return u == null ? null : new LoggedInUser(u);
        }

        public Game MostRecentGame => allGames.Games.FirstOrDefault();

        private void RefreshGames() => allGames = Config.DataSource.GetAllGames(User.UserId);

        public async Task RefreshGamesAsync() => allGames = await Config.DataSource.GetAllGamesAsync(User.UserId);

        public GameCollection GetGamesBySeason(short seasonNumber) => allGames.WhereSeasonIs(seasonNumber);

        public GameCollection GetRecentGames(int count, bool desc = true) => GetGames(desc, count: count);

        public GameCollection GetAllGames(bool desc = true) => desc ? allGames.OrderDescending() : allGames.OrderAscending();

        public Game GetGameById(long id) => allGames.Games.FirstOrDefault(x => x.GameID == id);

        public GameCollection GetGames(
            bool desc = true,
            short? season = null,
            short? gameWon = -1,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            short? skillRatingMin = null,
            short? skillRatingMax = null,
            List<string> heroes = null,
            string map = null,
            bool? attackFirst = null,
            byte? rounds = null,
            byte? bscore = null,
            byte? rscore = null,
            int? gameLengthMin = null,
            int? gameLengthMax = null,
            byte? groupSize = null,
            int? count = null)
            => allGames.PerformQuery(
            desc,
            season,
            gameWon,
            startDate,
            endDate,
            skillRatingMin,
            skillRatingMax,
            heroes,
            map,
            attackFirst,
            rounds,
            bscore,
            rscore,
            gameLengthMin,
            gameLengthMax,
            groupSize,
            count);

        public async Task UpdateBattleTag(string battleTag) => await Config.DataSource.UpdateUserAsync(User.UserId, User.Username, User.CompetitivePoints, battleTag);

        public async Task UpdateCompetitivePoints(int competitivePoints)
        {
            await Config.DataSource.UpdateUserAsync(User.UserId, User.Username, competitivePoints, User.BattleTag);
            User.CompetitivePoints = competitivePoints;
        }

        public async Task<bool> ChangeUsername(string username)
        {
            if (!string.IsNullOrWhiteSpace(username) && username.Length >= 3 && username.Length <= 50 && Config.DataSource.GetReadOnlyUserInfo(username) == null)
            {
                await Config.DataSource.UpdateUserAsync(User.UserId, username, User.CompetitivePoints, User.BattleTag);
                User.Username = username;
                return true;
            }

            return false;
        }

        public async Task<bool> ChangePassword(string oldPassword, string newPassword) => await Config.DataSource.ChangePasswordAsync(User.UserId, User.Username, oldPassword, newPassword);

        public async Task AddGame(Game g)
        {
            await Config.DataSource.AddGameAsync(User.UserId, g);

            if (User.CompetitivePoints < 6000)
            {
                int competitivePointsDelta = g.GameWon == true ? 10 : g.GameWon == null ? 3 : 0;
                await Config.DataSource.UpdateUserAsync(User.UserId, User.Username, User.CompetitivePoints + competitivePointsDelta > 6000 ? 6000 : User.CompetitivePoints + competitivePointsDelta, User.BattleTag);
                User.CompetitivePoints = User.CompetitivePoints + competitivePointsDelta > 6000 ? 6000 : User.CompetitivePoints + competitivePointsDelta;
            }
            await RefreshGamesAsync();
        }

        public async Task UpdateGame(Game g)
        {
            await Config.DataSource.UpdateGameAsync(g);
            await RefreshGamesAsync();
        }

        public async Task DeleteGame(long gameId)
        {
            await Config.DataSource.DeleteGameAsync(gameId, User.UserId);
            await RefreshGamesAsync();
        }
    }
}
