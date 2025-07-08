using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public interface ISaveLoadService
    {
        Task SaveBonsaiAsync(Bonsai bonsai);
        Task SaveBonsaiAsync(Bonsai bonsai, ShopManager? shopManager);
        Task<Bonsai> LoadBonsaiAsync();
        Task<(Bonsai Bonsai, List<string> UnlockedShopItems)> LoadBonsaiAsync(ShopManager? shopManager);
        Task<bool> SaveExistsAsync();
        Task<bool> DeleteSaveAsync();
        bool CanSave();
        long GetMaxSaveFileSize();
        TimeSpan GetMinSaveInterval();
    }
}