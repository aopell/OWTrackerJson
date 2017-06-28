using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWTracker.Data
{
    public interface IDataProvider
    {
        User Login(string username, string password);

        User GetReadOnlyUserInfo(string username);

        User Register(string username, string password);

        GameCollection GetAllGames(long userId, bool desc = true);

        Game GetGameById(long id);

        void UpdateUser(long id, string username, int? competitivePoints, string battleTag);

        bool ChangePassword(long id, string username, string oldPassword, string newPassword);

        void AddGame(long id, Game g);

        void UpdateGame(Game g);

        void DeleteGame(long gameId, long userId);
    }

    public interface IAsyncDataProvider : IDataProvider
    {
        Task<User> LoginAsync(string username, string password);

        Task<User> GetReadOnlyUserInfoAsync(string username);

        Task<User> RegisterAsync(string username, string password);

        Task<GameCollection> GetAllGamesAsync(long userId, bool desc = true);

        Task<Game> GetGameByIdAsync(long id);

        Task UpdateUserAsync(long id, string username, int? competitivePoints, string battleTag);

        Task<bool> ChangePasswordAsync(long id, string username, string oldPassword, string newPassword);

        Task AddGameAsync(long id, Game g);

        Task UpdateGameAsync(Game g);

        Task DeleteGameAsync(long gameId, long userId);
    }
}
