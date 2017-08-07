using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OWTracker.Data
{
    [LocalDataProvider]
    public class JsonDataProvider : IAsyncDataProvider
    {
        private readonly string usersFilePath;
        private readonly string gamesFilePath;

        public JsonDataProvider(string storageDirectory)
        {
            usersFilePath = storageDirectory + "\\users.json";
            gamesFilePath = storageDirectory + "\\games.json";
        }

        public User Login(string username, string password)
        {
            IEnumerable<User> users = LoadUsers();
            User user = users?.FirstOrDefault(x => x.Username == username);
            if (user == null) throw new ArgumentException("Invalid username");
            return user;
        }

        public User GetReadOnlyUserInfo(string username) => Login(username, null);

        public User Register(string username, string password)
        {
            var users = LoadUsers()?.ToList() ?? new List<User>();
            if (users.Any(x => x.Username == username)) return null;

            // Find unused user ID
            int id = Enumerable.Range(0, int.MaxValue).Except(users.Select(x => (int)x.UserId)).First();

            var user = new User(id, username, 0, null, true);
            users.Add(user);
            SaveUsers(users);

            return user;
        }

        public GameCollection GetAllGames(long userId, bool desc = true) => new GameCollection(LoadGames()?.Where(x => x.UserID == userId) ?? new List<Game>(), desc);

        public Game GetGameById(long id) => LoadGames()?.FirstOrDefault(x => x.GameID == id);

        public void UpdateUser(long id, string username, int? competitivePoints, string battleTag)
        {
            var users = LoadUsers().ToList();
            User user = users.First(x => x.UserId == id);
            user.Username = username;
            user.CompetitivePoints = competitivePoints;
            user.BattleTag = battleTag;
            SaveUsers(users);
        }

        public bool ChangePassword(long id, string username, string oldPassword, string newPassword) => throw new InvalidOperationException("That operation does not apply to the current " + nameof(IAsyncDataProvider));

        public void AddGame(long id, Game g)
        {
            var games = LoadGames()?.ToList() ?? new List<Game>();
            games.Add(g);
            SaveGames(games);
        }

        public void UpdateGame(Game g)
        {
            var games = LoadGames().ToList();
            Game gameToUpdate = games.First(x => x.GameID == g.GameID);

            gameToUpdate.AttackFirst = g.AttackFirst;
            gameToUpdate.Date = g.Date;
            gameToUpdate.GameLength = g.GameLength;
            gameToUpdate.GameWon = g.GameWon;
            gameToUpdate.GroupSize = g.GroupSize;
            gameToUpdate.Heroes = g.Heroes;
            gameToUpdate.Map = g.Map;
            gameToUpdate.Notes = g.Notes;
            gameToUpdate.Rounds = g.Rounds;
            gameToUpdate.Score = g.Score;
            gameToUpdate.Season = g.Season;
            gameToUpdate.SkillRating = g.SkillRating;
            gameToUpdate.SkillRatingDifference = g.SkillRatingDifference;
            gameToUpdate.UserID = g.UserID;

            SaveGames(games);
        }

        public void DeleteGame(long gameId, long userId)
        {
            var games = LoadGames().ToList();
            games.Remove(games.First(x => x.GameID == gameId));
            SaveGames(games);
        }

        public async Task<User> LoginAsync(string username, string password) => await AsyncHelpers.RunAsync(() => Login(username, password));

        public async Task<User> GetReadOnlyUserInfoAsync(string username) => await AsyncHelpers.RunAsync(() => GetReadOnlyUserInfo(username));

        public async Task<User> RegisterAsync(string username, string password) => await AsyncHelpers.RunAsync(() => Register(username, password));

        public async Task<GameCollection> GetAllGamesAsync(long userId, bool desc = true) => await AsyncHelpers.RunAsync(() => GetAllGames(userId, desc));

        public async Task<Game> GetGameByIdAsync(long id) => await AsyncHelpers.RunAsync(() => GetGameById(id));

        public async Task UpdateUserAsync(long id, string username, int? competitivePoints, string battleTag) => await AsyncHelpers.RunAsync(() => UpdateUser(id, username, competitivePoints, battleTag));

        public async Task<bool> ChangePasswordAsync(long id, string username, string oldPassword, string newPassword) => await AsyncHelpers.RunAsync(() => ChangePassword(id, username, oldPassword, newPassword));

        public async Task AddGameAsync(long id, Game g) => await AsyncHelpers.RunAsync(() => AddGame(id, g));

        public async Task UpdateGameAsync(Game g) => await AsyncHelpers.RunAsync(() => UpdateGame(g));

        public async Task DeleteGameAsync(long gameId, long userId) => await AsyncHelpers.RunAsync(() => DeleteGame(gameId, userId));

        private IEnumerable<User> LoadUsers()
        {
            if (!File.Exists(usersFilePath))
            {
                string directory = Path.GetDirectoryName(usersFilePath);
                if (Directory.Exists(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                File.Create(usersFilePath).Close();
            }
            return JsonConvert.DeserializeObject<IEnumerable<User>>(File.ReadAllText(usersFilePath));
        }

        private void SaveUsers(IEnumerable<User> users)
        {
            if (!File.Exists(usersFilePath))
            {
                string directory = Path.GetDirectoryName(usersFilePath);
                if (Directory.Exists(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                File.Create(usersFilePath).Close();
            }

            File.WriteAllText(usersFilePath, JsonConvert.SerializeObject(users));
        }

        private IEnumerable<Game> LoadGames()
        {
            if (!File.Exists(gamesFilePath))
            {
                string directory = Path.GetDirectoryName(gamesFilePath);
                if (Directory.Exists(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                File.Create(gamesFilePath).Close();
            }

            int gameId = 0;
            var games = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(gamesFilePath));
            games.ForEach(x => x.GameID = gameId++);
            return games.OrderByDescending(x => x.Date);
        }

        private void SaveGames(IEnumerable<Game> games)
        {
            if (!File.Exists(gamesFilePath))
            {
                string directory = Path.GetDirectoryName(gamesFilePath);
                if (Directory.Exists(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                File.Create(gamesFilePath).Close();
            }

            File.WriteAllText(gamesFilePath, JsonConvert.SerializeObject(games));
        }

    }
}
