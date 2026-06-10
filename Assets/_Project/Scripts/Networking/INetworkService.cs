using System;
using System.Threading.Tasks;

namespace FarmEmpire.Networking
{
    public interface INetworkService
    {
        event Action<string> OnStatusMessage;
        event Action<bool> OnConnectionStatus;

        Task<bool> CreateRoomAsync(string roomName, int maxPlayers, bool isPrivate);
        Task<bool> JoinRoomAsync(string joinCode);
        Task LeaveRoomAsync();
    }
}
